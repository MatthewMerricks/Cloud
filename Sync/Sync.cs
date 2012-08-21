﻿//
// Sync.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using FileMonitor;

namespace Sync
{
    public static class Sync
    {
        private const int HttpTimeoutMilliseconds = 180000;// 180 seconds

        private static ProcessingQueuesTimer GetFailureTimer(Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> AddChangesToProcessingQueue)
        {
            lock (failureTimerLocker)
            {
                if (_failureTimer == null)
                {
                    if (AddChangesToProcessingQueue == null)
                    {
                        throw new NullReferenceException("AddChangesToProcessingQueue cannot be null");
                    }

                    CLError timerError = ProcessingQueuesTimer.CreateAndInitializeProcessingQueuesTimer(FailureProcessing,
                        10000,// wait five seconds between processing
                        out _failureTimer,
                        AddChangesToProcessingQueue);
                    if (timerError != null)
                    {
                        throw timerError.GrabFirstException();
                    }
                }
                return _failureTimer;
            }
        }
        private static ProcessingQueuesTimer _failureTimer = null;
        private static object failureTimerLocker = new object();

        // private queue for failures;
        // lock on failureTimer.TimerRunningLocker for all access
        private static Queue<FileChange> FailedChangesQueue = new Queue<FileChange>();

        private static bool TempDownloadsCleaned = false;
        private static object TempDownloadsCleanedLocker = new object();
        private static HashSet<Guid> TempDownloads = new HashSet<Guid>();
        private static string TempDownloadsFolder = null;

