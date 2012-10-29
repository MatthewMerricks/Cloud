using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using System.Xml.XPath;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Event
{
    public partial class Router : ServiceBase
    {
        internal class Configurator : ServiceThread
        {
            internal class ConfigEvent : Event
            {
                private string data;
                /// <summary>
                /// Contents of the config file
                /// </summary>
                public string Data
                {
                    get
                    {
                        return data;
                    }
                    set
                    {
                        data = value;
                    }
                }

                /// <summary>
                /// Base constructor to create a new config event
                /// </summary>
                public ConfigEvent() :
                    base()
                {
                }

                /// <summary>
                /// Base constructor to create a new config event from a serialized event
                /// </summary>
                /// <param name="serializationData">Serialized event buffer</param>
                public ConfigEvent(byte[] serializationData) :
                    base(serializationData)
                {
                }

                public override void GetObjectData(WspBuffer buffer)
                {
                    buffer.AddElement(@"Data", data);
                }
            }

            public override void Start()
            {
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                string prevValue = string.Empty;
                long nextSendTick = DateTime.Now.Ticks;
                PublishManager pubMgr;
                ConfigEvent mgmtEvent;

                try
                {
                    try
                    {
                        Manager.ThreadInitialize.Release();
                    }
                    catch
                    {
                        // If the thread is restarted, this could throw an exception but just ignore
                    }

                    pubMgr = new PublishManager();

                    mgmtEvent = new ConfigEvent();

                    while (true)
                    {
                        if (string.Compare(role, @"origin", true) == 0)
                        {
                            if (nextSendTick <= DateTime.Now.Ticks)
                            {
                                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                                    AppDomain.CurrentDomain.FriendlyName + ".config";

                                LoadConfiguration();

                                mgmtEvent.Data = File.ReadAllText(configFile);
                                mgmtEvent.EventType = mgmtGroup;

                                if (autoConfig == true)
                                {
                                    pubMgr.Publish(mgmtEvent.Serialize());
                                }

                                nextSendTick = DateTime.Now.Ticks + 600000000;
                            }
                        }

                        try
                        {
                            element = mgmtQueue.Dequeue();

                            if (element.Equals(defaultElement) == true)
                            {
                                element = newElement;
                                elementRetrieved = false;
                            }
                            else
                            {
                                elementRetrieved = true;

                                forwarderQueue.Enqueue(element);
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (autoConfig == true && string.Compare(role, @"origin", true) != 0 && elementRetrieved == true)
                        {
                            try
                            {
                                ConfigEvent configEvent = new ConfigEvent(element.SerializedEvent);

                                if (string.Compare(prevValue, configEvent.Data, true) != 0)
                                {
                                    SaveNewConfigFile(configEvent.Data);

                                    LoadConfiguration();

                                    prevValue = configEvent.Data;
                                }
                            }
                            catch
                            {
                                EventLog.WriteEntry("WspEventRouter", "Could not deserialize a ConfigEvent.", EventLogEntryType.Warning);
                            }
                        }
                    }
                }

                catch (ThreadAbortException)
                {
                    // Another thread has signalled that this worker
                    // thread must terminate.  Typically, this occurs when
                    // the main service thread receives a service stop 
                    // command.
                }

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }
            }
        }

        internal static void LoadConfiguration()
        {
            string modifiedRoleName;
            string configValueIn;
            string machineNameIn;
            string nodeName;
            int numConnections;
            int portIn;
            int bufferSizeIn;
            int timeoutIn;

            string configFile;

            lock (configFileLock)
            {
            Restart:

                configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                     AppDomain.CurrentDomain.FriendlyName + ".config";

                XPathDocument document = new XPathDocument(configFile);
                XPathNavigator navigator = document.CreateNavigator();
                XPathNodeIterator iterator;

                if (File.Exists(configFile) == false)
                {
                    EventLog.WriteEntry("WspEventRouter", "No config file can be found.", EventLogEntryType.Error);

                    throw new Exception("No config file found");
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/eventRouterSettings/configInfo");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"autoConfig", String.Empty);
                        if (configValueIn.Length != 0)
                            autoConfig = bool.Parse(configValueIn);

                        bootstrapUrl = iterator.Current.GetAttribute(@"bootstrapUrl", String.Empty);

                        configValueIn = iterator.Current.GetAttribute(@"mgmtGroup", String.Empty);
                        if (configValueIn.Length != 0)
                            mgmtGroup = new Guid(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"cmdGroup", String.Empty);
                        if (configValueIn.Length != 0)
                            cmdGroup = new Guid(configValueIn);
                        else
                            cmdGroup = Guid.NewGuid();

                        configValueIn = iterator.Current.GetAttribute(@"role", String.Empty);
                        if (configValueIn.Length != 0)
                        {
                            role = configValueIn;
                        }
                    }
                }
                catch
                {
                    if (mgmtGroup == Guid.Empty)
                    {
                        if (autoConfig == true)
                        {
                            autoConfig = false;
                        }

                        mgmtGroup = Guid.NewGuid();
                    }

                    if (cmdGroup == Guid.Empty)
                    {
                        cmdGroup = Guid.NewGuid();
                    }
                }

                if (autoConfig == true && mgmtGroup == Guid.Empty)
                {
                    string originConfig = GetOriginConfig(bootstrapUrl);

                    SaveNewConfigFile(originConfig);

                    goto Restart;
                }
                else
                {
                    try
                    {
                        if (Router.LocalRouterName.Length >= 3)
                        {
                            modifiedRoleName = Router.LocalRouterName.Substring(0, 3).ToLower() + role;
                        }
                        else
                        {
                            modifiedRoleName = role;
                        }

                        nodeName = @"/configuration/eventRouterSettings/" + modifiedRoleName + @"RoleInfo/";

                        iterator = navigator.Select(@"/configuration/eventRouterSettings/" + modifiedRoleName + @"RoleInfo");

                        if (iterator.Count == 0)
                        {
                            nodeName = @"/configuration/eventRouterSettings/" + role + @"RoleInfo/";
                        }
                    }
                    catch
                    {
                        nodeName = @"/configuration/eventRouterSettings/" + role + @"RoleInfo/";
                    }

                    try
                    {
                        iterator = navigator.Select(nodeName + @"subscriptionManagement");

                        if (iterator.MoveNext() == true)
                        {
                            configValueIn = iterator.Current.GetAttribute(@"refreshIncrement", String.Empty);
                            if (configValueIn.Length != 0)
                                subscriptionRefreshIncrement = UInt32.Parse(configValueIn);

                            configValueIn = iterator.Current.GetAttribute(@"expirationIncrement", String.Empty);
                            if (configValueIn.Length != 0)
                                subscriptionExpirationIncrement = UInt32.Parse(configValueIn);
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        iterator = navigator.Select(nodeName + @"localPublish");

                        if (iterator.MoveNext() == true)
                        {
                            configValueIn = iterator.Current.GetAttribute(@"eventQueueName", String.Empty);
                            if (configValueIn.Length != 0)
                                eventQueueName = configValueIn;

                            configValueIn = iterator.Current.GetAttribute(@"eventQueueSize", String.Empty);
                            if (configValueIn.Length != 0)
                                eventQueueSize = UInt32.Parse(configValueIn);

                            configValueIn = iterator.Current.GetAttribute(@"averageEventSize", String.Empty);
                            if (configValueIn.Length != 0)
                                averageEventSize = Int32.Parse(configValueIn);
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        iterator = navigator.Select(nodeName + @"outputCommunicationQueues");

                        if (iterator.MoveNext() == true)
                        {
                            configValueIn = iterator.Current.GetAttribute(@"maxQueueSize", String.Empty);
                            if (configValueIn.Length != 0)
                                thisOutQueueMaxSize = Int32.Parse(configValueIn);

                            configValueIn = iterator.Current.GetAttribute(@"maxTimeout", String.Empty);
                            if (configValueIn.Length != 0)
                                thisOutQueueMaxTimeout = Int32.Parse(configValueIn);
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        iterator = navigator.Select(nodeName + @"thisRouter");

                        if (iterator.MoveNext() == true)
                        {
                            thisNic = iterator.Current.GetAttribute(@"nic", String.Empty).Trim();

                            configValueIn = iterator.Current.GetAttribute(@"port", String.Empty).Trim();
                            if (configValueIn.Length == 0)
                            {
                                thisPort = 0;
                            }
                            else
                            {
                                thisPort = int.Parse(configValueIn);
                            }

                            configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                            if (configValueIn.Length != 0)
                                thisBufferSize = int.Parse(configValueIn);

                            configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                            if (configValueIn.Length != 0)
                                thisTimeout = int.Parse(configValueIn);
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        iterator = navigator.Select(nodeName + @"parentRouter");

                        while (iterator.MoveNext() == true)
                        {
                            machineNameIn = iterator.Current.GetAttribute(@"name", String.Empty).Trim();

                            numConnections = int.Parse(iterator.Current.GetAttribute(@"numConnections", String.Empty));

                            portIn = int.Parse(iterator.Current.GetAttribute(@"port", String.Empty));

                            configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                            if (configValueIn.Length != 0)
                                bufferSizeIn = int.Parse(configValueIn);
                            else
                                bufferSizeIn = thisBufferSize;

                            configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                            if (configValueIn.Length != 0)
                                timeoutIn = int.Parse(configValueIn);
                            else
                                timeoutIn = thisTimeout;

                            parentRoute = new Route(machineNameIn, numConnections, portIn, bufferSizeIn, timeoutIn);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        internal static void SaveNewConfigFile(string originConfig)
        {
            if (string.IsNullOrEmpty(originConfig) == true)
            {
                EventLog.WriteEntry("WspEventRouter", "Machine cannot connect to Origin router to get bootstrap config data.", EventLogEntryType.Error);

                throw new Exception("Cannot access the origin router to bootstrap the config settings");
            }

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(originConfig);

            XmlNode node = doc.SelectSingleNode(@"descendant::eventRouterSettings");
            XmlElement root = node["configInfo"];

            if (root.HasAttribute("mgmtGroup") == true)
            {
                String mgmtGroupValue = root.GetAttribute("mgmtGroup");

                if (string.IsNullOrEmpty(mgmtGroupValue) == true)
                {
                    EventLog.WriteEntry("WspEventRouter", "Config data from Origin router does not contain a mgmtGroup value.", EventLogEntryType.Error);

                    throw new Exception("Error in origin config file, not mgmtGroup value found");
                }
            }
            else
            {
                EventLog.WriteEntry("WspEventRouter", "Config data from Origin router does not contain a mgmtGroup value.", EventLogEntryType.Error);

                throw new Exception("Error in origin config file, not mgmtGroup value found");
            }

            root.SetAttribute("role", role);

            doc.Save(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + AppDomain.CurrentDomain.FriendlyName + ".config");
        }

        internal static string GetOriginConfig(string bootstrapUrl)
        {
            HttpWebRequest request;
            HttpWebResponse response;
            Stream receiveStream;
            StreamReader readStream;
            string responseValue;

            if (bootstrapUrl == string.Empty)
            {
                return string.Empty;
            }

            request = (HttpWebRequest)WebRequest.Create(bootstrapUrl);

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch
            {
                return string.Empty;
            }

            if (response.ContentLength == 0)
            {
                return string.Empty;
            }

            receiveStream = response.GetResponseStream();

            readStream = new StreamReader(receiveStream, Encoding.UTF8);

            responseValue = readStream.ReadToEnd();

            response.Close();
            readStream.Close();

            return responseValue;
        }

        internal static long GetConfigFileTick()
        {
            string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                AppDomain.CurrentDomain.FriendlyName + ".config";

            return (new FileInfo(configFile)).LastWriteTimeUtc.Ticks;
        }

        internal static void LoadPersistConfig()
        {
            string configValueIn;
            Guid eventType;

            string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                AppDomain.CurrentDomain.FriendlyName + ".config";

            XPathDocument document = new XPathDocument(configFile);
            XPathNavigator navigator = document.CreateNavigator();
            XPathNodeIterator iterator;

            Persister.lastConfigFileTick = GetConfigFileTick();

            iterator = navigator.Select(@"/configuration/eventPersistSettings/event");

            while (iterator.MoveNext() == true)
            {
                PersistEventInfo eventInfo;

                configValueIn = iterator.Current.GetAttribute(@"type", String.Empty);

                eventType = new Guid(configValueIn);

                if (Persister.persistEvents.TryGetValue(eventType, out eventInfo) == false)
                {
                    eventInfo = new PersistEventInfo();

                    eventInfo.OutFileName = null;
                    eventInfo.OutStream = null;
                }

                eventInfo.InUse = true;
                eventInfo.Loaded = true;

                eventInfo.PersistEventType = eventType;

                configValueIn = iterator.Current.GetAttribute(@"localOnly", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.LocalOnly = true;
                }
                else
                {
                    eventInfo.LocalOnly = bool.Parse(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"maxFileSize", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == false)
                    eventInfo.MaxFileSize = long.Parse(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"maxCopyInterval", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == false)
                    eventInfo.CopyIntervalTicks = long.Parse(configValueIn) * 10000000;

                configValueIn = iterator.Current.GetAttribute(@"createEmptyFiles", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == false && string.Compare(configValueIn, "true", true) == 0)
                    eventInfo.CreateEmptyFiles = true;

                configValueIn = iterator.Current.GetAttribute(@"fieldTerminator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.FieldTerminator = ',';
                }
                else
                {
                    eventInfo.FieldTerminator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"rowTerminator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.RowTerminator = '\n';
                }
                else
                {
                    eventInfo.RowTerminator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"keyValueSeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.KeyValueSeparator = ':';
                }
                else
                {
                    eventInfo.KeyValueSeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"beginObjectSeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.BeginObjectSeparator = '{';
                }
                else
                {
                    eventInfo.BeginObjectSeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"endObjectSeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.EndObjectSeparator = '}';
                }
                else
                {
                    eventInfo.EndObjectSeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"beginArraySeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.BeginArraySeparator = '[';
                }
                else
                {
                    eventInfo.BeginArraySeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"endArraySeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.EndArraySeparator = ']';
                }
                else
                {
                    eventInfo.EndArraySeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"stringCharacter", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.StringDelimiter = '"';
                }
                else
                {
                    eventInfo.StringDelimiter = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"escapeCharacter", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.EscapeCharacter = '\\';
                }
                else
                {
                    eventInfo.EscapeCharacter = configValueIn[0];
                }

                configValueIn = iterator.Current.GetAttribute(@"tempFileDirectory", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    configValueIn = @"C:\temp\" + Guid.NewGuid().ToString() + @"\";
                }

                eventInfo.TempFileDirectory = configValueIn;

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"copyToFileDirectory", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    configValueIn = eventInfo.TempFileDirectory + @"log\";
                }

                eventInfo.CopyToFileDirectory = configValueIn;

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                configValueIn = configValueIn + @"temp\";

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                Persister.persistEvents[eventType] = eventInfo;
            }
        }

        internal static long GetLocalConfigFileTick()
        {
            string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                AppDomain.CurrentDomain.FriendlyName + ".local.config";

            if (File.Exists(configFile) == false)
            {
                return 0;
            }

            return (new FileInfo(configFile)).LastWriteTimeUtc.Ticks;
        }

        internal static void LoadPersistLocalConfig()
        {
            string configValueIn;
            Guid eventType;

            string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                AppDomain.CurrentDomain.FriendlyName + ".local.config";

            if (File.Exists(configFile) == false)
            {
                return;
            }

            XPathDocument document = new XPathDocument(configFile);
            XPathNavigator navigator = document.CreateNavigator();
            XPathNodeIterator iterator;

            Persister.lastLocalConfigFileTick = GetLocalConfigFileTick();

            iterator = navigator.Select(@"/configuration/eventPersistSettings/event");

            while (iterator.MoveNext() == true)
            {
                PersistEventInfo eventInfo;

                configValueIn = iterator.Current.GetAttribute(@"type", String.Empty);

                eventType = new Guid(configValueIn);

                if (Persister.persistEvents.TryGetValue(eventType, out eventInfo) == false)
                {
                    eventInfo = new PersistEventInfo();

                    eventInfo.OutFileName = null;
                    eventInfo.OutStream = null;
                }

                eventInfo.InUse = true;
                eventInfo.Loaded = true;

                eventInfo.PersistEventType = eventType;

                configValueIn = iterator.Current.GetAttribute(@"localOnly", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.LocalOnly = true;
                }
                else
                {
                    eventInfo.LocalOnly = bool.Parse(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"maxFileSize", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == false)
                    eventInfo.MaxFileSize = long.Parse(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"maxCopyInterval", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == false)
                    eventInfo.CopyIntervalTicks = long.Parse(configValueIn) * 10000000;

                configValueIn = iterator.Current.GetAttribute(@"createEmptyFiles", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == false && string.Compare(configValueIn, "true", true) == 0)
                    eventInfo.CreateEmptyFiles = true;

                configValueIn = iterator.Current.GetAttribute(@"fieldTerminator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.FieldTerminator = ',';
                }
                else
                {
                    eventInfo.FieldTerminator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"rowTerminator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.RowTerminator = '\n';
                }
                else
                {
                    eventInfo.RowTerminator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"keyValueSeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.KeyValueSeparator = ':';
                }
                else
                {
                    eventInfo.KeyValueSeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"beginObjectSeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.BeginObjectSeparator = '{';
                }
                else
                {
                    eventInfo.BeginObjectSeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"endObjectSeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.EndObjectSeparator = '}';
                }
                else
                {
                    eventInfo.EndObjectSeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"beginArraySeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.BeginArraySeparator = '[';
                }
                else
                {
                    eventInfo.BeginArraySeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"endArraySeparator", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.EndArraySeparator = ']';
                }
                else
                {
                    eventInfo.EndArraySeparator = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"stringCharacter", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.StringDelimiter = '"';
                }
                else
                {
                    eventInfo.StringDelimiter = ConvertDelimeter(configValueIn);
                }

                configValueIn = iterator.Current.GetAttribute(@"escapeCharacter", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    eventInfo.EscapeCharacter = '\\';
                }
                else
                {
                    eventInfo.EscapeCharacter = configValueIn[0];
                }

                configValueIn = iterator.Current.GetAttribute(@"tempFileDirectory", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    configValueIn = @"C:\temp\" + Guid.NewGuid().ToString() + @"\";
                }

                eventInfo.TempFileDirectory = configValueIn;

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"copyToFileDirectory", String.Empty);
                if (string.IsNullOrEmpty(configValueIn) == true)
                {
                    configValueIn = eventInfo.TempFileDirectory + @"log\";
                }

                eventInfo.CopyToFileDirectory = configValueIn;

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                configValueIn = configValueIn + @"temp\";

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                Persister.persistEvents[eventType] = eventInfo;
            }
        }

        internal static char ConvertDelimeter(string delimeterIn)
        {
            char delimeterOut = ',';

            if (delimeterIn.Length == 1)
            {
                return delimeterIn[0];
            }

            switch (delimeterIn.Substring(0, 2))
            {
                case @"\a":
                    delimeterOut = '\a';
                    break;

                case @"\b":
                    delimeterOut = '\b';
                    break;

                case @"\f":
                    delimeterOut = '\f';
                    break;

                case @"\n":
                    delimeterOut = '\n';
                    break;

                case @"\r":
                    delimeterOut = '\r';
                    break;

                case @"\t":
                    delimeterOut = '\t';
                    break;

                case @"\v":
                    delimeterOut = '\v';
                    break;

                case @"\u":
                    if (delimeterIn.Length == 6)
                    {
                        int x = 0;

                        for (int i = 2; i < delimeterIn.Length; i++)
                        {
                            x = x << 4;

                            x = x + Convert.ToInt32(delimeterIn.Substring(i, 1));
                        }

                        delimeterOut = Convert.ToChar(x);
                    }
                    break;

                default:
                    break;
            }

            return delimeterOut;
        }
    }
}
