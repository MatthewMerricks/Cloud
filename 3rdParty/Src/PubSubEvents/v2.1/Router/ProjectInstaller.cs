using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Resources;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Microsoft.WebSolutionsPlatform.Event
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Override 'Uninstall' method of Installer class.
        /// </summary>
        /// <param name="mySavedState"></param>
        public override void Uninstall(IDictionary mySavedState)
        {
            if (mySavedState == null)
            {
                Console.WriteLine("Uninstallation Error !");
            }
            else
            {
                PerformanceCounterSetup pcSetup = new PerformanceCounterSetup();
                pcSetup.Init("uninstall");

                base.Uninstall(mySavedState);
            }
        }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            string initComplete = Context.Parameters["initComplete"];

            if (initComplete == null || initComplete == string.Empty)
            {
                PerformanceCounterSetup pcSetup = new PerformanceCounterSetup();
                pcSetup.Init("install");

                WriteOutConfig();

                Context.Parameters["initComplete"] = "true";
            }
        }

        private void WriteOutConfig()
        {
            string targetDir = Context.Parameters["TARGETDIR"];

            string wspRole = Context.Parameters["ROLE"];
            string wspBootstrapUrl = Context.Parameters["BOOTSTRAPURL"];
            string wspMgmtGroup = Context.Parameters["MGMTGROUP"];
            string wspParent = Context.Parameters["WSPPARENT"];
            string wspInternetFacing = Context.Parameters["WSPINTERNETFACING"];
            string wspEventQueueSize = Context.Parameters["WSPQUEUESIZE"];
            string wspParentUi = Context.Parameters["WSPPARENTUI"];
            string wspInternetFacingUi = Context.Parameters["WSPINTERNETFACINGUI"];
            string wspEventQueueSizeUi = Context.Parameters["WSPQUEUESIZEUI"];

            string configPath = targetDir + "WspEventRouter.exe.config";

            if (wspParentUi != string.Empty)
            {
                wspParent = wspParentUi;
            }

            if (wspEventQueueSize == string.Empty)
            {
                wspEventQueueSize = wspEventQueueSizeUi;
            }

            if (wspInternetFacing == string.Empty)
            {
                wspInternetFacing = wspInternetFacingUi;
            }

            using (StreamWriter sw = new StreamWriter(configPath))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                sw.WriteLine("<configuration>");
                sw.WriteLine("	<configSections>");
                sw.WriteLine("		<section name=\"eventRouterSettings\" type=\"RouterSettings\"/>");
                sw.WriteLine("		<section name=\"eventPersistSettings\" type=\"PersistSettings\"/>");
                sw.WriteLine("	</configSections>");
                sw.WriteLine("");
                sw.WriteLine("	<eventRouterSettings>");
                sw.WriteLine("    <!-- If you choose autoConfig to be true then you need to first install the origin router and have the DNS configured with -->");
                sw.WriteLine("    <!-- data center routers if your topology requires it. With autoConfig turned on, all config files will be kept identical -->");
                sw.WriteLine("    <!-- to the origin's config file. When it is changed, all others will automatically be changed. -->");
                sw.WriteLine("    <!-- The bootstrapUrl must resolve to the origin router. It will initially be called by a client to retrieve its config file. -->");
                sw.WriteLine("    <!-- The mgmtGroup is the eventId which will be used to communicate all config info while servers are running. -->");
                sw.WriteLine("    <!-- Role defines what role a given server takes when automatically establishing the topology. -->");
                sw.WriteLine("    <!-- The values for role are: -->");
                sw.WriteLine("    <!--   origin   -->");
                sw.WriteLine("    <!--   primary   -->");
                sw.WriteLine("    <!--   secondary   -->");
                sw.WriteLine("    <!--   client   -->");
                sw.WriteLine("    <!-- <configInfo role=\"origin\" autoConfig=\"false\" bootstrapUrl=\"http://WspOrigin/GetConfig\" mgmtGroup=\"2B2B78DB-8AE7-4a16-AB6C-850F54A82D54\"/> -->");
                sw.WriteLine("");

                if (wspRole != string.Empty)
                {
                    if (string.Compare(wspRole, "origin", true) == 0)
                    {
                        sw.WriteLine("    <configInfo role=\"" + wspRole + "\" autoConfig=\"true\" bootstrapUrl=\"" + wspBootstrapUrl + "\" mgmtGroup=\"" + wspMgmtGroup + "\"/>");
                    }
                    else
                    {
                        sw.WriteLine("    <configInfo role=\"" + wspRole + "\" autoConfig=\"true\" bootstrapUrl=\"" + wspBootstrapUrl + "\"/>");
                    }

                    sw.WriteLine("");
                    sw.WriteLine("	</eventRouterSettings>");
                }
                else
                {
                    sw.WriteLine("    <configInfo role=\"client\" autoConfig=\"false\" bootstrapUrl=\"\"/>");
                    sw.WriteLine("");
                    sw.WriteLine("    <clientRoleInfo>");
                    sw.WriteLine("");
                    sw.WriteLine("      <subscriptionManagement refreshIncrement=\"3\"  expirationIncrement=\"10\"/>");
                    sw.WriteLine("");

                    if (wspEventQueueSize == null || wspEventQueueSize == string.Empty)
                    {
                        sw.WriteLine("		<localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"102400000\" averageEventSize=\"10240\"/>");
                    }
                    else
                    {
                        sw.WriteLine("		<localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"" + wspEventQueueSize + "\" averageEventSize=\"10240\"/>");
                    }

                    sw.WriteLine("");
                    sw.WriteLine("		<!-- These settings control what should happen to an output queue when communications is lost to a parent or child.-->");
                    sw.WriteLine("		<!-- maxQueueSize is in bytes and maxTimeout is in seconds.-->");
                    sw.WriteLine("		<!-- When the maxQueueSize is reached or the maxTimeout is reached for a communication that has been lost, the queue is deleted.-->");
                    sw.WriteLine("		<outputCommunicationQueues maxQueueSize=\"200000000\" maxTimeout=\"600\"/>");
                    sw.WriteLine("");
                    sw.WriteLine("		<!-- nic can be an alias which specifies a specific IP address or an IP address. -->");
                    sw.WriteLine("		<!-- port can be 0 if you don't want to have the router open a listening port to be a parent to other routers. -->");

                    if (wspInternetFacing == null || wspInternetFacing == string.Empty)
                    {
                        sw.WriteLine("		<thisRouter nic=\"\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" />");
                    }
                    else
                    {
                        sw.WriteLine("		<thisRouter nic=\"\" port=\"0\" bufferSize=\"1024000\" timeout=\"30000\" />");
                    }

                    sw.WriteLine("");

                    if (wspParent == null || wspParent == string.Empty)
                    {
                        sw.WriteLine("		<!-- <parentRouter name=\"ParentMachineName\" numConnections=\"2\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" />  -->");
                    }
                    else
                    {
                        sw.WriteLine("		<parentRouter name=\"" + wspParent + "\" numConnections=\"2\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" />");
                    }

                    sw.WriteLine("");
                    sw.WriteLine("    </clientRoleInfo>");
                    sw.WriteLine("");
                    sw.WriteLine("	</eventRouterSettings>");
                    sw.WriteLine("");
                    sw.WriteLine("	<eventPersistSettings>");
                    sw.WriteLine("");
                    sw.WriteLine("	    <!-- type specifies the EventType to be persisted.-->");
                    sw.WriteLine("		<!-- localOnly is a boolean which specifies whether only events published on this machine are persisted or if events from the entire network are persisted.-->");
                    sw.WriteLine("		<!-- maxFileSize specifies the maximum size in bytes that the persisted file should be before it is copied.-->");
                    sw.WriteLine("		<!-- maxCopyInterval specifies in seconds the longest time interval before the persisted file is copied.-->");
                    sw.WriteLine("		<!-- fieldTerminator specifies the character used between fields.-->");
                    sw.WriteLine("		<!-- rowTerminator specifies the character used at the end of each row written.-->");
                    sw.WriteLine("		<!-- tempFileDirectory is the local directory used for writing out the persisted event serializedEvent.-->");
                    sw.WriteLine("		<!-- copyToFileDirectory is the final destination of the persisted serializedEvent file. It can be local or remote using a UNC.-->");
                    sw.WriteLine("");
                    sw.WriteLine("		<!-- <event type=\"78422526-7B21-4559-8B9A-BC551B46AE34\" localOnly=\"true\" maxFileSize=\"2000000000\" maxCopyInterval=\"60\" fieldTerminator=\",\" rowTerminator=\"\\n\" tempFileDirectory=\"c:\\temp\\WebEvents\\\" copyToFileDirectory=\"c:\\temp\\WebEvents\\log\\\" /> -->");
                    sw.WriteLine("");
                    sw.WriteLine("	</eventPersistSettings>");
                }

                sw.WriteLine("</configuration>");
            }
        }
    }

    /// <summary>
    /// This exposes the class to manually install and remove the performance counter categories which need to be 
    /// done if the msi is not used to install the application
    /// </summary>
    public class PerformanceCounterSetup
    {
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
        /// Installs/Uninstalls the perforance counters used by the application
        /// </summary>
        /// <param name="arg">Argument should be "uninstall" to remove the performance counters</param>
        public void Init(string arg)
        {
            CounterCreationDataCollection CCDC;

            categoryName = "WspEventRouter";
            communicationCategoryName = "WspEventRouterCommunication";
            categoryHelp = "WspEventRouter counters showing internal performance of the router.";
            communicationCategoryHelp = "WspEventRouter counters showing communication queues to other machines";
            subscriptionQueueSizeName = "SubscriptionQueueSize";
            rePublisherQueueSizeName = "RePublisherQueueSize";
            persisterQueueSizeName = "PersisterQueueSize";
            forwarderQueueSizeName = "ForwarderQueueSize";
            subscriptionEntriesName = "SubscriptionEntries";
            eventsProcessedName = "EventsProcessed";
            eventsProcessedBytesName = "EventsProcessedBytes";
            baseInstance = "WspEventRouter";

            if (PerformanceCounterCategory.Exists(categoryName) == true)
            {
                PerformanceCounterCategory.Delete(categoryName);
            }

            if (EventLog.SourceExists("WspEventRouter") == true)
            {
                EventLog.DeleteEventSource("WspEventRouter");
            }

            if (arg == "uninstall")
            {
                return;
            }

            EventLog.CreateEventSource("WspEventRouter", "System");

            CCDC = new CounterCreationDataCollection();

            CounterCreationData subscriptionQueueCounter = new CounterCreationData();
            subscriptionQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            subscriptionQueueCounter.CounterName = subscriptionQueueSizeName;
            CCDC.Add(subscriptionQueueCounter);

            CounterCreationData rePublisherQueueCounter = new CounterCreationData();
            rePublisherQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            rePublisherQueueCounter.CounterName = rePublisherQueueSizeName;
            CCDC.Add(rePublisherQueueCounter);

            CounterCreationData persisterQueueCounter = new CounterCreationData();
            persisterQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            persisterQueueCounter.CounterName = persisterQueueSizeName;
            CCDC.Add(persisterQueueCounter);

            CounterCreationData subscriptionEntriesCounter = new CounterCreationData();
            subscriptionEntriesCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            subscriptionEntriesCounter.CounterName = subscriptionEntriesName;
            CCDC.Add(subscriptionEntriesCounter);

            CounterCreationData eventsProcessedCounter = new CounterCreationData();
            eventsProcessedCounter.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
            eventsProcessedCounter.CounterName = eventsProcessedName;
            CCDC.Add(eventsProcessedCounter);

            CounterCreationData eventsProcessedBytesCounter = new CounterCreationData();
            eventsProcessedBytesCounter.CounterType = PerformanceCounterType.RateOfCountsPerSecond64;
            eventsProcessedBytesCounter.CounterName = eventsProcessedBytesName;
            CCDC.Add(eventsProcessedBytesCounter);

            PerformanceCounterCategory.Create(categoryName, categoryHelp,
                PerformanceCounterCategoryType.SingleInstance, CCDC);

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

        }
    }
}