        // EventHandler for when the _failureTimer hits the end of its timer;
        // state object must be the Function which adds failed items to the FileMonitor processing queue
        private static void FailureProcessing(object state)
        {
            try
            {
                // cast state to Function type for adding failed items to the FileMonitor processing queue
                Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> AddChangesToProcessingQueue = state as Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>;

                // cast state is required
                if (AddChangesToProcessingQueue == null)
                {
                    throw new NullReferenceException("state must not not be null and must be castable to Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError>");
                }

                lock (GetFailureTimer(AddChangesToProcessingQueue).TimerRunningLocker)
                {
                    // dequeue all failed items into an array
                    FileChange[] failedChanges = new FileChange[FailedChangesQueue.Count];
                    for (int failedChangeIndex = 0; failedChangeIndex < failedChanges.Length; failedChangeIndex++)
                    {
                        failedChanges[failedChangeIndex] = FailedChangesQueue.Dequeue();
                    }

                    try
                    {
                        // Add failed changes back to FileMonitor via cast state;
                        // if there are any adds in error requeue them to the failure queue;
                        // Log any errors
                        GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>();
                        CLError err = AddChangesToProcessingQueue(failedChanges, true, errList);
                        if (errList.Value != null)
                        {
                            foreach (FileChange currentError in errList.Value)
                            {
                                FailedChangesQueue.Enqueue(currentError);

                                GetFailureTimer(AddChangesToProcessingQueue).StartTimerIfNotRunning();
                            }
                        }
                        if (err != null)
                        {
                            err.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                        }
                    }
                    catch
                    {
                        // An error occurred adding all the failed changes to the FileMonitor;
                        // requeue them all to the failure queue;
                        // rethrow error for logging
                        foreach (FileChange currentError in failedChanges)
                        {
                            FailedChangesQueue.Enqueue(currentError);

                            GetFailureTimer(AddChangesToProcessingQueue).StartTimerIfNotRunning();
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                ((CLError)ex).LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
            }
        }

        // extension method so that whenever CLError FileStreams are dequeued,
        // they can be disposed with a simple method call
        private static CLError DisposeAllStreams(this IEnumerable<FileStream> allStreams)
        {
            CLError disposalError = null;
            if (allStreams != null)
            {
                foreach (FileStream currentStream in allStreams)
                {
                    if (currentStream != null)
                    {
                        try
                        {
                            currentStream.Dispose();
                        }
                        catch (Exception ex)
                        {
                            disposalError += ex;
                        }
                    }
                }
            }
            return disposalError;
        }

        /// <summary>
        /// Primary method for all syncing (both From and To),
        /// synchronized so only a single thread can access it at a time
        /// </summary>
        /// <param name="grabChangesFromFileSystemMonitor">Retrieves all changes ready to process via delegate from FileMonitor</param>
        /// <param name="mergeToSql">Updates or inserts a FileChange object as an Event into SQL</param>
        /// <param name="addChangesToProcessingQueue">Queues FileChanges back into FileMonitor which will return later to start a new Sync process, pass true boolean to insert at top (LIFO)</param>
        /// <param name="respondingToPushNotification">Without any FileChanges to process, Sync communication will only occur if this parameter is set to true</param>
        /// <param name="completeSyncSql">Creates a new Sync in the database by SyncId, a list of already succesful events, and the location of the root sync directory</param>
        /// <returns>Returns all aggregated errors that occurred during the synchronous part of the Sync process, if any</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static CLError Run(GrabProcessedChanges grabChangesFromFileSystemMonitor,
            Func<FileChange, FileChange, CLError> mergeToSql,
            Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> addChangesToProcessingQueue,
            bool respondingToPushNotification,
            Func<string, IEnumerable<long>, string, CLError> completeSyncSql,
            Func<string> getLastSyncId,
            DependencyAssignments dependencyAssignment,
            Func<string> getCloudRoot,
            Func<FileChange, CLError> applySyncFromChange,
            Func<long, CLError> completeSingleEvent)
        {
            CLError toReturn = null;
            string syncStatus = "Sync Run entered";
            // errorsToQueue will have all changes to process,
            // items will be ignored as their successfulEventId is added
            List<KeyValuePair<FileChange, FileStream>> errorsToQueue = null;
            List<long> successfulEventIds = new List<long>();
            IEnumerable<FileChange> thingsThatWereDependenciesToQueue = null;
            try
            {
                // assert parameters are set
                if (grabChangesFromFileSystemMonitor == null)
                {
                    throw new NullReferenceException("grabChangesFromFileSystemMonitor cannot be null");
                }
                if (mergeToSql == null)
                {
                    throw new NullReferenceException("mergeToSql cannot be null");
                }
                if (addChangesToProcessingQueue == null)
                {
                    throw new NullReferenceException("addChangesToProcessingQueue cannot be null");
                }
                if (completeSyncSql == null)
                {
                    throw new NullReferenceException("completeSyncSql cannot be null");
                }
                if (getLastSyncId == null)
                {
                    throw new NullReferenceException("getLastSyncId cannot be null");
                }
                if (dependencyAssignment == null)
                {
                    throw new NullReferenceException("dependencyAssignment cannot be null");
                }

                bool tempNeedsCleaning = false;
                lock (TempDownloadsCleanedLocker)
                {
                    if (!TempDownloadsCleaned)
                    {
                        TempDownloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create) +
                            "\\Cloud\\DownloadTemp";

                        if (!Directory.Exists(TempDownloadsFolder))
                        {
                            Directory.CreateDirectory(TempDownloadsFolder);
                        }
                        else
                        {
                            tempNeedsCleaning = true;
                        }
                        TempDownloadsCleaned = true;
                    }
                }
                if (tempNeedsCleaning)
                {
                    lock (TempDownloads)
                    {
                        DirectoryInfo tempDownloadsFolderInfo = new DirectoryInfo(TempDownloadsFolder);
                        foreach (FileInfo currentTempFile in tempDownloadsFolderInfo.GetFiles())
                        {
                            if (currentTempFile.Name.Length == 32)
                            {
                                Guid tempGuid;
                                if (Guid.TryParse(currentTempFile.Name, out tempGuid))
                                {
                                    if (!TempDownloads.Contains(tempGuid))
                                    {
                                        try
                                        {
                                            currentTempFile.Delete();
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                IEnumerable<KeyValuePair<FileChange, FileStream>> outputChanges;
                IEnumerable<FileChange> outputChangesInError;
                lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                {
                    IEnumerable<FileChange> initialErrors = new FileChange[FailedChangesQueue.Count];
                    for (int initialErrorIndex = 0; initialErrorIndex < ((FileChange[])initialErrors).Length; initialErrorIndex++)
                    {
                        ((FileChange[])initialErrors)[initialErrorIndex] = FailedChangesQueue.Dequeue();
                    }

                    syncStatus = "Sync Run dequeued initial failures for dependency check";

                    try
                    {
                        toReturn = grabChangesFromFileSystemMonitor(initialErrors,
                            out outputChanges,
                            out outputChangesInError);
                    }
                    catch (Exception ex)
                    {
                        outputChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<FileChange, FileStream>>>();
                        outputChangesInError = Helpers.DefaultForType<IEnumerable<FileChange>>();
                        toReturn = ex;
                    }
                }
                // set errors to queue here with all processed changes and all failed changes so they can be added to failure queue on exception
                errorsToQueue = new List<KeyValuePair<FileChange, FileStream>>(outputChanges
                    .Concat(outputChangesInError.Select(currentChangeInError => new KeyValuePair<FileChange, FileStream>(currentChangeInError, null))));

                syncStatus = "Sync Run grabbed processed changes (with dependencies and final metadata)";

                // Synchronously or asynchronously fire off all events without dependencies that have a storage key (MDS events);
                // leave changes that did not complete in the errorsToQueue list so they will be added to the failure queue later;
                // pull out inner dependencies and append to the enumerable thingsThatWereDependenciesToQueue;
                // repeat this process with inner dependencies
                List<KeyValuePair<FileChange, FileStream>> preprocessedEvents = new List<KeyValuePair<FileChange, FileStream>>();
                HashSet<long> preprocessedEventIds = new HashSet<long>();
                // function which reinserts dependencies into topLevelChanges in sorted order and returns true for reprocessing
                Func<bool> reprocessForDependencies = () =>
                    {
                        if (thingsThatWereDependenciesToQueue != null)
                        {
                            List<FileChange> uploadDependenciesWithoutStreams = new List<FileChange>();

                            foreach (FileChange currentDependency in thingsThatWereDependenciesToQueue)
                            {
                                // these conditions ensure that no file uploads without FileStreams are processed in the current batch
                                if (currentDependency.Metadata == null
                                    || currentDependency.Metadata.HashableProperties.IsFolder
                                    || currentDependency.Type == FileChangeType.Deleted
                                    || currentDependency.Type == FileChangeType.Renamed
                                    || currentDependency.Direction == SyncDirection.From)
                                {
                                    errorsToQueue.Add(new KeyValuePair<FileChange, FileStream>(currentDependency, null));
                                }
                                else
                                {
                                    uploadDependenciesWithoutStreams.Add(currentDependency);
                                }
                            }

                            if (Enumerable.SequenceEqual(thingsThatWereDependenciesToQueue, uploadDependenciesWithoutStreams))
                            {
                                return false;
                            }

                            outputChanges = outputChanges
                                .Except(preprocessedEvents)
                                .Concat(thingsThatWereDependenciesToQueue
                                    .Except(uploadDependenciesWithoutStreams)
                                    .Select(currentDependencyToQueue => new KeyValuePair<FileChange, FileStream>(currentDependencyToQueue, null)));

                            thingsThatWereDependenciesToQueue = uploadDependenciesWithoutStreams;
                            return true;
                        }
                        return false;
                    };

                // process once then repeat if it needs to reprocess for dependencies
                do
                {
                    foreach (KeyValuePair<FileChange, FileStream> topLevelChange in outputChanges)
                    {
                        if (topLevelChange.Key.EventId > 0
                            && !preprocessedEventIds.Contains(topLevelChange.Key.EventId)
                            && topLevelChange.Key.Metadata != null
                            && !string.IsNullOrWhiteSpace(topLevelChange.Key.Metadata.StorageKey))
                        {
                            preprocessedEvents.Add(topLevelChange);
                            preprocessedEventIds.Add(topLevelChange.Key.EventId);
                            Nullable<long> successfulEventId;
                            Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask;
                            Exception completionException = CompleteFileChange(topLevelChange, applySyncFromChange, completeSingleEvent, GetFailureTimer(addChangesToProcessingQueue), out successfulEventId, out asyncTask);
                            if (successfulEventId != null
                                && (long)successfulEventId > 0)
                            {
                                successfulEventIds.Add((long)successfulEventId);
                                errorsToQueue.Remove(topLevelChange);

                                FileChangeWithDependencies changeWithDependencies = topLevelChange.Key as FileChangeWithDependencies;
                                if (changeWithDependencies != null
                                    && changeWithDependencies.DependenciesCount > 0)
                                {
                                    if (thingsThatWereDependenciesToQueue == null)
                                    {
                                        thingsThatWereDependenciesToQueue = changeWithDependencies.Dependencies;
                                    }
                                    else
                                    {
                                        thingsThatWereDependenciesToQueue = thingsThatWereDependenciesToQueue.Concat(changeWithDependencies.Dependencies);
                                    }
                                }
                            }
                            else
                            {
                                if (completionException != null)
                                {
                                    toReturn += completionException;
                                }
                                else
                                {
                                    errorsToQueue.Remove(topLevelChange);
                                }

                                if (asyncTask != null)
                                {
                                    KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>> nonNullTask = (KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>)asyncTask;

                                    nonNullTask.Value.ContinueWith(completeState =>
                                        {
                                            CLError sqlCompleteError = completeState.Result.Value(completeState.Result.Key);
                                            if (sqlCompleteError != null)
                                            {
                                                sqlCompleteError.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                            }
                                        });

                                    nonNullTask.Value.Start(HttpScheduler.GetSchedulerByDirection(nonNullTask.Key));
                                }
                            }
                        }
                    }
                }
                while (reprocessForDependencies());

                syncStatus = "Sync Run initial operations completed synchronously or queued";

                KeyValuePair<FileChange, FileStream>[] changesForCommunication = outputChanges.Except(preprocessedEvents).ToArray();
                if (changesForCommunication.Length > CLDefinitions.SyncConstantsMaximumSyncToEvents)
                {
                    KeyValuePair<FileChange, FileStream>[] excessEvents = changesForCommunication.Skip(CLDefinitions.SyncConstantsMaximumSyncToEvents).ToArray();

                    foreach (FileStream currentExcessStream in excessEvents.Select(currentExcess => currentExcess.Value))
                    {
                        if (currentExcessStream != null)
                        {
                            toReturn += currentExcessStream;
                        }
                    }

                    GenericHolder<List<FileChange>> queueingErrors = new GenericHolder<List<FileChange>>();
                    CLError addExcessEventsError = addChangesToProcessingQueue(excessEvents.Select(currentExcess => currentExcess.Key),
                        /* add to top */ true,
                        queueingErrors);

                    if (queueingErrors.Value != null)
                    {
                        lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                        {
                            foreach (FileChange currentQueueingError in queueingErrors.Value)
                            {
                                FailedChangesQueue.Enqueue(currentQueueingError);

                                GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                            }
                        }
                    }
                    if (addExcessEventsError != null)
                    {
                        toReturn += new AggregateException("Error adding excess events to processing queue before sync", addExcessEventsError.GrabExceptions());
                    }

                    errorsToQueue = new List<KeyValuePair<FileChange, FileStream>>(errorsToQueue.Except(excessEvents));
                }

                // Take events without dependencies that were not fired off in order to perform communication (or Sync From for no events left)
                IEnumerable<KeyValuePair<bool, FileChange>> completedChanges;
                IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> incompleteChanges;
                IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>> changesInError;
                string newSyncId;
                Exception communicationException = CommunicateWithServer(changesForCommunication.Take(CLDefinitions.SyncConstantsMaximumSyncToEvents),
                    mergeToSql,
                    getLastSyncId,
                    respondingToPushNotification,
                    getCloudRoot,
                    out completedChanges,
                    out incompleteChanges,
                    out changesInError,
                    out newSyncId);
                if (communicationException != null)
                {
                    toReturn += communicationException;
                }
                else if (newSyncId == null)
                {
                    syncStatus = "Sync Run communication aborted e.g. Sync To with no events";
                }
                else
                {
                    syncStatus = "Sync Run communication complete";

                    if (completedChanges != null)
                    {
                        foreach (KeyValuePair<bool, FileChange> currentCompletedChange in completedChanges)
                        {
                            successfulEventIds.Add(currentCompletedChange.Value.EventId);
                        }
                    }

                    // Merge in server values into DB (storage key, revision, etc) and add new Sync From events
                    foreach (KeyValuePair<bool, FileChange> currentStoreUpdate in (completedChanges ?? Enumerable.Empty<KeyValuePair<bool, FileChange>>())
                        .Concat((incompleteChanges ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>())
                            .Select(currentIncompleteChange => new KeyValuePair<bool, FileChange>(currentIncompleteChange.Key, currentIncompleteChange.Value.Key)))
                        .Concat((changesInError ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>())
                            .Select(currentChangeInError => new KeyValuePair<bool, FileChange>(currentChangeInError.Key, currentChangeInError.Value.Key))))
                    {
                        if (currentStoreUpdate.Key)
                        {
                            mergeToSql(currentStoreUpdate.Value, null);
                        }
                    }
                    if (changesInError != null)
                    {
                        foreach (KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>> grabException in changesInError)
                        {
                            toReturn += grabException.Value.Value.Value;
                            toReturn += grabException.Value.Value.Key;
                        }
                    }

                    syncStatus = "Sync Run server values merged into database";

                    // Within a lock on the failure queue (failureTimer.TimerRunningLocker),
                    // check if each current server action needs to be moved to a dependency under a failure event or a server action in the current batch
                    lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                    {
                        // Initialize and fill an array of FileChanges dequeued from the failure queue
                        FileChange[] dequeuedFailures = new FileChange[FailedChangesQueue.Count];
                        for (int currentQueueIndex = 0; currentQueueIndex < dequeuedFailures.Length; currentQueueIndex++)
                        {
                            dequeuedFailures[currentQueueIndex] = FailedChangesQueue.Dequeue();
                        }

                        // Define errors to set after dependency calculations
                        // (will be top level a.k.a. not have any dependencies)
                        IEnumerable<FileChange> topLevelErrors;

                        try
                        {
                            successfulEventIds.Sort();

                            List<KeyValuePair<FileChange, FileStream>> uploadFilesWithoutStreams = new List<KeyValuePair<FileChange, FileStream>>(
                                (thingsThatWereDependenciesToQueue ?? Enumerable.Empty<FileChange>())
                                .Select(uploadFileWithoutStream => new KeyValuePair<FileChange, FileStream>(uploadFileWithoutStream, null)));

                            CLError postCommunicationDependencyError = dependencyAssignment((incompleteChanges ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>())
                                    .Select(currentIncompleteChange => currentIncompleteChange.Value)
                                    .Concat(uploadFilesWithoutStreams),
                                dequeuedFailures.Concat((changesInError ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>())
                                    .Select(currentChangeInError => currentChangeInError.Value.Key)
                                    .Where(currentChangeInError => currentChangeInError != null)),// FileChange could be null for errors if there was an exeption but no FileChange was built
                                out outputChanges,
                                out topLevelErrors);
                            if (postCommunicationDependencyError != null)
                            {
                                if (outputChanges == null
                                    || topLevelErrors == null)
                                {
                                    throw new AggregateException("Error on dependencyAssignment and outputs are not set", postCommunicationDependencyError.GrabExceptions());
                                }
                                else
                                {
                                    toReturn += new AggregateException("Error on dependencyAssignment", postCommunicationDependencyError.GrabExceptions());
                                }
                            }

                            if (outputChanges != null)
                            {
                                if (uploadFilesWithoutStreams.Count > 0)
                                {
                                    thingsThatWereDependenciesToQueue = outputChanges
                                        .Select(outputChange => outputChange.Key)
                                        .Intersect(thingsThatWereDependenciesToQueue);
                                }
                                if (thingsThatWereDependenciesToQueue != null
                                    && thingsThatWereDependenciesToQueue.Count() == 0)
                                {
                                    thingsThatWereDependenciesToQueue = null;
                                }
                                if (thingsThatWereDependenciesToQueue != null)
                                {
                                    outputChanges = outputChanges.Where(outputChange => !thingsThatWereDependenciesToQueue.Contains(outputChange.Key));
                                }
                            }

                            errorsToQueue = new List<KeyValuePair<FileChange, FileStream>>((outputChanges ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>()));

                            foreach (FileChange currentTopLevelError in topLevelErrors ?? Enumerable.Empty<FileChange>())
                            {
                                FailedChangesQueue.Enqueue(currentTopLevelError);
                            }
                        }
                        catch
                        {
                            // On error of assigning dependencies,
                            // put all the original failure queue items back in the failure queue;
                            // finally, rethrow the exception
                            for (int currentQueueIndex = 0; currentQueueIndex < dequeuedFailures.Length; currentQueueIndex++)
                            {
                                FailedChangesQueue.Enqueue(dequeuedFailures[currentQueueIndex]);
                            }
                            throw;
                        }
                    }

                    syncStatus = "Sync Run post-communication dependencies calculated";

                    List<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTasksToRun = new List<KeyValuePair<SyncDirection,Task<KeyValuePair<long,Func<long,CLError>>>>>();

                    // Synchronously complete all local operations without dependencies (exclude file upload/download) and record successful events;
                    // If a completed event has dependencies, stick them on the end of the current batch;
                    // If an event fails to complete, leave it on errorsToQueue so it will be added to the failure queue later
                    foreach (KeyValuePair<FileChange, FileStream> topLevelChange in outputChanges)
                    {
                        Nullable<long> successfulEventId;
                        Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask;
                        Exception completionException = CompleteFileChange(topLevelChange, applySyncFromChange, completeSingleEvent, GetFailureTimer(addChangesToProcessingQueue), out successfulEventId, out asyncTask);
                        if (successfulEventId != null
                            && (long)successfulEventId > 0)
                        {
                            successfulEventIds.Add((long)successfulEventId);

                            FileChangeWithDependencies changeWithDependencies = topLevelChange.Key as FileChangeWithDependencies;
                            if (changeWithDependencies != null
                                && changeWithDependencies.DependenciesCount > 0)
                            {
                                if (thingsThatWereDependenciesToQueue == null)
                                {
                                    thingsThatWereDependenciesToQueue = changeWithDependencies.Dependencies;
                                }
                                else
                                {
                                    thingsThatWereDependenciesToQueue = thingsThatWereDependenciesToQueue.Concat(changeWithDependencies.Dependencies);
                                }
                            }
                        }
                        else
                        {
                            if (completionException != null)
                            {
                                toReturn += completionException;
                            }
                            else
                            {
                                errorsToQueue.Remove(topLevelChange);
                            }

                            if (asyncTask != null)
                            {
                                asyncTasksToRun.Add((KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>)asyncTask);
                            }
                        }
                    }

                    syncStatus = "Sync Run synchronous post-communication operations complete";

                    // Write new Sync point to database with succesful events
                    CLError recordSyncError = completeSyncSql(newSyncId, successfulEventIds, Settings.Instance.CloudFolderPath);
                    if (recordSyncError != null)
                    {
                        toReturn += recordSyncError.GrabFirstException();
                    }

                    syncStatus = "Sync Run new sync point persisted";

                    // Asynchronously fire off all remaining upload/download operations without dependencies
                    foreach (KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>> asyncTask in asyncTasksToRun)
                    {
                        try
                        {
                            asyncTask.Value.ContinueWith(eventCompletion =>
                                {
                                    CLError sqlCompleteError = eventCompletion.Result.Value(eventCompletion.Result.Key);
                                    if (sqlCompleteError != null)
                                    {
                                        sqlCompleteError.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                    }
                                });
                            asyncTask.Value.Start(HttpScheduler.GetSchedulerByDirection(asyncTask.Key));
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    syncStatus = "Sync Run async tasks started after communication (end of Sync)";
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
            }

            if (thingsThatWereDependenciesToQueue != null)
            {
                try
                {
                    // add all dependencies to the top of the processing queue in the order they were discovered;
                    // if there are any errors in queueing, add the changes to the failure queue instead;
                    // also if there are any errors, add them to the returned aggregated errors
                    GenericHolder<List<FileChange>> queueingErrors = new GenericHolder<List<FileChange>>();
                    CLError queueingError = addChangesToProcessingQueue(thingsThatWereDependenciesToQueue, /* add to top */ true, queueingErrors);
                    if (queueingErrors.Value != null)
                    {
                        lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                        {
                            foreach (FileChange currentQueueingError in queueingErrors.Value)
                            {
                                FailedChangesQueue.Enqueue(currentQueueingError);

                                GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                            }
                        }
                    }
                    if (queueingError != null)
                    {
                        toReturn += new AggregateException("Error adding dependencies to processing queue after sync", queueingError.GrabExceptions());
                    }
                }
                catch (Exception ex)
                {
                    // on serious error in queueing changes,
                    // instead add them all to the failure queue add add the error to the returned aggregate errors
                    lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                    {
                        foreach (FileChange currentDependency in thingsThatWereDependenciesToQueue)
                        {
                            FailedChangesQueue.Enqueue(currentDependency);

                            GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                        }
                    }
                    toReturn += ex;
                }
            }

            // if there are any errors to queue,
            // then for any that are not also in the list of successful events, add then to the failure queue;
            // also add the FileStreams from the changes in error to the aggregated error output so they can be disposed;
            // add the errors themselves to the output as well
            if (errorsToQueue != null)
            {
                if (errorsToQueue.Count > 0)
                {
                    try
                    {
                        successfulEventIds.Sort();
                        lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                        {
                            foreach (KeyValuePair<FileChange, FileStream> errorToQueue in errorsToQueue)
                            {
                                try
                                {
                                    if (successfulEventIds.BinarySearch(errorToQueue.Key.EventId) < 0)
                                    {
                                        toReturn += errorToQueue.Value;
                                        FailedChangesQueue.Enqueue(errorToQueue.Key);

                                        GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    toReturn += ex;
                                    toReturn += errorToQueue.Value;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }
            }

            // if the output error aggregate contains any errors,
            // then add the sync status (to notify position in case of catastrophic failure),
            // dispose all FileStreams,
            // and log all errors
            if (toReturn != null)
            {
                toReturn.errorInfo.Add(CLError.ErrorInfo_Sync_Run_Status, syncStatus);
                CLError disposalError = toReturn.DequeueFileStreams().DisposeAllStreams();
                foreach (Exception disposalException in CLError.GrabExceptions(disposalError))
                {
                    toReturn += disposalException;
                }

                bool onlyErrorIsFileStream = true;
                foreach (Exception errorException in toReturn.GrabExceptions())
                {
                    if (errorException.Message != CLError.FileStreamFirstMessage)
                    {
                        onlyErrorIsFileStream = false;
                        break;
                    }
                }
                if (!onlyErrorIsFileStream)
                {
                    toReturn.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                }
            }

            // return error aggregate
            return toReturn;
        }

        #region subroutine calls of Sync Run
        private static Exception CompleteFileChange(KeyValuePair<FileChange, FileStream> toComplete, Func<FileChange, CLError> applySyncFromChange, Func<long, CLError> completeSingleEvent, ProcessingQueuesTimer failureTimer, out Nullable<long> immediateSuccessEventId, out Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask)
        {
            try
            {
                // Except for file uploads/downloads, complete the FileChange synhronously, otherwise queue them appropriately;
                // If it completes synchronously and successfully, set the immediateSuccessEventId to toComplete.EventId, in all other cases set to null;
                // If it is supposed to run asynchrounously, set asyncTask with the task to complete, otherwise set it to null

                if (toComplete.Key.Direction == SyncDirection.From)
                {
                    if (toComplete.Key.Metadata.HashableProperties.IsFolder
                        && toComplete.Key.Type == FileChangeType.Modified)
                    {
                        throw new ArgumentException("toComplete's FileChange cannot be a folder and have Modified for Type");
                    }
                    if (!toComplete.Key.Metadata.HashableProperties.IsFolder
                        && (toComplete.Key.Type == FileChangeType.Created
                            || toComplete.Key.Type == FileChangeType.Modified))
                    {
                        immediateSuccessEventId = null;
                        asyncTask = new KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>(SyncDirection.From,
                            new Task<KeyValuePair<long, Func<long, CLError>>>(downloadState =>
                                {
                                    ProcessingQueuesTimer storeFailureTimer = null;
                                    FileChange storeFileChange = null;
                                    try
                                    {
                                        DownloadTaskState castState = downloadState as DownloadTaskState;
                                        if (castState == null)
                                        {
                                            throw new NullReferenceException("Download Task downloadState not castable as DownloadTaskState");
                                        }
                                        
                                        // attempt to check these two references first so that the failure can be added to the queue if exceptions occur later:
                                        // the failure timer and the failed FileChange
                                        if (castState.FailureTimer == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain FailureTimer");
                                        }
                                        storeFailureTimer = castState.FailureTimer;
                                        if (castState.FileToDownload == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain FileToDownload");
                                        }
                                        storeFileChange = castState.FileToDownload;

                                        if (castState.TempDownloads == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain TempDownloads");
                                        }
                                        if (castState.GetTempFolder == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain GetTempFolder");
                                        }
                                        if (castState.ApplySyncFromChange == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain ApplySyncFromChange");
                                        }
                                        if (castState.CompleteSingleEvent == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain CompleteSingleEvent");
                                        }

                                        string requestBody;
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            DownloadSerializer.WriteObject(ms,
                                                new JsonContracts.Download()
                                                {
                                                    StorageKey = castState.FileToDownload.Metadata.StorageKey
                                                });
                                            requestBody = Encoding.Default.GetString(ms.ToArray());
                                        }

                                        byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                                        HttpWebRequest downloadRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathDownload);
                                        downloadRequest.Method = CLDefinitions.HeaderAppendMethod;
                                        downloadRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                                        // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                                        downloadRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                                        downloadRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                                        downloadRequest.SendChunked = false;
                                        downloadRequest.Timeout = HttpTimeoutMilliseconds;
                                        downloadRequest.ContentType = CLDefinitions.HeaderAppendContentType;
                                        downloadRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                                        downloadRequest.ContentLength = requestBodyBytes.Length;

                                        if (Settings.Instance.TraceEnabled)
                                        {
                                            Trace.LogCommunication(Settings.Instance.TraceLocation,
                                                Settings.Instance.Udid,
                                                Settings.Instance.Uuid,
                                                CommunicationEntryDirection.Request,
                                                CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathDownload,
                                                true,
                                                downloadRequest.Headers,
                                                requestBody,
                                                Settings.Instance.TraceExcludeAuthorization);
                                        }

                                        using (Stream downloadRequestStream = downloadRequest.GetRequestStream())
                                        {
                                            downloadRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                                        }

                                        HttpWebResponse downloadResponse = (HttpWebResponse)downloadRequest.GetResponse();

                                        string responseBody = null;
                                        try
                                        {
                                            if (downloadResponse.StatusCode != HttpStatusCode.OK)
                                            {
                                                try
                                                {
                                                    using (Stream downloadResponseStream = downloadResponse.GetResponseStream())
                                                    {
                                                        using (StreamReader downloadResponseStreamReader = new StreamReader(downloadResponseStream, Encoding.UTF8))
                                                        {
                                                            responseBody = downloadResponseStreamReader.ReadToEnd();
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                }

                                                throw new Exception("Invalid HTTP response status code in file download: " + ((int)downloadResponse.StatusCode).ToString() +
                                                    (responseBody == null ? string.Empty
                                                        : Environment.NewLine + "Response:" + Environment.NewLine +
                                                        responseBody));
                                            }
                                            else
                                            {
                                                responseBody = "---Incomplete file download---";

                                                Guid newTempFile = Guid.NewGuid();
                                                castState.TempDownloads.Add(newTempFile);
                                                string newTempFileString = castState.GetTempFolder() + "\\" + newTempFile.ToString("N");

                                                long fileLength = 0;

                                                using (Stream downloadResponseStream = downloadResponse.GetResponseStream())
                                                {
                                                    using (FileStream tempFileStream = new FileStream(newTempFileString, FileMode.Create, FileAccess.Write, FileShare.None))
                                                    {
                                                        byte[] data = new byte[CLDefinitions.SyncConstantsResponseBufferSize];
                                                        int read;
                                                        while ((read = downloadResponseStream.Read(data, 0, data.Length)) > 0)
                                                        {
                                                            tempFileStream.Write(data, 0, read);

                                                            fileLength += read;
                                                        }
                                                        tempFileStream.Flush();
                                                    }
                                                }

                                                File.SetCreationTimeUtc(newTempFileString, toComplete.Key.Metadata.HashableProperties.CreationTime);
                                                File.SetLastAccessTimeUtc(newTempFileString, toComplete.Key.Metadata.HashableProperties.LastTime);
                                                File.SetLastWriteTimeUtc(newTempFileString, toComplete.Key.Metadata.HashableProperties.LastTime);

                                                CLError applyError = applySyncFromChange(new FileChange()
                                                {
                                                    Direction = SyncDirection.From,
                                                    DoNotAddToSQLIndex = true,
                                                    Metadata = toComplete.Key.Metadata,
                                                    NewPath = toComplete.Key.NewPath,
                                                    OldPath = newTempFileString,
                                                    Type = FileChangeType.Renamed
                                                });
                                                if (applyError != null)
                                                {
                                                    try
                                                    {
                                                        File.Delete(newTempFileString);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        throw new AggregateException("applySyncFromChange returned error and failed to delete temp download file", new Exception[] { applyError.GrabFirstException(), ex });
                                                    }
                                                    throw applyError.GrabFirstException();
                                                }
                                                else
                                                {
                                                    castState.TempDownloads.Remove(newTempFile);
                                                    responseBody = "---Completed file download---";
                                                    return new KeyValuePair<long, Func<long, CLError>>(toComplete.Key.EventId, castState.CompleteSingleEvent);
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            if (Settings.Instance.TraceEnabled)
                                            {
                                                Trace.LogCommunication(Settings.Instance.TraceLocation,
                                                    Settings.Instance.Udid,
                                                    Settings.Instance.Uuid,
                                                    CommunicationEntryDirection.Response,
                                                    CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathDownload,
                                                    true,
                                                    downloadResponse.Headers,
                                                    responseBody,
                                                    Settings.Instance.TraceExcludeAuthorization);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExecutableException<KeyValuePair<FileChange, ProcessingQueuesTimer>> wrappedEx = new ExecutableException<KeyValuePair<FileChange, ProcessingQueuesTimer>>((exceptionState, exceptions) =>
                                            {
                                                try
                                                {
                                                    if (exceptionState.Key == null)
                                                    {
                                                        throw new NullReferenceException("exceptionState's FileChange cannot be null");
                                                    }
                                                    if (exceptionState.Value == null)
                                                    {
                                                        throw new NullReferenceException("exceptionState's ProcessingQueuesTimer cannot be null");
                                                    }

                                                    lock (exceptionState.Value.TimerRunningLocker)
                                                    {
                                                        FailedChangesQueue.Enqueue(exceptionState.Key);

                                                        exceptionState.Value.StartTimerIfNotRunning();
                                                    }
                                                }
                                                catch (Exception innerEx)
                                                {
                                                    ((CLError)innerEx).LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                                }
                                            },
                                            new KeyValuePair<FileChange, ProcessingQueuesTimer>(toComplete.Key, storeFailureTimer),
                                            "Error in download Task, see inner exception",
                                            ex);
                                        throw wrappedEx;
                                    }
                                },
                            new DownloadTaskState()
                            {
                                FileToDownload = toComplete.Key,
                                TempDownloads = TempDownloads,
                                GetTempFolder = () => TempDownloadsFolder,
                                ApplySyncFromChange = applySyncFromChange,
                                CompleteSingleEvent = completeSingleEvent,
                                FailureTimer = failureTimer
                            }));
                    }
                    else
                    {
                        CLError applyChangeError = applySyncFromChange(toComplete.Key);
                        if (applyChangeError != null)
                        {
                            throw applyChangeError.GrabFirstException();
                        }
                        immediateSuccessEventId = toComplete.Key.EventId;
                        asyncTask = null;
                    }
                }
                else if (toComplete.Key.Metadata.HashableProperties.IsFolder)
                {
                    throw new ArgumentException("toComplete's FileChange cannot represent a folder with direction Sync To");
                }
                else if (toComplete.Key.Type == FileChangeType.Deleted)
                {
                    throw new ArgumentException("toComplete's FileChange has no completion action for file deletion");
                }
                else if (toComplete.Key.Type == FileChangeType.Renamed)
                {
                    throw new ArgumentException("toComplete's FileChange has no completion action for file rename/move");
                }
                else
                {
                    throw new NotImplementedException("File upload not implemented");
                }
            }
            catch (Exception ex)
            {
                immediateSuccessEventId = Helpers.DefaultForType<Nullable<long>>();
                asyncTask = Helpers.DefaultForType<Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>>>();
                return ex;
            }
            return null;
        }
        private class DownloadTaskState
        {
            public FileChange FileToDownload { get; set; }
            public HashSet<Guid> TempDownloads { get; set; }
            public Func<string> GetTempFolder { get; set; }
            public Func<FileChange, CLError> ApplySyncFromChange { get; set; }
            public Func<long, CLError> CompleteSingleEvent { get; set; }
            public ProcessingQueuesTimer FailureTimer { get; set; }
        }

        private static Exception CommunicateWithServer(IEnumerable<KeyValuePair<FileChange, FileStream>> toCommunicate,
            Func<FileChange, FileChange, CLError> mergeToSql,
            Func<string> getLastSyncId,
            bool respondingToPushNotification,
            Func<string> getCloudRoot,
            out IEnumerable<KeyValuePair<bool, FileChange>> completedChanges,
            out IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> incompleteChanges,
            out IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>> changesInError,
            out string newSyncId)
        {
            try
            {
                KeyValuePair<FileChange, FileStream>[] communicationArray = (toCommunicate ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>()).ToArray();

                // if there is at least one change to communicate or we have a push notification to communcate anyways,
                // then process communication
                if (communicationArray.Length > 0)
                {
                    // Run Sync To with the list toCommunicate;
                    // Anything immediately completed should be output as completedChanges;
                    // Anything in conflict should be output as changesInError;
                    // Anything else should be output as incompleteChanges;
                    // Mark any new FileChange or any FileChange with altered metadata with a true for return boolean
                    //     (notifies calling method to run MergeToSQL with the updates)

                    Func<bool, FileChangeType, FilePath, string> getArgumentException = (isFolder, changeType, targetPath) =>
                        {
                            throw new ArgumentException("Invalid combination: " +
                                (isFolder ? "Folder" : (targetPath == null ? "File" : "Shortcut")) +
                                " " + changeType.ToString());
                        };

                    Func<long, long> ensureNonZeroEventId = toCheck =>
                        {
                            if (toCheck == 0)
                            {
                                throw new ArgumentException("Cannot communicate FileChange without EventId");
                            }
                            return toCheck;
                        };

                    long lastEventId = communicationArray.OrderByDescending(currentEvent => ensureNonZeroEventId(currentEvent.Key.EventId)).First().Key.EventId;

                    JsonContracts.To syncTo = new JsonContracts.To()
                    {
                        SyncId = getLastSyncId() ?? CLDefinitions.CLDefaultSyncID,
                        Events = communicationArray.Select(currentEvent => new JsonContracts.Event()
                        {
                            Action =
                                // Folder events
                                (currentEvent.Key.Metadata.HashableProperties.IsFolder
                                ? (currentEvent.Key.Type == FileChangeType.Created
                                    ? CLDefinitions.CLEventTypeAddFolder
                                    : (currentEvent.Key.Type == FileChangeType.Deleted
                                        ? CLDefinitions.CLEventTypeDeleteFolder
                                        : (currentEvent.Key.Type == FileChangeType.Modified
                                            ? getArgumentException(true, FileChangeType.Modified, null)
                                            : (currentEvent.Key.Type == FileChangeType.Renamed
                                                ? CLDefinitions.CLEventTypeRenameFolder
                                                : getArgumentException(true, currentEvent.Key.Type, null)))))

                                // File events
                                : (currentEvent.Key.Metadata.LinkTargetPath == null
                                    ? (currentEvent.Key.Type == FileChangeType.Created
                                        ? CLDefinitions.CLEventTypeAddFile
                                        : (currentEvent.Key.Type == FileChangeType.Deleted
                                            ? CLDefinitions.CLEventTypeDeleteFile
                                            : (currentEvent.Key.Type == FileChangeType.Modified
                                                ? CLDefinitions.CLEventTypeModifyFile
                                                : (currentEvent.Key.Type == FileChangeType.Renamed
                                                    ? CLDefinitions.CLEventTypeRenameFile
                                                    : getArgumentException(false, currentEvent.Key.Type, null)))))

                                    // Shortcut events
                                    : (currentEvent.Key.Type == FileChangeType.Created
                                        ? CLDefinitions.CLEventTypeAddLink
                                        : (currentEvent.Key.Type == FileChangeType.Deleted
                                            ? CLDefinitions.CLEventTypeDeleteLink
                                            : (currentEvent.Key.Type == FileChangeType.Modified
                                                ? CLDefinitions.CLEventTypeModifyLink
                                                : (currentEvent.Key.Type == FileChangeType.Renamed
                                                    ? CLDefinitions.CLEventTypeRenameLink
                                                    : getArgumentException(false, currentEvent.Key.Type, currentEvent.Key.Metadata.LinkTargetPath))))))),

                            EventId = currentEvent.Key.EventId,
                            Metadata = new JsonContracts.Metadata()
                            {
                                CreatedDate = currentEvent.Key.Metadata.HashableProperties.CreationTime,
                                Deleted = currentEvent.Key.Type == FileChangeType.Deleted,
                                Hash = ((Func<FileChange, string>)(innerEvent =>
                                {
                                    string currentEventMD5;
                                    CLError currentEventMD5Error = innerEvent.GetMD5LowercaseString(out currentEventMD5);
                                    if (currentEventMD5Error != null)
                                    {
                                        throw new AggregateException("Error retrieving currentEvent.GetMD5LowercaseString", currentEventMD5Error.GrabExceptions());
                                    }
                                    return currentEventMD5;
                                }))(currentEvent.Key),
                                IsFolder = currentEvent.Key.Metadata.HashableProperties.IsFolder,
                                LastEventId = lastEventId,
                                ModifiedDate = currentEvent.Key.Metadata.HashableProperties.LastTime,
                                RelativeFromPath = (currentEvent.Key.OldPath == null
                                    ? null
                                    : currentEvent.Key.OldPath.GetRelativePath(getCloudRoot(), true)),
                                RelativePath = currentEvent.Key.NewPath.GetRelativePath(getCloudRoot(), true),
                                RelativeToPath = currentEvent.Key.NewPath.GetRelativePath(getCloudRoot(), true),
                                Revision = currentEvent.Key.Metadata.Revision,
                                Size = currentEvent.Key.Metadata.HashableProperties.Size,
                                StorageKey = currentEvent.Key.Metadata.StorageKey,
                                Version = "1.0"
                            }
                        }).ToArray()
                    };

                    string requestBody;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ToSerializer.WriteObject(ms, syncTo);
                        requestBody = Encoding.Default.GetString(ms.ToArray());
                    }

                    byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                    HttpWebRequest toRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLMetaDataServerURL +
                        CLDefinitions.MethodPathSyncTo +
                        Helpers.QueryStringBuilder(new KeyValuePair<string, string>[]
                            {
                                new KeyValuePair<string, string>(CLDefinitions.QueryStringUserId, Settings.Instance.Uuid)
                            }
                        ));
                    toRequest.Method = CLDefinitions.HeaderAppendMethod;
                    toRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                    // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                    toRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                    toRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                    toRequest.SendChunked = false;
                    toRequest.Timeout = HttpTimeoutMilliseconds;
                    toRequest.ContentType = CLDefinitions.HeaderAppendContentType;
                    toRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                    toRequest.ContentLength = requestBodyBytes.Length;

                    if (Settings.Instance.TraceEnabled)
                    {
                        Trace.LogCommunication(Settings.Instance.TraceLocation,
                            Settings.Instance.Udid,
                            Settings.Instance.Uuid,
                            CommunicationEntryDirection.Request,
                            CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom,
                            true,
                            toRequest.Headers,
                            requestBody,
                            Settings.Instance.TraceExcludeAuthorization);
                    }

                    using (Stream toRequestStream = toRequest.GetRequestStream())
                    {
                        toRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                    }

                    HttpWebResponse toResponse = (HttpWebResponse)toRequest.GetResponse();

                    JsonContracts.To deserializedResponse;
                    using (Stream toHttpWebResponseStream = toResponse.GetResponseStream())
                    {
                        Stream toResponseStream = (Settings.Instance.TraceEnabled
                            ? Helpers.CopyHttpWebResponseStreamAndClose(toHttpWebResponseStream)
                            : toHttpWebResponseStream);

                        try
                        {
                            if (Settings.Instance.TraceEnabled)
                            {
                                Trace.LogCommunication(Settings.Instance.TraceLocation,
                                    Settings.Instance.Udid,
                                    Settings.Instance.Uuid,
                                    CommunicationEntryDirection.Response,
                                    CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncTo,
                                    true,
                                    toResponse.Headers,
                                    toResponseStream,
                                    Settings.Instance.TraceExcludeAuthorization);
                            }

                            if (toResponse.StatusCode != HttpStatusCode.OK)
                            {
                                string toResponseString = null;
                                // Bug in MDS: ContentLength is not set so I cannot read the stream to compare against it
                                try
                                {
                                    using (TextReader toResponseStreamReader = new StreamReader(toResponseStream, Encoding.UTF8))
                                    {
                                        toResponseString = toResponseStreamReader.ReadToEnd();
                                    }
                                }
                                catch
                                {
                                }

                                throw new Exception("Invalid HTTP response status code in Sync To: " + ((int)toResponse.StatusCode).ToString() +
                                    (toResponseString == null ? string.Empty
                                        : Environment.NewLine + "Response:" + Environment.NewLine +
                                        toResponseString));
                            }

                            deserializedResponse = (JsonContracts.To)ToSerializer.ReadObject(toResponseStream);
                        }
                        finally
                        {
                            if (Settings.Instance.TraceEnabled)
                            {
                                toResponseStream.Dispose();
                            }
                        }
                    }

                    if (deserializedResponse.Events == null)
                    {
                        throw new NullReferenceException("Invalid HTTP response body in Sync To, Events cannot be null");
                    }
                    if (deserializedResponse.SyncId == null)
                    {
                        throw new NullReferenceException("Invalid HTTP response body in Sync To, SyncId cannot be null");
                    }
                    newSyncId = deserializedResponse.SyncId;

                    Dictionary<long, KeyValuePair<bool, FileChange>[]> completedChangesList = new Dictionary<long, KeyValuePair<bool, FileChange>[]>();
                    Dictionary<long, List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>> incompleteChangesList = new Dictionary<long, List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                    Dictionary<long, KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]> changesInErrorList = new Dictionary<long,KeyValuePair<bool,KeyValuePair<FileChange,KeyValuePair<FileStream,Exception>>>[]>();
                    HashSet<FileStream> completedStreams = new HashSet<FileStream>();
                    foreach (JsonContracts.Event currentEvent in deserializedResponse.Events)
                    {
                        FileChangeWithDependencies currentChange = null;
                        FileStream currentStream = null;
                        try
                        {
                            currentChange = CreateFileChangeFromBaseChangePlusHash(new FileChange()
                                {
                                    Direction = (string.IsNullOrEmpty(currentEvent.Header.Status) ? SyncDirection.From : SyncDirection.To),
                                    EventId = currentEvent.Header.EventId ?? 0,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentEvent.Metadata.IsFolder ?? false,
                                            currentEvent.Metadata.ModifiedDate,
                                            currentEvent.Metadata.CreatedDate,
                                            currentEvent.Metadata.Size),
                                        LinkTargetPath = currentEvent.Metadata.TargetPath,
                                        Revision = currentEvent.Metadata.Revision,
                                        StorageKey = currentEvent.Metadata.StorageKey
                                    },
                                    NewPath = getCloudRoot() + "\\" + (currentEvent.Metadata.RelativePath ?? currentEvent.Metadata.RelativeToPath).Replace('/', '\\'),
                                    OldPath = (currentEvent.Metadata.RelativeFromPath == null
                                        ? null
                                        : getCloudRoot() + "\\" + currentEvent.Metadata.RelativeFromPath.Replace('/', '\\')),
                                    Type = ParseEventStringToType(currentEvent.Header.Action ?? currentEvent.Action)
                                },
                                currentEvent.Metadata.Hash);

                            Nullable<KeyValuePair<FileChange, FileStream>> matchedChange = (currentChange.EventId == 0
                                ? (Nullable<KeyValuePair<FileChange, FileStream>>)null
                                : toCommunicate.FirstOrDefault(currentToCommunicate => currentToCommunicate.Key.EventId == currentChange.EventId));
                            if (matchedChange != null)
                            {
                                currentStream = ((KeyValuePair<FileChange, FileStream>)matchedChange).Value;
                            }

                            Func<FileChange, FileChange, bool> MD5sChanged = (firstChange, secondChange) =>
                                {
                                    byte[] firstMD5;
                                    CLError firstChangeMD5Error = firstChange.GetMD5Bytes(out firstMD5);
                                    if (firstChangeMD5Error != null)
                                    {
                                        throw new AggregateException("Error retrieving MD5 from firstChange", firstChangeMD5Error.GrabExceptions());
                                    }
                                    byte[] secondMD5;
                                    CLError secondChangeMD5Error = secondChange.GetMD5Bytes(out secondMD5);
                                    if (secondChangeMD5Error != null)
                                    {
                                        throw new AggregateException("Error retrieving MD5 from secondChange", secondChangeMD5Error.GrabExceptions());
                                    }

                                    return !((firstMD5 == null && secondMD5 == null)
                                        || (firstMD5 != null && secondMD5 != null && firstMD5.Length == secondMD5.Length && MonitorAgent.memcmp(firstMD5, secondMD5, new UIntPtr((uint)firstMD5.Length)) == 0));
                                };

                            bool metadataIsDifferent = (matchedChange == null)
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Direction != currentChange.Direction
                                || MD5sChanged(((KeyValuePair<FileChange, FileStream>)matchedChange).Key, currentChange)
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.CreationTime.CompareTo(currentChange.Metadata.HashableProperties.CreationTime) != 0
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.IsFolder != currentChange.Metadata.HashableProperties.IsFolder
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.LastTime.CompareTo(currentChange.Metadata.HashableProperties.LastTime) != 0
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.Size != currentChange.Metadata.HashableProperties.Size
                                || !((((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.LinkTargetPath == null && currentChange.Metadata.LinkTargetPath == null)
                                    || (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.LinkTargetPath != null && currentChange.Metadata.LinkTargetPath != null && FilePathComparer.Instance.Equals(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.LinkTargetPath, currentChange.Metadata.LinkTargetPath)))
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.Revision != currentChange.Metadata.Revision
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.StorageKey != currentChange.Metadata.StorageKey
                                || !((((KeyValuePair<FileChange, FileStream>)matchedChange).Key.NewPath == null && currentChange.NewPath == null)
                                    || (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.NewPath != null && currentChange.NewPath != null && FilePathComparer.Instance.Equals(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.NewPath, currentChange.NewPath)))
                                || !((((KeyValuePair<FileChange, FileStream>)matchedChange).Key.OldPath == null && currentChange.OldPath == null)
                                    || (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.OldPath != null && currentChange.OldPath != null && FilePathComparer.Instance.Equals(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.OldPath, currentChange.OldPath)))
                                || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Type != currentChange.Type;
                            
                            FileChangeWithDependencies castMatchedChange;
                            if (metadataIsDifferent)
                            {
                                if (matchedChange != null
                                    && (castMatchedChange = ((KeyValuePair<FileChange, FileStream>)matchedChange).Key as FileChangeWithDependencies) != null)
                                {
                                    foreach (FileChange matchedDependency in castMatchedChange.Dependencies)
                                    {
                                        currentChange.AddDependency(matchedDependency);
                                    }
                                }
                            }
                            else if ((castMatchedChange = ((KeyValuePair<FileChange, FileStream>)matchedChange).Key as FileChangeWithDependencies) != null)
                            {
                                currentChange = castMatchedChange;
                            }
                            else
                            {
                                CLError convertMatchedChangeError = FileChangeWithDependencies.CreateAndInitialize(((KeyValuePair<FileChange, FileStream>)matchedChange).Key, null, out currentChange);
                                if (convertMatchedChangeError != null)
                                {
                                    throw new AggregateException("Error converting matchedChange to FileChangeWithDependencies", convertMatchedChangeError.GrabExceptions());
                                }
                            }

                            Action addToIncompleteChanges = () =>
                                {
                                    KeyValuePair<bool, KeyValuePair<FileChange, FileStream>> addChange = new KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>(metadataIsDifferent,
                                            new KeyValuePair<FileChange, FileStream>(currentChange,
                                                currentStream));
                                    if (incompleteChangesList.ContainsKey(currentChange.EventId))
                                    {
                                        incompleteChangesList[currentChange.EventId].Add(addChange);
                                    }
                                    else
                                    {
                                        incompleteChangesList.Add(currentChange.EventId,
                                            new List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>(new KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>[]
                                                {
                                                    addChange
                                                }));
                                    }
                                };

                            switch (currentChange.Direction)
                            {
                                case SyncDirection.From:
                                    addToIncompleteChanges();
                                    break;
                                case SyncDirection.To:
                                    switch (currentEvent.Header.Status)
                                    {
                                        case CLDefinitions.CLEventTypeUpload:
                                        case CLDefinitions.CLEventTypeUploading:
                                            addToIncompleteChanges();
                                            break;
                                        case CLDefinitions.CLEventTypeAccepted:
                                        case CLDefinitions.CLEventTypeExists:
                                        case CLDefinitions.CLEventTypeDuplicate:
                                            KeyValuePair<bool, FileChange> addCompletedChange = new KeyValuePair<bool, FileChange>(metadataIsDifferent,
                                                currentChange);
                                            if (currentStream != null)
                                            {
                                                completedStreams.Add(currentStream);
                                                try
                                                {
                                                    currentStream.Dispose();
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            if (completedChangesList.ContainsKey(currentChange.EventId))
                                            {
                                                KeyValuePair<bool, FileChange>[] previousCompleted = completedChangesList[currentChange.EventId];
                                                KeyValuePair<bool, FileChange>[] newCompleted = new KeyValuePair<bool, FileChange>[previousCompleted.Length + 1];
                                                previousCompleted.CopyTo(newCompleted, 0);
                                                newCompleted[previousCompleted.Length] = addCompletedChange;
                                                completedChangesList[currentChange.EventId] = newCompleted;
                                            }
                                            else
                                            {
                                                completedChangesList.Add(currentChange.EventId,
                                                    new KeyValuePair<bool, FileChange>[]
                                                    {
                                                        addCompletedChange
                                                    });
                                            }
                                            break;
                                        case CLDefinitions.CLEventTypeConflict:
                                            KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>> addErrorChange = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>(metadataIsDifferent,
                                                new KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>(currentChange,
                                                    new KeyValuePair<FileStream, Exception>(currentStream,
                                                        new Exception(CLDefinitions.CLEventTypeConflict + " " +
                                                            (currentEvent.Header.Action ?? currentEvent.Action) +
                                                            " " + currentChange.EventId + " " + currentChange.NewPath.ToString()))));
                                            if (changesInErrorList.ContainsKey(currentChange.EventId))
                                            {
                                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] previousErrors = changesInErrorList[currentChange.EventId];
                                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] newErrors = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[previousErrors.Length + 1];
                                                previousErrors.CopyTo(newErrors, 0);
                                                newErrors[previousErrors.Length] = addErrorChange;
                                                changesInErrorList[currentChange.EventId] = newErrors;
                                            }
                                            else
                                            {
                                                changesInErrorList.Add(currentChange.EventId,
                                                    new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]
                                                    {
                                                        addErrorChange
                                                    });
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException("Uknown SyncHeader Status: " + currentEvent.Header.Status);
                                    }
                                    break;
                                default:
                                    throw new ArgumentException("Uknown SyncDirection in currentChange: " + currentChange.Direction.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>> addErrorChange = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>(currentChange != null,
                                new KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>(currentChange,
                                    new KeyValuePair<FileStream, Exception>(currentStream, ex)));
                            if (changesInErrorList.ContainsKey(currentChange == null ? 0 : currentChange.EventId))
                            {
                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] previousErrors = changesInErrorList[currentChange == null ? 0 : currentChange.EventId];
                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] newErrors = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[previousErrors.Length + 1];
                                previousErrors.CopyTo(newErrors, 0);
                                newErrors[previousErrors.Length] = addErrorChange;
                                changesInErrorList[currentChange.EventId] = newErrors;
                            }
                            else
                            {
                                changesInErrorList.Add(currentChange == null ? 0 : currentChange.EventId,
                                    new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]
                                        {
                                            addErrorChange
                                        });
                            }
                        }
                    }

                    foreach (KeyValuePair<FileChange, FileStream> currentOriginalChangeToFind in communicationArray)
                    {
                        bool foundEventId = false;
                        bool foundMatchedStream = currentOriginalChangeToFind.Value == null;
                        List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> tryGetIncompletes;
                        if (incompleteChangesList.TryGetValue(currentOriginalChangeToFind.Key.EventId, out tryGetIncompletes))
                        {
                            foundEventId = true;
                            if (!foundMatchedStream)
                            {
                                if (tryGetIncompletes.Any(currentIncomplete => currentIncomplete.Value.Value == currentOriginalChangeToFind.Value))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                        }
                        KeyValuePair<bool, FileChange>[] tryGetCompletes;
                        if ((!foundEventId && !foundMatchedStream)
                            && completedChangesList.TryGetValue(currentOriginalChangeToFind.Key.EventId, out tryGetCompletes))
                        {
                            foundEventId = true;
                            if (!foundMatchedStream)
                            {
                                if (completedStreams.Contains(currentOriginalChangeToFind.Value))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                        }
                        KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] tryGetErrors;
                        if ((!foundEventId && !foundMatchedStream)
                            && changesInErrorList.TryGetValue(currentOriginalChangeToFind.Key.EventId, out tryGetErrors))
                        {
                            foundEventId = true;
                            if (!foundMatchedStream)
                            {
                                if (tryGetErrors.Any(currentError => currentError.Value.Value.Key == currentOriginalChangeToFind.Value))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                        }

                        Nullable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>> missingStream = null;
                        if (!foundEventId)
                        {
                            missingStream = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>(false,
                                    new KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>(currentOriginalChangeToFind.Key,
                                        new KeyValuePair<FileStream, Exception>(currentOriginalChangeToFind.Value,
                                            new Exception("Found unmatched FileChange in communicationArray in output lists"))));
                        }
                        else if (!foundMatchedStream)
                        {
                            missingStream = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>(false,
                                    new KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>(null, // do not copy FileChange since it already exists in a list
                                        new KeyValuePair<FileStream, Exception>(currentOriginalChangeToFind.Value,
                                            new Exception("Found unmatched FileStream in communicationArray in output lists"))));
                        }
                        if (missingStream != null)
                        {
                            if (changesInErrorList.ContainsKey(currentOriginalChangeToFind.Key.EventId))
                            {
                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] previousErrors = changesInErrorList[currentOriginalChangeToFind.Key.EventId];
                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] newErrors = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[previousErrors.Length + 1];
                                previousErrors.CopyTo(newErrors, 0);
                                newErrors[previousErrors.Length] = (KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>)missingStream;
                                changesInErrorList[currentOriginalChangeToFind.Key.EventId] = newErrors;
                            }
                            else
                            {
                                changesInErrorList.Add(currentOriginalChangeToFind.Key.EventId,
                                    new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]
                                        {
                                            (KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>)missingStream
                                        });
                            }
                        }
                    }

                    completedChanges = completedChangesList.SelectMany(currentCompleted =>
                        currentCompleted.Value);
                    incompleteChanges = incompleteChangesList.SelectMany(currentIncomplete =>
                        currentIncomplete.Value);
                    changesInError = changesInErrorList.SelectMany(currentError =>
                        currentError.Value);
                }
                else
                {
                    if (respondingToPushNotification)
                    {
                        // Run Sync From
                        // Any events should be output as incompleteChanges, return all with the true boolean
                        //     (notifies calling method to run MergeToSQL with the updates)
                        // Should not give any errors/conflicts
                        // Should not give any completed changes

                        string requestBody;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            PushSerializer.WriteObject(ms,
                                new JsonContracts.Push()
                                {
                                    LastSyncId = getLastSyncId() ?? CLDefinitions.CLDefaultSyncID
                                });
                            requestBody = Encoding.Default.GetString(ms.ToArray());
                        }

                        byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                        HttpWebRequest pushRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom);
                        pushRequest.Method = CLDefinitions.HeaderAppendMethod;
                        pushRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                        // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                        pushRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                        pushRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                        pushRequest.SendChunked = false;
                        pushRequest.Timeout = HttpTimeoutMilliseconds;
                        pushRequest.ContentType = CLDefinitions.HeaderAppendContentType;
                        pushRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                        pushRequest.ContentLength = requestBodyBytes.Length;

                        if (Settings.Instance.TraceEnabled)
                        {
                            Trace.LogCommunication(Settings.Instance.TraceLocation,
                                Settings.Instance.Udid,
                                Settings.Instance.Uuid,
                                CommunicationEntryDirection.Request,
                                CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom,
                                true,
                                pushRequest.Headers,
                                requestBody,
                                Settings.Instance.TraceExcludeAuthorization);
                        }

                        using (Stream pushRequestStream = pushRequest.GetRequestStream())
                        {
                            pushRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                        }

                        HttpWebResponse pushResponse = (HttpWebResponse)pushRequest.GetResponse();

                        JsonContracts.PushResponse deserializedResponse;
                        using (Stream pushHttpWebResponseStream = pushResponse.GetResponseStream())
                        {
                            Stream pushResponseStream = (Settings.Instance.TraceEnabled
                                ? Helpers.CopyHttpWebResponseStreamAndClose(pushHttpWebResponseStream)
                                : pushHttpWebResponseStream);

                            try
                            {
                                if (Settings.Instance.TraceEnabled)
                                {
                                    Trace.LogCommunication(Settings.Instance.TraceLocation,
                                        Settings.Instance.Udid,
                                        Settings.Instance.Uuid,
                                        CommunicationEntryDirection.Response,
                                        CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom,
                                        true,
                                        pushResponse.Headers,
                                        pushResponseStream,
                                        Settings.Instance.TraceExcludeAuthorization);
                                }

                                if (pushResponse.StatusCode != HttpStatusCode.OK)
                                {
                                    string pushResponseString = null;
                                    // Bug in MDS: ContentLength is not set so I cannot read the stream to compare against it
                                    try
                                    {
                                        using (TextReader pushResponseStreamReader = new StreamReader(pushResponseStream, Encoding.UTF8))
                                        {
                                            pushResponseString = pushResponseStreamReader.ReadToEnd();
                                        }
                                    }
                                    catch
                                    {
                                    }

                                    throw new Exception("Invalid HTTP response status code in Sync From: " + ((int)pushResponse.StatusCode).ToString() +
                                        (pushResponseString == null ? string.Empty
                                            : Environment.NewLine + "Response:" + Environment.NewLine +
                                            pushResponseString));
                                }

                                deserializedResponse = (JsonContracts.PushResponse)PushResponseSerializer.ReadObject(pushResponseStream);
                            }
                            finally
                            {
                                if (Settings.Instance.TraceEnabled)
                                {
                                    pushResponseStream.Dispose();
                                }
                            }
                        }

                        newSyncId = deserializedResponse.SyncId;
                        incompleteChanges = deserializedResponse.Events.Select(currentEvent => new KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>(/* needs to update SQL */ true, new KeyValuePair<FileChange, FileStream>(CreateFileChangeFromBaseChangePlusHash(new FileChange()
                        {
                            Direction = SyncDirection.From,
                            NewPath = getCloudRoot() + "\\" + (currentEvent.Metadata.RelativePath ?? currentEvent.Metadata.RelativeToPath).Replace('/', '\\'),
                            OldPath = (currentEvent.Metadata.RelativeFromPath == null
                                ? null
                                : getCloudRoot() + "\\" + currentEvent.Metadata.RelativeFromPath.Replace('/', '\\')),
                            Type = ParseEventStringToType(currentEvent.Action ?? currentEvent.Header.Action),
                            Metadata = new FileMetadata()
                            {
                                //Need to find what key this is //LinkTargetPath
                                HashableProperties = new FileMetadataHashableProperties((currentEvent.Metadata.IsFolder ?? false),
                                    currentEvent.Metadata.ModifiedDate,
                                    currentEvent.Metadata.CreatedDate,
                                    currentEvent.Metadata.Size),
                                Revision = currentEvent.Metadata.Revision,
                                StorageKey = currentEvent.Metadata.StorageKey
                            }
                        },
                        currentEvent.Metadata.Hash), null)))
                        .ToArray();
                    }
                    // else if there are no changes to communicate and we're not responding to a push notification,
                    // do not process any communication (instead set all outputs to empty arrays)
                    else
                    {
                        incompleteChanges = new KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>[0];
                        newSyncId = null;
                    }
                    completedChanges = Enumerable.Empty<KeyValuePair<bool, FileChange>>();
                    changesInError = Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>();
                }
            }
            catch (Exception ex)
            {
                completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                newSyncId = Helpers.DefaultForType<string>();
                return ex;
            }
            return null;
        }

        private static FileChangeWithDependencies CreateFileChangeFromBaseChangePlusHash(FileChange baseChange, string hashString)
        {
            if (!string.IsNullOrWhiteSpace(hashString))
            {
                char[] hexChars;
                if (hashString.Length % 2 == 1)
                {
                    hexChars = new char[hashString.Length + 1];
                    hexChars[0] = '0';
                    hashString.ToCharArray().CopyTo(hexChars, 1);
                }
                else
                {
                    hexChars = hashString.ToCharArray();
                }

                int hexCharLength = hexChars.Length;
                byte[] hexBuffer = new byte[hexCharLength / 2 + hexCharLength % 2];

                int hexBufferIndex = 0;
                for (int charIndex = 0; charIndex < hexCharLength - 1; charIndex += 2)
                {
                    hexBuffer[hexBufferIndex] = byte.Parse(hexChars[charIndex].ToString(),
                        System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture);
                    hexBuffer[hexBufferIndex] <<= 4;
                    hexBuffer[hexBufferIndex] += byte.Parse(hexChars[charIndex + 1].ToString(),
                        System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture);
                    hexBufferIndex++;
                }

                baseChange.SetMD5(hexBuffer);
            }
            FileChangeWithDependencies returnedChange;
            CLError changeConversionError = FileChangeWithDependencies.CreateAndInitialize(baseChange, null, out returnedChange);
            if (changeConversionError != null)
            {
                throw new AggregateException("Error converting baseChange to a FileChangeWithDependencies", changeConversionError.GrabExceptions());
            }
            return returnedChange;
        }

        private static FileChangeType ParseEventStringToType(string actionString)
        {
            if (CLDefinitions.SyncHeaderCreations.Contains(actionString))
            {
                return FileChangeType.Created;
            }
            if (CLDefinitions.SyncHeaderDeletions.Contains(actionString))
            {
                return FileChangeType.Deleted;
            }
            if (CLDefinitions.SyncHeaderModifications.Contains(actionString))
            {
                return FileChangeType.Modified;
            }
            if (CLDefinitions.SyncHeaderRenames.Contains(actionString))
            {
                return FileChangeType.Renamed;
            }
            throw new ArgumentException("eventString was not parsable to FileChangeType: " + actionString);
        }

        private static DataContractJsonSerializer PushSerializer
        {
            get
            {
                lock (PushSerializerLocker)
                {
                    return _pushSerializer
                        ?? (_pushSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Push)));
                }
            }
        }
        private static DataContractJsonSerializer _pushSerializer = null;
        private static readonly object PushSerializerLocker = new object();
        private static DataContractJsonSerializer PushResponseSerializer
        {
            get
            {
                lock (PushResponseSerializerLocker)
                {
                    return _pushResponseSerializer
                        ?? (_pushResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PushResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _pushResponseSerializer = null;
        private static readonly object PushResponseSerializerLocker = new object();
        private static DataContractJsonSerializer DownloadSerializer
        {
            get
            {
                lock (DownloadSerializerLocker)
                {
                    return _downloadSerializer
                        ?? (_downloadSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Download)));
                }
            }
        }
        private static DataContractJsonSerializer _downloadSerializer = null;
        private static readonly object DownloadSerializerLocker = new object();
        private static DataContractJsonSerializer ToSerializer
        {
            get
            {
                lock (ToSerializerLocker)
                {
                    return _toSerializer
                        ?? (_toSerializer = new DataContractJsonSerializer(typeof(JsonContracts.To)));
                }
            }
        }
        private static DataContractJsonSerializer _toSerializer = null;
        private static readonly object ToSerializerLocker = new object();
        private static DataContractJsonSerializer EventSerializer
        {
            get
            {
                lock (EventSerializerLocker)
                {
                    return _eventSerializer
                        ?? (_eventSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Event)));
                }
            }
        }
        private static DataContractJsonSerializer _eventSerializer = null;
        private static readonly object EventSerializerLocker = new object();

        #endregion
    }
}