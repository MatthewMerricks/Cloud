using System;
using System.Runtime.InteropServices;
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

namespace Microsoft.WebSolutionsPlatform.Event
{
    public partial class Router : ServiceBase
    {
        internal class ParentConnection
        {
            internal Thread parentInConnection;
            internal Thread parentOutConnection;

            internal ParentConnection()
            {
            }
        }

        internal class Communicator : ServiceThread
        {
            internal static Thread receiveServerThread;
            internal static bool abortParent = false;
            internal static List<ParentConnection> parentConnections = new List<ParentConnection>();
            internal static Thread distributeThread;

            internal static object threadQueuesLock = new object();

            internal static Dictionary<string, List<Thread>> commInThreads = new Dictionary<string, List<Thread>>(StringComparer.CurrentCultureIgnoreCase);
            internal static Dictionary<string, List<Thread>> commOutThreads = new Dictionary<string, List<Thread>>(StringComparer.CurrentCultureIgnoreCase);

            internal static Dictionary<string, SynchronizationQueue<QueueElement>> threadQueues =
                new Dictionary<string, SynchronizationQueue<QueueElement>>(StringComparer.CurrentCultureIgnoreCase);

            internal static Dictionary<string, object> deadThreadQueues = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);

            public Communicator()
            {
            }

