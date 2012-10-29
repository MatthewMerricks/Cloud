//
// SyncData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using FileMonitor;
using SQLIndexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using win_client.Services.FileSystemMonitoring;

namespace win_client.SyncImplementations
{
    public sealed class SyncData : ISyncDataObject
    {
        private MonitorAgent Monitor;
        private IndexingAgent Indexer;

        public CLError grabChangesFromFileSystemMonitor(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError)
        {
            return Monitor.GrabPreprocessedChanges(initialFailures,
                out outputChanges,
                out outputChangesInError);
        }

        public CLError mergeToSql(IEnumerable<FileChangeMerge> mergeToFroms,
            bool alreadyObtainedLock = false)
        {
            return Indexer.MergeEventsIntoDatabase(mergeToFroms,
                alreadyObtainedLock);
        }

        public CLError addChangesToProcessingQueue(IEnumerable<FileChange> toAdd,
            bool insertAtTop,
            GenericHolder<List<FileChange>> errorHolder)
        {
            return Monitor.AddFileChangesToProcessingQueue(toAdd,
                insertAtTop,
                errorHolder);
        }

        public CLError completeSyncSql(string syncId,
            IEnumerable<long> syncedEventIds,
            out long syncCounter,
            string newRootPath = null)
        {
            return Indexer.RecordCompletedSync(syncId,
                syncedEventIds,
                out syncCounter,
                newRootPath);
        }

        public string getLastSyncId
        {
            get
            {
                CLFSMonitoringService.Instance.IndexingAgent.LastSyncLocker.EnterReadLock();
                try
                {
                    return CLFSMonitoringService.Instance.IndexingAgent.LastSyncId;
                }
                finally
                {
                    CLFSMonitoringService.Instance.IndexingAgent.LastSyncLocker.ExitReadLock();
                }
            }
        }

        public CLError dependencyAssignment(IEnumerable<PossiblyStreamableFileChange> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<FileChange> outputFailures)
        {
            return Monitor.AssignDependencies(toAssign,
                currentFailures,
                out outputChanges,
                out outputFailures);
        }

        public string getCloudRoot
        {
            get
            {
                return CLFSMonitoringService.Instance.MonitorAgent.GetCurrentPath();
            }
        }

        public CLError applySyncFromChange(FileChange toApply)
        {
            return Monitor.ApplySyncFromFileChange(toApply);
        }

        public CLError completeSingleEvent(long eventId)
        {
            return Indexer.MarkEventAsCompletedOnPreviousSync(eventId);
        }

        public CLError getMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            return Indexer.GetMetadataByPathAndRevision(path, revision, out metadata);
        }
    }
}