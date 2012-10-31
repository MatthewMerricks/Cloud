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
using System.Xml.XPath;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Event
{
    public partial class Router : ServiceBase
    {
        internal class PersistEventInfo
        {
            internal bool InUse;
            internal bool Loaded;
            internal string CopyToFileDirectory;
            internal string TempFileDirectory;
            internal long MaxFileSize;
            internal long CopyIntervalTicks;
            internal long NextCopyTick;
            internal bool CreateEmptyFiles;
            internal string OutFileName;
            internal StreamWriter OutStream;
            internal char FieldTerminator;
            internal char RowTerminator;
            internal char KeyValueSeparator;
            internal char BeginObjectSeparator;
            internal char EndObjectSeparator;
            internal char BeginArraySeparator;
            internal char EndArraySeparator;
            internal char StringDelimiter;
            internal char EscapeCharacter;
            internal Subscription subscription;
            
            internal Guid persistEventType;
            internal Guid PersistEventType
            {
                get
                {
                    return persistEventType;
                }
                set
                {
                    persistEventType = value;
                    subscription.SubscriptionEventType = value;
                }
            }

            internal bool localOnly;
            internal bool LocalOnly
            {
                get
                {
                    return localOnly;
                }
                set
                {
                    localOnly = value;
                    subscription.LocalOnly = value;
                }
            }

            internal PersistEventInfo()
            {
                InUse = false;
                Loaded = false;
                CopyIntervalTicks = 600000000;
                MaxFileSize = long.MaxValue - 1;
                CreateEmptyFiles = false;

                subscription = new Subscription();
                subscription.SubscriptionId = Guid.NewGuid();
                subscription.Subscribe = true;
            }
        }

        internal class Persister : ServiceThread
        {
            internal static bool copyInProcess = false;
            internal static bool localOnly = true;

            internal static long lastConfigFileTick;
            internal static long nextConfigFileCheckTick = 0;

            internal static long lastLocalConfigFileTick;

            internal static PublishManager pubMgr = null;

            internal static Dictionary<Guid, PersistEventInfo> persistEvents = new Dictionary<Guid, PersistEventInfo>();

            private Stack<PersistFileEvent> persistFileEvents;

            private long nextCopyTick = 0;

            private string fileNameBase = string.Empty;
            private string fileNameSuffix = string.Empty;

            private QueueElement element;
            private QueueElement defaultElement = default(QueueElement);

            public Persister()
            {
                fileNameBase = Dns.GetHostName() + @".Events.";
                fileNameSuffix = @".txt";

                persistFileEvents = new Stack<PersistFileEvent>();
            }

            public override void Start()
            {
                bool elementRetrieved;
                WspBuffer serializedEvent;
                long currentTick;
                long configFileTick;
                long localConfigFileTick;
                PersistEventInfo eventInfo;
                string eventFieldTerminator = @",";
                StreamWriter eventStream;

                string originatingRouterName;
                string inRouterName;
                Guid eventType; ;

                string propName;
                byte propType;
                object propValue;

                try
                {
                    Manager.ThreadInitialize.Release();
                }
                catch
                {
                    // If the thread is restarted, this could throw an exception but just ignore
                }

                try
                {
                    pubMgr = new PublishManager();

                    while (true)
                    {
                        currentTick = DateTime.UtcNow.Ticks;

                        if (currentTick > nextConfigFileCheckTick)
                        {
                            nextConfigFileCheckTick = currentTick + 300000000;

                            configFileTick = Router.GetConfigFileTick();
                            localConfigFileTick = Router.GetLocalConfigFileTick();

                            if (configFileTick != lastConfigFileTick || localConfigFileTick != lastLocalConfigFileTick)
                            {
                                nextCopyTick = 0;

                                foreach (PersistEventInfo eInfo in persistEvents.Values)
                                {
                                    eInfo.Loaded = false;
                                }

                                lock (configFileLock)
                                {
                                    Router.LoadPersistConfig();
                                    Router.LoadPersistLocalConfig();
                                }

                                foreach (PersistEventInfo eInfo in persistEvents.Values)
                                {
                                    if (eInfo.Loaded == false)
                                    {
                                        eInfo.InUse = false;
                                        eInfo.NextCopyTick = currentTick - 1;

                                        eInfo.subscription.Subscribe = false;
                                        pubMgr.Publish(eInfo.subscription.Serialize());
                                    }
                                }

                                lastConfigFileTick = configFileTick;
                                lastLocalConfigFileTick = localConfigFileTick;
                            }

                            foreach (PersistEventInfo eInfo in persistEvents.Values)
                            {
                                if (eInfo.InUse == true)
                                {
                                    eInfo.subscription.Subscribe = true;
                                    pubMgr.Publish(eInfo.subscription.Serialize());
                                }
                            }
                        }

                        try
                        {
                            element = persisterQueue.Dequeue();

                            if (element.Equals(defaultElement) == true)
                            {
                                elementRetrieved = false;
                            }
                            else
                            {
                                elementRetrieved = true;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            elementRetrieved = false;
                        }

                        currentTick = DateTime.UtcNow.Ticks;

                        if (currentTick > nextCopyTick)
                        {
                            nextCopyTick = long.MaxValue;

                            foreach (PersistEventInfo persistEventInfo in persistEvents.Values)
                            {
                                if (currentTick > persistEventInfo.NextCopyTick)
                                {
                                    persistEventInfo.NextCopyTick = currentTick + persistEventInfo.CopyIntervalTicks;

                                    if (persistEventInfo.OutStream != null)
                                    {
                                        persistEventInfo.OutStream.Close();

                                        SendPersistEvent(PersistFileState.Close, persistEventInfo, persistEventInfo.OutFileName);

                                        persistEventInfo.OutStream = null;
                                    }

                                    if (persistEventInfo.InUse == true)
                                    {
                                        persistEventInfo.OutFileName = persistEventInfo.TempFileDirectory + fileNameBase + DateTime.UtcNow.ToString("u").Replace(":", "-") + fileNameSuffix;

                                        if (File.Exists(persistEventInfo.OutFileName) == true)
                                            persistEventInfo.OutStream = new StreamWriter(File.Open(persistEventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                        else
                                            persistEventInfo.OutStream = new StreamWriter(File.Open(persistEventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                        SendPersistEvent(PersistFileState.Open, persistEventInfo, persistEventInfo.OutFileName);
                                    }
                                }

                                if (persistEventInfo.NextCopyTick < nextCopyTick)
                                {
                                    nextCopyTick = persistEventInfo.NextCopyTick;
                                }
                            }

                            if (copyInProcess == false)
                            {
                                Thread copyThread = new Thread(new ThreadStart(CopyFile));

                                copyInProcess = true;

                                copyThread.Start();
                            }
                        }

                        if (elementRetrieved == true)
                        {
                            eventInfo = persistEvents[element.EventType];

                            if (eventInfo.InUse == true)
                            {
                                eventFieldTerminator = eventInfo.FieldTerminator.ToString();

                                eventStream = eventInfo.OutStream;

                                if (eventStream == null)
                                {
                                    eventInfo.NextCopyTick = DateTime.UtcNow.Ticks + eventInfo.CopyIntervalTicks;

                                    eventInfo.OutFileName = eventInfo.TempFileDirectory + fileNameBase + DateTime.UtcNow.ToString("u").Replace(":", "-") + fileNameSuffix;

                                    if (File.Exists(eventInfo.OutFileName) == true)
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                    else
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                    eventStream = eventInfo.OutStream;

                                    if (eventInfo.NextCopyTick < nextCopyTick)
                                    {
                                        nextCopyTick = eventInfo.NextCopyTick;
                                    }

                                    SendPersistEvent(PersistFileState.Open, eventInfo, eventInfo.OutFileName);
                                }

                                eventStream.Write(eventInfo.BeginObjectSeparator);

                                //eventStream.Write(eventInfo.StringDelimiter.ToString());
                                //eventStream.Write("EventType");
                                //eventStream.Write(eventInfo.StringDelimiter.ToString());
                                //eventStream.Write(eventInfo.KeyValueSeparator.ToString());
                                //eventStream.Write(eventInfo.StringDelimiter.ToString());
                                //eventStream.Write(CleanseString(element.EventType.ToString(), eventInfo));
                                //eventStream.Write(eventInfo.StringDelimiter.ToString());
                                //eventStream.Write(eventInfo.FieldTerminator.ToString());

                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write("OriginatingRouterName");
                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write(eventInfo.KeyValueSeparator.ToString());
                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write(CleanseString(element.OriginatingRouterName, eventInfo));
                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write(eventInfo.FieldTerminator.ToString());

                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write("InRouterName");
                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write(eventInfo.KeyValueSeparator.ToString());
                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                eventStream.Write(CleanseString(element.InRouterName, eventInfo));
                                eventStream.Write(eventInfo.StringDelimiter.ToString());

                                serializedEvent = new WspBuffer(element.SerializedEvent);

                                if (serializedEvent.GetHeader(out originatingRouterName, out inRouterName, out eventType) == false)
                                {
                                    throw new EventDeserializationException("Error reading OriginatingRouterName from serializedEvent");
                                }

                                while (serializedEvent.Position < serializedEvent.Size)
                                {
                                    if (serializedEvent.Read(out propName) == false)
                                    {
                                        throw new EventDeserializationException("Error reading PropertyName from buffer");
                                    }

                                    eventStream.Write(eventInfo.FieldTerminator.ToString());
                                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                                    eventStream.Write(CleanseString(propName, eventInfo));
                                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                                    eventStream.Write(eventInfo.KeyValueSeparator.ToString());

                                    if (serializedEvent.Read(out propType) == false)
                                    {
                                        throw new EventDeserializationException("Error reading PropertyType from buffer");
                                    }

                                    switch (propType)
                                    {
                                        case (byte)PropertyType.StringDictionary:

                                            WriteStringDictionary(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.ObjectDictionary:

                                            WriteObjectDictionary(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.StringList:
                                            WriteStringList(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.ObjectList:
                                            WriteObjectList(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.ByteArray:
                                            WriteByteArray(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.CharArray:
                                            WriteCharArray(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.DateTime:
                                            WriteDateTime(eventStream, serializedEvent, eventInfo);
                                            break;

                                        case (byte)PropertyType.Int64:
                                            if (string.Compare(propName, "EventTime", true) == 0)
                                            {
                                                WriteDateTime(eventStream, serializedEvent, eventInfo);
                                            }
                                            else
                                            {
                                                if (serializedEvent.Read((PropertyType)propType, out propValue) == false)
                                                {
                                                    throw new EventDeserializationException("Error reading PropertyValue from buffer");
                                                }

                                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                                eventStream.Write(CleanseString(propValue.ToString(), eventInfo));
                                                eventStream.Write(eventInfo.StringDelimiter.ToString());
                                            }
                                            break;

                                        default:
                                            if (serializedEvent.Read((PropertyType)propType, out propValue) == false)
                                            {
                                                throw new EventDeserializationException("Error reading PropertyValue from buffer");
                                            }

                                            eventStream.Write(eventInfo.StringDelimiter.ToString());
                                            eventStream.Write(CleanseString(propValue.ToString(), eventInfo));
                                            eventStream.Write(eventInfo.StringDelimiter.ToString());
                                            break;
                                    }
                                }

                                eventStream.Write(eventInfo.EndObjectSeparator);

                                eventStream.Write(eventInfo.RowTerminator);

                                if (eventStream.BaseStream.Length >= eventInfo.MaxFileSize)
                                {
                                    eventInfo.NextCopyTick = currentTick - 1;
                                    nextCopyTick = eventInfo.NextCopyTick;
                                }
                            }
                        }
                    }
                }

                catch (IOException e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Error);
                    Thread.Sleep(60000);
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
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Error);
                    throw e;
                }
            }

            private void WriteStringDictionary(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                int dictCount;
                string stringKey;
                string stringValue;
                bool first = true;

                if (serializedEvent.Read(out dictCount) == false)
                {
                    throw new EventDeserializationException("Error reading StringDictionary length from buffer");
                }

                eventStream.Write(eventInfo.BeginObjectSeparator);

                for (int i = 0; i < dictCount; i++)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        eventStream.Write(eventInfo.FieldTerminator);
                    }

                    if (serializedEvent.Read(out stringKey) == false)
                    {
                        throw new EventDeserializationException("Error reading StringDictionary key from buffer");
                    }

                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                    eventStream.Write(CleanseString(stringKey, eventInfo));
                    eventStream.Write(eventInfo.StringDelimiter.ToString());

                    eventStream.Write(eventInfo.KeyValueSeparator);

                    if (serializedEvent.Read(out stringValue) == false)
                    {
                        throw new EventDeserializationException("Error reading StringDictionary value from buffer");
                    }

                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                    eventStream.Write(CleanseString(stringValue, eventInfo));
                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                }

                eventStream.Write(eventInfo.EndObjectSeparator);
            }

            private void WriteObjectDictionary(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                int dictCount;
                string stringKey;
                byte propType;
                object propValue;
                bool first = true;

                if (serializedEvent.Read(out dictCount) == false)
                {
                    throw new EventDeserializationException("Error reading StringDictionary length from buffer");
                }

                eventStream.Write(eventInfo.BeginObjectSeparator);

                for (int i = 0; i < dictCount; i++)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        eventStream.Write(eventInfo.FieldTerminator);
                    }

                    if (serializedEvent.Read(out stringKey) == false)
                    {
                        throw new EventDeserializationException("Error reading StringDictionary key from buffer");
                    }

                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                    eventStream.Write(CleanseString(stringKey, eventInfo));
                    eventStream.Write(eventInfo.StringDelimiter.ToString());

                    eventStream.Write(eventInfo.KeyValueSeparator);

                    if (serializedEvent.Read(out propType) == false)
                    {
                        throw new EventDeserializationException("Error reading PropertyType from buffer");
                    }

                    if ((PropertyType)propType == PropertyType.StringDictionary)
                    {
                        WriteStringDictionary(eventStream, serializedEvent, eventInfo);
                    }
                    else
                    {
                        if ((PropertyType)propType == PropertyType.ObjectDictionary)
                        {
                            WriteObjectDictionary(eventStream, serializedEvent, eventInfo);
                        }
                        else
                        {
                            if ((PropertyType)propType == PropertyType.StringList)
                            {
                                WriteStringList(eventStream, serializedEvent, eventInfo);
                            }
                            else
                            {
                                if ((PropertyType)propType == PropertyType.ObjectList)
                                {
                                    WriteObjectList(eventStream, serializedEvent, eventInfo);
                                }
                                else
                                {
                                    if (serializedEvent.Read((PropertyType)propType, out propValue) == false)
                                    {
                                        throw new EventDeserializationException("Error reading PropertyValue from buffer");
                                    }

                                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                                    eventStream.Write(CleanseString(propValue.ToString(), eventInfo));
                                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                                }
                            }
                        }
                    }
                }

                eventStream.Write(eventInfo.EndObjectSeparator);
            }

            private void WriteStringList(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                int listCount;
                string stringValue;
                bool first = true;

                if (serializedEvent.Read(out listCount) == false)
                {
                    throw new EventDeserializationException("Error reading List length from buffer");
                }

                eventStream.Write(eventInfo.BeginArraySeparator);

                for (int i = 0; i < listCount; i++)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        eventStream.Write(eventInfo.FieldTerminator);
                    }

                    if (serializedEvent.Read(out stringValue) == false)
                    {
                        throw new EventDeserializationException("Error reading List value from buffer");
                    }

                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                    eventStream.Write(CleanseString(stringValue, eventInfo));
                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                }

                eventStream.Write(eventInfo.EndArraySeparator);
            }

            private void WriteObjectList(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                int listCount;
                byte propType;
                object propValue;
                bool first = true;

                if (serializedEvent.Read(out listCount) == false)
                {
                    throw new EventDeserializationException("Error reading List length from buffer");
                }

                eventStream.Write(eventInfo.BeginArraySeparator);

                for (int i = 0; i < listCount; i++)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        eventStream.Write(eventInfo.FieldTerminator);
                    }

                    if (serializedEvent.Read(out propType) == false)
                    {
                        throw new EventDeserializationException("Error reading PropertyType from buffer");
                    }

                    if ((PropertyType)propType == PropertyType.StringDictionary)
                    {
                        WriteStringDictionary(eventStream, serializedEvent, eventInfo);
                    }
                    else
                    {
                        if ((PropertyType)propType == PropertyType.ObjectDictionary)
                        {
                            WriteObjectDictionary(eventStream, serializedEvent, eventInfo);
                        }
                        else
                        {
                            if ((PropertyType)propType == PropertyType.StringList)
                            {
                                WriteStringList(eventStream, serializedEvent, eventInfo);
                            }
                            else
                            {
                                if ((PropertyType)propType == PropertyType.ObjectList)
                                {
                                    WriteObjectList(eventStream, serializedEvent, eventInfo);
                                }
                                else
                                {
                                    if (serializedEvent.Read((PropertyType)propType, out propValue) == false)
                                    {
                                        throw new EventDeserializationException("Error reading PropertyValue from buffer");
                                    }

                                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                                    eventStream.Write(CleanseString(propValue.ToString(), eventInfo));
                                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                                }
                            }
                        }
                    }
                }

                eventStream.Write(eventInfo.EndArraySeparator);
            }

            private void WriteByteArray(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                byte[] byteArray;
                bool first = true;

                if (serializedEvent.Read(out byteArray) == false)
                {
                    throw new EventDeserializationException("Error reading List length from buffer");
                }

                eventStream.Write(eventInfo.BeginArraySeparator);

                for (int i = 0; i < byteArray.Length; i++)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        eventStream.Write(eventInfo.FieldTerminator);
                    }

                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                    eventStream.Write(CleanseString(byteArray[i].ToString(), eventInfo));
                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                }

                eventStream.Write(eventInfo.EndArraySeparator);
            }

            private void WriteCharArray(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                char[] charArray;
                bool first = true;

                if (serializedEvent.Read(out charArray) == false)
                {
                    throw new EventDeserializationException("Error reading List length from buffer");
                }

                eventStream.Write(eventInfo.BeginArraySeparator);

                for (int i = 0; i < charArray.Length; i++)
                {
                    if (first == true)
                    {
                        first = false;
                    }
                    else
                    {
                        eventStream.Write(eventInfo.FieldTerminator);
                    }

                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                    eventStream.Write(CleanseString(charArray[i].ToString(), eventInfo));
                    eventStream.Write(eventInfo.StringDelimiter.ToString());
                }

                eventStream.Write(eventInfo.EndArraySeparator);
            }

            private void WriteDateTime(StreamWriter eventStream, WspBuffer serializedEvent, PersistEventInfo eventInfo)
            {
                DateTime dateTime;

                if (serializedEvent.Read(out dateTime) == false)
                {
                    throw new EventDeserializationException("Error reading List length from buffer");
                }

                eventStream.Write(eventInfo.StringDelimiter.ToString());
                eventStream.Write(CleanseString(dateTime.ToString("o"), eventInfo));
                eventStream.Write(eventInfo.StringDelimiter.ToString());
            }

            private string CleanseString(string stringIn, PersistEventInfo eventInfo)
            {
                StringBuilder sb = new StringBuilder();

                if (string.IsNullOrEmpty(stringIn) == false)
                {
                    for (int i = 0; i < stringIn.Length; i++)
                    {
                        if (stringIn[i] == eventInfo.StringDelimiter)
                        {
                            sb.Append(eventInfo.EscapeCharacter);
                            sb.Append(eventInfo.StringDelimiter);
                            continue;
                        }
                        else if (stringIn[i] == eventInfo.EscapeCharacter)
                        {
                            if (eventInfo.EscapeCharacter == '\\' && (i + 1) < stringIn.Length)
                            {
                                switch (stringIn.Substring(i, 2))
                                {
                                    case @"\b":
                                        sb.Append(@"\b");
                                        i++;
                                        continue;

                                    case @"\f":
                                        sb.Append(@"\f");
                                        i++;
                                        continue;

                                    case @"\n":
                                        sb.Append(@"\n");
                                        i++;
                                        continue;

                                    case @"\r":
                                        sb.Append(@"\r");
                                        i++;
                                        continue;

                                    case @"\t":
                                        sb.Append(@"\t");
                                        i++;
                                        continue;

                                    case @"\u":
                                        sb.Append(@"\u");
                                        i++;
                                        continue;

                                    case @"\/":
                                        sb.Append(@"\/");
                                        i++;
                                        continue;

                                    case "\\\"":
                                        sb.Append("\\\"");
                                        i++;
                                        continue;

                                    case @"\\":
                                        sb.Append(@"\\");
                                        i++;
                                        continue;

                                    default:
                                        sb.Append(eventInfo.EscapeCharacter);
                                        sb.Append(eventInfo.EscapeCharacter);
                                        continue;
                                }
                            }
                            else
                            {
                                sb.Append(eventInfo.EscapeCharacter);
                                sb.Append(eventInfo.EscapeCharacter);
                                continue;
                            }
                        }
                        else
                        {
                            switch (stringIn[i])
                            {
                                case '\b':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('b');
                                    continue;

                                case '\f':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('f');
                                    continue;

                                case '\n':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('n');
                                    continue;

                                case '\r':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('r');
                                    continue;

                                case '\t':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('t');
                                    continue;

                                case '/':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('/');
                                    continue;

                                case '\"':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('\"');
                                    continue;

                                case '\\':
                                    sb.Append(eventInfo.EscapeCharacter);
                                    sb.Append('\\');
                                    continue;

                                default:
                                    sb.Append(stringIn[i]);
                                    continue;
                            }
                        }
                    }
                }

                return sb.ToString();
            }

            private void CopyFile()
            {
                PersistEventInfo eventInfo;
                bool inCopy = true;

                string[] files;

                FileInfo currentFileInfo;
                FileInfo listFileInfo = null;

                try
                {
                    copyInProcess = true;

                    foreach (Guid eventType in persistEvents.Keys)
                    {
                        eventInfo = persistEvents[eventType];

                        files = Directory.GetFiles(eventInfo.TempFileDirectory);
                        currentFileInfo = new FileInfo(eventInfo.OutFileName);

                        for (int i = 0; i < files.Length; i++)
                        {
                            try
                            {
                                listFileInfo = new FileInfo(files[i]);

                                if (eventInfo.InUse == false || string.Compare(currentFileInfo.Name, listFileInfo.Name, true) != 0)
                                {
                                    try
                                    {
                                        inCopy = true;

                                        File.Copy(files[i], eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, true);

                                        inCopy = false;

                                        SendPersistEvent(PersistFileState.Copy, eventInfo, files[i]);

                                        try
                                        {
                                            File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                            SendPersistEvent(PersistFileState.Move, eventInfo, eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                        }
                                        catch (IOException)
                                        {
                                            SendPersistEvent(PersistFileState.MoveFailed, eventInfo, eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);

                                            if (File.Exists(eventInfo.CopyToFileDirectory + listFileInfo.Name) == true &&
                                                new FileInfo(eventInfo.CopyToFileDirectory + listFileInfo.Name).Length ==
                                                new FileInfo(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name).Length)
                                            {
                                                File.Delete(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);
                                            }
                                            else
                                            {
                                                File.Delete(eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                                File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                                SendPersistEvent(PersistFileState.Move, eventInfo, eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                            }
                                        }

                                        File.Delete(files[i]);
                                    }
                                    catch
                                    {
                                        if (inCopy == true)
                                        {
                                            SendPersistEvent(PersistFileState.CopyFailed, eventInfo, files[i]);
                                        }
                                        else
                                        {
                                            SendPersistEvent(PersistFileState.MoveFailed, eventInfo, eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);
                                        }

                                        Directory.CreateDirectory(eventInfo.CopyToFileDirectory);
                                        Directory.CreateDirectory(eventInfo.CopyToFileDirectory + @"temp\");

                                        inCopy = true;

                                        File.Copy(files[i], eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, true);

                                        inCopy = false;

                                        SendPersistEvent(PersistFileState.Copy, eventInfo, files[i]);

                                        File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                        SendPersistEvent(PersistFileState.Move, eventInfo, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                        File.Delete(files[i]);
                                    }
                                }
                            }
                            catch
                            {
                                if (inCopy == true)
                                {
                                    SendPersistEvent(PersistFileState.CopyFailed, eventInfo, files[i]);
                                }
                                else
                                {
                                    if (listFileInfo == null)
                                    {
                                        SendPersistEvent(PersistFileState.MoveFailed, eventInfo, null);
                                    }
                                    else
                                    {
                                        SendPersistEvent(PersistFileState.MoveFailed, eventInfo, eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);
                                    }
                                }
                            }
                        }
                    }
                }

                catch (ThreadAbortException e)
                {
                    throw e;
                }

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }

                finally
                {
                    copyInProcess = false;
                }
            }

            private void SendPersistEvent(PersistFileState fileState, PersistEventInfo eventInfo, string outFileName)
            {
                PersistFileEvent persistFileEvent;

                try
                {
                    persistFileEvent = persistFileEvents.Pop();
                }
                catch
                {
                    persistFileEvent = new PersistFileEvent();
                }

                persistFileEvent.PersistEventType = eventInfo.PersistEventType;
                persistFileEvent.FileState = fileState;
                persistFileEvent.FileName = outFileName;
                persistFileEvent.SettingLocalOnly = eventInfo.LocalOnly;
                persistFileEvent.SettingMaxCopyInterval = (int)(eventInfo.CopyIntervalTicks / 10000000);
                persistFileEvent.SettingMaxFileSize = eventInfo.MaxFileSize;
                persistFileEvent.SettingFieldTerminator = eventInfo.FieldTerminator;
                persistFileEvent.SettingRowTerminator = eventInfo.RowTerminator;
                persistFileEvent.SettingBeginObjectSeparator = eventInfo.BeginObjectSeparator;
                persistFileEvent.SettingEndObjectSeparator = eventInfo.EndObjectSeparator;
                persistFileEvent.SettingBeginArraySeparator = eventInfo.BeginArraySeparator;
                persistFileEvent.SettingEndArraySeparator = eventInfo.EndArraySeparator;
                persistFileEvent.SettingKeyValueSeparator = eventInfo.KeyValueSeparator;
                persistFileEvent.SettingStringDelimiter = eventInfo.StringDelimiter;
                persistFileEvent.SettingEscapeCharacter = eventInfo.EscapeCharacter;
                persistFileEvent.FileNameBase = fileNameBase;

                if (fileState == PersistFileState.Open || outFileName == null)
                {
                    persistFileEvent.FileSize = 0;
                }
                else
                {
                    try
                    {
                        persistFileEvent.FileSize = (new FileInfo(outFileName)).Length;
                    }
                    catch
                    {
                        persistFileEvent.FileSize = 0;
                    }
                }

                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        pubMgr.Publish(persistFileEvent.Serialize());
                        break;
                    }
                    catch
                    {
                    }
                }

                persistFileEvents.Push(persistFileEvent);
            }
        }
    }
}
