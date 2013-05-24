//
// SyncData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using Cloud.FileMonitor;
using Cloud.SQLIndexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.SQLIndexer.Model;

namespace Cloud.FileMonitor.SyncImplementation
{
    /// <Summary>
    /// The FileMonitor implementation of the ISyncDataObject interface.
    /// These methods are called by Sync.
    /// </Summary>
    internal sealed class SyncData : ISyncDataObject
    {
        private MonitorAgent Monitor;
        private IndexingAgent Indexer;
        
        /// <summary>
        /// Callback from SyncEngine upon completion of the primary Sync logic with a sync id to store along with ids of events which were completed in the process;
        /// must output an incrementing counter to record all syncs;
        /// if a newRootPath is provided and different from the previous root, update accordingly
        /// </summary>
        /// <param name="syncId">New sync id to store (should be returned on getLastSyncId on next call)</param>
        /// <param name="syncedEventIds">Ids of events which were completed</param>
        /// <param name="syncCounter">(output) Incrementing counter to record all syncs</param>
        /// <param name="newRootPath">(optional) If provided and different from the previous root, make sure to update accordingly</param>
        /// <returns>Should return any error that occurred while marking sync completion, should not throw the exception</returns>
        public CLError RecordCompletedSync(IEnumerable<PossiblyChangedFileChange> communicatedChanges, string syncId, IEnumerable<long> syncedEventIds, out long syncCounter, string rootFolderUID = null)
        {
            return this.Indexer.RecordCompletedSync(communicatedChanges, syncId, syncedEventIds, out syncCounter, rootFolderUID);
        }

        public CLError CreateNewServerUid(string serverUid, string revision, out long serverUidId, SQLTransactionalBase existingTransaction = null)
        {
            return this.Indexer.CreateNewServerUid(serverUid, revision, out serverUidId, existingTransaction);
        }

        public CLError UpdateServerUid(long serverUidId, string serverUid, string revision, out Nullable<long> existingServerUidIdRequiringMerging, SQLTransactionalBase existingTransaction = null)
        {
            return this.Indexer.UpdateServerUid(serverUidId, serverUid, revision, out existingServerUidIdRequiringMerging, existingTransaction);
        }

        public CLError QueryServerUid(long serverUidId, out string serverUid, out string revision, SQLTransactionalBase existingTransaction = null)
        {
            return this.Indexer.QueryServerUid(serverUidId, out serverUid, out revision, existingTransaction);
        }

        public CLError QueryOrCreateServerUid(string serverUid, out long serverUidId, string revision, bool syncFromFileModify, SQLTransactionalBase existingTransaction = null)
        {
            return this.Indexer.QueryOrCreateServerUid(serverUid, out serverUidId, revision, syncFromFileModify, existingTransaction);
        }
        
        /// <summary>
        /// ¡¡ Call this carefully, completely wipes index database (use when user deletes local repository or relinks) !!
        /// </summary>
        /// <param name="newRootPath">Full path string to directory to sync without any trailing slash (except for drive letter root)</param>
        /// <returns>Returns any error that occurred while wiping the database index</returns>
        public CLError WipeIndex(string newRootPath)
        {
            return this.Indexer.WipeIndex(newRootPath);
        }

        public CLError ChangeSyncboxPath(string newSyncboxPath)
        {
            return this.Indexer.ChangeSyncboxPath(newSyncboxPath);
        }

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
        /// <param name="nullChangeFound">(output) Whether a null FileChange was found in the processing queue (which does not get output)</param>
        /// <param name="firstTimeRunning">Whether this is the first time the engine was ran</param>
        /// <param name="failedOutChanges">(optional) The list containing failed out changes which should be locked if it exists by the method caller</param>
        /// <returns>An error or null.</returns>
        public CLError grabChangesFromFileSystemMonitor(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out int outputChangesCount,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError,
            out int outputChangesInErrorCount,
            out bool nullChangeFound,
            bool firstTimeRunning,
            List<FileChange> failedOutChanges = null)
        {
            return Monitor.GrabPreprocessedChanges(initialFailures,
                out outputChanges,
                out outputChangesCount,
                out outputChangesInError,
                out outputChangesInErrorCount,
                out nullChangeFound,
                firstTimeRunning,
                failedOutChanges);
        }

        /// <summary>
        /// Sync is presenting a set of file change events to merge to the database (indexer).
        /// </summary>
        /// <param name="mergeToFroms">The file change events to merge.</param>
        /// <param name="alreadyObtainedLock">true: The indexer lock has already been obtainte.  Default: false.</param>
        /// <returns>An error or null.</returns>
        public CLError mergeToSql(IEnumerable<FileChangeMerge> mergeToFroms, SQLTransactionalBase existingTransaction = null)
        {
            return Indexer.MergeEventsIntoDatabase(mergeToFroms, existingTransaction);
        }

        /// <summary>
        /// Creates a new transactional object which can be passed back into database access calls and externalizes the ability to dispose or commit the transaction
        /// </summary>
        public SQLTransactionalBase GetNewTransaction()
        {
            return Indexer.GetNewTransaction();
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
        /// <param name="failedOutChanges">(optional) The list containing failed out changes which should be locked if it exists by the method caller</param>
        /// <returns>An aggregated error or null.</returns>
        public CLError dependencyAssignment(IEnumerable<PossiblyStreamableFileChange> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<FileChange> outputFailures,
            List<FileChange> failedOutChanges = null)
        {
            return Monitor.AssignDependencies(toAssign,
                currentFailures,
                out outputChanges,
                out outputFailures,
                failedOutChanges);
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
        /// Callback from SyncEngine to perform the action of a FileChange locally (i.e. for a Sync From Folder Creation, actually create the folder on disk);
        /// May include moving a downloaded file from a temporary download path (outside the root directoy) to somewhere within the root directory;
        /// Make sure to create parent folders as needed to handle the current change;
        /// Do not return resulting events back on subsequent calls to grabChangesFromFileSystemMonitor
        /// </summary>
        /// <param name="toApply">FileChange to perform</param>
        /// <returns>Should return any error that occurred while performing the FileChange</returns>
        public CLError applySyncFromChange<T>(FileChange toApply, Func<T, bool> onAllPathsLockAndReturnWhetherToContinue, Action<T> onBeforeAllPathsUnlock, T userState, object lockerInsideAllPaths)
        {
            return Monitor.ApplySyncFromFileChange(toApply, onAllPathsLockAndReturnWhetherToContinue, onBeforeAllPathsUnlock, userState, lockerInsideAllPaths);
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

        public CLError GetCalculatedFullPathByServerUid(string serverUid, out string calculatedFullPath, Nullable<long> excludedEventId = null)
        {
            return Indexer.GetCalculatedFullPathByServerUid(serverUid, out calculatedFullPath, excludedEventId);
        }

        public CLError GetServerUidByNewPath(string newPath, out string serverUid)
        {
            return Indexer.GetServerUidByNewPath(newPath, out serverUid);
        }

        //public void SwapOrderBetweenTwoEventIds(long eventIdA, long eventIdB, SQLTransactionalBase requiredTransaction)
        //{
        //    Indexer.SwapOrderBetweenTwoEventIds(eventIdA, eventIdB, requiredTransaction);
        //}
    }
}