            public override void Start()
            {
                int i;
                List<string> removeRouters = new List<string>();
                List<KeyValuePair<string, Thread>> removeThreads = new List<KeyValuePair<string, Thread>>();
                long currentTickTimeout;
                PerformanceCounter threadQueueCounter;

                while (true)
                {
                    try
                    {
                        if (thisPort != 0)
                        {
                            if (receiveServerThread == null || receiveServerThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                if (receiveServerThread != null &&
                                    receiveServerThread.ThreadState == System.Threading.ThreadState.Stopped)
                                {
                                    receiveServerThread.Abort();
                                }

                                receiveServerThread = new Thread(new ThreadStart(new ReceiveServer().Start));

                                receiveServerThread.Start();
                            }
                        }

                        if (distributeThread == null || distributeThread.ThreadState == System.Threading.ThreadState.Stopped)
                        {
                            if (distributeThread != null && distributeThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                distributeThread.Abort();
                            }

                            distributeThread = new Thread(new ThreadStart(new DistributeHandler().Start));

                            distributeThread.Start();
                        }

                        if (Router.parentRoute != null)
                        {
                            try
                            {
                                for (i = 0; i < parentConnections.Count; i++)
                                {
                                    ParentConnection parentConnection = parentConnections[i];

                                    if (parentConnection.parentInConnection == null || parentConnection.parentOutConnection == null)
                                    {
                                        abortParent = true;
                                    }

                                    if (abortParent == true ||
                                        (parentConnection.parentInConnection != null &&
                                            parentConnection.parentInConnection.ThreadState == System.Threading.ThreadState.Stopped) ||
                                        (parentConnection.parentOutConnection != null &&
                                            parentConnection.parentOutConnection.ThreadState == System.Threading.ThreadState.Stopped))
                                    {
                                        if (parentConnection.parentInConnection != null)
                                        {
                                            parentConnection.parentInConnection.Abort();
                                            parentConnection.parentInConnection = null;
                                        }

                                        if (parentConnection.parentOutConnection != null)
                                        {
                                            parentConnection.parentOutConnection.Abort();
                                            parentConnection.parentOutConnection = null;
                                        }

                                        parentConnections.RemoveAt(i);
                                        i--;
                                    }
                                }

                                for (i = 0; i + parentConnections.Count < parentRoute.NumConnections; i++)
                                {
                                    if (string.IsNullOrEmpty(parentRoute.RouterName) == true)
                                    {
                                        continue;
                                    }

                                    Socket parentSocket = OpenParentSocket();

                                    if (parentSocket != null)
                                    {
                                        ParentConnection parentConnection = new ParentConnection();

                                        parentConnection.parentInConnection =
                                            new Thread(new ThreadStart(new CommunicationHandler(parentSocket, parentRoute.RouterName, null).ConnectionInStart));

                                        parentConnection.parentInConnection.Start();

                                        ManualResetEvent startEvent = new ManualResetEvent(false);
                                        startEvent.Reset();

                                        parentConnection.parentOutConnection =
                                            new Thread(new ThreadStart(new CommunicationHandler(parentSocket, parentRoute.RouterName, startEvent).ConnectionOutStart));

                                        lock (threadQueuesLock)
                                        {
                                            List<Thread> threads;

                                            if (Communicator.commOutThreads.TryGetValue(parentRoute.RouterName, out threads) == false)
                                            {
                                                Communicator.commOutThreads[parentRoute.RouterName] = new List<Thread>();
                                            }

                                            Communicator.commOutThreads[parentRoute.RouterName].Add(parentConnection.parentOutConnection);
                                        }

                                        parentConnection.parentOutConnection.Start();

                                        startEvent.WaitOne(10000, false);

                                        parentConnections.Add(parentConnection);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        lock (threadQueuesLock)
                        {
                            foreach (string routerName in commInThreads.Keys)
                            {
                                foreach (Thread thread in commInThreads[routerName])
                                {
                                    if (thread.ThreadState == System.Threading.ThreadState.Stopped)
                                    {
                                        removeThreads.Add(new KeyValuePair<string, Thread>(routerName, thread));
                                        removeRouters.Add(routerName);
                                    }
                                }
                            }

                            if (removeThreads.Count > 0)
                            {
                                for (i = 0; i < removeThreads.Count; i++)
                                {
                                    removeThreads[i].Value.Abort();

                                    commInThreads[removeThreads[i].Key].Remove(removeThreads[i].Value);
                                }

                                removeThreads.Clear();
                            }

                            if (removeRouters.Count > 0)
                            {
                                List<Thread> threads;

                                for (i = 0; i < removeRouters.Count; i++)
                                {
                                    commInThreads.TryGetValue(removeRouters[i], out threads);

                                    if (threads != null && commInThreads[removeRouters[i]].Count == 0)
                                    {
                                        commInThreads.Remove(removeRouters[i]);
                                    }
                                }

                                removeRouters.Clear();
                            }

                            foreach (string routerName in commOutThreads.Keys)
                            {
                                foreach (Thread thread in commOutThreads[routerName])
                                {
                                    if (thread.ThreadState == System.Threading.ThreadState.Stopped)
                                    {
                                        removeThreads.Add(new KeyValuePair<string, Thread>(routerName, thread));
                                        removeRouters.Add(routerName);
                                    }
                                }
                            }

                            if (removeThreads.Count > 0)
                            {
                                for (i = 0; i < removeThreads.Count; i++)
                                {
                                    removeThreads[i].Value.Abort();

                                    commOutThreads[removeThreads[i].Key].Remove(removeThreads[i].Value);
                                }

                                removeThreads.Clear();
                            }

                            if (removeRouters.Count > 0)
                            {
                                for (i = 0; i < removeRouters.Count; i++)
                                {
                                    if (commOutThreads.ContainsKey(removeRouters[i]) == true)
                                    {
                                        if (commOutThreads[removeRouters[i]].Count == 0)
                                        {
                                            threadQueues[removeRouters[i]].InUse = false;
                                            threadQueues[removeRouters[i]].LastUsedTick = DateTime.Now.Ticks;

                                            Communicator.deadThreadQueues[removeRouters[i]] = null;

                                            commOutThreads.Remove(removeRouters[i]);
                                        }
                                    }
                                }

                                removeRouters.Clear();
                            }

                            if (deadThreadQueues.Count > 0)
                            {
                                currentTickTimeout = DateTime.Now.Ticks - (((long)thisOutQueueMaxTimeout) * 10000000);

                                removeRouters.Clear();

                                foreach (string threadQueueName in deadThreadQueues.Keys)
                                {
                                    try
                                    {
                                        if (threadQueues[threadQueueName].LastUsedTick < currentTickTimeout ||
                                            threadQueues[threadQueueName].Size > thisOutQueueMaxSize)
                                        {
                                            removeRouters.Add(threadQueueName);
                                        }
                                    }
                                    catch
                                    {
                                        removeRouters.Add(threadQueueName);
                                    }
                                }

                                if (removeRouters.Count > 0)
                                {
                                    for (i = 0; i < removeRouters.Count; i++)
                                    {
                                        try
                                        {
                                            threadQueueCounter = new PerformanceCounter();
                                            threadQueueCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                            threadQueueCounter.CategoryName = communicationCategoryName;
                                            threadQueueCounter.CounterName = forwarderQueueSizeName;
                                            threadQueueCounter.InstanceName = removeRouters[i];
                                            threadQueueCounter.ReadOnly = false;

                                            threadQueueCounter.RemoveInstance();

                                            Communicator.threadQueues[removeRouters[i]].Clear();
                                            Communicator.threadQueues.Remove(removeRouters[i]);
                                        }
                                        finally
                                        {
                                            deadThreadQueues.Remove(removeRouters[i]);
                                        }
                                    }

                                    removeRouters.Clear();
                                }
                            }
                        }

                        Thread.Sleep(10000);
                    }

                    catch (ThreadAbortException)
                    {
                        // Another thread has signalled that this worker
                        // thread must terminate.  Typically, this occurs when
                        // the main service thread receives a service stop 
                        // command.

                        if (distributeThread != null)
                        {
                            distributeThread.Abort();
                        }
                    }

                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }
                }
            }

            internal Socket OpenParentSocket()
            {
                Socket socket = null;
                SocketError socketError;
                string clientRouterName = string.Empty;
                byte[] preamble;
                byte[] inResponse = new byte[1];

                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];

                BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)(Router.parentRoute.Timeout / 1000)).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
                BitConverter.GetBytes((uint)10).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

                try
                {
                    clientRouterName = Router.parentRoute.RouterName;

                    socket = ConnectSocket(Router.parentRoute.RouterName, Router.parentRoute.Port);

                    if (socket == null)
                    {
                        return null;
                    }

                    socket.NoDelay = true;
                    socket.ReceiveTimeout = Router.parentRoute.Timeout;
                    socket.SendTimeout = Router.parentRoute.Timeout;

                    //socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                    //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    preamble = new byte[routerNameEncoded.Length + 4];

                    Buffer.BlockCopy(BitConverter.GetBytes((Int32)preamble.Length), 0, preamble, 0, 4);
                    Buffer.BlockCopy(routerNameEncoded, 0, preamble, 4, routerNameEncoded.Length);

                    socket.Send(preamble, 0, preamble.Length, SocketFlags.None, out socketError);

                    if (socketError != SocketError.Success)
                    {
                        CloseSocket(socket, clientRouterName);
                        socket = null;

                        //EventLog.WriteEntry("WspEventRouter", 
                        //    "Send failed to parent router " + clientRouterName + " with bad return code: " + socketError.ToString(), 
                        //    EventLogEntryType.Warning);

                        return null;
                    }

                    socket.Receive(inResponse, 0, inResponse.Length, SocketFlags.None, out socketError);

                    if (socketError != SocketError.Success || inResponse[0] != 1)
                    {
                        CloseSocket(socket, clientRouterName);
                        socket = null;

                        //EventLog.WriteEntry("WspEventRouter", 
                        //    "Receive failed to parent router " + clientRouterName + " with bad return code: " + socketError.ToString(), 
                        //    EventLogEntryType.Warning);

                        return null;
                    }
                }
                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }

