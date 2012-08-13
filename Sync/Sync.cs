//
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
                    for (int initialErrorIndex = 0; initialErrorIndex < FailedChangesQueue.Count; initialErrorIndex++)
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
                IEnumerable<KeyValuePair<bool, FileChange>> changesInError;
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
                        .Concat(changesInError ?? Enumerable.Empty<KeyValuePair<bool, FileChange>>()))
                    {
                        if (currentStoreUpdate.Key)
                        {
                            mergeToSql(currentStoreUpdate.Value, null);
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
                                dequeuedFailures.Concat((changesInError ?? Enumerable.Empty<KeyValuePair<bool, FileChange>>())
                                    .Select(currentChangeInError => currentChangeInError.Value)),
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

                                        using (Stream downloadRequestStream = downloadRequest.GetRequestStream())
                                        {
                                            downloadRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                                        }

                                        HttpWebResponse downloadResponse = (HttpWebResponse)downloadRequest.GetResponse();

                                        if (downloadResponse.StatusCode != HttpStatusCode.OK)
                                        {
                                            string downloadResponseString = null;
                                            try
                                            {
                                                using (Stream downloadResponseStream = downloadResponse.GetResponseStream())
                                                {
                                                    using (StreamReader downloadResponseStreamReader = new StreamReader(downloadResponseStream, Encoding.UTF8))
                                                    {
                                                        downloadResponseString = downloadResponseStreamReader.ReadToEnd();
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                            }

                                            throw new Exception("Invalid HTTP response status code in file download: " + ((int)downloadResponse.StatusCode).ToString() +
                                                (downloadResponseString == null ? string.Empty
                                                    : Environment.NewLine + "Response:" + Environment.NewLine +
                                                    downloadResponseString));
                                        }
                                        else
                                        {
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
                                                return new KeyValuePair<long,Func<long,CLError>>(toComplete.Key.EventId, castState.CompleteSingleEvent);
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
            out IEnumerable<KeyValuePair<bool, FileChange>> changesInError,
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

                    throw new NotImplementedException("Sync To not implemented");
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
                                    RelativeRootPath = getLastSyncId()
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

                        using (Stream pushRequestStream = pushRequest.GetRequestStream())
                        {
                            pushRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                        }

                        HttpWebResponse pushResponse = (HttpWebResponse)pushRequest.GetResponse();

                        if (pushResponse.StatusCode != HttpStatusCode.OK)
                        {
                            string pushResponseString = null;
                            // Bug in MDS: ContentLength is not set so I cannot read the stream to compare against it
                            try
                            {
                                using (Stream pushResponseStream = pushResponse.GetResponseStream())
                                {
                                    using (StreamReader pushResponseStreamReader = new StreamReader(pushResponseStream, Encoding.UTF8))
                                    {
                                        pushResponseString = pushResponseStreamReader.ReadToEnd();
                                    }
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

                        JsonContracts.PushResponse deserializedResponse;
                        using (Stream pushResponseStream = pushResponse.GetResponseStream())
                        {
                            deserializedResponse = (JsonContracts.PushResponse)PushResponseSerializer.ReadObject(pushResponseStream);
                        }

                        newSyncId = deserializedResponse.SyncId;
                        Func<FileChange, string, FileChangeWithDependencies> createFileChange = (baseChange, hashString) =>
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
                            };
                        Func<string, FileChangeType> parseEvent = eventString =>
                            {
                                if (CLDefinitions.SyncHeaderCreations.Contains(eventString))
                                {
                                    return FileChangeType.Created;
                                }
                                if (CLDefinitions.SyncHeaderDeletions.Contains(eventString))
                                {
                                    return FileChangeType.Deleted;
                                }
                                if (CLDefinitions.SyncHeaderModifications.Contains(eventString))
                                {
                                    return FileChangeType.Modified;
                                }
                                if (CLDefinitions.SyncHeaderRenames.Contains(eventString))
                                {
                                    return FileChangeType.Renamed;
                                }
                                throw new ArgumentException("eventString was not parsable to FileChangeType: " + eventString);
                            };
                        incompleteChanges = deserializedResponse.Events.Select(currentEvent => new KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>(/* needs to update SQL */ true, new KeyValuePair<FileChange, FileStream>(createFileChange(new FileChange()
                        {
                            Direction = SyncDirection.From,
                            NewPath = getCloudRoot() + "\\" + currentEvent.Metadata.RelativePath.Replace('/', '\\'),
                            OldPath = (currentEvent.Metadata.RelativeFromPath == null
                                ? null
                                : getCloudRoot() + "\\" + currentEvent.Metadata.RelativeFromPath.Replace('/', '\\')),
                            Type = parseEvent(currentEvent.Action ?? currentEvent.Header.Event),
                            Metadata = new FileMetadata()
                            {
                                //Need to find what key this is //LinkTargetPath
                                HashableProperties = new FileMetadataHashableProperties(currentEvent.Metadata.IsFolder,
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
                    completedChanges = new KeyValuePair<bool, FileChange>[0];
                    changesInError = new KeyValuePair<bool, FileChange>[0];
                }
            }
            catch (Exception ex)
            {
                completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                newSyncId = Helpers.DefaultForType<string>();
                return ex;
            }
            return null;
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

        #endregion
    }
}