//
//  CLServicesManager.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using win_client.Common;
using GalaSoft.MvvmLight.Messaging;
using win_client.Services.Badging;
using win_client.Services.Sync;
using win_client.Services.UiActivity;
using win_client.Services.Indexing;
using win_client.Services.Notification;
using win_client.Services.FileSystemMonitoring;
#if TRASH
using win_client.DragDropServer;
#endif // TRASH

namespace win_client.Services.ServicesManager
{
    /// <summary>
    /// Singleton class to represent the services manager.
    /// </summary>
    public sealed class CLServicesManager
    {
        private static CLServicesManager _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private static bool _coreServicesStarted = false;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLServicesManager Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLServicesManager();

                        // Initialize at first Instance access here
                        CLAppMessages.Message_ReachabilityChangedNotification.Register(_instance, _instance.ReachabilityChanged);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLServicesManager()
        {
            // Initialize members, etc. here (at static initialization time).
        }
        
        /// <summary>
        /// Install any shell integration support, and start all of the services.
        /// </summary>
        public void StartCoreServices()
        {

            // Merged 7/16/12
            //// In order, I think... GP
    
            //// Start Agent Monitor
            //[[CLAgentService sharedService] beginAgentServices];  
    
            //// Start UI Activity Services
            //[[CLUIActivityService sharedService] beginUIActivityService];

            //// Starts Root Folder Monitoring Service
            //[[CLCFMonitoringService sharedService] beginCloudFolderMonitoring];
    
            //// Start monitoring network availability
            //[[CLNetworkMonitor sharedService] beginNetworkMonitoring];

            //// Start Sync services only if we have network connection,
            //// otherwise we will seat tight until we get internet connection.
            //if ([[[CLNetworkMonitor sharedService] cloudReach] currentReachabilityStatus] != NotReachable) {

            //    // Start Push Notification Service
            //    [[CLNotificationServices sharedService] connectPushNotificationServer];
            //}

            //// Start File System Monitoring Service
            //[[CLFSMonitoringService sharedService] beginFileSystemMonitoring];

            //if ([[[CLNetworkMonitor sharedService] cloudReach] currentReachabilityStatus] != NotReachable) {
            //    // Start Sync Services
            //    [[CLSyncService sharedService] beginSyncServices];
            //}

            if (!_coreServicesStarted)
            {
                _coreServicesStarted = true;

                // Allows icon overlays to start receiving calls to SetOrRemoveBadge,
                // before the initial list is passed (via InitializeOrReplace)
                CLBadgingService.Instance.BeginBadgingServices();
                CLUIActivityService.Instance.BeginUIActivityService(); 
                CLIndexingService.Instance.StartIndexingService();
                CLNetworkMonitorService.Instance.BeginNetworkMonitoring();
                CLFSMonitoringService.Instance.BeginFileSystemMonitoring();
                CLCFMonitoringService.Instance.BeginCloudFolderMonitoring();
                if (CLNetworkMonitorService.Instance.CloudReach)
                {
                    // Outdated, Sync process replaced
                    // -David
                    //CLSyncService.Instance.BeginSyncServices();
                }
                if (CLNetworkMonitorService.Instance.CloudReach)
                {
                    CLNotificationService.Instance.ConnectPushNotificationServer();
                }
                //TODO: Enable to hook all user processes for the start of a drag/drop operation
                //DragDropServer.DragDropServer.Instance.StartDragDropServer();
            }
        }

        public void StopCoreServices()
        {
            if (_coreServicesStarted)
            {
                _coreServicesStarted = false;
                CLUIActivityService.Instance.EndUIActivityService();
                CLBadgingService.Instance.EndBadgingServices();

                //TODO: Enable to hook all user processes for the start of a drag/drop operation
                //DragDropServer.DragDropServer.Instance.StopDragDropServer();

                CLNotificationService.Instance.DisconnectPushNotificationServer();
                CLNetworkMonitorService.Instance.EndNetworkMonitoring();
                CLFSMonitoringService.Instance.EndFileSystemMonitoring();
                CLCFMonitoringService.Instance.EndCloudFolderMonitoring();
                DelayProcessable<FileChange>.TerminateAllProcessing();
                global::Sync.Sync.Shutdown();

                // Outdated, Sync process replaced
                // -David
                //CLSyncService.Instance.StopSyncServices();
            }
        }
        public void StartSyncServices()
        {
            CLFSMonitoringService.Instance.BeginFileSystemMonitoring();
            // Outdated, Sync process replaced
            // -David
            //CLSyncService.Instance.BeginSyncServices();
        }

        public void StopSyncServices()
        {
            CLFSMonitoringService.Instance.EndFileSystemMonitoring();
            // Outdated, Sync process replaced
            // -David
            //CLSyncService.Instance.StopSyncServices();
        }

        /// <summary>
        /// Handle the Messsage_ReachabilityChangedNotification.  Do we have a connection to the server?
        /// </summary>
        private void ReachabilityChanged(DialogMessage msg)
        {
            //TODO: Test network reachability here.
#if TRASH
            static int hit = 0;
            Reachability cloudReach = note.Xobject();
            NSParameterAssert(cloudReach.IsKindOfClass([Reachability class]));
            NetworkStatus netStatus = cloudReach.CurrentReachabilityStatus();
            if (hit == 0) {
                hit = 1;
                if (netStatus != (int) NetworkStatus.NotReachable) {
                    (CLNotificationServices.SharedService()).ConnectPushNotificationServer();
                }

                return;
            }

            if (netStatus == (int) NetworkStatus.NotReachable) {
                (CLNotificationServices.SharedService()).DisconnectPushNotificationServer();
                this.StopSyncServices();
            }
            else {
                (CLNotificationServices.SharedService()).ConnectPushNotificationServer();
                this.StartSyncServices();
            }
#endif  // TRASH
        }
    }
}
