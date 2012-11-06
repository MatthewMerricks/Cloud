﻿//
// SyncData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.FileMonitor;
using CloudApiPublic.SQLIndexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <Summary>
/// The FileMonitor implementation of the ISyncDataObject interface.
/// These methods are called by Sync.
/// </Summary>
namespace CloudApiPublic.FileMonitor.SyncImplementation
{
    public sealed class SyncData : ISyncDataObject
    {
        private MonitorAgent Monitor;
        private IndexingAgent Indexer;

        /// <summary>
        /// The SyncData constructor.  Specify the file monitor agent and
        /// the indexing agent to use.
        /// </summary>
        /// <param name="Monitor">The file monitoring agent.</param>
        /// <param name="Indexer">The indexing agent.</param>
        /// <exception cref="NullReferenceException">Thown if Monitor or Indexer is null.</exception>
        public SyncData(MonitorAgent Monitor, IndexingAgent Indexer)
        {
            if (Monitor == null)
            {
                throw new NullReferenceException("Monitor cannot be null");
            }
            if (Indexer == null)
            {
                throw new NullReferenceException("Indexer cannot be null");
            }
            this.Monitor = Monitor;
            this.Indexer = Indexer;
        }

        /// <summary>
        /// Sync is asking the file monitor for new file change events.
        /// </summary>
        /// <param name="initialFailures">The current set of file change events in error.</param>
        /// <param name="outputChanges">(output) The set of new file change events.</param>
        /// <param name="outputChangesInError">(output) The adjusted set of file change events in error.</param>
        /// <returns>An error or null.</returns>
        public CLError grabChangesFromFileSystemMonitor(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError)
        {
            return Monitor.GrabPreprocessedChanges(initialFailures,
                out outputChanges,
                out outputChangesInError);
        }

        /// <summary>
        /// Sync is presenting a set of file change events to merge to the database (indexer).
        /// </summary>
        /// <param name="mergeToFroms">The file change events to merge.</param>
        /// <param name="alreadyObtainedLock">true: The indexer lock has already been obtainte.  Default: false.</param>
        /// <returns>An error or null.</returns>
        public CLError mergeToSql(IEnumerable<FileChangeMerge> mergeToFroms,
            bool alreadyObtainedLock = false)
        {
            return Indexer.MergeEventsIntoDatabase(mergeToFroms,
                alreadyObtainedLock);
        }

        /// <summary>
        /// Queues file change events back into file change monitor, which will return later to start a new Sync process.  Pass true boolean to insert at top of the queue (LIFO).
        /// </summary>
        /// <param name="toAdd">The list of file change events to add to the queue.</param>
        /// <param name="insertAtTop">True: Add the events to the top of the queue (LIFO).</param>
        /// <param name="errorHolder">An output list of file change events in error.</param>
        /// <returns>An error or null.</returns>
        public CLError addChangesToProcessingQueue(IEnumerable<FileChange> toAdd,
            bool insertAtTop,
            GenericHolder<List<FileChange>> errorHolder)
        {
            return Monitor.AddFileChangesToProcessingQueue(toAdd,
                insertAtTop,
                errorHolder);
        }

        /// <summary>
        /// Creates a new Sync in the database by the SyncId, a list of already succesful events, and the location of the root sync directory.
        /// </summary>
        /// <param name="syncId">The Sync ID.</param>
        /// <param name="syncedEventIds">A list of successful events.</param>
        /// <param name="syncCounter">(output) Sync counter local identity.</param>
        /// <param name="newRootPath">A new SyncBox folder full path, or null.</param>
        /// <returns>An aggregated error, or null.</returns>
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

        /// <summary>
        /// Get the last Sync ID.
        /// </summary>
        public string getLastSyncId
        {
            get
            {
                Indexer.LastSyncLocker.EnterReadLock();
                try
                {
                    return Indexer.LastSyncId;
                }
                finally
                {
                    Indexer.LastSyncLocker.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Assign file change event dependencies.
        /// </summary>
        /// <param name="toAssign">The set of file change events for which to assign dependencies.</param>
        /// <param name="currentFailures">The current set of file change events in error.</param>
        /// <param name="outputChanges">(output) The new set of top-level file change events to process.</param>
        /// <param name="outputFailures">(output) The new set of file change events in error.</param>
        /// <returns>An aggregated error or null.</returns>
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

        /// <summary>
        /// Applies a Sync_From FileChange to the local file system i.e. a folder creation would cause the local FileSystem to create a folder locally;
        /// changes in-memory index first to prevent firing Sync_To events.
        /// </summary>
        /// <param name="toApply">The file change event to apply.</param>
        /// <returns>An aggregated error or null.</returns>
        public CLError applySyncFromChange(FileChange toApply)
        {
            return Monitor.ApplySyncFromFileChange(toApply);
        }

        /// <summary>
        /// Use the indexer to includes an event in the last set of sync states,
        /// or in other words processes it as complete
        /// (event will no longer be included in GetEventsSinceLastSync).
        /// </summary>
        /// <param name="eventId">Primary key value of the event to process.</param>
        /// <returns>Returns an error that occurred marking the event complete, or null.</returns>
        public CLError completeSingleEvent(long eventId)
        {
            return Indexer.MarkEventAsCompletedOnPreviousSync(eventId);
        }

        /// <summary>
        /// Use the indexer to retrieve a file's metadata by path and revision.
        /// </summary>
        /// <param name="path">The full path of the file.</param>
        /// <param name="revision">The revision.</param>
        /// <param name="metadata">(output) The returned metadata.</param>
        /// <returns>An error or null.</returns>
        public CLError getMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            return Indexer.GetMetadataByPathAndRevision(path, revision, out metadata);
        }
    }
}