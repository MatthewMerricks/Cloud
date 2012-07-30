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
using System.Runtime.CompilerServices;
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
            Func<string, IEnumerable<long>, string, CLError> completeSyncSql)
        {
            CLError toReturn = null;
            string syncStatus = "Sync Run entered";
            // errorsToQueue will have all changes to process,
            // items will be ignored as their successfulEventId is added
            List<KeyValuePair<FileChange, FileStream>> errorsToQueue = null;
            List<long> successfulEventIds = new List<long>();
            bool needToStartFailureQueueTimer = false;
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

                // Grab processed changes (will open locked FileStreams for all file adds/modifies), grabs MD5s and updates metadata
                KeyValuePair<FileChange, FileStream>[] outputChanges;
                FileChange[] outputChangesInError;
                grabChangesFromFileSystemMonitor(toReturn,
                    out outputChanges,
                    out outputChangesInError);

                // set errors to queue here with all processed changes so they can be added to failure queue on exception
                errorsToQueue = new List<KeyValuePair<FileChange, FileStream>>(outputChanges);

                syncStatus = "Sync Run grabbed processed changes";

                // Define changes to set after dependency calculations
                // (will be top level a.k.a. not have any dependencies)
                IEnumerable<KeyValuePair<FileChange, FileStream>> topLevelChanges;

                // Within a lock on the failure queue (failureTimer.TimerRunningLocker),
                // check if each current event needs to be moved to a dependency under a failure event or an event in the current batch
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
                        AssignDependencies(outputChanges,
                            dequeuedFailures.Concat(
                                outputChangesInError),
                            out topLevelChanges,
                            out topLevelErrors);
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
                    // replace errors queue with dependency-assigned values
                    errorsToQueue = new List<KeyValuePair<FileChange, FileStream>>(topLevelChanges);
                    foreach (FileChange topLevelError in topLevelErrors)
                    {
                        FailedChangesQueue.Enqueue(topLevelError);

                        // do not need to start failure queue timer
                        // since these original errors all came from that queue which must already have its timer running
                    }
                }

                syncStatus = "Sync Run initial dependencies calculated";

                // Synchronously or asynchronously fire off all events without dependencies that have a storage key (MDS events);
                // leave changes that did not complete in the errorsToQueue list so they will be added to the failure queue later
                List<KeyValuePair<FileChange, FileStream>> preprocessedEvents = new List<KeyValuePair<FileChange, FileStream>>();
                foreach (KeyValuePair<FileChange, FileStream> topLevelChange in topLevelChanges)
                {
                    if (topLevelChange.Key.Metadata != null
                        && !string.IsNullOrWhiteSpace(topLevelChange.Key.Metadata.StorageKey))
                    {
                        preprocessedEvents.Add(topLevelChange);
                        Nullable<long> successfulEventId;
                        Nullable<KeyValuePair<SyncDirection, Task<long>>> asyncTask;
                        Exception completionException = CompleteFileChange(topLevelChange, out successfulEventId, out asyncTask);
                        if (successfulEventId != null
                            && (long)successfulEventId > 0)
                        {
                            successfulEventIds.Add((long)successfulEventId);

                            FileChangeWithDependencies changeWithDependencies = topLevelChange.Key as FileChangeWithDependencies;
                            if (changeWithDependencies != null)
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
                                KeyValuePair<SyncDirection, Task<long>> nonNullTask = (KeyValuePair<SyncDirection, Task<long>>)asyncTask;

                                nonNullTask.Value.Start(HttpScheduler.GetSchedulerByDirection(nonNullTask.Key));
                            }
                        }
                    }
                }

                syncStatus = "Sync Run initial operations completed synchronously or queued";

                // Take events without dependencies that were not fired off in order to perform communication (or Sync From for no events left)
                IEnumerable<KeyValuePair<bool, FileChange>> completedChanges;
                IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> incompleteChanges;
                IEnumerable<KeyValuePair<bool, FileChange>> changesInError;
                string newSyncId;
                Exception communicationException = CommunicateWithServer(topLevelChanges.Except(
                        preprocessedEvents),
                    mergeToSql,
                    respondingToPushNotification,
                    out completedChanges,
                    out incompleteChanges,
                    out changesInError,
                    out newSyncId);
                if (communicationException != null)
                {
                    toReturn += communicationException;
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

                            AssignDependencies(toCheck: (incompleteChanges ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>())
                                    .Select(currentIncompleteChange => currentIncompleteChange.Value),

                                alreadyInError: dequeuedFailures

                                    .Concat(preprocessedEvents
                                        .Intersect(errorsToQueue ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>())
                                        .Select(currentPreprocessError => currentPreprocessError.Key)
                                        .Where(currentPreprocessError => successfulEventIds.BinarySearch(currentPreprocessError.EventId) < 0))

                                    .Concat((changesInError ?? Enumerable.Empty<KeyValuePair<bool, FileChange>>())
                                        .Select(currentChangeInError => currentChangeInError.Value)),

                                outputTopLevels: out topLevelChanges,
                                outputTopErrors: out topLevelErrors);
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

                        // replace errors queue with dependency-assigned values
                        errorsToQueue = new List<KeyValuePair<FileChange, FileStream>>(topLevelChanges);
                        foreach (FileChange topLevelError in topLevelErrors)
                        {
                            FailedChangesQueue.Enqueue(topLevelError);

                            // in case the failure queue didn't have any items in it before,
                            // mark that its timer may need to be started
                            needToStartFailureQueueTimer = true;
                        }
                    }

                    syncStatus = "Sync Run post-communication dependencies calculated";

                    List<KeyValuePair<SyncDirection, Task<long>>> asyncTasksToRun = new List<KeyValuePair<SyncDirection, Task<long>>>();

                    // Synchronously complete all local operations without dependencies (exclude file upload/download) and record successful events;
                    // If a completed event has dependencies, stick them on the end of the current batch;
                    // If an event fails to complete, leave it on errorsToQueue so it will be added to the failure queue later
                    foreach (KeyValuePair<FileChange, FileStream> topLevelChange in topLevelChanges)
                    {
                        Nullable<long> successfulEventId;
                        Nullable<KeyValuePair<SyncDirection, Task<long>>> asyncTask;
                        Exception completionException = CompleteFileChange(topLevelChange, out successfulEventId, out asyncTask);
                        if (successfulEventId != null
                            && (long)successfulEventId > 0)
                        {
                            successfulEventIds.Add((long)successfulEventId);

                            FileChangeWithDependencies changeWithDependencies = topLevelChange.Key as FileChangeWithDependencies;
                            if (changeWithDependencies != null)
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
                                asyncTasksToRun.Add((KeyValuePair<SyncDirection, Task<long>>)asyncTask);
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
                    foreach (KeyValuePair<SyncDirection, Task<long>> asyncTask in asyncTasksToRun)
                    {
                        try
                        {
                            asyncTask.Value.Start(HttpScheduler.GetSchedulerByDirection(asyncTask.Key));
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    syncStatus = "Sync Run async tasks started after communication";
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
                    CLError queueingError = addChangesToProcessingQueue(thingsThatWereDependenciesToQueue, true, queueingErrors);
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
                        foreach (Exception queueingException in queueingError.GrabExceptions() ?? Enumerable.Empty<Exception>())
                        {
                            toReturn += queueingException;
                        }
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

                toReturn.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
            }

            // if failure queue was marked that it needed to start timing,
            // then start it now
            if (needToStartFailureQueueTimer)
            {
                lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                {
                    GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                }
            }

            // return error aggregate
            return toReturn;
        }

        #region subroutine calls of Sync Run

        private static void AssignDependencies(IEnumerable<KeyValuePair<FileChange, FileStream>> toCheck, IEnumerable<FileChange> alreadyInError, out IEnumerable<KeyValuePair<FileChange, FileStream>> outputTopLevels, out IEnumerable<FileChange> outputTopErrors)
        {
            // Rebuild FileChange enumerables for output so each item output has no dependencies;
            // all input changes must be the direct outputs or a subsequent dependency
            throw new NotImplementedException();
        }

        private static Exception CompleteFileChange(KeyValuePair<FileChange, FileStream> toComplete, out Nullable<long> immediateSuccessEventId, out Nullable<KeyValuePair<SyncDirection, Task<long>>> asyncTask)
        {
            throw new NotImplementedException();
            try
            {
                // Except for file uploads/downloads, complete the FileChange synhronously, otherwise queue them appropriately;
                // If it completes synchronously and successfully, set the immediateSuccessEventId to toComplete.EventId, in all other cases set to null;
                // If it is supposed to run asynchrounously, set asyncTask with the task to complete, otherwise set it to null
            }
            catch (Exception ex)
            {
                immediateSuccessEventId = Helpers.DefaultForType<Nullable<long>>();
                asyncTask = Helpers.DefaultForType<Nullable<KeyValuePair<SyncDirection, Task<long>>>>();
                return ex;
            }
            return null;
        }

        private static Exception CommunicateWithServer(IEnumerable<KeyValuePair<FileChange, FileStream>> toCommunicate, 
            Func<FileChange, FileChange, CLError> mergeToSql,
            bool respondingToPushNotification,
            out IEnumerable<KeyValuePair<bool, FileChange>> completedChanges,
            out IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> incompleteChanges,
            out IEnumerable<KeyValuePair<bool, FileChange>> changesInError,
            out string newSyncId)
        {
            throw new NotImplementedException();
            try
            {
                KeyValuePair<FileChange, FileStream>[] communicationArray = (toCommunicate ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>()).ToArray();

                // if there is at least one change to communicate or we have a push notification to communcate anyways,
                // then process communication
                if (communicationArray.Length > 0
                    || respondingToPushNotification)
                {
                    // Run Sync From/Sync To with the list toCommunicate;
                    // Anything immediately complete should be output as completedChanges;
                    // Anything in conflict should be output as changesInError;
                    // Anything else should be output as incompleteChanges;
                    // The latest data from the server should be merged into each FileChange and then added to DB via mergeToSql
                }
                // else if there are no changes to communicate and we're not responding to a push notification,
                // do not process any communication (instead set all outputs to empty arrays)
                else
                {
                    completedChanges = new KeyValuePair<bool, FileChange>[0];
                    incompleteChanges = new KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>[0];
                    changesInError = new KeyValuePair<bool, FileChange>[0];
                    newSyncId = null;
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

        #endregion
    }
}