                return socket;
            }

            private static Socket ConnectSocket(string server, int port)
            {
                Socket socket;
                IPHostEntry hostEntry = null;

                // Get host related information.
                hostEntry = Dns.GetHostEntry(server);

                // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
                // an exception that occurs when the host IP Address is not compatible with the address family
                // (typical in the IPv6 case).
                foreach (IPAddress address in hostEntry.AddressList)
                {
                    // Only use IPv4
                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        continue;
                    }

                    try
                    {
                        IPEndPoint ipe = new IPEndPoint(address, port);

                        socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        socket.Connect(ipe);

                        if (socket.Connected)
                        {
                            return socket;
                        }
                    }
                    catch
                    {
                        // Intentionally left empty, just loop to next.
                    }
                }

                return null;
            }

            internal static void CloseSocket(Socket socket, string clientRouterName)
            {
                if (socket != null)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Router:" + clientRouterName + "  " + e.ToString(),
                            EventLogEntryType.Warning);
                    }
                }
            }
        }

        internal class ReceiveServer : ServiceThread
        {
            private IPAddress thisAddress;
            private IPEndPoint thisEndPoint;

            public ReceiveServer()
            {
                if (thisNic == string.Empty)
                {
                    thisAddress = IPAddress.Any;
                }
                else
                {
                    IPHostEntry iph = Dns.GetHostEntry(thisNic);

                    thisAddress = null;

                    for (int i = 0; i < iph.AddressList.Length; i++)
                    {
                        if (thisNic == iph.AddressList[i].ToString())
                        {
                            thisAddress = iph.AddressList[i];

                            break;
                        }
                    }

                    if (thisAddress == null)
                    {
                        thisAddress = iph.AddressList[iph.AddressList.Length - 1];
                    }
                }

                thisEndPoint = new IPEndPoint(thisAddress, thisPort);
            }

            public override void Start()
            {
                Socket server = null;
                string clientRouterName = string.Empty;
                Thread commThread;

                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];

                BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)(thisTimeout / 1000)).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
                BitConverter.GetBytes((uint)10).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

                try
                {
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    server.NoDelay = true;
                    server.Bind(thisEndPoint);
                    server.Listen(20);

                    while (true)
                    {
                        clientRouterName = string.Empty;

                        Socket socket = server.Accept();

                        socket.NoDelay = true;
                        socket.ReceiveTimeout = thisTimeout;
                        socket.SendTimeout = thisTimeout;

                        //socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                        //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, inOptionValues);

                        commThread = new Thread(new ThreadStart(new CommunicationHandler(socket, clientRouterName, null).AcceptConnection));
                        commThread.Start();
                    }
                }

                catch
                {
                    if (server != null)
                    {
                        Communicator.CloseSocket(server, clientRouterName);
                    }
                }
            }
        }

        internal class ReceiveStateObject
        {
            public Socket socket;
            public SocketError socketError;
            public byte[] buffer;
            public ArrayList buffers;
            public int totalBytesRead;
            public ManualResetEvent receiveDone;
            public int currentReceiveLength;
            public byte[] currentReceiveLengthBytes;
            public int currentReceiveLengthBytesRead;
            public int currentProcessedLength;
            public string clientRouterName = string.Empty;
            public bool blocking;

            internal ReceiveStateObject()
            {
                socketError = SocketError.TryAgain;
                buffer = new byte[averageEventSize];
                buffers = new ArrayList(10);
                receiveDone = new ManualResetEvent(false);
                currentReceiveLengthBytes = new byte[4];
                blocking = false;
            }

            internal void Reset()
            {
                socketError = SocketError.TryAgain;
                totalBytesRead = 0;
                currentReceiveLength = 0;
                currentReceiveLengthBytesRead = 0;
                currentProcessedLength = 0;

                buffers.Clear();
                receiveDone.Reset();
            }
        }

        internal class CommunicationHandler : ServiceThread
        {
            private static ManualResetEvent sendDone = new ManualResetEvent(false);

            private Socket socket;
            private string clientRouterName;

            public ManualResetEvent startEvent;
            public ManualResetEvent parentStartEvent;

            internal PerformanceCounter threadQueueCounter;

            internal SynchronizationQueue<QueueElement> threadQueue;

            public CommunicationHandler(Socket socket, string clientRouterName, ManualResetEvent parentStartEvent)
            {
                this.socket = socket;
                this.clientRouterName = clientRouterName;
                this.parentStartEvent = parentStartEvent;
            }

            public override void Start()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public void AcceptConnection()
            {
                Thread commThread;

                try
                {
                    lock (Communicator.threadQueuesLock)
                    {
                        clientRouterName = InitConnection();
                    }
                }
                catch
                {
                    Communicator.CloseSocket(socket, clientRouterName);

                    socket = null;
                }

                if (socket != null)
                {
                    lock (Communicator.threadQueuesLock)
                    {
                        commThread = new Thread(new ThreadStart(new CommunicationHandler(socket, clientRouterName, null).ConnectionInStart));

                        List<Thread> threads;

                        if (Communicator.commInThreads.TryGetValue(clientRouterName, out threads) == false)
                        {
                            Communicator.commInThreads[clientRouterName] = new List<Thread>();
                        }

                        Communicator.commInThreads[clientRouterName].Add(commThread);

                        commThread.Start();

                        startEvent = new ManualResetEvent(false);
                        startEvent.Reset();

                        commThread = new Thread(new ThreadStart(new CommunicationHandler(socket, clientRouterName, startEvent).ConnectionOutStart));

                        if (Communicator.commOutThreads.TryGetValue(clientRouterName, out threads) == false)
                        {
                            Communicator.commOutThreads[clientRouterName] = new List<Thread>();
                        }

                        Communicator.commOutThreads[clientRouterName].Add(commThread);

                        commThread.Start();
                    }

                    startEvent.WaitOne(10000, false);
                }
            }

            private string InitConnection()
            {
                SocketError socketError;
                string clientRouterName = string.Empty;

                byte[] inResponse = new byte[1];
                byte[] outResponse = new byte[1];
                byte[] inBuffer = new byte[1000];
                ReceiveStateObject receiveState = new ReceiveStateObject();

                byte[] inStream = new byte[1000];

                receiveState.socket = socket;
                receiveState.clientRouterName = clientRouterName;
                receiveState.buffer = inStream;
                receiveState.receiveDone.Reset();

                socket.BeginReceive(receiveState.buffer, 0, receiveState.buffer.Length, SocketFlags.None,
                    InitialReceiveCallback, receiveState);

                receiveState.receiveDone.WaitOne();

                if (receiveState.socketError != SocketError.Success)
                {
                    Communicator.CloseSocket(socket, clientRouterName);
                    socket = null;

                    EventLog.WriteEntry("WspEventRouter", "Receive failed with bad return code: " + receiveState.socketError.ToString(), EventLogEntryType.Warning);

                    return string.Empty;
                }

                int preambleLength = BitConverter.ToInt32(receiveState.buffer, 0);

                UnicodeEncoding uniEncoding = new UnicodeEncoding();
                clientRouterName = uniEncoding.GetString(receiveState.buffer, 4, preambleLength - 4);

                if (preambleLength == receiveState.totalBytesRead)
                {
                    outResponse[0] = 1;
                }
                else
                {
                    outResponse[0] = 0;
                }

                socket.Send(outResponse, 0, 1, SocketFlags.None, out socketError);

                if (socketError != SocketError.Success || outResponse[0] == 0)
                {
                    Communicator.CloseSocket(socket, clientRouterName);
                    socket = null;

                    //if (socketError != SocketError.Success)
                    //{
                    //    EventLog.WriteEntry("WspEventRouter", "Send failed with bad return code: " + socketError.ToString(), EventLogEntryType.Warning);
                    //}
                    //else
                    //{
                    //    EventLog.WriteEntry("WspEventRouter", "Connection was rejected by parent with false return code", EventLogEntryType.Warning);
                    //}

                    return clientRouterName;
                }

                return clientRouterName;
            }

            private static void InitialReceiveCallback(IAsyncResult ar)
            {
                ReceiveStateObject receiveState = null;

                try
                {
                    receiveState = (ReceiveStateObject)ar.AsyncState;

                    Socket socket = receiveState.socket;

                    receiveState.totalBytesRead = socket.EndReceive(ar, out receiveState.socketError);
                }
                finally
                {
                    if (receiveState != null)
                    {
                        receiveState.receiveDone.Set();
                    }
                }
            }

            public void ConnectionInStart()
            {
                try
                {
                    InHandler(new ReceiveStateObject());
                }
                catch
                {
                    // Intentionally left blank. Just end the thread and cleanup.
                }
                finally
                {
                    try
                    {
                        if (socket != null)
                        {
                            Communicator.CloseSocket(socket, clientRouterName);
                        }
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }

                    socket = null;
                }

                return;
            }

            public void ConnectionOutStart()
            {
                bool exitThread = false;

                try
                {
                    try
                    {
                        lock (Communicator.threadQueuesLock)
                        {
                            if (Communicator.threadQueues.TryGetValue(clientRouterName, out threadQueue) == false)
                            {
                                threadQueueCounter = new PerformanceCounter();
                                threadQueueCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                threadQueueCounter.CategoryName = communicationCategoryName;
                                threadQueueCounter.CounterName = forwarderQueueSizeName;
                                threadQueueCounter.InstanceName = clientRouterName;
                                threadQueueCounter.ReadOnly = false;

                                threadQueue = new SynchronizationQueue<QueueElement>(threadQueueCounter);

                                Communicator.threadQueues[clientRouterName] = threadQueue;

                                threadQueue.InUse = true;

                                //SubscriptionMgr.ResendSubscriptions(clientRouterName);
                            }
                            else
                            {
                                if (threadQueue.InUse != true)
                                {
                                    threadQueue.InUse = true;

                                    if (Communicator.deadThreadQueues.ContainsKey(clientRouterName) == true)
                                    {
                                        Communicator.deadThreadQueues.Remove(clientRouterName);
                                    }

                                    //SubscriptionMgr.ResendSubscriptions(clientRouterName);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);

                        exitThread = true;
                    }
                    finally
                    {
                        parentStartEvent.Set();
                    }

                    if (exitThread == false)
                    {
                        OutHandler();
                    }
                }
                catch
                {
                    // Intentionally left blank. Just end the thread and cleanup.
                }
                finally
                {
                    try
                    {
                        Communicator.CloseSocket(socket, clientRouterName);
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }

                    socket = null;
                }

                return;
            }

            private void InHandler(ReceiveStateObject state)
            {
                Thread receiveMonitorThread;

                state.socket = this.socket;
                state.clientRouterName = this.clientRouterName;

                state.Reset();

                state.blocking = true;

                receiveMonitorThread = new Thread(new ThreadStart(new ReceiveMonitor(state, socket.ReceiveTimeout).Start));

                receiveMonitorThread.Start();

                state.socket.BeginReceive(state.buffer, 0, averageEventSize, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), state);

                state.receiveDone.WaitOne();

                state.blocking = false;
            }

            private static void ReceiveCallback(IAsyncResult ar)
            {
                ReceiveStateObject state = (ReceiveStateObject)ar.AsyncState;
                Socket socket = state.socket;
                SocketError socketError;

                Guid eventType = Guid.Empty;
                string originatingRouterName = string.Empty;
                string inRouterName = string.Empty;

                int bytesRead = 0;
                int bytesProcessed = 0;
                int remainingLength = 0;

                try
                {
                    if (socket.Connected == true)
                    {
                        try
                        {
                            bytesRead = socket.EndReceive(ar, out socketError);

                            if (socketError != SocketError.Success)
                            {
                                //EventLog.WriteEntry("WspEventRouter", "Receive failed with bad return code: " + socketError.ToString(), EventLogEntryType.Warning);

                                state.receiveDone.Set();
                                return;
                            }
                            else
                            {
                                if (bytesRead == 0)
                                {
                                    if (socket.Connected == false)
                                    {
                                        state.receiveDone.Set();
                                        return;
                                    }

                                    socket.Blocking = false;

                                    int bytes = socket.Receive(new byte[1], 0, 1, SocketFlags.Peek, out socketError);

                                    socket.Blocking = true;

                                    if (socketError == SocketError.WouldBlock)
                                    {
                                        // connection is still alive
                                    }
                                    else
                                    {
                                        if (socketError != SocketError.Success)
                                        {
                                            state.receiveDone.Set();
                                            return;
                                        }
                                        else
                                        {
                                            if (bytes == 0)
                                            {
                                                state.receiveDone.Set();
                                                return;
                                            }
                                            else
                                            {
                                                // have actual serializedEvent from the server ready to be received
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);

                            state.receiveDone.Set();
                            return;
                        }
                    }
                    else
                    {
                        state.receiveDone.Set();
                        return;
                    }

                    while (bytesProcessed < bytesRead)
                    {
                        if (state.currentReceiveLength == 0)
                        {
                            bytesProcessed += GetReceiveLength(state, bytesProcessed, bytesRead);
                            continue;
                        }

                        remainingLength = state.currentReceiveLength - state.currentProcessedLength;

                        if (bytesRead - bytesProcessed >= remainingLength)
                        {
                            byte[] inBuffer = new byte[remainingLength];

                            Buffer.BlockCopy(state.buffer, bytesProcessed, inBuffer, 0, remainingLength);

                            state.buffers.Add(inBuffer);

                            state.totalBytesRead = state.totalBytesRead + remainingLength;

                            bytesProcessed = bytesProcessed + remainingLength;
                            state.currentProcessedLength = state.currentProcessedLength + remainingLength;
                        }
                        else
                        {
                            byte[] inBuffer = new byte[bytesRead - bytesProcessed];

                            Buffer.BlockCopy(state.buffer, bytesProcessed, inBuffer, 0, bytesRead - bytesProcessed);

                            state.buffers.Add(inBuffer);

                            state.totalBytesRead = state.totalBytesRead + bytesRead - bytesProcessed;

                            state.currentProcessedLength = state.currentProcessedLength + bytesRead - bytesProcessed;
                            bytesProcessed = bytesRead;
                        }

                        if (state.currentProcessedLength == state.currentReceiveLength)
                        {
                            byte[] serializedEvent = ConcatArrayList(state.buffers);

                            Event.GetHeader(serializedEvent, out originatingRouterName, out inRouterName, out eventType);

                            if (String.Compare(originatingRouterName, Router.localRouterName, true) != 0)
                            {
                                Router.channelDictionary[originatingRouterName] = state.clientRouterName;

                                QueueElement element = new QueueElement();

                                element.SerializedEvent = serializedEvent;
                                element.SerializedLength = state.totalBytesRead;
                                element.EventType = eventType;
                                element.OriginatingRouterName = originatingRouterName;
                                element.InRouterName = state.clientRouterName;

                                rePublisherQueue.Enqueue(element);
                            }

                            state.Reset();
                        }
                    }

                    if (bytesRead > 0 || socket.Connected == true)
                    {
                        socket.BeginReceive(state.buffer, 0, averageEventSize, 0,
                            new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {
                        state.receiveDone.Set();
                    }
                }
                catch
                {
                    state.receiveDone.Set();
                }
            }

            private static int GetReceiveLength(ReceiveStateObject state, int bytesProcessed, int bytesRead)
            {
                if (bytesRead - bytesProcessed < 4 - state.currentReceiveLengthBytesRead)
                {
                    Buffer.BlockCopy(state.buffer, bytesProcessed,
                        state.currentReceiveLengthBytes, state.currentReceiveLengthBytesRead, bytesRead - bytesProcessed);

                    state.currentReceiveLengthBytesRead = state.currentReceiveLengthBytesRead + bytesRead - bytesProcessed;

                    return bytesRead - bytesProcessed;
                }

                int returnLength = 4 - state.currentReceiveLengthBytesRead;

                Buffer.BlockCopy(state.buffer, bytesProcessed,
                    state.currentReceiveLengthBytes, state.currentReceiveLengthBytesRead, 4 - state.currentReceiveLengthBytesRead);

                state.currentReceiveLengthBytesRead = 4;

                state.currentReceiveLength = BitConverter.ToInt32(state.currentReceiveLengthBytes, 0);

                return returnLength;
            }

            private void OutHandler()
            {
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                ArraySegment<byte> bufferLengthOut;
                ArraySegment<byte> bufferDataOut;
                List<ArraySegment<byte>> buffersOut;
                SocketError socketError;

                while (true)
                {
                    try
                    {
                        if (socket.Connected == false)
                        {
                            return;
                        }

                        element = threadQueue.Dequeue();

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
                        buffersOut = new List<ArraySegment<byte>>(2);

                        bufferLengthOut = new ArraySegment<byte>(BitConverter.GetBytes(element.SerializedEvent.Length));
                        buffersOut.Add(bufferLengthOut);

                        bufferDataOut = new ArraySegment<byte>(element.SerializedEvent);
                        buffersOut.Add(bufferDataOut);

                        try
                        {
                            socket.Send(buffersOut, SocketFlags.None, out socketError);
                        }
                        catch (ObjectDisposedException)
                        {
                            try
                            {
                                threadQueue.Enqueue(element);
                            }
                            catch (Exception ee)
                            {
                                EventLog.WriteEntry("WspEventRouter", ee.ToString(), EventLogEntryType.Warning);
                            }

                            return;
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                threadQueue.Enqueue(element);
                            }
                            catch (Exception ee)
                            {
                                EventLog.WriteEntry("WspEventRouter", ee.ToString(), EventLogEntryType.Warning);
                            }

                            EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);

                            return;
                        }

                        if (socketError != SocketError.Success)
                        {
                            try
                            {
                                threadQueue.Enqueue(element);
                            }
                            catch (Exception ee)
                            {
                                EventLog.WriteEntry("WspEventRouter", ee.ToString(), EventLogEntryType.Warning);
                            }

                            Communicator.CloseSocket(socket, clientRouterName);
                            socket = null;

                            //EventLog.WriteEntry("WspEventRouter", "Send failed with bad return code: " + socketError.ToString(), EventLogEntryType.Warning);

                            return;
                        }
                    }
                }
            }
        }

        internal class ReceiveMonitor : ServiceThread
        {
            ReceiveStateObject state;
            int timeout;

            public ReceiveMonitor(ReceiveStateObject state, int timeout)
            {
                this.state = state;
                this.timeout = timeout;
            }

            public override void Start()
            {
                while (true)
                {
                    Thread.Sleep(timeout);

                    if (state.blocking == false)
                    {
                        return;
                    }

                    try
                    {
                        if (state.socket.Connected == true)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        // If an exception occurs, we want to kill the current connection
                    }

                    state.receiveDone.Set();

                    return;
                }
            }
        }

        internal class DistributeHandler : ServiceThread
        {
            public DistributeHandler()
            {
            }

            public override void Start()
            {
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;

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
                    while (true)
                    {
                        try
                        {
                            element = forwarderQueue.Dequeue();

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
                        catch (System.TimeoutException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (elementRetrieved == true)
                        {
                            if (channelDictionary.TryGetValue(element.OriginatingRouterName, out element.InRouterName) == false)
                            {
                                element.InRouterName = string.Empty;
                            }

                            if (element.EventType == Event.SubscriptionEvent || element.EventType == mgmtGroup || element.EventType == cmdGroup)
                            {
                                lock (Communicator.threadQueuesLock)
                                {
                                    foreach (string routerName in Communicator.threadQueues.Keys)
                                    {
                                        if (string.Compare(element.InRouterName, routerName, true) != 0)
                                        {
                                            Communicator.threadQueues[routerName].Enqueue(element);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SubscriptionDetail subscriptionDetail;
                                SynchronizationQueue<QueueElement> queue;

                                if (SubscriptionMgr.subscriptions.TryGetValue(element.EventType, out subscriptionDetail) == true)
                                {
                                    lock (subscriptionDetail.SubscriptionDetailLock)
                                    {
                                        foreach (string routerName in subscriptionDetail.Routes.Keys)
                                        {
                                            if (string.Compare(routerName, Router.LocalRouterName, true) != 0 &&
                                                string.Compare(routerName, element.InRouterName, true) != 0)
                                            {
                                                if (Communicator.threadQueues.TryGetValue(routerName, out queue) == true)
                                                {
                                                    queue.Enqueue(element);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);

                    return;
                }
            }
        }
    }
}
