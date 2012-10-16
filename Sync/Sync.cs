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
using BadgeNET;
using System.Threading;
using System.Security.Cryptography;
using Sync.Static;
using CloudApiPrivate.EventMessageReceiver;

namespace Sync
{
    public static class Sync
    {
        private const int HttpTimeoutMilliseconds = 180000;// 180 seconds
        private const byte MaxNumberOfFailureRetries = 20;
        private const byte MaxNumberOfNotFounds = 10;

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
        private static readonly object failureTimerLocker = new object();

        // private queue for failures;
        // lock on failureTimer.TimerRunningLocker for all access
        private static readonly Queue<FileChange> FailedChangesQueue = new Queue<FileChange>();

        private static bool TempDownloadsCleaned = false;
        private static readonly object TempDownloadsCleanedLocker = new object();
        private static readonly Dictionary<long, List<KeyValuePair<Guid, byte[]>>> TempDownloads = new Dictionary<long, List<KeyValuePair<Guid, byte[]>>>();
        private static string TempDownloadsFolder = null;

        private static readonly CancellationTokenSource FullShutdownToken = new CancellationTokenSource();

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
                    if (FailedChangesQueue.Count > 0)
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
                                bool atLeastOneFailureAdded = false;

                                foreach (FileChange currentError in errList.Value)
                                {
                                    FailedChangesQueue.Enqueue(currentError);

                                    atLeastOneFailureAdded = true;
                                }

