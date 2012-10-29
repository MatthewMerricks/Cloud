using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using System.Xml.XPath;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;
using Microsoft.WebSolutionsPlatform.Common;

namespace Microsoft.WebSolutionsPlatform.Event
{
    public partial class Router : ServiceBase
    {
        internal class CommandProcessor : ServiceThread
        {
            public override void Start()
            {
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                string prevValue = string.Empty;
                long nextSendTick = DateTime.Now.Ticks;
                PublishManager pubMgr;
                Regex targetFilter;
                bool processCommands = true;

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

                    while (true)
                    {
                        try
                        {
                            pubMgr = new PublishManager((uint)Router.thisTimeout);

                            break;
                        }
                        catch (SharedQueueException)
                        {
                            Thread.Sleep(10000);
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            element = cmdQueue.Dequeue();

                            if (element.Equals(defaultElement) == true)
                            {
                                element = newElement;
                                elementRetrieved = false;
                            }
                            else
                            {
                                elementRetrieved = true;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (elementRetrieved == true)
                        {
                            forwarderQueue.Enqueue(element);

                            CommandRequest request = new CommandRequest(element.SerializedEvent);
                            CommandResponse response = request.GetResponse();

                            if (string.IsNullOrEmpty(request.TargetMachineFilter) == false)
                            {
                                targetFilter = new Regex(request.TargetMachineFilter, RegexOptions.IgnoreCase);

                                if (targetFilter.IsMatch(LocalRouterName) == false)
                                {
                                    continue;
                                }
                            }

                            if (string.IsNullOrEmpty(request.TargetRoleFilter) == false)
                            {
                                targetFilter = new Regex(request.TargetRoleFilter, RegexOptions.IgnoreCase);

                                if (targetFilter.IsMatch(role) == false)
                                {
                                    continue;
                                }
                            }

                            if (processCommands == false && string.Compare("wsp_processcommands", request.Command, true) != 0)
                            {
                                continue;
                            }

                            switch (request.Command.ToLower())
                            {

                                case "wsp_processcommands":
                                    try
                                    {
                                        if (request.Arguments.Count > 0 && request.Arguments[0].GetType() == typeof(string))
                                        {
                                            response.ReturnCode = 0;

                                            if (string.Compare("true", (string)request.Arguments[0], true) == 0)
                                            {
                                                processCommands = true;
                                            }
                                            else
                                            {
                                                if (string.Compare("false", (string)request.Arguments[0], true) == 0)
                                                {
                                                    processCommands = false;
                                                }
                                                else
                                                {
                                                    response.Message = "Argument must be 'true' or 'false'";

                                                    response.ReturnCode = -1;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            response.Message = "Argument must be 'true' or 'false'";

                                            response.ReturnCode = -1;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getsysteminfo":
                                    try
                                    {
                                        AddDictionaryElement(response.Results, "HostName", Dns.GetHostName());

                                        try
                                        {
                                            AddDictionaryElement(response.Results, "SiteName", ActiveDirectorySite.GetComputerSite().Name);
                                        }
                                        catch
                                        {
                                        }

                                        try
                                        {
                                            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

                                            List<object> addresses = new List<object>(hostEntry.AddressList);
                                            List<string> aliases = new List<string>(hostEntry.Aliases);

                                            AddDictionaryElement(response.Results, "Addresses", addresses);
                                            AddDictionaryElement(response.Results, "Aliases", aliases);
                                        }
                                        catch
                                        {
                                        }

                                        Dictionary<string, object> osInfo = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "OperatingSystemInfo", osInfo);

                                        OperatingSystem os = Environment.OSVersion;

                                        AddDictionaryElement(osInfo, "ServicePack", os.ServicePack);
                                        AddDictionaryElement(osInfo, "Version", os.Version);
                                        AddDictionaryElement(osInfo, "VersionString", os.VersionString);

                                        Dictionary<string, object> timeInfo = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "TimeInfo", timeInfo);

                                        TimeZone timezone = TimeZone.CurrentTimeZone;
                                        DateTime time = DateTime.Now;

                                        AddDictionaryElement(timeInfo, "DaylightName", timezone.DaylightName);
                                        AddDictionaryElement(timeInfo, "StandardName", timezone.StandardName);
                                        AddDictionaryElement(timeInfo, "IsDaylightSavingTime", timezone.IsDaylightSavingTime(time));
                                        AddDictionaryElement(timeInfo, "UtcOffsetTicks", timezone.GetUtcOffset(time).Ticks);
                                        AddDictionaryElement(timeInfo, "LocalTimeTicks", time.Ticks);
                                        AddDictionaryElement(timeInfo, "UtcTimeTicks", time.ToUniversalTime().Ticks);

                                        try
                                        {
                                            RegistryKey regHKLM = Registry.LocalMachine;
                                            RegistryKey vmKey = regHKLM.OpenSubKey("software\\microsoft\\Virtual Machine\\Guest\\Parameters");

                                            if (vmKey != null)
                                            {
                                                Dictionary<string, object> vmInfo = new Dictionary<string, object>();

                                                AddDictionaryElement(response.Results, "VirtualMachineInfo", vmInfo);

                                                string[] paramNames = vmKey.GetValueNames();

                                                for (int i = 0; i < paramNames.Length; i++)
                                                {
                                                    AddDictionaryElement(vmInfo, paramNames[i], vmKey.GetValue(paramNames[i]));
                                                }
                                            }
                                        }
                                        catch
                                        {
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getregistrykeys":
                                    try
                                    {
                                        RegistryKey regHKLM = Registry.LocalMachine;

                                        bool allStrings = true;

                                        foreach (object obj in request.Arguments)
                                        {
                                            if (obj.GetType() != typeof(string))
                                            {
                                                allStrings = false;
                                                break;
                                            }
                                        }

                                        if (allStrings == false)
                                        {
                                            response.Message = "Error: Some or all the arguments are not strings";
                                            response.ReturnCode = -1;
                                        }
                                        else
                                        {
                                            foreach (object regKeyArg in request.Arguments)
                                            {
                                                Dictionary<string, object> regKeyInfo = new Dictionary<string, object>(2);

                                                AddDictionaryElement(response.Results, (string)regKeyArg, regKeyInfo);

                                                RegistryKey regKey = regHKLM.OpenSubKey((string)regKeyArg);

                                                if (regKey != null)
                                                {
                                                    regKeyInfo.Add("SubKeys", new List<string>(regKey.GetSubKeyNames()));

                                                    string[] regValueNames = regKey.GetValueNames();

                                                    Dictionary<string, object> regKeyValues = new Dictionary<string,object>(regValueNames.Length);
                                                    regKeyInfo.Add("Values", regKeyValues);

                                                    for(int i = 0; i < regValueNames.Length; i++)
                                                    {
                                                        AddDictionaryElement(regKeyValues, regValueNames[i], regKey.GetValue(regValueNames[i]));
                                                    }
                                                }
                                            }

                                            response.ReturnCode = 0;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getwspassemblyinfo":
                                    try
                                    {
                                        Assembly assembly = Assembly.GetExecutingAssembly();
                                        AddDictionaryElement(response.Results, "CodeBase", assembly.CodeBase);
                                        AddDictionaryElement(response.Results, "EscapedCodeBase", assembly.EscapedCodeBase);
                                        AddDictionaryElement(response.Results, "FullName", assembly.FullName);
                                        AddDictionaryElement(response.Results, "GlobalAssemblyCache", assembly.GlobalAssemblyCache);
                                        AddDictionaryElement(response.Results, "Location", assembly.Location);
                                        AddDictionaryElement(response.Results, "ReflectionOnly", assembly.ReflectionOnly);
                                        AddDictionaryElement(response.Results, "ImageRuntimeVersion", assembly.ImageRuntimeVersion);

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_geteventloginfo":
                                    try
                                    {
                                        Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                                        RegistryKey regHKLM = Registry.LocalMachine;
                                        RegistryKey regLogs = regHKLM.OpenSubKey("system\\currentcontrolset\\services\\eventlog");

                                        string[] logs = regLogs.GetSubKeyNames();

                                        for (int i = 0; i < logs.Length; i++)
                                        {
                                            Dictionary<string, object> logInfo = new Dictionary<string, object>();

                                            EventLog elog = new EventLog(logs[i]);

                                            AddDictionaryElement(response.Results, elog.Log, logInfo);

                                            AddDictionaryElement(logInfo, "EntriesCount", elog.Entries.Count);
                                            AddDictionaryElement(logInfo, "LogDisplayName", elog.LogDisplayName);
                                            AddDictionaryElement(logInfo, "MaximumKilobytes", elog.MaximumKilobytes);
                                            AddDictionaryElement(logInfo, "MinimumRetentionDays", elog.MinimumRetentionDays);
                                            AddDictionaryElement(logInfo, "OverflowAction", elog.OverflowAction.ToString());
                                            AddDictionaryElement(logInfo, "EnableRaisingEvents", elog.EnableRaisingEvents);

                                            RegistryKey logKey = regLogs.OpenSubKey(logs[i]);

                                            string[] sources = logKey.GetSubKeyNames();

                                            Dictionary<string, object> sourceInfo = new Dictionary<string, object>();

                                            for (int x = 0; x < sources.Length; x++)
                                            {
                                                Dictionary<string, object> sourceDetails = new Dictionary<string, object>();
                                                sourceInfo.Add(sources[x], sourceDetails);
                                            }

                                            AddDictionaryElement(logInfo, "Sources", sourceInfo);

                                            EventLogEntryCollection coll = elog.Entries;

                                            for (int y = 0; y < coll.Count; y++)
                                            {
                                                try
                                                {
                                                    object obj;
                                                    Dictionary<string, object> sourceDetails;
                                                    List<object> sourceTypeValues;

                                                    if (sourceInfo.TryGetValue(coll[y].Source, out obj) == true)
                                                    {
                                                        sourceDetails = (Dictionary<string, object>)obj;
                                                    }
                                                    else
                                                    {
                                                        sourceDetails = new Dictionary<string, object>();
                                                        sourceInfo.Add(coll[y].Source, sourceDetails);
                                                    }

                                                    if (sourceDetails.TryGetValue(coll[y].EntryType.ToString(), out obj) == true)
                                                    {
                                                        sourceTypeValues = (List<object>) obj;
                                                    }
                                                    else
                                                    {
                                                        sourceTypeValues = new List<object>(5);

                                                        sourceTypeValues.Add((int) 0);
                                                        sourceTypeValues.Add((int) 0);
                                                        sourceTypeValues.Add((int) 0);
                                                        sourceTypeValues.Add((int) 0);
                                                        sourceTypeValues.Add((int) 0);

                                                        sourceDetails.Add(coll[y].EntryType.ToString(), sourceTypeValues);
                                                    }

                                                    DateTime currentTime = DateTime.Now;

                                                    currentTime = currentTime.AddMinutes(-1);

                                                    if (currentTime.CompareTo(coll[y].TimeWritten) < 0)
                                                    {
                                                        sourceTypeValues[0] = (int)(sourceTypeValues[0]) + 1;
                                                    }
                                                    else
                                                    {
                                                        currentTime = currentTime.AddMinutes(-9);

                                                        if (currentTime.CompareTo(coll[y].TimeWritten) < 0)
                                                        {
                                                            sourceTypeValues[1] = (int)(sourceTypeValues[1]) + 1;
                                                        }
                                                        else
                                                        {
                                                            currentTime = currentTime.AddMinutes(-50);

                                                            if (currentTime.CompareTo(coll[y].TimeWritten) < 0)
                                                            {
                                                                sourceTypeValues[2] = (int)(sourceTypeValues[2]) + 1;
                                                            }
                                                            else
                                                            {
                                                                currentTime = currentTime.AddMinutes(-304);

                                                                if (currentTime.CompareTo(coll[y].TimeWritten) < 0)
                                                                {
                                                                    sourceTypeValues[3] = (int)(sourceTypeValues[3]) + 1;
                                                                }
                                                                else
                                                                {
                                                                    sourceTypeValues[4] = (int)(sourceTypeValues[4]) + 1;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    sourceInfo.Add(coll[y].Source, (int)0);
                                                }
                                            }
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    Thread.CurrentThread.Priority = ThreadPriority.Normal;

                                    break;

                                case "wsp_getdriveinfo":
                                    try
                                    {
                                        DriveInfo[] drives = DriveInfo.GetDrives();

                                        for (int i = 0; i < drives.Length; i++)
                                        {
                                            Dictionary<string, object> driveInfo = new Dictionary<string, object>();

                                            AddDictionaryElement(response.Results, drives[i].Name, driveInfo);

                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "AvailableFreeSpace", drives[i].AvailableFreeSpace);
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "DriveFormat", drives[i].DriveFormat);
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "DriveType", drives[i].DriveType.ToString());
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "IsReady", drives[i].IsReady);
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "RootDirectory", drives[i].RootDirectory.FullName);
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "TotalFreeSpace", drives[i].TotalFreeSpace);
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "TotalSize", drives[i].TotalSize);
                                            }
                                            catch
                                            {
                                            }
                                            try
                                            {
                                                AddDictionaryElement(driveInfo, "VolumeLabel", drives[i].VolumeLabel);
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getnetworkinfo":
                                    try
                                    {
                                        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                                        List<object> interfaceInfo = new List<object>();

                                        AddDictionaryElement(response.Results, "NetworkInterfaces", interfaceInfo);

                                        for (int i = 0; i < interfaces.Length; i++)
                                        {
                                            Dictionary<string, object> netInterface = new Dictionary<string, object>(3);

                                            interfaceInfo.Add(netInterface);

                                            AddDictionaryElement(netInterface, "Description", interfaces[i].Description);
                                            AddDictionaryElement(netInterface, "Id", interfaces[i].Id);
                                            AddDictionaryElement(netInterface, "IsReceiveOnly", interfaces[i].IsReceiveOnly);
                                            AddDictionaryElement(netInterface, "Name", interfaces[i].Name);
                                            AddDictionaryElement(netInterface, "NetworkInterfaceType", interfaces[i].NetworkInterfaceType.ToString());
                                            AddDictionaryElement(netInterface, "OperationalStatus", interfaces[i].OperationalStatus.ToString());
                                            AddDictionaryElement(netInterface, "Speed", interfaces[i].Speed);
                                            AddDictionaryElement(netInterface, "SupportsMulticast", interfaces[i].SupportsMulticast);

                                            IPInterfaceProperties interfaceProperties = interfaces[i].GetIPProperties();

                                            AddDictionaryElement(netInterface, "DnsSuffix", interfaceProperties.DnsSuffix);
                                            AddDictionaryElement(netInterface, "IsDnsEnabled", interfaceProperties.IsDnsEnabled);
                                            AddDictionaryElement(netInterface, "IsDynamicDnsEnabled", interfaceProperties.IsDynamicDnsEnabled);

                                            AddDictionaryElement(netInterface, "PhysicalAddress", interfaces[i].GetPhysicalAddress().ToString());

                                            List<object> addresses;
                                            IPInterfaceProperties iProps = interfaces[i].GetIPProperties();

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "AnycastAddresses", addresses);

                                            for (int x = 0; x < iProps.AnycastAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.AnycastAddresses[x].Address);
                                            }

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "DhcpServerAddresses", addresses);

                                            for (int x = 0; x < iProps.DhcpServerAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.DhcpServerAddresses[x].ToString());
                                            }

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "DnsAddresses", addresses);

                                            for (int x = 0; x < iProps.DnsAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.DnsAddresses[x].ToString());
                                            }

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "GatewayAddresses", addresses);

                                            for (int x = 0; x < iProps.GatewayAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.GatewayAddresses[x].Address);
                                            }

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "MulticastAddresses", addresses);

                                            for (int x = 0; x < iProps.MulticastAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.MulticastAddresses[x].Address);
                                            }

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "UnicastAddresses", addresses);

                                            for (int x = 0; x < iProps.UnicastAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.UnicastAddresses[x].Address);
                                            }

                                            addresses = new List<object>();
                                            AddDictionaryElement(netInterface, "WinsServersAddresses", addresses);

                                            for (int x = 0; x < iProps.WinsServersAddresses.Count; x++)
                                            {
                                                addresses.Add(iProps.WinsServersAddresses[x].ToString());
                                            }
                                        }

                                        IPEndPoint[] tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

                                        List<object> tcpListenerInfo = new List<object>();

                                        AddDictionaryElement(response.Results, "TcpListeners", tcpListenerInfo);

                                        for (int i = 0; i < tcpListeners.Length; i++)
                                        {
                                            Dictionary<string, object> listener = new Dictionary<string, object>(3);
                                            
                                            tcpListenerInfo.Add(listener);

                                            AddDictionaryElement(listener, "Address", tcpListeners[i].Address);
                                            AddDictionaryElement(listener, "Port", tcpListeners[i].Port);
                                            AddDictionaryElement(listener, "AddressFamily", tcpListeners[i].AddressFamily.ToString());
                                        }

                                        IPEndPoint[] udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

                                        List<object> udpListenerInfo = new List<object>();

                                        AddDictionaryElement(response.Results, "UdpListeners", udpListenerInfo);

                                        for (int i = 0; i < udpListeners.Length; i++)
                                        {
                                            Dictionary<string, object> listener = new Dictionary<string, object>(3);

                                            udpListenerInfo.Add(listener);

                                            AddDictionaryElement(listener, "Address", udpListeners[i].Address);
                                            AddDictionaryElement(listener, "Port", udpListeners[i].Port);
                                            AddDictionaryElement(listener, "AddressFamily", udpListeners[i].AddressFamily.ToString());
                                        }

                                        TcpConnectionInformation[] tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

                                        List<object> tcpConnectionInfo = new List<object>();

                                        AddDictionaryElement(response.Results, "TcpConnections", tcpConnectionInfo);

                                        for (int i = 0; i < tcpConnections.Length; i++)
                                        {
                                            Dictionary<string, object> connections = new Dictionary<string, object>(7);

                                            tcpConnectionInfo.Add(connections);

                                            AddDictionaryElement(connections, "LocalAddress", tcpConnections[i].LocalEndPoint.Address);
                                            AddDictionaryElement(connections, "LocalPort", tcpConnections[i].LocalEndPoint.Port);
                                            AddDictionaryElement(connections, "LocalAddressFamily", tcpConnections[i].LocalEndPoint.AddressFamily.ToString());
                                            AddDictionaryElement(connections, "RemoteAddress", tcpConnections[i].RemoteEndPoint.Address);
                                            AddDictionaryElement(connections, "RemotePort", tcpConnections[i].RemoteEndPoint.Port);
                                            AddDictionaryElement(connections, "RemoteAddressFamily", tcpConnections[i].RemoteEndPoint.AddressFamily.ToString());
                                            AddDictionaryElement(connections, "State", tcpConnections[i].State.ToString());
                                        }

                                        IcmpV4Statistics icmpV4 = IPGlobalProperties.GetIPGlobalProperties().GetIcmpV4Statistics();

                                        Dictionary<string, object> icmpV4Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "IcmpV4Statistics", icmpV4Statistics);

                                        AddDictionaryElement(icmpV4Statistics, "AddressMaskRepliesReceived", icmpV4.AddressMaskRepliesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "AddressMaskRepliesSent", icmpV4.AddressMaskRepliesSent);
                                        AddDictionaryElement(icmpV4Statistics, "AddressMaskRequestsReceived", icmpV4.AddressMaskRequestsReceived);
                                        AddDictionaryElement(icmpV4Statistics, "AddressMaskRequestsSent", icmpV4.AddressMaskRequestsSent);
                                        AddDictionaryElement(icmpV4Statistics, "DestinationUnreachableMessagesReceived", icmpV4.DestinationUnreachableMessagesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "DestinationUnreachableMessagesSent", icmpV4.DestinationUnreachableMessagesSent);
                                        AddDictionaryElement(icmpV4Statistics, "EchoRepliesReceived", icmpV4.EchoRepliesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "EchoRepliesSent", icmpV4.EchoRepliesSent);
                                        AddDictionaryElement(icmpV4Statistics, "EchoRequestsReceived", icmpV4.EchoRequestsReceived);
                                        AddDictionaryElement(icmpV4Statistics, "EchoRequestsSent", icmpV4.EchoRequestsSent);
                                        AddDictionaryElement(icmpV4Statistics, "ErrorsReceived", icmpV4.ErrorsReceived);
                                        AddDictionaryElement(icmpV4Statistics, "ErrorsSent", icmpV4.ErrorsSent);
                                        AddDictionaryElement(icmpV4Statistics, "MessagesReceived", icmpV4.MessagesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "MessagesSent", icmpV4.MessagesSent);
                                        AddDictionaryElement(icmpV4Statistics, "ParameterProblemsReceived", icmpV4.ParameterProblemsReceived);
                                        AddDictionaryElement(icmpV4Statistics, "ParameterProblemsSent", icmpV4.ParameterProblemsSent);
                                        AddDictionaryElement(icmpV4Statistics, "RedirectsReceived", icmpV4.RedirectsReceived);
                                        AddDictionaryElement(icmpV4Statistics, "RedirectsSent", icmpV4.RedirectsSent);
                                        AddDictionaryElement(icmpV4Statistics, "SourceQuenchesReceived", icmpV4.SourceQuenchesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "SourceQuenchesSent", icmpV4.SourceQuenchesSent);
                                        AddDictionaryElement(icmpV4Statistics, "TimeExceededMessagesReceived", icmpV4.TimeExceededMessagesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "TimeExceededMessagesSent", icmpV4.TimeExceededMessagesSent);
                                        AddDictionaryElement(icmpV4Statistics, "TimestampRepliesReceived", icmpV4.TimestampRepliesReceived);
                                        AddDictionaryElement(icmpV4Statistics, "TimestampRepliesSent", icmpV4.TimestampRepliesSent);
                                        AddDictionaryElement(icmpV4Statistics, "TimestampRequestsReceived", icmpV4.TimestampRequestsReceived);
                                        AddDictionaryElement(icmpV4Statistics, "TimestampRequestsSent", icmpV4.TimestampRequestsSent);

                                        IcmpV6Statistics icmpV6 = IPGlobalProperties.GetIPGlobalProperties().GetIcmpV6Statistics();

                                        Dictionary<string, object> icmpV6Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "IcmpV6Statistics", icmpV6Statistics);

                                        AddDictionaryElement(icmpV6Statistics, "DestinationUnreachableMessagesReceived", icmpV6.DestinationUnreachableMessagesReceived);
                                        AddDictionaryElement(icmpV6Statistics, "DestinationUnreachableMessagesSent", icmpV6.DestinationUnreachableMessagesSent);
                                        AddDictionaryElement(icmpV6Statistics, "EchoRepliesReceived", icmpV6.EchoRepliesReceived);
                                        AddDictionaryElement(icmpV6Statistics, "EchoRepliesSent", icmpV6.EchoRepliesSent);
                                        AddDictionaryElement(icmpV6Statistics, "EchoRequestsReceived", icmpV6.EchoRequestsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "EchoRequestsSent", icmpV6.EchoRequestsSent);
                                        AddDictionaryElement(icmpV6Statistics, "ErrorsReceived", icmpV6.ErrorsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "ErrorsSent", icmpV6.ErrorsSent);
                                        AddDictionaryElement(icmpV6Statistics, "MembershipQueriesReceived", icmpV6.MembershipQueriesReceived);
                                        AddDictionaryElement(icmpV6Statistics, "MembershipQueriesSent", icmpV6.MembershipQueriesSent);
                                        AddDictionaryElement(icmpV6Statistics, "MembershipReductionsReceived", icmpV6.MembershipReductionsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "MembershipReductionsSent", icmpV6.MembershipReductionsSent);
                                        AddDictionaryElement(icmpV6Statistics, "MembershipReportsReceived", icmpV6.MembershipReportsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "MembershipReportsSent", icmpV6.MembershipReportsSent);
                                        AddDictionaryElement(icmpV6Statistics, "MessagesReceived", icmpV6.MessagesReceived);
                                        AddDictionaryElement(icmpV6Statistics, "MessagesSent", icmpV6.MessagesSent);
                                        AddDictionaryElement(icmpV6Statistics, "NeighborAdvertisementsReceived", icmpV6.NeighborAdvertisementsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "NeighborAdvertisementsSent", icmpV6.NeighborAdvertisementsSent);
                                        AddDictionaryElement(icmpV6Statistics, "NeighborSolicitsReceived", icmpV6.NeighborSolicitsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "NeighborSolicitsSent", icmpV6.NeighborSolicitsSent);
                                        AddDictionaryElement(icmpV6Statistics, "PacketTooBigMessagesReceived", icmpV6.PacketTooBigMessagesReceived);
                                        AddDictionaryElement(icmpV6Statistics, "PacketTooBigMessagesSent", icmpV6.PacketTooBigMessagesSent);
                                        AddDictionaryElement(icmpV6Statistics, "ParameterProblemsReceived", icmpV6.ParameterProblemsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "ParameterProblemsSent", icmpV6.ParameterProblemsSent);
                                        AddDictionaryElement(icmpV6Statistics, "RedirectsReceived", icmpV6.RedirectsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "RedirectsSent", icmpV6.RedirectsSent);
                                        AddDictionaryElement(icmpV6Statistics, "RouterAdvertisementsReceived", icmpV6.RouterAdvertisementsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "RouterAdvertisementsSent", icmpV6.RouterAdvertisementsSent);
                                        AddDictionaryElement(icmpV6Statistics, "RouterSolicitsReceived", icmpV6.RouterSolicitsReceived);
                                        AddDictionaryElement(icmpV6Statistics, "RouterSolicitsSent", icmpV6.RouterSolicitsSent);
                                        AddDictionaryElement(icmpV6Statistics, "TimeExceededMessagesReceived", icmpV6.TimeExceededMessagesReceived);
                                        AddDictionaryElement(icmpV6Statistics, "TimeExceededMessagesSent", icmpV6.TimeExceededMessagesSent);

                                        IPGlobalStatistics ipV4 = IPGlobalProperties.GetIPGlobalProperties().GetIPv4GlobalStatistics();

                                        Dictionary<string, object> ipV4Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "IpV4Statistics", icmpV4Statistics);

