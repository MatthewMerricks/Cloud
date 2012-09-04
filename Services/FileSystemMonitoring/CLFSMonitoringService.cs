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
using Sync;
using SQLIndexer;
using CloudApiPublic.Static;
using win_client.Common;


namespace win_client.Services.FileSystemMonitoring
{
    public sealed class CLFSMonitoringService
    {
        static readonly CLFSMonitoringService _instance = new CLFSMonitoringService();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace = CLTrace.Instance;
        public MonitorAgent MonitorAgent { get; private set; }
        public IndexingAgent IndexingAgent { get; private set; }

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

            // Start the indexer at the first reference of the FSM.
            IndexingAgent indexerToSet;
            CLError indexCreationError = IndexingAgent.CreateNewAndInitialize(out indexerToSet);
            if (indexerToSet == null)
            {
                _trace.writeToLog(1, "CLFSMonitoringService: ERROR: Creating the indexer.");
            }
            IndexingAgent = indexerToSet;
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {

            // Todo: handle index creation error

            MonitorAgent monitorToSet;
            CLError fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(Settings.Instance.CloudFolderPath,
                out monitorToSet,
                global::Sync.Sync.Run,
                IndexingAgent.MergeEventIntoDatabase,
                (syncId, successfulEventIds, newRootPath) =>
                {
                    long newSyncCounter;
                    return IndexingAgent.RecordCompletedSync(syncId, successfulEventIds, out newSyncCounter, newRootPath);
                },
                () =>
                {
                    IndexingAgent.LastSyncLocker.EnterReadLock();
                    try
                    {
                        return IndexingAgent.LastSyncId;
                    }
                    finally
                    {
                        IndexingAgent.LastSyncLocker.ExitReadLock();
                    }
                },
                IndexingAgent.MarkEventAsCompletedOnPreviousSync,
                IndexingAgent.GetMetadataByPathAndRevision,
                () => Settings.Instance.DeviceName,
                () => IndexingAgent.CELocker);
            if (monitorToSet != null)
            {
                this.MonitorAgent = monitorToSet;

                CLAppMessages.Message_DidReceivePushNotificationFromServer.Register(monitorToSet, monitorToSet.PushNotification);
            }

            // Todo: handle file monitor creation error

            MonitorStatus returnStatus;
            CLError fileMonitorStartError = MonitorAgent.Start(out returnStatus);

            // Todo: handle file monitor start error

            CLError indexerStartError = IndexingAgent.StartInitialIndexing(MonitorAgent.BeginProcessing,
                MonitorAgent.GetCurrentPath);

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
            if (MonitorAgent != null)
            {
                MonitorAgent.Dispose();
                MonitorAgent = null;
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
