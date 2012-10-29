using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Resources;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;

using Microsoft.WebSolutionsPlatform.Common;

[assembly: CLSCompliant(true)]

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Enum for different types of worker threads
    /// </summary>
    public enum WorkerThreadType : int
    {
        /// <summary>
        /// Not defined
        /// </summary>
        None = 0,
        /// <summary>
        /// This is the thread listening for new events to the queue
        /// </summary>
        ListenerThread = 1,
        /// <summary>
        /// This is the thread that communicates with the parent and children machines in the mesh
        /// </summary>
        CommunicatorThread = 2,
        /// <summary>
        /// This is the thread that takes incoming events from a parent/child machine and 
        /// publishes them to this machine.
        /// </summary>
        RePublisherThread = 3,
        /// <summary>
        /// This is the thread that manages the subscription routing table
        /// </summary>
        SubscriptionMgrThread = 4,
        /// <summary>
        /// This is the thread that persists events to the file system
        /// </summary>
        PersisterThread = 5,
        /// <summary>
        /// This is the thread that monitors the health of the other threads and restarts threads as 
        /// needed
        /// </summary>
        ManagerThread = 6
    }

    internal enum Role : int
    {
        Origin = 1,
        Primary = 2,
        Secondary = 3,
        Client = 4
    }

    /// <summary>
    /// Struct containing two dictionary entries
    /// </summary>
    public struct DoubleDictionary<TDictionary1, TDictionary2>
    {
        private Dictionary<TDictionary1, DateTime> dictionary1;
        /// <summary>
        /// First dictionary of structure.
        /// </summary>
        public Dictionary<TDictionary1, DateTime> Dictionary1
        {
            get
            {
                return dictionary1;
            }

            set
            {
                dictionary1 = value;
            }
        }

        private Dictionary<TDictionary2, DateTime> dictionary2;
        /// <summary>
        /// First dictionary of structure.
        /// </summary>
        public Dictionary<TDictionary2, DateTime> Dictionary2
        {
            get
            {
                return dictionary2;
            }

            set
            {
                dictionary2 = value;
            }
        }
    }

    internal struct QueueElement
    {
        internal Guid EventType;
        internal Event Event;
        internal byte[] SerializedEvent;
        internal int SerializedLength;
        internal string OriginatingRouterName;
        internal string InRouterName;
    }

    /// <summary>
    /// Abstract class for worker threads
    /// </summary>
    public abstract class ServiceThread
    {
        /// <summary>
        /// The Start method is used to start the thread.
        /// </summary>
        public abstract void Start();
    }

    /// <summary>
    /// Main class for the Event Router
    /// </summary>
    public partial class Router : ServiceBase
    {
        internal static object configFileLock = new object();
        internal static bool autoConfig = false;
        internal static string bootstrapUrl = string.Empty;
        internal static Guid mgmtGroup = Guid.Empty;
        internal static Guid cmdGroup = Guid.Empty;
        internal static string role = @"client";

        internal static UInt32 eventQueueSize;
        internal static Int32 averageEventSize;

        internal static string eventQueueName;

        internal static SharedQueue eventQueue;

        internal static uint subscriptionRefreshIncrement = 3; // in minutes
        internal static uint subscriptionExpirationIncrement = 10; // in minutes

        internal static string thisNic;
        internal static int thisPort;
        internal static int thisBufferSize;
        internal static int thisTimeout; //Timeout in milliseconds (1000 = 1 second)

        internal static int thisOutQueueMaxSize = 102400000; // in bytes
        internal static int thisOutQueueMaxTimeout = 600; // in seconds

        private static string localRouterName = string.Empty;
        internal static string LocalRouterName
        {
            get
            {
                if (localRouterName.Length == 0)
                {
                    localRouterName = Dns.GetHostName();
                }

                return localRouterName;
            }
        }

        private static byte[] routerNameEncodedPriv;
        internal static byte[] routerNameEncoded
        {
            get
            {
                if (routerNameEncodedPriv == null)
                {
                    UnicodeEncoding uniEncoding = new UnicodeEncoding();
                    routerNameEncodedPriv = uniEncoding.GetBytes(Dns.GetHostName());
                }

                return routerNameEncodedPriv;
            }
        }

        internal static Route parentRoute;

        internal static Dictionary<Type, Thread> workerThreads;

        internal static Listener listener;
        internal static RePublisher rePublisher;
        internal static SubscriptionMgr subscriptionMgr;
        internal static Persister persister;
        internal static Communicator communicator;
        internal static Configurator configurator;
        internal static CommandProcessor commandProcessor;
        internal static Manager manager;

        internal static SynchronizationQueue<QueueElement> subscriptionMgrQueue;
        internal static SynchronizationQueue<QueueElement> rePublisherQueue;
        internal static SynchronizationQueue<QueueElement> persisterQueue;
        internal static SynchronizationQueue<QueueElement> forwarderQueue;
        internal static SynchronizationQueue<QueueElement> mgmtQueue;
        internal static SynchronizationQueue<QueueElement> cmdQueue;

        internal static Dictionary<string, string> channelDictionary; //RouterName, OutRouterName (channel)

        internal static string categoryName;
        internal static string communicationCategoryName;
        internal static string categoryHelp;
        internal static string communicationCategoryHelp;
        internal static string subscriptionQueueSizeName;
        internal static string rePublisherQueueSizeName;
        internal static string persisterQueueSizeName;
        internal static string forwarderQueueSizeName;
        internal static string subscriptionEntriesName;
        internal static string eventsProcessedName;
        internal static string eventsProcessedBytesName;
        internal static string baseInstance;

        internal static PerformanceCounter subscriptionQueueSize;
        internal static PerformanceCounter rePublisherQueueSize;
        internal static PerformanceCounter persisterQueueSize;
        internal static PerformanceCounter forwarderQueueSize;
        internal static PerformanceCounter subscriptionEntries;
        internal static PerformanceCounter eventsProcessed;
        internal static PerformanceCounter eventsProcessedBytes;

        /// <summary>
        /// Default constructor for the Router class
        /// </summary>
        public Router()
        {
            CounterCreationDataCollection CCDC;

            ResourceManager rm = new ResourceManager("Router.WspEventRouter", Assembly.GetExecutingAssembly());

            categoryName = rm.GetString("CategoryName");
            communicationCategoryName = rm.GetString("CommunicationCategoryName");
            categoryHelp = rm.GetString("CategoryHelp");
            communicationCategoryHelp = rm.GetString("CommunicationCategoryHelp");
            subscriptionQueueSizeName = rm.GetString("SubscriptionQueueSizeName");
            rePublisherQueueSizeName = rm.GetString("RePublisherQueueSizeName");
            persisterQueueSizeName = rm.GetString("PersisterQueueSizeName");
            forwarderQueueSizeName = rm.GetString("ForwarderQueueSizeName");
            subscriptionEntriesName = rm.GetString("SubscriptionEntriesName");
            eventsProcessedName = rm.GetString("EventsProcessedName");
            eventsProcessedBytesName = rm.GetString("EventsProcessedBytesName");
            baseInstance = rm.GetString("BaseInstance");

            subscriptionQueueSize = new PerformanceCounter(categoryName, subscriptionQueueSizeName, string.Empty, false);
            rePublisherQueueSize = new PerformanceCounter(categoryName, rePublisherQueueSizeName, string.Empty, false);
            persisterQueueSize = new PerformanceCounter(categoryName, persisterQueueSizeName, string.Empty, false);
            subscriptionEntries = new PerformanceCounter(categoryName, subscriptionEntriesName, string.Empty, false);
            eventsProcessed = new PerformanceCounter(categoryName, eventsProcessedName, string.Empty, false);
            eventsProcessedBytes = new PerformanceCounter(categoryName, eventsProcessedBytesName, string.Empty, false);

            if (PerformanceCounterCategory.Exists(communicationCategoryName) == false)
            {
                CCDC = new CounterCreationDataCollection();

                CounterCreationData forwarderQueueCounter = new CounterCreationData();
                forwarderQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
                forwarderQueueCounter.CounterName = forwarderQueueSizeName;
                CCDC.Add(forwarderQueueCounter);

                PerformanceCounterCategory.Create(communicationCategoryName, communicationCategoryHelp,
                    PerformanceCounterCategoryType.MultiInstance, CCDC);
            }

            forwarderQueueSize = new PerformanceCounter(communicationCategoryName, forwarderQueueSizeName, baseInstance, false);

            subscriptionQueueSize.RawValue = 0;
            rePublisherQueueSize.RawValue = 0;
            persisterQueueSize.RawValue = 0;
            forwarderQueueSize.RawValue = 0;
            subscriptionEntries.RawValue = 0;
            eventsProcessed.RawValue = 0;
            eventsProcessedBytes.RawValue = 0;
            eventQueueSize = 10240;
            averageEventSize = 10240;

            eventQueueName = @"WspEventQueue";

            thisBufferSize = 1024000;
            thisTimeout = 10000;

            listener = new Listener();
            rePublisher = new RePublisher();
            subscriptionMgr = new SubscriptionMgr();
            persister = new Persister();
            communicator = new Communicator();
            configurator = new Configurator();
            commandProcessor = new CommandProcessor();
            manager = new Manager();

            subscriptionMgrQueue = new SynchronizationQueue<QueueElement>(2000, subscriptionQueueSize);
            rePublisherQueue = new SynchronizationQueue<QueueElement>(2000, rePublisherQueueSize);
            persisterQueue = new SynchronizationQueue<QueueElement>(2000, persisterQueueSize);
            forwarderQueue = new SynchronizationQueue<QueueElement>(2000, forwarderQueueSize);
            mgmtQueue = new SynchronizationQueue<QueueElement>(100);
            cmdQueue = new SynchronizationQueue<QueueElement>(100);

            workerThreads = new Dictionary<Type, Thread>();

            workerThreads.Add(listener.GetType(), null);
            workerThreads.Add(rePublisher.GetType(), null);
            workerThreads.Add(subscriptionMgr.GetType(), null);
            workerThreads.Add(persister.GetType(), null);
            workerThreads.Add(communicator.GetType(), null);
            workerThreads.Add(configurator.GetType(), null);
            workerThreads.Add(commandProcessor.GetType(), null);
            workerThreads.Add(manager.GetType(), null);

            channelDictionary = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            try
            {
                LoadConfiguration();
            }
            catch
            {
                // We'll load the config later
            }

            this.ServiceName = "WspEventRouter";
        }

        /// <summary>
        /// Implements the OnStart for the service
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            Thread workerThread = new Thread(new ThreadStart(manager.Start));

            workerThreads[manager.GetType()] = workerThread;

            workerThread.Start();
        }

        internal void Start()
        {
            Thread workerThread = new Thread(new ThreadStart(manager.Start));

            workerThreads[manager.GetType()] = workerThread;

            workerThread.Start();
        }

        /// <summary>
        /// Implements the OnStop for the service
        /// </summary>
        protected override void OnStop()
        {
            Thread[] threads = new Thread[workerThreads.Count];
            Thread managerThread = workerThreads[manager.GetType()];

            workerThreads.Values.CopyTo(threads, 0);

            if ((managerThread != null) && (managerThread.IsAlive))
            {
                managerThread.Abort();
            }

            foreach (Thread thread in threads)
            {
                if (thread != null && thread.IsAlive == true)
                {
                    thread.Abort();
                }
            }
        }

        /// <summary>
        /// Concatenates an ArrayList of byte[] and returns one byte[]
        /// </summary>
        /// <param name="arrayIn">ArrayList of byte[]</param>
        public static byte[] ConcatArrayList(ArrayList arrayIn)
        {
            int size = 0;
            int location = 0;

            if (arrayIn.Count == 1)
            {
                return (byte[])arrayIn[0];
            }

            for (int i = 0; i < arrayIn.Count; i++)
            {
                size = size + ((byte[])arrayIn[i]).Length;
            }

            byte[] arrayOut = new byte[size];

            for (int i = 0; i < arrayIn.Count; i++)
            {
                ((byte[])arrayIn[i]).CopyTo(arrayOut, location);

                location = location + ((byte[])arrayIn[i]).Length;
            }

            return arrayOut;
        }
    }

    internal class Route : IComparable<Route>
    {
        private string routerName;
        /// <summary>
        /// Name of the router
        /// </summary>
        public string RouterName
        {
            get
            {
                return routerName;
            }
        }

        private int numConnections;
        /// <summary>
        /// Number of connections to parent
        /// </summary>
        public int NumConnections
        {
            get
            {
                return numConnections;
            }
            internal set
            {
                numConnections = value;
            }
        }

        private int port;
        /// <summary>
        /// TCP port for route
        /// </summary>
        public int Port
        {
            get
            {
                return port;
            }
            internal set
            {
                port = value;
            }
        }

        private int bufferSize;
        /// <summary>
        /// Buffer size used for the TCP port for route
        /// </summary>
        public int BufferSize
        {
            get
            {
                return bufferSize;
            }
            internal set
            {
                bufferSize = value;
            }
        }

        private int timeout;
        /// <summary>
        /// Timeout used for TCP calls
        /// </summary>
        public int Timeout
        {
            get
            {
                return timeout;
            }
            internal set
            {
                timeout = value;
            }
        }

        private DateTime expirationTime;
        /// <summary>
        /// Expiration time for route
        /// </summary>
        public DateTime ExpirationTime
        {
            get
            {
                return expirationTime;
            }
            internal set
            {
                expirationTime = value;
            }
        }

        /// <summary>
        /// Used to create a Route used by RouteMgr
        /// </summary>
        /// <param name="routerName">Name of the router</param>
        /// <param name="numConnections">Number of socket connections to open to routerName</param>
        /// <param name="port">TCP port used by the router</param>
        /// <param name="bufferSize">Buffer size used for the TCP port for route</param>
        /// <param name="timeout">Timeout used for TCP calls</param>
        public Route(string routerName, int numConnections, int port, int bufferSize, int timeout)
        {
            this.routerName = routerName;
            this.numConnections = numConnections;
            this.port = port;
            this.bufferSize = bufferSize;
            this.timeout = timeout;
            this.expirationTime = DateTime.UtcNow.AddMinutes(5);
        }

        public int CompareTo(Route otherRoute)
        {
            return RouterName.CompareTo(otherRoute.RouterName);
        }
    }
}