                                if (atLeastOneFailureAdded)
                                {
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
                            bool atLeastOneFailureAdded = false;

                            // An error occurred adding all the failed changes to the FileMonitor;
                            // requeue them all to the failure queue;
                            // rethrow error for logging
                            foreach (FileChange currentError in failedChanges)
                            {
                                FailedChangesQueue.Enqueue(currentError);

                                atLeastOneFailureAdded = true;
                            }

                            if (atLeastOneFailureAdded)
                            {
                                GetFailureTimer(AddChangesToProcessingQueue).StartTimerIfNotRunning();
                            }

                            throw;
                        }
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
            Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql,
            Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> addChangesToProcessingQueue,
            bool respondingToPushNotification,
            Func<string, IEnumerable<long>, string, CLError> completeSyncSql,
            Func<string> getLastSyncId,
            DependencyAssignments dependencyAssignment,
            Func<string> getCloudRoot,
            Func<FileChange, CLError> applySyncFromChange,
            Func<long, CLError> completeSingleEvent,
            MetadataByPathAndRevision getMetadataByPathAndRevision,
            Func<string> getDeviceName)
        {
            CLError toReturn = null;
            string syncStatus = "Sync Run entered";
            // errorsToQueue will have all changes to process (format KeyValuePair<KeyValuePair<[whether error was pulled from existing failures], [error change]>, [filestream for error]>),
            // items will be ignored as their successfulEventId is added
            List<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>> errorsToQueue = null;
            List<long> successfulEventIds = new List<long>();
            IEnumerable<FileChange> thingsThatWereDependenciesToQueue = null;
            try
            {
                // check for Sync shutdown
                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        return toReturn;
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

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
                if (getCloudRoot == null)
                {
                    throw new NullReferenceException("getCloudRoot cannot be null");
                }
                if (applySyncFromChange == null)
                {
                    throw new NullReferenceException("applySyncFromChange cannot be null");
                }
                if (completeSingleEvent == null)
                {
                    throw new NullReferenceException("completeSingleEvent cannot be null");
                }
                if (getMetadataByPathAndRevision == null)
                {
                    throw new NullReferenceException("getMetadataByPathAndRevision cannot be null");
                }
                if (getDeviceName == null)
                {
                    throw new NullReferenceException("getDeviceName cannot be null");
                }

                // Startup download temp folder
                // Bool to store whether download temp folder is marked for cleaning, defaulting to false
                bool tempNeedsCleaning = false;
                // Lock for reading/writing to whether startup occurred
                lock (TempDownloadsCleanedLocker)
                {
                    // Check global bool to see if startup has occurred
                    if (!TempDownloadsCleaned)
                    {
                        // Calculate download temp path
                        TempDownloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                            "\\Cloud\\DownloadTemp";

                        // Create directory if needed otherwise mark for cleaning
                        if (!Directory.Exists(TempDownloadsFolder))
                        {
                            Directory.CreateDirectory(TempDownloadsFolder);
                        }
                        else
                        {
                            tempNeedsCleaning = true;
                        }

                        // Startup taken care of
                        TempDownloadsCleaned = true;
                    }
                }
                // If download temp folder was marked for cleaning
                if (tempNeedsCleaning)
                {
                    // Lock for reading/writing to list of temp downloads
                    lock (TempDownloads)
                    {
                        // Loop through files in temp download folder
                        DirectoryInfo tempDownloadsFolderInfo = new DirectoryInfo(TempDownloadsFolder);
                        foreach (FileInfo currentTempFile in tempDownloadsFolderInfo.GetFiles())
                        {
                            // If temp download file name is appropriate for a [guid].ToString()
                            if (currentTempFile.Name.Length == 32)
                            {
                                // Check if file name is parsable as a Guid
                                Guid tempGuid;
                                if (Guid.TryParse(currentTempFile.Name, out tempGuid))
                                {
                                    // If file name is not queued for download,
                                    // Then it is old and can be deleted
                                    if (!TempDownloads.Values.Any(currentTempDownload => currentTempDownload.Any(currentInnerTempDownload => currentInnerTempDownload.Key == tempGuid)))
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

                syncStatus = "Sync Run temp download files cleaned";

                // check for Sync shutdown
                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        return toReturn;
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

                // Define field where top level hierarchy changes to process will be stored
                IEnumerable<KeyValuePair<FileChange, FileStream>> outputChanges;
                // Define field for changes that are immediately in error upon retrieval from FileSystem (including all items currently in failure queue)
                IEnumerable<KeyValuePair<bool, FileChange>> outputChangesInError;

                lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                {
                    IEnumerable<KeyValuePair<bool, FileChange>> initialErrors = new KeyValuePair<bool, FileChange>[FailedChangesQueue.Count];
                    for (int initialErrorIndex = 0; initialErrorIndex < ((KeyValuePair<bool, FileChange>[])initialErrors).Length; initialErrorIndex++)
                    {
                        ((KeyValuePair<bool, FileChange>[])initialErrors)[initialErrorIndex] = new KeyValuePair<bool, FileChange>(true, FailedChangesQueue.Dequeue());
                    }

                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunInitialErrors, initialErrors.Select(currentInitialError => currentInitialError.Value));
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
                        outputChangesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                        toReturn = ex;
                    }
                }

                // outputChanges now contains changes dequeued for processing from the filesystem monitor

                // set errors to queue here with all processed changes and all failed changes so they can be added to failure queue on exception
                errorsToQueue = new List<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>(outputChanges
                    .Select(currentOutputChange => new KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>(new KeyValuePair<bool, FileChange>(false, currentOutputChange.Key), currentOutputChange.Value))
                    .Concat(outputChangesInError.Select(currentChangeInError => new KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>(
                        currentChangeInError,
                        null))));

                // errorsToQueue now contains all errors previously in the failure queue (with bool true for being existing errors)
                // and errors that occurred while grabbing queued processing changes from file monitor (with bool false for not being existing errors);
                // it also contains all FileChanges which have yet to process but are already assumed to be in error until explicitly marked successful or removed from this list

                syncStatus = "Sync Run grabbed processed changes (with dependencies and final metadata)";

                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        return toReturn;
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

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
                            if (currentDependency.EventId == 0)
                            {
                                throw new ArgumentException("Cannot communicate FileChange without EventId");
                            }

                            // these conditions ensure that no file uploads without FileStreams are processed in the current batch
                            if (currentDependency.Metadata == null
                                || currentDependency.Metadata.HashableProperties.IsFolder
                                || currentDependency.Type == FileChangeType.Deleted
                                || currentDependency.Type == FileChangeType.Renamed
                                || currentDependency.Direction == SyncDirection.From)
                            {
                                errorsToQueue.Add(new KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>(new KeyValuePair<bool, FileChange>(true, currentDependency), null));
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

                // after each time reprocessForDependencies runs,
                // outputChanges will then equal all of the FileChanges it used to contain except for FileChanges which were previously preprocessed;
                // it also contains any uncovered dependencies beneath completed changes which can be processed (are not file uploads which are missing FileStreams)

                // after each time reprocessForDependencies runs,
                // errorsToQueue may be appended with dependencies whose parents have been completed
                // and who themselves can be processed (are not file uploads which are missing FileStreams)

                List<FileChange> synchronouslyPreprocessed = new List<FileChange>();
                List<FileChange> asynchronouslyPreprocessed = new List<FileChange>();

                // process once then repeat if it needs to reprocess for dependencies
                do
                {
                    foreach (KeyValuePair<FileChange, FileStream> topLevelChange in outputChanges)
                    {
                        Monitor.Enter(FullShutdownToken);
                        try
                        {
                            if (FullShutdownToken.Token.IsCancellationRequested)
                            {
                                return toReturn;
                            }
                        }
                        finally
                        {
                            Monitor.Exit(FullShutdownToken);
                        }

                        if (topLevelChange.Key.EventId > 0
                            && !preprocessedEventIds.Contains(topLevelChange.Key.EventId)
                            && (topLevelChange.Key.Direction == SyncDirection.From
                                || ((topLevelChange.Key.Type == FileChangeType.Created || topLevelChange.Key.Type == FileChangeType.Modified)
                                    && topLevelChange.Key.Metadata != null
                                    && !string.IsNullOrWhiteSpace(topLevelChange.Key.Metadata.StorageKey))))
                        {
                            preprocessedEvents.Add(topLevelChange);
                            preprocessedEventIds.Add(topLevelChange.Key.EventId);
                            Nullable<long> successfulEventId;
                            Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask;
                            Exception completionException = CompleteFileChange(topLevelChange, applySyncFromChange, completeSingleEvent, GetFailureTimer(addChangesToProcessingQueue), addChangesToProcessingQueue, mergeToSql, out successfulEventId, out asyncTask);
                            if (successfulEventId != null
                                && (long)successfulEventId > 0)
                            {
                                synchronouslyPreprocessed.Add(topLevelChange.Key);

                                successfulEventIds.Add((long)successfulEventId);

                                Nullable<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>> foundErrorToRemove = errorsToQueue
                                    .Where(findErrorToQueue => findErrorToQueue.Key.Value == topLevelChange.Key && findErrorToQueue.Value == topLevelChange.Value)
                                    .Select(findErrorToQueue => (Nullable<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>)findErrorToQueue)
                                    .FirstOrDefault();
                                if (foundErrorToRemove != null)
                                {
                                    errorsToQueue.Remove((KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>)foundErrorToRemove);
                                }

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

                                if (asyncTask != null)
                                {
                                    KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>> nonNullTask = (KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>)asyncTask;

                                    switch (asyncTask.Value.Key)
                                    {
                                        case SyncDirection.From:
                                            // direction for downloads
                                            nonNullTask.Value.ContinueWith(eventCompletion =>
                                            {
                                                if (!eventCompletion.IsFaulted)
                                                {
                                                    EventMessageReceiver.IncrementDownloadedCount();
                                                }
                                            }, TaskContinuationOptions.NotOnFaulted);
                                            break;

                                        case SyncDirection.To:
                                            // direction for uploads
                                            nonNullTask.Value.ContinueWith(eventCompletion =>
                                            {
                                                if (!eventCompletion.IsFaulted)
                                                {
                                                    EventMessageReceiver.IncrementUploadedCount();
                                                }
                                            }, TaskContinuationOptions.NotOnFaulted);
                                            break;

                                        default:
                                            // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                                            throw new NotSupportedException("Unknown SyncDirection: " + asyncTask.Value.ToString());
                                    }

                                    nonNullTask.Value.ContinueWith(completeState =>
                                    {
                                        Monitor.Enter(FullShutdownToken);
                                        try
                                        {
                                            if (FullShutdownToken.Token.IsCancellationRequested)
                                            {
                                                return;
                                            }
                                        }
                                        finally
                                        {
                                            Monitor.Exit(FullShutdownToken);
                                        }

                                        CLError sqlCompleteError = completeState.Result.Value(completeState.Result.Key);
                                        if (sqlCompleteError != null)
                                        {
                                            sqlCompleteError.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                        }

                                    }, TaskContinuationOptions.NotOnFaulted);

                                    lock (FileChange.UpDownEventLocker)
                                    {
                                        FileChange.UpDownEvent += topLevelChange.Key.FileChange_UpDown;
                                    }
                                    try
                                    {
                                        nonNullTask.Value.Start(HttpScheduler.GetSchedulerByDirection(nonNullTask.Key));

                                        asynchronouslyPreprocessed.Add(topLevelChange.Key);
                                    }
                                    catch
                                    {
                                        lock (FileChange.UpDownEventLocker)
                                        {
                                            FileChange.UpDownEvent -= topLevelChange.Key.FileChange_UpDown;
                                        }
                                    }

                                    Nullable<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>> foundErrorToRemove = errorsToQueue
                                        .Where(findErrorToQueue => findErrorToQueue.Key.Value == topLevelChange.Key && findErrorToQueue.Value == topLevelChange.Value)
                                        .Select(findErrorToQueue => (Nullable<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>)findErrorToQueue)
                                        .FirstOrDefault();
                                    if (foundErrorToRemove != null)
                                    {
                                        errorsToQueue.Remove((KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>)foundErrorToRemove);
                                    }
                                }
                            }
                        }
                    }
                }
                while (reprocessForDependencies());

                if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                {
                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunPreprocessedEventsSynchronous, synchronouslyPreprocessed);
                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunPreprocessedEventsAsynchronous, asynchronouslyPreprocessed);
                }

                // after each loop where more FileChanges from previous dependencies are processed,
                // if any FileChange is synchronously complete or queued for file upload/download then it is removed from errorsToQueue

                // see notes after reprocessForDependencies is defined to see what it does to errorsToQueue and outputChanges

                syncStatus = "Sync Run initial operations completed synchronously or queued";

                KeyValuePair<FileChange, FileStream>[] changesForCommunication = outputChanges.Except(preprocessedEvents).ToArray();

                // outputChanges is not used again,
                // it is redefined after communication and after reassigning dependencies

                KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>[] errorsToRequeue = errorsToQueue.Where(currentErrorToQueue => !Enumerable.Range(0, changesForCommunication.Length)
                        .Any(communicationIndex => changesForCommunication[communicationIndex].Key == currentErrorToQueue.Key.Value))
                    .ToArray();

                RequeueFailures(new List<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>(errorsToRequeue),
                    successfulEventIds, addChangesToProcessingQueue, mergeToSql, toReturn);

                if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                {
                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunRequeuedFailuresBeforeCommunication, errorsToRequeue.Select(currentErrorToRequeue => currentErrorToRequeue.Key.Value));
                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunChangesForCommunication, changesForCommunication.Select(currentChangeForCommunication => currentChangeForCommunication.Key));
                }

                errorsToQueue = new List<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>(changesForCommunication
                    .Select(currentChangeForCommunication => new KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>(new KeyValuePair<bool, FileChange>(false, currentChangeForCommunication.Key),
                        currentChangeForCommunication.Value)));

                // errorToQueue is now defined as the changesForCommunication
                // (all the previous errors that correspond to FileChanges which will not continue onto communication were added back to the failure queue)

                syncStatus = "Sync Run errors queued which were not changes that continued to communication";

                // Take events without dependencies that were not fired off in order to perform communication (or Sync From for no events left)
                IEnumerable<KeyValuePair<bool, FileChange>> completedChanges;
                IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> incompleteChanges;
                IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>> changesInError;
                string newSyncId;
                Exception communicationException = CommunicateWithServer(changesForCommunication,
                    mergeToSql,
                    getLastSyncId,
                    respondingToPushNotification,
                    getCloudRoot,
                    addChangesToProcessingQueue,
                    getMetadataByPathAndRevision,
                    getDeviceName,
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

                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.CommunicationCompletedChanges, completedChanges.Select(currentCompletedChange => currentCompletedChange.Value));
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.CommunicationIncompletedChanges, incompleteChanges.Select(currentIncompleteChange => currentIncompleteChange.Value.Key));
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.CommunicationChangesInError, changesInError.Select(currentChangeInError => currentChangeInError.Value.Key));
                    }

                    if (completedChanges != null)
                    {
                        foreach (KeyValuePair<bool, FileChange> currentCompletedChange in completedChanges)
                        {
                            successfulEventIds.Add(currentCompletedChange.Value.EventId);

                            FileChangeWithDependencies castCurrentCompletedChange = currentCompletedChange.Value as FileChangeWithDependencies;
                            if (castCurrentCompletedChange != null
                                && castCurrentCompletedChange.DependenciesCount > 0)
                            {
                                if (thingsThatWereDependenciesToQueue == null)
                                {
                                    thingsThatWereDependenciesToQueue = castCurrentCompletedChange.Dependencies;
                                }
                                else
                                {
                                    thingsThatWereDependenciesToQueue = thingsThatWereDependenciesToQueue.Concat(castCurrentCompletedChange.Dependencies);
                                }
                            }
                        }
                    }

                    // Merge in server values into DB (storage key, revision, etc) and add new Sync From events
                    mergeToSql((completedChanges ?? Enumerable.Empty<KeyValuePair<bool, FileChange>>())
                        .Concat((incompleteChanges ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>())
                            .Select(currentIncompleteChange => new KeyValuePair<bool, FileChange>(currentIncompleteChange.Key, currentIncompleteChange.Value.Key)))
                        .Concat((changesInError ?? Enumerable.Empty<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>())
                            .Select(currentChangeInError => new KeyValuePair<bool, FileChange>(currentChangeInError.Key, currentChangeInError.Value.Key)))
                        .Where(currentMerge => currentMerge.Key)
                        .Select(currentMerge => new KeyValuePair<FileChange, FileChange>(currentMerge.Value, null)), false);

                    if (changesInError != null)
                    {
                        foreach (KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>> grabException in changesInError)
                        {
                            toReturn += grabException.Value.Value.Value;
                            toReturn += grabException.Value.Value.Key;
                        }
                    }

                    syncStatus = "Sync Run server values merged into database";

                    Monitor.Enter(FullShutdownToken);
                    try
                    {
                        if (FullShutdownToken.Token.IsCancellationRequested)
                        {
                            return toReturn;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(FullShutdownToken);
                    }

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

                        if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                        {
                            Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunPostCommunicationDequeuedFailures, dequeuedFailures);
                        }

                        // Define errors to set after dependency calculations
                        // (will be top level a.k.a. not have any dependencies)
                        IEnumerable<FileChange> topLevelErrors;

                        bool atLeastOneErrorAdded = false;

                        try
                        {
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

                                // outputChanges now contains the dependency-assigned changes left to process after communication (from incompleteChanges)

                                if (outputChanges != null)
                                {
                                    if (uploadFilesWithoutStreams.Count > 0)
                                    {
                                        thingsThatWereDependenciesToQueue = outputChanges
                                            .Select(outputChange => outputChange.Key)
                                            .Intersect(thingsThatWereDependenciesToQueue);
                                    }
                                    if (thingsThatWereDependenciesToQueue != null
                                        && !thingsThatWereDependenciesToQueue.Any())
                                    {
                                        thingsThatWereDependenciesToQueue = null;
                                    }
                                    if (thingsThatWereDependenciesToQueue != null)
                                    {
                                        outputChanges = outputChanges.Where(outputChange => !thingsThatWereDependenciesToQueue.Contains(outputChange.Key));
                                    }
                                }

                                if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                                {
                                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.DependencyAssignmentOutputChanges, outputChanges.Select(currentOutputChange => currentOutputChange.Key));
                                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.DependencyAssignmentTopLevelErrors, topLevelErrors);
                                }

                                // outputChanges now excludes any FileChanges which overlapped with the existing list of thingsThatWereDependenciesToQueue
                                // (because that means the changes are file uploads without FileStreams and cannot be processed now)

                                errorsToQueue = new List<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>((outputChanges ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>())
                                    .Select(currentOutputChange => new KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>(new KeyValuePair<bool, FileChange>(false, currentOutputChange.Key), currentOutputChange.Value)));

                                // errorsToQueue now contains the outputChanges from after communication and dependency assignment
                                // (errors from communication and dependency assignment will be added immediately to failure queue

                                foreach (FileChange currentTopLevelError in topLevelErrors ?? Enumerable.Empty<FileChange>())
                                {
                                    FailedChangesQueue.Enqueue(currentTopLevelError);

                                    atLeastOneErrorAdded = true;
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

                                    atLeastOneErrorAdded = true;
                                }
                                throw;
                            }
                        }
                        finally
                        {
                            if (atLeastOneErrorAdded)
                            {
                                GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                            }
                        }
                    }

                    syncStatus = "Sync Run post-communication dependencies calculated";

                    Monitor.Enter(FullShutdownToken);
                    try
                    {
                        if (FullShutdownToken.Token.IsCancellationRequested)
                        {
                            return toReturn;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(FullShutdownToken);
                    }

                    List<KeyValuePair<KeyValuePair<FileChange, FileStream>, KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>>> asyncTasksToRun = new List<KeyValuePair<KeyValuePair<FileChange, FileStream>, KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>>>();

                    List<FileChange> postCommunicationSynchronousChanges = new List<FileChange>();

                    // Synchronously complete all local operations without dependencies (exclude file upload/download) and record successful events;
                    // If a completed event has dependencies, stick them on the end of the current batch;
                    // If an event fails to complete, leave it on errorsToQueue so it will be added to the failure queue later
                    foreach (KeyValuePair<FileChange, FileStream> topLevelChange in outputChanges)
                    {
                        Nullable<long> successfulEventId;
                        Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask;
                        Exception completionException = CompleteFileChange(topLevelChange, applySyncFromChange, completeSingleEvent, GetFailureTimer(addChangesToProcessingQueue), addChangesToProcessingQueue, mergeToSql, out successfulEventId, out asyncTask);
                        if (successfulEventId != null
                            && (long)successfulEventId > 0)
                        {
                            postCommunicationSynchronousChanges.Add(topLevelChange.Key);

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

                            if (asyncTask != null)
                            {
                                asyncTasksToRun.Add(new KeyValuePair<KeyValuePair<FileChange, FileStream>, KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>>(topLevelChange, (KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>)asyncTask));
                            }
                        }
                    }

                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunPostCommunicationSynchronous, postCommunicationSynchronousChanges);
                    }

                    syncStatus = "Sync Run synchronous post-communication operations complete";

                    Monitor.Enter(FullShutdownToken);
                    try
                    {
                        if (FullShutdownToken.Token.IsCancellationRequested)
                        {
                            completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                            incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                            changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                            newSyncId = Helpers.DefaultForType<string>();
                            return new Exception("Shut down in the middle of communication");
                        }
                    }
                    finally
                    {
                        Monitor.Exit(FullShutdownToken);
                    }

                    // Write new Sync point to database with successful events
                    CLError recordSyncError = completeSyncSql(newSyncId, successfulEventIds, Settings.Instance.CloudFolderPath);
                    if (recordSyncError != null)
                    {
                        toReturn += recordSyncError.GrabFirstException();
                    }

                    syncStatus = "Sync Run new sync point persisted";

                    Monitor.Enter(FullShutdownToken);
                    try
                    {
                        if (FullShutdownToken.Token.IsCancellationRequested)
                        {
                            completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                            incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                            changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                            newSyncId = Helpers.DefaultForType<string>();
                            return new Exception("Shut down in the middle of communication");
                        }
                    }
                    finally
                    {
                        Monitor.Exit(FullShutdownToken);
                    }

                    List<FileChange> postCommunicationAsynchronousChanges = new List<FileChange>();

                    // Asynchronously fire off all remaining upload/download operations without dependencies
                    foreach (KeyValuePair<KeyValuePair<FileChange, FileStream>, KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask in asyncTasksToRun)
                    {
                        try
                        {
                            switch (asyncTask.Value.Key)
                            {
                                case SyncDirection.From:
                                // direction for downloads
                                    asyncTask.Value.Value.ContinueWith(eventCompletion =>
                                    {
                                        if (!eventCompletion.IsFaulted)
                                        {
                                            EventMessageReceiver.IncrementDownloadedCount();
                                        }
                                    }, TaskContinuationOptions.NotOnFaulted);
                                    break;

                                case SyncDirection.To:
                                // direction for uploads
                                    asyncTask.Value.Value.ContinueWith(eventCompletion =>
                                    {
                                        if (!eventCompletion.IsFaulted)
                                        {
                                            EventMessageReceiver.IncrementUploadedCount();
                                        }
                                    }, TaskContinuationOptions.NotOnFaulted);
                                    break;

                                default:
                                    // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                                    throw new NotSupportedException("Unknown SyncDirection: " + asyncTask.Value.ToString());
                            }

                            asyncTask.Value.Value.ContinueWith(eventCompletion =>
                            {
                                CLError sqlCompleteError = eventCompletion.Result.Value(eventCompletion.Result.Key);
                                if (sqlCompleteError != null)
                                {
                                    sqlCompleteError.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                }
                            }, TaskContinuationOptions.NotOnFaulted);

                            lock (FileChange.UpDownEventLocker)
                            {
                                FileChange.UpDownEvent += asyncTask.Key.Key.FileChange_UpDown;
                            }
                            try
                            {
                                asyncTask.Value.Value.Start(HttpScheduler.GetSchedulerByDirection(asyncTask.Value.Key));

                                postCommunicationAsynchronousChanges.Add(asyncTask.Key.Key);

                                Nullable<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>> foundErrorToRemove = errorsToQueue
                                    .Where(findErrorToQueue => findErrorToQueue.Key.Value == asyncTask.Key.Key && findErrorToQueue.Value == asyncTask.Key.Value)
                                    .Select(findErrorToQueue => (Nullable<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>>)findErrorToQueue)
                                    .FirstOrDefault();
                                if (foundErrorToRemove != null)
                                {
                                    errorsToQueue.Remove((KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>)foundErrorToRemove);
                                }
                            }
                            catch
                            {
                                lock (FileChange.UpDownEventLocker)
                                {
                                    FileChange.UpDownEvent -= asyncTask.Key.Key.FileChange_UpDown;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunPostCommunicationAsynchronous, postCommunicationAsynchronousChanges);
                    }

                    // for any FileChange which was asynchronously queued for file upload or download,
                    // errorsToQueue had that change removed

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
                            bool atLeastOneFailureAdded = false;

                            foreach (FileChange currentQueueingError in queueingErrors.Value)
                            {
                                FailedChangesQueue.Enqueue(currentQueueingError);

                                atLeastOneFailureAdded = true;
                            }

                            if (atLeastOneFailureAdded)
                            {
                                GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                            }
                        }
                    }
                    if (queueingError != null)
                    {
                        toReturn += new AggregateException("Error adding dependencies to processing queue after sync", queueingError.GrabExceptions());
                    }

                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunEndThingsThatWereDependenciesToQueue, thingsThatWereDependenciesToQueue);
                    }
                }
                catch (Exception ex)
                {
                    // on serious error in queueing changes,
                    // instead add them all to the failure queue add add the error to the returned aggregate errors
                    lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                    {
                        bool atLeastOneFailureAdded = false;

                        foreach (FileChange currentDependency in thingsThatWereDependenciesToQueue)
                        {
                            FailedChangesQueue.Enqueue(currentDependency);

                            atLeastOneFailureAdded = true;
                        }

                        if (atLeastOneFailureAdded)
                        {
                            GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                        }
                    }
                    toReturn += ex;
                }
            }

            // errorsToQueue should contain all FileChanges which were already added back to the failure queue
            // or are asynchronously queued for file upload or download; it may also contain some completed changes
            // which will be checked against successfulEventIds;
            // this should be true regardless of whether a line in the main try/catch threw an exception since it should be up to date at all times

            RequeueFailures(errorsToQueue, successfulEventIds, addChangesToProcessingQueue, mergeToSql, toReturn);

            if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
            {
                Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.SyncRunEndRequeuedFailures, errorsToQueue.Select(currentErrorToQueue => currentErrorToQueue.Key.Value));
            }

            // errorsToQueue is no longer used (all its errors were added back to the failure queue)

            // if the output error aggregate contains any errors,
            // then add the sync status (to notify position in case of catastrophic failure),
            // dispose all FileStreams,
            // and log all errors
            if (toReturn != null)
            {
                toReturn.errorInfo.Add(CLError.ErrorInfo_Sync_Run_Status, syncStatus);
                CLError disposalError = toReturn.DequeueFileStreams().DisposeAllStreams();
                if (disposalError != null)
                {
                    foreach (Exception disposalException in disposalError.GrabExceptions())
                    {
                        toReturn += disposalException;
                    }
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

        /// <summary>
        /// Call this to terminate all sync threads including active uploads and downloads;
        /// after calling this, a restart is required to sync again
        /// </summary>
        public static void Shutdown()
        {
            Monitor.Enter(FullShutdownToken);
            try
            {
                FullShutdownToken.Cancel();
            }
            catch
            {
            }
            finally
            {
                Monitor.Exit(FullShutdownToken);
            }
        }

        #region subroutine calls of Sync Run
        private static void RequeueFailures(List<KeyValuePair<KeyValuePair<bool, FileChange>, FileStream>> errorsToQueue,
            List<long> successfulEventIds,
            Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> addChangesToProcessingQueue,
            Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql,
            CLError appendErrors)
        {
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
                            bool atLeastOneErrorAdded = false;
                            foreach (KeyValuePair<KeyValuePair<bool, FileChange>, FileStream> errorToQueue in errorsToQueue)
                            {
                                try
                                {
                                    if (successfulEventIds.BinarySearch(errorToQueue.Key.Value.EventId) < 0)
                                    {
                                        appendErrors += errorToQueue.Value;

                                        if (ContinueToRetry(mergeToSql, errorToQueue.Key.Value, errorToQueue.Key.Key))
                                        {
                                            FailedChangesQueue.Enqueue(errorToQueue.Key.Value);

                                            atLeastOneErrorAdded = true;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    appendErrors += ex;
                                    appendErrors += errorToQueue.Value;
                                }
                            }

                            if (atLeastOneErrorAdded)
                            {
                                GetFailureTimer(addChangesToProcessingQueue).StartTimerIfNotRunning();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        appendErrors += ex;
                    }
                }
            }
        }

        private static Exception CompleteFileChange(KeyValuePair<FileChange, FileStream> toComplete, Func<FileChange, CLError> applySyncFromChange, Func<long, CLError> completeSingleEvent, ProcessingQueuesTimer failureTimer, Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> AddChangesToProcessingQueue, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql, out Nullable<long> immediateSuccessEventId, out Nullable<KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>> asyncTask)
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
                        bool fileMatches = false;

                        byte[] toCompleteBytes = null;
                        if (toComplete.Key.GetMD5Bytes(out toCompleteBytes) == null)
                        {
                            try
                            {
                                string toCompletePath = toComplete.Key.NewPath.ToString();
                                FileInfo existingInfo = new FileInfo(toCompletePath);
                                if (existingInfo.Exists
                                    && existingInfo.Length == (toComplete.Key.Metadata.HashableProperties.Size ?? -1))
                                {
                                    using (FileStream existingStream = new FileStream(toCompletePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        MD5 md5Hasher = MD5.Create();

                                        byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                        int fileReadBytes;

                                        while ((fileReadBytes = existingStream.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                        {
                                            md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                        }

                                        md5Hasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);

                                        fileMatches = NativeMethods.memcmp(toCompleteBytes, md5Hasher.Hash, new UIntPtr((uint)toCompleteBytes.Length)) == 0;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        if (fileMatches)
                        {
                            immediateSuccessEventId = toComplete.Key.EventId;
                            asyncTask = null;
                        }
                        else
                        {
                            immediateSuccessEventId = null;
                            asyncTask = new KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>(SyncDirection.From,
                                new Task<KeyValuePair<long, Func<long, CLError>>>(downloadState =>
                                {
                                    ProcessingQueuesTimer storeFailureTimer = null;
                                    FileChange storeFileChange = null;
                                    Nullable<Guid> newTempFile = null;
                                    string tempFolderString = null;
                                    Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> stateMergeToSql = null;
                                    try
                                    {
                                        Monitor.Enter(FullShutdownToken);
                                        try
                                        {
                                            if (FullShutdownToken.Token.IsCancellationRequested)
                                            {
                                                return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                            }
                                        }
                                        finally
                                        {
                                            Monitor.Exit(FullShutdownToken);
                                        }

                                        DownloadTaskState castState = downloadState as DownloadTaskState;
                                        if (castState == null)
                                        {
                                            throw new NullReferenceException("Download Task downloadState not castable as DownloadTaskState");
                                        }

                                        stateMergeToSql = castState.mergeToSql;

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

                                        if (castState.GetTempFolder == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain GetTempFolder");
                                        }
                                        tempFolderString = castState.GetTempFolder();

                                        if (castState.ApplySyncFromChange == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain ApplySyncFromChange");
                                        }
                                        if (castState.CompleteSingleEvent == null)
                                        {
                                            throw new NullReferenceException("DownloadTaskState must contain CompleteSingleEvent");
                                        }

                                        string responseBody = null;
                                        lock (TempDownloads)
                                        {
                                            List<KeyValuePair<Guid, byte[]>> tempDownloadsInSize;
                                            if (TempDownloads.TryGetValue((long)storeFileChange.Metadata.HashableProperties.Size,
                                                out tempDownloadsInSize))
                                            {
                                                foreach (KeyValuePair<Guid, byte[]> currentTempDownload in tempDownloadsInSize)
                                                {
                                                    if (NativeMethods.memcmp(castState.MD5, currentTempDownload.Value, new UIntPtr((uint)castState.MD5.Length)) == 0)
                                                    {
                                                        newTempFile = currentTempDownload.Key;
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (newTempFile != null)
                                        {
                                            string newTempFileString = tempFolderString + "\\" + ((Guid)newTempFile).ToString("N");

                                            MoveCompletedDownload(castState.ApplySyncFromChange,
                                                newTempFileString,
                                                storeFileChange,
                                                castState.AddChangesToProcessingQueue,
                                                ref responseBody,
                                                castState.FailureTimer,
                                                (Guid)newTempFile);

                                            return new KeyValuePair<long, Func<long, CLError>>(storeFileChange.EventId, castState.CompleteSingleEvent);
                                        }
                                        
                                        string requestBody;
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            DownloadSerializer.WriteObject(ms,
                                                new JsonContracts.Download()
                                                {
                                                    StorageKey = storeFileChange.Metadata.StorageKey
                                                });
                                            requestBody = Encoding.Default.GetString(ms.ToArray());
                                        }

                                        byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                                        HttpWebRequest downloadRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathDownload);
                                        downloadRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                                        downloadRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                                        // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                                        downloadRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                                        downloadRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                                        downloadRequest.SendChunked = false;
                                        downloadRequest.KeepAlive = false;
                                        downloadRequest.Timeout = HttpTimeoutMilliseconds;
                                        downloadRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson;
                                        downloadRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                                        downloadRequest.ContentLength = requestBodyBytes.Length;

                                        if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                        {
                                            Trace.LogCommunication(Settings.Instance.TraceLocation,
                                                Settings.Instance.Udid,
                                                Settings.Instance.Uuid,
                                                CommunicationEntryDirection.Request,
                                                CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathDownload,
                                                true,
                                                downloadRequest.Headers,
                                                requestBody,
                                                null,
                                                Settings.Instance.TraceExcludeAuthorization,
                                                downloadRequest.Host,
                                                downloadRequest.ContentLength.ToString(),
                                                (downloadRequest.Expect == null ? "100-continue" : downloadRequest.Expect),
                                                (downloadRequest.KeepAlive ? "Keep-Alive" : "Close"));
                                        }

                                        AsyncRequestHolder requestHolder = new AsyncRequestHolder();

                                        IAsyncResult requestAsyncResult;

                                        lock (requestHolder)
                                        {
                                            requestAsyncResult = downloadRequest.BeginGetRequestStream(new AsyncCallback(MakeAsyncRequestSynchronous), requestHolder);

                                            Monitor.Wait(requestHolder);
                                        }

                                        if (requestHolder.Error != null)
                                        {
                                            throw requestHolder.Error;
                                        }

                                        if (requestHolder.IsCanceled)
                                        {
                                            return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                        }

                                        using (Stream downloadRequestStream = downloadRequest.EndGetRequestStream(requestAsyncResult))
                                        {
                                            downloadRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                                        }

                                        HttpWebResponse downloadResponse;
                                        try
                                        {
                                            AsyncRequestHolder responseHolder = new AsyncRequestHolder();

                                            IAsyncResult responseAsyncResult;

                                            lock (responseHolder)
                                            {
                                                responseAsyncResult = downloadRequest.BeginGetResponse(new AsyncCallback(MakeAsyncRequestSynchronous), responseHolder);

                                                Monitor.Wait(responseHolder);
                                            }

                                            if (responseHolder.Error != null)
                                            {
                                                throw responseHolder.Error;
                                            }

                                            if (responseHolder.IsCanceled)
                                            {
                                                return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                            }

                                            downloadResponse = (HttpWebResponse)downloadRequest.EndGetResponse(responseAsyncResult);
                                        }
                                        catch (WebException ex)
                                        {
                                            if (ex.Response == null)
                                            {
                                                throw new NullReferenceException("downloadRequest Response cannot be null", ex);
                                            }
                                            downloadResponse = (HttpWebResponse)ex.Response;
                                        }

                                        try
                                        {
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

                                                    newTempFile = Guid.NewGuid();
                                                    lock (TempDownloads)
                                                    {
                                                        if (TempDownloads.ContainsKey((long)storeFileChange.Metadata.HashableProperties.Size))
                                                        {
                                                            TempDownloads[(long)storeFileChange.Metadata.HashableProperties.Size].Add(new KeyValuePair<Guid, byte[]>((Guid)newTempFile, castState.MD5));
                                                        }
                                                        else
                                                        {
                                                            TempDownloads.Add((long)storeFileChange.Metadata.HashableProperties.Size,
                                                                new List<KeyValuePair<Guid, byte[]>>(new KeyValuePair<Guid, byte[]>[]
                                                                {
                                                                    new KeyValuePair<Guid, byte[]>((Guid)newTempFile, castState.MD5)
                                                                }));
                                                        }
                                                    }
                                                    string newTempFileString = tempFolderString + "\\" + ((Guid)newTempFile).ToString("N");

                                                    using (Stream downloadResponseStream = downloadResponse.GetResponseStream())
                                                    {
                                                        using (FileStream tempFileStream = new FileStream(newTempFileString, FileMode.Create, FileAccess.Write, FileShare.None))
                                                        {
                                                            byte[] data = new byte[CLDefinitions.SyncConstantsResponseBufferSize];
                                                            int read;
                                                            while ((read = downloadResponseStream.Read(data, 0, data.Length)) > 0)
                                                            {
                                                                tempFileStream.Write(data, 0, read);

                                                                Monitor.Enter(FullShutdownToken);
                                                                try
                                                                {
                                                                    if (FullShutdownToken.Token.IsCancellationRequested)
                                                                    {
                                                                        return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                                                    }
                                                                }
                                                                finally
                                                                {
                                                                    Monitor.Exit(FullShutdownToken);
                                                                }
                                                            }
                                                            tempFileStream.Flush();
                                                        }
                                                    }

                                                    File.SetCreationTimeUtc(newTempFileString, storeFileChange.Metadata.HashableProperties.CreationTime);
                                                    File.SetLastAccessTimeUtc(newTempFileString, storeFileChange.Metadata.HashableProperties.LastTime);
                                                    File.SetLastWriteTimeUtc(newTempFileString, storeFileChange.Metadata.HashableProperties.LastTime);

                                                    MoveCompletedDownload(castState.ApplySyncFromChange,
                                                        newTempFileString,
                                                        storeFileChange,
                                                        castState.AddChangesToProcessingQueue,
                                                        ref responseBody,
                                                        castState.FailureTimer,
                                                        (Guid)newTempFile);

                                                    return new KeyValuePair<long, Func<long, CLError>>(storeFileChange.EventId, castState.CompleteSingleEvent);
                                                }
                                            }
                                            finally
                                            {
                                                if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                                {
                                                    Trace.LogCommunication(Settings.Instance.TraceLocation,
                                                        Settings.Instance.Udid,
                                                        Settings.Instance.Uuid,
                                                        CommunicationEntryDirection.Response,
                                                        CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathDownload,
                                                        true,
                                                        downloadResponse.Headers,
                                                        responseBody,
                                                        (int)downloadResponse.StatusCode,
                                                        Settings.Instance.TraceExcludeAuthorization);
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            try
                                            {
                                                downloadResponse.Close();
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                                        {
                                            Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.UploadDownloadFailure, (storeFileChange == null ? null : new FileChange[] { storeFileChange }));
                                        }

                                        ExecutableException<KeyValuePair<KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>, KeyValuePair<KeyValuePair<string, Nullable<Guid>>, ProcessingQueuesTimer>>> wrappedEx = new ExecutableException<KeyValuePair<KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>, KeyValuePair<KeyValuePair<string, Nullable<Guid>>, ProcessingQueuesTimer>>>((exceptionState, exceptions) =>
                                        {
                                            try
                                            {
                                                if (exceptionState.Key.Key == null)
                                                {
                                                    throw new NullReferenceException("exceptionState's FileChange cannot be null");
                                                }
                                                if (exceptionState.Value.Value == null)
                                                {
                                                    throw new NullReferenceException("exceptionState's ProcessingQueuesTimer cannot be null");
                                                }

                                                string growlErrorMessage = "An error occurred downloading " +
                                                    exceptionState.Key.Key.NewPath.ToString() + ": " +

                                                    ((exceptions.InnerException == null || exceptions.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.Message))
                                                        ? ((exceptions.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.Message))
                                                            ? exceptions.Message
                                                            : exceptions.InnerException.Message)
                                                        : exceptions.InnerException.InnerException.Message);

                                                if (ContinueToRetry(exceptionState.Key.Value, exceptionState.Key.Key, false))
                                                {
                                                    lock (exceptionState.Value.Value.TimerRunningLocker)
                                                    {
                                                        FailedChangesQueue.Enqueue(exceptionState.Key.Key);

                                                        exceptionState.Value.Value.StartTimerIfNotRunning();
                                                    }

                                                    growlErrorMessage += "; Retrying";
                                                }
                                                // If failed out and no more retries, delete any temp download
                                                else
                                                {
                                                    try
                                                    {
                                                        if (exceptionState.Value.Value != null
                                                            && exceptionState.Key.Key != null
                                                            && !string.IsNullOrWhiteSpace(exceptionState.Value.Key.Key))
                                                        {
                                                            lock (TempDownloads)
                                                            {
                                                                List<KeyValuePair<Guid, byte[]>> errorTemp;
                                                                if (TempDownloads.TryGetValue((long)exceptionState.Key.Key.Metadata.HashableProperties.Size, out errorTemp))
                                                                {
                                                                    foreach (KeyValuePair<Guid, byte[]> matchedErrorTemp in errorTemp.Where(currentError => currentError.Key == (Guid)exceptionState.Value.Key.Value))
                                                                    {
                                                                        File.Delete(exceptionState.Value.Key.Key + "\\" + ((Guid)newTempFile).ToString("N"));

                                                                        errorTemp.Remove(matchedErrorTemp);
                                                                        if (errorTemp.Count == 0)
                                                                        {
                                                                            TempDownloads.Remove((long)exceptionState.Key.Key.Metadata.HashableProperties.Size);
                                                                        }
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }

                                                EventMessageReceiver.DisplayErrorGrowl(growlErrorMessage);
                                            }
                                            catch (Exception innerEx)
                                            {
                                                ((CLError)innerEx).LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                            }
                                        },
                                            new KeyValuePair<KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>, KeyValuePair<KeyValuePair<string, Nullable<Guid>>, ProcessingQueuesTimer>>(new KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>(storeFileChange,
                                                    stateMergeToSql),
                                                new KeyValuePair<KeyValuePair<string, Nullable<Guid>>,ProcessingQueuesTimer>(new KeyValuePair<string, Nullable<Guid>>(tempFolderString, newTempFile),
                                                    storeFailureTimer)),
                                            "Error in download Task, see inner exception",
                                            ex);
                                        throw wrappedEx;
                                    }
                                    finally
                                    {
                                        if (storeFileChange != null)
                                        {
                                            lock (FileChange.UpDownEventLocker)
                                            {
                                                FileChange.UpDownEvent -= storeFileChange.FileChange_UpDown;
                                            }
                                        }
                                    }
                                },
                                new DownloadTaskState()
                                {
                                    FileToDownload = toComplete.Key,
                                    MD5 = toCompleteBytes,
                                    GetTempFolder = () => TempDownloadsFolder,
                                    ApplySyncFromChange = applySyncFromChange,
                                    CompleteSingleEvent = completeSingleEvent,
                                    FailureTimer = failureTimer,
                                    AddChangesToProcessingQueue = AddChangesToProcessingQueue,
                                    mergeToSql = mergeToSql
                                }));
                        }
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
                    immediateSuccessEventId = null;
                    asyncTask = new KeyValuePair<SyncDirection, Task<KeyValuePair<long, Func<long, CLError>>>>(SyncDirection.To,
                        new Task<KeyValuePair<long, Func<long, CLError>>>(uploadState =>
                        {
                            ProcessingQueuesTimer storeFailureTimer = null;
                            FileChange storeFileChange = null;
                            Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> stateMergeToSql = null;
                            FileStream storeFileStream = null;
                            try
                            {
                                try
                                {
                                    Monitor.Enter(FullShutdownToken);
                                    try
                                    {
                                        if (FullShutdownToken.Token.IsCancellationRequested)
                                        {
                                            return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                        }
                                    }
                                    finally
                                    {
                                        Monitor.Exit(FullShutdownToken);
                                    }

                                    UploadTaskState castState = uploadState as UploadTaskState;
                                    if (castState == null)
                                    {
                                        throw new NullReferenceException("Upload Task uploadState not castable as UploadTaskState");
                                    }

                                    stateMergeToSql = castState.mergeToSql;
                                    storeFileStream = castState.UploadStream;

                                    // attempt to check these two references first so that the failure can be added to the queue if exceptions occur later:
                                    // the failure timer and the failed FileChange
                                    if (castState.FailureTimer == null)
                                    {
                                        throw new NullReferenceException("UploadTaskState must contain FailureTimer");
                                    }
                                    storeFailureTimer = castState.FailureTimer;
                                    if (castState.FileToUpload == null)
                                    {
                                        throw new NullReferenceException("UploadTaskState must contain FileToDownload");
                                    }
                                    storeFileChange = castState.FileToUpload;

                                    if (castState.UploadStream == null)
                                    {
                                        throw new NullReferenceException("UploadTaskState must contain UploadStream");
                                    }

                                    if (castState.CompleteSingleEvent == null)
                                    {
                                        throw new NullReferenceException("UploadTaskState must contain CompleteSingleEvent");
                                    }

                                    if (storeFileChange.Metadata.HashableProperties.Size == null)
                                    {
                                        throw new NullReferenceException("storeFileChange must have a Size");
                                    }

                                    if (string.IsNullOrWhiteSpace(storeFileChange.Metadata.StorageKey))
                                    {
                                        throw new NullReferenceException("storeFileChange must have a StorageKey");
                                    }

                                    string hash;
                                    CLError retrieveHashError = storeFileChange.GetMD5LowercaseString(out hash);
                                    if (retrieveHashError != null)
                                    {
                                        throw new AggregateException("Unable to retrieve MD5 from storeFileChange", retrieveHashError.GrabExceptions());
                                    }
                                    if (hash == null)
                                    {
                                        throw new NullReferenceException("storeFileChange must have a hash");
                                    }

                                    HttpWebRequest uploadRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathUpload);
                                    uploadRequest.Method = CLDefinitions.HeaderAppendMethodPut;
                                    uploadRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                                    // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                                    uploadRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                                    uploadRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                                    uploadRequest.SendChunked = false;
                                    uploadRequest.Timeout = HttpTimeoutMilliseconds;
                                    uploadRequest.ContentType = CLDefinitions.HeaderAppendContentTypeBinary;
                                    uploadRequest.ContentLength = (long)storeFileChange.Metadata.HashableProperties.Size;
                                    uploadRequest.Headers[CLDefinitions.HeaderAppendStorageKey] = storeFileChange.Metadata.StorageKey;
                                    uploadRequest.Headers[CLDefinitions.HeaderAppendContentMD5] = hash;
                                    uploadRequest.KeepAlive = true;

                                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    {
                                        Trace.LogCommunication(Settings.Instance.TraceLocation,
                                            Settings.Instance.Udid,
                                            Settings.Instance.Uuid,
                                            CommunicationEntryDirection.Request,
                                            CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathUpload,
                                            true,
                                            uploadRequest.Headers,
                                            "---File upload started---",
                                            null,
                                            Settings.Instance.TraceExcludeAuthorization,
                                            uploadRequest.Host,
                                            uploadRequest.ContentLength.ToString(),
                                            (uploadRequest.Expect == null ? "100-continue" : uploadRequest.Expect),
                                            (uploadRequest.KeepAlive ? "Keep-Alive" : "Close"));
                                    }

                                    AsyncRequestHolder requestHolder = new AsyncRequestHolder();

                                    IAsyncResult requestAsyncResult;

                                    lock (requestHolder)
                                    {
                                        requestAsyncResult = uploadRequest.BeginGetRequestStream(new AsyncCallback(MakeAsyncRequestSynchronous), requestHolder);

                                        Monitor.Wait(requestHolder);
                                    }

                                    if (requestHolder.Error != null)
                                    {
                                        throw requestHolder.Error;
                                    }

                                    if (requestHolder.IsCanceled)
                                    {
                                        return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                    }

                                    using (Stream uploadRequestStream = uploadRequest.EndGetRequestStream(requestAsyncResult))
                                    {
                                        byte[] uploadBuffer = new byte[FileConstants.BufferSize];

                                        int bytesRead = 0;

                                        while ((bytesRead = storeFileStream.Read(uploadBuffer, 0, uploadBuffer.Length)) != 0)
                                        {
                                            uploadRequestStream.Write(uploadBuffer, 0, bytesRead);

                                            Monitor.Enter(FullShutdownToken);
                                            try
                                            {
                                                if (FullShutdownToken.Token.IsCancellationRequested)
                                                {
                                                    return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                                }
                                            }
                                            finally
                                            {
                                                Monitor.Exit(FullShutdownToken);
                                            }
                                        }
                                    }

                                    try
                                    {
                                        storeFileStream.Dispose();
                                        storeFileStream = null;
                                    }
                                    catch
                                    {
                                    }

                                    HttpWebResponse uploadResponse;
                                    try
                                    {
                                        AsyncRequestHolder responseHolder = new AsyncRequestHolder();

                                        IAsyncResult responseAsyncResult;

                                        lock (responseHolder)
                                        {
                                            responseAsyncResult = uploadRequest.BeginGetResponse(new AsyncCallback(MakeAsyncRequestSynchronous), responseHolder);

                                            Monitor.Wait(responseHolder);
                                        }

                                        if (responseHolder.Error != null)
                                        {
                                            throw responseHolder.Error;
                                        }

                                        if (responseHolder.IsCanceled)
                                        {
                                            return new KeyValuePair<long, Func<long, CLError>>(0, null);
                                        }

                                        uploadResponse = (HttpWebResponse)uploadRequest.EndGetResponse(responseAsyncResult);
                                    }
                                    catch (WebException ex)
                                    {
                                        if (ex.Response == null)
                                        {
                                            throw new NullReferenceException("uploadRequest Response cannot be null", ex);
                                        }
                                        uploadResponse = (HttpWebResponse)ex.Response;
                                    }

                                    try
                                    {
                                        string responseBody = "---File upload incomplete---";
                                        try
                                        {
                                            if (uploadResponse.StatusCode != HttpStatusCode.OK
                                                && uploadResponse.StatusCode != HttpStatusCode.Created
                                                && uploadResponse.StatusCode != HttpStatusCode.NotModified)
                                            {
                                                try
                                                {
                                                    using (Stream downloadResponseStream = uploadResponse.GetResponseStream())
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

                                                throw new Exception("Invalid HTTP response status code in file upload: " + ((int)uploadResponse.StatusCode).ToString() +
                                                    (responseBody == null ? string.Empty
                                                        : Environment.NewLine + "Response:" + Environment.NewLine +
                                                        responseBody));
                                            }
                                            else
                                            {
                                                if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                                                {
                                                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.UploadDownloadSuccess, new FileChange[] { storeFileChange });
                                                }

                                                responseBody = "---File upload complete---";

                                                FileChangeWithDependencies toCompleteWithDependencies = toComplete.Key as FileChangeWithDependencies;
                                                if (toCompleteWithDependencies != null
                                                    && toCompleteWithDependencies.DependenciesCount > 0)
                                                {
                                                    GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>();
                                                    CLError err = castState.AddChangesToProcessingQueue(toCompleteWithDependencies.Dependencies, true, errList);
                                                    if (errList.Value != null)
                                                    {
                                                        bool atLeastOneFailuredAdded = false;

                                                        foreach (FileChange currentError in errList.Value)
                                                        {
                                                            FailedChangesQueue.Enqueue(currentError);

                                                            atLeastOneFailuredAdded = true;
                                                        }

                                                        if (atLeastOneFailuredAdded)
                                                        {
                                                            castState.FailureTimer.StartTimerIfNotRunning();
                                                        }
                                                    }
                                                    if (err != null)
                                                    {
                                                        err.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                                    }
                                                }

                                                return new KeyValuePair<long, Func<long, CLError>>(toComplete.Key.EventId, castState.CompleteSingleEvent);
                                            }
                                        }
                                        finally
                                        {
                                            if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                            {
                                                Trace.LogCommunication(Settings.Instance.TraceLocation,
                                                    Settings.Instance.Udid,
                                                    Settings.Instance.Uuid,
                                                    CommunicationEntryDirection.Response,
                                                    CLDefinitions.CLUploadDownloadServerURL + CLDefinitions.MethodPathUpload,
                                                    true,
                                                    uploadResponse.Headers,
                                                    responseBody,
                                                    (int)uploadResponse.StatusCode,
                                                    Settings.Instance.TraceExcludeAuthorization);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        try
                                        {
                                            uploadResponse.Close();
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                                catch
                                {
                                    if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                                    {
                                        Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.UploadDownloadFailure, (storeFileChange == null ? null : new FileChange[] { storeFileChange }));
                                    }

                                    throw;
                                }
                            }
                            catch (Exception ex)
                            {
                                ExecutableException<KeyValuePair<KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>, KeyValuePair<ProcessingQueuesTimer, FileStream>>> wrappedEx = new ExecutableException<KeyValuePair<KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>, KeyValuePair<ProcessingQueuesTimer, FileStream>>>((exceptionState, exceptions) =>
                                {
                                    try
                                    {
                                        if (exceptionState.Value.Value != null)
                                        {
                                            try
                                            {
                                                exceptionState.Value.Value.Dispose();
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        if (exceptionState.Key.Key == null)
                                        {
                                            throw new NullReferenceException("exceptionState's FileChange cannot be null");
                                        }
                                        if (exceptionState.Value.Key == null)
                                        {
                                            throw new NullReferenceException("exceptionState's ProcessingQueuesTimer cannot be null");
                                        }

                                        string growlErrorMessage = "An error occurred uploading " +
                                            exceptionState.Key.Key.NewPath.ToString() + ": " +

                                            ((exceptions.InnerException == null || exceptions.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.Message))
                                                ? ((exceptions.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.Message))
                                                    ? exceptions.Message
                                                    : exceptions.InnerException.Message)
                                                : exceptions.InnerException.InnerException.Message);

                                        if (ContinueToRetry(exceptionState.Key.Value, exceptionState.Key.Key, false))
                                        {
                                            lock (exceptionState.Value.Key.TimerRunningLocker)
                                            {
                                                FailedChangesQueue.Enqueue(exceptionState.Key.Key);

                                                exceptionState.Value.Key.StartTimerIfNotRunning();
                                            }

                                            growlErrorMessage += "; Retrying";
                                        }

                                        EventMessageReceiver.DisplayErrorGrowl(growlErrorMessage);
                                    }
                                    catch (Exception innerEx)
                                    {
                                        ((CLError)innerEx).LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                                    }
                                },
                                    new KeyValuePair<KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>, KeyValuePair<ProcessingQueuesTimer, FileStream>>(new KeyValuePair<FileChange, Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError>>(toComplete.Key,
                                            stateMergeToSql),
                                        new KeyValuePair<ProcessingQueuesTimer, FileStream>(storeFailureTimer, storeFileStream)),
                                    "Error in upload Task, see inner exception",
                                    ex);
                                throw wrappedEx;
                            }
                            finally
                            {
                                if (storeFileChange != null)
                                {
                                    lock (FileChange.UpDownEventLocker)
                                    {
                                        FileChange.UpDownEvent -= storeFileChange.FileChange_UpDown;
                                    }
                                }
                            }
                        },
                        new UploadTaskState()
                        {
                            CompleteSingleEvent = completeSingleEvent,
                            FailureTimer = failureTimer,
                            FileToUpload = toComplete.Key,
                            UploadStream = toComplete.Value,
                            AddChangesToProcessingQueue = AddChangesToProcessingQueue,
                            mergeToSql = mergeToSql
                        }));
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
            public byte[] MD5 { get; set; }
            public Func<string> GetTempFolder { get; set; }
            public Func<FileChange, CLError> ApplySyncFromChange { get; set; }
            public Func<long, CLError> CompleteSingleEvent { get; set; }
            public ProcessingQueuesTimer FailureTimer { get; set; }
            public Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> AddChangesToProcessingQueue { get; set; }
            public Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql { get; set; }
        }
        private class UploadTaskState
        {
            public FileChange FileToUpload { get; set; }
            public FileStream UploadStream { get; set; }
            public Func<long, CLError> CompleteSingleEvent { get; set; }
            public ProcessingQueuesTimer FailureTimer { get; set; }
            public Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> AddChangesToProcessingQueue { get; set; }
            public Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql { get; set; }
        }
        private class AsyncRequestHolder
        {
            public bool IsCanceled
            {
                get
                {
                    return _isCanceled;
                }
            }
            private bool _isCanceled = false;

            public void Cancel()
            {
                _isCanceled = true;
            }

            public Exception Error
            {
                get
                {
                    return _error;
                }
            }
            private Exception _error = null;

            public void MarkException(Exception toMark)
            {
                _error = toMark ?? new NullReferenceException("toMark is null");
                lock (this)
                {
                    Monitor.Pulse(this);
                }
            }
        }

        private static void MakeAsyncRequestSynchronous(IAsyncResult makeSynchronous)
        {
            AsyncRequestHolder castHolder = (AsyncRequestHolder)makeSynchronous.AsyncState;
            try
            {
                if (makeSynchronous.IsCompleted)
                {
                    lock (castHolder)
                    {
                        Monitor.Pulse(castHolder);
                    }
                }
                else
                {
                    Monitor.Enter(FullShutdownToken);
                    try
                    {
                        if (FullShutdownToken.Token.IsCancellationRequested)
                        {
                            castHolder.Cancel();

                            lock (castHolder)
                            {
                                Monitor.Pulse(castHolder);
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(FullShutdownToken);
                    }
                }
            }
            catch (Exception ex)
            {
                castHolder.MarkException(ex);
            }
        }

        private static void MoveCompletedDownload(Func<FileChange, CLError> applySyncFromChange,
            string newTempFileString,
            FileChange completedDownload,
            Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> addChangesToProcessingQueue,
            ref string responseBody,
            ProcessingQueuesTimer failureTimer,
            Guid newTempFile)
        {
            CLError applyError;
            lock (FileChange.UpDownEventLocker)
            {
                applyError = applySyncFromChange(new FileChange()
                {
                    Direction = SyncDirection.From,
                    DoNotAddToSQLIndex = true,
                    Metadata = completedDownload.Metadata,
                    NewPath = completedDownload.NewPath,
                    OldPath = newTempFileString,
                    Type = FileChangeType.Renamed
                });
            }
            if (applyError != null)
            {
                throw applyError.GrabFirstException();
            }
            else
            {
                if ((Settings.Instance.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                {
                    Trace.LogFileChangeFlow(Settings.Instance.TraceLocation, Settings.Instance.Udid, Settings.Instance.Uuid, FileChangeFlowEntryPositionInFlow.UploadDownloadSuccess, new FileChange[] { completedDownload });
                }

                lock (TempDownloads)
                {
                    List<KeyValuePair<Guid, byte[]>> foundSize;
                    if (TempDownloads.TryGetValue((long)completedDownload.Metadata.HashableProperties.Size, out foundSize))
                    {
                        foreach (KeyValuePair<Guid, byte[]> tempDownloadsInSize in foundSize.ToArray())
                        {
                            if (tempDownloadsInSize.Key == newTempFile)
                            {
                                foundSize.Remove(tempDownloadsInSize);
                                break;
                            }
                        }

                        if (foundSize.Count == 0)
                        {
                            TempDownloads.Remove((long)completedDownload.Metadata.HashableProperties.Size);
                        }
                    }
                }
                responseBody = "---Completed file download---";

                FileChangeWithDependencies toCompleteWithDependencies = completedDownload as FileChangeWithDependencies;
                if (toCompleteWithDependencies != null
                    && toCompleteWithDependencies.DependenciesCount > 0)
                {
                    GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>();
                    CLError err = addChangesToProcessingQueue(toCompleteWithDependencies.Dependencies, true, errList);
                    if (errList.Value != null)
                    {
                        bool atLeastOneFailureAdded = false;

                        lock (failureTimer)
                        {
                            foreach (FileChange currentError in errList.Value)
                            {
                                FailedChangesQueue.Enqueue(currentError);

                                atLeastOneFailureAdded = true;
                            }

                            if (atLeastOneFailureAdded)
                            {
                                failureTimer.StartTimerIfNotRunning();
                            }
                        }
                    }
                    if (err != null)
                    {
                        err.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                    }
                }
            }
        }

        private static Exception CommunicateWithServer(IEnumerable<KeyValuePair<FileChange, FileStream>> toCommunicate,
            Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql,
            Func<string> getLastSyncId,
            bool respondingToPushNotification,
            Func<string> getCloudRoot,
            Func<IEnumerable<FileChange>, bool, GenericHolder<List<FileChange>>, CLError> addChangesToProcessingQueue,
            MetadataByPathAndRevision getMetadataByPathAndRevision,
            Func<string> getDeviceName,
            out IEnumerable<KeyValuePair<bool, FileChange>> completedChanges,
            out IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> incompleteChanges,
            out IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>> changesInError,
            out string newSyncId)
        {
            try
            {
                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                        incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                        changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                        newSyncId = Helpers.DefaultForType<string>();
                        return new Exception("Shut down in the middle of communication");
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

                KeyValuePair<FileChange, FileStream>[] communicationArray = (toCommunicate ?? Enumerable.Empty<KeyValuePair<FileChange, FileStream>>()).ToArray();

                FilePathDictionary<FileChange> failuresDict = null;
                Func<FilePathDictionary<FileChange>> getFailuresDict = () =>
                {
                    if (failuresDict == null)
                    {
                        CLError createFailuresDictError = FilePathDictionary<FileChange>.CreateAndInitialize(getCloudRoot(),
                            out failuresDict);
                        if (createFailuresDictError != null)
                        {
                            throw new AggregateException("Error creating failuresDict", createFailuresDictError.GrabExceptions());
                        }
                        lock (GetFailureTimer(addChangesToProcessingQueue).TimerRunningLocker)
                        {
                            foreach (FileChange currentInError in FailedChangesQueue)
                            {
                                failuresDict[currentInError.NewPath] = currentInError;
                            }
                        }
                    }
                    return failuresDict;
                };

                FilePathDictionary<FileChange> runningUpDownChangesDict = null;
                Func<FilePathDictionary<FileChange>> getRunningUpDownChangesDict = () =>
                {
                    if (runningUpDownChangesDict == null)
                    {
                        List<FileChange> runningUpDownChanges = new List<FileChange>();
                        FileChange.RunUnDownEvent(new FileChange.UpDownEventArgs(currentUpDown =>
                            runningUpDownChanges.Add(currentUpDown)));

                        CLError createUpDownDictError = FilePathDictionary<FileChange>.CreateAndInitialize(getCloudRoot(),
                            out runningUpDownChangesDict);
                        if (createUpDownDictError != null)
                        {
                            throw new AggregateException("Error creating upDownDict", createUpDownDictError.GrabExceptions());
                        }
                        foreach (FileChange currentUpDownChange in runningUpDownChanges)
                        {
                            runningUpDownChangesDict[currentUpDownChange.NewPath] = currentUpDownChange;
                        }
                    }
                    return runningUpDownChangesDict;
                };

                string syncString;
                if ((syncString = (getLastSyncId() ?? CLDefinitions.CLDefaultSyncID)) == CLDefinitions.CLDefaultSyncID)
                {
                    JsonContracts.PurgePending purge = new JsonContracts.PurgePending()
                    {
                        DeviceId = Settings.Instance.Udid,
                        UserId = Settings.Instance.Uuid
                    };

                    string requestBody;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        PurgePendingSerializer.WriteObject(ms, purge);
                        requestBody = Encoding.Default.GetString(ms.ToArray());
                    }

                    byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                    HttpWebRequest purgeRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathPurgePending);
                    purgeRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                    purgeRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                    // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                    purgeRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                    purgeRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                    purgeRequest.SendChunked = false;
                    purgeRequest.Timeout = HttpTimeoutMilliseconds;
                    purgeRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson;
                    purgeRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                    purgeRequest.ContentLength = requestBodyBytes.Length;

                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                    {
                        Trace.LogCommunication(Settings.Instance.TraceLocation,
                            Settings.Instance.Udid,
                            Settings.Instance.Uuid,
                            CommunicationEntryDirection.Request,
                            CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathPurgePending,
                            true,
                            purgeRequest.Headers,
                            requestBody,
                            null,
                            Settings.Instance.TraceExcludeAuthorization,
                            purgeRequest.Host,
                            purgeRequest.ContentLength.ToString(),
                            (purgeRequest.Expect == null ? "100-continue" : purgeRequest.Expect),
                            (purgeRequest.KeepAlive ? "Keep-Alive" : "Close"));
                    }

                    using (Stream purgeRequestStream = purgeRequest.GetRequestStream())
                    {
                        purgeRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                    }

                    HttpWebResponse purgeResponse;
                    try
                    {
                        purgeResponse = (HttpWebResponse)purgeRequest.GetResponse();
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response == null)
                        {
                            throw new NullReferenceException("purgeRequest Response cannot be null", ex);
                        }
                        purgeResponse = (HttpWebResponse)ex.Response;
                    }

                    JsonContracts.PurgePendingResponse deserializedResponse;
                    try
                    {
                        using (Stream purgeHttpWebResponseStream = purgeResponse.GetResponseStream())
                        {
                            Stream purgeResponseStream = (((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                ? Helpers.CopyHttpWebResponseStreamAndClose(purgeHttpWebResponseStream)
                                : purgeHttpWebResponseStream);

                            try
                            {
                                if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                {
                                    Trace.LogCommunication(Settings.Instance.TraceLocation,
                                        Settings.Instance.Udid,
                                        Settings.Instance.Uuid,
                                        CommunicationEntryDirection.Response,
                                        CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathPurgePending,
                                        true,
                                        purgeResponse.Headers,
                                        purgeResponseStream,
                                        (int)purgeResponse.StatusCode,
                                        Settings.Instance.TraceExcludeAuthorization);
                                }

                                if (purgeResponse.StatusCode != HttpStatusCode.OK
                                    && purgeResponse.StatusCode != HttpStatusCode.Accepted)
                                {
                                    string purgeResponseString = null;
                                    // Bug in MDS: ContentLength is not set so I cannot read the stream to compare against it
                                    try
                                    {
                                        using (TextReader purgeResponseStreamReader = new StreamReader(purgeResponseStream, Encoding.UTF8))
                                        {
                                            purgeResponseString = purgeResponseStreamReader.ReadToEnd();
                                        }
                                    }
                                    catch
                                    {
                                    }

                                    throw new Exception("Invalid HTTP response status code in Purge Pending: " + ((int)purgeResponse.StatusCode).ToString() +
                                        (purgeResponseString == null ? string.Empty
                                            : Environment.NewLine + "Response:" + Environment.NewLine +
                                            purgeResponseString));
                                }

                                deserializedResponse = (JsonContracts.PurgePendingResponse)PurgePendingResponseSerializer.ReadObject(purgeResponseStream);
                            }
                            finally
                            {
                                if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                {
                                    purgeResponseStream.Dispose();
                                }
                            }
                        }
                    }
                    finally
                    {
                        try
                        {
                            purgeResponse.Close();
                        }
                        catch
                        {
                        }
                    }
                }

                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                        incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                        changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                        newSyncId = Helpers.DefaultForType<string>();
                        return new Exception("Shut down in the middle of communication");
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

                // if there is at least one change to communicate or we have a push notification to communicate anyways,
                // then process communication
                if (communicationArray.Length > 0)
                {
                    #region Sync To
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

                    JsonContracts.To deserializedResponse = null;

                    int totalBatches = (int)Math.Ceiling(((double)communicationArray.Length) / ((double)CLDefinitions.SyncConstantsMaximumSyncToEvents));
                    for (int batchNumber = 0; batchNumber < totalBatches; batchNumber++)
                    {
                        Monitor.Enter(FullShutdownToken);
                        try
                        {
                            if (FullShutdownToken.Token.IsCancellationRequested)
                            {
                                completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                                incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                                changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                                newSyncId = Helpers.DefaultForType<string>();
                                return new Exception("Shut down in the middle of communication");
                            }
                        }
                        finally
                        {
                            Monitor.Exit(FullShutdownToken);
                        }

                        KeyValuePair<FileChange, FileStream>[] currentBatch = new KeyValuePair<FileChange, FileStream>[batchNumber == totalBatches - 1
                            ? communicationArray.Length % CLDefinitions.SyncConstantsMaximumSyncToEvents
                            : CLDefinitions.SyncConstantsMaximumSyncToEvents];
                        Array.Copy(sourceArray: communicationArray,
                            sourceIndex: batchNumber * CLDefinitions.SyncConstantsMaximumSyncToEvents,
                            destinationArray: currentBatch,
                            destinationIndex: 0,
                            length: currentBatch.Length);

                        long lastEventId = currentBatch.OrderByDescending(currentEvent => ensureNonZeroEventId(currentEvent.Key.EventId)).First().Key.EventId;

                        JsonContracts.To syncTo = new JsonContracts.To()
                        {
                            SyncId = syncString,
                            Events = currentBatch.Select(currentEvent => new JsonContracts.Event()
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
                                    Version = "1.0",
                                    TargetPath = (currentEvent.Key.Metadata.LinkTargetPath == null
                                        ? null
                                        : currentEvent.Key.Metadata.LinkTargetPath.GetRelativePath(getCloudRoot(), true))
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

                        string syncToHostAndMethod = CLDefinitions.CLMetaDataServerURL +
                            CLDefinitions.MethodPathSyncTo +
                            Helpers.QueryStringBuilder(new KeyValuePair<string, string>[]
                                {
                                    new KeyValuePair<string, string>(CLDefinitions.QueryStringUserId, Settings.Instance.Uuid)
                                }
                            );

                        HttpWebRequest toRequest = (HttpWebRequest)HttpWebRequest.Create(syncToHostAndMethod);
                        toRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                        toRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                        // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                        toRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                        toRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                        toRequest.SendChunked = false;
                        toRequest.Timeout = HttpTimeoutMilliseconds;
                        toRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson;
                        toRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                        toRequest.ContentLength = requestBodyBytes.Length;

                        if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            Trace.LogCommunication(Settings.Instance.TraceLocation,
                                Settings.Instance.Udid,
                                Settings.Instance.Uuid,
                                CommunicationEntryDirection.Request,
                                syncToHostAndMethod,
                                true,
                                toRequest.Headers,
                                requestBody,
                                null,
                                Settings.Instance.TraceExcludeAuthorization,
                                toRequest.Host,
                                toRequest.ContentLength.ToString(),
                                (toRequest.Expect == null ? "100-continue" : toRequest.Expect),
                                (toRequest.KeepAlive ? "Keep-Alive" : "Close"));
                        }

                        using (Stream toRequestStream = toRequest.GetRequestStream())
                        {
                            toRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                        }

                        HttpWebResponse toResponse;
                        try
                        {
                            toResponse = (HttpWebResponse)toRequest.GetResponse();
                        }
                        catch (WebException ex)
                        {
                            if (ex.Response == null)
                            {
                                throw new NullReferenceException("toRequest Response cannot be null", ex);
                            }
                            toResponse = (HttpWebResponse)ex.Response;
                        }

                        JsonContracts.To currentBatchResponse;
                        try
                        {
                            using (Stream toHttpWebResponseStream = toResponse.GetResponseStream())
                            {
                                Stream toResponseStream = (((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    ? Helpers.CopyHttpWebResponseStreamAndClose(toHttpWebResponseStream)
                                    : toHttpWebResponseStream);

                                try
                                {
                                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    {
                                        Trace.LogCommunication(Settings.Instance.TraceLocation,
                                            Settings.Instance.Udid,
                                            Settings.Instance.Uuid,
                                            CommunicationEntryDirection.Response,
                                            syncToHostAndMethod,
                                            true,
                                            toResponse.Headers,
                                            toResponseStream,
                                            (int)toResponse.StatusCode,
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

                                    currentBatchResponse = (JsonContracts.To)ToSerializer.ReadObject(toResponseStream);
                                }
                                finally
                                {
                                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    {
                                        toResponseStream.Dispose();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            try
                            {
                                toResponse.Close();
                            }
                            catch
                            {
                            }
                        }

                        if (currentBatchResponse.Events == null)
                        {
                            throw new NullReferenceException("Invalid HTTP response body in Sync To, Events cannot be null");
                        }
                        if (currentBatchResponse.SyncId == null)
                        {
                            throw new NullReferenceException("Invalid HTTP response body in Sync To, SyncId cannot be null");
                        }

                        syncString = currentBatchResponse.SyncId;

                        if (deserializedResponse == null)
                        {
                            deserializedResponse = currentBatchResponse;
                        }
                        else
                        {
                            JsonContracts.Event[] previousEvents = deserializedResponse.Events;
                            JsonContracts.Event[] newEvents = currentBatchResponse.Events;

                            deserializedResponse = currentBatchResponse;
                            deserializedResponse.Events = new JsonContracts.Event[previousEvents.Length + newEvents.Length];
                            previousEvents.CopyTo(deserializedResponse.Events, 0);
                            newEvents.CopyTo(deserializedResponse.Events, previousEvents.Length);
                        }
                    }

                    if (deserializedResponse == null)
                    {
                        throw new NullReferenceException("After all Sync To batches, deserializedResponse cannot be null");
                    }
                    else
                    {
                        newSyncId = deserializedResponse.SyncId;
                    }

                    List<int> duplicatedEvents = new List<int>();
                    if (deserializedResponse.Events.Length > 0)
                    {
                        List<int> fromEvents = new List<int>();
                        HashSet<FilePath> eventsByPath = new HashSet<FilePath>(FilePathComparer.Instance);
                        for (int currentEventIndex = 0; currentEventIndex < deserializedResponse.Events.Length; currentEventIndex++)
                        {
                            try
                            {
                                JsonContracts.Event currentEvent = deserializedResponse.Events[currentEventIndex];

                                if (string.IsNullOrEmpty(currentEvent.Header.Status))
                                {
                                    fromEvents.Add(currentEventIndex);
                                }
                                else if (currentEvent.Header.Status != CLDefinitions.CLEventTypeDownload)// exception for download when looking for dependencies since we actually want the Sync From event
                                {
                                    eventsByPath.Add(getCloudRoot() + "\\" + (currentEvent.Metadata.RelativePath ?? currentEvent.Metadata.RelativeToPath).Replace('/', '\\'));
                                }
                            }
                            catch
                            {
                            }
                        }
                        foreach (int currentEventIndex in fromEvents)
                        {
                            try
                            {
                                if (eventsByPath.Contains(getCloudRoot() + "\\" + (deserializedResponse.Events[currentEventIndex].Metadata.RelativePath ?? deserializedResponse.Events[currentEventIndex].Metadata.RelativeToPath).Replace('/', '\\')))
                                {
                                    duplicatedEvents.Add(currentEventIndex);
                                }
                            }
                            catch
                            {
                            }
                        }
                        duplicatedEvents.Sort();
                    }

                    Dictionary<long, KeyValuePair<bool, FileChange>[]> completedChangesList = new Dictionary<long, KeyValuePair<bool, FileChange>[]>();
                    Dictionary<long, List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>> incompleteChangesList = new Dictionary<long, List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                    Dictionary<long, KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]> changesInErrorList = new Dictionary<long, KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]>();
                    HashSet<FileStream> completedStreams = new HashSet<FileStream>();
                    for (int currentEventIndex = 0; currentEventIndex < deserializedResponse.Events.Length; currentEventIndex++)
                    {
                        Monitor.Enter(FullShutdownToken);
                        try
                        {
                            if (FullShutdownToken.Token.IsCancellationRequested)
                            {
                                completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                                incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                                changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                                newSyncId = Helpers.DefaultForType<string>();
                                return new Exception("Shut down in the middle of communication");
                            }
                        }
                        finally
                        {
                            Monitor.Exit(FullShutdownToken);
                        }

                        if (duplicatedEvents.BinarySearch(currentEventIndex) < 0)
                        {
                            JsonContracts.Event currentEvent = deserializedResponse.Events[currentEventIndex];
                            FileChangeWithDependencies currentChange = null;
                            FileStream currentStream = null;
                            string previousRevisionOnConflictException = null;
                            try
                            {
                                currentChange = CreateFileChangeFromBaseChangePlusHash(new FileChange()
                                {
                                    Direction = (string.IsNullOrEmpty(currentEvent.Header.Status) ? SyncDirection.From : SyncDirection.To),
                                    EventId = currentEvent.Header.EventId ?? 0,
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

                                if (matchedChange != null
                                    && ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata != null)
                                {
                                    previousRevisionOnConflictException = ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.Revision;
                                }

                                currentChange.Metadata = new FileMetadata(matchedChange == null ? null : ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.RevisionChanger)
                                {
                                    HashableProperties = new FileMetadataHashableProperties(currentEvent.Metadata.IsFolder ?? ParseEventStringToIsFolder(currentEvent.Header.Action ?? currentEvent.Action),
                                        currentEvent.Metadata.ModifiedDate,
                                        currentEvent.Metadata.CreatedDate,
                                        currentEvent.Metadata.Size),
                                    LinkTargetPath = (string.IsNullOrEmpty(currentEvent.Metadata.TargetPath)
                                        ? null
                                        : getCloudRoot() + "\\" + currentEvent.Metadata.TargetPath.Replace("/", "\\")),
                                    Revision = currentEvent.Metadata.Revision,
                                    StorageKey = currentEvent.Metadata.StorageKey
                                };

                                if (matchedChange != null)
                                {
                                    currentStream = ((KeyValuePair<FileChange, FileStream>)matchedChange).Value;
                                }

                                if (currentChange.Type == FileChangeType.Renamed)
                                {
                                    if (currentChange.OldPath == null)
                                    {
                                        throw new NullReferenceException("OldPath cannot be null if currentChange is of Type Renamed");
                                    }

                                    FileChange originalMetadata = null;
                                    if (matchedChange == null)
                                    {
                                        if (string.IsNullOrEmpty(currentChange.Metadata.Revision))
                                        {
                                            throw new NullReferenceException("Revision cannot be null if currentChange is of Type Renamed and matchedChange is also null");
                                        }

                                        FileChange foundOldPath;
                                        foreach (FileChange findMetadata in (getRunningUpDownChangesDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                                ? new FileChange[] { foundOldPath }
                                                : Enumerable.Empty<FileChange>())
                                            .Concat(getFailuresDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                                ? new FileChange[] { foundOldPath }
                                                : Enumerable.Empty<FileChange>())
                                            .Concat(communicationArray.Where(currentCommunication =>
                                                    FilePathComparer.Instance.Equals(currentCommunication.Key.NewPath, currentChange.OldPath))
                                                .Select(currentCommunication => currentCommunication.Key))
                                            .OrderByDescending(currentOldPath => currentOldPath.EventId))
                                        {
                                            if (findMetadata.Metadata.Revision == currentChange.Metadata.Revision)
                                            {
                                                originalMetadata = findMetadata;
                                            }
                                        }

                                        if (originalMetadata == null)
                                        {
                                            FileMetadata syncStateMetadata;
                                            CLError queryMetadataError = getMetadataByPathAndRevision(currentChange.OldPath.ToString(),
                                                currentChange.Metadata.Revision,
                                                out syncStateMetadata);
                                            if (queryMetadataError != null)
                                            {
                                                throw new AggregateException("Error querying SqlIndexer for sync state by path: " + currentChange.OldPath.ToString() +
                                                    " and revision: " + currentChange.Metadata.Revision);
                                            }
                                            if (syncStateMetadata == null)
                                            {
                                                throw new NullReferenceException("syncStateMetadata must be found by getMetadataByPathAndRevision");
                                            }

                                            originalMetadata = new FileChange()
                                            {
                                                NewPath = "Z:\\NotAProperFileChange",
                                                Metadata = syncStateMetadata
                                            };
                                        }
                                    }
                                    else
                                    {
                                        originalMetadata = ((KeyValuePair<FileChange, FileStream>)matchedChange).Key;
                                    }

                                    currentChange.Metadata = originalMetadata.Metadata;
                                }

                                bool sameCreationTime = false;
                                bool sameLastTime = false;

                                bool metadataIsDifferent = (matchedChange == null)
                                    || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Type != currentChange.Type
                                    || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.IsFolder != currentChange.Metadata.HashableProperties.IsFolder
                                    || !((((KeyValuePair<FileChange, FileStream>)matchedChange).Key.NewPath == null && currentChange.NewPath == null)
                                        || (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.NewPath != null && currentChange.NewPath != null && FilePathComparer.Instance.Equals(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.NewPath, currentChange.NewPath)))
                                    || !((((KeyValuePair<FileChange, FileStream>)matchedChange).Key.OldPath == null && currentChange.OldPath == null)
                                        || (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.OldPath != null && currentChange.OldPath != null && FilePathComparer.Instance.Equals(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.OldPath, currentChange.OldPath)))
                                    || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.Revision != currentChange.Metadata.Revision
                                    || (currentChange.Type != FileChangeType.Renamed
                                        && (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Direction != currentChange.Direction
                                            || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.Size != currentChange.Metadata.HashableProperties.Size
                                            || ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.StorageKey != currentChange.Metadata.StorageKey
                                            || !((((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.LinkTargetPath == null && currentChange.Metadata.LinkTargetPath == null)
                                                || (((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.LinkTargetPath != null && currentChange.Metadata.LinkTargetPath != null && FilePathComparer.Instance.Equals(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.LinkTargetPath, currentChange.Metadata.LinkTargetPath)))
                                            || !(sameCreationTime = Helpers.DateTimesWithinOneSecond(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.CreationTime, currentChange.Metadata.HashableProperties.CreationTime))
                                            || !(sameLastTime = Helpers.DateTimesWithinOneSecond(((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.LastTime, currentChange.Metadata.HashableProperties.LastTime))));

                                FileChangeWithDependencies castMatchedChange;
                                if (metadataIsDifferent)
                                {
                                    currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);

                                    if (matchedChange != null)
                                    {
                                        if ((castMatchedChange = ((KeyValuePair<FileChange, FileStream>)matchedChange).Key as FileChangeWithDependencies) != null)
                                        {
                                            foreach (FileChange matchedDependency in castMatchedChange.Dependencies)
                                            {
                                                currentChange.AddDependency(matchedDependency);
                                            }
                                        }

                                        if (sameCreationTime || sameLastTime)
                                        {
                                            currentChange.Metadata.HashableProperties = new FileMetadataHashableProperties(currentChange.Metadata.HashableProperties.IsFolder,
                                                (sameLastTime ? ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.LastTime : currentChange.Metadata.HashableProperties.LastTime),
                                                (sameCreationTime ? ((KeyValuePair<FileChange, FileStream>)matchedChange).Key.Metadata.HashableProperties.CreationTime : currentChange.Metadata.HashableProperties.CreationTime),
                                                currentChange.Metadata.HashableProperties.Size);
                                        }
                                    }
                                }
                                else
                                {
                                    if ((castMatchedChange = ((KeyValuePair<FileChange, FileStream>)matchedChange).Key as FileChangeWithDependencies) != null)
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
                                            case CLDefinitions.CLEventTypeNotFound:
                                            case CLDefinitions.CLEventTypeDownload:
                                                if (currentEvent.Header.Status == CLDefinitions.CLEventTypeNotFound
                                                    && currentChange.Type != FileChangeType.Deleted)
                                                {
                                                    currentChange.Type = FileChangeType.Created;
                                                    currentChange.OldPath = null;
                                                    currentChange.Metadata.Revision = null;
                                                    currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);
                                                    currentChange.Metadata.StorageKey = null;

                                                    KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>> notFoundChange = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>(true,
                                                        new KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>(currentChange,
                                                            new KeyValuePair<FileStream, Exception>(currentStream,
                                                                new Exception(CLDefinitions.CLEventTypeNotFound + " " +
                                                                        (currentEvent.Header.Action ?? currentEvent.Action) +
                                                                        " " + currentChange.EventId + " " + currentChange.NewPath.ToString()))));

                                                    if (changesInErrorList.ContainsKey(currentChange.EventId))
                                                    {
                                                        KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] previousErrors = changesInErrorList[currentChange.EventId];
                                                        KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[] newErrors = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[previousErrors.Length + 1];
                                                        previousErrors.CopyTo(newErrors, 0);
                                                        newErrors[previousErrors.Length] = notFoundChange;
                                                        changesInErrorList[currentChange.EventId] = newErrors;
                                                    }
                                                    else
                                                    {
                                                        changesInErrorList.Add(currentChange.EventId,
                                                            new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>[]
                                                            {
                                                                notFoundChange
                                                            });
                                                    }
                                                }
                                                else
                                                {
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
                                                }
                                                break;
                                            case CLDefinitions.CLEventTypeConflict:
                                                FilePath originalConflictPath = currentChange.NewPath;

                                                Exception innerExceptionAppend = null;

                                                try
                                                {
                                                    Func<string, string, KeyValuePair<bool, string>> getNextName = (extension, mainName) =>
                                                    {
                                                        if (mainName.Length > 0
                                                            && mainName[mainName.Length - 1] == ')')
                                                        {
                                                            int numDigits = 0;
                                                            while (mainName.Length > numDigits + 2
                                                                && numDigits < 11)
                                                            {
                                                                if (char.IsDigit(mainName[mainName.Length - 2 - numDigits]))
                                                                {
                                                                    numDigits++;
                                                                }
                                                                else if (numDigits > 0
                                                                    && mainName[mainName.Length - 2 - numDigits] == '('
                                                                    && mainName[mainName.Length - 3 - numDigits] == ' ')
                                                                {
                                                                    string numPortion = mainName.Substring(mainName.Length - 1 - numDigits, numDigits);
                                                                    int numPortionParsed;
                                                                    if (int.TryParse(numPortion, out numPortionParsed)
                                                                        && numPortionParsed != int.MaxValue)
                                                                    {
                                                                        mainName = mainName.Substring(0, mainName.Length - 3 - numDigits);
                                                                    }

                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        int highestNumFound = 0;
                                                        foreach (string currentSibling in Directory.EnumerateFiles(originalConflictPath.Parent.ToString()))
                                                        {
                                                            if (currentSibling.Equals(mainName + extension, StringComparison.InvariantCultureIgnoreCase))
                                                            {
                                                                if (highestNumFound == 0)
                                                                {
                                                                    highestNumFound = 1;
                                                                }
                                                            }
                                                            else if (currentSibling.StartsWith(mainName + " (", StringComparison.InvariantCultureIgnoreCase)
                                                                && currentSibling.EndsWith(")" + extension, StringComparison.InvariantCultureIgnoreCase))
                                                            {
                                                                string siblingNumberPortion = currentSibling.Substring(mainName.Length + 2,
                                                                    currentSibling.Length - mainName.Length - extension.Length - 3);
                                                                int siblingNumberParsed;
                                                                if (int.TryParse(siblingNumberPortion, out siblingNumberParsed))
                                                                {
                                                                    if (siblingNumberParsed > highestNumFound)
                                                                    {
                                                                        highestNumFound = siblingNumberParsed;
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        return new KeyValuePair<bool, string>(highestNumFound == int.MaxValue,
                                                            mainName + " (" + (highestNumFound == int.MaxValue ? highestNumFound : highestNumFound + 1).ToString() + ")");
                                                    };

                                                    string findMainName;
                                                    string findExtension;

                                                    int extensionIndex = originalConflictPath.Name.LastIndexOf('.');
                                                    if (extensionIndex == -1)
                                                    {
                                                        findExtension = string.Empty;
                                                        findMainName = originalConflictPath.Name;
                                                    }
                                                    else
                                                    {
                                                        findExtension = currentChange.NewPath.Name.Substring(extensionIndex);
                                                        findMainName = currentChange.NewPath.Name.Substring(0, extensionIndex);
                                                    }

                                                    string deviceAppend = " CONFLICT " + getDeviceName();
                                                    string finalizedMainName;

                                                    if (findMainName.IndexOf(deviceAppend, 0, StringComparison.InvariantCultureIgnoreCase) == -1)
                                                    {
                                                        finalizedMainName = findMainName + deviceAppend;
                                                    }
                                                    else
                                                    {
                                                        KeyValuePair<bool, string> mainNameIteration = new KeyValuePair<bool, string>(true, findMainName);

                                                        while (mainNameIteration.Key)
                                                        {
                                                            mainNameIteration = getNextName(findExtension, mainNameIteration.Value);
                                                        }

                                                        finalizedMainName = mainNameIteration.Value;
                                                    }

                                                    FileChangeType storeType = currentChange.Type;
                                                    FilePath storePath = currentChange.NewPath;

                                                    try
                                                    {
                                                        currentChange.Type = FileChangeType.Created;
                                                        currentChange.NewPath = new FilePath(finalizedMainName + findExtension, originalConflictPath.Parent);
                                                        currentChange.Metadata.Revision = null;
                                                        currentChange.Metadata.StorageKey = null;
                                                        currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);

                                                        FileChangeWithDependencies reparentConflict;
                                                        CLError reparentCreateError = FileChangeWithDependencies.CreateAndInitialize(new FileChange()
                                                        {
                                                            Direction = SyncDirection.From,
                                                            Metadata = new FileMetadata()
                                                            {
                                                                HashableProperties = currentChange.Metadata.HashableProperties,
                                                                LinkTargetPath = currentChange.Metadata.LinkTargetPath
                                                            },
                                                            NewPath = currentChange.NewPath,
                                                            OldPath = originalConflictPath,
                                                            Type = FileChangeType.Renamed
                                                        }, new FileChange[] { currentChange },
                                                        out reparentConflict);

                                                        if (reparentCreateError != null)
                                                        {
                                                            throw new AggregateException("Error creating reparentConflict", reparentCreateError.GrabExceptions());
                                                        }

                                                        mergeToSql(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(null, currentChange) }, false);
                                                        currentChange.EventId = 0;
                                                        mergeToSql(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(reparentConflict, null) }, false);
                                                        mergeToSql(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(currentChange, null) }, false);

                                                        currentChange = reparentConflict;
                                                        metadataIsDifferent = false;
                                                    }
                                                    catch
                                                    {
                                                        currentChange.Type = storeType;
                                                        currentChange.NewPath = storePath;
                                                        throw;
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    currentChange.Metadata.Revision = previousRevisionOnConflictException;
                                                    currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);

                                                    innerExceptionAppend = new Exception("Error creating local rename to apply for conflict", ex);
                                                }

                                                KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>> addErrorChange = new KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>(metadataIsDifferent,
                                                    new KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>(currentChange,
                                                        new KeyValuePair<FileStream, Exception>(currentStream,
                                                            new Exception(CLDefinitions.CLEventTypeConflict + " " +
                                                                    (currentEvent.Header.Action ?? currentEvent.Action) +
                                                                    " " + currentChange.EventId + " " + originalConflictPath.ToString(),
                                                                innerExceptionAppend))));

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
                                                throw new ArgumentException("Unknown SyncHeader Status: " + currentEvent.Header.Status);
                                        }
                                        break;
                                    default:
                                        throw new ArgumentException("Unknown SyncDirection in currentChange: " + currentChange.Direction.ToString());
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
                    }

                    foreach (KeyValuePair<FileChange, FileStream> currentOriginalChangeToFind in communicationArray)
                    {
                        bool foundEventId = false;
                        bool foundMatchedStream = false;
                        List<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>> tryGetIncompletes;
                        if (incompleteChangesList.TryGetValue(currentOriginalChangeToFind.Key.EventId, out tryGetIncompletes))
                        {
                            foundEventId = true;
                            if (currentOriginalChangeToFind.Value != null
                                && !foundMatchedStream)
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
                            if (currentOriginalChangeToFind.Value != null
                                && !foundMatchedStream)
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
                            if (currentOriginalChangeToFind.Value != null
                                && !foundMatchedStream)
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
                        else if (currentOriginalChangeToFind.Value != null
                            && !foundMatchedStream)
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
                    #endregion
                }
                else
                {
                    if (respondingToPushNotification)
                    {
                        #region Sync From
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
                                    LastSyncId = syncString
                                });
                            requestBody = Encoding.Default.GetString(ms.ToArray());
                        }

                        byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                        HttpWebRequest pushRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom);
                        pushRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                        pushRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient;
                        // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                        pushRequest.Headers[CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersionHeaderName] = CloudApiPrivate.Model.CLPrivateDefinitions.CLClientVersion;
                        pushRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendToken + CLDefinitions.WrapInDoubleQuotes(Settings.Instance.Akey);
                        pushRequest.SendChunked = false;
                        pushRequest.Timeout = HttpTimeoutMilliseconds;
                        pushRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson;
                        pushRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding;
                        pushRequest.ContentLength = requestBodyBytes.Length;

                        if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            Trace.LogCommunication(Settings.Instance.TraceLocation,
                                Settings.Instance.Udid,
                                Settings.Instance.Uuid,
                                CommunicationEntryDirection.Request,
                                CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom,
                                true,
                                pushRequest.Headers,
                                requestBody,
                                null,
                                Settings.Instance.TraceExcludeAuthorization,
                                pushRequest.Host,
                                pushRequest.ContentLength.ToString(),
                                (pushRequest.Expect == null ? "100-continue" : pushRequest.Expect),
                                (pushRequest.KeepAlive ? "Keep-Alive" : "Close"));
                        }

                        using (Stream pushRequestStream = pushRequest.GetRequestStream())
                        {
                            pushRequestStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                        }

                        HttpWebResponse pushResponse;
                        try
                        {
                            pushResponse = (HttpWebResponse)pushRequest.GetResponse();
                        }
                        catch (WebException ex)
                        {
                            if (ex.Response == null)
                            {
                                throw new NullReferenceException("pushRequest Response cannot be null", ex);
                            }
                            pushResponse = (HttpWebResponse)ex.Response;
                        }

                        JsonContracts.PushResponse deserializedResponse;
                        try
                        {
                            using (Stream pushHttpWebResponseStream = pushResponse.GetResponseStream())
                            {
                                Stream pushResponseStream = (((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    ? Helpers.CopyHttpWebResponseStreamAndClose(pushHttpWebResponseStream)
                                    : pushHttpWebResponseStream);

                                try
                                {
                                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    {
                                        Trace.LogCommunication(Settings.Instance.TraceLocation,
                                            Settings.Instance.Udid,
                                            Settings.Instance.Uuid,
                                            CommunicationEntryDirection.Response,
                                            CLDefinitions.CLMetaDataServerURL + CLDefinitions.MethodPathSyncFrom,
                                            true,
                                            pushResponse.Headers,
                                            pushResponseStream,
                                            (int)pushResponse.StatusCode,
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
                                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                                    {
                                        pushResponseStream.Dispose();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            try
                            {
                                pushResponse.Close();
                            }
                            catch
                            {
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
                                StorageKey = currentEvent.Metadata.StorageKey,
                                LinkTargetPath = (currentEvent.Metadata.TargetPath == null
                                    ? null
                                    : getCloudRoot() + "\\" + currentEvent.Metadata.TargetPath.Replace("/", "\\"))
                            }
                        },
                        currentEvent.Metadata.Hash), null)))
                        .ToArray();

                        foreach (FileChange currentChange in incompleteChanges
                            .Where(incompleteChange => incompleteChange.Value.Key.Type == FileChangeType.Renamed)
                            .Select(incompleteChange => incompleteChange.Value.Key))
                        {
                            Monitor.Enter(FullShutdownToken);
                            try
                            {
                                if (FullShutdownToken.Token.IsCancellationRequested)
                                {
                                    completedChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, FileChange>>>();
                                    incompleteChanges = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, FileStream>>>>();
                                    changesInError = Helpers.DefaultForType<IEnumerable<KeyValuePair<bool, KeyValuePair<FileChange, KeyValuePair<FileStream, Exception>>>>>();
                                    newSyncId = Helpers.DefaultForType<string>();
                                    return new Exception("Shut down in the middle of communication");
                                }
                            }
                            finally
                            {
                                Monitor.Exit(FullShutdownToken);
                            }

                            if (currentChange.OldPath == null)
                            {
                                throw new NullReferenceException("OldPath cannot be null if currentChange is of Type Renamed");
                            }

                            FileChange originalMetadata = null;
                            FileChange foundOldPath;
                            foreach (FileChange findMetadata in (getRunningUpDownChangesDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                    ? new FileChange[] { foundOldPath }
                                    : Enumerable.Empty<FileChange>())
                                .Concat(getFailuresDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                    ? new FileChange[] { foundOldPath }
                                    : Enumerable.Empty<FileChange>())
                                .OrderByDescending(currentOldPath => currentOldPath.EventId))
                            {
                                if (findMetadata.Metadata.Revision == currentChange.Metadata.Revision)
                                {
                                    originalMetadata = findMetadata;
                                }
                            }

                            if (originalMetadata == null)
                            {
                                FileMetadata syncStateMetadata;
                                CLError queryMetadataError = getMetadataByPathAndRevision(currentChange.OldPath.ToString(),
                                    currentChange.Metadata.Revision,
                                    out syncStateMetadata);
                                if (queryMetadataError != null)
                                {
                                    throw new AggregateException("Error querying SqlIndexer for sync state by path: " + currentChange.OldPath.ToString() +
                                        " and revision: " + currentChange.Metadata.Revision);
                                }
                                if (syncStateMetadata == null)
                                {
                                    throw new NullReferenceException("syncStateMetadata must be found by getMetadataByPathAndRevision");
                                }

                                originalMetadata = new FileChange()
                                {
                                    NewPath = "Z:\\NotAProperFileChange",
                                    Metadata = syncStateMetadata
                                };
                            }

                            currentChange.Metadata = originalMetadata.Metadata;
                        }
                        #endregion
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

        private static bool ParseEventStringToIsFolder(string actionString)
        {
            return CLDefinitions.SyncHeaderIsFolders.Contains(actionString);
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

        private static bool ContinueToRetry(Func<IEnumerable<KeyValuePair<FileChange, FileChange>>, bool, CLError> mergeToSql, FileChange toRetry, bool existingError)
        {
            if (existingError)
            {
                return true;
            }
            else
            {
                if (toRetry == null)
                {
                    throw new NullReferenceException("toRetry cannot be null");
                }

                if (toRetry.FailureCounter == 0)
                {
                    IconOverlay.QueueSetBadge(cloudAppIconBadgeType.cloudAppBadgeFailed,
                        ((toRetry.Direction == SyncDirection.From && toRetry.Type == FileChangeType.Renamed)
                            ? toRetry.OldPath
                            : toRetry.NewPath));
                }

                toRetry.FailureCounter++;

                if (toRetry.FailureCounter < MaxNumberOfFailureRetries
                    && toRetry.NotFoundForStreamCounter < MaxNumberOfNotFounds)
                {
                    return true;
                }

                if (toRetry.NotFoundForStreamCounter == MaxNumberOfNotFounds)
                {
                    IconOverlay.QueueSetBadge(cloudAppIconBadgeType.cloudAppBadgeSynced, toRetry.NewPath);
                    if (mergeToSql != null)
                    {
                        mergeToSql(new KeyValuePair<FileChange, FileChange>[] { new KeyValuePair<FileChange, FileChange>(null, toRetry) }, false);
                    }
                }

                FileChangeWithDependencies castRetry = toRetry as FileChangeWithDependencies;
                if (castRetry != null
                    && castRetry.DependenciesCount > 0)
                {
                    BadgeDependenciesAsFailures(castRetry.Dependencies);
                }

                return false;
            }
        }

        private static void BadgeDependenciesAsFailures(IEnumerable<FileChange> dependencies)
        {
            foreach (FileChange dependency in (dependencies ?? Enumerable.Empty<FileChange>()))
            {
                IconOverlay.QueueSetBadge(cloudAppIconBadgeType.cloudAppBadgeFailed,
                    ((dependency.Direction == SyncDirection.From && dependency.Type == FileChangeType.Renamed)
                        ? dependency.OldPath
                        : dependency.NewPath));

                FileChangeWithDependencies castDependency = dependency as FileChangeWithDependencies;
                if (castDependency != null
                    && castDependency.DependenciesCount > 0)
                {
                    BadgeDependenciesAsFailures(castDependency.Dependencies);
                }
            }
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
        //private static DataContractJsonSerializer EventSerializer
        //{
        //    get
        //    {
        //        lock (EventSerializerLocker)
        //        {
        //            return _eventSerializer
        //                ?? (_eventSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Event)));
        //        }
        //    }
        //}
        //private static DataContractJsonSerializer _eventSerializer = null;
        //private static readonly object EventSerializerLocker = new object();
        private static DataContractJsonSerializer NotificationResponseSerializer
        {
            get
            {
                lock (NotificationResponseSerializerLocker)
                {
                    return _notificationResponseSerializer
                        ?? (_notificationResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.NotificationResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _notificationResponseSerializer = null;
        private static readonly object NotificationResponseSerializerLocker = new object();
        public static string NotificationResponseToJSON(JsonContracts.NotificationResponse notificationResponse)
        {
            using (MemoryStream stringStream = new MemoryStream())
            {
                NotificationResponseSerializer.WriteObject(stringStream, notificationResponse);
                stringStream.Position = 0;
                using (StreamReader stringReader = new StreamReader(stringStream))
                {
                    return stringReader.ReadToEnd();
                }
            }
        }
        public static JsonContracts.NotificationResponse ParseNotificationResponse(string notificationResponse)
        {
            MemoryStream stringStream = null;
            try
            {
                stringStream = new MemoryStream(Encoding.Unicode.GetBytes(notificationResponse));
                return (JsonContracts.NotificationResponse)NotificationResponseSerializer.ReadObject(stringStream);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (stringStream != null)
                {
                    stringStream.Dispose();
                }
            }
        }
        private static DataContractJsonSerializer PurgePendingSerializer
        {
            get
            {
                lock (PurgePendingSerializerLocker)
                {
                    return _purgePendingSerializer
                        ?? (_purgePendingSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PurgePending)));
                }
            }
        }
        private static DataContractJsonSerializer _purgePendingSerializer = null;
        private static readonly object PurgePendingSerializerLocker = new object();
        private static DataContractJsonSerializer PurgePendingResponseSerializer
        {
            get
            {
                lock (PurgePendingResponseSerializerLocker)
                {
                    return _purgePendingResponseSerializer
                        ?? (_purgePendingResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PurgePendingResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _purgePendingResponseSerializer = null;
        private static readonly object PurgePendingResponseSerializerLocker = new object();

        #endregion
    }
}