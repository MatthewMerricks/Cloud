//
//  CLFSMonitoringService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using FileMonitor;
using win_client.Services.Sync;


namespace win_client.Services.FileSystemMonitoring
{
    public sealed class CLFSMonitoringService
    {
        static readonly CLFSMonitoringService _instance = new CLFSMonitoringService();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace = null;
        private MonitorAgent _agent = null;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLFSMonitoringService Instance
        {
            get
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;

                    // Initialize at first Instance access here
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLFSMonitoringService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {
            CLError error = MonitorAgent.CreateNewAndInitialize(Settings.Instance.CloudFolderPath, out _agent, CLSyncService.Instance.SyncFromFileSystemMonitorWithGroupedUserEventsCallback);
            if (error != null)
            {
                _trace.writeToLog(1, "Error initializing the file system monitor. Description: {0}. Code: {1}.", error.errorDescription, error.errorCode);
            }
            else
            {
                MonitorStatus status;
                error = _agent.Start(out status);
                if (error != null)
                {
                    _trace.writeToLog(1, "Error starting the file system monitor. Description: {0}. Code: {1}.", error.errorDescription, error.errorCode);
                }
            }
        }

        /// <summary>
        /// Stop the file system monitoring service.
        /// </summary>
        public void EndFileSystemMonitoring()
        {
            if (_agent != null)
            {
                _agent.Dispose();
                _agent = null;
            }
        }

        public void CheckWithFSMForEvents()
        {
#if TRASH
            if ([self.fileSystemEvents count] > 0) 
            {
                dispatch_async(get_cloud_FSM_queue(), ^{
                    [self postEventsWithEventId:self.lastKnownEventId];
                });
                
            }
            else
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    [self fireSimulatedPushNotification];
                });
            }
#endif  // end TRASH
        }
    }
}
