//
//  CLServicesManager.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using CloudApi;
using CloudApi.Support;
using win_client.Common;
using GalaSoft.MvvmLight.Messaging;
using win_client.Services.Badging;
using win_client.Services.UiActivity;
using win_client.Services.Indexing;
using win_client.Services.Notification;

namespace win_client.Services.ServicesManager
{
    /// <summary>
    /// Singleton class to represent the services manager.
    /// </summary>
    public sealed class CLServicesManager
    {
        private static CLServicesManager _instance = null;
        private static object _instanceLocker = new object();
        private static CLSptTrace _trace;

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
            _trace = CLSptTrace.Instance;
        }
        
        /// <summary>
        /// Install any shell integration support, and start all of the services.
        /// </summary>
        public void StartCoreServices()
        {
            CLApiError error;
            _trace.writeToLog(9, "CLServicesManager: startCoreServices: Entry.");
            bool success = RunShellIntegrationServicesAndSetError(out error);
            if (!success) {
                _trace.writeToLog(1, "CLServicesManager: startCoreServices: Failed to run the shell integration support.");
            }

            CLBadgingService.Instance.BeginBadgingServices();
            CLUIActivityService.Instance.BeginUIActivityService();
            CLIndexingService.Instance.StartIndexingService();
            CLNetworkMonitorService.Instance.BeginNetworkMonitoring();
            CLFSMonitoringService.Instance.BeginFileSystemMonitoring();
            CLCFMonitoringService.Instance.BeginCloudFolderMonitoring();
            CLSyncService.Instance.BeginSyncServices();
            CLNotificationService.Instance.BeginNotificationService();
        }

        public void StopCoreServices()
        {
            CLUIActivityService.Instance.EndUIActivityService();
            CLBadgingService.Instance.EndBadgingServices();
            CLNotificationService.Instance.DisconnectPushNotificationServer();
            CLNetworkMonitorService.Instance.EndNetworkMonitoring();
            CLFSMonitoringService.Instance.EndFileSystemMonitoring();
            CLCFMonitoringService.Instance.EndCloudFolderMonitoring();
            CLSyncService.Instance.StopSyncServices();
        }

        public void StartSyncServices()
        {
            CLFSMonitoringService.Instance.BeginFileSystemMonitoring();
            CLSyncService.Instance.BeginSyncServices();
        }

        public void StopSyncServices()
        {
            CLFSMonitoringService.Instance.EndFileSystemMonitoring();
            CLSyncService.Instance.StopSyncServices();
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

        public bool RunShellIntegrationServicesAndSetError(out CLApiError error)
        {
            _trace.writeToLog(9, "CLServicesManager: runShellIntegrationServicesAndSetError: Entry.");

            error = null;
            //TODO: Run any shell installation programs here.

            return true;
        }

    }
}