                                        AddDictionaryElement(ipV4Statistics, "DefaultTtl", ipV4.DefaultTtl);
                                        AddDictionaryElement(ipV4Statistics, "ForwardingEnabled", ipV4.ForwardingEnabled);
                                        AddDictionaryElement(ipV4Statistics, "NumberOfInterfaces", ipV4.NumberOfInterfaces);
                                        AddDictionaryElement(ipV4Statistics, "NumberOfIPAddresses", ipV4.NumberOfIPAddresses);
                                        AddDictionaryElement(ipV4Statistics, "NumberOfRoutes", ipV4.NumberOfRoutes);
                                        AddDictionaryElement(ipV4Statistics, "OutputPacketRequests", ipV4.OutputPacketRequests);
                                        AddDictionaryElement(ipV4Statistics, "OutputPacketRoutingDiscards", ipV4.OutputPacketRoutingDiscards);
                                        AddDictionaryElement(ipV4Statistics, "OutputPacketsDiscarded", ipV4.OutputPacketsDiscarded);
                                        AddDictionaryElement(ipV4Statistics, "OutputPacketsWithNoRoute", ipV4.OutputPacketsWithNoRoute);
                                        AddDictionaryElement(ipV4Statistics, "PacketFragmentFailures", ipV4.PacketFragmentFailures);
                                        AddDictionaryElement(ipV4Statistics, "PacketReassembliesRequired", ipV4.PacketReassembliesRequired);
                                        AddDictionaryElement(ipV4Statistics, "PacketReassemblyFailures", ipV4.PacketReassemblyFailures);
                                        AddDictionaryElement(ipV4Statistics, "PacketReassemblyTimeout", ipV4.PacketReassemblyTimeout);
                                        AddDictionaryElement(ipV4Statistics, "PacketsFragmented", ipV4.PacketsFragmented);
                                        AddDictionaryElement(ipV4Statistics, "PacketsReassembled", ipV4.PacketsReassembled);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPackets", ipV4.ReceivedPackets);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPacketsDelivered", ipV4.ReceivedPacketsDelivered);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPacketsDiscarded", ipV4.ReceivedPacketsDiscarded);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPacketsForwarded", ipV4.ReceivedPacketsForwarded);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPacketsWithAddressErrors", ipV4.ReceivedPacketsWithAddressErrors);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPacketsWithHeadersErrors", ipV4.ReceivedPacketsWithHeadersErrors);
                                        AddDictionaryElement(ipV4Statistics, "ReceivedPacketsWithUnknownProtocol", ipV4.ReceivedPacketsWithUnknownProtocol);

                                        IPGlobalStatistics ipV6 = IPGlobalProperties.GetIPGlobalProperties().GetIPv6GlobalStatistics();

                                        Dictionary<string, object> ipV6Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "IpV6Statistics", icmpV4Statistics);

                                        AddDictionaryElement(ipV6Statistics, "DefaultTtl", ipV6.DefaultTtl);
                                        AddDictionaryElement(ipV6Statistics, "ForwardingEnabled", ipV6.ForwardingEnabled);
                                        AddDictionaryElement(ipV6Statistics, "NumberOfInterfaces", ipV6.NumberOfInterfaces);
                                        AddDictionaryElement(ipV6Statistics, "NumberOfIPAddresses", ipV6.NumberOfIPAddresses);
                                        AddDictionaryElement(ipV6Statistics, "NumberOfRoutes", ipV6.NumberOfRoutes);
                                        AddDictionaryElement(ipV6Statistics, "OutputPacketRequests", ipV6.OutputPacketRequests);
                                        AddDictionaryElement(ipV6Statistics, "OutputPacketRoutingDiscards", ipV6.OutputPacketRoutingDiscards);
                                        AddDictionaryElement(ipV6Statistics, "OutputPacketsDiscarded", ipV6.OutputPacketsDiscarded);
                                        AddDictionaryElement(ipV6Statistics, "OutputPacketsWithNoRoute", ipV6.OutputPacketsWithNoRoute);
                                        AddDictionaryElement(ipV6Statistics, "PacketFragmentFailures", ipV6.PacketFragmentFailures);
                                        AddDictionaryElement(ipV6Statistics, "PacketReassembliesRequired", ipV6.PacketReassembliesRequired);
                                        AddDictionaryElement(ipV6Statistics, "PacketReassemblyFailures", ipV6.PacketReassemblyFailures);
                                        AddDictionaryElement(ipV6Statistics, "PacketReassemblyTimeout", ipV6.PacketReassemblyTimeout);
                                        AddDictionaryElement(ipV6Statistics, "PacketsFragmented", ipV6.PacketsFragmented);
                                        AddDictionaryElement(ipV6Statistics, "PacketsReassembled", ipV6.PacketsReassembled);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPackets", ipV6.ReceivedPackets);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPacketsDelivered", ipV6.ReceivedPacketsDelivered);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPacketsDiscarded", ipV6.ReceivedPacketsDiscarded);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPacketsForwarded", ipV6.ReceivedPacketsForwarded);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPacketsWithAddressErrors", ipV6.ReceivedPacketsWithAddressErrors);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPacketsWithHeadersErrors", ipV6.ReceivedPacketsWithHeadersErrors);
                                        AddDictionaryElement(ipV6Statistics, "ReceivedPacketsWithUnknownProtocol", ipV6.ReceivedPacketsWithUnknownProtocol);

                                        TcpStatistics tcpV4 = IPGlobalProperties.GetIPGlobalProperties().GetTcpIPv4Statistics();

                                        Dictionary<string, object> tcpV4Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "TcpV4Statistics", icmpV4Statistics);

                                        AddDictionaryElement(tcpV4Statistics, "ConnectionsAccepted", tcpV4.ConnectionsAccepted);
                                        AddDictionaryElement(tcpV4Statistics, "ConnectionsInitiated", tcpV4.ConnectionsInitiated);
                                        AddDictionaryElement(tcpV4Statistics, "CumulativeConnections", tcpV4.CumulativeConnections);
                                        AddDictionaryElement(tcpV4Statistics, "CurrentConnections", tcpV4.CurrentConnections);
                                        AddDictionaryElement(tcpV4Statistics, "ErrorsReceived", tcpV4.ErrorsReceived);
                                        AddDictionaryElement(tcpV4Statistics, "FailedConnectionAttempts", tcpV4.FailedConnectionAttempts);
                                        AddDictionaryElement(tcpV4Statistics, "MaximumConnections", tcpV4.MaximumConnections);
                                        AddDictionaryElement(tcpV4Statistics, "MaximumTransmissionTimeout", tcpV4.MaximumTransmissionTimeout);
                                        AddDictionaryElement(tcpV4Statistics, "MinimumTransmissionTimeout", tcpV4.MinimumTransmissionTimeout);
                                        AddDictionaryElement(tcpV4Statistics, "ResetConnections", tcpV4.ResetConnections);
                                        AddDictionaryElement(tcpV4Statistics, "ResetsSent", tcpV4.ResetsSent);
                                        AddDictionaryElement(tcpV4Statistics, "SegmentsReceived", tcpV4.SegmentsReceived);
                                        AddDictionaryElement(tcpV4Statistics, "SegmentsResent", tcpV4.SegmentsResent);
                                        AddDictionaryElement(tcpV4Statistics, "SegmentsSent", tcpV4.SegmentsSent);

                                        TcpStatistics tcpV6 = IPGlobalProperties.GetIPGlobalProperties().GetTcpIPv6Statistics();

                                        Dictionary<string, object> tcpV6Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "TcpV6Statistics", icmpV6Statistics);

                                        AddDictionaryElement(tcpV6Statistics, "ConnectionsAccepted", tcpV6.ConnectionsAccepted);
                                        AddDictionaryElement(tcpV6Statistics, "ConnectionsInitiated", tcpV6.ConnectionsInitiated);
                                        AddDictionaryElement(tcpV6Statistics, "CumulativeConnections", tcpV6.CumulativeConnections);
                                        AddDictionaryElement(tcpV6Statistics, "CurrentConnections", tcpV6.CurrentConnections);
                                        AddDictionaryElement(tcpV6Statistics, "ErrorsReceived", tcpV6.ErrorsReceived);
                                        AddDictionaryElement(tcpV6Statistics, "FailedConnectionAttempts", tcpV6.FailedConnectionAttempts);
                                        AddDictionaryElement(tcpV6Statistics, "MaximumConnections", tcpV6.MaximumConnections);
                                        AddDictionaryElement(tcpV6Statistics, "MaximumTransmissionTimeout", tcpV6.MaximumTransmissionTimeout);
                                        AddDictionaryElement(tcpV6Statistics, "MinimumTransmissionTimeout", tcpV6.MinimumTransmissionTimeout);
                                        AddDictionaryElement(tcpV6Statistics, "ResetConnections", tcpV6.ResetConnections);
                                        AddDictionaryElement(tcpV6Statistics, "ResetsSent", tcpV6.ResetsSent);
                                        AddDictionaryElement(tcpV6Statistics, "SegmentsReceived", tcpV6.SegmentsReceived);
                                        AddDictionaryElement(tcpV6Statistics, "SegmentsResent", tcpV6.SegmentsResent);
                                        AddDictionaryElement(tcpV6Statistics, "SegmentsSent", tcpV6.SegmentsSent);

                                        UdpStatistics udpV4 = IPGlobalProperties.GetIPGlobalProperties().GetUdpIPv4Statistics();

                                        Dictionary<string, object> udpV4Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "UdpV4Statistics", icmpV4Statistics);

                                        AddDictionaryElement(udpV4Statistics, "ConnectionsAccepted", udpV4.DatagramsReceived);
                                        AddDictionaryElement(udpV4Statistics, "ConnectionsInitiated", udpV4.DatagramsSent);
                                        AddDictionaryElement(udpV4Statistics, "CumulativeConnections", udpV4.IncomingDatagramsDiscarded);
                                        AddDictionaryElement(udpV4Statistics, "CurrentConnections", udpV4.IncomingDatagramsWithErrors);
                                        AddDictionaryElement(udpV4Statistics, "ErrorsReceived", udpV4.UdpListeners);

                                        UdpStatistics udpV6 = IPGlobalProperties.GetIPGlobalProperties().GetUdpIPv6Statistics();

                                        Dictionary<string, object> udpV6Statistics = new Dictionary<string, object>();

                                        AddDictionaryElement(response.Results, "UdpV6Statistics", icmpV6Statistics);

                                        AddDictionaryElement(udpV6Statistics, "ConnectionsAccepted", udpV6.DatagramsReceived);
                                        AddDictionaryElement(udpV6Statistics, "ConnectionsInitiated", udpV6.DatagramsSent);
                                        AddDictionaryElement(udpV6Statistics, "CumulativeConnections", udpV6.IncomingDatagramsDiscarded);
                                        AddDictionaryElement(udpV6Statistics, "CurrentConnections", udpV6.IncomingDatagramsWithErrors);
                                        AddDictionaryElement(udpV6Statistics, "ErrorsReceived", udpV6.UdpListeners);

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getfileversioninfo":
                                    try
                                    {
                                        FileVersionInfo fv;
                                        
                                        if(request.Arguments.Count > 0 && request.Arguments[0].GetType() == typeof(String))
                                        {
                                            fv = FileVersionInfo.GetVersionInfo((string) request.Arguments[0]);

                                            AddDictionaryElement(response.Results, "Comments", fv.Comments);
                                            AddDictionaryElement(response.Results, "CompanyName", fv.CompanyName);
                                            AddDictionaryElement(response.Results, "FileBuildPart", fv.FileBuildPart);
                                            AddDictionaryElement(response.Results, "FileDescription", fv.FileDescription);
                                            AddDictionaryElement(response.Results, "FileMajorPart", fv.FileMajorPart);
                                            AddDictionaryElement(response.Results, "FileMinorPart", fv.FileMinorPart);
                                            AddDictionaryElement(response.Results, "FileName", fv.FileName);
                                            AddDictionaryElement(response.Results, "FilePrivatePart", fv.FilePrivatePart);
                                            AddDictionaryElement(response.Results, "FileVersion", fv.FileVersion);
                                            AddDictionaryElement(response.Results, "InternalName", fv.InternalName);
                                            AddDictionaryElement(response.Results, "IsDebug", fv.IsDebug);
                                            AddDictionaryElement(response.Results, "IsPatched", fv.IsPatched);
                                            AddDictionaryElement(response.Results, "IsPreRelease", fv.IsPreRelease);
                                            AddDictionaryElement(response.Results, "IsPrivateBuild", fv.IsPrivateBuild);
                                            AddDictionaryElement(response.Results, "IsSpecialBuild", fv.IsSpecialBuild);
                                            AddDictionaryElement(response.Results, "Language", fv.Language);
                                            AddDictionaryElement(response.Results, "LegalCopyright", fv.LegalCopyright);
                                            AddDictionaryElement(response.Results, "LegalTrademarks", fv.LegalTrademarks);
                                            AddDictionaryElement(response.Results, "OriginalFilename", fv.OriginalFilename);
                                            AddDictionaryElement(response.Results, "PrivateBuild", fv.PrivateBuild);
                                            AddDictionaryElement(response.Results, "ProductBuildPart", fv.ProductBuildPart);
                                            AddDictionaryElement(response.Results, "ProductMajorPart", fv.ProductMajorPart);
                                            AddDictionaryElement(response.Results, "ProductMinorPart", fv.ProductMinorPart);
                                            AddDictionaryElement(response.Results, "ProductName", fv.ProductName);
                                            AddDictionaryElement(response.Results, "ProductPrivatePart", fv.ProductPrivatePart);
                                            AddDictionaryElement(response.Results, "ProductVersion", fv.ProductVersion);
                                            AddDictionaryElement(response.Results, "SpecialBuild", fv.SpecialBuild);
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (FileNotFoundException e)
                                    {
                                        response.Message = "File not found - " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getserviceinfo":
                                    try
                                    {
                                        ServiceController[] serviceList = ServiceController.GetServices();

                                        for (int i = 0; i < serviceList.Length; i++)
                                        {
                                            Dictionary<string, object> service = new Dictionary<string, object>();

                                            AddDictionaryElement(response.Results, serviceList[i].ServiceName, service);

                                            AddDictionaryElement(service, "CanPulseAndContinue", serviceList[i].CanPauseAndContinue);
                                            AddDictionaryElement(service, "CanShutdown", serviceList[i].CanShutdown);
                                            AddDictionaryElement(service, "CanStop", serviceList[i].CanStop);
                                            AddDictionaryElement(service, "DisplayName", serviceList[i].DisplayName);
                                            AddDictionaryElement(service, "ServiceName", serviceList[i].ServiceName);
                                            AddDictionaryElement(service, "ServiceType", serviceList[i].ServiceType.ToString());
                                            AddDictionaryElement(service, "Status", serviceList[i].Status.ToString());

                                            List<string> dependentList = new List<string>(serviceList[i].DependentServices.Length);

                                            AddDictionaryElement(service, "DependentServices", dependentList);

                                            for(int x = 0; x < serviceList[i].DependentServices.Length; x++)
                                            {
                                                try
                                                {
                                                    dependentList.Add(serviceList[i].DependentServices[x].ServiceName);
                                                }
                                                catch
                                                {
                                                }
                                            }

                                            List<string> dependedOnList = new List<string>(serviceList[i].ServicesDependedOn.Length);

                                            AddDictionaryElement(service, "ServicesDependedOn", dependedOnList);

                                            for (int x = 0; x < serviceList[i].ServicesDependedOn.Length; x++ )
                                            {
                                                try
                                                {
                                                    dependedOnList.Add(serviceList[i].ServicesDependedOn[x].ServiceName);
                                                }
                                                catch
                                                {
                                                }
                                            }
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getdeviceinfo":
                                    try
                                    {
                                        ServiceController[] deviceList = ServiceController.GetDevices();

                                        for (int i = 0; i < deviceList.Length; i++)
                                        {
                                            Dictionary<string, object> device = new Dictionary<string, object>();

                                            AddDictionaryElement(response.Results, deviceList[i].ServiceName, device);

                                            AddDictionaryElement(device, "CanPulseAndContinue", deviceList[i].CanPauseAndContinue);
                                            AddDictionaryElement(device, "CanShutdown", deviceList[i].CanShutdown);
                                            AddDictionaryElement(device, "CanStop", deviceList[i].CanStop);
                                            AddDictionaryElement(device, "DisplayName", deviceList[i].DisplayName);
                                            AddDictionaryElement(device, "ServiceName", deviceList[i].ServiceName);
                                            AddDictionaryElement(device, "ServiceType", deviceList[i].ServiceType.ToString());
                                            AddDictionaryElement(device, "Status", deviceList[i].Status.ToString());

                                            List<string> dependentList = new List<string>(deviceList[i].DependentServices.Length);

                                            AddDictionaryElement(device, "DependentServices", dependentList);

                                            for (int x = 0; x < deviceList[i].DependentServices.Length; x++)
                                            {
                                                dependentList.Add(deviceList[i].DependentServices[x].ServiceName);
                                            }

                                            List<string> dependedOnList = new List<string>(deviceList[i].ServicesDependedOn.Length);

                                            AddDictionaryElement(device, "ServicesDependedOn", dependedOnList);

                                            for (int x = 0; x < deviceList[i].ServicesDependedOn.Length; x++)
                                            {
                                                dependentList.Add(deviceList[i].ServicesDependedOn[x].ServiceName);
                                            }
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getprocessinfo":
                                    try
                                    {
                                        Process[] processes = Process.GetProcesses();

                                        for (int i = 0; i < processes.Length; i++)
                                        {
                                            Dictionary<string, object> processInfo = new Dictionary<string, object>();

                                            AddDictionaryElement(response.Results, processes[i].ProcessName + ":" + processes[i].Id, processInfo);

                                            try
                                            {
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "HasExited", processes[i].HasExited);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "ModuleName", processes[i].MainModule.ModuleName);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "ModuleFileName", processes[i].MainModule.FileName);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "MainWindowTitle", processes[i].MainWindowTitle);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "MaxWorkingSet", processes[i].MaxWorkingSet.ToInt64());
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "MinWorkingSet", processes[i].MinWorkingSet.ToInt64());
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "BasePriority", processes[i].BasePriority);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "EnableRaisingEvents", processes[i].EnableRaisingEvents);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "HandleCount", processes[i].HandleCount);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "Id", processes[i].Id);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "NonpagedSystemMemorySize64", processes[i].NonpagedSystemMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PagedMemorySize64", processes[i].PagedMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PagedSystemMemorySize64", processes[i].PagedSystemMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PeakPagedMemorySize64", processes[i].PeakPagedMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PeakVirtualMemorySize64", processes[i].PeakVirtualMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PeakWorkingSet64", processes[i].PeakWorkingSet64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PriorityBoostEnabled", processes[i].PriorityBoostEnabled);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PriorityClass", processes[i].PriorityClass.ToString());
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PrivateMemorySize64", processes[i].PrivateMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "PrivilegedProcessorTimeMilliseconds", processes[i].PrivilegedProcessorTime.TotalMilliseconds);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "ProcessName", processes[i].ProcessName);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "Responding", processes[i].Responding);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "SessionId", processes[i].SessionId);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "Arguments", processes[i].StartInfo.Arguments);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "UserName", processes[i].StartInfo.UserName);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "StartTimeTicks", processes[i].StartTime.Ticks);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "Threads", processes[i].Threads.Count);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "TotalProcessorTimeMilliseconds", processes[i].TotalProcessorTime.TotalMilliseconds);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "UserProcessorTimeMilliseconds", processes[i].UserProcessorTime.TotalMilliseconds);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "VirtualMemorySize64", processes[i].VirtualMemorySize64);
                                                }
                                                catch
                                                {
                                                }
                                                try
                                                {
                                                    AddDictionaryElement(processInfo, "WorkingSet64", processes[i].WorkingSet64);
                                                }
                                                catch
                                                {
                                                }

                                                List<string> moduleList = new List<string>();

                                                try
                                                {
                                                    for (int x = 0; x < processes[i].Modules.Count; x++)
                                                    {
                                                        moduleList.Add(processes[i].Modules[x].FileName);
                                                    }
                                                }
                                                catch
                                                {
                                                }

                                                AddDictionaryElement(processInfo, "Modules", moduleList);
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                case "wsp_getperformancecounters":
                                    try
                                    {
                                        if (request.Arguments.Count == 0)
                                        {
                                            response.Message = "Error: No arguments provided";
                                            response.ReturnCode = -1;
                                        }
                                        else
                                        {
                                            bool allStrings = true;

                                            foreach (object obj in request.Arguments)
                                            {
                                                if (obj.GetType() != typeof(string))
                                                {
                                                    allStrings = false;
                                                    break;
                                                }
                                            }

                                            if (allStrings == false)
                                            {
                                                response.Message = "Error: Some or all the arguments are not strings";
                                                response.ReturnCode = -1;
                                            }
                                            else
                                            {
                                                foreach (object perfCategoryName in request.Arguments)
                                                {
                                                    Dictionary<string, object> perfCounterDictionary = new Dictionary<string, object>();

                                                    if (PerformanceCounterCategory.Exists((string)perfCategoryName) == false)
                                                    {
                                                        AddDictionaryElement(response.Results, (string)perfCategoryName, perfCounterDictionary);
                                                        break;
                                                    }

                                                    PerformanceCounterCategory perfCounterCategory = new PerformanceCounterCategory((string)perfCategoryName);

                                                    if (perfCounterCategory.CategoryType == PerformanceCounterCategoryType.Unknown)
                                                    {
                                                        AddDictionaryElement(response.Results, (string)perfCategoryName, perfCounterDictionary);
                                                        continue;
                                                    }

                                                    if (perfCounterCategory.CategoryType == PerformanceCounterCategoryType.SingleInstance)
                                                    {
                                                        PerformanceCounter[] perfCounters = perfCounterCategory.GetCounters();

                                                        for (int i = 0; i < perfCounters.Length; i++ )
                                                        {
                                                            perfCounters[i].NextValue();
                                                        }

                                                        Thread.Sleep(1000);

                                                        for (int i = 0; i < perfCounters.Length; i++)
                                                        {
                                                            AddDictionaryElement(perfCounterDictionary, perfCounters[i].CounterName, perfCounters[i].NextValue());
                                                        }

                                                        AddDictionaryElement(response.Results, (string)perfCategoryName, perfCounterDictionary);
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        string[] instanceNames = perfCounterCategory.GetInstanceNames();

                                                        if (instanceNames.Length == 0)
                                                        {
                                                            AddDictionaryElement(response.Results, (string)perfCategoryName, perfCounterDictionary);
                                                            continue;
                                                        }

                                                        Dictionary<string, PerformanceCounter[]> instanceDictionary = new Dictionary<string, PerformanceCounter[]>();

                                                        for (int i = 0; i < instanceNames.Length; i++)
                                                        {
                                                            PerformanceCounter[] perfCounters = perfCounterCategory.GetCounters(instanceNames[i]);

                                                            instanceDictionary.Add(instanceNames[i], perfCounters);

                                                            for (int x = 0; x < perfCounters.Length; x++)
                                                            {
                                                                perfCounters[x].NextValue();
                                                            }
                                                        }

                                                        Thread.Sleep(1000);

                                                        for (int i = 0; i < instanceNames.Length; i++)
                                                        {
                                                            Dictionary<string, object> perfInstanceDictionary = new Dictionary<string, object>();

                                                            PerformanceCounter[] perfCounters = instanceDictionary[instanceNames[i]];

                                                            for (int x = 0; x < perfCounters.Length; x++)
                                                            {
                                                                AddDictionaryElement(perfInstanceDictionary, perfCounters[x].CounterName, perfCounters[x].NextValue());
                                                            }

                                                            AddDictionaryElement(perfCounterDictionary, instanceNames[i], perfInstanceDictionary);
                                                        }

                                                        AddDictionaryElement(response.Results, (string)perfCategoryName, perfCounterDictionary);
                                                        continue;
                                                    }
                                                }
                                            }
                                        }

                                        response.ReturnCode = 0;
                                    }
                                    catch (Exception e)
                                    {
                                        response.Message = "Error: " + e.Message;
                                        response.ResponseException = e;

                                        response.ReturnCode = -1;
                                    }

                                    break;

                                default:
                                    response.Message = "Error: Unknown command";
                                    response.ReturnCode = -1;
                                    break;
                            }

                            if (response.ReturnCode == 0)
                            {
                                EventLog.WriteEntry("WspEventRouter", FormatMessage(request, response), EventLogEntryType.Information);
                            }
                            else
                            {
                                EventLog.WriteEntry("WspEventRouter", FormatMessage(request, response), EventLogEntryType.Warning);
                            }

                            try
                            {
                                pubMgr.Publish(response.Serialize());
                            }
                            catch
                            {
                                EventLog.WriteEntry("WspEventRouter",
                                    "Error trying to publish command response event: Command = " + request.Command + ", Correlation ID = " + response.CorrelationID.ToString(),
                                    EventLogEntryType.Warning);
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

            internal void AddDictionaryElement(Dictionary<string, object> dictionary, string key, object value)
            {
                if (value == null)
                {
                    dictionary[key] = string.Empty;
                }
                else
                {
                    dictionary[key] = value;
                }
            }

            internal string FormatMessage(CommandRequest request, CommandResponse response)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Originating Machine: ");
                sb.Append(request.OriginatingRouterName);
                sb.Append('\n');

                sb.Append("Command Time: ");
                sb.Append(new DateTime(request.EventTime).ToShortDateString());
                sb.Append(" ");
                sb.Append(new DateTime(request.EventTime).ToLongTimeString());
                sb.Append('\n');

                sb.Append("Command: ");
                sb.Append(request.Command);
                sb.Append('\n');

                if (request.Arguments.Count == 0)
                {
                    sb.Append("Argument1: <empty>");
                    sb.Append('\n');
                }
                else
                {
                    try
                    {
                        for (int i = 0; i < request.Arguments.Count; i++)
                        {
                            sb.Append("Argument");
                            sb.Append(i.ToString());
                            sb.Append(": ");
                            sb.Append(request.Arguments[i].ToString());
                            sb.Append('\n');
                        }
                    }
                    catch
                    {
                    }
                }

                sb.Append("TargetMachineFilter: ");
                sb.Append(request.TargetMachineFilter);
                sb.Append('\n');

                sb.Append("TargetRoleFilter: ");
                sb.Append(request.TargetRoleFilter);
                sb.Append('\n');

                sb.Append("TimeToLive: ");
                sb.Append(request.TimeToLive.ToString());
                sb.Append('\n');

                sb.Append("EventIdForResponse: ");
                sb.Append(request.EventIdForResponse.ToString());
                sb.Append('\n');

                sb.Append("CorrelationID: ");
                sb.Append(request.CorrelationID.ToString());
                sb.Append('\n');

                sb.Append("ReturnCode: ");
                sb.Append(response.ReturnCode.ToString());
                sb.Append('\n');

                sb.Append("Message: ");
                sb.Append(response.Message);
                sb.Append('\n');

                if (sb.Length > 32760)
                {
                    return sb.ToString(0, 32760);
                }
                else
                {
                    return sb.ToString();
                }
            }
        }
    }
}
