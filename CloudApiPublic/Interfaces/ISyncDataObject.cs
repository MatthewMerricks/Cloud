﻿//
// ISyncDataObject.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Interfaces
{
    /*! \interface CloudApiPublic.Interfaces.ISyncDataObject
     * 
     * \section sec Overview
     * 
     * Overview of ISyncDataObject
     * 
     * \subsection subSec1 Examples
     * 
     * Here is an example:
     * 
     * \code{.cs}
     *  using CloudApiPublic.Interfaces;
     * 
     *  public sealed class EventSource : ISyncDataObject
     *  {
     *      // my interface methods here!
     *  }
     * \endcode
     */

    /// <summary>
    /// Interface to implement on an event source to pass to a SyncEngine; do not use any instance of an implementor for more than one SyncEngine
    /// </summary>
    public interface ISyncDataObject
    {
        /// <summary>
        /// Callback from SyncEngine to retrieve events to process, with dependencies assigned;
        /// outputChangesInError should have true for IsPreexisting if and only if the error was passed in input via initialFailures;
        /// outputChanges should have only FileChanges without dependencies (highest level changes) and should contain a stream if it is a change requiring a file upload
        /// (do not include a stream for file upload for any dependencies underneath)
        /// </summary>
        /// <param name="initialFailures">Passed in failures that were previously queued for reprocessing, to be used to merge in possible dependencies from new events (may have dependencies)</param>
        /// <param name="outputChanges">(output) Highest level FileChanges and necessary Streams to process (without dependencies)</param>
        /// <param name="outputChangesInError">(output) Highest level FileChanges to be queued for error processing</param>
        /// <returns>Should return any error that occured while grabbing events, should not throw the exception</returns>
        CLError grabChangesFromFileSystemMonitor(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError);

        /// <summary>
        /// Callback from SyncEngine for updating database with changes to FileChanges
        /// </summary>
        /// <param name="mergeToFroms">Enumerable of FileChanges to merge into database</param>
        /// <returns>Should return any error that occurred while updating the database, should not throw the exception</returns>
        CLError mergeToSql(IEnumerable<FileChangeMerge> mergeToFroms);

        /// <summary>
        /// Callback from SyncEngine for adding reprocessing failures or previously dependent FileChanges to queue for next SyncEngine Run
        /// (should return these changes on a subsequent call to grabChangesFromFileSystemMonitor)
        /// </summary>
        /// <param name="toAdd">Enumerable of FileChanges to queue for next SyncEngine Run</param>
        /// <param name="insertAtTop">Whether these new changes should come back first on next SyncEngine Run (as opposed to placing on bottom)</param>
        /// <param name="errorHolder">A holder containing a List of FileChanges which should be appended with changes which cannot be added back to the processing queue;
        /// Value property of holder should start at null so on first append create a new list to set the Value</param>
        /// <returns>Should return any error that occurred while adding FileChanges, should not throw the exception</returns>
        CLError addChangesToProcessingQueue(IEnumerable<FileChange> toAdd,
            bool insertAtTop,
            GenericHolder<List<FileChange>> errorHolder);

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
        CLError completeSyncSql(string syncId,
            IEnumerable<long> syncedEventIds,
            out long syncCounter,
            string newRootPath = null);

        /// <summary>
        /// Should return the latest recorded sync id (from latest callback of completeSyncSql), return null or "0" if no syncs have been previously recorded
        /// </summary>
        string getLastSyncId { get; }

        /// <summary>
        /// Callback from SyncEngine to assign dependencies between provided processing changes (toAssign), currentFailures, and any events queued in the event source for the next grabChangesFromFileSystemMonitor callback;
        /// For events pulled from the queue for the next grabChangesFromFileSystemMonitor callback into one of the outputs, make sure to not return them on the next grabChangesFromFileSystemMonitor callback unless added back again via addChangesToProcessingQueue callback;
        /// Any streams which are not returned in outputChanges (at the highest level only) should be disposed
        /// </summary>
        /// <param name="toAssign">Processing changes (may have dependencies)</param>
        /// <param name="currentFailures">Current failures which would be reprocessed later (may have dependencies)</param>
        /// <param name="outputChanges">(output) Changes without dependencies (highest level) to process only with original streams from toAssign input (do not start new streams); dependencies should have no streams</param>
        /// <param name="outputFailures">(output) Changes which will be placed back into the failure queue for reprocessing later</param>
        /// <returns>Should return any error that occurred while rebuilding the dependency trees, should not throw any exception</returns>
        CLError dependencyAssignment(IEnumerable<PossiblyStreamableFileChange> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<FileChange> outputFailures);

        /// <summary>
        /// Callback from SyncEngine to perform the action of a FileChange locally (i.e. for a Sync From Folder Creation, actually create the folder on disk);
        /// May include moving a downloaded file from a temporary download path (outside the root directoy) to somewhere within the root directory;
        /// Make sure to create parent folders as needed to handle the current change;
        /// Do not return resulting events back on subsequent calls to grabChangesFromFileSystemMonitor
        /// </summary>
        /// <param name="toApply">FileChange to perform</param>
        /// <returns>Should return any error that occurred while performing the FileChange</returns>
        CLError applySyncFromChange(FileChange toApply);

        /// <summary>
        /// Callback from SyncEngine to complete a single FileChange as of the last sync (used for asynchronously completed operations file upload and file download)
        /// </summary>
        /// <param name="toApply">Id of a single FileChange to mark complete</param>
        /// <returns>Should return any error that occurred while marking a FileChange complete, should not throw any exception</returns>
        CLError completeSingleEvent(long eventId);

        /// <summary>
        /// Callback from SyncEngine to retrieve the latest file metadata for a given full path and revision; if not found output null instead of returning an error for not found
        /// </summary>
        /// <param name="path">Full path to the file</param>
        /// <param name="revision">Previous revision of the file</param>
        /// <param name="metadata">(output) The retrieved metadata for the file</param>
        /// <returns>Should return any error that occurred while retrieving the file metadata, should not throw any exception</returns>
        CLError getMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata);
    }
}