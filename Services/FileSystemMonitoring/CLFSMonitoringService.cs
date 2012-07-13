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
using SQLIndexer;


namespace win_client.Services.FileSystemMonitoring
{
    public sealed class CLFSMonitoringService
    {
        static readonly CLFSMonitoringService _instance = new CLFSMonitoringService();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace = null;
        private IndexingAgent _indexer = null;

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
        /// Property to return the FSM MonitorAgent.
        /// </summary>
        private MonitorAgent _agent = null;
        public MonitorAgent Agent
        {
            get { return _agent; }
            private set { _agent = value; }
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {
            CLError indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer);

            // Todo: handle index creation error

            CLError fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(Settings.Instance.CloudFolderPath,
                out _agent,
                CLSyncService.Instance.SyncFromFileSystemMonitorWithGroupedUserEventsCallback,
                _indexer.MergeEventIntoDatabase);

            // Todo: handle file monitor creation error

            MonitorStatus returnStatus;
            CLError fileMonitorStartError = _agent.Start(out returnStatus);

            // Todo: handle file monitor start error

            CLError indexerStartError = _indexer.StartInitialIndexing(_agent.BeginProcessing,
                _agent.GetCurrentPath);

            // Todo: handle indexer start error

#if TRASH
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
#endif  // TRASH
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
            // Merged 7/13/12
            // Not Necessary.
            //    if ([self.fileSystemEvents count] > 0) {
            //        dispatch_async(get_cloud_FSM_queue(), ^{
            //            [self postEventsWithEventId:self.lastKnownEventId];
            //        });

            //    } else {
            //        dispatch_async(dispatch_get_main_queue(), ^{
            //            [self fireSimulatedPushNotification];
            //        });
            //    }
        }
    }
}
