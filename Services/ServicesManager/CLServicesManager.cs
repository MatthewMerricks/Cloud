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
using win_client.Services.UiActivity;
using win_client.Services.FileSystemMonitoring;
using win_client.Services.ContextMenu;
using CloudApiPrivate.Common;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.FileMonitor.SyncImplementation;
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
        private static object _coreServicesLocker = new object();

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

            lock (_coreServicesLocker)
            {
                try
                {
                    if (!_coreServicesStarted)
                    {
                        _trace.writeToLog(1, "CLServicesManager: StartCoreServices: Starting.");

                        // Initialize the growl service
                        Growl.Growl.StartGrowlService();

                        // Update the shell integration shortcuts
                        CLShortcuts.UpdateAllShortcuts(Settings.Instance.CloudFolderPath);

                        // Allows icon overlays to start receiving calls to SetOrRemoveBadge,
                        // before the initial list is passed (via InitializeOrReplace)
                        //TODO: Handle any CLErrors returned from these services.
                        CLContextMenuService.Instance.BeginContextMenuServices();
                        CLUIActivityService.Instance.BeginUIActivityService();
                        CLNetworkMonitorService.Instance.BeginNetworkMonitoring();
                        CLFSMonitoringService.Instance.BeginFileSystemMonitoring();
                        if (CLNetworkMonitorService.Instance.CloudReach)
                        {
                            // Outdated, Sync process replaced
                            // -David
                            //CLSyncService.Instance.BeginSyncServices();
                        }

                        //TODO: Enable to hook all user processes for the start of a drag/drop operation
                        //DragDropServer.DragDropServer.Instance.StartDragDropServer();

                        _coreServicesStarted = true;            // at least we went through all of the startup functions.
                    }
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                    _trace.writeToLog(1, "CLServicesManager: StartCoreServices: ERROR: Exception: Msg: <{0}>.", ex.Message);
                }
            }
        }

        public void StopCoreServices()
        {
            lock (_coreServicesLocker)
            {
                try
                {
                    if (_coreServicesStarted)
                    {
                        _trace.writeToLog(1, "CLServicesManager: StopCoreServices: Stop core services.");
                        CLUIActivityService.Instance.EndUIActivityService();
                        CLContextMenuService.Instance.EndContextMenuServices();

                        //TODO: Enable to hook all user processes for the start of a drag/drop operation
                        //DragDropServer.DragDropServer.Instance.StopDragDropServer();

                        CLNetworkMonitorService.Instance.EndNetworkMonitoring();
                        CLFSMonitoringService.Instance.EndFileSystemMonitoring();
                        CLSyncEngine.ShutdownSchedulers();
                        
                        // Stop the growl service
                        Growl.Growl.ShutdownGrowlService();

                        // Outdated, Sync process replaced
                        // -David
                        //CLSyncService.Instance.StopSyncServices();
                    }
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                    _trace.writeToLog(1, "CLServicesManager: StopCoreServices: ERROR: Exception: Msg: <{0}>.", ex.Message);
                }
                finally
                {
                    _coreServicesStarted = false;
                }
            }
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
