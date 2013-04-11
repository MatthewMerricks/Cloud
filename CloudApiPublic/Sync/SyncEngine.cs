//
// SyncEngine.cs
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
using System.Threading;
using System.Security.Cryptography;
using Cloud.Model;
using Cloud.Static;
using Cloud.Support;
using Cloud.Interfaces;
using Cloud.JsonContracts;
using Cloud.REST;
using Cloud.Model.EventMessages.ErrorInfo;

namespace Cloud.Sync
{
    /// <summary>
    /// Processes events between an input event source (ISyncDataObject) such as FileMonitor and the server with callbacks for grabbing, rearranging, or updating events; also fires global event callbacks with status
    /// </summary>
    internal sealed class SyncEngine : IDisposable
    {
        #region instance fields, all readonly
        // locker to prevent multiple simultaneous processing of the same SyncEngine, also storing whether Run has fired before for special initial upload/download processing
        private readonly GenericHolder<bool> RunLocker = new GenericHolder<bool>(false);
        // holder to whether this SyncEngine has faulted on server connection failure
        private readonly GenericHolder<bool> HaltedOnServerConnectionFailure = new GenericHolder<bool>(false);
        // store event source
        private readonly ISyncDataObject syncData;
        // callback to fire upon any status change
        private readonly System.Threading.WaitCallback statusUpdated;
        // userstate to pass to status change callback
        private readonly object statusUpdatedUserState;
        // store settings source
        private readonly CLSyncBox syncBox;
        // store client for Http REST communication
        private readonly CLHttpRest httpRestClient;
        // time to wait before presuming communication failure
        private readonly int HttpTimeoutMilliseconds;
        // number of times to retry a event dependency tree before stopping
        private readonly byte MaxNumberOfFailureRetries;
        // max number of consecutive server connection failures to a particular domain before halting the engine
        private readonly byte MaxNumberOfServerConnectionFailures;
        // number of times to retry an event that receives a not found error before presuming the event was cancelled out
        private readonly byte MaxNumberOfNotFounds;
        // time to wait between retrying queued failures
        private readonly int ErrorProcessingMillisecondInterval;
        // holder for consecutive count of failures communicating with the upload/download server
        private readonly GenericHolder<byte> UploadDownloadConnectionFailures = new GenericHolder<byte>(0);
        // holder for consecutive count of failures communicating with the metadata server
        private readonly GenericHolder<byte> MetadataConnectionFailures = new GenericHolder<byte>(0);
        // time to wait to take everything that has failed out and retry
        private readonly int FailedOutRetryMillisecondInterval;

        private readonly bool DependencyDebugging;
        #endregion

        #region is internet connected
        public static bool InternetConnected
        {
            get
            {
                lock (InternetConnectedLocker)
                {
                    return true;
                    //TODO: Debug on SkyNet and revert to this statement: return _internetConnected;
                }
            }
            set
            {
                lock (InternetConnectedLocker)
                {
                    _internetConnected = value;
                }
            }
        }
        private static bool _internetConnected = false;
        private static readonly object InternetConnectedLocker = new object();
        #endregion

        private readonly GenericHolder<CredentialErrorType> CredentialErrorDetected = new GenericHolder<CredentialErrorType>(CredentialErrorType.NoError);
        private enum CredentialErrorType : byte
        {
            NoError,
            ExpiredCredentials,
            OtherError
        }

        /// <summary>
        /// Engine constructor
        /// </summary>
        /// <param name="syncData">Event source</param>
        /// <param name="syncBox">SyncBox to sync</param>
        /// <param name="httpRestClient">Http client for REST communication</param>
        /// <param name="engine">(output) Created SyncEngine</param>
        /// <param name="statusUpdated">(optional) Callback to fire upon update of the running status</param>
        /// <param name="statusUpdatedUserState">(optional) Userstate to pass to the statusUpdated callback</param>
        /// <param name="HttpTimeoutMilliseconds">(optional) Milliseconds to wait before presuming communication failure</param>
        /// <param name="MaxNumberOfFailureRetries">(optional) Number of times to retry an event dependency tree before stopping</param>
        /// <param name="MaxNumberOfNotFounds">(optional) Number of times to retry an event that keeps getting a not found error before presuming the event was cancelled out, should be less than MaxNumberOfFailureRetries</param>
        /// <param name="ErrorProcessingMillisecondInterval">(optional) Milliseconds to delay between each attempt at reprocessing queued failures</param>
        /// <returns>Returns any error that occurred creating the SyncEngine, if any</returns>
        public static CLError CreateAndInitialize(
            ISyncDataObject syncData,
            CLSyncBox syncBox,
            CLHttpRest httpRestClient,
            out SyncEngine engine,
            bool DependencyDebugging,
            System.Threading.WaitCallback statusUpdated = null,
            object statusUpdatedUserState = null,
            int HttpTimeoutMilliseconds = CLDefinitions.HttpTimeoutDefaultMilliseconds,
            byte MaxNumberOfFailureRetries = 20,
            byte MaxNumberOfNotFounds = 3,
            int ErrorProcessingMillisecondInterval = 10000,// wait ten seconds between processing
            byte MaxNumberConnectionFailures = 40,
            int FailedOutRetryMillisecondInterval = 14400000) // wait four hours between retrying failed out changes
        {
            try
            {
                engine = new SyncEngine(
                    syncData,
                    syncBox,
                    httpRestClient,
                    DependencyDebugging,
                    statusUpdated,
                    statusUpdatedUserState,
                    HttpTimeoutMilliseconds,
                    MaxNumberOfFailureRetries,
                    MaxNumberOfNotFounds,
                    ErrorProcessingMillisecondInterval,
                    MaxNumberConnectionFailures,
                    FailedOutRetryMillisecondInterval);
            }
            catch (Exception ex)
            {
                engine = Helpers.DefaultForType<SyncEngine>();
                return ex;
            }
            return null;
        }

        public SyncEngine(ISyncDataObject syncData,
            CLSyncBox syncBox,
            CLHttpRest httpRestClient,
            bool DependencyDebugging,
            System.Threading.WaitCallback statusUpdated,
            object statusUpdatedUserState,
            int HttpTimeoutMilliseconds,
            byte MaxNumberOfFailureRetries,
            byte MaxNumberOfNotFounds,
            int ErrorProcessingMillisecondInterval,
            byte MaxNumberConnectionFailures,
            int FailedOutRetryMillisecondInterval)
        {
            #region validate parameters
            if (syncData == null)
            {
                throw new NullReferenceException("syncData cannot be null");
            }
            if (syncBox == null)
            {
                throw new NullReferenceException("syncBox cannot be null");
            }
            if (httpRestClient == null)
            {
                throw new NullReferenceException("restHttpClient cannot be null");
            }
            if (HttpTimeoutMilliseconds <= 0)
            {
                throw new ArgumentException("HttpTimeoutMilliseconds must be greater than zero");
            }
            if (ErrorProcessingMillisecondInterval <= 0)
            {
                throw new ArgumentException("ErrorProcessingMillisecondInterval must be greater than zero");
            }
            #endregion

            #region assign instance fields
            this.syncData = syncData;
            this.statusUpdated = statusUpdated;
            if (statusUpdated == null)
            {
                this.statusUpdatedUserState = null;
            }
            else
            {
                this.statusUpdatedUserState = statusUpdatedUserState;
            }

            this.syncBox = syncBox;

            // set the Http REST client
            this.httpRestClient = httpRestClient;

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(this.syncBox.CopiedSettings.TraceLocation, "Cloud", "log", this.syncBox.CopiedSettings.TraceLevel, this.syncBox.CopiedSettings.LogErrors);
            CLTrace.Instance.writeToLog(9, "SyncEngine: SyncEngine: Entry.");

            this.DefaultTempDownloadsPath = Helpers.GetTempFileDownloadPath(this.syncBox.CopiedSettings, this.syncBox.SyncBoxId);

            this.HttpTimeoutMilliseconds = HttpTimeoutMilliseconds;
            this.MaxNumberOfFailureRetries = MaxNumberOfFailureRetries;
            this.MaxNumberOfNotFounds = MaxNumberOfNotFounds;
            this.ErrorProcessingMillisecondInterval = ErrorProcessingMillisecondInterval;
            this.MaxNumberOfServerConnectionFailures = MaxNumberConnectionFailures;
            this.DependencyDebugging = DependencyDebugging;

            // 12 is Default Connection Limit (6 up/6 down)
            ServicePointManager.DefaultConnectionLimit = CLDefinitions.MaxNumberOfConcurrentDownloads + CLDefinitions.MaxNumberOfConcurrentUploads;
            #endregion
        }

        // Timer to handle wait callbacks to requeue failures to reprocess
        private ProcessingQueuesTimer FailureTimer
        {
            get
            {
                // lock to prevent reading and writing to timer simultaneously
                lock (failureTimerLocker)
                {
                    // if timer has not been created, then create it
                    if (_failureTimer == null)
                    {
                        // create timer, store creation error
                        CLError timerError = ProcessingQueuesTimer.CreateAndInitializeProcessingQueuesTimer(
                            FailureProcessing, // callback when timer finishes
                            ErrorProcessingMillisecondInterval, // length of timer
                            out _failureTimer, // output new timer
                            this); // user state

                        // if an error occurred creating the timer, then rethrow the exception
                        if (timerError != null)
                        {
                            throw timerError.GrabFirstException();
                        }
                    }
                    // return existing or newly created timer
                    return _failureTimer;
                }
            }
        }
        // storage of requeue failures timer
        private ProcessingQueuesTimer _failureTimer = null;
        // locker for creating or retrieving requeue failures timer
        private readonly object failureTimerLocker = new object();

        // private queue for failures;
        // lock on FailureTimer.TimerRunningLocker for all access
        private readonly Queue<FileChange> FailedChangesQueue = new Queue<FileChange>();

        // Timer to handle wait callbacks to requeue failures to reprocess
        private ProcessingQueuesTimer FailedOutTimer
        {
            get
            {
                // lock to prevent reading and writing to timer simultaneously
                lock (failedOutTimerLocker)
                {
                    // if timer has not been created, then create it
                    if (_failedOutTimer == null)
                    {
                        // create timer, store creation error
                        CLError timerError = ProcessingQueuesTimer.CreateAndInitializeProcessingQueuesTimer(
                            FailedOutProcessing, // callback when timer finishes
                            FailedOutRetryMillisecondInterval, // length of timer
                            out _failedOutTimer, // output new timer
                            this); // user state

                        // if an error occurred creating the timer, then rethrow the exception
                        if (timerError != null)
                        {
                            throw timerError.GrabFirstException();
                        }
                    }
                    // return existing or newly created timer
                    return _failedOutTimer;
                }
            }
        }
        // storage of requeue failures timer
        private ProcessingQueuesTimer _failedOutTimer = null;
        // locker for creating or retrieving requeue failures timer
        private readonly object failedOutTimerLocker = new object();

        // private queue for changes which have failed out;
        // lock on FailedOutTimer.TimerRunningLocker for all access
        private readonly List<FileChange> FailedOutChanges = new List<FileChange>();

        // hashset of temp download folders marked as cleaned
        private static readonly HashSet<string> TempDownloadsCleaned = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        // locker for checking or modifying if a temp download folder has been marked clean
        private static readonly object TempDownloadsCleanedLocker = new object();
        // lookup by temp download folder to lookup of download file size to download infoes
        private static readonly Dictionary<string, Dictionary<long, List<DownloadIdAndMD5>>> TempDownloads = new Dictionary<string, Dictionary<long, List<DownloadIdAndMD5>>>(StringComparer.InvariantCultureIgnoreCase);

        // cancellation source which is marked cancelled on stopping the engine which is checked in many places to break out of running
        private readonly CancellationTokenSource FullShutdownToken = new CancellationTokenSource();

        // build a default location for temp downloads by path in current user's non-roaming application data plus the name of the currently running application
        private readonly string DefaultTempDownloadsPath;

        // EventHandler for when the _failureTimer hits the end of its timer;
        // state object must be the Function which adds failed items to the FileMonitor processing queue
        private static void FailureProcessing(object state)
        {
            // settings is required to log errors, so declare its instance outside the try/catch and default to null
            ICLSyncSettingsAdvanced storeSettings = null;
            try
            {
                // try cast state
                SyncEngine thisEngine = state as SyncEngine;

                // if try cast was not successful, then throw exception
                if (thisEngine == null)
                {
                    throw new NullReferenceException("state cannot be null and must be castable to SyncEngine");
                }

                // store settings from cast state
                storeSettings = thisEngine.syncBox.CopiedSettings;

                // retrieve failure timer only once to save on the getter code execution
                ProcessingQueuesTimer getTimer = thisEngine.FailureTimer;
                // lock on timer for all failure queue access
                lock (getTimer.TimerRunningLocker)
                {
                    // if any failures are queued, then try to requeue failures for reprocessing
                    if (thisEngine.FailedChangesQueue.Count > 0)
                    {
                        // dequeue all failed items into an array
                        FileChange[] failedChanges = new FileChange[thisEngine.FailedChangesQueue.Count];
                        for (int failedChangeIndex = 0; failedChangeIndex < failedChanges.Length; failedChangeIndex++)
                        {
                            failedChanges[failedChangeIndex] = thisEngine.FailedChangesQueue.Dequeue();
                        }

                        // attempt to requeue failures for reprocessing within a try/catch (upon failure, readd to failure queue)
                        try
                        {
                            // Add failed changes back to FileMonitor via cast state;
                            // if there are any adds in error requeue them to the failure queue;
                            // Log any errors
                            GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>();
                            CLError err = thisEngine.syncData.addChangesToProcessingQueue(failedChanges, true, errList);
                            if (errList.Value != null)
                            {
                                bool atLeastOneFailureAdded = false;

                                foreach (FileChange currentError in errList.Value)
                                {
                                    thisEngine.FailedChangesQueue.Enqueue(currentError);

                                    atLeastOneFailureAdded = true;
                                }

                                // if at least one failure was readded, restart the timer
                                if (atLeastOneFailureAdded)
                                {
                                    getTimer.StartTimerIfNotRunning();
                                }
                            }
                            if (err != null)
                            {
                                err.LogErrors(storeSettings.TraceLocation, storeSettings.LogErrors);
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
                                thisEngine.FailedChangesQueue.Enqueue(currentError);

                                atLeastOneFailureAdded = true;
                            }

                            // if at least one failure was readded, restart the timer
                            if (atLeastOneFailureAdded)
                            {
                                getTimer.StartTimerIfNotRunning();
                            }

                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (storeSettings == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Unable to LogErrors since storeSettings is null. Original error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                else
                {
                    ((CLError)ex).LogErrors(storeSettings.TraceLocation, storeSettings.LogErrors);
                }
            }
        }

        // EventHandler for when the _failedOutTimer hits the end of its timer;
        // state object must be the Function which adds failed items to the FileMonitor processing queue
        private static void FailedOutProcessing(object state)
        {
            // settings is required to log errors, so declare its instance outside the try/catch and default to null
            ICLSyncSettingsAdvanced storeSettings = null;
            try
            {
                // try cast state
                SyncEngine thisEngine = state as SyncEngine;

                // if try cast was not successful, then throw exception
                if (thisEngine == null)
                {
                    throw new NullReferenceException("state cannot be null and must be castable to SyncEngine");
                }

                // store settings from cast state
                storeSettings = thisEngine.syncBox.CopiedSettings;

                // retrieve failed out timer only once to save on the getter code execution
                ProcessingQueuesTimer getTimer = thisEngine.FailedOutTimer;
                // lock on timer for all failed out list access
                lock (getTimer.TimerRunningLocker)
                {
                    // if any failed out changes are in their list, then try to requeue failed out changes for reprocessing
                    if (thisEngine.FailedOutChanges.Count > 0)
                    {
                        // clear out all failed items into an array
                        FileChange[] failedOutChanges = thisEngine.FailedOutChanges.ToArray();
                        thisEngine.FailedOutChanges.Clear();

                        // attempt to requeue failed out changes for reprocessing within a try/catch (upon failure, readd to failure queue)
                        try
                        {
                            // Add failed out changes back to FileMonitor via cast state;
                            // if there are any adds in error requeue them to the failure queue;
                            // Log any errors
                            GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>();
                            CLError err = thisEngine.syncData.addChangesToProcessingQueue(failedOutChanges, true, errList);
                            if (errList.Value != null)
                            {
                                // retrieve failed out timer only once to save on the getter code execution
                                ProcessingQueuesTimer failureTimer = thisEngine.FailureTimer;

                                lock (failureTimer.TimerRunningLocker)
                                {
                                    bool atLeastOneFailureAdded = false;

                                    foreach (FileChange currentError in errList.Value)
                                    {
                                        thisEngine.FailedChangesQueue.Enqueue(currentError);

                                        atLeastOneFailureAdded = true;
                                    }

                                    // if at least one failure was readded, start the failure timer
                                    if (atLeastOneFailureAdded)
                                    {
                                        failureTimer.StartTimerIfNotRunning();
                                    }
                                }
                            }
                            if (err != null)
                            {
                                err.LogErrors(storeSettings.TraceLocation, storeSettings.LogErrors);
                            }
                        }
                        catch
                        {
                            // retrieve failed out timer only once to save on the getter code execution
                            ProcessingQueuesTimer failureTimer = thisEngine.FailureTimer;

                            lock (failureTimer.TimerRunningLocker)
                            {
                                bool atLeastOneFailureAdded = false;

                                // An error occurred adding all the failed changes to the FileMonitor;
                                // requeue them all to the failure queue;
                                // rethrow error for logging
                                foreach (FileChange currentError in failedOutChanges)
                                {
                                    thisEngine.FailedChangesQueue.Enqueue(currentError);

                                    atLeastOneFailureAdded = true;
                                }

                                // if at least one failure was readded, start the failure timer
                                if (atLeastOneFailureAdded)
                                {
                                    failureTimer.StartTimerIfNotRunning();
                                }

                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (storeSettings == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Unable to LogErrors since storeSettings is null. Original error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                else
                {
                    ((CLError)ex).LogErrors(storeSettings.TraceLocation, storeSettings.LogErrors);
                }
            }
        }

        // locker for current status aggregation thread and whether it is running
        private readonly GenericHolder<bool> StatusAggregatorRunning = new GenericHolder<bool>();
        // sychronously checks the status aggregation thread and starts it if it is not already running,
        // must be fired under a lock on StatusChangesQueue and that queue must have at least one item in it already
        private void StartStatusAggregatorIfNotStarted()
        {
            // lock to check/change the status aggregation thread
            lock (StatusAggregatorRunning)
            {
                // if the status aggregation thread is not running, then start it
                if (!StatusAggregatorRunning.Value)
                {
                    // mark that the aggregation thread has been started
                    StatusAggregatorRunning.Value = true;
                    // start the aggregation thread
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessStatusAggregation, // code to handle aggregation
                        new StatusAggregationState( // state containing all required parameters to aggregate status
                            StatusAggregatorRunning, // holder for whether status aggregator thread is running
                            StatusHolder, // holder for the current status object which can be retrieved by a user
                            statusUpdated, // optional callback which may have been provided by the user for when status gets updated
                            statusUpdatedUserState, // optional userstate which will be passed to statusUpdated callback when fired
                            threadStateKillTime, // holder for the next scheduled killtime when at least one thread would be dead and require cleanup
                            threadStateKillTimer, // holder for a threading Timer which is created to start after the earliest ThreadState should be cleaned up
                            ThreadsToStatus, // dictionary of thread ids to thread status where the latest status for each thread is aggregated
                            StatusChangesQueue)); // queue of change of ThreadStatus to apply to the ThreadsToStatus dictionary
                }
            }
        }
        // code which runs the status aggregation by dequeueing the StatusChangesQueue into ThreadsToStatus and updating the StatusHolder,
        // synchronized to only have one thread running in here at a time using StatusAggregatorRunning bool holder
        private static void ProcessStatusAggregation(object state)
        {
            try
            {
                StatusAggregationState castState = state as StatusAggregationState;

                if (castState == null)
                {
                    throw new Exception("Unable to cast state as correct type");
                }
                else
                {
                    Func<GenericHolder<bool>, Queue<KeyValuePair<Guid, ThreadStatus>>, GenericHolder<Timer>, GenericHolder<CLSyncCurrentStatus>, bool> continueProcessing = (threadRunning, statusQueue, killTimer, statusHolder) =>
                    {
                        lock (statusQueue)
                        {
                            if (statusQueue.Count > 0)
                            {
                                return true;
                            }

                            lock (threadRunning)
                            {
                                threadRunning.Value = false;

                                // make a check for an idle state in order to kill any remaining kill timer (which won't have anything to clean)
                                lock (statusHolder)
                                {
                                    if (statusHolder.Value == null
                                        || statusHolder.Value.CurrentState == CLSyncCurrentState.Idle)
                                    {
                                        lock (killTimer)
                                        {
                                            if (killTimer.Value != null)
                                            {
                                                try
                                                {
                                                    killTimer.Value.Dispose();
                                                }
                                                catch
                                                {
                                                }
                                                killTimer.Value = null;
                                            }
                                        }
                                    }
                                }

                                return false;
                            }
                        }
                    };

                    do
                    {
                        Nullable<KeyValuePair<Guid, ThreadStatus>> currentDequeued;
                        lock (castState.StatusChangesQueue)
                        {
                            currentDequeued = castState.StatusChangesQueue.Dequeue();
                        }

                        Func<Queue<KeyValuePair<Guid, ThreadStatus>>, Nullable<KeyValuePair<Guid, ThreadStatus>>> moreToDequeue = statusQueue =>
                        {
                            lock (statusQueue)
                            {
                                if (statusQueue.Count > 0)
                                {
                                    return statusQueue.Dequeue();
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        };

                        // we are sure there is an item left in the queue since we just dequeued one to start, so no need to apply while condition till after
                        do
                        {
                            KeyValuePair<Guid, ThreadStatus> nonNullDequeued = (KeyValuePair<Guid, ThreadStatus>)currentDequeued;

                            if (nonNullDequeued.Value.LastUpdateTime != null // the thread state for the sync communication thread will always have a last update time
                                || nonNullDequeued.Value.TotalByteSize != null) // the thread state for a file upload or a file download will always have a total byte size
                            // the combination of conditions should cover all valid thread states (and not cover a blank ThreadStatus which is used just to trigger a timeout check)
                            {
                                castState.ThreadsToStatus[nonNullDequeued.Key] = nonNullDequeued.Value;
                            }
                        }
                        while ((currentDequeued = moreToDequeue(castState.StatusChangesQueue)) != null);

                        CLSyncCurrentState outputState = CLSyncCurrentState.Idle;
                        List<CLSyncTransferringFile> outputTransferring = null;

                        Nullable<DateTime> earliestToKill = null;
                        DateTime killTime = DateTime.UtcNow.Subtract(ThreadStatusTimeoutSpan);
                        List<Guid> removedStatusKeys = null;
                        foreach (KeyValuePair<Guid, ThreadStatus> currentStatus in castState.ThreadsToStatus)
                        {
                            if (currentStatus.Value.LastUpdateTime == null
                                || killTime.CompareTo((DateTime)currentStatus.Value.LastUpdateTime) <= 0)
                            {
                                // define a bool for whether this status change represents a file upload or download which has completed and should be cleared out
                                bool uploadDownloadCompleted = false;

                                // condition for communicating changes thread and for active upload/download
                                if (currentStatus.Value.LastUpdateTime != null)
                                {
                                    // condition for completed upload/download
                                    if (currentStatus.Value.ByteProgress != null
                                        && ((long)currentStatus.Value.ByteProgress) == ((long)currentStatus.Value.TotalByteSize))
                                    {
                                        if (removedStatusKeys == null)
                                        {
                                            removedStatusKeys = new List<Guid>();
                                        }

                                        removedStatusKeys.Add(currentStatus.Key);

                                        uploadDownloadCompleted = true;
                                    }
                                    // condition for communicating changes thread of incomplete/active upload/download
                                    // and also only if the last update time is earlier than the earliest found so far
                                    else if (earliestToKill == null
                                        || ((DateTime)earliestToKill).CompareTo((DateTime)currentStatus.Value.LastUpdateTime) > 0)
                                    {
                                        earliestToKill = (DateTime)currentStatus.Value.LastUpdateTime;
                                    }
                                }

                                // condition for communicating changes thread
                                if (currentStatus.Value.TotalByteSize == null)
                                {
                                    outputState |= CLSyncCurrentState.CommunicatingChanges;
                                }
                                // condition for active or queued upload/download which haven't failed
                                else if (!currentStatus.Value.IsError
                                    && !uploadDownloadCompleted)
                                {
                                    // condition for active upload
                                    if (((SyncDirection)currentStatus.Value.Direction) == SyncDirection.To)
                                    {
                                        outputState |= CLSyncCurrentState.UploadingFiles;
                                    }
                                    // condition for active download
                                    else
                                    {
                                        outputState |= CLSyncCurrentState.DownloadingFiles;
                                    }

                                    if (outputTransferring == null)
                                    {
                                        outputTransferring = new List<CLSyncTransferringFile>();
                                    }

                                    outputTransferring.Add(new CLSyncTransferringFile(
                                        (long)currentStatus.Value.EventID,
                                        (SyncDirection)currentStatus.Value.Direction,
                                        currentStatus.Value.RelativePath,
                                        currentStatus.Value.ByteProgress ?? 0, // null-coalesce for queued upload/download
                                        (long)currentStatus.Value.TotalByteSize));
                                }
                            }
                            else
                            {
                                if (removedStatusKeys == null)
                                {
                                    removedStatusKeys = new List<Guid>();
                                }

                                removedStatusKeys.Add(currentStatus.Key);
                            }
                        }

                        if (removedStatusKeys != null)
                        {
                            foreach (Guid removeStatusKey in removedStatusKeys)
                            {
                                castState.ThreadsToStatus.Remove(removeStatusKey);
                            }
                        }

                        if (earliestToKill != null)
                        {
                            DateTime newKillTime = ((DateTime)earliestToKill).Add(ThreadStatusTimeoutSpan);

                            lock (castState.ThreadStateKillTimer)
                            {
                                if (castState.ThreadStateKillTimer.Value == null
                                    || castState.ThreadStateKillTime.Value.CompareTo(newKillTime) <= 0)
                                {
                                    castState.ThreadStateKillTime.Value = newKillTime;

                                    if (castState.ThreadStateKillTimer.Value != null)
                                    {
                                        try
                                        {
                                            castState.ThreadStateKillTimer.Value.Dispose();
                                        }
                                        catch
                                        {
                                        }
                                    }

                                    TimeSpan startTime;
                                    DateTime timeForTimer = DateTime.UtcNow;
                                    if (timeForTimer.CompareTo(newKillTime) >= 0)
                                    {
                                        startTime = TimeSpan.Zero;
                                    }
                                    else
                                    {
                                        startTime = newKillTime.Subtract(timeForTimer);
                                    }

                                    castState.ThreadStateKillTimer.Value = new Timer(ProcessKillTimer,
                                        state,
                                        startTime,
                                        NoPeriodTimeSpan);
                                }
                            }
                        }

                        lock (castState.StatusHolder)
                        {
                            castState.StatusHolder.Value = new CLSyncCurrentStatus(outputState,
                                outputTransferring);

                            if (castState.StatusUpdated != null)
                            {
                                try
                                {
                                    // Call the optional status changed callback.
                                    castState.StatusUpdated(castState.StatusUpdatedUserState);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    while (continueProcessing(
                        castState.StatusAggregatorRunning,
                        castState.StatusChangesQueue,
                        castState.ThreadStateKillTimer,
                        castState.StatusHolder));
                }
            }
            catch (Exception ex)
            {
                MessageEvents.FireNewEventMessage(
                    "An error occurred in ProcessStatusAggregation: " + ex.Message,
                    EventMessageLevel.Important,
                    new HaltAllOfCloudSDKErrorInfo());
            }
        }
        private static void ProcessKillTimer(object state)
        {
            try
            {
                StatusAggregationState castState = state as StatusAggregationState;

                if (castState == null)
                {
                    throw new Exception("Unable to cast state as correct type");
                }
                else
                {
                    lock (castState.ThreadStateKillTimer)
                    {
                        if (castState.ThreadStateKillTimer.Value != null)
                        {
                            try
                            {
                                castState.ThreadStateKillTimer.Value.Dispose();
                            }
                            catch
                            {
                            }
                            castState.ThreadStateKillTimer.Value = null;
                        }
                    }

                    lock (castState.StatusChangesQueue)
                    {
                        lock (castState.StatusAggregatorRunning)
                        {
                            if (!castState.StatusAggregatorRunning.Value)
                            {
                                if (castState.StatusChangesQueue.Count == 0)
                                {
                                    castState.StatusChangesQueue.Enqueue(new KeyValuePair<Guid, ThreadStatus>(Guid.Empty, new ThreadStatus()));
                                }

                                castState.StatusAggregatorRunning.Value = true;
                                ProcessStatusAggregation(state);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageEvents.FireNewEventMessage(
                    "An error occurred in ProcessKillTimer: " + ex.Message,
                    EventMessageLevel.Important,
                    new HaltAllOfCloudSDKErrorInfo());
            }
        }
        private readonly Dictionary<Guid, ThreadStatus> ThreadsToStatus = new Dictionary<Guid, ThreadStatus>();
        private static readonly TimeSpan ThreadStatusTimeoutSpan = TimeSpan.FromMinutes(5d);
        private readonly GenericHolder<DateTime> threadStateKillTime = new GenericHolder<DateTime>(DateTime.MinValue);
        private readonly GenericHolder<Timer> threadStateKillTimer = new GenericHolder<Timer>(null);
        private static readonly TimeSpan NoPeriodTimeSpan = TimeSpan.FromMilliseconds(-1d);
        private void SyncStillRunning(Guid threadId)
        {
            try
            {
                lock (StatusChangesQueue)
                {
                    StatusChangesQueue.Enqueue(new KeyValuePair<Guid, ThreadStatus>(
                        threadId,
                        new ThreadStatus()
                        {
                            LastUpdateTime = DateTime.UtcNow
                        }));

                    StartStatusAggregatorIfNotStarted();
                }
            }
            catch
            {
            }
        }
        private void SyncStoppedRunning(Guid threadId)
        {
            try
            {
                lock (StatusChangesQueue)
                {
                    StatusChangesQueue.Enqueue(new KeyValuePair<Guid, ThreadStatus>(
                        threadId,
                        new ThreadStatus()
                        {
                            LastUpdateTime = DateTime.MinValue
                        }));

                    StartStatusAggregatorIfNotStarted();
                }
            }
            catch
            {
            }
        }
        private void FileTransferQueued(Guid threadId, long eventId, SyncDirection direction, string relativePath, long totalByteSize)
        {
            try
            {
                lock (StatusChangesQueue)
                {
                    StatusChangesQueue.Enqueue(new KeyValuePair<Guid, ThreadStatus>(
                        threadId,
                        new ThreadStatus()
                        {
                            Direction = direction,
                            EventID = eventId,
                            RelativePath = relativePath,
                            TotalByteSize = totalByteSize
                        }));

                    StartStatusAggregatorIfNotStarted();
                }
            }
            catch
            {
            }
        }
        private void FileTransferStatusUpdate(Guid threadId, long eventId, SyncDirection direction, string relativePath, long byteProgress, long totalByteSize, bool isError)
        {
            try
            {
                lock (StatusChangesQueue)
                {
                    StatusChangesQueue.Enqueue(new KeyValuePair<Guid, ThreadStatus>(
                        threadId,
                        new ThreadStatus()
                        {
                            ByteProgress = byteProgress,
                            Direction = direction,
                            EventID = eventId,
                            LastUpdateTime = DateTime.UtcNow,
                            RelativePath = relativePath,
                            TotalByteSize = totalByteSize
                        }));

                    StartStatusAggregatorIfNotStarted();
                }
            }
            catch
            {
            }
        }
        private readonly Queue<KeyValuePair<Guid, ThreadStatus>> StatusChangesQueue = new Queue<KeyValuePair<Guid, ThreadStatus>>();
        private sealed class StatusAggregationState
        {
            public GenericHolder<bool> StatusAggregatorRunning
            {
                get
                {
                    return _statusAggregatorRunning;
                }
            }
            private readonly GenericHolder<bool> _statusAggregatorRunning;

            public GenericHolder<CLSyncCurrentStatus> StatusHolder
            {
                get
                {
                    return _statusHolder;
                }
            }
            private readonly GenericHolder<CLSyncCurrentStatus> _statusHolder;

            public System.Threading.WaitCallback StatusUpdated
            {
                get
                {
                    return _statusUpdated;
                }
            }
            private readonly System.Threading.WaitCallback _statusUpdated;

            public object StatusUpdatedUserState
            {
                get
                {
                    return _statusUpdatedUserState;
                }
            }
            private readonly object _statusUpdatedUserState;

            public GenericHolder<DateTime> ThreadStateKillTime
            {
                get
                {
                    return _threadStateKillTime;
                }
            }
            private readonly GenericHolder<DateTime> _threadStateKillTime;

            public GenericHolder<Timer> ThreadStateKillTimer
            {
                get
                {
                    return _threadStateKillTimer;
                }
            }
            private readonly GenericHolder<Timer> _threadStateKillTimer;

            public Dictionary<Guid, ThreadStatus> ThreadsToStatus
            {
                get
                {
                    return _threadsToStatus;
                }
            }
            private readonly Dictionary<Guid, ThreadStatus> _threadsToStatus;

            public Queue<KeyValuePair<Guid, ThreadStatus>> StatusChangesQueue
            {
                get
                {
                    return _statusChangesQueue;
                }
            }
            private readonly Queue<KeyValuePair<Guid, ThreadStatus>> _statusChangesQueue;

            public StatusAggregationState(
                GenericHolder<bool> StatusAggregatorRunning,
                GenericHolder<CLSyncCurrentStatus> StatusHolder,
                System.Threading.WaitCallback StatusUpdated,
                object StatusUpdatedUserState,
                GenericHolder<DateTime> ThreadStateKillTime,
                GenericHolder<Timer> ThreadStateKillTimer,
                Dictionary<Guid, ThreadStatus> ThreadsToStatus,
                Queue<KeyValuePair<Guid, ThreadStatus>> StatusChangesQueue)
            {
                this._statusAggregatorRunning = StatusAggregatorRunning;
                this._statusHolder = StatusHolder;
                this._statusUpdated = StatusUpdated;
                this._statusUpdatedUserState = StatusUpdatedUserState;
                this._threadStateKillTime = ThreadStateKillTime;
                this._threadStateKillTimer = ThreadStateKillTimer;
                this._threadsToStatus = ThreadsToStatus;
                this._statusChangesQueue = StatusChangesQueue;
            }
        }
        private sealed class ThreadStatus
        {
            public bool IsError
            {
                get
                {
                    return _isError;
                }
                set
                {
                    _isError = value;
                }
            }
            private bool _isError = false; // Should only be true if an error occurs during Task processing for a file upload/file download

            public Nullable<DateTime> LastUpdateTime
            {
                get
                {
                    return _lastUpdateTime;
                }
                set
                {
                    _lastUpdateTime = value;
                }
            }
            private Nullable<DateTime> _lastUpdateTime = null; // Set for sync running thread, but only set for file upload/file download if Task started

            public Nullable<long> EventID
            {
                get
                {
                    return _eventId;
                }
                set
                {
                    _eventId = value;
                }
            }
            private Nullable<long> _eventId = null; // Null for sync running thread, but set for file upload/file download

            public Nullable<SyncDirection> Direction
            {
                get
                {
                    return _direction;
                }
                set
                {
                    _direction = value;
                }
            }
            private Nullable<SyncDirection> _direction = null; // Null for sync running thread, but set for file upload/file download

            public string RelativePath
            {
                get
                {
                    return _relativePath;
                }
                set
                {
                    _relativePath = value;
                }
            }
            private string _relativePath = null; // Null for sync running thread, but set for file upload/file download

            public Nullable<long> ByteProgress
            {
                get
                {
                    return _byteProgress;
                }
                set
                {
                    _byteProgress = value;
                }
            }
            private Nullable<long> _byteProgress; // Null for sync running thread, but only set for file upload/file download if the first buffer has cleared

            public Nullable<long> TotalByteSize
            {
                get
                {
                    return _totalByteSize;
                }
                set
                {
                    _totalByteSize = value;
                }
            }
            private Nullable<long> _totalByteSize = null; // Null for sync running thread, but set for file upload/file download
        }

        public KeyValuePair<FilePathDictionary<List<FileChange>>, CLError> GetUploadDownloadTransfersInProgress(string CurrentFolderPath)
        {
            FilePathDictionary<List<FileChange>> toReturnKey;
            CLError toReturnValue = FilePathDictionary<List<FileChange>>.CreateAndInitialize(
                CurrentFolderPath,
                out toReturnKey);
            if (toReturnValue == null
                && toReturnKey != null)
            {
                RunUpDownEvent(
                    new FileChange.UpDownEventArgs(
                        (upDownChange, innerState) =>
                        {
                            FilePath storeNewPath;
                            FilePathDictionary<List<FileChange>> castState = innerState as FilePathDictionary<List<FileChange>>;
                            if (castState == null)
                            {
                                MessageEvents.FireNewEventMessage(
                                    "Unable to cast innerState as FilePathDictionary<List<FileChange>>",
                                    EventMessageLevel.Important,
                                    new HaltAllOfCloudSDKErrorInfo());
                            }
                            else if ((storeNewPath = upDownChange.NewPath) != null)
                            {
                                List<FileChange> upDownsAtPath;
                                if (!castState.TryGetValue(
                                    storeNewPath,
                                    out upDownsAtPath))
                                {
                                    castState.Add(storeNewPath, upDownsAtPath = new List<FileChange>());
                                }
                                upDownsAtPath.Add(upDownChange);
                            }
                        }),
                    toReturnKey);
            }

            return new KeyValuePair<FilePathDictionary<List<FileChange>>, CLError>(
                toReturnKey,
                toReturnValue);
        }

        /// <summary>
        /// Primary method for all syncing (both From and To),
        /// full contention between multiple simultaneous access on the same SyncEngine (each thread blocks until previous threads complete their syncs)
        /// </summary>
        /// <param name="respondingToPushNotification">Without any FileChanges to process, Sync communication will only occur if this parameter is set to true</param>
        /// <returns>Returns any error that occurred while processing</returns>
        public CLError Run(bool respondingToPushNotification)
        {
            #region local data including common fields for delegates

            // declare error which will be aggregated with exceptions and returned
            GenericHolder<CLError> toReturn = new GenericHolder<CLError>();

            // declare a string which provides better line-range information for the last state when an error is logged
            string syncStatus;

            GenericHolder<bool> commonRespondingToPushNotification = new GenericHolder<bool>(respondingToPushNotification);

            Guid commonRunThreadId = Guid.NewGuid();

            // errorsToQueue will have all changes to process (format KeyValuePair<KeyValuePair<[whether error was pulled from existing failures], [error change]>, [filestream for error]>),
            // items will be ignored as their successfulEventId is added
            GenericHolder<List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>> commonErrorsToQueue = new GenericHolder<List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>>(null);
            // list of completed events which will be recorded as a batch at the end to the database, used to filter out errors from being logged and requeued
            List<long> commonSuccessfulEventIds = new List<long>();
            // declare the enumeration of FileChanges which were dependent on completed changes to process again
            GenericHolder<IEnumerable<FileChange>> commonThingsThatWereDependenciesToQueue = new GenericHolder<IEnumerable<FileChange>>(null);

            GenericHolder<bool> commonWithinFailureTimerLock = new GenericHolder<bool>(false);
            // Field is for dequeued changes from the failure queue which exclude null changes
            // field is also used for changes that are immediately in error upon retrieval from FileSystem (including all items currently in failure queue)
            GenericHolder<IEnumerable<PossiblyPreexistingFileChangeInError>> commonDequeuedFailuresExcludingNulls = new GenericHolder<IEnumerable<PossiblyPreexistingFileChangeInError>>(null);
            GenericHolder<bool> commonNullErrorFoundInFailureQueue = new GenericHolder<bool>(false);
            GenericHolder<FileChange[]> commonDequeuedFailuresIncludingNulls = new GenericHolder<FileChange[]>(null);

            GenericHolder<Nullable<FileChangeFlowEntryPositionInFlow>> positionInChangeFlow = new GenericHolder<Nullable<FileChangeFlowEntryPositionInFlow>>(null);
            GenericHolder<IEnumerable<FileChange>> changesToTrace = new GenericHolder<IEnumerable<FileChange>>(null);

            // Define field where top level hierarchy changes to process will be stored
            GenericHolder<IEnumerable<PossiblyStreamableFileChange>> commonOutputChanges = new GenericHolder<IEnumerable<PossiblyStreamableFileChange>>(null);

            // null-coallesce the download temp path
            string commonTempDownloadsFolder;
            try
            {
                commonTempDownloadsFolder = String.IsNullOrWhiteSpace(syncBox.CopiedSettings.TempDownloadFolderFullPath) ? DefaultTempDownloadsPath : syncBox.CopiedSettings.TempDownloadFolderFullPath;
            }
            catch (Exception ex)
            {
                return ex;
            }

            // declare bool for whether an exception (not converted as CLError) was thrown when retrieving events to process
            bool errorGrabbingChanges = false;

            // storage for changes which were synchronously processed (not file uploads/downloads)
            GenericHolder<IList<FileChange>> commonSynchronouslyProcessed = new GenericHolder<IList<FileChange>>(null);
            // storage for changes which were asynchronously processed (only file uploads/downloads)
            GenericHolder<IList<FileChange>> commonAsynchronouslyProcessed = new GenericHolder<IList<FileChange>>(null);

            GenericHolder<List<PossiblyStreamableFileChange>> commonPreprocessedEvents = new GenericHolder<List<PossiblyStreamableFileChange>>(null);

            GenericHolder<HashSet<long>> commonSyncFromInitialDownloadMetadataErrors = new GenericHolder<HashSet<long>>(null);

            GenericHolder<PossiblyStreamableFileChange[]> commonChangesForCommunication = new GenericHolder<PossiblyStreamableFileChange[]>(null);

            // Declare enumerable for changes completed during communication (i.e. client sends up that a local folder was created and the server responds with a success status)
            IEnumerable<PossiblyChangedFileChange> completedChanges;
            // Declare enumerable for changes which still need to be completed (i.e. the server sends down that a new folder needs to be created, or client sends up that a local file was created and the server responds with an upload status)
            IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange> incompleteChanges;
            // Declare enumerable for the changes which had errors for requeueing
            IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError> changesInError;
            // Declare string for the newest sync id from the server
            string newSyncId;
            string syncRootUid;

            GenericHolder<Exception> commonCommunicationException = new GenericHolder<Exception>(null);

            GenericHolder<IEnumerable<PossiblyChangedFileChange>> commonCompletedChanges = new GenericHolder<IEnumerable<PossiblyChangedFileChange>>(null);
            GenericHolder<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>> commonIncompleteChanges = new GenericHolder<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>(null);
            GenericHolder<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>> commonChangesInError = new GenericHolder<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>(null);

            GenericHolder<string> commonNewSyncId = new GenericHolder<string>(null);
            GenericHolder<string> commonRootFolderServerUid = new GenericHolder<string>(null);

            CredentialErrorType credentialsError;
            GenericHolder<Nullable<CredentialErrorType>> commonCredentialsError = new GenericHolder<Nullable<CredentialErrorType>>(null);

            GenericHolder<IEnumerable<FileChange>> commonTopLevelErrors = new GenericHolder<IEnumerable<FileChange>>(null);

            #endregion

            #region local delegates with corresponding data holders

            #region checkHaltsAndErrors

            var checkHaltsAndErrors = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this
                },
                (Data, errorToAccumulate) =>
                {
                    // check for halted engine to return error
                    if (Data.commonThisEngine.CheckForMaxCommunicationFailuresHalt())
                    {
                        return new ObjectDisposedException("SyncEngine already halted from server connection failure");
                    }

                    lock (Data.commonThisEngine.CredentialErrorDetected)
                    {
                        switch (Data.commonThisEngine.CredentialErrorDetected.Value)
                        {
                            case CredentialErrorType.ExpiredCredentials:
                                return new ObjectDisposedException("SyncEngine already halted from an expired token");

                            case CredentialErrorType.OtherError:
                                return new ObjectDisposedException("SyncEngine already halted from authorization credentials error");

                            case CredentialErrorType.NoError:
                                return null;

                            default:
                                return new InvalidOperationException("SyncEngine credential error value is of unknown type: " + Data.commonThisEngine.CredentialErrorDetected.Value.ToString());
                        }
                    }
                },
                toReturn);

            #endregion

            #region getIsShutdown

            var getIsShutdown = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this
                },
                (Data, errorToAccumulate) =>
                {
                    // check for Sync shutdown
                    Monitor.Enter(Data.commonThisEngine.FullShutdownToken);
                    try
                    {
                        return Data.commonThisEngine.FullShutdownToken.Token.IsCancellationRequested;
                    }
                    finally
                    {
                        Monitor.Exit(Data.commonThisEngine.FullShutdownToken);
                    }
                },
                toReturn);

            #endregion

            #region checkInternetConnection

            var checkInternetConnection = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    getIsShutdown = getIsShutdown
                },
                (Data, errorToAccumulate) =>
                {
                    // if internet is not connected, no point to try sync, so make sure sync is requeued and send the informational message
                    if (!InternetConnected) // static, cannot use thisEngine reference
                    {
                        try
                        {
                            if (Data.getIsShutdown.TypedProcess())
                            {
                                throw new ObjectDisposedException("Unable to start new Sync Run, SyncEngine has been shut down");
                            }

                            // lock on timer for access to failure queue
                            lock (Data.commonThisEngine.FailureTimer.TimerRunningLocker)
                            {
                                if (Data.commonThisEngine.FailedChangesQueue.Count == 0)
                                {
                                    Data.commonThisEngine.FailedChangesQueue.Enqueue(null);

                                    Data.commonThisEngine.FailureTimer.StartTimerIfNotRunning();
                                }
                            }

                            MessageEvents.FireNewEventMessage(
                                "No internet connection detected. Retrying Sync after a short delay.",
                                SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                                DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);

                            return new GenericHolder<Exception>(null);
                        }
                        catch (Exception ex)
                        {
                            return new GenericHolder<Exception>(ex);
                        }
                    }

                    return null;
                },
                toReturn);

            #endregion

            #region stopOnFullHaltOrStartup

            var stopOnFullHaltOrStartup = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this
                },
                (Data, errorToAccumulate) =>
                {
                    try
                    {
                        if (Helpers.AllHaltedOnUnrecoverableError)
                        {
                            throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
                        }

                        // status message
                        MessageEvents.FireNewEventMessage(
                            Message: "Started checking for sync changes to process",
                            Error: null,
                            SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                            DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                    return null;
                },
                toReturn);

            #endregion

            #region checkTempNeedsCleaning

            var checkTempNeedsCleaning = DelegateAndDataHolder.Create(
                new
                {
                    commonTempDownloadsFolder = commonTempDownloadsFolder
                },
                (Data, errorToAccumulate) =>
                {
                    // Lock for reading/writing to whether startup occurred
                    lock (TempDownloadsCleanedLocker)
                    {
                        // Check global bool to see if startup has occurred
                        if (!TempDownloadsCleaned.Contains(Data.commonTempDownloadsFolder))
                        {
                            // Create directory if needed otherwise mark for cleaning
                            if (!Directory.Exists(Data.commonTempDownloadsFolder))
                            {
                                Directory.CreateDirectory(Data.commonTempDownloadsFolder);
                            }
                            else
                            {
                                //download temp folder is marked for cleaning
                                return true;
                            }

                            // Startup taken care of
                            TempDownloadsCleaned.Add(Data.commonTempDownloadsFolder);
                        }
                    }

                    return false;
                },
                toReturn);

            #endregion

            #region cleanTempDownloads

            var cleanTempDownloads = DelegateAndDataHolder.Create(
                new
                {
                    commonTempDownloadsFolder = commonTempDownloadsFolder
                },
                (Data, errorToAccumulate) =>
                {
                    // Declare the map of file size to download ids
                    Dictionary<long, List<DownloadIdAndMD5>> currentDownloads;
                    // Lock for reading/writing to list of temp downloads
                    lock (TempDownloads)
                    {
                        if (!TempDownloads.TryGetValue(Data.commonTempDownloadsFolder, out currentDownloads))
                        {
                            TempDownloads.Add(Data.commonTempDownloadsFolder,
                                currentDownloads = new Dictionary<long, List<DownloadIdAndMD5>>());
                        }
                    }

                    lock (currentDownloads)
                    {
                        // Loop through files in temp download folder
                        DirectoryInfo tempDownloadsFolderInfo = new DirectoryInfo(Data.commonTempDownloadsFolder);
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
                                    if (!currentDownloads.Values.Any(currentTempDownload => currentTempDownload.Any(currentInnerTempDownload => currentInnerTempDownload.Id == tempGuid)))
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
                },
                toReturn);

            #endregion

            #region verifyUnderFailureTimerLock

            var verifyUnderFailureTimerLock = DelegateAndDataHolder.Create(
                new
                {
                    commonWithinFailureTimerLock = commonWithinFailureTimerLock
                },
                (Data, errorToAccumulate) =>
                {
                    if (!Data.commonWithinFailureTimerLock.Value)
                    {
                        const string invalidMessage = "dequeueNonNullFailuresAndReturnWhetherNullFound: Can only acccess failure queue within a lock on its timer";

                        MessageEvents.FireNewEventMessage(invalidMessage,
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());

                        throw new InvalidOperationException(invalidMessage);
                    }
                },
                toReturn);

            #endregion

            #region dequeueNonNullFailuresAndReturnWhetherNullFound

            var dequeueNonNullFailuresAndReturnWhetherNullFound = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonDequeuedFailuresExcludingNulls = commonDequeuedFailuresExcludingNulls,
                    verifyUnderFailureTimerLock = verifyUnderFailureTimerLock
                },
                (Data, errorToAccumulate) =>
                {
                    Data.verifyUnderFailureTimerLock.Process();

                    bool nullErrorFound = false;

                    List<FileChange> failuresExcludingNulls = new List<FileChange>();
                    int storeFailureCount = Data.commonThisEngine.FailedChangesQueue.Count;
                    for (int initialErrorIndex = 0; initialErrorIndex < storeFailureCount; initialErrorIndex++)
                    {
                        FileChange possiblyNullError = Data.commonThisEngine.FailedChangesQueue.Dequeue();

                        if (possiblyNullError == null)
                        {
                            nullErrorFound = true;
                        }
                        else
                        {
                            failuresExcludingNulls.Add(possiblyNullError);
                        }
                    }

                    // store queued failures to array (typed as enumerable)
                    PossiblyPreexistingFileChangeInError[] initialErrors;
                    Data.commonDequeuedFailuresExcludingNulls.Value = initialErrors = new PossiblyPreexistingFileChangeInError[failuresExcludingNulls.Count];
                    for (int initialErrorIndex = 0; initialErrorIndex < ((PossiblyPreexistingFileChangeInError[])initialErrors).Length; initialErrorIndex++)
                    {
                        initialErrors[initialErrorIndex] = new PossiblyPreexistingFileChangeInError(true, failuresExcludingNulls[initialErrorIndex]);
                    }

                    return nullErrorFound;
                },
                toReturn);

            #endregion

            #region traceChangesEnumerableWithFlowState AND oneLineChangeFlowTrace

            var traceChangesEnumerableWithFlowState = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    positionInChangeFlow = positionInChangeFlow,
                    changesToTrace = changesToTrace
                },
                (Data, errorToAccumulate) =>
                {
                    if (Data.positionInChangeFlow.Value == null)
                    {
                        throw new NullReferenceException("commonPositionInChangFlow not set for this trace operation");
                    }

                    // advanced trace
                    if ((Data.commonThisEngine.syncBox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        ComTrace.LogFileChangeFlow(
                            Data.commonThisEngine.syncBox.CopiedSettings.TraceLocation,
                            Data.commonThisEngine.syncBox.CopiedSettings.DeviceId,
                            Data.commonThisEngine.syncBox.SyncBoxId,
                            (FileChangeFlowEntryPositionInFlow)Data.positionInChangeFlow.Value,
                            Data.changesToTrace.Value);
                    }
                },
                toReturn);
            Action<FileChangeFlowEntryPositionInFlow, IEnumerable<FileChange>, DelegateAndDataHolder, GenericHolder<Nullable<FileChangeFlowEntryPositionInFlow>>, GenericHolder<IEnumerable<FileChange>>> oneLineChangeFlowTrace =
                (positionToSet, changesToSet, baseTraceFunction, basePositionInChangeFlow, baseChangesToTrace) =>
                {
                    basePositionInChangeFlow.Value = positionToSet;
                    baseChangesToTrace.Value = changesToSet;
                    baseTraceFunction.Process();
                    basePositionInChangeFlow.Value = null;
                    baseChangesToTrace.Value = null;
                };

            #endregion

            #region grabFileMonitorChangesAndCombineHierarchyWithErrors

            var grabFileMonitorChangesAndCombineHierarchyWithErrorsAndReturnWhetherSuccessful = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonDequeuedFailuresExcludingNulls = commonDequeuedFailuresExcludingNulls,
                    verifyUnderFailureTimerLock = verifyUnderFailureTimerLock,
                    commonOutputChanges = commonOutputChanges,
                    commonNullErrorFoundInFailureQueue = commonNullErrorFoundInFailureQueue,
                    commonRespondingToPushNotification = commonRespondingToPushNotification
                },
                (Data, errorToAccumulate) =>
                {
                    Data.verifyUnderFailureTimerLock.Process();

                    // try/catch to retrieve events to process from event source (should include dependency assignments as needed),
                    // normally an error is allowed to occur to continue processing if returned as CLError but if catch is triggered then readd failures and mark bool to stop processing
                    try
                    {
                        // declare a bool for whether a null change was in the processing queue in the FileSystemMonitor
                        bool nullChangeFoundInFileSystemMonitor;
                        int outputChangesCount;
                        int outputChangesInErrorCount;

                        lock (Data.commonThisEngine.FailedOutTimer.TimerRunningLocker)
                        {
                            bool previousFailedOutChange = Data.commonThisEngine.FailedOutChanges.Count > 0;

                            IEnumerable<PossiblyStreamableFileChange> outputChanges;
                            IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError;

                            // retrieve events to process, with dependencies reassigned (including previous errors)
                            errorToAccumulate.Value = Data.commonThisEngine.syncData.grabChangesFromFileSystemMonitor(
                                Data.commonDequeuedFailuresExcludingNulls.Value,
                                out outputChanges,
                                out outputChangesCount,
                                out outputChangesInError,
                                out outputChangesInErrorCount,
                                out nullChangeFoundInFileSystemMonitor,
                                Data.commonThisEngine.FailedOutChanges);

                            Data.commonOutputChanges.Value = outputChanges;
                            Data.commonDequeuedFailuresExcludingNulls.Value = outputChangesInError;

                            if (previousFailedOutChange
                                && Data.commonThisEngine.FailedOutChanges.Count == 0)
                            {
                                Data.commonThisEngine.FailedOutTimer.TriggerTimerCompletionImmediately();
                            }
                        }

                        // if there was a null change, then communication should occur regardless of other changes since we cannot communicate a null change on a SyncTo (will cause a SyncFrom if no other changes)
                        if ((nullChangeFoundInFileSystemMonitor || Data.commonNullErrorFoundInFailureQueue.Value)
                            && !Data.commonRespondingToPushNotification.Value)
                        {
                            // marks for push notification which will always cause communication to occur even for no SyncTo changes (via SyncFrom)
                            Data.commonRespondingToPushNotification.Value = true;
                        }

                        // status message
                        // e.g. "5 changes to process, 1 change waiting to retry"
                        MessageEvents.FireNewEventMessage(
                            "Found " +
                                outputChangesCount.ToString() +
                                " change" + (outputChangesCount == 1 ? string.Empty : "s") + " to process" +
                                (outputChangesInErrorCount == 0
                                    ? string.Empty
                                    : ", and " +
                                        outputChangesInErrorCount.ToString() +
                                        " change" + (outputChangesInErrorCount == 1 ? " is" : "s are") + " waiting to retry"),
                            (outputChangesCount == 0
                                ? EventMessageLevel.Minor
                                : EventMessageLevel.Important),
                            SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                            DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);

                        return true; // true for successful
                    }
                    catch (Exception ex)
                    {
                        // an exception occurred (not converted as CLError), requeue failures and mark bool to stop processing

                        Data.commonOutputChanges.Value = Helpers.DefaultForType<IEnumerable<PossiblyStreamableFileChange>>();
                        Data.commonDequeuedFailuresExcludingNulls.Value = Helpers.DefaultForType<IEnumerable<PossiblyPreexistingFileChangeInError>>();
                        errorToAccumulate.Value = ex;

                        bool atLeastOneReAddError = false;

                        foreach (PossiblyPreexistingFileChangeInError reAddError in (Data.commonDequeuedFailuresExcludingNulls.Value ?? Enumerable.Empty<PossiblyPreexistingFileChangeInError>()))
                        {
                            atLeastOneReAddError = true;
                            Data.commonThisEngine.FailedChangesQueue.Enqueue(reAddError.FileChange);
                        }

                        if (atLeastOneReAddError)
                        {
                            Data.commonThisEngine.FailureTimer.StartTimerIfNotRunning();
                        }

                        // status message
                        MessageEvents.FireNewEventMessage(
                            Message: "An error occurred checking for changes",
                            Level: EventMessageLevel.Important,
                            Error: new GeneralErrorInfo(),
                            SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                            DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);

                        return false; // false for not successful
                    }
                },
                toReturn);

            #endregion

            #region assignOutputErrorsFromOutputChangesAndOutputErrorsExcludingNulls

            var assignOutputErrorsFromOutputChangesAndOutputErrorsExcludingNulls = DelegateAndDataHolder.Create(
                new
                {
                    commonErrorsToQueue = commonErrorsToQueue,
                    commonOutputChanges = commonOutputChanges,
                    commonDequeuedFailuresExcludingNulls = commonDequeuedFailuresExcludingNulls
                },
                (Data, errorToAccumulate) =>
                {
                    // set errors to queue here with all processed changes and all failed changes so they can be added to failure queue on exception
                    Data.commonErrorsToQueue.Value = new List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>(Data.commonOutputChanges.Value
                        .Select(currentOutputChange => new PossiblyStreamableAndPossiblyPreexistingErrorFileChange(false, currentOutputChange.FileChange, currentOutputChange.Stream))
                        .Concat(Data.commonDequeuedFailuresExcludingNulls.Value.Select(currentChangeInError => new PossiblyStreamableAndPossiblyPreexistingErrorFileChange(currentChangeInError.IsPreexisting, currentChangeInError.FileChange, null))));
                },
                toReturn);

            #endregion

            #region doesPreprocessingNeedToRepeatForDependencies

            // function which reinserts dependencies into topLevelChanges in sorted order and returns true for reprocessing
            var doesPreprocessingNeedToRepeatForDependencies = DelegateAndDataHolder.Create(
                new
                {
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue,
                    commonErrorsToQueue = commonErrorsToQueue,
                    commonOutputChanges = commonOutputChanges,
                    commonSyncFromInitialDownloadMetadataErrors = commonSyncFromInitialDownloadMetadataErrors
                },
                (Data, errorToAccumulate) =>
                {
                    // if there are no dependencies to reprocess, then return false
                    if (Data.commonThingsThatWereDependenciesToQueue.Value == null)
                    {
                        // return false to stop preprocessing
                        return false;
                    }

                    // create a list to store dependencies that cannot be processed since they were file uploads (and only highest level changes for file upload had streams);
                    // also contains FileChanges for initial load condition where a metadata query was not successful
                    List<FileChange> uploadDependenciesWithoutStreamsAndFailedMetadataFileTransfers = new List<FileChange>();

                    // loop through dependencies to either add to the list of errors (since they will be preprocessed) or to the list of unprocessable changes (requiring streams)
                    foreach (FileChange currentDependency in Data.commonThingsThatWereDependenciesToQueue.Value)
                    {
                        // events must have an id
                        if (currentDependency.EventId == 0)
                        {
                            throw new ArgumentException("Cannot communicate FileChange without EventId");
                        }

                        // these conditions ensure that no file uploads without FileStreams are processed in the current batch
                        // if all conditions are met, then the file change can be processed and should be added to the list of errors
                        if (currentDependency.Metadata == null // cannot process without metadata
                            || currentDependency.Metadata.HashableProperties.IsFolder // folders don't require streams
                            || currentDependency.Type == FileChangeType.Deleted // deleted files don't require streams
                            || currentDependency.Type == FileChangeType.Renamed // renamed files don't require streams
                            || (currentDependency.Direction == SyncDirection.From // files for download don't need upload streams
                                && !Data.commonSyncFromInitialDownloadMetadataErrors.Value.Contains(currentDependency.EventId)))
                        {
                            Data.commonErrorsToQueue.Value.Add(new PossiblyStreamableAndPossiblyPreexistingErrorFileChange(true, currentDependency, null));
                        }
                        // else if any condition was not met, then the file change was missing a stream and cannot be processed, add to that list
                        else
                        {
                            uploadDependenciesWithoutStreamsAndFailedMetadataFileTransfers.Add(currentDependency);
                        }
                    }

                    // if all the dependencies are not processable, return false to stop preprocessing
                    if (Enumerable.SequenceEqual(Data.commonThingsThatWereDependenciesToQueue.Value, uploadDependenciesWithoutStreamsAndFailedMetadataFileTransfers))
                    {
                        return false;
                    }

                    // replace changes to process with previous changes except already preprocessed changes and then add in dependencies which were processable
                    Data.commonOutputChanges.Value = Data.commonOutputChanges.Value // previous changes.Except(preprocessedEvents) // exclude already preprocessed events
                        .Concat(Data.commonThingsThatWereDependenciesToQueue.Value // add dependencies from preprocessed events (see next line for condition)
                            .Except(uploadDependenciesWithoutStreamsAndFailedMetadataFileTransfers) // which are themselves processable
                            .Select(currentDependencyToQueue => new PossiblyStreamableFileChange(currentDependencyToQueue, null))); // all the processable dependencies are non-streamable

                    // the only dependencies not in the the output list (to process) are the non-processable changes which are now the thingsThatWereDependenciesToQueue
                    Data.commonThingsThatWereDependenciesToQueue.Value = uploadDependenciesWithoutStreamsAndFailedMetadataFileTransfers;

                    // return true to continue preprocessing
                    return true;
                },
                toReturn);

            #endregion

            #region completeEventInSqlAndReturnWhetherErrorOccurred

            var completeEventInSqlAndReturnWhetherErrorOccurred = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    eventIdToComplete = new GenericHolder<Nullable<long>>(null)
                },
                (Data, errorToAccumulate) =>
                {
                    if (Data.eventIdToComplete.Value == null)
                    {
                        try
                        {
                            throw new NullReferenceException("eventIdToComplete cannot be null");
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += ex;
                            return true;
                        }
                    }

                    // mark event success in the database
                    CLError completeEventError = Data.commonThisEngine.syncData.completeSingleEvent((long)Data.eventIdToComplete.Value);
                    if (completeEventError != null)
                    {
                        errorToAccumulate.Value = CLError.AddException(errorToAccumulate.Value, new AggregateException("Error on completeSingleEvent", completeEventError.GrabExceptions()), replaceErrorDescription: true);

                        MessageEvents.FireNewEventMessage(
                            "syncData.completeSingleEvent returned an error after completing an event: " + completeEventError.errorDescription,
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());

                        return true;
                    }

                    return false;
                },
                toReturn);

            #endregion

            #region onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent

            var onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonErrorsToQueue = commonErrorsToQueue,
                    commonSuccessfulEventIds = commonSuccessfulEventIds,
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue,
                    commonSynchronouslyPreprocessed = commonSynchronouslyProcessed,
                    topLevelChange = new GenericHolder<PossiblyStreamableFileChange>(new PossiblyStreamableFileChange()), // <-- default constructed PossiblyStreamableFileChange is invalid, must be replaced before property getters fire
                    preprocessedEventIds = new GenericHolder<HashSet<long>>(null),
                    commonPreprocessedEvents = commonPreprocessedEvents,
                    completeEventInSqlAndReturnWhetherErrorOccurred = completeEventInSqlAndReturnWhetherErrorOccurred
                },
                (Data, errorToAccumulate) =>
                {
                    // add successful id in order to not preprocess the same event again
                    Data.preprocessedEventIds.Value.Add(Data.topLevelChange.Value.FileChange.EventId);
                    // add event as preprocessed so it will be excluded from changes to communicate
                    Data.commonPreprocessedEvents.Value.Add(Data.topLevelChange.Value);

                    // add change to synchronous list
                    Data.commonSynchronouslyPreprocessed.Value.Add(Data.topLevelChange.Value.FileChange);

                    Data.completeEventInSqlAndReturnWhetherErrorOccurred.TypedData.eventIdToComplete.Value = Data.topLevelChange.Value.FileChange.EventId;
                    if (Data.completeEventInSqlAndReturnWhetherErrorOccurred.TypedProcess())
                    {
                        return true;
                    }

                    // add successful id for completed event
                    Data.commonSuccessfulEventIds.Add(Data.topLevelChange.Value.FileChange.EventId);

                    // search for the FileChange in the errors by FileChange equality and Stream equality
                    Nullable<PossiblyStreamableAndPossiblyPreexistingErrorFileChange> foundErrorToRemove = Data.commonErrorsToQueue.Value
                        .Where(findErrorToQueue => findErrorToQueue.FileChange == Data.topLevelChange.Value.FileChange && findErrorToQueue.Stream == Data.topLevelChange.Value.Stream)
                        .Select(findErrorToQueue => (Nullable<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>)findErrorToQueue)
                        .FirstOrDefault();
                    // if a matching error was found, then remove it from the errors
                    if (foundErrorToRemove != null)
                    {
                        Data.commonErrorsToQueue.Value.Remove((PossiblyStreamableAndPossiblyPreexistingErrorFileChange)foundErrorToRemove);
                    }

                    // try to cast the successful change as one with dependencies
                    FileChangeWithDependencies changeWithDependencies = Data.topLevelChange.Value.FileChange as FileChangeWithDependencies;
                    // if change was one with dependencies and has a dependency, then concatenate the dependencies into thingsThatWereDependenciesToQueue
                    if (changeWithDependencies != null
                        && changeWithDependencies.DependenciesCount > 0)
                    {
                        if (Data.commonThingsThatWereDependenciesToQueue.Value == null)
                        {
                            Data.commonThingsThatWereDependenciesToQueue.Value = changeWithDependencies.Dependencies;
                        }
                        else
                        {
                            Data.commonThingsThatWereDependenciesToQueue.Value = Data.commonThingsThatWereDependenciesToQueue.Value.Concat(changeWithDependencies.Dependencies);
                        }
                    }

                    return false;
                },
                toReturn);

            #endregion

            #region fillPendingStorageKeysForPendingUploadsAndReturnValue

            var fillPendingStorageKeysForPendingUploadsAndReturnValue = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    pendingStorageKeys = new GenericHolder<HashSet<string>>(null)
                },
                (Data, errorToAccumulate) =>
                {
                    if (Data.pendingStorageKeys.Value == null)
                    {
                        CLHttpRestStatus getAllPendingsStatus;
                        JsonContracts.PendingResponse getAllPendingsResult;
                        CLError getAllPendingsError = Data.commonThisEngine.httpRestClient.GetAllPending(
                            Data.commonThisEngine.HttpTimeoutMilliseconds,
                            out getAllPendingsStatus,
                            out getAllPendingsResult);

                        if (getAllPendingsStatus != CLHttpRestStatus.Success)
                        {
                            const string pendingsErrorString = "Unable to query pending files for comparison with preexisting uploads";

                            Exception fireMessageException = null;

                            try
                            {
                                MessageEvents.FireNewEventMessage(
                                    pendingsErrorString,
                                    EventMessageLevel.Important,
                                    /*Error*/ new GeneralErrorInfo(),
                                    Data.commonThisEngine.syncBox.SyncBoxId,
                                    Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                            }
                            catch (Exception ex)
                            {
                                fireMessageException = ex;
                            }

                            throw new AggregateException(pendingsErrorString,

                                (fireMessageException == null
                                    ? getAllPendingsError.GrabExceptions()
                                    : getAllPendingsError.GrabExceptions().Concat(Helpers.EnumerateSingleItem(fireMessageException))));
                        }

                        Data.pendingStorageKeys.Value = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                        if (getAllPendingsResult != null)
                        {
                            foreach (JsonContracts.Metadata pendingMetadata in (getAllPendingsResult.Files ?? Enumerable.Empty<JsonContracts.Metadata>()))
                            {
                                if (!string.IsNullOrEmpty(pendingMetadata.StorageKey))
                                {
                                    Data.pendingStorageKeys.Value.Add(pendingMetadata.StorageKey);
                                }
                            }
                        }
                    }

                    return Data.pendingStorageKeys.Value;
                },
                toReturn);

            #endregion

            #region notifyOnConfirmMetadataForInitialUploadOrDownload

            var notifyOnConfirmMetadataForInitialUploadOrDownload = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    confirmingMetadataForPreexistingUploadDownloads = new GenericHolder<bool>(false),
                    unhandledPreexistingUploadDownloadEventMessage = new GenericHolder<bool>(false)
                },
                (Data, errorToAccumulate) =>
                {
                    if (!Data.confirmingMetadataForPreexistingUploadDownloads.Value)
                    {
                        try
                        {
                            Data.confirmingMetadataForPreexistingUploadDownloads.Value = EventHandledLevel.IsHandled ==
                                MessageEvents.FireNewEventMessage(
                                    "At least one preexisting upload or download was found on startup, confirming metadata before processing" +
                                        (Data.unhandledPreexistingUploadDownloadEventMessage.Value
                                            ? "; Handle message event to prevent duplicate messages"
                                            : string.Empty),
                                    EventMessageLevel.Regular,
                                    SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                                    DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);

                            Data.unhandledPreexistingUploadDownloadEventMessage.Value = !Data.confirmingMetadataForPreexistingUploadDownloads.Value;
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += ex;
                        }
                    }
                },
                toReturn);

            #endregion

            #region verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred

            var verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    existingFilePath = new GenericHolder<string>(null),
                    topLevelChange = new GenericHolder<PossiblyStreamableFileChange>(new PossiblyStreamableFileChange()), // <-- default constructed PossiblyStreamableFileChange is invalid, must be replaced before property getters fire
                    existingFileMD5Error = new GenericHolder<CLError>(null),
                    existingFileMD5 = new GenericHolder<byte[]>(null),
                    onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent = onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent,
                    notifyOnConfirmMetadataForInitialUploadOrDownload = notifyOnConfirmMetadataForInitialUploadOrDownload,
                    commonPreprocessedEvents = commonPreprocessedEvents,
                    commonSyncFromInitialDownloadMetadataErrors = commonSyncFromInitialDownloadMetadataErrors,
                    initialMetadataFailuresAsInnerDependencies = new GenericHolder<List<FileChange>>(null)
                },
                (Data, errorToAccumulate) =>
                {
                    // TODO: remove all references of System.IO in this engine, should be a callback to the event source (FileMonitor)

                    // compare metadata with disk first

                    FileInfo existingFile = new FileInfo(Data.existingFilePath.Value);
                    bool matchingFileFound;
                    if (Data.existingFileMD5Error.Value == null
                        && Data.existingFileMD5.Value != null
                        && existingFile.Exists
                        && (Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                            || Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.CreationTime.ToUniversalTime().Ticks == FileConstants.InvalidUtcTimeTicks
                            || Helpers.DateTimesWithinOneSecond(Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.CreationTime, existingFile.CreationTimeUtc))
                        && (Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                            || Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.LastTime.ToUniversalTime().Ticks == FileConstants.InvalidUtcTimeTicks
                            || Helpers.DateTimesWithinOneSecond(Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.LastTime, existingFile.LastWriteTimeUtc))
                        && Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.Size != null
                        && ((long)Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.Size) == existingFile.Length)
                    {
                        // create MD5 to calculate hash
                        MD5 md5Hasher = MD5.Create();

                        // define buffer for reading the file
                        byte[] fileBuffer = new byte[FileConstants.BufferSize];
                        // declare int for storying how many bytes were read on each buffer transfer
                        int fileReadBytes;

                        try
                        {
                            // open a file read stream for reading the hash at the existing temp file location
                            using (FileStream verifyTempDownloadStream = new FileStream(Data.existingFilePath.Value,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.Read))
                            {
                                // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the file
                                while ((fileReadBytes = verifyTempDownloadStream.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                {
                                    // add the buffer block to the hash calculation
                                    md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                }

                                // transform one final empty block to complete the hash calculation
                                md5Hasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);

                                // if the existing file has an identical hash, then use the existing file for the current download
                                matchingFileFound = NativeMethods.memcmp(Data.existingFileMD5.Value, md5Hasher.Hash, new UIntPtr((uint)md5Hasher.Hash.Length)) == 0; // matching hash
                            }
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += new Exception("Error comparing MD5 hashes from topLevelChange FileChange with the file at its path on disk", ex);
                            matchingFileFound = false;
                        }
                    }
                    else
                    {
                        matchingFileFound = false;
                    }

                    if (matchingFileFound)
                    {
                        Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedData.topLevelChange.Value = Data.topLevelChange.Value;
                        if (Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedProcess())
                        {
                            return true;
                        }
                    }
                    else
                    {
                        Data.notifyOnConfirmMetadataForInitialUploadOrDownload.Process();

                        CLHttpRestStatus latestMetadataStatus;
                        JsonContracts.Metadata latestMetadataResult;
                        CLError latestMetadataError = Data.commonThisEngine.httpRestClient.GetMetadata(
                            Data.topLevelChange.Value.FileChange.NewPath,
                            /* isFolder */ false,
                            Data.commonThisEngine.HttpTimeoutMilliseconds,
                            out latestMetadataStatus,
                            out latestMetadataResult);

                        if (latestMetadataStatus != CLHttpRestStatus.Success
                            && latestMetadataStatus != CLHttpRestStatus.NoContent)
                        {
                            const string fileMetadataErrorString = "Errors occurred finding latest metadata for a preexisting download";

                            errorToAccumulate.Value += new AggregateException(fileMetadataErrorString,
                                latestMetadataError.GrabExceptions());

                            try
                            {
                                MessageEvents.FireNewEventMessage(
                                    fileMetadataErrorString,
                                    EventMessageLevel.Regular,
                                    /*Error*/new GeneralErrorInfo(),
                                    Data.commonThisEngine.syncBox.SyncBoxId,
                                    Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                            }
                            catch (Exception ex)
                            {
                                errorToAccumulate.Value += ex;
                            }
                        }

                        bool markDownloadError = false;

                        if (latestMetadataStatus == CLHttpRestStatus.Success
                            && latestMetadataResult != null)
                        {
                            CLHttpRestStatus fileVersionStatus;
                            JsonContracts.FileVersion[] fileVersionResult;
                            CLError fileVersionError = Data.commonThisEngine.httpRestClient.GetFileVersions(
                                latestMetadataResult.ServerUid,
                                Data.commonThisEngine.HttpTimeoutMilliseconds,
                                out fileVersionStatus,
                                out fileVersionResult);

                            if (fileVersionStatus != CLHttpRestStatus.Success
                                && fileVersionStatus != CLHttpRestStatus.NoContent)
                            {
                                const string fileVersionsErrorString = "Errors occurred finding previous versions for a preexisting download";

                                errorToAccumulate.Value += new AggregateException(fileVersionsErrorString,
                                    fileVersionError.GrabExceptions());

                                try
                                {
                                    MessageEvents.FireNewEventMessage(
                                        fileVersionsErrorString,
                                        EventMessageLevel.Regular,
                                        /*Error*/new GeneralErrorInfo(),
                                        Data.commonThisEngine.syncBox.SyncBoxId,
                                        Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                                }
                                catch (Exception ex)
                                {
                                    errorToAccumulate.Value += ex;
                                }
                            }

                            JsonContracts.FileVersion latestNonPendingVersion;
                            byte[] latestNonPendingHash;
                            if (fileVersionResult == null
                                || (latestNonPendingVersion = (fileVersionResult
                                    .OrderByDescending(fileVersion => (fileVersion.Version ?? -1))
                                    .FirstOrDefault(fileVersion =>
                                        fileVersion.IsNotPending != false
                                            && fileVersion.IsDeleted != true))) == null
                                || (latestNonPendingHash = Helpers.ParseHexadecimalStringToByteArray(latestNonPendingVersion.FileHash)) == null
                                || Data.existingFileMD5.Value == null
                                || NativeMethods.memcmp(Data.existingFileMD5.Value, latestNonPendingHash, new UIntPtr((uint)Data.existingFileMD5.Value.Length)) != 0
                                || Data.topLevelChange.Value.FileChange.Metadata.HashableProperties.Size != latestNonPendingVersion.FileSize)
                            {
                                markDownloadError = true;
                            }
                        }
                        else
                        {
                            markDownloadError = true;
                        }

                        if (markDownloadError)
                        {
                            Data.commonPreprocessedEvents.Value.Add(Data.topLevelChange.Value);

                            Data.commonSyncFromInitialDownloadMetadataErrors.Value.Add(Data.topLevelChange.Value.FileChange.EventId);

                            Data.initialMetadataFailuresAsInnerDependencies.Value.Add(Data.topLevelChange.Value.FileChange);

                            return true; // return true for error occurred
                        }
                    }

                    return false; // return false for no error occurred
                },
                toReturn);

            #endregion

            #region finalizeAndStartAsyncTask

            var finalizeAndStartAsyncTask = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    asyncTask = new GenericHolder<PossiblyStreamableFileChangeWithUploadDownloadTask>(new PossiblyStreamableFileChangeWithUploadDownloadTask()),
                    commonErrorsToQueue = commonErrorsToQueue,
                    commonAsynchronouslyProcessed = commonAsynchronouslyProcessed
                },
                (Data, errorToAccumulate) =>
                {
                    // switch on async task direction to either increment downloaded or uploaded count
                    switch (Data.asyncTask.Value.Task.Direction)
                    {
                        case SyncDirection.From:
                            // direction for downloads
                            Data.asyncTask.Value.Task.Task.ContinueWith(eventCompletion =>
                            {
                                // if event id is valid, then increment downloaded count
                                if (eventCompletion.Result.EventId != 0)
                                {
                                    MessageEvents.IncrementDownloadedCount(
                                        incrementAmount: 1,
                                        SyncBoxId: eventCompletion.Result.SyncBoxId,
                                        DeviceId: eventCompletion.Result.SyncSettings.DeviceId);
                                }
                            }, TaskContinuationOptions.NotOnFaulted); // only increment count when not faulted
                            break;

                        case SyncDirection.To:
                            // direction for uploads
                            Data.asyncTask.Value.Task.Task.ContinueWith(eventCompletion =>
                            {
                                // if event id is valid, then increment uploaded count
                                if (eventCompletion.Result.EventId != 0)
                                {
                                    MessageEvents.IncrementUploadedCount(
                                        incrementAmount: 1,
                                        SyncBoxId: eventCompletion.Result.SyncBoxId,
                                        DeviceId: eventCompletion.Result.SyncSettings.DeviceId);
                                }
                            }, TaskContinuationOptions.NotOnFaulted); // only increment count when not faulted
                            break;

                        default:
                            // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                            throw new NotSupportedException("Unknown SyncDirection: " + Data.asyncTask.Value.Task.Direction.ToString());
                    }

                    // add continuation task for recording the completed event into the database
                    Data.asyncTask.Value.Task.Task.ContinueWith(eventCompletion =>
                    {
                        // if the completed task had a valid id, then record completion into the database
                        if (eventCompletion.Result.EventId != 0)
                        {
                            // record completion into the database, storing any error that occurs
                            CLError sqlCompleteError = eventCompletion.Result.SyncData.completeSingleEvent(eventCompletion.Result.EventId);
                            // if an error occurred storing the completion, then log it
                            if (sqlCompleteError != null)
                            {
                                sqlCompleteError.LogErrors(eventCompletion.Result.SyncSettings.TraceLocation, eventCompletion.Result.SyncSettings.LogErrors);
                            }
                        }
                    }, TaskContinuationOptions.NotOnFaulted); // only run continuation if successful

                    // attach the current FileChange's UpDownEvent handler to the local UpDownEvent
                    Data.commonThisEngine.AddFileChangeToUpDownEvent(Data.asyncTask.Value.FileChange.FileChange);

                    Data.commonThisEngine.FileTransferQueued(Data.asyncTask.Value.Task.ThreadId,
                        Data.asyncTask.Value.FileChange.FileChange.EventId,
                        Data.asyncTask.Value.Task.Direction,
                        Data.asyncTask.Value.FileChange.FileChange.NewPath.GetRelativePath(Data.commonThisEngine.syncBox.CopiedSettings.SyncRoot, false),
                        (long)Data.asyncTask.Value.FileChange.FileChange.Metadata.HashableProperties.Size);

                    // try/catch to start the async task, or removing the UpDownEvent handler and rethrowing on exception
                    try
                    {
                        // start async task on the HttpScheduler
                        Data.asyncTask.Value.Task.Task.Start(
                            HttpScheduler.GetSchedulerByDirection(Data.asyncTask.Value.Task.Direction, Data.commonThisEngine.syncBox.CopiedSettings)); // retrieve HttpScheduler by direction and settings (upload and download are on seperate queues, and settings are used for error logging)

                        // add task as asynchronously processed
                        Data.commonAsynchronouslyProcessed.Value.Add(Data.asyncTask.Value.FileChange.FileChange);
                    }
                    catch
                    {
                        // remove UpDownEvent handler on exception
                        Data.commonThisEngine.RemoveFileChangeFromUpDownEvent(Data.asyncTask.Value.FileChange.FileChange);

                        // rethrow
                        throw;
                    }
                    
                    // remove the FileChange in the errors by FileChange equality and Stream equality
                    Data.commonErrorsToQueue.Value.RemoveAll(findErrorToQueue =>
                        findErrorToQueue.FileChange == Data.asyncTask.Value.FileChange.FileChange
                            && findErrorToQueue.Stream == Data.asyncTask.Value.FileChange.Stream);
                },
                toReturn);

            #endregion

            #region preprocessExistingEventsAndReturnWhetherShutdown

            var preprocessExistingEventsAndReturnWhetherShutdown = DelegateAndDataHolder.Create(
                new
                {
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue,
                    commonOutputChanges = commonOutputChanges,
                    commonThisEngine = this,
                    commonRunThreadId = commonRunThreadId,
                    getIsShutdown = getIsShutdown,
                    traceChangesEnumerableWithFlowState = traceChangesEnumerableWithFlowState,
                    oneLineChangeFlowTrace = oneLineChangeFlowTrace,
                    positionInChangeFlow = positionInChangeFlow,
                    changesToTrace = changesToTrace,
                    commonTempDownloadsFolder = commonTempDownloadsFolder,
                    commonSynchronouslyProcessed = commonSynchronouslyProcessed,
                    commonAsynchronouslyProcessed = commonAsynchronouslyProcessed,
                    commonPreprocessedEvents = commonPreprocessedEvents,
                    commonSyncFromInitialDownloadMetadataErrors = commonSyncFromInitialDownloadMetadataErrors,
                    doesPreprocessingNeedToRepeatForDependencies = doesPreprocessingNeedToRepeatForDependencies,
                    onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent = onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent,
                    notifyOnConfirmMetadataForInitialUploadOrDownload = notifyOnConfirmMetadataForInitialUploadOrDownload,
                    verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred = verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred,
                    finalizeAndStartAsyncTask = finalizeAndStartAsyncTask
                },
                (Data, errorToAccumulate) =>
                {
                    // Synchronously or asynchronously fire off all events without dependencies that have a storage key (MDS events);
                    // leave changes that did not complete in the errorsToQueue list so they will be added to the failure queue later;
                    // pull out inner dependencies and append to the enumerable thingsThatWereDependenciesToQueue;
                    // repeat this process with inner dependencies
                    Data.commonPreprocessedEvents.Value = new List<PossiblyStreamableFileChange>();
                    Data.commonSyncFromInitialDownloadMetadataErrors.Value = new HashSet<long>();
                    HashSet<long> preprocessedEventIds;
                    Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedData.preprocessedEventIds.Value = preprocessedEventIds = new HashSet<long>();

                    // after each time reprocessForDependencies runs,
                    // outputChanges will then equal all of the FileChanges it used to contain except for FileChanges which were previously preprocessed;
                    // it also contains any uncovered dependencies beneath completed changes which can be processed (are not file uploads which are missing FileStreams)

                    // after each time reprocessForDependencies runs,
                    // errorsToQueue may be appended with dependencies whose parents have been completed
                    // and who themselves can be processed (are not file uploads which are missing FileStreams)

                    // create list to store changes which were synchronously preprocessed (not file uploads/downloads)
                    Data.commonSynchronouslyProcessed.Value = new List<FileChange>();
                    // create list to store changes which were asynchronously preprocessed (only file uploads/downloads)
                    Data.commonAsynchronouslyProcessed.Value = new List<FileChange>();

                    // process once then repeat if it needs to reprocess for dependencies
                    do
                    {
                        List<FileChange> initialMetadataFailuresAsInnerDependencies = new List<FileChange>();

                        // loop through all changes to process
                        foreach (PossiblyStreamableFileChange topLevelChange in Data.commonOutputChanges.Value)
                        {
                            Data.commonThisEngine.SyncStillRunning(Data.commonRunThreadId);

                            if (Data.getIsShutdown.TypedProcess())
                            {
                                return true; // true for shutdown; should cause outer to return toReturn.Value
                            }

                            bool initialUploadDownloadMetadataError = false;

                            // if change is a valid upload/download but this is the initial Sync Run, then metadata needs to be verified
                            if (!Data.commonThisEngine.RunLocker.Value
                                && (topLevelChange.FileChange.Type == FileChangeType.Created || topLevelChange.FileChange.Type == FileChangeType.Modified)
                                && topLevelChange.FileChange.Metadata != null
                                && !topLevelChange.FileChange.Metadata.HashableProperties.IsFolder
                                && !string.IsNullOrEmpty(topLevelChange.FileChange.Metadata.StorageKey))
                            {
                                // advanced trace, InitialRunFileTransfer
                                Data.oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.InitialRunFileTransfer, Helpers.EnumerateSingleItem(topLevelChange.FileChange), Data.traceChangesEnumerableWithFlowState, Data.positionInChangeFlow, Data.changesToTrace);

                                byte[] existingFileMD5;
                                CLError existingFileMD5Error = topLevelChange.FileChange.GetMD5Bytes(out existingFileMD5);
                                if (existingFileMD5Error != null)
                                {
                                    errorToAccumulate.Value += new AggregateException("Error retrieving MD5 from topLevelChange FileChange", existingFileMD5Error.GrabExceptions());
                                }
                                string existingFilePath = topLevelChange.FileChange.NewPath.ToString();

                                switch (topLevelChange.FileChange.Direction)
                                {
                                    case SyncDirection.From:
                                        Data.verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred.TypedData.existingFileMD5.Value = existingFileMD5;
                                        Data.verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred.TypedData.existingFileMD5Error.Value = existingFileMD5Error;
                                        Data.verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred.TypedData.existingFilePath.Value = existingFilePath;
                                        Data.verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred.TypedData.initialMetadataFailuresAsInnerDependencies.Value = initialMetadataFailuresAsInnerDependencies;
                                        Data.verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred.TypedData.topLevelChange.Value = topLevelChange;

                                        if (Data.verifyInitialDownloadMetadataAndReturnWhetherErrorOccurred.TypedProcess())
                                        {
                                            if (Helpers.AllHaltedOnUnrecoverableError) // onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent can halt the whole engine, doesn't differentiate the error on the above return true
                                            {
                                                return true;
                                            }

                                            initialUploadDownloadMetadataError = true;
                                        }
                                        break;

                                    case SyncDirection.To:
                                        Data.notifyOnConfirmMetadataForInitialUploadOrDownload.Process();

                                        HashSet<string> pendings = fillPendingStorageKeysForPendingUploadsAndReturnValue.TypedProcess();

                                        if (!pendings.Contains(topLevelChange.FileChange.Metadata.StorageKey))
                                        {
                                            Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedData.topLevelChange.Value = topLevelChange;
                                            if (Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedProcess())
                                            {
                                                return true;
                                            }

                                            if (topLevelChange.Stream != null)
                                            {
                                                try
                                                {
                                                    topLevelChange.Stream.Dispose();
                                                }
                                                catch
                                                {
                                                }
                                            }
                                        }
                                        break;

                                    default:
                                        throw new NotSupportedException("Unknown topLevelChange FileChange Direction: " + topLevelChange.FileChange.Direction.ToString());
                                }
                            }

                            // preprocess the current change if valid
                            if (!initialUploadDownloadMetadataError
                                && topLevelChange.FileChange.EventId > 0 // requires a valid event id
                                && !preprocessedEventIds.Contains(topLevelChange.FileChange.EventId) // only preprocess if not already preprocessed
                                && (topLevelChange.FileChange.Direction == SyncDirection.From // can preprocess all Sync From events
                                    || ((topLevelChange.FileChange.Type == FileChangeType.Created || topLevelChange.FileChange.Type == FileChangeType.Modified) // if not a Sync From event, first requirement for preprocessing is a creation or modification (file uploads)
                                        && topLevelChange.FileChange.Metadata != null // file uploads require metadata
                                        && !string.IsNullOrWhiteSpace(topLevelChange.FileChange.Metadata.StorageKey)))) // file uploads requires a storage key
                            {
                                // declare storage for the event id if it processes succesfully
                                Nullable<long> successfulEventId;
                                // declare storage for an upload or download task if one will need to be started
                                Nullable<AsyncUploadDownloadTask> asyncTask;

                                // run private method which handles performing the action of a single FileChange, storing any exceptions thrown (which are caught and returned)
                                Exception completionException = Data.commonThisEngine.CompleteFileChange(
                                    topLevelChange, // change to perform
                                    Data.commonThisEngine.FailureTimer, // timer for failure queue
                                    out successfulEventId, // output successful event id or null
                                    out asyncTask, // out async upload or download task to perform or null
                                    Data.commonTempDownloadsFolder); // full path location of folder to store temp file downloads

                                // if there was a non-null and valid event id output as succesful,
                                // then add to synchronous list, add to success list, remove from errors, and check and concatenate any dependent FileChanges
                                if (successfulEventId != null
                                    && (long)successfulEventId > 0)
                                {
                                    Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedData.topLevelChange.Value = topLevelChange;
                                    if (Data.onCompletionOfSynchronousPreprocessedEventReturnWhetherErrorOccurredCompletingEvent.TypedProcess())
                                    {
                                        return true;
                                    }
                                }
                                // else if there was not a valid successful event id, then process for errors or async tasks
                                else
                                {
                                    // the following two additions to preprocessed lists are only within this else statement because onSynchronousCompletion also adds to these lists above

                                    // add current change to list of those preprocessed
                                    Data.commonPreprocessedEvents.Value.Add(topLevelChange);
                                    // add current change id to list of those preprocessed
                                    preprocessedEventIds.Add(topLevelChange.FileChange.EventId);

                                    // if there was an exception, aggregate into returned error
                                    if (completionException != null)
                                    {
                                        errorToAccumulate.Value += completionException;
                                    }

                                    // if there was an async task to perform, then process starting it
                                    if (asyncTask != null)
                                    {
                                        Data.finalizeAndStartAsyncTask.TypedData.asyncTask.Value =
                                            new PossiblyStreamableFileChangeWithUploadDownloadTask(
                                                topLevelChange,
                                                (AsyncUploadDownloadTask)asyncTask);
                                        Data.finalizeAndStartAsyncTask.Process();
                                    }
                                }
                            }
                        }

                        if (initialMetadataFailuresAsInnerDependencies.Count > 0)
                        {
                            if (Data.commonThingsThatWereDependenciesToQueue.Value == null)
                            {
                                Data.commonThingsThatWereDependenciesToQueue.Value = initialMetadataFailuresAsInnerDependencies;
                            }
                            else
                            {
                                Data.commonThingsThatWereDependenciesToQueue.Value = Data.commonThingsThatWereDependenciesToQueue.Value.Concat(initialMetadataFailuresAsInnerDependencies);
                            }
                        }
                    }
                    // run function which reinserts dependencies into topLevelChanges in sorted order and loops when true is returned for reprocessing
                    while (Data.doesPreprocessingNeedToRepeatForDependencies.TypedProcess());

                    return false; // false for not shutdown
                },
                toReturn);

            #endregion

            #region onBatchProcessedSendStatusMessages

            var onBatchProcessedSendStatusMessages = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonSynchronouslyProcessed = commonSynchronouslyProcessed,
                    commonAsynchronouslyProcessed = commonAsynchronouslyProcessed
                },
                (Data, errorToAccumulate) =>
                {
                    if (Data.commonSynchronouslyProcessed.Value.Count != 0)
                    {
                        int syncToCount = Data.commonSynchronouslyProcessed.Value.Count(syncProcess => syncProcess.Direction == SyncDirection.To);
                        int syncFromCount = Data.commonSynchronouslyProcessed.Value.Count - syncToCount;

                        MessageEvents.FireNewEventMessage(
                            (syncToCount == 0
                                ? string.Empty
                                : syncToCount.ToString() +
                                    " change" + (syncToCount == 1 ? string.Empty : "s") + " synced to server" +
                                    (syncFromCount == 0
                                        ? string.Empty
                                        : " and ")) +
                                (syncFromCount == 0
                                    ? string.Empty
                                    : syncFromCount.ToString() +
                                        " change" + (syncFromCount == 1 ? string.Empty : "s") + " synced from server"),
                            EventMessageLevel.Important,
                            SyncBoxId: syncBox.SyncBoxId,
                            DeviceId: syncBox.CopiedSettings.DeviceId);
                    }
                    if (Data.commonAsynchronouslyProcessed.Value.Count != 0)
                    {
                        int syncToCount = Data.commonAsynchronouslyProcessed.Value.Count(syncProcess => syncProcess.Direction == SyncDirection.To);
                        int syncFromCount = Data.commonAsynchronouslyProcessed.Value.Count - syncToCount;

                        MessageEvents.FireNewEventMessage(
                            (syncToCount == 0
                                ? string.Empty
                                : syncToCount.ToString() +
                                    " file" + (syncToCount == 1 ? string.Empty : "s") + " queued for upload" +
                                    (syncFromCount == 0
                                        ? string.Empty
                                        : " and ")) +
                                (syncFromCount == 0
                                    ? string.Empty
                                    : syncFromCount.ToString() +
                                        " file" + (syncFromCount == 1 ? string.Empty : "s") + " queued for download"),
                            EventMessageLevel.Important,
                            SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                            DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                    }
                },
                toReturn);

            #endregion

            #region buildChangesForCommunicationArrayAndSetErrorsToQueueToRemainingChanges

            var buildChangesForCommunicationArrayAndSetErrorsToQueueToRemainingChanges = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonOutputChanges = commonOutputChanges,
                    commonPreprocessedEvents = commonPreprocessedEvents,
                    traceChangesEnumerableWithFlowState = traceChangesEnumerableWithFlowState,
                    oneLineChangeFlowTrace = oneLineChangeFlowTrace,
                    positionInChangeFlow = positionInChangeFlow,
                    changesToTrace = changesToTrace,
                    commonErrorsToQueue = commonErrorsToQueue,
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue,
                    commonSuccessfulEventIds = commonSuccessfulEventIds,
                    queuedFailures = new GenericHolder<IEnumerable<FileChange>>(null)
                },
                (Data, errorToAccumulate) =>
                {
                    // store changes not already processed as a new array
                    PossiblyStreamableFileChange[] innerChangesForCommunication;

                    List<PossiblyStreamableFileChange> normalSyncToCommuncationChanges = null;
                    List<PossiblyStreamableFileChange> changesNotForCommunication = null;
                    HashSet<FileChange> changesInCommunicationOrDequeuedDependencies = new HashSet<FileChange>();

                    foreach (PossiblyStreamableFileChange currentOutputChange in Data.commonOutputChanges.Value.Except(Data.commonPreprocessedEvents.Value))
                    {
                        try
                        {
                            switch (currentOutputChange.FileChange.Direction)
                            {
                                case SyncDirection.From:
                                    if (changesNotForCommunication == null)
                                    {
                                        changesNotForCommunication = new List<PossiblyStreamableFileChange>();
                                    }

                                    changesNotForCommunication.Add(currentOutputChange);
                                    break;

                                case SyncDirection.To:
                                    if (normalSyncToCommuncationChanges == null)
                                    {
                                        normalSyncToCommuncationChanges = new List<PossiblyStreamableFileChange>();
                                    }

                                    normalSyncToCommuncationChanges.Add(currentOutputChange);

                                    changesInCommunicationOrDequeuedDependencies.Add(currentOutputChange.FileChange);
                                    break;

                                default:
                                    throw new NotSupportedException("Unknown currentOutputChange FileChange Direction: " + currentOutputChange.FileChange.Direction.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += ex;
                        }
                    }

                    if (normalSyncToCommuncationChanges == null)
                    {
                        innerChangesForCommunication = new PossiblyStreamableFileChange[0];
                    }
                    else
                    {
                        innerChangesForCommunication = normalSyncToCommuncationChanges.ToArray();
                    }

                    if (changesNotForCommunication != null)
                    {
                        // for advanced trace, SyncRunErrorSyncFromForCommunication
                        Data.oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunErrorSyncFromForCommunication, changesNotForCommunication.Select(currentChangeNotForCommunication => currentChangeNotForCommunication.FileChange), Data.traceChangesEnumerableWithFlowState, Data.positionInChangeFlow, Data.changesToTrace);
                    }

                    // outputChanges is not used again,
                    // it is redefined after communication and after reassigning dependencies

                    if (Data.commonThingsThatWereDependenciesToQueue.Value != null)
                    {
                        foreach (FileChange thingThatWasADependency in Data.commonThingsThatWereDependenciesToQueue.Value)
                        {
                            changesInCommunicationOrDequeuedDependencies.Add(thingThatWasADependency);
                        }
                    }

                    // store the current errors which will not be making it to the next step (communication) to an array
                    PossiblyStreamableAndPossiblyPreexistingErrorFileChange[] errorsToRequeue = Data.commonErrorsToQueue.Value
                        .Where(currentErrorToQueue => !changesInCommunicationOrDequeuedDependencies.Contains(currentErrorToQueue.FileChange))
                        .ToArray();

                    // declare the enumerable of failures for the log
                    IEnumerable<FileChange> failuresBeforeCommunicationToLog;

                    // add errors to failure queue (which will exclude changes which were succesfully completed)
                    Data.commonThisEngine.RequeueFailures(
                        new List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>(errorsToRequeue), // all communication errors and some completed events
                        Data.commonSuccessfulEventIds, // completed event ids, used to filter errors to just errors
                        errorToAccumulate, // return error to aggregate with more errors
                        out failuresBeforeCommunicationToLog); // output the enumerable of failures for the log

                    Data.queuedFailures.Value = failuresBeforeCommunicationToLog;

                    // redefine the errors enumeration by all the changes which will be used in the next step (communication)
                    Data.commonErrorsToQueue.Value = new List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>(
                        innerChangesForCommunication
                            .Select(currentChangeForCommunication => new PossiblyStreamableAndPossiblyPreexistingErrorFileChange(
                                false,
                                currentChangeForCommunication.FileChange,
                                currentChangeForCommunication.Stream)));

                    return innerChangesForCommunication;
                },
                toReturn);

            #endregion

            #region isInitialSyncId

            var isInitialSyncId = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this
                },
                (Data, errorToAccumulate) =>
                {
                    // declare a string to store the previously recorded sync ID
                    string syncString;
                    return (syncString = (Data.commonThisEngine.syncData.getLastSyncId ?? CLDefinitions.CLDefaultSyncID)) == CLDefinitions.CLDefaultSyncID;
                },
                toReturn);

            #endregion

            #region fillInServerUidsOnInitialSyncAndReturnWhetherErrorOccurred

            var fillInServerUidsOnInitialSyncAndReturnWhetherErrorOccurred = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonChangesForCommunication = commonChangesForCommunication
                },
                (Data, errorToAccumulate) =>
                {
                    try
                    {
                        FilePath rootPath = Data.commonThisEngine.syncBox.CopiedSettings.SyncRoot;

                        CLHttpRestStatus getRootStatus;
                        JsonContracts.Metadata rootResponse;
                        CLError rootError = Data.commonThisEngine.httpRestClient.GetMetadata(
                            rootPath,
                            /* isFolder */ true,
                            Data.commonThisEngine.HttpTimeoutMilliseconds,
                            out getRootStatus,
                            out rootResponse);

                        if (getRootStatus != CLHttpRestStatus.Success)
                        {
                            const string rootErrorString = "Error getting folder contents for root object";

                            if (rootError == null)
                            {
                                throw new NullReferenceException("Error getting folder contents for root object");
                            }
                            else
                            {
                                throw new AggregateException(rootErrorString, rootError.GrabExceptions());
                            }
                        }

                        if (rootResponse == null)
                        {
                            throw new NullReferenceException("Folder contents for root object contains no root object");
                        }

                        if (Data.commonChangesForCommunication.Value != null)
                        {
                            foreach (PossiblyStreamableFileChange changeForCommunication in Data.commonChangesForCommunication.Value)
                            {
                                if (!FilePathComparer.Instance.Equals(changeForCommunication.FileChange.NewPath.Parent, rootPath))
                                {
                                    throw new ArgumentException("Cannot initially sync a change which is not directly under the root folder");
                                }
                                if (changeForCommunication.FileChange.Type != FileChangeType.Created)
                                {
                                    throw new ArgumentException("Cannot initially sync a change which is not a creation");
                                }

                                changeForCommunication.FileChange.Metadata.ParentFolderServerUid = rootResponse.ServerUid;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorToAccumulate.Value += ex;
                        return true;
                    }
                    return false;
                },
                toReturn);

            #endregion

            #region fillInServerUidsWhereNecessaryForCommunicationAndReturnWhetherErrorOccurred

            var fillInServerUidsWhereNecessaryForCommunicationAndReturnWhetherErrorOccurred = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonChangesForCommunication = commonChangesForCommunication
                },
                (Data, errorToAccumulate) =>
                {
                    try
                    {
                        if (Data.commonChangesForCommunication.Value != null)
                        {
                            foreach (PossiblyStreamableFileChange currentChangeToCommunicate in Data.commonChangesForCommunication.Value)
                            {
                                switch (currentChangeToCommunicate.FileChange.Type)
                                {
                                    case FileChangeType.Created:
                                    case FileChangeType.Renamed:
                                        string parentServerUid;
                                        CLError parentServerUidError = Data.commonThisEngine.syncData.GetServerUidByNewPath(currentChangeToCommunicate.FileChange.NewPath.Parent.ToString(), out parentServerUid);
                                        if (parentServerUidError != null)
                                        {
                                            throw new AggregateException("Error finding parent folder server uid for current event parent folder path", parentServerUidError.GrabExceptions());
                                        }
                                        if (parentServerUid == null)
                                        {
                                            throw new NullReferenceException("Unable to find parent folder server uid for current event parent folder path");
                                        }
                                        currentChangeToCommunicate.FileChange.Metadata.ParentFolderServerUid = parentServerUid;
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorToAccumulate.Value += ex;
                        return true;
                    }

                    return false;
                },
                toReturn);

            #endregion

            #region handleCredentialsErrorIfAny

            var handleCredentialsErrorIfAny = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonCredentialsError = commonCredentialsError
                },
                (Data, errorToAccumulate) =>
                {
                    if (Data.commonCredentialsError.Value == null
                        || ((CredentialErrorType)Data.commonCredentialsError.Value) != CredentialErrorType.NoError)
                    {
                        lock (Data.commonThisEngine.CredentialErrorDetected)
                        {
                            Data.commonThisEngine.CredentialErrorDetected.Value = Data.commonCredentialsError.Value ?? CredentialErrorType.OtherError;
                        }

                        try
                        {
                            string errorMessage;
                            switch (Data.commonCredentialsError.Value ?? CredentialErrorType.OtherError)
                            {
                                case CredentialErrorType.ExpiredCredentials:
                                    errorMessage = "SyncEngine halted after credentials expired";
                                    break;

                                case CredentialErrorType.OtherError:
                                    errorMessage = "SyncEngine halted after failing to authenticate";
                                    break;

                                //case CredentialErrorType.NoError: // should not happen since we already checked for no error
                                default:
                                    errorMessage = "SyncEngine halted after unknown credentials error: " + ((CredentialErrorType)Data.commonCredentialsError.Value).ToString();
                                    break;
                            }

                            MessageEvents.FireNewEventMessage(
                                errorMessage,
                                EventMessageLevel.Important,
                                /*Error*/new HaltSyncEngineOnAuthenticationFailureErrorInfo(TokenExpired:
                                    Data.commonCredentialsError.Value != null && ((CredentialErrorType)Data.commonCredentialsError.Value) == CredentialErrorType.ExpiredCredentials),
                                Data.commonThisEngine.syncBox.SyncBoxId,
                                Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += ex;
                        }
                    }
                },
                toReturn);

            #endregion

            #region pullSuccessIdsFromCompletedChangesAndPullOutDependencies

            var pullSuccessIdsFromCompletedChangesAndPullOutDependencies = DelegateAndDataHolder.Create(
                new
                {
                    commonCompletedChanges = commonCompletedChanges,
                    commonSuccessfulEventIds = commonSuccessfulEventIds,
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue
                },
                (Data, errorToAccumulate) =>
                {
                    // loop through completed changes
                    foreach (PossiblyChangedFileChange currentCompletedChange in Data.commonCompletedChanges.Value)
                    {
                        // add id of completed changes to list
                        Data.commonSuccessfulEventIds.Add(currentCompletedChange.FileChange.EventId);

                        // try casting the completed change to one with dependencies
                        FileChangeWithDependencies castCurrentCompletedChange = currentCompletedChange.FileChange as FileChangeWithDependencies;
                        // if the completed change is one with dependencies with a count greater than zero, then concatenate the dependencies to all dependencies to requeue
                        if (castCurrentCompletedChange != null
                            && castCurrentCompletedChange.DependenciesCount > 0)
                        {
                            if (Data.commonThingsThatWereDependenciesToQueue.Value == null)
                            {
                                Data.commonThingsThatWereDependenciesToQueue.Value  = castCurrentCompletedChange.Dependencies;
                            }
                            else
                            {
                                Data.commonThingsThatWereDependenciesToQueue.Value  = Data.commonThingsThatWereDependenciesToQueue.Value .Concat(castCurrentCompletedChange.Dependencies);
                            }
                        }
                    }
                },
                toReturn);

            #endregion

            #region mergePostCommunicationChangesToSQLAndReturnWhetherTransactionErrorOccurred

            var mergePostCommunicationChangesToSQLAndReturnWhetherTransactionErrorOccurred = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonCompletedChanges = commonCompletedChanges,
                    commonIncompleteChanges = commonIncompleteChanges,
                    commonChangesInError = commonChangesInError,
                    commonNewSyncId = commonNewSyncId,
                    commonRootFolderServerUid = commonRootFolderServerUid
                },
                (Data, errorToAccumulate) =>
                {
                    long newSyncCounter;

                    CLError completeSyncError = Data.commonThisEngine.syncData.RecordCompletedSync(
                        // concatenate together all the groups of changes (before filtering by whether or not the sql change is needed)
                        (Data.commonCompletedChanges.Value ?? Enumerable.Empty<PossiblyChangedFileChange>()) // changes completed during communication
                        .Concat((Data.commonIncompleteChanges.Value ?? Enumerable.Empty<PossiblyStreamableAndPossiblyChangedFileChange>()) // concatenate changes that still need to be performed
                            .Select(currentIncompleteChange => new PossiblyChangedFileChange(currentIncompleteChange.ResultOrder, currentIncompleteChange.Changed, currentIncompleteChange.FileChange))) // reselect into same format
                        .Concat((Data.commonChangesInError.Value ?? Enumerable.Empty<PossiblyStreamableAndPossiblyChangedFileChangeWithError>()) // concatenate changes that were in error during communication (i.e. conflicts)
                            .Select(currentChangeInError => new PossiblyChangedFileChange(currentChangeInError.ResultOrder, currentChangeInError.Changed, currentChangeInError.FileChange))), // reselect into same format

                        Data.commonNewSyncId.Value,
                        (Data.commonCompletedChanges.Value ?? Enumerable.Empty<PossiblyChangedFileChange>()).Select(currentCompletedChange => currentCompletedChange.FileChange.EventId),
                        out newSyncCounter,
                        Data.commonRootFolderServerUid.Value);

                    // failing to merge communicated changes into SQL is a serious error and will cause changes to be lost if the sync id is updated in the database later;
                    // because this may be at least partially unrecoverable, if there was an error, then show the message
                    if (completeSyncError != null)
                    {
                        errorToAccumulate.Value += new AggregateException("Error on completeSyncSql", completeSyncError.GrabExceptions());

                        MessageEvents.FireNewEventMessage(
                            "syncData.completeSyncSql returned an error after communicating changes: " + completeSyncError.errorDescription,
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());

                        return true;
                    }

                    return false;
                },
                toReturn);

            #endregion

            #region appendPostCommunicationErrorsToReturn

            var appendPostCommunicationErrorsToReturn = DelegateAndDataHolder.Create(
                new
                {
                    commonChangesInError = commonChangesInError
                },
                (Data, errorToAccumulate) =>
                {
                    // if there were changes in error during communication, then loop through them to add their streams and exceptions to the return error
                    if (Data.commonChangesInError.Value != null)
                    {
                        // loop through the changes in error during communication to add their streams and exceptions to the return error
                        foreach (PossiblyStreamableAndPossiblyChangedFileChangeWithError grabException in Data.commonChangesInError.Value)
                        {
                            errorToAccumulate.Value += grabException.Stream;
                            errorToAccumulate.Value += grabException.Error;
                        }
                    }
                },
                toReturn);

            #endregion

            #region dequeueFailuresIncludingNulls

            var dequeueFailuresIncludingNulls = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    verifyUnderFailureTimerLock = verifyUnderFailureTimerLock,
                    commonDequeuedFailuresIncludingNulls = commonDequeuedFailuresIncludingNulls
                },
                (Data, errorToAccumulate) =>
                {
                    Data.verifyUnderFailureTimerLock.Process();

                    // Initialize and fill an array of FileChanges dequeued from the failure queue
                    Data.commonDequeuedFailuresIncludingNulls.Value = new FileChange[Data.commonThisEngine.FailedChangesQueue.Count];
                    for (int currentQueueIndex = 0; currentQueueIndex < Data.commonDequeuedFailuresIncludingNulls.Value.Length; currentQueueIndex++)
                    {
                        Data.commonDequeuedFailuresIncludingNulls.Value[currentQueueIndex] = Data.commonThisEngine.FailedChangesQueue.Dequeue();
                    }
                },
                toReturn);

            #endregion

            #region assignDependenciesAfterCommunication

            var assignDependenciesAfterCommunication = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    verifyUnderFailureTimerLock = verifyUnderFailureTimerLock,
                    commonTopLevelErrors = commonTopLevelErrors,
                    commonNullErrorFoundInFailureQueue = commonNullErrorFoundInFailureQueue,
                    commonSuccessfulEventIds = commonSuccessfulEventIds,
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue,
                    commonIncompleteChanges = commonIncompleteChanges,
                    commonChangesInError = commonChangesInError,
                    commonDequeuedFailuresIncludingNulls = commonDequeuedFailuresIncludingNulls,
                    commonOutputChanges = commonOutputChanges,
                    commonErrorsToQueue = commonErrorsToQueue,
                    traceChangesEnumerableWithFlowState = traceChangesEnumerableWithFlowState,
                    oneLineChangeFlowTrace = oneLineChangeFlowTrace,
                    positionInChangeFlow = positionInChangeFlow,
                    changesToTrace = changesToTrace
                },
                (Data, errorToAccumulate) =>
                {
                    Data.verifyUnderFailureTimerLock.Process();

                    Data.commonNullErrorFoundInFailureQueue.Value = false;

                    // Define a boolean to store whether an error was requeued, defaulting to false
                    bool atLeastOneErrorAdded = false;

                    // Try/finally to reassign dependencies (between changes left to complete, errors that need to be reprocessed, changes which cannot be processed because they are uploads without streams, and changes in the event source which should also be compared)
                    // On finally, if an error was requeued, start the failure queue timer
                    try
                    {
                        // Try/catch to reassign dependencies (between changes left to complete, errors that need to be reprocessed, changes which cannot be processed because they are uploads without streams, and changes in the event source which should also be compared)
                        // On catch, requeue changes which were dequeued from the error queue and rethrow the exception
                        try
                        {
                            // Sort list of successully completed event ids so it can be used for binary search
                            Data.commonSuccessfulEventIds.Sort();

                            // Create a list of file upload changes which cannot be processed because they are missing streams
                            List<PossiblyStreamableFileChange> uploadFilesWithoutStreams = new List<PossiblyStreamableFileChange>(
                                (Data.commonThingsThatWereDependenciesToQueue.Value ?? Enumerable.Empty<FileChange>())
                                    .Select(uploadFileWithoutStream => new PossiblyStreamableFileChange(uploadFileWithoutStream, null))); // reselected to appropriate format

                            CLError postCommunicationDependencyError;

                            //Func<FileChange, GenericHolder<bool>, bool> checkForNullAndMark = (toCheck, nullFound) =>
                            //{
                            //    if (toCheck == null)
                            //    {
                            //        nullErrorFound.Value = true;
                            //        return false;
                            //    }

                            //    return true;
                            //};

                            lock (Data.commonThisEngine.FailedOutTimer.TimerRunningLocker)
                            {
                                bool previousFailedOutChange = Data.commonThisEngine.FailedOutChanges.Count > 0;

                                // Declare enumerable for errors to set after dependency calculations
                                // (will be top level a.k.a. not have any dependencies)
                                IEnumerable<FileChange> topLevelErrors;

                                IEnumerable<PossiblyStreamableFileChange> outputChanges;

                                // Assign dependencies through a callback to the event source,
                                // between changes left to complete, errors that need to be reprocessed, changes which cannot be processed because they are uploads without streams, and changes in the event source which should also be compared;
                                // Store any error returned
                                postCommunicationDependencyError = Data.commonThisEngine.syncData.dependencyAssignment(

                                    // first pass the enumerable of processing changes from the incomplete changes and the changes which cannot be processed due to a lack of Stream
                                    (Data.commonIncompleteChanges.Value ?? Enumerable.Empty<PossiblyStreamableAndPossiblyChangedFileChange>())
                                        .Select(currentIncompleteChange => new PossiblyStreamableFileChange(currentIncompleteChange.FileChange, currentIncompleteChange.Stream))
                                        .Concat(uploadFilesWithoutStreams),

                                    // second pass the enumerable of errors from the changes which had an error during communication and the changes that were in the queue for reprocessing
                                    Data.commonDequeuedFailuresIncludingNulls.Value

                                        // filter for non-nulls, also if a null is found then mark it
                                        .Where(dequeuedFailure =>
                                            {
                                                if (dequeuedFailure == null)
                                                {
                                                    Data.commonNullErrorFoundInFailureQueue.Value = true;
                                                    return false;
                                                }
                                                return true;
                                            })
                                        .Concat((Data.commonChangesInError.Value ?? Enumerable.Empty<PossiblyStreamableAndPossiblyChangedFileChangeWithError>())
                                        .Select(currentChangeInError => currentChangeInError.FileChange)
                                        .Where(currentChangeInError => currentChangeInError != null)),// FileChange could be null for errors if there was an exeption but no FileChange was built

                                    // output changes to process
                                    out outputChanges,

                                    // output changes to put into failure queue for reprocessing
                                    out topLevelErrors);

                                Data.commonOutputChanges.Value = outputChanges;
                                Data.commonTopLevelErrors.Value = topLevelErrors;

                                if (previousFailedOutChange
                                    && Data.commonThisEngine.FailedOutChanges.Count == 0)
                                {
                                    Data.commonThisEngine.FailedOutTimer.TriggerTimerCompletionImmediately();
                                }
                            }

                            // if there was an error assigning dependencies, then rethrow exception for null outputs or aggregate error to return and continue for non-null outputs
                            if (postCommunicationDependencyError != null)
                            {
                                // if either output is null, then rethrow the exception
                                if (Data.commonOutputChanges.Value == null
                                    || Data.commonTopLevelErrors.Value == null)
                                {
                                    throw new AggregateException("Error on dependencyAssignment and outputs are not set", postCommunicationDependencyError.GrabExceptions());
                                }
                                // else if both outputs were not null, then aggregate the exception to the return error
                                else
                                {
                                    errorToAccumulate.Value += new AggregateException("Error on dependencyAssignment", postCommunicationDependencyError.GrabExceptions());
                                }
                            }

                            // outputChanges now contains the dependency-assigned changes left to process after communication (from incompleteChanges)

                            // if there were any changes to process, then set redefine the thingsThatWereDependenciesToQueue and outputChanges appropriately
                            if (Data.commonOutputChanges.Value != null)
                            {
                                // if there were any file changes which could not be uploaded due to a missing stream,
                                // then assign thingsThatWereDependenciesToQueue by processing changes which were changes that could not be processed
                                if (uploadFilesWithoutStreams.Count > 0)
                                {
                                    Data.commonThingsThatWereDependenciesToQueue.Value = Data.commonOutputChanges.Value
                                        .Select(outputChange => outputChange.FileChange)
                                        .Intersect(Data.commonThingsThatWereDependenciesToQueue.Value);
                                }

                                // if the enumerable of dependency changes to requeue has been cleared out (exists but contains nothing), then redefine to null
                                if (Data.commonThingsThatWereDependenciesToQueue.Value != null
                                    && !Data.commonThingsThatWereDependenciesToQueue.Value.Any())
                                {
                                    Data.commonThingsThatWereDependenciesToQueue.Value = null;
                                }

                                // if the resulting enumerable of dependency changes to requeue remains, then set the processing changes to the changes not in that enumerable
                                if (Data.commonThingsThatWereDependenciesToQueue.Value != null)
                                {
                                    Data.commonOutputChanges.Value = Data.commonOutputChanges.Value.Where(outputChange => !Data.commonThingsThatWereDependenciesToQueue.Value.Contains(outputChange.FileChange));
                                }
                            }

                            // For advanced trace, DependencyAssignmentOutputChanges and DependencyAssignmentTopLevelErrors
                            Data.oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.DependencyAssignmentOutputChanges, Data.commonOutputChanges.Value.Select(outputChange => outputChange.FileChange), Data.traceChangesEnumerableWithFlowState, Data.positionInChangeFlow, Data.changesToTrace);
                            Data.oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.DependencyAssignmentTopLevelErrors, Data.commonTopLevelErrors.Value, Data.traceChangesEnumerableWithFlowState, Data.positionInChangeFlow, Data.changesToTrace);

                            // outputChanges now excludes any FileChanges which overlapped with the existing list of thingsThatWereDependenciesToQueue
                            // (because that means the changes are file uploads without FileStreams and cannot be processed now)

                            Data.commonErrorsToQueue.Value = new List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange>((Data.commonOutputChanges.Value ?? Enumerable.Empty<PossiblyStreamableFileChange>())
                                .Select(currentOutputChange => new PossiblyStreamableAndPossiblyPreexistingErrorFileChange(false, currentOutputChange.FileChange, currentOutputChange.Stream)));

                            // errorsToQueue now contains the outputChanges from after communication and dependency assignment
                            // (errors from communication and dependency assignment will be added immediately to failure queue

                            // Loop through errors returned from reassigning dependencies to requeue for failure processing
                            foreach (FileChange currentTopLevelError in Data.commonTopLevelErrors.Value ?? Enumerable.Empty<FileChange>())
                            {
                                // Requeue change for failure processing
                                Data.commonThisEngine.FailedChangesQueue.Enqueue(currentTopLevelError);

                                // Mark that a change was queued for failure processing
                                atLeastOneErrorAdded = true;
                            }
                        }
                        catch
                        {
                            // On error of assigning dependencies,
                            // put all the original failure queue items back in the failure queue;
                            // finally, rethrow the exception
                            for (int currentQueueIndex = 0; currentQueueIndex < Data.commonDequeuedFailuresIncludingNulls.Value.Length; currentQueueIndex++)
                            {
                                Data.commonThisEngine.FailedChangesQueue.Enqueue(Data.commonDequeuedFailuresIncludingNulls.Value[currentQueueIndex]);

                                atLeastOneErrorAdded = true;
                            }
                            throw;
                        }
                    }
                    finally
                    {
                        // No matter what, if even one change had been requeued to the failure queue, then start the failure queue timer
                        if (atLeastOneErrorAdded)
                        {
                            Data.commonThisEngine.FailureTimer.StartTimerIfNotRunning();
                        }
                        else if (Data.commonNullErrorFoundInFailureQueue.Value)
                        {
                            Data.commonThisEngine.FailedChangesQueue.Enqueue(null);
                            Data.commonThisEngine.FailureTimer.StartTimerIfNotRunning();
                        }
                    }
                },
                toReturn);

            #endregion

            #region completeSynchronousChangesAfterCommunicationAndReturnRemainingAsynchronousTasks

            var completeSynchronousChangesAfterCommunicationAndReturnRemainingAsynchronousTasks = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonSynchronouslyProcessed = commonSynchronouslyProcessed,
                    commonOutputChanges = commonOutputChanges,
                    commonSuccessfulEventIds = commonSuccessfulEventIds,
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue,
                    commonTempDownloadsFolder = commonTempDownloadsFolder,
                    completeEventInSqlAndReturnWhetherErrorOccurred = completeEventInSqlAndReturnWhetherErrorOccurred
                },
                (Data, errorToAccumulate) =>
                {
                    List<PossiblyStreamableFileChangeWithUploadDownloadTask> asyncTasksToRun = new List<PossiblyStreamableFileChangeWithUploadDownloadTask>();
                    
                    // Create a new list for changes which can be performed synchronously
                    Data.commonSynchronouslyProcessed.Value = new List<FileChange>();

                    // Synchronously complete all local operations without dependencies (exclude file upload/download) and record successful events;
                    // If a completed event has dependencies, stick them on the end of the current batch;
                    // If an event fails to complete, leave it on errorsToQueue so it will be added to the failure queue later
                    foreach (PossiblyStreamableFileChange topLevelChange in Data.commonOutputChanges.Value)
                    {
                        // Declare event id for a successfully performed synchronous change
                        Nullable<long> successfulEventId;
                        // Declare Task for uploading or downloading files to perform asynchronously
                        Nullable<AsyncUploadDownloadTask> asyncTask;

                        // run private method which handles performing the action of a single FileChange, storing any exceptions thrown (which are caught and returned)
                        Exception completionException = Data.commonThisEngine.CompleteFileChange(
                            topLevelChange, // change to perform
                            Data.commonThisEngine.FailureTimer, // timer for failure queue
                            out successfulEventId, // output successful event id or null
                            out asyncTask, // out async upload or download task to perform or null
                            Data.commonTempDownloadsFolder); // full path location of folder to store temp file downloads

                        // if there was a non-null and valid event id output as succesful,
                        // then add to synchronous list, add to success list, remove from errors, and check and concatenate any dependent FileChanges
                        if (successfulEventId != null
                            && (long)successfulEventId > 0)
                        {
                            // add change to synchronous list
                            Data.commonSynchronouslyProcessed.Value.Add(topLevelChange.FileChange);

                            // add successful id for completed event
                            Data.commonSuccessfulEventIds.Add((long)successfulEventId);

                            Data.completeEventInSqlAndReturnWhetherErrorOccurred.TypedData.eventIdToComplete.Value = (long)successfulEventId;
                            if (Data.completeEventInSqlAndReturnWhetherErrorOccurred.TypedProcess())
                            {
                                return null; // serious error if unable to complete an event in the database, engine now scheduled to halt so return null for no more sync or async processing
                            }

                            // try to cast the successful change as one with dependencies
                            FileChangeWithDependencies changeWithDependencies = topLevelChange.FileChange as FileChangeWithDependencies;
                            // if change was one with dependencies and has a dependency, then concatenate the dependencies into thingsThatWereDependenciesToQueue
                            if (changeWithDependencies != null
                                && changeWithDependencies.DependenciesCount > 0)
                            {
                                if (Data.commonThingsThatWereDependenciesToQueue.Value == null)
                                {
                                    Data.commonThingsThatWereDependenciesToQueue.Value = changeWithDependencies.Dependencies;
                                }
                                else
                                {
                                    Data.commonThingsThatWereDependenciesToQueue.Value = Data.commonThingsThatWereDependenciesToQueue.Value.Concat(changeWithDependencies.Dependencies);
                                }
                            }
                        }
                        // else if there was not a valid successful event id, then process for errors or async tasks
                        else
                        {
                            // if there was an exception, aggregate into returned error
                            if (completionException != null)
                            {
                                errorToAccumulate.Value += completionException;
                            }

                            // if there was an async task to perform, then add it to list to start
                            if (asyncTask != null)
                            {
                                // add task to list to start
                                asyncTasksToRun.Add(new PossiblyStreamableFileChangeWithUploadDownloadTask(topLevelChange,
                                    (AsyncUploadDownloadTask)asyncTask));
                            }
                        }
                    }

                    return asyncTasksToRun;
                },
                toReturn);

            #endregion

            #region startAndReturnPostCommunicationAsynchronousChanges

            var startAndReturnPostCommunicationAsynchronousChanges = DelegateAndDataHolder.Create(
                new
                {
                    asyncTasksToRun = new GenericHolder<List<PossiblyStreamableFileChangeWithUploadDownloadTask>>(null),
                    commonAsynchronouslyProcessed = commonAsynchronouslyProcessed,
                    finalizeAndStartAsyncTask = finalizeAndStartAsyncTask
                },
                (Data, errorToAccumulate) =>
                {
                    if (Data.asyncTasksToRun.Value != null)
                    {
                        Data.commonAsynchronouslyProcessed.Value = new List<FileChange>();

                        // Asynchronously fire off all remaining upload/download operations without dependencies
                        foreach (PossiblyStreamableFileChangeWithUploadDownloadTask asyncTask in Data.asyncTasksToRun.Value)
                        {
                            // try/catch to process async task, on catch append exception to return
                            try
                            {
                                Data.finalizeAndStartAsyncTask.TypedData.asyncTask.Value = asyncTask;
                                Data.finalizeAndStartAsyncTask.Process();
                            }
                            catch (Exception ex)
                            {
                                // append exception to return error
                                errorToAccumulate.Value += ex;
                            }
                        }
                    }
                },
                toReturn);

            #endregion

            #region sendFinishedMessage

            var sendFinishedMessage = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this
                },
                (Data, errorToAccumulate) =>
                {
                    MessageEvents.FireNewEventMessage(
                        "Finished processing sync changes",
                        EventMessageLevel.Minor,
                        SyncBoxId: Data.commonThisEngine.syncBox.SyncBoxId,
                        DeviceId: Data.commonThisEngine.syncBox.CopiedSettings.DeviceId);
                },
                toReturn);

            #endregion

            #region addDependenciesBackToProcessingQueue

            var addDependenciesBackToProcessingQueue = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonThingsThatWereDependenciesToQueue = commonThingsThatWereDependenciesToQueue
                },
                (Data, errorToAccumulate) =>
                {
                    // if there were dependencies to queue, then process adding them back to the event source to come back on next sync
                    if (Data.commonThingsThatWereDependenciesToQueue.Value != null)
                    {
                        // try/catch to add dependencies to processing queue in event source to come back on next sync, on catch add dependencies to failure queue
                        try
                        {
                            // add all dependencies to the top of the processing queue in the order they were discovered;
                            // if there are any errors in queueing, add the changes to the failure queue instead;
                            // also if there are any errors, add them to the returned aggregated errors
                            GenericHolder<List<FileChange>> queueingErrors = new GenericHolder<List<FileChange>>();
                            // add dependencies to top of queue for next sync run when it calls back to event source
                            CLError queueingError = Data.commonThisEngine.syncData.addChangesToProcessingQueue(Data.commonThingsThatWereDependenciesToQueue.Value, /* add to top */ true, queueingErrors);
                            // if an error list was set, then add failed changes to failure queue
                            if (queueingErrors.Value != null)
                            {
                                // lock on failure queue timer to modify failure queue
                                lock (Data.commonThisEngine.FailureTimer.TimerRunningLocker)
                                {
                                    // define bool for whether at least one failure was added
                                    bool atLeastOneFailureAdded = false;

                                    // loop through changes failed to add to processing queue
                                    foreach (FileChange currentQueueingError in queueingErrors.Value)
                                    {
                                        // enqueue current failed change to failure queue
                                        Data.commonThisEngine.FailedChangesQueue.Enqueue(currentQueueingError);

                                        // mark that a failure was added
                                        atLeastOneFailureAdded = true;
                                    }

                                    // if at least one failure was added, then start the failure queue timer
                                    if (atLeastOneFailureAdded)
                                    {
                                        Data.commonThisEngine.FailureTimer.StartTimerIfNotRunning();
                                    }
                                }
                            }
                            // if there was an error queueing dependencies to processing queue in event source, then aggregate error to return error
                            if (queueingError != null)
                            {
                                errorToAccumulate.Value += new AggregateException("Error adding dependencies to processing queue after sync", queueingError.GrabExceptions());
                            }
                        }
                        // on catch, add dependencies to failure queue
                        catch (Exception ex)
                        {
                            // on serious error in queueing changes,
                            // instead add them all to the failure queue add add the error to the returned aggregate errors

                            // lock on failure queue timer to modify failure queue
                            lock (Data.commonThisEngine.FailureTimer.TimerRunningLocker)
                            {
                                // define bool for whether at least one failure was added
                                bool atLeastOneFailureAdded = false;

                                // loop through changes failed to add to processing queue
                                foreach (FileChange currentQueueingError in Data.commonThingsThatWereDependenciesToQueue.Value)
                                {
                                    // enqueue current failed change to failure queue
                                    Data.commonThisEngine.FailedChangesQueue.Enqueue(currentQueueingError);

                                    // mark that a failure was added
                                    atLeastOneFailureAdded = true;
                                }

                                // if at least one failure was added, then start the failure queue timer
                                if (atLeastOneFailureAdded)
                                {
                                    Data.commonThisEngine.FailureTimer.StartTimerIfNotRunning();
                                }
                            }
                            // append exception to return error
                            errorToAccumulate.Value += ex;
                        }
                    }
                },
                toReturn);

            #endregion

            #region requeueFinalFailures

            var requeueFinalFailures = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    commonErrorsToQueue = commonErrorsToQueue,
                    commonSuccessfulEventIds = commonSuccessfulEventIds
                },
                (Data, errorToAccumulate) =>
                {
                    IEnumerable<FileChange> syncRunEndFailuresToLog;

                    // add errors to failure queue (which will exclude changes which were succesfully completed)
                    Data.commonThisEngine.RequeueFailures(Data.commonErrorsToQueue.Value, // all errors and some completed events
                        Data.commonSuccessfulEventIds, // completed event ids, used to filter errors to just errors
                        errorToAccumulate, // return error to aggregate with more errors
                        out syncRunEndFailuresToLog); // output enumerable of errors to log

                    return syncRunEndFailuresToLog;
                },
                toReturn);

            #endregion

            #region disposeErrorStreamsAndLogErrors

            var disposeErrorStreamsAndLogErrors = DelegateAndDataHolder.Create(
                new
                {
                    commonThisEngine = this,
                    syncStatus = new GenericHolder<string>(null)
                },
                (Data, errorToAccumulate) =>
                {
                    // if the output error aggregate contains any errors,
                    // then add the sync status (to notify position in case of catastrophic failure),
                    // dispose all FileStreams,
                    // and log all errors
                    if (errorToAccumulate.Value != null)
                    {
                        // add latest sync status to error so it will appear in error log
                        errorToAccumulate.Value.errorInfo.Add(CLError.ErrorInfo_Sync_Run_Status, Data.syncStatus.Value);
                        // dequeue all Streams from the return error and dispose them, storing any error that occurs
                        CLError disposalError = errorToAccumulate.Value.DequeueStreams().DisposeAllStreams();
                        // if there was an error disposing streams, then aggregate all errors to return error
                        if (disposalError != null)
                        {
                            foreach (Exception disposalException in disposalError.GrabExceptions())
                            {
                                errorToAccumulate.Value += disposalException;
                            }
                        }

                        // define boolean to store whether the only content of the return error are streams to dispose, defaulting to true
                        bool onlyErrorIsFileStream = true;
                        // loop through all exceptions in the return error
                        foreach (Exception errorException in errorToAccumulate.Value.GrabExceptions())
                        {
                            // if the current exception is not the exception added when a stream is added to the return error without an exception,
                            // then mark that a non-stream content was found in the return error and stop checking
                            if (errorException.Message != CLError.StreamFirstMessage)
                            {
                                onlyErrorIsFileStream = false;
                                break;
                            }
                        }
                        // if there was any error content besides streams to dispose, then log the errors
                        if (!onlyErrorIsFileStream)
                        {
                            errorToAccumulate.Value.LogErrors(Data.commonThisEngine.syncBox.CopiedSettings.TraceLocation, Data.commonThisEngine.syncBox.CopiedSettings.LogErrors);
                        }
                    }
                },
                toReturn);

            #endregion

            #endregion

            // lock to prevent multiple simultaneous syncing on the current SyncEngine
            lock (RunLocker)
            {
                var initialHaltException = checkHaltsAndErrors.TypedProcess();
                if (initialHaltException != null)
                {
                    return initialHaltException;
                }

                var disconnectedException = checkInternetConnection.TypedProcess();
                if (disconnectedException != null)
                {
                    return disconnectedException.Value;
                }

                syncStatus = "Sync Run entered";

                // try/catch for primary sync logic, exception is aggregated to return
                try
                {
                    var startingUpException = stopOnFullHaltOrStartup.TypedProcess();
                    if (startingUpException != null)
                    {
                        return startingUpException;
                    }

                    if (getIsShutdown.TypedProcess())
                    {
                        try
                        {
                            throw new ObjectDisposedException("Unable to start new Sync Run, SyncEngine has been shut down");
                        }
                        catch (Exception ex)
                        {
                            return ex;
                        }
                    }

                    SyncStillRunning(commonRunThreadId);

                    // If download temp folder was marked for cleaning
                    if (checkTempNeedsCleaning.TypedProcess())
                    {
                        cleanTempDownloads.Process();
                    }

                    syncStatus = "Sync Run temp download files cleaned";

                    SyncStillRunning(commonRunThreadId);

                    if (getIsShutdown.TypedProcess())
                    {
                        return toReturn.Value;
                    }

                    try
                    {
                        // lock on timer for access to failure queue
                        lock (FailureTimer.TimerRunningLocker)
                        {
                            commonWithinFailureTimerLock.Value = true; // set back to false on finally outside of the lock

                            commonNullErrorFoundInFailureQueue.Value = dequeueNonNullFailuresAndReturnWhetherNullFound.TypedProcess();

                            oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunInitialErrors, commonDequeuedFailuresExcludingNulls.Value.Select(currentInitialError => currentInitialError.FileChange), traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                            // update last status
                            syncStatus = "Sync Run dequeued initial failures for dependency check";

                            errorGrabbingChanges = !grabFileMonitorChangesAndCombineHierarchyWithErrorsAndReturnWhetherSuccessful.TypedProcess();
                        }
                    }
                    finally
                    {
                        commonWithinFailureTimerLock.Value = false;
                    }

                    // if there was no exception thrown (not converted as CLError) when retrieving events to process, then continue processing
                    if (!errorGrabbingChanges)
                    {
                        // outputChanges now contains changes dequeued for processing from the filesystem monitor

                        assignOutputErrorsFromOutputChangesAndOutputErrorsExcludingNulls.Process();

                        // errorsToQueue now contains all errors previously in the failure queue (with bool true for being existing errors)
                        // and errors that occurred while grabbing queued processing changes from file monitor (with bool false for not being existing errors);
                        // it also contains all FileChanges which have yet to process but are already assumed to be in error until explicitly marked successful or removed from this list

                        // update last status
                        syncStatus = "Sync Run grabbed processed changes (with dependencies and final metadata)";

                        SyncStillRunning(commonRunThreadId);

                        if (getIsShutdown.TypedProcess())
                        {
                            return toReturn.Value;
                        }

                        if (preprocessExistingEventsAndReturnWhetherShutdown.TypedProcess())
                        {
                            return toReturn.Value;
                        }

                        // for advanced trace, log SyncRunPreprocessedEventsSynchronous and SyncRunPreprocessedEventsAsynchronous
                        oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunPreprocessedEventsSynchronous, commonSynchronouslyProcessed.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);
                        oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunPreprocessedEventsAsynchronous, commonAsynchronouslyProcessed.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                        onBatchProcessedSendStatusMessages.Process();

                        // after each loop where more FileChanges from previous dependencies are processed,
                        // if any FileChange is synchronously complete or queued for file upload/download then it is removed from errorsToQueue

                        // see notes after reprocessForDependencies is defined to see what it does to errorsToQueue and outputChanges

                        // update last status
                        syncStatus = "Sync Run initial operations completed synchronously or queued";

                        commonChangesForCommunication.Value = buildChangesForCommunicationArrayAndSetErrorsToQueueToRemainingChanges.TypedProcess();

                        // errorToQueue is now defined as the changesForCommunication
                        // (all the previous errors that correspond to FileChanges which will not continue onto communication were added back to the failure queue)

                        // for advanced trace, SyncRunRequeuedFailuresBeforeCommunication and SyncRunChangesForCommunication
                        oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunRequeuedFailuresBeforeCommunication, buildChangesForCommunicationArrayAndSetErrorsToQueueToRemainingChanges.TypedData.queuedFailures.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);
                        oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunChangesForCommunication, (commonChangesForCommunication.Value ?? Enumerable.Empty<PossiblyStreamableFileChange>()).Select(currentChangeForCommunication => currentChangeForCommunication.FileChange), traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                        // update latest status
                        syncStatus = "Sync Run errors queued which were not changes that continued to communication";

                        if (isInitialSyncId.TypedProcess())
                        {
                            if (fillInServerUidsOnInitialSyncAndReturnWhetherErrorOccurred.TypedProcess())
                            {
                                return toReturn.Value;
                            }
                        }
                        else if (fillInServerUidsWhereNecessaryForCommunicationAndReturnWhetherErrorOccurred.TypedProcess())
                        {
                            return toReturn.Value;
                        }

                        // Take events without dependencies that were not fired off in order to perform communication (or Sync From for no events left)

                        // Communicate with server with all the changes to process, storing any exception that occurs
                        commonCommunicationException.Value = CommunicateWithServer(
                            commonChangesForCommunication.Value, // changes to process
                            respondingToPushNotification, // whether the current SyncEngine Run was called for responding to a push notification or on manual polling
                            out completedChanges, // output changes completed during communication
                            out incompleteChanges, // output changes that still need to be performed
                            out changesInError, // output changes that were marked in error during communication (i.e. conflicts)
                            out newSyncId, // output newest sync id from server
                            out credentialsError,
                            out syncRootUid);

                        commonCompletedChanges.Value = completedChanges;
                        commonIncompleteChanges.Value = incompleteChanges;
                        commonChangesInError.Value = changesInError;
                        commonNewSyncId.Value = newSyncId;
                        commonRootFolderServerUid.Value = syncRootUid;

                        commonCredentialsError.Value = credentialsError;

                        handleCredentialsErrorIfAny.Process();

                        // if an exception occurred during server communication, then aggregate it into the return error
                        if (commonCommunicationException.Value != null)
                        {
                            toReturn.Value += commonCommunicationException.Value;
                        }
                        // else if no exception occurred during server communication and the server was not contacted for a new sync id, then only update status
                        else if (newSyncId == null)
                        {
                            RunLocker.Value = true; // sync ran through one time; no longer in initial run state

                            // update latest status
                            syncStatus = "Sync Run communication aborted e.g. Sync To with no events";
                        }
                        else
                        {
                            // update latest status
                            syncStatus = "Sync Run communication complete";

                            // for advanced trace, CommunicationCompletedChanges, CommunicationIncompletedChanges, and CommunicationChangesInError
                            oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.CommunicationCompletedChanges, completedChanges.Select(currentCompletedChange => currentCompletedChange.FileChange), traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);
                            oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.CommunicationIncompletedChanges, incompleteChanges.Select(currentIncompleteChange => currentIncompleteChange.FileChange), traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);
                            oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.CommunicationChangesInError, changesInError.Select(currentChangeInError => currentChangeInError.FileChange), traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                            pullSuccessIdsFromCompletedChangesAndPullOutDependencies.Process();

                            if (mergePostCommunicationChangesToSQLAndReturnWhetherTransactionErrorOccurred.TypedProcess())
                            {
                                return toReturn.Value;
                            }

                            RunLocker.Value = true; // sync ran through one time; no longer in initial run state

                            appendPostCommunicationErrorsToReturn.Process();

                            // update latest status
                            syncStatus = "Sync Run server values merged into database and new sync point persisted";

                            SyncStillRunning(commonRunThreadId);

                            if (getIsShutdown.TypedProcess())
                            {
                                return toReturn.Value;
                            }

                            try
                            {
                                // Within a lock on the failure queue (failureTimer.TimerRunningLocker),
                                // check if each current server action needs to be moved to a dependency under a failure event or a server action in the current batch
                                lock (FailureTimer.TimerRunningLocker)
                                {
                                    commonWithinFailureTimerLock.Value = true; // set back to false on finally outside of the lock

                                    dequeueFailuresIncludingNulls.Process();

                                    // For advanced trace, SyncRunPostCommunicationDequeuedFailures
                                    oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunPostCommunicationDequeuedFailures, commonDequeuedFailuresIncludingNulls.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                                    assignDependenciesAfterCommunication.Process();
                                }
                            }
                            finally
                            {
                                commonWithinFailureTimerLock.Value = false;
                            }

                            // Update latest status
                            syncStatus = "Sync Run post-communication dependencies calculated";

                            SyncStillRunning(commonRunThreadId);

                            if (getIsShutdown.TypedProcess())
                            {
                                return toReturn.Value;
                            }

                            // Complete synchronous changes and get the list for asynchronous tasks to process
                            startAndReturnPostCommunicationAsynchronousChanges.TypedData.asyncTasksToRun.Value = completeSynchronousChangesAfterCommunicationAndReturnRemainingAsynchronousTasks.TypedProcess();

                            // advanced trace, SyncRunPostCommunicationSynchronous
                            oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunPostCommunicationSynchronous, commonSynchronouslyProcessed.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                            // update latest status
                            syncStatus = "Sync Run synchronous post-communication operations complete";

                            SyncStillRunning(commonRunThreadId);

                            if (getIsShutdown.TypedProcess())
                            {
                                //completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                                //incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                                //changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                                //newSyncId = Helpers.DefaultForType<string>();
                                //return new Exception("Shut down in the middle of communication");
                                return toReturn.Value;
                            }

                            SyncStillRunning(commonRunThreadId);

                            if (getIsShutdown.TypedProcess())
                            {
                                //completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                                //incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                                //changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                                //newSyncId = Helpers.DefaultForType<string>();
                                //return new Exception("Shut down in the middle of communication");
                                return toReturn.Value;
                            }

                            startAndReturnPostCommunicationAsynchronousChanges.Process();

                            // advanced trace, SyncRunPostCommunicationAsynchronous
                            oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunPostCommunicationAsynchronous, commonAsynchronouslyProcessed.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                            onBatchProcessedSendStatusMessages.Process();

                            // for any FileChange which was asynchronously queued for file upload or download,
                            // errorsToQueue had that change removed

                            sendFinishedMessage.Process();

                            // update latest status
                            syncStatus = "Sync Run async tasks started after communication (end of Sync)";
                        }
                    }
                }
                catch (Exception ex)
                {
                    // append exception to return error
                    toReturn.Value += ex;
                }

                addDependenciesBackToProcessingQueue.Process();

                // advanced trace, SyncRunEndThingsThatWereDependenciesToQueue
                oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunEndThingsThatWereDependenciesToQueue, commonThingsThatWereDependenciesToQueue.Value, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                // errorsToQueue should contain all FileChanges which were already added back to the failure queue
                // or are asynchronously queued for file upload or download; it may also contain some completed changes
                // which will be checked against successfulEventIds;
                // this should be true regardless of whether a line in the main try/catch threw an exception since it should be up to date at all times

                IEnumerable<FileChange> syncRunEndFailuresToLog = requeueFinalFailures.TypedProcess();

                // advanced trace, SyncRunEndRequeuedFailures
                oneLineChangeFlowTrace(FileChangeFlowEntryPositionInFlow.SyncRunEndRequeuedFailures, syncRunEndFailuresToLog, traceChangesEnumerableWithFlowState, positionInChangeFlow, changesToTrace);

                // errorsToQueue is no longer used (all its errors were added back to the failure queue)

                disposeErrorStreamsAndLogErrors.TypedData.syncStatus.Value = syncStatus;
                disposeErrorStreamsAndLogErrors.Process();

                SyncStoppedRunning(commonRunThreadId);

                // return error aggregate
                return toReturn.Value;
            }
        }

        // returns whether sync engine is halted from reaching the max allowed failures to communicate with server; also fires the status change notification the first time the halt condition is found
        private bool CheckForMaxCommunicationFailuresHalt()
        {
            byte metadataFailureCount;
            byte uploadDownloadFailureCount;
            bool maxFailuresReached;
            lock (MetadataConnectionFailures)
            {
                metadataFailureCount = MetadataConnectionFailures.Value;
            }
            lock (UploadDownloadConnectionFailures)
            {
                uploadDownloadFailureCount = UploadDownloadConnectionFailures.Value;
            }
            maxFailuresReached = metadataFailureCount > MaxNumberOfServerConnectionFailures
                || uploadDownloadFailureCount > MaxNumberOfServerConnectionFailures;

            bool needToNotifyOnInitialHalting = false;
            bool toReturn;

            lock (HaltedOnServerConnectionFailure)
            {
                if (maxFailuresReached)
                {
                    if (!HaltedOnServerConnectionFailure.Value)
                    {
                        HaltedOnServerConnectionFailure.Value = needToNotifyOnInitialHalting = true;
                    }
                }

                toReturn = HaltedOnServerConnectionFailure.Value;
            }

            if (needToNotifyOnInitialHalting)
            {
                MessageEvents.FireNewEventMessage(
                    "SyncEngine halted after repeated failure to communicate over one of the Cloud server domains",
                    EventMessageLevel.Important,
                    /*Error*/new HaltSyncEngineOnConnectionFailureErrorInfo(),
                    syncBox.SyncBoxId,
                    syncBox.CopiedSettings.DeviceId);

                if (statusUpdated != null)
                {
                    try
                    {
                        statusUpdated(statusUpdatedUserState);
                    }
                    catch
                    {
                    }
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Get the current status of this SyncEngine
        /// </summary>
        /// <param name="status">(output) Status of this SyncEngine</param>
        /// <returns>Returns any error that occurred retrieving the status (usually shutdown), if any</returns>
        public CLError GetCurrentStatus(out CLSyncCurrentStatus status)
        {
            try
            {
                // check for shutdown
                bool isShutdown = false;
                Monitor.Enter(FullShutdownToken);
                try
                {
                    isShutdown = FullShutdownToken.IsCancellationRequested;
                }
                catch
                {
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

                if (isShutdown)
                {
                    throw new ObjectDisposedException("this", "Sync already shutdown");
                }

                bool halted;
                bool expiredCredentials;
                lock (HaltedOnServerConnectionFailure)
                {
                    halted = HaltedOnServerConnectionFailure.Value;
                }
                if (!halted)
                {
                    lock (CredentialErrorDetected)
                    {
                        switch (CredentialErrorDetected.Value)
                        {
                            case CredentialErrorType.ExpiredCredentials:
                                halted = true;
                                expiredCredentials = true;
                                break;

                            case CredentialErrorType.NoError:
                                expiredCredentials = false;
                                break;

                            //case CredentialErrorType.OtherError:
                            default:
                                halted = true;
                                expiredCredentials = false;
                                break;
                        }
                    }
                }
                else
                {
                    expiredCredentials = false;
                }

                lock (StatusHolder)
                {
                    if (StatusHolder.Value == null)
                    {
                        status = new CLSyncCurrentStatus(CLSyncCurrentState.Idle, null);
                    }
                    else
                    {
                        status = StatusHolder.Value;
                    }
                }

                // if the sync engine has been marked halted from reaching the max connection failures to a server, then create or rebuild the return status to add the halted status enum flag
                if (halted)
                {
                    if (status == null)
                    {
                        status = new CLSyncCurrentStatus(
                            (expiredCredentials
                                ? CLSyncCurrentState.HaltedOnExpiredCredentials
                                : CLSyncCurrentState.HaltedOnConnectionFailure), null);
                    }
                    else
                    {
                        status = new CLSyncCurrentStatus(
                            status.CurrentState
                                | (expiredCredentials
                                    ? CLSyncCurrentState.HaltedOnExpiredCredentials
                                    : CLSyncCurrentState.HaltedOnConnectionFailure),
                            status.DownloadingFiles.Concat(status.UploadingFiles));
                    }
                }
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<CLSyncCurrentStatus>();
                return ex;
            }
            return null;
        }
        private readonly GenericHolder<CLSyncCurrentStatus> StatusHolder = new GenericHolder<CLSyncCurrentStatus>(null);

        /// <summary>
        /// Call this to terminate all sync threads including active uploads and downloads;
        /// after calling this, a restart is required to sync again
        /// </summary>
        public void Shutdown()
        {
            // shutdown is processed through a locked shutdown token which is checked in synchronous sync run and also in all asynchronous tasks
            Monitor.Enter(FullShutdownToken);
            try
            {
                if (!FullShutdownToken.IsCancellationRequested)
                {
                    FullShutdownToken.Cancel();
                }
            }
            catch
            {
            }
            finally
            {
                Monitor.Exit(FullShutdownToken);
            }

            lock (threadStateKillTimer)
            {
                if (threadStateKillTimer.Value != null)
                {
                    try
                    {
                        threadStateKillTimer.Value.Dispose();
                        threadStateKillTimer.Value = null;
                    }
                    catch
                    {
                    }
                }
            }

            lock (FailureTimer.TimerRunningLocker)
            {
                FailureTimer.TriggerTimerCompletionImmediately();
            }

            lock (FailedOutTimer.TimerRunningLocker)
            {
                FailedOutTimer.TriggerTimerCompletionImmediately();
            }
        }

        #region IDisposable members
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~SyncEngine()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            Shutdown();
        }

        #region subroutine calls of Sync Run
        // add errors to failure queue (which will exclude changes which were succesfully completed)
        private void RequeueFailures(List<PossiblyStreamableAndPossiblyPreexistingErrorFileChange> errorsToQueue, // errors to queue (before filtering out successes)
            List<long> successfulEventIds, // successes to filter out
            GenericHolder<CLError> appendErrors, // return error to aggregate with any errors that occur
            out IEnumerable<FileChange> filteredErrors) // errors which were actually queued (not successful events)
        {
            List<FileChange> filteredErrorsList;
            // use a list to append for the errors to log as queued failures
            filteredErrors = filteredErrorsList = new List<FileChange>();

            // if there are any errors to queue,
            // then for any that are not also in the list of successful events, add then to the failure queue;
            // also add the FileStreams from the changes in error to the aggregated error output so they can be disposed;
            // add the errors themselves to the output as well
            if (errorsToQueue != null)
            {
                // if there is at least one error to queue, then queue them
                if (errorsToQueue.Count > 0)
                {
                    // try/catch to queue errors, aggregating any exception that occurs to the return error
                    try
                    {
                        // sort ids of completed events to allow for binary search
                        successfulEventIds.Sort();

                        // define list for changes which have failed out to add to the failed out queue, defaulting to null
                        List<FileChange> failedOutChanges = null;

                        // lock on failure queue timer for modifying the failure queue
                        lock (FailureTimer.TimerRunningLocker)
                        {
                            // define bool for whether a change was added to the failure queue, defaulting to false
                            bool atLeastOneErrorAdded = false;
                            // loop through failure events to queue
                            foreach (PossiblyStreamableAndPossiblyPreexistingErrorFileChange errorToQueue in errorsToQueue)
                            {
                                // try/catch for the current failed event to add it to the queue, on exception aggregate the exception and possibly a Stream for the event
                                try
                                {
                                    // if the current event was not found in the list of completed events, then it was a failed event: queue to failure queue
                                    if (successfulEventIds.BinarySearch(errorToQueue.FileChange.EventId) < 0)
                                    {
                                        // the error has been filtered, add the list used for output
                                        filteredErrorsList.Add(errorToQueue.FileChange);

                                        // declare an enumerable to store dependencies which were freed up when cancelling out a not found change
                                        IEnumerable<FileChange> notFoundDependencies;

                                        // if event can continue retrying (meaning it has not hit its max failure limit),
                                        // then queue the failure for reprocessing
                                        if (ContinueToRetry(
                                            new PossiblyPreexistingFileChangeInError(errorToQueue.IsPreexisting, errorToQueue.FileChange), // select error into required format
                                            syncData, // event source used if a change needs to get pushed back to the event source
                                            MaxNumberOfFailureRetries, // max failure repeats before stopping
                                            MaxNumberOfNotFounds, // max not founds before presuming the event was cancelled out
                                            out notFoundDependencies)) // output freed dependencies, if any
                                        {
                                            // enqueue the failure for reprocessing
                                            FailedChangesQueue.Enqueue(errorToQueue.FileChange);

                                            // mark that an error was added to the failure queue
                                            atLeastOneErrorAdded = true;
                                        }
                                        else
                                        {
                                            // define recursing action to reset failure counters for a FileChange (and any inner FileChanges)
                                            Action<FileChange, object> recurseResetCounters = (currentLevelChange, thisAction) =>
                                            {
                                                Action<FileChange, object> castAction;
                                                if ((castAction = thisAction as Action<FileChange, object>) != null)
                                                {
                                                    currentLevelChange.NotFoundForStreamCounter = 0;
                                                    currentLevelChange.FailureCounter = 0;

                                                    FileChangeWithDependencies castChange = currentLevelChange as FileChangeWithDependencies;
                                                    if (castChange != null
                                                        && castChange.DependenciesCount > 0)
                                                    {
                                                        foreach (FileChange recurseChange in castChange.Dependencies)
                                                        {
                                                            if (recurseChange != null)
                                                            {
                                                                castAction(recurseChange, thisAction);
                                                            }
                                                        }
                                                    }
                                                }
                                            };

                                            // if there is a failed out change to add, then reset its failure counters recursively and add the change to the list of those failed out
                                            if (errorToQueue.FileChange != null
                                                && errorToQueue.FileChange.NotFoundForStreamCounter < MaxNumberOfNotFounds)
                                            {
                                                recurseResetCounters(errorToQueue.FileChange, recurseResetCounters);

                                                if (failedOutChanges == null)
                                                {
                                                    failedOutChanges = new List<FileChange>();
                                                }
                                                failedOutChanges.Add(errorToQueue.FileChange);
                                            }
                                        }

                                        // if ContinueToRetry output freed dependencies to process, process them
                                        if (notFoundDependencies != null)
                                        {
                                            // loop through the freed dependencies
                                            foreach (FileChange currentDependency in notFoundDependencies)
                                            {
                                                // enqueue the freed dependency for processing
                                                FailedChangesQueue.Enqueue(currentDependency);

                                                // mark that an error was added to the failure queue
                                                atLeastOneErrorAdded = true;
                                            }
                                        }

                                        // add any error stream to the return error so it will be disposed (does not cause a problem to add null, it will just be ignored)
                                        appendErrors.Value += errorToQueue.Stream;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // add exception followed by Stream to return error (order is important, if the return error was null then the first thing added determines the error description)
                                    appendErrors.Value += ex;
                                    appendErrors.Value += errorToQueue.Stream;
                                }
                            }

                            // if an error was queued, then start the failure queue timer
                            if (atLeastOneErrorAdded)
                            {
                                FailureTimer.StartTimerIfNotRunning();
                            }
                        }

                        if (failedOutChanges != null)
                        {
                            lock (FailedOutTimer.TimerRunningLocker)
                            {
                                foreach (FileChange currentFailedOut in failedOutChanges)
                                {
                                    FailedOutChanges.Add(currentFailedOut);
                                }

                                FailedOutTimer.StartTimerIfNotRunning();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // aggregate the exception to the return error
                        appendErrors.Value += ex;
                    }
                }
            }
        }

        // private method which handles performing the action of a single FileChange, storing any exceptions thrown (which are caught and returned)
        private Exception CompleteFileChange(PossiblyStreamableFileChange toComplete, // FileChange to perform
            ProcessingQueuesTimer failureTimer, // timer of failure queue
            out Nullable<long> immediateSuccessEventId, // output synchronously succesful event id
            out Nullable<AsyncUploadDownloadTask> asyncTask, // output asynchronous task which still needs to run
            string TempDownloadsFolder) // full path location to folder which will contain temp downloads
        {
            // try/catch to perform the whole FileChange, returning any exception to the calling method
            try
            {
                // Except for file uploads/downloads, complete the FileChange synhronously, otherwise queue them appropriately;
                // If it completes synchronously and successfully, set the immediateSuccessEventId to toComplete.EventId, in all other cases set to null;
                // If it is supposed to run asynchrounously, set asyncTask with the task to complete, otherwise set it to null

                // if the FileChange was a change that the server is telling the client to perform (Sync From),
                // then perform the FileChange to the local computer
                if (toComplete.FileChange.Direction == SyncDirection.From)
                {
                    // if the FileChange to complete represents a folder which was modified, then throw an exception for this invalid scenario
                    if (toComplete.FileChange.Metadata.HashableProperties.IsFolder
                        && toComplete.FileChange.Type == FileChangeType.Modified)
                    {
                        throw new ArgumentException("toComplete's FileChange cannot be a folder and have Modified for Type");
                    }

                    // if the FileChange to complete represents a file marked for creation or modification, then process change as a file download
                    if (!toComplete.FileChange.Metadata.HashableProperties.IsFolder
                        && (toComplete.FileChange.Type == FileChangeType.Created
                            || toComplete.FileChange.Type == FileChangeType.Modified))
                    {
                        // define bool to store whether a file already exists on disk with a matching hash and size
                        bool fileMatches = false;

                        // declare byte array for the event hash for comparison
                        byte[] toCompleteBytes;
                        // if unable to retrieve existing MD5 then set MD5 from revision
                        if (toComplete.FileChange.GetMD5Bytes(out toCompleteBytes) != null // retrieves hash and set MD5 from revision if error occurred
                            || toCompleteBytes == null) // or no error occurred retrieving MD5 but there was no MD5 then set MD5 from revision
                        {
                            try
                            {
                                toCompleteBytes = Helpers.ParseHexadecimalStringToByteArray(toComplete.FileChange.Metadata.Revision);
                            }
                            catch
                            {
                                toCompleteBytes = null;
                            }
                        }

                        // if able to retrieve MD5 from either the GetMD5Bytes or by parsing revision, then check if file already matches on disk
                        if (toCompleteBytes != null)
                        {
                            // try/catch to compare event has with file on disk, silence exception
                            try
                            {
                                // grab path of event
                                string toCompletePath = toComplete.FileChange.NewPath.ToString();
                                // turn event path into FileInfo for IO access
                                FileInfo existingInfo = new FileInfo(toCompletePath);
                                // if IO access determines that the file exists and has a matching size to the event, then continue comparing via hash comparison
                                if (existingInfo.Exists
                                    && existingInfo.Length == (toComplete.FileChange.Metadata.HashableProperties.Size ?? -1))
                                {
                                    // open read share stream on file at event path for reading to generate hash
                                    using (FileStream existingStream = new FileStream(toCompletePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        // create MD5 to calculate hash
                                        MD5 md5Hasher = MD5.Create();

                                        // define counter for size to verify with the size retrieved via IO
                                        long sizeCounter = 0;

                                        // define buffer for reading the file
                                        byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                        // declare int for storying how many bytes were read on each buffer transfer
                                        int fileReadBytes;

                                        // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the file and store the read byte count
                                        while ((fileReadBytes = existingStream.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                        {
                                            // append the count of read bytes for final size verification
                                            sizeCounter += fileReadBytes;
                                            // add the buffer block to the hash calculation
                                            md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                        }

                                        // transform one final empty block to complete the hash calculation
                                        md5Hasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);

                                        // set that a file matches by a matching size and MD5 hash
                                        fileMatches = sizeCounter == (toComplete.FileChange.Metadata.HashableProperties.Size ?? -1)
                                            && NativeMethods.memcmp(toCompleteBytes, md5Hasher.Hash, new UIntPtr((uint)toCompleteBytes.Length)) == 0;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        // if a file was marked to exist at the event path with a matching file size and MD5 hash, then mark that it was already successfully downloaded synchronously
                        if (fileMatches)
                        {
                            // immediately successful
                            immediateSuccessEventId = toComplete.FileChange.EventId;
                            // no task to run
                            asyncTask = null;
                        }
                        // else if file was not marked as already downloaded, then set the async task to run for the download
                        else
                        {
                            // not immediately successful
                            immediateSuccessEventId = null;
                            Guid asyncTaskThreadId = Guid.NewGuid();
                            // create task for download
                            asyncTask = new AsyncUploadDownloadTask(SyncDirection.From, // From direction is for downloads
                                new Task<EventIdAndCompletionProcessor>(DownloadForTask, // Callback which processes the actual task code when started
                                    new DownloadTaskState()
                                    {
                                        // Properties function as named:

                                        ThreadId = asyncTaskThreadId,
                                        StatusUpdate = FileTransferStatusUpdate,
                                        FailureTimer = FailureTimer,
                                        FileToDownload = toComplete.FileChange,
                                        MD5 = toCompleteBytes,
                                        SyncData = syncData,
                                        SyncBox = syncBox,
                                        TempDownloadFolderPath = TempDownloadsFolder,
                                        ShutdownToken = FullShutdownToken,
                                        MoveCompletedDownload = MoveCompletedDownload,
                                        HttpTimeoutMilliseconds = HttpTimeoutMilliseconds,
                                        MaxNumberOfFailureRetries = MaxNumberOfFailureRetries,
                                        MaxNumberOfNotFounds = MaxNumberOfNotFounds,
                                        FailedChangesQueue = FailedChangesQueue,
                                        RemoveFileChangeEvents = RemoveFileChangeFromUpDownEvent,
                                        RestClient = httpRestClient,
                                        UploadDownloadServerConnectionFailureCount = UploadDownloadConnectionFailures
                                    }),
                                    asyncTaskThreadId);
                        }
                    }
                    // else if the FileChange does not represent a file that needs to be downloaded, then perform the change using the event source (which could perform the action to disk)
                    else
                    {
                        // perform the change via the event source, storing any errors that occured
                        CLError applyChangeError = syncData.applySyncFromChange(toComplete.FileChange);
                        // if an error occurred performing the event, rethrow the error
                        if (applyChangeError != null)
                        {
                            try
                            {
                                MessageEvents.FireNewEventMessage(
                                    "Error applying change locally: " + applyChangeError.errorDescription,
                                    EventMessageLevel.Regular,
                                    /*Error*/new GeneralErrorInfo(),
                                    syncBox.SyncBoxId,
                                    syncBox.CopiedSettings.DeviceId);
                            }
                            catch
                            {
                            }

                            throw applyChangeError.GrabFirstException();
                        }
                        // event was completed synchronously, output successful event id
                        immediateSuccessEventId = toComplete.FileChange.EventId;
                        // no task needs to be started
                        asyncTask = null;
                    }
                }
                // else if FileChange was not a Sync From change and change represents a folder, throw exception for invalid combination
                else if (toComplete.FileChange.Metadata.HashableProperties.IsFolder)
                {
                    throw new ArgumentException("toComplete's FileChange cannot represent a folder with direction Sync To");
                }
                // else if FileChange was not a Sync From change and change represents a deleted file, throw exception for invalid combination
                else if (toComplete.FileChange.Type == FileChangeType.Deleted)
                {
                    throw new ArgumentException("toComplete's FileChange has no completion action for file deletion");
                }
                // else if FileChange was not a Sync From change and change represents a renamed file, throw exception for invalid combination
                else if (toComplete.FileChange.Type == FileChangeType.Renamed)
                {
                    throw new ArgumentException("toComplete's FileChange has no completion action for file rename/move");
                }
                // else if FileChange was not a Sync From change and change represents either a created file or a modified file, then process change as a file upload
                else if (toComplete.FileChange.Direction == SyncDirection.To)
                {
                    // event needs to be completed asynchronously, no synchronous success to record
                    immediateSuccessEventId = null;
                    Guid asyncTaskThreadId = Guid.NewGuid();
                    // output new task for file upload
                    asyncTask = new AsyncUploadDownloadTask(SyncDirection.To, // To is direction for uploads
                        new Task<EventIdAndCompletionProcessor>(UploadForTask, // Callback containing code to run for task
                            new UploadTaskState()
                            {
                                // Properties are purposed as named

                                ThreadId = asyncTaskThreadId,
                                StatusUpdate = FileTransferStatusUpdate,
                                FailureTimer = FailureTimer,
                                FileToUpload = toComplete.FileChange,
                                SyncData = syncData,
                                SyncBox = syncBox,
                                UploadStream = toComplete.Stream,
                                ShutdownToken = FullShutdownToken,
                                HttpTimeoutMilliseconds = HttpTimeoutMilliseconds,
                                MaxNumberOfFailureRetries = MaxNumberOfFailureRetries,
                                MaxNumberOfNotFounds = MaxNumberOfNotFounds,
                                FailedChangesQueue = FailedChangesQueue,
                                RemoveFileChangeEvents = RemoveFileChangeFromUpDownEvent,
                                RestClient = httpRestClient,
                                UploadDownloadServerConnectionFailureCount = UploadDownloadConnectionFailures
                            }),
                        asyncTaskThreadId);
                }
                //  else if FileChange was not a Sync From change nor a Sync To change, throw exception for unknown direction
                else
                {
                    // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                    throw new NotSupportedException("Unknown SyncDirection: " + toComplete.FileChange.Direction.ToString());
                }
            }
            catch (Exception ex)
            {
                // on error default output parameters and return the error

                immediateSuccessEventId = Helpers.DefaultForType<Nullable<long>>();
                asyncTask = Helpers.DefaultForType<Nullable<AsyncUploadDownloadTask>>();
                return ex;
            }

            // no error occurred, return
            return null;
        }

        // Code to run for an upload Task (object state should be a UploadTaskState)
        private static EventIdAndCompletionProcessor UploadForTask(object uploadState)
        {
            if (Helpers.AllHaltedOnUnrecoverableError)
            {
                throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
            }

            // Define cast state, defaulting to null
            UploadTaskState castState = null;

            // Create an object to store the DateTime when it is first retrieved (and will be reused thus keeping the same time)
            GenericHolder<Nullable<DateTime>> startTimeHolder = new GenericHolder<Nullable<DateTime>>(null);
            // Function to retrieve the DateTime only when it is needed and stored so the same time will be reused
            Func<GenericHolder<Nullable<DateTime>>, DateTime> getStartTime = timeHolder =>
            {
                // Lock on time holder for modification or retrieval of time
                lock (timeHolder)
                {
                    // retrieve or assign and retrieve time
                    return timeHolder.Value
                        ?? (DateTime)(timeHolder.Value = DateTime.Now);
                }
            };

            // try/catch to process upload logic, on exception try to wrap error for cleanup when it reaches garbage collection (wrapped error is rethrown)
            try
            {
                // try/catch to process upload logic, on exception advanced trace as necessary and rethrow
                try
                {
                    // try cast state
                    castState = uploadState as UploadTaskState;

                    // validate required parameters, throwing exception when assertions fail

                    if (castState == null)
                    {
                        throw new NullReferenceException("Upload Task uploadState not castable as UploadTaskState");
                    }

                    if (castState.RemoveFileChangeEvents == null)
                    {
                        throw new NullReferenceException("DownloadTaskState must contain RemoveFileChangeEvents");
                    }

                    if (castState.ShutdownToken == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain ShutdownToken");
                    }

                    if (castState.UploadDownloadServerConnectionFailureCount == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain UploadDownloadServerConnectionFailureCount");
                    }

                    // check for sync shutdown
                    Monitor.Enter(castState.ShutdownToken);
                    try
                    {
                        if (castState.ShutdownToken.Token.IsCancellationRequested)
                        {
                            return new EventIdAndCompletionProcessor(0, castState.SyncData, castState.SyncBox.CopiedSettings, castState.SyncBox.SyncBoxId);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(castState.ShutdownToken);
                    }

                    // continue validating required parameters, throwing exception when assertions fail

                    // attempt to check these two references first so that the failure can be added to the queue if exceptions occur later:
                    // the failure timer and the failed FileChange
                    if (castState.FailureTimer == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain FailureTimer");
                    }

                    if (castState.MaxNumberOfNotFounds == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain MaxNumberOfNotFounds");
                    }

                    if (castState.MaxNumberOfFailureRetries == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain MaxNumberOfFailureRetries");
                    }

                    if (castState.ShutdownToken == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain ShutdownToken");
                    }

                    if (castState.FileToUpload == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain FileToDownload");
                    }

                    if (castState.UploadStream == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain UploadStream");
                    }

                    if (castState.SyncData == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain SyncData");
                    }

                    if (castState.SyncBox == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain SyncBox");
                    }

                    if (castState.FailedChangesQueue == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain FailedChangesQueue");
                    }

                    if (castState.HttpTimeoutMilliseconds == null)
                    {
                        throw new NullReferenceException("UploadTaskState must contain HttpTimeoutMilliseconds");
                    }

                    if (castState.FileToUpload.Metadata.HashableProperties.Size == null)
                    {
                        throw new NullReferenceException("storeFileChange must have a Size");
                    }

                    if (string.IsNullOrWhiteSpace(castState.FileToUpload.Metadata.StorageKey))
                    {
                        throw new NullReferenceException("storeFileChange must have a StorageKey");
                    }

                    if (castState.RestClient == null)
                    {
                        throw new NullReferenceException("DownloadTaskState must contain RestClient");
                    }

                    if (castState.StatusUpdate == null)
                    {
                        throw new NullReferenceException("DownloadTaskState must contain StatusUpdate");
                    }

                    // declare the status for performing a rest communication
                    CLHttpRestStatus uploadStatus;
                    string uploadMessage;
                    // upload the file using the REST client, storing any error that occurs
                    CLError uploadError = castState.RestClient.UploadFile(castState.UploadStream, // stream for upload
                        castState.FileToUpload, // upload change
                        (int)castState.HttpTimeoutMilliseconds, // milliseconds before communication timeout (does not apply to the amount of time it takes to actually upload the file)
                        out uploadStatus, // output the status of communication
                        out uploadMessage,
                        castState.ShutdownToken, // pass in the shutdown token for the optional parameter so it can be cancelled
                        castState.StatusUpdate,
                        castState.ThreadId);

                    // depending on whether the communication status is a connection failure or not, either increment the failure count or clear it, respectively

                    if (uploadStatus == CLHttpRestStatus.ConnectionFailed)
                    {
                        lock (castState.UploadDownloadServerConnectionFailureCount)
                        {
                            if (castState.UploadDownloadServerConnectionFailureCount.Value != ((byte)255))
                            {
                                castState.UploadDownloadServerConnectionFailureCount.Value = (byte)(castState.UploadDownloadServerConnectionFailureCount.Value + 1);
                            }
                        }
                    }
                    else
                    {
                        lock (castState.UploadDownloadServerConnectionFailureCount)
                        {
                            if (castState.UploadDownloadServerConnectionFailureCount.Value != ((byte)0))
                            {
                                castState.UploadDownloadServerConnectionFailureCount.Value = 0;
                            }
                        }
                    }

                    // if an error occurred while uploading the file, rethrow the error
                    if (uploadError != null)
                    {
                        throw new AggregateException("An error occurred uploading a file: " + uploadError.errorDescription, uploadError.GrabExceptions());
                    }

                    // for advanced trace, UploadDownloadSuccess
                    if ((castState.SyncBox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        ComTrace.LogFileChangeFlow(castState.SyncBox.CopiedSettings.TraceLocation, castState.SyncBox.CopiedSettings.DeviceId, castState.SyncBox.SyncBoxId, FileChangeFlowEntryPositionInFlow.UploadDownloadSuccess, new FileChange[] { castState.FileToUpload });
                    }

                    // status message
                    MessageEvents.FireNewEventMessage(
                        "File finished uploading from path " + castState.FileToUpload.NewPath.ToString(),
                        EventMessageLevel.Regular,
                        SyncBoxId: castState.SyncBox.SyncBoxId,
                        DeviceId: castState.SyncBox.CopiedSettings.DeviceId);

                    // return with the info for which event id completed, the event source for marking a complete event, and the settings for tracing and error logging
                    return new EventIdAndCompletionProcessor(castState.FileToUpload.EventId, castState.SyncData, castState.SyncBox.CopiedSettings, castState.SyncBox.SyncBoxId);
                }
                catch
                {
                    // advanced trace, UploadDownloadFailure
                    if ((castState.SyncBox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                    {
                        ComTrace.LogFileChangeFlow(castState.SyncBox.CopiedSettings.TraceLocation, castState.SyncBox.CopiedSettings.DeviceId, castState.SyncBox.SyncBoxId, FileChangeFlowEntryPositionInFlow.UploadDownloadFailure, (castState.FileToUpload == null ? null : new FileChange[] { castState.FileToUpload }));
                    }

                    // rethrow
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (castState != null
                    && castState.FileToUpload != null
                    && castState.FileToUpload.NewPath != null
                    && castState.SyncBox != null
                    && castState.FileToUpload.Metadata != null
                    && castState.StatusUpdate != null
                    && castState.FileToUpload.Metadata.HashableProperties.Size != null)
                {
                    castState.StatusUpdate(
                        castState.ThreadId, // threadId
                        castState.FileToUpload.EventId, // eventId
                        SyncDirection.To, // direction
                        castState.FileToUpload.NewPath.GetRelativePath(castState.SyncBox.CopiedSettings.SyncRoot, false), // relativePath
                        (long)castState.FileToUpload.Metadata.HashableProperties.Size, // byteProgress
                        (long)castState.FileToUpload.Metadata.HashableProperties.Size, // totalByteSize
                        true); // error occurred
                }

                // if the error was any that are not recoverable, display a message to the user for the serious problem and return

                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Unable to cast uploadState as UploadTaskState and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.FileToUpload == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain FileToUpload and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.SyncData == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain SyncData and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.SyncBox == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain SyncBox and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.MaxNumberOfFailureRetries == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain MaxNumberOfFailureRetries and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.MaxNumberOfNotFounds == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain MaxNumberOfNotFounds and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.FailedChangesQueue == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain FailedChangesQueue and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }

                // else if none of the unrecoverable errors occurred, then wrap the error to execute for cleanup on garbage collection and rethrow it
                else
                {
                    // wrap the error to execute for cleanup
                    ExecutableException<PossiblyStreamableFileChangeWithSyncData> wrappedEx = new ExecutableException<PossiblyStreamableFileChangeWithSyncData>(ProcessUploadError, // callback with the code to handle cleanup
                        new PossiblyStreamableFileChangeWithSyncData(castState.FailedChangesQueue, // failure queue for reprocessing failed events
                            (byte)castState.MaxNumberOfFailureRetries, // how many times to retry on failure before stopping
                            (byte)castState.MaxNumberOfNotFounds, // how many not found errors can occur before presuming the event was cancelled out
                            castState.FailureTimer, // timer for failure queue
                            new PossiblyStreamableFileChange(castState.FileToUpload, // event which failed
                                castState.UploadStream, // upload stream for failed event
                                ignoreStreamException: true), // ignore stream exception because we set the reference castState.UploadStream to null when it is normally disposed
                            castState.SyncData, // event source for updating when needed
                            castState.SyncBox), // settings for tracing or logging errors
                        "Error in upload Task, see inner exception", // exception message
                        ex); // original exception

                    // rethrow (will be handled by the task unhandled exception handler in HttpScheduler which runs upon garbage collection of exceptions)
                    throw wrappedEx;
                }
            }
            finally
            {
                // if state was succesfully cast and it contains both the event for the file upload and a callback to remove event handlers on the event,
                // then remove event handlers on the event
                if (castState != null
                    && castState.FileToUpload != null
                    && castState.RemoveFileChangeEvents != null)
                {
                    castState.RemoveFileChangeEvents(castState.FileToUpload);
                }
            }
        }

        // code to handle cleanup when an error occurred during upload
        private static void ProcessUploadError(PossiblyStreamableFileChangeWithSyncData exceptionState, AggregateException exceptions)
        {
            // try/catch cleanup after an upload error, on catch log the error that occurred during cleanup
            try
            {
                // if the cleanup data has a stream, then dispose the stream
                if (exceptionState.FileChange.Stream != null)
                {
                    try
                    {
                        exceptionState.FileChange.Stream.Dispose();
                    }
                    catch
                    {
                    }
                }

                // build the first part of a message which will be sent to an event handler for error messages
                string growlErrorMessage = "An error occurred uploading " +
                    exceptionState.FileChange.FileChange.NewPath.ToString() + ": " + // include file path in message

                    // Because of exception wrapping, the real cause of the error is probably in the message of the exception's inner inner exception,
                    // so attempt to grab it from there otherwise attempt to grab it from the exception's inner exception otherwise attempt to grab it from the exception itself

                    ((exceptions.InnerException == null || exceptions.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.Message)
                        || exceptions.InnerException.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.InnerException.Message))
                        ? ((exceptions.InnerException == null || exceptions.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.Message))
                            ? ((exceptions.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.Message))
                                ? exceptions.Message // failed to find the second inner exception in the exception, so output the deepest found
                                : exceptions.InnerException.Message) // failed to find the second inner exception in the exception, so output the deepest found
                            : exceptions.InnerException.InnerException.Message) // failed to find the third inner exception in the exception, so output the deepest found
                        : exceptions.InnerException.InnerException.InnerException.Message); // success for finding all inner exceptions up to the real source of the error

                // declare a bool for whether error is serious (failed and no longer retrying)
                bool isErrorSerious;

                // declare an enumerable to store dependencies which were freed up when cancelling out a not found change
                IEnumerable<FileChange> notFoundDependencies;

                // if event can continue retrying (meaning it has not hit its max failure limit),
                // then queue the failure for reprocessing
                if (ContinueToRetry(new PossiblyPreexistingFileChangeInError( // select error into required format
                        false, // not a prexisting error (will cause error counter to increment)
                        exceptionState.FileChange.FileChange), // event to possibly retry
                    exceptionState.SyncData, // event source used if a change needs to get pushed back to the event source
                    exceptionState.MaxNumberOfFailureRetries, // max failure repeats before stopping
                    exceptionState.MaxNumberOfNotFounds, // max not founds before presuming the event was cancelled out
                    out notFoundDependencies)) // output freed dependencies, if any
                {
                    // lock on failure queue timer for modification of the failure queue
                    lock (exceptionState.DownloadErrorTimer.TimerRunningLocker)
                    {
                        // enqueue failed upload event to failure queue for reprocessing
                        exceptionState.FailedChangesQueue.Enqueue(exceptionState.FileChange.FileChange);

                        // start failure queue timer
                        exceptionState.DownloadErrorTimer.StartTimerIfNotRunning();
                    }

                    // append string to indicate that the failed change will be retried
                    growlErrorMessage += "; Retrying";

                    // mark error as not serious since it will be retried
                    isErrorSerious = false;
                }
                // else if event cannot continue retrying and the reason is because it was not found, then error is not serious
                else if (exceptionState.FileChange.FileChange.NotFoundForStreamCounter >= exceptionState.MaxNumberOfNotFounds)
                {
                    // mark error as not serious since it was presumed cancelled out (not found)
                    isErrorSerious = false;
                }
                // else if event cannot continue retrying and the reason is some other problem, then error is serious
                else
                {
                    isErrorSerious = true;
                }

                // if ContinueToRetry output freed dependencies to process, process them
                if (notFoundDependencies != null)
                {
                    // define bool for whether a change was added to the failure queue, defaulting to false
                    bool atLeastOneErrorAdded = false;

                    // lock on the failure queue timer for modifications to the failure queue
                    lock (exceptionState.DownloadErrorTimer)
                    {
                        // loop through the freed dependencies
                        foreach (FileChange currentDependency in notFoundDependencies)
                        {
                            // enqueue the freed dependency for processing
                            exceptionState.FailedChangesQueue.Enqueue(currentDependency);

                            // mark that an error was added to the failure queue
                            atLeastOneErrorAdded = true;
                        }

                        // if at least one error was added to the failure queue, then start the failure queue timer
                        if (atLeastOneErrorAdded)
                        {
                            exceptionState.DownloadErrorTimer.StartTimerIfNotRunning();
                        }
                    }
                }

                // fire the event handler for a failure message
                MessageEvents.FireNewEventMessage(
                    growlErrorMessage, // message
                    (isErrorSerious ? EventMessageLevel.Important : EventMessageLevel.Regular), // important of error based on flag for whether it is serious
                    Error: new GeneralErrorInfo(),
                    SyncBoxId: exceptionState.SyncBox.SyncBoxId,
                    DeviceId: exceptionState.SyncBox.CopiedSettings.DeviceId);

            }
            catch (Exception innerEx)
            {
                // log error that occurred in attempting to clean up the error
                ((CLError)innerEx).LogErrors(exceptionState.SyncBox.CopiedSettings.TraceLocation, exceptionState.SyncBox.CopiedSettings.LogErrors);
            }
        }

        // Code to run for a download Task (object state should be a DownloadTaskState)
        private static EventIdAndCompletionProcessor DownloadForTask(object downloadState)
        {
            if (Helpers.AllHaltedOnUnrecoverableError)
            {
                throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
            }

            // Define cast state, defaulting to null
            DownloadTaskState castState = null;
            // Define a unique id which will be used as the temp download file name, defaulting to null
            Nullable<Guid> newTempFile = null;

            // Create an object to store the DateTime when it is first retrieved (and will be reused thus keeping the same time)
            GenericHolder<Nullable<DateTime>> startTimeHolder = new GenericHolder<Nullable<DateTime>>(null);
            // Function to retrieve the DateTime only when it is needed and stored so the same time will be reused
            Func<GenericHolder<Nullable<DateTime>>, DateTime> getStartTime = timeHolder =>
            {
                // Lock on time holder for modification or retrieval of time
                lock (timeHolder)
                {
                    // retrieve or assign and retrieve the time
                    return timeHolder.Value
                        ?? (DateTime)(timeHolder.Value = DateTime.Now);
                }
            };

            // try/catch/finally to process upload logic, on exception try to wrap error for cleanup when it reaches garbage collection (wrapped error is rethrown),
            // finally remove eventhandlers from event
            try
            {
                // try cast state
                castState = downloadState as DownloadTaskState;

                // validate required parameters, throwing an exception when assertions fail

                if (castState == null)
                {
                    throw new NullReferenceException("Download Task downloadState not castable as DownloadTaskState");
                }

                // attempt to check these two references first so that the failure can be added to the queue if exceptions occur later:
                // the failure timer and the failed FileChange
                if (castState.RemoveFileChangeEvents == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain RemoveFileChangeEvents");
                }

                if (castState.FailureTimer == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain FailureTimer");
                }

                if (castState.FileToDownload == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain FileToDownload");
                }

                if (castState.SyncData == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain SyncData");
                }

                if (castState.SyncBox == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain SyncBox");
                }

                if (string.IsNullOrWhiteSpace(castState.TempDownloadFolderPath))
                {
                    throw new NullReferenceException("DownloadTaskState must contain TempDownloadFolderPath");
                }

                if (castState.ShutdownToken == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain ShutdownToken");
                }

                if (castState.MoveCompletedDownload == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain MoveCompletedDownload");
                }

                if (castState.HttpTimeoutMilliseconds == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain HttpTimeoutMilliseconds");
                }

                if (castState.MaxNumberOfNotFounds == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain MaxNumberOfNotFounds");
                }

                if (castState.MaxNumberOfFailureRetries == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain MaxNumberOfFailureRetries");
                }

                if (castState.FailedChangesQueue == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain FailedChangesQueue");
                }

                if (castState.RestClient == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain RestClient");
                }

                if (castState.StatusUpdate == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain StatusUpdate");
                }

                if (castState.UploadDownloadServerConnectionFailureCount == null)
                {
                    throw new NullReferenceException("DownloadTaskState must contain UploadDownloadServerConnectionFailureCount");
                }

                // check for sync shutdown
                Monitor.Enter(castState.ShutdownToken);
                try
                {
                    if (castState.ShutdownToken.Token.IsCancellationRequested)
                    {
                        return new EventIdAndCompletionProcessor(0, castState.SyncData, castState.SyncBox.CopiedSettings, castState.SyncBox.SyncBoxId, castState.TempDownloadFolderPath);
                    }
                }
                finally
                {
                    Monitor.Exit(castState.ShutdownToken);
                }

                // declare a mapping of file sizes to download ids and hashes
                Dictionary<long, List<DownloadIdAndMD5>> currentDownloads;
                // lock on the map of temp download folder to downloaded id maps to retrieve current download id map
                lock (TempDownloads)
                {
                    // try to retrieve the current download id map for the current temp download folder and if unsuccesful, then add a new download id map
                    if (!TempDownloads.TryGetValue(castState.TempDownloadFolderPath, out currentDownloads))
                    {
                        TempDownloads.Add(castState.TempDownloadFolderPath,
                            currentDownloads = new Dictionary<long, List<DownloadIdAndMD5>>());
                    }
                }

                // lock on current download id map for modification
                lock (currentDownloads)
                {
                    // declare list of download ids and hashes for the current file size
                    List<DownloadIdAndMD5> tempDownloadsInSize;
                    // try to retrieve the list of download ids and hashes by current file size and if found,
                    // then check for an already downloaded temp file with the same hash instead of downloading again
                    if (currentDownloads.TryGetValue((long)castState.FileToDownload.Metadata.HashableProperties.Size,
                        out tempDownloadsInSize))
                    {
                        // loop through temp downloads
                        foreach (DownloadIdAndMD5 currentTempDownload in tempDownloadsInSize)
                        {
                            // if the hash for the temp download matches the current event, then check the hash against the file itself to verify match
                            if (NativeMethods.memcmp(castState.MD5, currentTempDownload.MD5, new UIntPtr((uint)castState.MD5.Length)) == 0)
                            {
                                // try/catch to check hash of file, silencing errors
                                try
                                {
                                    string existingDownloadPath = castState.TempDownloadFolderPath + "\\" + currentTempDownload.Id.ToString("N");
                                    if (System.IO.File.Exists(existingDownloadPath))
                                    {
                                        // create MD5 to calculate hash
                                        MD5 md5Hasher = MD5.Create();

                                        // define counter for size to verify with the size retrieved via IO
                                        long sizeCounter = 0;

                                        // define buffer for reading the file
                                        byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                        // declare int for storying how many bytes were read on each buffer transfer
                                        int fileReadBytes;

                                        // open a file read stream for reading the hash at the existing temp file location
                                        using (FileStream verifyTempDownloadStream = new FileStream(existingDownloadPath,
                                            FileMode.Open,
                                            FileAccess.Read,
                                            FileShare.Read))
                                        {
                                            // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the file and store the read byte count
                                            while ((fileReadBytes = verifyTempDownloadStream.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                            {
                                                // append the count of read bytes for final size verification
                                                sizeCounter += fileReadBytes;
                                                // add the buffer block to the hash calculation
                                                md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                            }

                                            // transform one final empty block to complete the hash calculation
                                            md5Hasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);

                                            // if the existing temp file has the same size and an identical hash, then use the existing file for the current download
                                            if (sizeCounter == (long)castState.FileToDownload.Metadata.HashableProperties.Size // matching size
                                                && NativeMethods.memcmp(currentTempDownload.MD5, md5Hasher.Hash, new UIntPtr((uint)md5Hasher.Hash.Length)) == 0) // matching hash
                                            {
                                                // use the existing file instead of downloading
                                                newTempFile = currentTempDownload.Id;

                                                // stop checking existing files upon match
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }

                // define the response body for download, defaulting to null
                string responseBody = null;

                // if a file already exists which matches the current download, then move the existing file instead of starting a new download
                if (newTempFile != null)
                {
                    // calculate and store the path for the existing file
                    string newTempFileString = castState.TempDownloadFolderPath + "\\" + ((Guid)newTempFile).ToString("N");

                    // move the file from the temp download path to the final location
                    castState.MoveCompletedDownload(newTempFileString, // temp download path
                        castState.FileToDownload, // event for the file download
                        ref responseBody, // reference to the response body which will be set as "completed" if successful
                        castState.FailureTimer, // timer for the failure queue
                        (Guid)newTempFile); // the id of the existing temp file

                    // using the existing temp file download succeeded so return success immediately
                    return new EventIdAndCompletionProcessor(castState.FileToDownload.EventId, // id of succesful event
                        castState.SyncData, // event source for notifying completion
                        castState.SyncBox.CopiedSettings, // settings for tracing and error logging
                        castState.SyncBox.SyncBoxId,
                        castState.TempDownloadFolderPath); // path to the folder containing temp downloads
                }

                // declare the enumeration to store the state of the download
                CLHttpRestStatus downloadStatus;
                // perform the download of the file, storing any error that occurs
                CLError downloadError = castState.RestClient.DownloadFile(castState.FileToDownload, // the download change
                    OnAfterDownloadToTempFile, // handler for when downloading completes, needs to move the file to the final location and update the status string message
                    new OnAfterDownloadToTempFileState() // userstate which will be passed along when the callback is fired when downloading completes
                    {
                        FailureTimer = castState.FailureTimer, // pass-through the timer for the failure queue
                        MoveCompletedDownload = castState.MoveCompletedDownload // pass-through the delegate to fire which moves the file from the temporary download location to the final location
                    },
                    castState.HttpTimeoutMilliseconds ?? 0, // milliseconds before communication throws exception from timeout, excludes the time it takes to actually download the file
                    out downloadStatus, // output the status of the communication
                    OnBeforeDownloadToTempFile, // handler for when downloading is going to start, which stores the new download id to a local dictionary
                    new OnBeforeDownloadToTempFileState() // userstate which will be passed along when the callback is fired before downloading starts
                    {
                        currentDownloads = currentDownloads, // pass-through the dictionary of current downloads
                        FileToDownload = castState.FileToDownload, // pass-through the file change itself
                        MD5 = castState.MD5 // pass-through the MD5 hash of the file
                    },
                    castState.ShutdownToken, // the cancellation token which can cause the download to stop in the middle
                    castState.TempDownloadFolderPath, // the full path to the folder which will contain all the temporary-downloaded files
                    castState.StatusUpdate,
                    castState.ThreadId);

                // depending on whether the communication status is a connection failure or not, either increment the failure count or clear it, respectively

                if (downloadStatus == CLHttpRestStatus.ConnectionFailed)
                {
                    lock (castState.UploadDownloadServerConnectionFailureCount)
                    {
                        if (castState.UploadDownloadServerConnectionFailureCount.Value != ((byte)255))
                        {
                            castState.UploadDownloadServerConnectionFailureCount.Value = (byte)(castState.UploadDownloadServerConnectionFailureCount.Value + 1);
                        }
                    }
                }
                else
                {
                    lock (castState.UploadDownloadServerConnectionFailureCount)
                    {
                        if (castState.UploadDownloadServerConnectionFailureCount.Value != ((byte)0))
                        {
                            castState.UploadDownloadServerConnectionFailureCount.Value = 0;
                        }
                    }
                }

                // if there was an error while downloading, rethrow the error
                if (downloadError != null)
                {
                    throw new AggregateException("An error occurred downloading a file", downloadError.GrabExceptions());
                }

                // The download was successful (no exceptions), but it may have been cancelled.
                if (downloadStatus == CLHttpRestStatus.Cancelled
                    && castState.FileToDownload.NewPath != null) // cancelled via setting a null path such as when event was cancelled out on another thread
                {
                    return new EventIdAndCompletionProcessor(0, castState.SyncData, castState.SyncBox.CopiedSettings, castState.SyncBox.SyncBoxId, castState.TempDownloadFolderPath);
                }

                // if the download was not a success throw an error
                if (downloadStatus != CLHttpRestStatus.Success)
                {
                    throw new Exception("The return status from downloading a file was not successful: CLHttpRestStatus." + downloadStatus.ToString());
                }

                if (downloadStatus != CLHttpRestStatus.Cancelled) // possible that it was cancelled if path was set as null when event was cancelled out on another thread
                {
                    // status message
                    MessageEvents.FireNewEventMessage(
                        "File finished downloading to path " + castState.FileToDownload.NewPath.ToString(),
                        EventMessageLevel.Regular,
                        SyncBoxId: castState.SyncBox.SyncBoxId,
                        DeviceId: castState.SyncBox.CopiedSettings.DeviceId);
                }

                // return the success
                return new EventIdAndCompletionProcessor(castState.FileToDownload.EventId, // successful event id
                    castState.SyncData, // event source to handle callback for completing the event
                    castState.SyncBox.CopiedSettings, // settings for tracing and logging errors
                    castState.SyncBox.SyncBoxId,
                    castState.TempDownloadFolderPath); // location of folder for temp downloads
            }
            catch (Exception ex)
            {
                if (castState != null
                    && castState.FileToDownload != null
                    && castState.FileToDownload.NewPath != null
                    && castState.SyncBox != null
                    && castState.FileToDownload.Metadata != null
                    && castState.StatusUpdate != null
                    && castState.FileToDownload.Metadata.HashableProperties.Size != null)
                {
                    castState.StatusUpdate(
                        castState.ThreadId, // threadId
                        castState.FileToDownload.EventId, // eventId
                        SyncDirection.From, // direction
                        castState.FileToDownload.NewPath.GetRelativePath(castState.SyncBox.CopiedSettings.SyncRoot, false), // relativePath
                        (long)castState.FileToDownload.Metadata.HashableProperties.Size, // byteProgress
                        (long)castState.FileToDownload.Metadata.HashableProperties.Size, // totalByteSize
                        true); // error occurred
                }
                // for advanced trace, UploadDownloadFailure
                if ((castState.SyncBox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                {
                    ComTrace.LogFileChangeFlow(castState.SyncBox.CopiedSettings.TraceLocation, castState.SyncBox.CopiedSettings.DeviceId, castState.SyncBox.SyncBoxId,
                        FileChangeFlowEntryPositionInFlow.UploadDownloadFailure, (castState.FileToDownload == null ? null : new FileChange[] { castState.FileToDownload }));
                }

                // if there was a download event, then fire the eventhandler for finishing the status of the transfer
                if (castState.FileToDownload != null)
                {
                    // try/catch to finish the status of the transfer, failing silently
                    try
                    {
                        // fire the event callback for the final transfer status
                        MessageEvents.UpdateFileDownload(
                            eventId: castState.FileToDownload.EventId, // event id to uniquely identify this transfer
                            parameters: new CLStatusFileTransferUpdateParameters(
                                getStartTime(startTimeHolder), // retrieve the download start time

                                // need to send a file size which matches the total downloaded bytes so they are equal to cancel the status
                                (castState.FileToDownload.Metadata == null
                                    ? 0 // if the event has no metadata, then use 0 as size
                                    : castState.FileToDownload.Metadata.HashableProperties.Size ?? 0), // else if the event has metadata, then grab the size or use 0

                                // try to build the same relative path that would be used in the normal status, falling back first to the full path then to an empty string
                                (castState.FileToDownload.NewPath == null
                                    ? string.Empty
                                    : castState.SyncBox.CopiedSettings == null
                                        ? castState.FileToDownload.NewPath.ToString()
                                        : castState.FileToDownload.NewPath.GetRelativePath((castState.SyncBox.CopiedSettings.SyncRoot ?? string.Empty), false) ?? string.Empty),

                                // need to send a total downloaded bytes which matches the file size so they are equal to cancel the status
                                (castState.FileToDownload.Metadata == null
                                    ? 0 // if the event has no metadata, then use 0 as total downloaded bytes
                                    : castState.FileToDownload.Metadata.HashableProperties.Size ?? 0)), // else if the event has metadata, then use the size as total uploaded bytes or use 0
                            SyncBoxId: castState.SyncBox.SyncBoxId,
                            DeviceId: castState.SyncBox.CopiedSettings.DeviceId);
                    }
                    catch
                    {
                    }
                }


                // if the error was any that are not recoverable, display a message to the user for the serious problem and return

                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Unable to cast downloadState as DownloadTaskState and thus unable cleanup after download error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.FileToDownload == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "downloadState must contain FileToDownload and thus unable cleanup after download error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.SyncData == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "downloadState must contain SyncData and thus unable cleanup after download error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.SyncBox == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "downloadState must contain SyncBox and thus unable cleanup after download error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.MaxNumberOfFailureRetries == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "downloadState must contain MaxNumberOfFailureRetries and thus unable cleanup after download error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.MaxNumberOfNotFounds == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "downloadState must contain MaxNumberOfNotFounds and thus unable cleanup after download error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }
                else if (castState.FailedChangesQueue == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "uploadState must contain FailedChangesQueue and thus unable cleanup after upload error: " + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());

                    return new EventIdAndCompletionProcessor(0, null, null, 0);
                }

                // else if none of the unrecoverable errors occurred, then wrap the error to execute for cleanup on garbage collection and rethrow it
                else
                {
                    // wrap the error to execute for cleanup
                    ExecutableException<PossiblyStreamableFileChangeWithSyncData> wrappedEx = new ExecutableException<PossiblyStreamableFileChangeWithSyncData>(ProcessDownloadError, // callback with the code to handle cleanup
                        new PossiblyStreamableFileChangeWithSyncData(castState.FailedChangesQueue, // failure queue for reprocessing failed events
                            (byte)castState.MaxNumberOfFailureRetries, // how many times to retry on failure before stopping
                            (byte)castState.MaxNumberOfNotFounds, // how many not found errors can occur before presuming the event was cancelled out
                            castState.FailureTimer, // timer for failure queue
                            new PossiblyStreamableFileChange(castState.FileToDownload, null), // event which failed
                            castState.SyncData, // event source for updating when needed
                            castState.SyncBox), // settings for tracing or logging errors
                        "Error in download Task, see inner exception", // exception message
                        ex); // original exception

                    // rethrow (will be handled by the task unhandled exception handler in HttpScheduler which runs upon garbage collection of exceptions)
                    throw wrappedEx;
                }
            }
            finally
            {
                // if the state was castable and contained the event and also contained the callback to remove eventhandlers, then remove eventhandlers from the event
                if (castState != null
                    && castState.FileToDownload != null
                    && castState.RemoveFileChangeEvents != null)
                {
                    castState.RemoveFileChangeEvents(castState.FileToDownload);
                }
            }
        }

        /// <summary>
        /// ¡¡ Action required: move the completed download file from the temp directory to the final destination !!
        /// Handler called after a file download completes with the id used as the file name in the originally provided temporary download folder,
        /// passes through UserState, passes the download change itself, gives a constructed full path where the downloaded file can be found in the temp folder,
        /// and references a string which should be set to something useful for communications trace to denote a completed file such as "---Completed file download---" (but only set after the file was succesfully moved)
        /// </summary>
        /// <param name="tempFileFullPath">Full path to where the downloaded file can be found in the temp folder (which needs to be moved)</param>
        /// <param name="downloadChange">The download change itself</param>
        /// <param name="responseBody">Reference to string used to trace communication, should be set to something useful to read in communications trace such as "---Completed file download---" (but only after the file was successfully moved)</param>
        /// <param name="UserState">Object passed through from the download method call specific to after download</param>
        /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
        private static void OnAfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId)
        {
            OnAfterDownloadToTempFileState castState = UserState as OnAfterDownloadToTempFileState;

            if (castState == null)
            {
                throw new NullReferenceException("UserState must be castable as OnAfterDownloadToTempFileState");
            }
            if (castState.FailureTimer == null)
            {
                throw new NullReferenceException("UserState must have FailureTimer");
            }
            if (castState.MoveCompletedDownload == null)
            {
                throw new NullReferenceException("UserState must have MoveCompletedDownload");
            }

            // set the file attributes so when the file move triggers a change in the event source its metadata should match the current event;
            // also, perform each attribute change with up to 4 retries since it seems to throw errors under normal conditions (if it still fails then it rethrows the exception);
            // attributes to set: creation time, last modified time, and last access time

            Helpers.RunActionWithRetries(actionState => System.IO.File.SetCreationTimeUtc(actionState.Key, actionState.Value),
                new KeyValuePair<string, DateTime>(tempFileFullPath, downloadChange.Metadata.HashableProperties.CreationTime),
                true);
            Helpers.RunActionWithRetries(actionState => System.IO.File.SetLastAccessTimeUtc(actionState.Key, actionState.Value),
                new KeyValuePair<string, DateTime>(tempFileFullPath, downloadChange.Metadata.HashableProperties.LastTime),
                true);
            Helpers.RunActionWithRetries(actionState => System.IO.File.SetLastWriteTimeUtc(actionState.Key, actionState.Value),
                new KeyValuePair<string, DateTime>(tempFileFullPath, downloadChange.Metadata.HashableProperties.LastTime),
                true);

            // fire callback to perform the actual move of the temp file to the final destination
            castState.MoveCompletedDownload(tempFileFullPath, // location of temp file
                downloadChange, // download event
                ref responseBody, // reference to response string (sets to "completed" on success)
                castState.FailureTimer, // timer for failure queue
                tempId); // id for the downloaded file
        }

        private sealed class OnAfterDownloadToTempFileState
        {
            public ProcessingQueuesTimer FailureTimer { get; set; }
            public MoveCompletedDownloadDelegate MoveCompletedDownload { get; set; }
        }

        private static void OnBeforeDownloadToTempFile(Guid tempId, object UserState)
        {
            OnBeforeDownloadToTempFileState castState = UserState as OnBeforeDownloadToTempFileState;

            if (castState == null)
            {
                throw new NullReferenceException("UserState must be castable as OnBeforeDownloadToTempFileState");
            }
            if (castState.FileToDownload == null)
            {
                throw new NullReferenceException("UserState FileToDownload must not be null");
            }
            if (castState.FileToDownload.Metadata == null)
            {
                throw new NullReferenceException("UserState FileToDownload Metadata must not be null");
            }

            byte[] findMD5;

            if (castState.MD5 == null)
            {
                if (string.IsNullOrEmpty(castState.FileToDownload.Metadata.Revision))
                {
                    throw new NullReferenceException("UserState FileToDownload Metadata Revision must not be null");
                }
                findMD5 = Helpers.ParseHexadecimalStringToByteArray(castState.FileToDownload.Metadata.Revision);
            }
            else
            {
                findMD5 = castState.MD5;
            }
            if (findMD5 == null || findMD5.Length != 16)
            {
                throw new ArgumentException("UserState MD5 must be a 16-length byte array for the MD5 hash of the file");
            }
            if (castState.currentDownloads == null)
            {
                throw new NullReferenceException("UserState currentDownloads must not be null");
            }

            // lock on current download id map for modification
            lock (castState.currentDownloads)
            {
                // if current download id map contains downloads for the current file size, then add the new download to the existing list
                if (castState.currentDownloads.ContainsKey((long)castState.FileToDownload.Metadata.HashableProperties.Size))
                {
                    castState.currentDownloads[(long)castState.FileToDownload.Metadata.HashableProperties.Size].Add(new DownloadIdAndMD5(tempId, findMD5));
                }
                // else if current download id map does not contain downloads for the current file size,
                // create the new list of downloads with the new download as its initial value
                else
                {
                    castState.currentDownloads.Add((long)castState.FileToDownload.Metadata.HashableProperties.Size,
                        new List<DownloadIdAndMD5>(new DownloadIdAndMD5[]
                                            {
                                                new DownloadIdAndMD5(tempId, findMD5)
                                            }));
                }
            }
        }

        private sealed class OnBeforeDownloadToTempFileState
        {
            public FileChange FileToDownload { get; set; }
            public byte[] MD5 { get; set; }
            public Dictionary<long, List<DownloadIdAndMD5>> currentDownloads { get; set; }
        }

        // code to handle cleanup when an error occurred during upload
        private static void ProcessDownloadError(PossiblyStreamableFileChangeWithSyncData exceptionState, AggregateException exceptions)
        {
            // try/catch cleanup after a download error, on catch log the error that occurred during cleanup
            try
            {
                // if the exception data does not have a FileChange, then cannot process FileChange so throw exception
                if (exceptionState.FileChange.FileChange == null)
                {
                    throw new NullReferenceException("exceptionState's FileChange cannot be null");
                }

                // build the first part of a message which will be sent to an event handler for error messages
                string growlErrorMessage = "An error occurred downloading " +
                    exceptionState.FileChange.FileChange.NewPath.ToString() + ": " +

                    // Because of exception wrapping, the real cause of the error is probably in the message of the exception's inner inner exception,
                    // so attempt to grab it from there otherwise attempt to grab it from the exception's inner exception otherwise attempt to grab it from the exception itself

                    ((exceptions.InnerException == null || exceptions.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.Message)
                        || exceptions.InnerException.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.InnerException.Message))
                        ? ((exceptions.InnerException == null || exceptions.InnerException.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.InnerException.Message))
                            ? ((exceptions.InnerException == null || string.IsNullOrEmpty(exceptions.InnerException.Message))
                                ? exceptions.Message // failed to find the second inner exception in the exception, so output the deepest found
                                : exceptions.InnerException.Message) // failed to find the second inner exception in the exception, so output the deepest found
                            : exceptions.InnerException.InnerException.Message) // failed to find the third inner exception in the exception, so output the deepest found
                        : exceptions.InnerException.InnerException.InnerException.Message); // success for finding all inner exceptions up to the real source of the error

                // declare a bool for whether error is serious (failed and no longer retrying)
                bool isErrorSerious;

                // declare an enumerable to store dependencies which were freed up when cancelling out a not found change
                IEnumerable<FileChange> notFoundDependencies;

                // if event can continue retrying (meaning it has not hit its max failure limit),
                // then queue the failure for reprocessing
                if (ContinueToRetry(new PossiblyPreexistingFileChangeInError( // select error into required format
                        false,  // not a prexisting error (will cause error counter to increment)
                        exceptionState.FileChange.FileChange), // event to possibly retry
                    exceptionState.SyncData, // event source used if a change needs to get pushed back to the event source
                    exceptionState.MaxNumberOfFailureRetries, // max failure repeats before stopping
                    exceptionState.MaxNumberOfNotFounds, // max not founds before presuming the event was cancelled out
                    out notFoundDependencies)) // output freed dependencies, if any
                {
                    // lock on failure queue timer for modification of the failure queue
                    lock (exceptionState.DownloadErrorTimer.TimerRunningLocker)
                    {
                        // enqueue failed upload event to failure queue for reprocessing
                        exceptionState.FailedChangesQueue.Enqueue(exceptionState.FileChange.FileChange);

                        // start failure queue timer
                        exceptionState.DownloadErrorTimer.StartTimerIfNotRunning();
                    }

                    // append string to indicate that the failed change will be retried
                    growlErrorMessage += "; Retrying";

                    // mark error as not serious since it will be retried
                    isErrorSerious = false;
                }
                // else if event cannot continue, then determine seriousness and clear any temp download
                else
                {
                    // if event cannot continue retrying and the reason is because it was not found, then error is not serious
                    if (exceptionState.FileChange.FileChange.NotFoundForStreamCounter >= exceptionState.MaxNumberOfNotFounds)
                    {
                        // mark error as not serious since it was presumed cancelled out (not found)
                        isErrorSerious = false;
                    }
                    // else if event cannot continue retrying and the reason is some other problem, then error is serious
                    else
                    {
                        isErrorSerious = true;
                    }

                    // try/catch to clear out the temp download, failing silently
                    try
                    {
                        // if there is a valid temp download folder path and a valid temp file id, then clear out the temp download file
                        if (!string.IsNullOrWhiteSpace(exceptionState.TempDownloadFolderPath)
                            && exceptionState.TempDownloadFileId != null)
                        {
                            // declare a mapping of file sizes to download ids and hashes
                            Dictionary<long, List<DownloadIdAndMD5>> currentDownloads;
                            // lock on the map of temp download folder to downloaded id maps to retrieve current download id map
                            lock (TempDownloads)
                            {
                                // try to retrieve the current download id map for the current temp download folder and if unsuccesful, then add a new download id map
                                if (!TempDownloads.TryGetValue(exceptionState.TempDownloadFolderPath,
                                    out currentDownloads))
                                {
                                    TempDownloads.Add(exceptionState.TempDownloadFolderPath,
                                        currentDownloads = new Dictionary<long, List<DownloadIdAndMD5>>());
                                }
                            }

                            // lock on current download id map for modification
                            lock (currentDownloads)
                            {
                                // declare the current download ids
                                List<DownloadIdAndMD5> errorTemp;
                                // try to retrieve the current download ids by the current download size and if successful, then try to find the current download id and delete the corresponding file
                                if (currentDownloads.TryGetValue((long)exceptionState.FileChange.FileChange.Metadata.HashableProperties.Size, out errorTemp))
                                {
                                    // loop through the current download ids where the id matches the current download id
                                    foreach (DownloadIdAndMD5 matchedErrorTemp in errorTemp.Where(currentError => currentError.Id == (Guid)exceptionState.TempDownloadFileId))
                                    {
                                        // delete the current temp file
                                        System.IO.File.Delete(exceptionState.TempDownloadFolderPath + "\\" + ((Guid)exceptionState.TempDownloadFileId).ToString("N"));

                                        // remove the current temp file id from the list
                                        errorTemp.Remove(matchedErrorTemp);
                                        // if the list is now cleared out after the removal, then remove the list from the download id map
                                        if (errorTemp.Count == 0)
                                        {
                                            currentDownloads.Remove((long)exceptionState.FileChange.FileChange.Metadata.HashableProperties.Size);
                                        }

                                        // stop looping through download ids since one was already found to match
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

                // if ContinueToRetry output freed dependencies to process, process them
                if (notFoundDependencies != null)
                {
                    // define bool for whether a change was added to the failure queue, defaulting to false
                    bool atLeastOneErrorAdded = false;

                    // lock on the failure queue timer for modifications to the failure queue
                    lock (exceptionState.DownloadErrorTimer)
                    {
                        // loop through the freed dependencies
                        foreach (FileChange currentDependency in notFoundDependencies)
                        {
                            // enqueue the freed dependency for processing
                            exceptionState.FailedChangesQueue.Enqueue(currentDependency);

                            // mark that an error was added to the failure queue
                            atLeastOneErrorAdded = true;
                        }

                        // if at least one error was added to the failure queue, then start the failure queue timer
                        if (atLeastOneErrorAdded)
                        {
                            exceptionState.DownloadErrorTimer.StartTimerIfNotRunning();
                        }
                    }
                }

                // fire the event handler for a failure message
                MessageEvents.FireNewEventMessage(
                    growlErrorMessage, // message
                    (isErrorSerious ? EventMessageLevel.Important : EventMessageLevel.Regular), // important of error based on flag for whether it is serious
                    Error: new GeneralErrorInfo(),
                    SyncBoxId: exceptionState.SyncBox.SyncBoxId,
                    DeviceId: exceptionState.SyncBox.CopiedSettings.DeviceId);
            }
            catch (Exception innerEx)
            {
                // log the error that occurred trying to cleanup after a download error
                ((CLError)innerEx).LogErrors(exceptionState.SyncBox.CopiedSettings.TraceLocation, exceptionState.SyncBox.CopiedSettings.LogErrors);
            }
        }
        /// <summary>
        /// Data object to be sent to the code that runs for the download task
        /// </summary>
        private sealed class DownloadTaskState : ITransferTaskState
        {
            public Guid ThreadId { get; set; }
            public FileTransferStatusUpdateDelegate StatusUpdate { get; set; }
            public FileChange FileToDownload { get; set; }
            public byte[] MD5 { get; set; }
            public ProcessingQueuesTimer FailureTimer { get; set; }
            public ISyncDataObject SyncData { get; set; }
            public string TempDownloadFolderPath { get; set; }
            public CLSyncBox SyncBox { get; set; }
            public CancellationTokenSource ShutdownToken { get; set; }
            public MoveCompletedDownloadDelegate MoveCompletedDownload { get; set; }
            public Nullable<int> HttpTimeoutMilliseconds { get; set; }
            public Nullable<byte> MaxNumberOfFailureRetries { get; set; }
            public Nullable<byte> MaxNumberOfNotFounds { get; set; }
            public Queue<FileChange> FailedChangesQueue { get; set; }
            public Action<FileChange> RemoveFileChangeEvents { get; set; }
            public CLHttpRest RestClient { get; set; }
            public GenericHolder<byte> UploadDownloadServerConnectionFailureCount { get; set; }
        }
        /// <summary>
        /// Data object to be sent to the code that runs for the upload task
        /// </summary>
        private sealed class UploadTaskState : ITransferTaskState
        {
            public Guid ThreadId { get; set; }
            public FileTransferStatusUpdateDelegate StatusUpdate { get; set; }
            public FileChange FileToUpload { get; set; }
            public Stream UploadStream { get; set; }
            public ProcessingQueuesTimer FailureTimer { get; set; }
            public ISyncDataObject SyncData { get; set; }
            public CLSyncBox SyncBox { get; set; }
            public CancellationTokenSource ShutdownToken { get; set; }
            public Nullable<int> HttpTimeoutMilliseconds { get; set; }
            public Nullable<byte> MaxNumberOfFailureRetries { get; set; }
            public Nullable<byte> MaxNumberOfNotFounds { get; set; }
            public Queue<FileChange> FailedChangesQueue { get; set; }
            public Action<FileChange> RemoveFileChangeEvents { get; set; }
            public CLHttpRest RestClient { get; set; }
            public GenericHolder<byte> UploadDownloadServerConnectionFailureCount { get; set; }
        }
        /// <summary>
        /// Async HTTP operation holder used to help make async calls synchronous
        /// </summary>
        private sealed class AsyncRequestHolder
        {
            /// <summary>
            /// cancelation token to check between async calls to cancel out of the operation
            /// </summary>
            public CancellationTokenSource FullShutdownToken
            {
                get
                {
                    return _fullShutdownToken;
                }
            }
            private readonly CancellationTokenSource _fullShutdownToken;

            /// <summary>
            /// Constructor for the async HTTP operation holder
            /// </summary>
            /// <param name="FullShutdownToken">Token to check for cancelation upon async calls</param>
            public AsyncRequestHolder(CancellationTokenSource FullShutdownToken)
            {
                // ensure the cancelation token was passed in
                if (FullShutdownToken == null)
                {
                    throw new NullReferenceException("FullShutdownToken cannot be null");
                }

                // store the cancelation token
                this._fullShutdownToken = FullShutdownToken;
            }

            /// <summary>
            /// Whether the current async HTTP operation holder detected cancellation
            /// </summary>
            public bool IsCanceled
            {
                get
                {
                    return _isCanceled;
                }
            }
            // storage for cancellation
            private bool _isCanceled = false;

            /// <summary>
            /// Marks the current async HTTP operation holder as cancelled
            /// </summary>
            public void Cancel()
            {
                _isCanceled = true;
            }

            /// <summary>
            /// Any error that happened during current async HTTP operation
            /// </summary>
            public Exception Error
            {
                get
                {
                    return _error;
                }
            }
            // storage for any error that occurs
            private Exception _error = null;

            /// <summary>
            /// Marks the current async HTTP operation holder with any error that occurs
            /// </summary>
            /// <param name="toMark"></param>
            public void MarkException(Exception toMark)
            {
                // null coallesce the exception with a new exception that the exception was null
                _error = toMark ?? new NullReferenceException("toMark is null");
                // lock on this current async HTTP operation holder for pulsing waiters
                lock (this)
                {
                    Monitor.Pulse(this);
                }
            }
        }

        // Method to make async HTTP operations synchronous which can be ; requires passing an AsyncRequestHolder as the userstate
        private static void MakeAsyncRequestSynchronous(IAsyncResult makeSynchronous)
        {
            // try cast userstate as AsyncRequestHolder
            AsyncRequestHolder castHolder = makeSynchronous.AsyncState as AsyncRequestHolder;

            // ensure the cast userstate was successful
            if (castHolder == null)
            {
                throw new NullReferenceException("makeSynchronous AsyncState must be castable as AsyncRequestHolder");
            }

            // try/catch check for completion or cancellation to pulse the AsyncRequestHolder, on catch mark the exception in the AsyncRequestHolder (which will also pulse out)
            try
            {
                // if asynchronous task completed, then pulse the AsyncRequestHolder
                if (makeSynchronous.IsCompleted)
                {
                    lock (castHolder)
                    {
                        Monitor.Pulse(castHolder);
                    }
                }
                // else if asychronous task is not completed, then check for cancellation
                else
                {
                    // check for cancellation
                    Monitor.Enter(castHolder.FullShutdownToken);
                    try
                    {
                        // if cancelled, then mark the AsyncRequestHolder as cancelled and pulse out
                        if (castHolder.FullShutdownToken.Token.IsCancellationRequested)
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
                        Monitor.Exit(castHolder.FullShutdownToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // mark AsyncRequestHolder with error (which will also pulse out)
                castHolder.MarkException(ex);
            }
        }

        /// <summary>
        /// Delegate for MoveCompletedDownload which takes a completed download from the temp location and uses the event source to move it to the final location and, when successful,
        /// removes the temp download from their list and adds any events dependent on the completed event to the processing queue in the event source
        /// </summary>
        /// <param name="newTempFileString">Full path location of downloaded file in temp directory</param>
        /// <param name="completedDownload">Event that is being performed (file download)</param>
        /// <param name="responseBody">(reference) Response body string to set to "completed" upon completion (instead of the content of the actual download)</param>
        /// <param name="failureTimer">Timer for the failure queue</param>
        /// <param name="newTempFile">ID of the temp download file</param>
        private delegate void MoveCompletedDownloadDelegate(string newTempFileString,
            FileChange completedDownload,
            ref string responseBody,
            ProcessingQueuesTimer failureTimer,
            Guid newTempFile);
        /// <summary>
        /// Takes a completed download from the temp location and uses the event source to move it to the final location and, when successful,
        /// removes the temp download from their list and adds any events dependent on the completed event to the processing queue in the event source
        /// </summary>
        /// <param name="newTempFileString">Full path location of downloaded file in temp directory</param>
        /// <param name="completedDownload">Event that is being performed (file download)</param>
        /// <param name="responseBody">(reference) Response body string to set to "completed" upon completion (instead of the content of the actual download)</param>
        /// <param name="failureTimer">Timer for the failure queue</param>
        /// <param name="newTempFile">ID of the temp download file</param>
        private void MoveCompletedDownload(string newTempFileString,
            FileChange completedDownload,
            ref string responseBody,
            ProcessingQueuesTimer failureTimer,
            Guid newTempFile)
        {
            // Create a new file move change (from the temp download file path to the final destination) and perform it via the event source, storing any error that occurs
            // And store any errors returned from performing the file move operation via the event source
            CLError applyError = syncData.applySyncFromChange(new FileChange()
                {
                    Direction = SyncDirection.From, // File downloads are always Sync From operations
                    DoNotAddToSQLIndex = true, // If the root download event fails, it will be retried; this 'pseudo' server event should not be recorded
                    Metadata = completedDownload.Metadata, // Use the metadata from the actual download event so that it will match the file properties
                    NewPath = completedDownload.NewPath, // Move to the destination from the actual download event
                    OldPath = newTempFileString, // Move from the location of the file within the temp download directory
                    Type = FileChangeType.Renamed // Operation is a move
                },
            onLockState =>
            {
                if (onLockState.fileDownloadMoveLocker != null)
                {
                    Monitor.Enter(onLockState.fileDownloadMoveLocker);
                }
                try
                {
                    if (onLockState.NewPath == null
                        && !string.IsNullOrEmpty(onLockState.newTempFileString))
                    {
                        File.Delete(onLockState.newTempFileString);
                    }
                }
                catch
                {
                }
            },
            onBeforeUnlockState =>
            {
                if (onBeforeUnlockState.fileDownloadMoveLocker != null)
                {
                    Monitor.Exit(onBeforeUnlockState.fileDownloadMoveLocker);
                }
            },
            new
                {
                    fileDownloadMoveLocker = completedDownload.fileDownloadMoveLocker,
                    NewPath = completedDownload.NewPath,
                    newTempFileString = newTempFileString
                },
            lockerInsideAllPaths: UpDownEventLocker); // Lock for changes to the UpDownEvent (the FilePath of the download could actually change when the parent folder is renamed on a different thread)

            // If an error occurred moving the file from the temp download folder to the final destination, then rethrow the exception
            if (applyError != null)
            {
                throw applyError.GrabFirstException();
            }
            // Else if no errors occurred moving the file from the temp download folder to the final destination, then remove the temp download from its list and possibly queue inner dependencies
            else
            {
                // For advanced trace, UploadDownloadSuccess
                if ((syncBox.CopiedSettings.TraceType & TraceType.FileChangeFlow) == TraceType.FileChangeFlow)
                {
                    ComTrace.LogFileChangeFlow(syncBox.CopiedSettings.TraceLocation, syncBox.CopiedSettings.DeviceId, syncBox.SyncBoxId, FileChangeFlowEntryPositionInFlow.UploadDownloadSuccess, new FileChange[] { completedDownload });
                }

                // Pull the location of the temp download folder by finding the directory path portion before the name of the downloaded file
                string tempDownloadFolder = newTempFileString.Substring(0, newTempFileString.LastIndexOf('\\'));

                // declare a mapping of file sizes to download ids and hashes
                Dictionary<long, List<DownloadIdAndMD5>> currentDownloads;
                // lock on the map of temp download folder to downloaded id maps to retrieve current download id map
                lock (TempDownloads)
                {
                    // try to retrieve the current download id map for the current temp download folder and if unsuccesful, then add a new download id map
                    if (!TempDownloads.TryGetValue(tempDownloadFolder, out currentDownloads))
                    {
                        TempDownloads.Add(tempDownloadFolder,
                            currentDownloads = new Dictionary<long, List<DownloadIdAndMD5>>());
                    }
                }

                // lock on current download id map for modification
                lock (currentDownloads)
                {
                    // declare the map of file size to download ids
                    List<DownloadIdAndMD5> foundSize;
                    // try to get the download ids for the current download size and if successful, then try to find the current download id to remove
                    if (currentDownloads.TryGetValue((long)completedDownload.Metadata.HashableProperties.Size, out foundSize))
                    {
                        // loop through the download ids
                        foreach (DownloadIdAndMD5 tempDownloadsInSize in foundSize.ToArray())
                        {
                            // if the current download id matches, then remove the download id from its list and stop searching
                            if (tempDownloadsInSize.Id == newTempFile)
                            {
                                foundSize.Remove(tempDownloadsInSize);
                                break;
                            }
                        }

                        // if the download ids have been cleared out for this file size, then remove the current download ids
                        if (foundSize.Count == 0)
                        {
                            currentDownloads.Remove((long)completedDownload.Metadata.HashableProperties.Size);
                        }
                    }
                }

                // record the download completion response
                responseBody = "---Completed file download---";

                // try cast download event as one with dependencies
                FileChangeWithDependencies toCompleteWithDependencies = completedDownload as FileChangeWithDependencies;
                // if the download event was succesfully cast as one with dependencies and has at least one dependency, then add dependencies to processing queue in event source
                if (toCompleteWithDependencies != null
                    && toCompleteWithDependencies.DependenciesCount > 0)
                {
                    // try/catch to add the dependencies to the processing queue in the event source, on catch add the dependencies to the failure queue instead
                    try
                    {
                        // create a folder for a list of events in error that could not be added to the processing queue in the event source
                        GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>();
                        // add dependencies to processing queue in the event source, storing any error that occurs
                        CLError err = syncData.addChangesToProcessingQueue(toCompleteWithDependencies.Dependencies, // dependencies to add
                            /* add to top */ true, // add dependencies to top of queue
                            errList); // holder for list of dependencies which could not be added to the processing queue

                        // if there is a list of dependencies which failed to add to the dependency queue, then add them to the failure queue for reprocessing
                        if (errList.Value != null)
                        {
                            // define a bool for whether at least one failed event was added to the failure queue
                            bool atLeastOneFailureAdded = false;

                            // lock on the failure queue timer for modifying the failure queue
                            lock (failureTimer)
                            {
                                // loop through the dependencies which failed to add to the processing queue
                                foreach (FileChange currentError in errList.Value)
                                {
                                    // add the failed event to the failure queue
                                    FailedChangesQueue.Enqueue(currentError);

                                    // mark that an event was added to the failure queue
                                    atLeastOneFailureAdded = true;
                                }

                                // if at least one event was added to the failure queue, then start the failure queue timer
                                if (atLeastOneFailureAdded)
                                {
                                    failureTimer.StartTimerIfNotRunning();
                                }
                            }
                        }

                        // if there was an error adding the dependencies to the processing queue, then log the error
                        if (err != null)
                        {
                            err.LogErrors(syncBox.CopiedSettings.TraceLocation, syncBox.CopiedSettings.LogErrors);
                        }
                    }
                    catch (Exception ex)
                    {
                        // define a bool for whether at least one failed event was added to the failure queue
                        bool atLeastOneFailureAdded = false;

                        // lock on the failure queue timer for modifying the failure queue
                        lock (failureTimer)
                        {
                            // loop through the dependencies which failed to add to the processing queue
                            foreach (FileChange currentError in toCompleteWithDependencies.Dependencies)
                            {
                                // add the failed event to the failure queue
                                FailedChangesQueue.Enqueue(currentError);

                                // mark that an event was added to the failure queue
                                atLeastOneFailureAdded = true;
                            }

                            // if at least one event was added to the failure queue, then start the failure queue timer
                            if (atLeastOneFailureAdded)
                            {
                                failureTimer.StartTimerIfNotRunning();
                            }
                        }

                        // log the error
                        ((CLError)new Exception("Error adding dependencies of a completed file download to the processing queue", ex))
                            .LogErrors(syncBox.CopiedSettings.TraceLocation, syncBox.CopiedSettings.LogErrors);
                    }
                }
            }
        }

        /// <summary>
        /// Perform the Sync From or Sync To communication with the server; returns any exceptions so they do not bubble up to the calling method
        /// </summary>
        /// <param name="toCommunicate">Enumerable of FileChanges to communicate (if any exist, then this is a Sync To)</param>
        /// <param name="respondingToPushNotification">Whether the current SyncEngine Run was triggered by a push notification or manual polling (for Sync From)</param>
        /// <param name="completedChanges">(output) Enumerable of changes completed synchronously during communication</param>
        /// <param name="incompleteChanges">(output) Enumerable of changes which still need to be performed</param>
        /// <param name="changesInError">(output) Enumerable of changes which had errors during communication (i.e. "conflict" status)</param>
        /// <param name="newSyncId">(output) New sync id returned from the server or null if no communication was performed (no events to communicate and not responding to push notification)</param>
        /// <returns>Returns any exception that occurred during communication</returns>
        private Exception CommunicateWithServer(IEnumerable<PossiblyStreamableFileChange> toCommunicate,
            bool respondingToPushNotification,
            out IEnumerable<PossiblyChangedFileChange> completedChanges,
            out IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange> incompleteChanges,
            out IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError> changesInError,
            out string newSyncId,
            out CredentialErrorType credentialsError,
            out string syncRootUid)
        {
            credentialsError = CredentialErrorType.NoError;
            syncRootUid = null;

            // try/catch to perform all communication with the server (or no communication if not needed), on catch return the exception
            try
            {
                // check for sync shutdown
                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                        incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                        changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                        newSyncId = Helpers.DefaultForType<string>();
                        return new Exception("Shut down in the middle of communication");
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }

                int resultOrder = 0;

                // define an array out of the changes to communicate
                PossiblyStreamableFileChange[] communicationArray = (toCommunicate ?? Enumerable.Empty<PossiblyStreamableFileChange>()).ToArray();

                // define a dictionary which will store FileChanges from the failure queue to allow lookup of metadata when needed (renames from server do not include metadata),
                // default to null (will be initialized and filled in by a function when and if it is needed and retrieved in the same state until the end of the method)
                FilePathDictionary<FileChange> failuresDict = null;
                // define a function to initialize and fill in the failuresDict for lookup of metadata when needed (runs only when needed to prevent unnecessary logic under the failure queue locker)
                Func<FilePathDictionary<FileChange>> getFailuresDict = () =>
                {
                    // if the failuresDict has not already been initialized, then initialize it and fill it out
                    if (failuresDict == null)
                    {
                        // initialize the failuresDict and store any error that occurs in the process
                        CLError createFailuresDictError = FilePathDictionary<FileChange>.CreateAndInitialize((syncBox.CopiedSettings.SyncRoot ?? string.Empty),
                            out failuresDict);
                        // if an error occurred initializing the failuresDict, then rethrow the error
                        if (createFailuresDictError != null)
                        {
                            throw new AggregateException("Error creating failuresDict", createFailuresDictError.GrabExceptions());
                        }
                        // lock on failure queue timer for modifying the failure queue
                        lock (FailureTimer.TimerRunningLocker)
                        {
                            // loop through the events in the failure queue
                            foreach (FileChange currentInError in FailedChangesQueue)
                            {
                                // add the current failed event to the failuresDict dictionary via a recursive function which also adds any inner dependencies
                                AddChangeToDictionary(failuresDict, currentInError);
                            }
                        }
                    }
                    // return the previously initialized or newly initialized failure dictionary
                    return failuresDict;
                };

                // define a dictionary which will store FileChanges which are asynchronously processing for uploads or downloads to allow lookup of metadata when needed (renames from the server do not include metadata),
                // default to null (will be initialized and filled in by a function when and if it is needed and retrieved in the same state until the end of the method)
                FilePathDictionary<FileChange> runningUpDownChangesDict = null;
                // define a function to initialize and fill in the failuresDict for lookup of metadata when needed (runs only when needed to prevent unnecessary logic under the UpDownEvent locker)
                Func<FilePathDictionary<FileChange>> getRunningUpDownChangesDict = () =>
                {
                    // if the runningUpDownChangesDict has not already been initialized, then initialize it and fill it out
                    if (runningUpDownChangesDict == null)
                    {
                        // create a list to store the changes which are currently uploading or downloading
                        List<FileChange> runningUpDownChanges = new List<FileChange>();
                        // retrieve the changes which are currently uploading or downloading via UpDownEvent (all under the UpDownEvent locker)
                        RunUpDownEvent(
                            new FileChange.UpDownEventArgs((currentUpDown, innerState) =>
                                {
                                    List<FileChange> castState = innerState as List<FileChange>;
                                    if (castState == null)
                                    {
                                        MessageEvents.FireNewEventMessage(
                                            "Unable to cast innerState as List<FileChange>",
                                            EventMessageLevel.Important,
                                            new HaltAllOfCloudSDKErrorInfo());
                                    }
                                    else
                                    {
                                        castState.Add(currentUpDown);
                                    }
                                }),
                            runningUpDownChanges);

                        // initialize the runningUpDownChangesDict and store any error that occurs in the process
                        CLError createUpDownDictError = FilePathDictionary<FileChange>.CreateAndInitialize((syncBox.CopiedSettings.SyncRoot ?? string.Empty),
                            out runningUpDownChangesDict);
                        // if an error occurred initializing the runningUpDownChangesDict, then rethrow the error
                        if (createUpDownDictError != null)
                        {
                            throw new AggregateException("Error creating upDownDict", createUpDownDictError.GrabExceptions());
                        }
                        // loop through the events which are uploading or downloading
                        foreach (FileChange currentUpDownChange in runningUpDownChanges)
                        {
                            // add the uploading or downloading event to the runningUpDownChangesDict dictionary via a recursive function which also adds any inner dependencies
                            AddChangeToDictionary(runningUpDownChangesDict, currentUpDownChange);
                        }
                    }
                    // return the previously initialized or newly initialized uploading/downloading changes dictionary
                    return runningUpDownChangesDict;
                };

                // declare a string to store the previously recorded sync ID
                string syncString;
                // set the previously recoded sync ID, defaulting to "0" and if it is "0", then purge pending changes on the server
                if ((syncString = (syncData.getLastSyncId ?? CLDefinitions.CLDefaultSyncID)) == CLDefinitions.CLDefaultSyncID)
                {
                    #region purge pending on SID "0"
                    // declare the json contract object for the response content
                    PendingResponse purgeResponse;
                    // declare the success/failure status for the communication
                    CLHttpRestStatus purgeStatus;
                    // purge pending communication with the purge request content, storing any error that occurs
                    CLError purgePendingError = httpRestClient.PurgePending(
                        HttpTimeoutMilliseconds, // milliseconds before communication timeout
                        out purgeStatus, // output the success/failure status
                        out purgeResponse); // output the response content (this response content does not get used anywhere later)

                    // depending on whether the communication status is a connection failure or not, either increment the failure count or clear it, respectively

                    if (purgeStatus == CLHttpRestStatus.ConnectionFailed)
                    {
                        lock (MetadataConnectionFailures)
                        {
                            if (MetadataConnectionFailures.Value != ((byte)255))
                            {
                                MetadataConnectionFailures.Value = (byte)(MetadataConnectionFailures.Value + 1);
                            }
                        }
                    }
                    else if (purgeStatus == CLHttpRestStatus.NotAuthorized)
                    {
                        credentialsError = CredentialErrorType.OtherError;
                    }
                    else if (purgeStatus == CLHttpRestStatus.NotAuthorizedExpiredCredentials)
                    {
                        credentialsError = CredentialErrorType.ExpiredCredentials;
                    }
                    else
                    {
                        lock (MetadataConnectionFailures)
                        {
                            if (MetadataConnectionFailures.Value != ((byte)0))
                            {
                                MetadataConnectionFailures.Value = 0;
                            }
                        }
                    }

                    // check if an error occurred purging pending and if so, rethrow the error
                    if (purgePendingError != null)
                    {
                        // if this would have been a SyncFrom, a null FileChange needs to be queued into errors to trigger a retry, otherwise it would never come back in without push notification/manual poll to retry SyncFrom
                        CLError err;
                        if (communicationArray.Length > 0)
                        {
                            err = null;
                        }
                        else
                        {
                            // adding a null FileChange to queue will trigger a SyncFrom without an extra change (gets filtered out on "ProcessingChanges.DequeueAll()" in FileMonitor)
                            GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>(); // no need to check the failed to add list since we won't add a null FileChange to the list of failed changes
                            err = syncData.addChangesToProcessingQueue(new FileChange[] { null }, true, errList);
                        }

                        throw new AggregateException("Error purging existing pending items on first sync" +
                            (err == null
                                ? string.Empty
                                : " and an error occurred queueing for a new SyncFrom"),
                            (err == null
                                ? purgePendingError.GrabExceptions()
                                : purgePendingError.GrabExceptions().Concat(
                                    err.GrabExceptions())));
                    }
                    #endregion
                }

                // check for sync shutdown
                Monitor.Enter(FullShutdownToken);
                try
                {
                    if (FullShutdownToken.Token.IsCancellationRequested)
                    {
                        completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                        incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                        changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                        newSyncId = Helpers.DefaultForType<string>();
                        return new Exception("Shut down in the middle of communication");
                    }
                }
                finally
                {
                    Monitor.Exit(FullShutdownToken);
                }
                
                // in order to not use path from server communications,
                // these helpers have to be used to find paths by server uid

                Dictionary<string, FilePath> serverUidsToPath = new Dictionary<string, FilePath>();
                FilePathDictionary<string> pathsToServerUid;
                CLError createPathsToServerUid = FilePathDictionary<string>.CreateAndInitialize((syncBox.CopiedSettings.SyncRoot ?? string.Empty),
                    out pathsToServerUid,
                    recursiveDeleteCallback: delegate(FilePath recursivePathBeingDeleted, string serverUidRenamed, FilePath originalDeletedPath)
                    {
                        serverUidsToPath[serverUidRenamed] = originalDeletedPath;
                    },
                    recursiveRenameCallback: delegate(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath, string serverUidRenamed, FilePath originalOldPath, FilePath originalNewPath)
                    {
                        serverUidsToPath.Remove(serverUidRenamed);
                    });
                    
                var findPathsByUids = DelegateAndDataHolder.Create(
                    new
                    {
                        commonThisEngine = this,
                        currentEvent = new GenericHolder<JsonContracts.Event>(null),
                        serverUidsToPath = serverUidsToPath,
                        pathsToServerUid = pathsToServerUid,
                        matchedChange = new GenericHolder<Nullable<PossiblyStreamableFileChange>>(null)
                    },
                    (Data, errorToAccumulate) =>
                    {
                        bool currentEventIsRename = CLDefinitions.SyncHeaderRenames.Contains(Data.currentEvent.Value.Header.Action ?? Data.currentEvent.Value.Action);
                        
                        // currentEvent ServerUid is null when "header" "status" is "not_found", therefore try to null-coallesce with a previous, matched change
                        string currentEventServerUid = 
                            Data.currentEvent.Value.Metadata.ServerUid
                                ?? (Data.matchedChange.Value == null
                                    ? null
                                    : ((PossiblyStreamableFileChange)Data.matchedChange.Value).FileChange.Metadata.ServerUid);
                        string currentEventParentServerUid =
                            (Data.currentEvent.Value.Metadata.ToParentUid ?? Data.currentEvent.Value.Metadata.ParentUid) // ToParentUid is correct when it is set for renames, otherwise grab from regular ParentUid
                                ?? (Data.matchedChange.Value == null
                                    ? null
                                    : ((PossiblyStreamableFileChange)Data.matchedChange.Value).FileChange.Metadata.ParentFolderServerUid);
                                        
                        FilePath localDictionaryPath;
                        if (!Data.serverUidsToPath.TryGetValue(currentEventServerUid, out localDictionaryPath))
                        {
                            string localDictionaryPathString;
                            CLError queryDatabaseForPath = Data.commonThisEngine.syncData.GetCalculatedFullPathByServerUid(
                                currentEventServerUid,
                                out localDictionaryPathString,
                                (currentEventIsRename ? Data.currentEvent.Value.Header.EventId : null));
                            if (queryDatabaseForPath != null)
                            {
                                throw new AggregateException("Error grabbing path by server uid", queryDatabaseForPath.GrabExceptions());
                            }

                            localDictionaryPath = localDictionaryPathString;
                        }

                        bool needsParentFolderSearch = (localDictionaryPath == null || currentEventIsRename);

                        if (currentEventIsRename && localDictionaryPath == null)
                        {
                            throw new NullReferenceException("Unable to find previous path before rename by server uid");
                        }
                                        
                        FilePath localDictionaryPreviousPath;
                        if (needsParentFolderSearch)
                        {
                            if (currentEventIsRename)
                            {
                                localDictionaryPreviousPath = localDictionaryPath;
                                localDictionaryPath = null;
                            }
                            else
                            {
                                localDictionaryPreviousPath = null;
                            }

                            FilePath localDictionaryParentPath;
                            if (!Data.serverUidsToPath.TryGetValue(currentEventParentServerUid, out localDictionaryParentPath))
                            {
                                string localDictionaryParentPathString;
                                CLError queryDatabaseForParentPath = Data.commonThisEngine.syncData.GetCalculatedFullPathByServerUid(
                                    currentEventParentServerUid,
                                    out localDictionaryParentPathString,
                                    (currentEventIsRename ? Data.currentEvent.Value.Header.EventId : null));
                                if (queryDatabaseForParentPath != null)
                                {
                                    throw new AggregateException("Error grabbing parent path for parent folder server uid", queryDatabaseForParentPath.GrabExceptions());
                                }

                                if (localDictionaryParentPathString == null)
                                {
                                    throw new NullReferenceException(currentEventIsRename
                                        ? "Unable to find parent folder path for parent folder by parent folder server uid"
                                        : "Unable to find path by server uid or parent folder path for parent folder by parent folder server uid");
                                }

                                localDictionaryParentPath = localDictionaryParentPathString;
                            }

                            if (Data.currentEvent.Value.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                            {
                                Data.serverUidsToPath[currentEventParentServerUid] = localDictionaryParentPath;
                                Data.pathsToServerUid[localDictionaryParentPath.Copy()] = currentEventParentServerUid;
                            }

                            localDictionaryPath = new FilePath(Data.currentEvent.Value.Metadata.ToName ?? Data.currentEvent.Value.Metadata.Name, localDictionaryParentPath.Copy());
                        }
                        else
                        {
                            localDictionaryPreviousPath = null;
                        }

                        return new
                        {
                            findNewPath = localDictionaryPath.ToString(),
                            findOldPath = (localDictionaryPreviousPath == null ? null : localDictionaryPreviousPath.ToString())
                        };
                    },
                    null);

                // create a dictionary mapping event id to changes in error
                Dictionary<long, PossiblyStreamableAndPossiblyChangedFileChangeWithError[]> changesInErrorList = new Dictionary<long, PossiblyStreamableAndPossiblyChangedFileChangeWithError[]>();
                HashSet<int> communicationArrayPrecheckErrorIndexes = new HashSet<int>(
                    communicationArray.Select((checkToCommunicate, checkIndex) =>
                    {
                        Exception storeCheckException = null;

                        if ((checkToCommunicate.FileChange.Type == FileChangeType.Created
                                || checkToCommunicate.FileChange.Type == FileChangeType.Renamed)
                            && (checkToCommunicate.FileChange.Metadata == null || checkToCommunicate.FileChange.Metadata.ParentFolderServerUid == null))
                        {
                            try
                            {
                                const string parentServerUidError = "FileChange Metadata ParentFolderServerUid cannot be null for creations or renames";

                                MessageEvents.FireNewEventMessage(
                                    parentServerUidError,
                                    EventMessageLevel.Important,
                                    new GeneralErrorInfo(),
                                    syncBox.SyncBoxId,
                                    syncBox.CopiedSettings.DeviceId);

                                throw new NullReferenceException(parentServerUidError);
                            }
                            catch (Exception ex)
                            {
                                storeCheckException = ex;
                            }
                        }

                        if (storeCheckException == null
                            && checkToCommunicate.FileChange.Type != FileChangeType.Created
                            && (checkToCommunicate.FileChange.Metadata == null || checkToCommunicate.FileChange.Metadata.ServerUid == null))
                        {
                            try
                            {
                                const string serverUidError = "FileChange Metadata ServerUid cannot be null for modifications, deletions, or renames";

                                MessageEvents.FireNewEventMessage(
                                    serverUidError,
                                    EventMessageLevel.Important,
                                    new GeneralErrorInfo(),
                                    syncBox.SyncBoxId,
                                    syncBox.CopiedSettings.DeviceId);

                                throw new NullReferenceException(serverUidError);
                            }
                            catch (Exception ex)
                            {
                                storeCheckException = ex;
                            }
                        }

                        if (storeCheckException != null)
                        {
                            PossiblyStreamableAndPossiblyChangedFileChangeWithError uidNotFoundChange = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(
                                resultOrder++,
                                false,
                                checkToCommunicate.FileChange,
                                checkToCommunicate.Stream,
                                storeCheckException);

                            // if a change in error already exists for the current event id, then expand the array of errors at this event id with the created FileChange
                            if (changesInErrorList.ContainsKey(checkToCommunicate.FileChange.EventId))
                            {
                                // store the previous array of errors
                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] previousErrors = changesInErrorList[checkToCommunicate.FileChange.EventId];
                                // create a new array for error with a size expanded by one
                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] newErrors = new PossiblyStreamableAndPossiblyChangedFileChangeWithError[previousErrors.Length + 1];
                                // copy all the previous errors to the new array
                                previousErrors.CopyTo(newErrors, 0);
                                // put the new error as the last index of the new array
                                newErrors[previousErrors.Length] = uidNotFoundChange;
                                // replace the value in the error mapping dictionary for the current event id with the expanded array
                                changesInErrorList[checkToCommunicate.FileChange.EventId] = newErrors;
                            }
                            // else if a change in error does not already exist for the current event id, then add a new array with just the current created FileChange
                            else
                            {
                                // add a new array with just the created FileChange to the error mapping dictionary for the current event id
                                changesInErrorList.Add(checkToCommunicate.FileChange.EventId,
                                    new PossiblyStreamableAndPossiblyChangedFileChangeWithError[]
                                    {
                                        uidNotFoundChange
                                    });
                            }

                            return checkIndex;
                        }

                        return -1;
                    }));
                communicationArrayPrecheckErrorIndexes.Remove(-1);

                // if there is at least one change to communicate or we have a push notification to communicate anyways,
                // then process communication

                // if there is at least one change to communicate, this is a Sync To
                if ((communicationArray.Length - communicationArrayPrecheckErrorIndexes.Count) > 0)
                {
                    #region Sync To
                    // Run Sync To with the list toCommunicate;
                    // Anything immediately completed should be output as completedChanges;
                    // Anything in conflict should be output as changesInError;
                    // Anything else should be output as incompleteChanges (changes which still need to be performed such as Sync From events or file uploads);
                    // Mark any new FileChange or any FileChange with altered metadata with a true for return boolean
                    //     (notifies calling method to run MergeToSQL with the updates)

                    // status message
                    MessageEvents.FireNewEventMessage(
                        "Communicating " +
                            (communicationArray.Length - communicationArrayPrecheckErrorIndexes.Count).ToString() +
                            " change" + ((communicationArray.Length - communicationArrayPrecheckErrorIndexes.Count) == 1 ? string.Empty : "s") + " to server and checking for any new changes to sync from server",
                        EventMessageLevel.Regular,
                        SyncBoxId: syncBox.SyncBoxId,
                        DeviceId: syncBox.CopiedSettings.DeviceId);

                    // build a function which will throw a formatted exception when the FileChange's FileChangeType and the type of file system object (file or folder) do not match
                    Func<bool, FileChangeType, FilePath, string> getArgumentException = (isFolder, changeType, targetPath) =>
                    {
                        // throw the exception with the formatted message by file system object type (file or folder) and change type
                        throw new ArgumentException("Invalid combination: " +
                            (isFolder ? "Folder" : (targetPath == null ? "File" : "Shortcut")) +
                            " " + changeType.ToString());
                    };

                    // build a function which will throw an error if an event id is not valid
                    Func<long, long> ensureNonZeroEventId = toCheck =>
                    {
                        // if event id is invalid, then throw an error
                        if (toCheck <= 0)
                        {
                            throw new ArgumentException("Cannot communicate FileChange without EventId");
                        }
                        // return the input (valid event id)
                        return toCheck;
                    };

                    // define the json response object for the response from a Sync To operation
                    To deserializedResponse = null;

                    // calculate how many batches of <= 1000 events are required to send all events
                    int totalBatches = (int)Math.Ceiling(((double)communicationArray.Length) / ((double)CLDefinitions.SyncConstantsMaximumSyncToEvents));
                    // loop once for each batch to communicate
                    for (int batchNumber = 0; batchNumber < totalBatches; batchNumber++)
                    {
                        // check for sync shutdown
                        Monitor.Enter(FullShutdownToken);
                        try
                        {
                            if (FullShutdownToken.Token.IsCancellationRequested)
                            {
                                completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                                incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                                changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                                newSyncId = Helpers.DefaultForType<string>();
                                return new Exception("Shut down in the middle of communication");
                            }
                        }
                        finally
                        {
                            Monitor.Exit(FullShutdownToken);
                        }

                        // define an empty array for the changes to communicate in the current batch;
                        // if the batch is the final batch (or the only batch), then the array size is the size of the final batch
                        // else if the batch is not the final batch, then the array size is the max size for a batch
                        PossiblyStreamableFileChange[] currentBatch = new PossiblyStreamableFileChange[batchNumber == totalBatches - 1
                            ? communicationArray.Length % CLDefinitions.SyncConstantsMaximumSyncToEvents
                            : CLDefinitions.SyncConstantsMaximumSyncToEvents];

                        // copy the current batch of events to the array
                        Array.Copy(sourceArray: communicationArray, // all the events
                            sourceIndex: batchNumber * CLDefinitions.SyncConstantsMaximumSyncToEvents, // starting index based on how many batches have already been processed
                            destinationArray: currentBatch, // array to store the current batch
                            destinationIndex: 0, // write to the start of the destination array
                            length: currentBatch.Length); // write a number of events to fill the destination array

                        // store the highest event id of the events in the current batch
                        long lastEventId = currentBatch.OrderByDescending(currentEvent => ensureNonZeroEventId(currentEvent.FileChange.EventId)).First().FileChange.EventId;

                        // create the json Sync To object for the request body
                        To syncTo = new To()
                        {
                            SyncId = syncString, // previous sync id, server should send all newer events
                            Events = currentBatch
                                .Where((currentEvent, currentEventIndex) => !communicationArrayPrecheckErrorIndexes.Contains(batchNumber * CLDefinitions.SyncConstantsMaximumSyncToEvents + currentEventIndex))
                                .Select(currentEvent => new Event() // fill in the events from the current batch, requires reselection
                                {
                                    // action is the FileChangeType plus file system object type combined into a string
                                    Action =
                                        // Folder events (isFolder is true)
                                        (currentEvent.FileChange.Metadata.HashableProperties.IsFolder
                                        ? (currentEvent.FileChange.Type == FileChangeType.Created
                                            ? CLDefinitions.CLEventTypeAddFolder
                                            : (currentEvent.FileChange.Type == FileChangeType.Deleted
                                                ? CLDefinitions.CLEventTypeDeleteFolder
                                                : (currentEvent.FileChange.Type == FileChangeType.Modified
                                                    ? getArgumentException(true, FileChangeType.Modified, null) // a folder cannot have a modified event
                                                    : (currentEvent.FileChange.Type == FileChangeType.Renamed
                                                        ? CLDefinitions.CLEventTypeRenameFolder
                                                        : getArgumentException(true, currentEvent.FileChange.Type, null))))) // the only FileChangeTypes recognized are created/deleted/renamed/modified

                                        // File events (isFolder is not true and the file does not have a shortcut target path)
                                        : (/* override as true since LinkTargetPath was temporarily removed due to database complexity in removing all paths */ true //currentEvent.FileChange.Metadata.LinkTargetPath == null
                                            ? (currentEvent.FileChange.Type == FileChangeType.Created
                                                ? CLDefinitions.CLEventTypeAddFile
                                                : (currentEvent.FileChange.Type == FileChangeType.Deleted
                                                    ? CLDefinitions.CLEventTypeDeleteFile
                                                    : (currentEvent.FileChange.Type == FileChangeType.Modified
                                                        ? CLDefinitions.CLEventTypeModifyFile
                                                        : (currentEvent.FileChange.Type == FileChangeType.Renamed
                                                            ? CLDefinitions.CLEventTypeRenameFile
                                                            : getArgumentException(false, currentEvent.FileChange.Type, null))))) // the only FileChangeTypes recognized are created/deleted/renamed/modified

                                            // Shortcut events (isFolder is not true and the file does have a shortcut target path)
                                            : (currentEvent.FileChange.Type == FileChangeType.Created
                                                ? CLDefinitions.CLEventTypeAddLink
                                                : (currentEvent.FileChange.Type == FileChangeType.Deleted
                                                    ? CLDefinitions.CLEventTypeDeleteLink
                                                    : (currentEvent.FileChange.Type == FileChangeType.Modified
                                                        ? CLDefinitions.CLEventTypeModifyLink
                                                        : (currentEvent.FileChange.Type == FileChangeType.Renamed
                                                            ? CLDefinitions.CLEventTypeRenameLink
                                                            : getArgumentException(false, currentEvent.FileChange.Type, "Place LinkTargetPath here" /*currentEvent.FileChange.Metadata.LinkTargetPath*/))))))), // the only FileChangeTypes recognized are created/deleted/renamed/modified

                                    EventId = currentEvent.FileChange.EventId, // this is out local identifier for the event which will be passed as the "client_reference" and returned so we can correlate the response event
                                    Metadata = new Metadata()
                                    {
                                        ServerUid = currentEvent.FileChange.Metadata.ServerUid, // the unique id on the server
                                        ParentUid = currentEvent.FileChange.Metadata.ParentFolderServerUid,
                                        ToParentUid = currentEvent.FileChange.Metadata.ParentFolderServerUid,
                                        Name = currentEvent.FileChange.NewPath.Name,
                                        ToName = currentEvent.FileChange.NewPath.Name,

                                        //// Phil says this case is fixed, test thoroughly before removing this commented section; the commented section immediately below this is currently not fixed, so don't remove that one until checked seperately
                                        //
                                        //// TODO: remove this property setter; supposedly "You do not have to provide to_path." according to Phil, but if you don't provide it then folder renames give the error “No to_path found.”
                                        //// for now this is left in until this problem is fixed
                                        //RelativeToPath = (currentEvent.FileChange.Type == FileChangeType.Renamed
                                        //    ? currentEvent.FileChange.NewPath.GetRelativePath((syncBox.CopiedSettings.SyncRoot ?? string.Empty), true) + // path relative to the root with slashes switched for the NewPath (this one should be the one read only for renames, but set it anyways)
                                        //        (currentEvent.FileChange.Metadata.HashableProperties.IsFolder
                                        //            ? "/" // append forward slash at end of folder paths
                                        //            : string.Empty)
                                        //    : null),

                                        //// TODO: remove this property setter; same as above, except folder creations give the error "No path provided." if path is not provided
                                        //// for now this is left in until this problem is fixed
                                        //RelativePath = (currentEvent.FileChange.Type == FileChangeType.Created
                                        //    ? currentEvent.FileChange.NewPath.GetRelativePath((syncBox.CopiedSettings.SyncRoot ?? string.Empty), true) + // path relative to the root with slashes switched for the NewPath (this one should be the one read only for renames, but set it anyways)
                                        //        (currentEvent.FileChange.Metadata.HashableProperties.IsFolder
                                        //            ? "/" // append forward slash at end of folder paths
                                        //            : string.Empty)
                                        //    : null),

                                        CreatedDate = currentEvent.FileChange.Metadata.HashableProperties.CreationTime, // when the file system object was created
                                        Deleted = currentEvent.FileChange.Type == FileChangeType.Deleted, // whether or not the file system object is deleted
                                        Hash = ((Func<FileChange, string>)(innerEvent => // hash must be retrieved via function because the appropriate FileChange call has an output parameter (and requires error checking)
                                        {
                                            // declare hash to return
                                            string currentEventMD5;
                                            // try to retrieve the hash from the current FileChange (can be null), storing any error
                                            CLError currentEventMD5Error = innerEvent.GetMD5LowercaseString(out currentEventMD5);
                                            // if there was an error retrieving the hash, then rethrow the error
                                            if (currentEventMD5Error != null)
                                            {
                                                throw new AggregateException("Error retrieving currentEvent.GetMD5LowercaseString", currentEventMD5Error.GrabExceptions());
                                            }
                                            // return the retrieved hash (or null)
                                            return currentEventMD5;
                                        }))(currentEvent.FileChange), // run the above hash retrieval function for the current FileChange
                                        IsFolder = currentEvent.FileChange.Metadata.HashableProperties.IsFolder, // whether this is a folder
                                        LastEventId = lastEventId, // the highest event id of all FileChanges in the current batch
                                        ModifiedDate = currentEvent.FileChange.Metadata.HashableProperties.LastTime, // when this file system object was last modified
                                        Revision = currentEvent.FileChange.Metadata.Revision, // last communicated revision for this FileChange
                                        Size = currentEvent.FileChange.Metadata.HashableProperties.Size, // the file size (or null for folders)
                                        StorageKey = currentEvent.FileChange.Metadata.StorageKey, // the server location for storage of this file (or null for a folder); probably not read
                                        Version = "1.0", // I do not know what value should be placed here
                                        MimeType = currentEvent.FileChange.Metadata.MimeType // never retrieved from Windows
                                    }
                                }).ToArray(), // selected into a new array
                            SyncBoxId = syncBox.SyncBoxId, // pass in the sync box id
                            DeviceId = syncBox.CopiedSettings.DeviceId // pass in the device id
                        };

                        // declare the status for the sync to http operation
                        CLHttpRestStatus syncToStatus;
                        // declare the json contract object for the response content
                        To currentBatchResponse;
                        // perform a sync to of the current batch of changes, storing any error
                        CLError syncToError = httpRestClient.SyncToCloud(syncTo, // request object with current batch of changes to upload and current sync id
                            HttpTimeoutMilliseconds, // milliseconds before communication would timeout for each operation
                            out syncToStatus, // output the status of the communication
                            out currentBatchResponse); // output the response object from a successful communication

                        // depending on whether the communication status is a connection failure or not, either increment the failure count or clear it, respectively

                        if (syncToStatus == CLHttpRestStatus.ConnectionFailed)
                        {
                            lock (MetadataConnectionFailures)
                            {
                                if (MetadataConnectionFailures.Value != ((byte)255))
                                {
                                    MetadataConnectionFailures.Value = (byte)(MetadataConnectionFailures.Value + 1);
                                }
                            }
                        }
                        else if (syncToStatus == CLHttpRestStatus.NotAuthorized)
                        {
                            credentialsError = CredentialErrorType.OtherError;
                        }
                        else if (syncToStatus == CLHttpRestStatus.NotAuthorizedExpiredCredentials)
                        {
                            credentialsError = CredentialErrorType.ExpiredCredentials;
                        }
                        else
                        {
                            lock (MetadataConnectionFailures)
                            {
                                if (MetadataConnectionFailures.Value != ((byte)0))
                                {
                                    MetadataConnectionFailures.Value = 0;
                                }
                            }
                        }

                        // if an error occurred performing sync to, rethrow the error
                        if (syncToError != null)
                        {
                            throw new AggregateException("An error occurred in SyncTo communication", syncToError.GrabExceptions());
                        }

                        // if sync to was not successful, throw an error
                        if (syncToStatus != CLHttpRestStatus.Success)
                        {
                            throw new Exception("SyncTo communication was not successful: CLHttpRestStatus." + syncToStatus.ToString());
                        }

                        // validate a couple fields in the response which are required for processing

                        if (currentBatchResponse.Events == null)
                        {
                            throw new NullReferenceException("Invalid HTTP response body in Sync To, Events cannot be null");
                        }
                        if (currentBatchResponse.SyncId == null)
                        {
                            throw new NullReferenceException("Invalid HTTP response body in Sync To, SyncId cannot be null");
                        }

                        // record the new sync id from the server
                        syncString = currentBatchResponse.SyncId;

                        // if no batches have been processed so far, then use the current batch response as the response to process
                        if (deserializedResponse == null)
                        {
                            deserializedResponse = currentBatchResponse;
                        }
                        // else if at least one batch has already been processed, then append the current batch
                        else
                        {
                            // store the previous events
                            Event[] previousEvents = deserializedResponse.Events;
                            // store the current batch events
                            Event[] newEvents = currentBatchResponse.Events;

                            // use the current batch as the base object to process (will therefore take properties like the sync id from the latest batch)
                            deserializedResponse = currentBatchResponse;
                            // assign a new event array the size of the combined event lengths
                            deserializedResponse.Events = new Event[previousEvents.Length + newEvents.Length];
                            // copy the first portion of the events from the previous events
                            previousEvents.CopyTo(deserializedResponse.Events, 0);
                            // copy the second portion of the events from the current batch events
                            newEvents.CopyTo(deserializedResponse.Events, previousEvents.Length);
                        }
                    }

                    // if after processing all batches, the response to process was not set, then throw an exception
                    if (deserializedResponse == null)
                    {
                        throw new NullReferenceException("After all Sync To batches, deserializedResponse cannot be null");
                    }
                    // else if there is a response to process, then store the response's sync id for return
                    else
                    {
                        newSyncId = deserializedResponse.SyncId;
                    }

                    // create a list for events duplicated between Sync From and Sync To
                    List<int> duplicatedEvents = new List<int>();
                    // if there are events in the response to process, then loop through all events looking for duplicates between Sync From and Sync To
                    if (deserializedResponse.Events.Length > 0)
                    {
                        AppendRandomSubSecondTicksToSyncFromFolderCreationTimes(deserializedResponse.Events);

                        // create a list for the indexes of events for Sync From
                        List<int> fromEvents = new List<int>();
                        // create a list for the paths of events for Sync To (excluding events with a "download" status)
                        HashSet<string> eventsByUid = new HashSet<string>();
                        // loop for all the indexes in the response events
                        for (int currentEventIndex = 0; currentEventIndex < deserializedResponse.Events.Length; currentEventIndex++)
                        {
                            // try/catch for the current event, add the current index to fromEvents if Sync From or add the current event path to eventsByPath if a non-download Sync To, failing silently
                            try
                            {
                                // grab the current event
                                Event currentEvent = deserializedResponse.Events[currentEventIndex];

                                bool isRootFolder = (currentEvent.Metadata != null && currentEvent.Metadata.Name == string.Empty && string.IsNullOrEmpty(currentEvent.Metadata.ToName)); // special event on SID "0" for root folder

                                if (isRootFolder)
                                {
                                    syncRootUid = currentEvent.Metadata.ServerUid;
                                    serverUidsToPath[currentEvent.Metadata.ServerUid] = syncBox.CopiedSettings.SyncRoot;
                                    pathsToServerUid[syncBox.CopiedSettings.SyncRoot] = currentEvent.Metadata.ServerUid;
                                }

                                if (currentEvent.Metadata == null || !isRootFolder)
                                {
                                    if (currentEvent.Header == null)
                                    {
                                        throw new NullReferenceException("Invalid HTTP response body in Sync To, an Event has a null Sync Header");
                                    }

                                    // if there is no status set (Sync From), then add current index to fromEvents
                                    if (string.IsNullOrEmpty(currentEvent.Header.Status))
                                    {
                                        fromEvents.Add(currentEventIndex);
                                    }
                                    // else if there is a status set (Sync To) and the event is not a download, then add to eventsByPath (Sync To events)
                                    else if (currentEvent.Header.Status != CLDefinitions.CLEventTypeDownload) // exception for download when looking for dependencies since we actually want the Sync From event
                                    {
                                        // add the file path to eventsByPath (Sync To paths) from either the original change (rename events only) or produce it from the root folder path plus the metadata path
                                        eventsByUid.Add((currentEvent.Metadata == null

                                            // if the current event does not have metadata (a sign of a rename event??), then find the original change sent to the server which matches by event id and use its file path
                                            ? toCommunicate.First(currentToCommunicate => (currentEvent.EventId != null || currentEvent.Header.EventId != null) && currentToCommunicate.FileChange.EventId == (long)(currentEvent.EventId ?? currentEvent.Header.EventId))
                                                .FileChange.Metadata.ServerUid

                                            // else if the current event does have metadata (non-rename events), then build the path from the root path plus the metadata path
                                            : currentEvent.Metadata.ServerUid));
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        // loop through the indexes of From Events
                        foreach (int currentEventIndex in fromEvents)
                        {
                            // try/catch check if the Sync From event is duplicated from a Sync To event, failing silently
                            try
                            {
                                // if the current Sync From event's path is found in the paths for Sync To events, then add the current Sync From event index as duplicate
                                if (eventsByUid.Contains(deserializedResponse.Events[currentEventIndex].Metadata.ServerUid))
                                {
                                    // from event is duplicate, add its index to duplicates
                                    duplicatedEvents.Add(currentEventIndex);
                                }
                            }
                            catch
                            {
                            }
                        }

                        // sort the duplicates list so it can be searched by binary search
                        duplicatedEvents.Sort();
                    }

                    // for the following, because it will be unlikely to have two events in a communication with the same event id,
                    // completed changes and changes in error mappings are to arrays which are more efficient to create but more difficult to modify (which should be rare);
                    // incomplete changes are more likely to contend on the same event id so they are mapped to a list which is slower to create but easier to modify (I can't remember why I thought it would be more common to content)

                    // create a dictionary mapping event id to completed changes
                    Dictionary<long, PossiblyChangedFileChange[]> completedChangesList = new Dictionary<long, PossiblyChangedFileChange[]>();
                    // create a dictionary mapping event id to incompleted changes
                    Dictionary<long, List<PossiblyStreamableAndPossiblyChangedFileChange>> incompleteChangesList = new Dictionary<long, List<PossiblyStreamableAndPossiblyChangedFileChange>>();
                    // create a hashset for storing Streams which are synchronously disposed because they are not needed
                    HashSet<Stream> completedStreams = new HashSet<Stream>();

                    // declare a dictionary for already visited Sync From renames so if metadata was found for a rename in an event then later renames can carry forward the metadata
                    FilePathDictionary<FileMetadata> alreadyVisitedRenames;
                    // initialize the visited renames dictionary, storing any error that occurs
                    CLError createVisitedRenames = FilePathDictionary<FileMetadata>.CreateAndInitialize((syncBox.CopiedSettings.SyncRoot ?? string.Empty),
                        out alreadyVisitedRenames);

                    // create a dictionary mapping event id to changes which were moved as dependencies under new pseudo-Sync From changes (i.e. conflict)
                    Dictionary<long, PossiblyStreamableFileChange[]> changesConvertedToDependencies = new Dictionary<long, PossiblyStreamableFileChange[]>();

                    // loop for all the indexes in the response events
                    for (int currentEventIndex = 0; currentEventIndex < deserializedResponse.Events.Length; currentEventIndex++)
                    {
                        // check for sync shutdown
                        Monitor.Enter(FullShutdownToken);
                        try
                        {
                            if (FullShutdownToken.Token.IsCancellationRequested)
                            {
                                completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                                incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                                changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                                newSyncId = Helpers.DefaultForType<string>();
                                return new Exception("Shut down in the middle of communication");
                            }
                        }
                        finally
                        {
                            Monitor.Exit(FullShutdownToken);
                        }

                        // if the current event index is not found in the list of duplicates, then process the event
                        if (duplicatedEvents.BinarySearch(currentEventIndex) < 0)
                        {
                            // grab the current event by index
                            Event currentEvent = deserializedResponse.Events[currentEventIndex];

                            bool isRootFolder = (currentEvent.Metadata != null && currentEvent.Metadata.Name == string.Empty && string.IsNullOrEmpty(currentEvent.Metadata.ToName)); // special event on SID "0" for root folder

                            if (isRootFolder)
                            {
                                syncRootUid = currentEvent.Metadata.ServerUid;
                                serverUidsToPath[currentEvent.Metadata.ServerUid] = syncBox.CopiedSettings.SyncRoot;
                                pathsToServerUid[syncBox.CopiedSettings.SyncRoot] = currentEvent.Metadata.ServerUid;
                            }

                            if (currentEvent.Metadata == null || !isRootFolder)
                            {
                                // define the current FileChange, defaulting to null
                                FileChangeWithDependencies currentChange = null;
                                // define the current Stream, defaulting to null
                                Stream currentStream = null;
                                // define a string for storing an event's revision which will be used to replace a Sync To event revision upon certain conflict conditions
                                string previousRevisionOnConflictException = null;

                                // try/catch create a FileChange out of the current event, handle special rename conditions, decide if the metadata has changed to update SQL, and case-switch to decide what to do with the FileChange, on catch add the FileChange as an error to be reprocessed
                                try
                                {
                                    // full path for the destination of the event
                                    FilePath findNewPath;
                                    // full path for a previous destination of a rename event
                                    FilePath findOldPath;
                                    // MD5 hash for the event as a string
                                    string findHash;
                                    // unique id from server
                                    string findServerUid;
                                    // unique id of parent from server
                                    string findParentId;
                                    // Metadata properties for the event
                                    FileMetadataHashableProperties findHashableProperties;
                                    // storage key for a file event
                                    string findStorageKey;
                                    // revision for a file event
                                    string findRevision;
                                    // never set on Windows
                                    string findMimeType;
                                    
                                    // set the previous FileChange which was matched to the current event, first from the previous FileChange calculated for no event metadata or null if no "client_reference" was returned or finally search it from the communicated events by event id
                                    Nullable<PossiblyStreamableFileChange> matchedChange;

                                    // if the current event has no metadata (for rename events??), then use the previous FileChange for metadata and fill in all the fields for the current FileChange
                                    if (currentEvent.Metadata == null)
                                    {
                                        // define a FileChange for the previous event which may be found from the events which were sent up to the server, or null as default
                                        // use the previous FileChange for metadata, searching by matching event ids, throws an error if no matching FileChanges are found
                                        Nullable<PossiblyStreamableFileChange> usePreviousFileChange = toCommunicate.FirstOrDefault(currentToCommunicate =>
                                            (currentEvent.EventId != null || currentEvent.Header.EventId != null)
                                                && currentToCommunicate.FileChange.EventId == (long)(currentEvent.EventId ?? currentEvent.Header.EventId));

                                        // is the next line just duplicate logic form the line above???

                                        // set the previous FileChange which was matched to the current event, first from the previous FileChange calculated for no event metadata or null if no "client_reference" was returned or finally search it from the communicated events by event id
                                        matchedChange = usePreviousFileChange // already found previous FileChange if the current event had no metadata
                                            ?? ((currentEvent.Header.EventId == null || currentEvent.Header.EventId == 0)
                                                ? (Nullable<PossiblyStreamableFileChange>)null // if the current event has metadata and does not have "client_reference" set, then there was no previous change (new Sync From)
                                                : toCommunicate.FirstOrDefault(currentToCommunicate => currentToCommunicate.FileChange.EventId == (long)currentEvent.Header.EventId)); // else if the current event has metadata and has "client_reference" set then use it to find the previous event from the list communicated (match against event id)

                                        // cast the found change as non-nullable
                                        PossiblyStreamableFileChange nonNullPreviousFileChange = (PossiblyStreamableFileChange)usePreviousFileChange;
                                        // set the new path
                                        findNewPath = nonNullPreviousFileChange.FileChange.NewPath;
                                        // set the old path for renames or null otherwise
                                        findOldPath = nonNullPreviousFileChange.FileChange.OldPath;
                                        // try to retrieve the hash, storing any error that occurs (could be null for non-files)
                                        CLError hashRetrievalError = nonNullPreviousFileChange.FileChange.GetMD5LowercaseString(out findHash);
                                        // if an error occurred retrieving the hash, then rethrow the exception
                                        if (hashRetrievalError != null)
                                        {
                                            throw new AggregateException("Error retrieving MD5 hash as lowercase string", hashRetrievalError.GrabExceptions());
                                        }
                                        // set the unique server id
                                        findServerUid = nonNullPreviousFileChange.FileChange.Metadata.ServerUid;
                                        // set the unique parent folder server id
                                        findParentId = nonNullPreviousFileChange.FileChange.Metadata.ParentFolderServerUid;
                                        // set the metadata properties
                                        findHashableProperties = nonNullPreviousFileChange.FileChange.Metadata.HashableProperties;
                                        // set the storage key, or null if the event is not for a file
                                        findStorageKey = nonNullPreviousFileChange.FileChange.Metadata.StorageKey;
                                        // set the revision, or null if the event is not for a file
                                        findRevision = nonNullPreviousFileChange.FileChange.Metadata.Revision;
                                        // never set on Windows
                                        findMimeType = nonNullPreviousFileChange.FileChange.Metadata.MimeType;
                                    }
                                    // else if the current event has metadata, then set all the properties for the FileChange from the event metadata
                                    else
                                    {
                                        // set the previous FileChange which was matched to the current event, first from the previous FileChange calculated for no event metadata or null if no "client_reference" was returned or finally search it from the communicated events by event id
                                        findPathsByUids.TypedData.matchedChange.Value = matchedChange = (currentEvent.Header.EventId == null || currentEvent.Header.EventId == 0)
                                            ? (Nullable<PossiblyStreamableFileChange>)null // if the current event has metadata and does not have "client_reference" set, then there was no previous change (new Sync From)
                                            : toCommunicate.FirstOrDefault(currentToCommunicate => currentToCommunicate.FileChange.EventId == (long)currentEvent.Header.EventId); // else if the current event has metadata and has "client_reference" set then use it to find the previous event from the list communicated (match against event id)

                                        findPathsByUids.TypedData.currentEvent.Value = currentEvent;
                                        var findPathsResult = findPathsByUids.TypedProcess();
                                        findNewPath = findPathsResult.findNewPath;
                                        findOldPath = findPathsResult.findOldPath;

                                        // set the MD5 hash, or null for non-files
                                        findHash = currentEvent.Metadata.Hash;
                                        // set the unique server id
                                        findServerUid = currentEvent.Metadata.ServerUid;
                                        // set the parent folder server id
                                        findParentId = currentEvent.Metadata.ToParentUid ?? currentEvent.Metadata.ParentUid;
                                        // set the metadata properties
                                        findHashableProperties = new FileMetadataHashableProperties(currentEvent.Metadata.IsFolder ?? ParseEventStringToIsFolder(currentEvent.Header.Action ?? currentEvent.Action), // whether the event represents a folder, first try to grab the bool otherwise you can parse it from the action
                                            currentEvent.Metadata.ModifiedDate, // the last time the file system object was modified
                                            currentEvent.Metadata.CreatedDate, // the time the file system object was created
                                            currentEvent.Metadata.Size); // the size of a file or null for non-files
                                        // set the revision from the current file, or null for non-files
                                        findRevision = currentEvent.Metadata.Revision;
                                        // set the storage key from the current file, or null for non-files
                                        findStorageKey = currentEvent.Metadata.StorageKey;
                                        // never set on Windows
                                        findMimeType = currentEvent.Metadata.MimeType;
                                    }

                                    // create a FileChange with dependencies using a new FileChange from the stored FileChange data (except metadata) and adding the MD5 hash (null for non-files)
                                    currentChange = CreateFileChangeFromBaseChangePlusHash(
                                        new FileChange(
                                            DelayCompletedLocker: null,
                                            fileDownloadMoveLocker:
                                                ((string.IsNullOrEmpty(currentEvent.Header.Status)
                                                        || CLDefinitions.SyncHeaderDeletions.Contains(currentEvent.Header.Action ?? currentEvent.Action)
                                                        || CLDefinitions.SyncHeaderRenames.Contains(currentEvent.Header.Action ?? currentEvent.Action))
                                                    ? null
                                                    : new object()))
                                            {
                                                Direction = (string.IsNullOrEmpty(currentEvent.Header.Status) ? SyncDirection.From : SyncDirection.To), // Sync From events have no status while Sync To events have status
                                                EventId = currentEvent.Header.EventId ?? 0, // The "client_reference" field from the communication which was set from a Sync To event or left out for Sync From, null-coallesce for the second case
                                                NewPath = findNewPath, // The full path for the event
                                                OldPath = findOldPath, // The previous path for rename events, or null for everything else
                                                Type = ParseEventStringToType(currentEvent.Header.Action ?? currentEvent.Action) // The FileChange type parsed from the event action
                                            },
                                        findHash, // The MD5 hash, or null for non-files
                                        DependencyDebugging);

                                    // if a matched change was set and has metadata, then record its revision as the previous revision to set for conflicts
                                    if (matchedChange != null
                                        && ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata != null)
                                    {
                                        previousRevisionOnConflictException = ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.Revision;
                                    }

                                    // set the metadata for the current FileChange (copying the RevisionChanger if a previous matched FileChange was found)
                                    currentChange.Metadata = new FileMetadata(matchedChange == null ? null : ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.RevisionChanger) // copy previous RevisionChanger if possible
                                        {
                                            ServerUid = findServerUid, // set the server unique id
                                            ParentFolderServerUid = findParentId,
                                            HashableProperties = findHashableProperties, // set the metadata properties
                                            Revision = findRevision, // set the file revision, or null for non-files
                                            StorageKey = findStorageKey, // set the storage key, or null for non-files
                                            MimeType = findMimeType // never set on Windows
                                        };

                                    // store the previous values for revision and server uid since firing the revision changer will change these;
                                    // the originals are needed for comparison to see if metadata has changed
                                    string storeOldRevision = (matchedChange == null ? null : ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.Revision);
                                    string storeOldServerUid = (matchedChange == null ? null : ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.ServerUid);

                                    if (matchedChange != null
                                        && (storeOldRevision != findRevision
                                            || storeOldServerUid != findServerUid))
                                    {
                                        currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);
                                    }

                                    // if a matched change was set, then use the Stream from the previous FileChange as the current Stream
                                    if (matchedChange != null)
                                    {
                                        currentStream = ((PossiblyStreamableFileChange)matchedChange).Stream;
                                    }

                                    // define a bool for whether the current event is a rename but no metadata was found amongst current FileChanges nor in the last sync states in the database
                                    bool notFoundRename = false;

                                    // define an action to add the current FileChange to the list of incomplete changes, including any Stream and whether or not the FileChange requires updating the event source database
                                    Action<Dictionary<long, List<PossiblyStreamableAndPossiblyChangedFileChange>>, FileChangeWithDependencies, Stream, bool> AddToIncompleteChanges = (innerIncompleteChangesList, innerCurrentChange, innerCurrentStream, innerMetadataIsDifferent) =>
                                        {
                                            // wrap the current change for adding to the incomplete changes list
                                            PossiblyStreamableAndPossiblyChangedFileChange addChange = new PossiblyStreamableAndPossiblyChangedFileChange(resultOrder++,
                                                innerMetadataIsDifferent,
                                                innerCurrentChange,
                                                innerCurrentStream);

                                            // if the incomplete change's map already contains the current change's event id, then add the current change to the existing list
                                            if (innerIncompleteChangesList.ContainsKey(innerCurrentChange.EventId))
                                            {
                                                innerIncompleteChangesList[innerCurrentChange.EventId].Add(addChange);
                                            }
                                            // else if the incomplete change's map does not already contain the current change's event id, then create a new list with only the current change and add it to the map for the current change's event id
                                            else
                                            {
                                                innerIncompleteChangesList.Add(innerCurrentChange.EventId,
                                                    new List<PossiblyStreamableAndPossiblyChangedFileChange>(Helpers.EnumerateSingleItem(
                                                            addChange
                                                        )));
                                            }
                                        };

                                    switch (currentChange.Type)
                                    {
                                        // if the current event is a rename, then try to find its metadata from existing changes, the database, or lastly if not found then mark it not found and try to query the server for metadata and process a new event
                                        case FileChangeType.Renamed:
                                            // the current FileChange is a rename and should have a previous path, if not then throw an exception
                                            if (currentChange.OldPath == null)
                                            {
                                                throw new NullReferenceException("OldPath cannot be null if currentChange is of Type Renamed");
                                            }

                                            // define a FileChange which will store the last matching FileChange with the metadata for this rename event
                                            FileChange fileChangeForOriginalMetadata = null;
                                            // if a previous matched change for the current event was not found, then try to search for a change by the events path and revision
                                            if (matchedChange == null)
                                            {
                                                // if the current event does not have a revision then we cannot search, so throw an exception
                                                if (string.IsNullOrEmpty(currentChange.Metadata.Revision))
                                                {
                                                    throw new NullReferenceException("Revision cannot be null if currentChange is of Type Renamed and matchedChange is also null");
                                                }

                                                // declare a FileChange which will be used as temp storage upon iterations searching the current UpDownEvent changes and the current failures
                                                FileChange foundOldPath;
                                                // declare metadata which will be used as temp storage upon iterations searching the previously visited renames
                                                FileMetadata foundOldPathMetadataOnly;

                                                // loop through previously visited renamed Metadata, the FileChanges in the current UpDownEvent changes, the failure changes, and the currently processing changes where the OldPath matches the current event's previous path
                                                foreach (FileChange findMetadata in

                                                    // search the already visited Sync From rename events by the current event's previous path for when multiple rename events in a communication batch keep moving the metadata forward
                                                    (alreadyVisitedRenames.TryGetValue(currentChange.OldPath, out foundOldPathMetadataOnly)
                                                        ? Helpers.EnumerateSingleItem(
                                                                new FileChange() // if a match is found, then include the found result
                                                                {
                                                                    Metadata = foundOldPathMetadataOnly, // metadata to move forward
                                                                }
                                                            )
                                                        : Enumerable.Empty<FileChange>()) // else if a match is not found, then use an empty enumeration

                                                    // search the current UpDownEvents for one matching the current event's previous path (comparing against the UpDownEvent's NewPath)
                                                    .Concat((getRunningUpDownChangesDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                                        ? new FileChange[] { foundOldPath } // if a match is found, then include the found result
                                                        : Enumerable.Empty<FileChange>()) // else if a match is not found, then use an empty enumeration

                                                        // then search the current failures for the one matching the current event's previous path (comparing against the failed event's NewPath)
                                                        .Concat(getFailuresDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                                            ? new FileChange[] { foundOldPath } // if a match is found, then include the found result
                                                            : Enumerable.Empty<FileChange>()) // else if a match is not found, then use an empty enumeration

                                                        // lastly search the current list of communicated events for one matching the current event's previous path (comparing against the communicated event's NewPath)
                                                        .Concat(communicationArray.Where(currentCommunication =>
                                                                FilePathComparer.Instance.Equals(currentCommunication.FileChange.NewPath, currentChange.OldPath)) // search current rename event's OldPath against the other communication event's NewPath
                                                            .Select(currentCommunication => currentCommunication.FileChange)) // select into the correct format

                                                            // order the results descending by EventId so the first match will be the latest event
                                                            .OrderByDescending(currentOldPath => currentOldPath.EventId)))
                                                {
                                                    // if the current matched change by path also matches by revision, then use the found change for the previous metadata and stop searching
                                                    if (findMetadata.Metadata.Revision == currentChange.Metadata.Revision)
                                                    {
                                                        // use the found change for the previous metadata
                                                        fileChangeForOriginalMetadata = findMetadata;

                                                        // if the current event direction is from the server, then add the current event new path as a visited path for the Sync From renames
                                                        if (currentChange.Direction == SyncDirection.From)
                                                        {
                                                            // declare a hierarchy for the old path of the rename
                                                            FilePathHierarchicalNode<FileMetadata> renameHierarchy;
                                                            // grab the hierarchy for the old path of the rename from already visited renames, storing any error that occurred
                                                            CLError grabHierarchyError = alreadyVisitedRenames.GrabHierarchyForPath(currentChange.OldPath, out renameHierarchy, suppressException: true);
                                                            // if there was an error grabbing the hierarchy, then rethrow the error
                                                            if (grabHierarchyError != null)
                                                            {
                                                                throw new AggregateException("Error grabbing renameHierarchy from alreadyVisitedRenames", grabHierarchyError.GrabExceptions());
                                                            }

                                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                                            {
                                                                // if there was a hierarchy found at the old path for the rename, then apply a rename to the dictionary based on the current rename
                                                                if (renameHierarchy != null)
                                                                {
                                                                    alreadyVisitedRenames.Rename(currentChange.OldPath, currentChange.NewPath.Copy());
                                                                }

                                                                // add the currently found metadata to the rename dictionary so it can be searched for subsequent renames
                                                                alreadyVisitedRenames[currentChange.NewPath.Copy()] = findMetadata.Metadata;
                                                            }
                                                        }

                                                        // stop searching for a match
                                                        break;
                                                    }
                                                }

                                                // if a change was not found by the rename event's old path and revision, then try to grab the previous metadata from the database
                                                if (fileChangeForOriginalMetadata == null)
                                                {
                                                    // declare metadata which will be output from the database
                                                    FileMetadata syncStateMetadata;
                                                    // search the database for metadata for the event's previous path and revision, storing any error that occurred
                                                    CLError queryMetadataError = syncData.getMetadataByPathAndRevision(currentChange.OldPath.ToString(),
                                                        currentChange.Metadata.Revision,
                                                        out syncStateMetadata);

                                                    // if there was an error querying the database for existing metadata, then rethrow the error
                                                    if (queryMetadataError != null)
                                                    {
                                                        throw new AggregateException("Error querying SqlIndexer for sync state by path: " + currentChange.OldPath.ToString() +
                                                            " and revision: " + currentChange.Metadata.Revision, queryMetadataError.GrabExceptions());
                                                    }

                                                    // if no metadata was returned from the database, then throw an error if the change originated on the client or otherwise try to grab the metadata from the server for a new creation event at the final destination of the rename
                                                    if (syncStateMetadata == null)
                                                    {
                                                        // if the change is a Sync From, then try to grab the metadata from the server at the new destination for the rename to use to create a new creation event at the new path
                                                        if (currentChange.Direction == SyncDirection.From)
                                                        {
                                                            // declare the status of communication from getting metadata
                                                            CLHttpRestStatus getNewMetadataStatus;
                                                            // declare the response object of the actual metadata when returned
                                                            JsonContracts.Metadata newMetadata;
                                                            // grab the metadata from the server for the current path and whether or not the current event represents a folder, storing any error that occurs
                                                            CLError getNewMetadataError = httpRestClient.GetMetadata(currentChange.NewPath, // path to query
                                                                currentChange.Metadata.HashableProperties.IsFolder, // whether path represents a folder (as opposed to a file or shortcut)
                                                                HttpTimeoutMilliseconds, // milliseconds before communication would expire on an operation
                                                                out getNewMetadataStatus, // output the status of communication
                                                                out newMetadata); // output the resulting metadata, if any is found

                                                            // if an error occurred getting metadata, rethrow the error
                                                            if (getNewMetadataError != null)
                                                            {
                                                                throw new AggregateException("An error occurred retrieving metadata", getNewMetadataError.GrabExceptions());
                                                            }

                                                            // if the communication was not successful, then throw an error with the bad status
                                                            if (getNewMetadataStatus != CLHttpRestStatus.Success
                                                                && getNewMetadataStatus != CLHttpRestStatus.NoContent)
                                                            {
                                                                throw new Exception("Retrieving metadata did not return successful status: CLHttpRestStatus." + getNewMetadataStatus.ToString());
                                                            }

                                                            // if there was no content, then the metadata was not found at the given path so throw an error
                                                            if (getNewMetadataStatus == CLHttpRestStatus.NoContent
                                                                || newMetadata.Deleted == true)
                                                            {
                                                                throw new Exception("Metadata not found for given path");
                                                            }

                                                            if (newMetadata.IsNotPending == false)
                                                            {
                                                                CLHttpRestStatus fileVersionsStatus;
                                                                JsonContracts.FileVersion[] fileVersions;
                                                                CLError fileVersionsError = httpRestClient.GetFileVersions(
                                                                    newMetadata.ServerUid,
                                                                    HttpTimeoutMilliseconds,
                                                                    out fileVersionsStatus,
                                                                    out fileVersions);

                                                                if (fileVersionsStatus != CLHttpRestStatus.Success
                                                                    && fileVersionsStatus != CLHttpRestStatus.NoContent)
                                                                {
                                                                    throw new AggregateException("An error occurred retrieving previous versions of a file", fileVersionsError.GrabExceptions());
                                                                }

                                                                JsonContracts.FileVersion lastNonPendingVersion = (fileVersions ?? Enumerable.Empty<JsonContracts.FileVersion>())
                                                                    .OrderByDescending(fileVersion => (fileVersion.Version ?? -1))
                                                                    .FirstOrDefault(fileVersion => fileVersion.IsDeleted != true
                                                                        && fileVersion.IsNotPending != false);

                                                                if (lastNonPendingVersion == null)
                                                                {
                                                                    throw new Exception("A previous non-pending file version was not found");
                                                                }

                                                                newMetadata.IsNotPending = true;

                                                                // server does not version other metadata, so these are the only ones we can really use to update
                                                                newMetadata.StorageKey = lastNonPendingVersion.StorageKey;
                                                                newMetadata.Hash = lastNonPendingVersion.FileHash;
                                                                newMetadata.Size = lastNonPendingVersion.FileSize;
                                                            }

                                                            // create and initialize the FileChange for the new file creation by combining data from the current rename event with the metadata from the server, also adds the hash
                                                            FileChangeWithDependencies newPathCreation = CreateFileChangeFromBaseChangePlusHash(new FileChange(DelayCompletedLocker: null, fileDownloadMoveLocker: new object())
                                                                {
                                                                    Direction = SyncDirection.From, // emulate a new Sync From event so the client will try to download the file from the new location
                                                                    NewPath = currentChange.NewPath, // new location only (no previous location since this is converted from a rename to a create)
                                                                    Type = FileChangeType.Created, // a create to download a new file or process a new folder
                                                                    Metadata = new FileMetadata()
                                                                    {
                                                                        //Need to find what key this is //LinkTargetPath <-- what does this comment mean?

                                                                        ServerUid = currentChange.Metadata.ServerUid, // the unique id on the server
                                                                        HashableProperties = new FileMetadataHashableProperties(currentChange.Metadata.HashableProperties.IsFolder, // whether this creation is a folder
                                                                            newMetadata.ModifiedDate, // last modified time for this file system object
                                                                            newMetadata.CreatedDate, // creation time for this file system object
                                                                            newMetadata.Size), // file size or null for folders
                                                                        Revision = newMetadata.Revision, // file revision or null for folders
                                                                        StorageKey = newMetadata.StorageKey, // file storage key or null for folders
                                                                        MimeType = newMetadata.MimeType // never set on Windows
                                                                    }
                                                                },
                                                                newMetadata.Hash, // file MD5 hash or null for folder
                                                                DependencyDebugging);

                                                            // make sure to add change to SQL
                                                            newPathCreation.DoNotAddToSQLIndex = false;
                                                            currentChange.DoNotAddToSQLIndex = false;

                                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                                            {
                                                                alreadyVisitedRenames[newPathCreation.NewPath.Copy()] = newPathCreation.Metadata;
                                                            }

                                                            // merge the creation of the new FileChange for a pseudo Sync From creation event with the event source database, storing any error that occurs
                                                            CLError newPathCreationError = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(newPathCreation, currentChange)));
                                                            // if an error occurred merging the new FileChange with the event source database, then rethrow the error
                                                            if (newPathCreationError != null)
                                                            {
                                                                throw new AggregateException("Error merging new file creation change in response to not finding existing metadata at sync from rename old path", newPathCreationError.GrabExceptions());
                                                            }

                                                            // create the change in a new format to add to errors for reprocessing
                                                            PossiblyStreamableAndPossiblyChangedFileChangeWithError notFoundChange = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                                                /* changed */false, // technically this is a change, but it was manually added to SQL so effectively it's not different from the database
                                                                newPathCreation, // wrapped FileChange
                                                                null, // no stream since this is not a file upload
                                                                new Exception("Unable to find metadata for file. May have been a rename on a local file path that does not exist. Created new FileChange for creation at path: " + newPathCreation.NewPath.ToString())); // Error message for growl or logging

                                                            // if a change in error already exists for the current event id, then expand the array of errors at this event id with the created FileChange
                                                            if (changesInErrorList.ContainsKey(currentChange.EventId))
                                                            {
                                                                // store the previous array of errors
                                                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] previousErrors = changesInErrorList[currentChange.EventId];
                                                                // create a new array for error with a size expanded by one
                                                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] newErrors = new PossiblyStreamableAndPossiblyChangedFileChangeWithError[previousErrors.Length + 1];
                                                                // copy all the previous errors to the new array
                                                                previousErrors.CopyTo(newErrors, 0);
                                                                // put the new error as the last index of the new array
                                                                newErrors[previousErrors.Length] = notFoundChange;
                                                                // replace the value in the error mapping dictionary for the current event id with the expanded array
                                                                changesInErrorList[currentChange.EventId] = newErrors;
                                                            }
                                                            // else if a change in error does not already exist for the current event id, then add a new array with just the current created FileChange
                                                            else
                                                            {
                                                                // add a new array with just the created FileChange to the error mapping dictionary for the current event id
                                                                changesInErrorList.Add(currentChange.EventId,
                                                                    new PossiblyStreamableAndPossiblyChangedFileChangeWithError[]
                                                                    {
                                                                        notFoundChange
                                                                    });
                                                            }


                                                            // a file may still exist at the old path on disk, so to make sure the server looks the same, we must duplicate the file on the server
                                                            // TODO: all System.IO or disk access should be done through syncData ISyncDataObject interface object

                                                            if (currentChange.OldPath != null)
                                                            {
                                                                FileStream uploadStreamForDuplication = null;
                                                                try
                                                                {
                                                                    string oldPathString = currentChange.OldPath.ToString();
                                                                    bool fileExists;
                                                                    try
                                                                    {
                                                                        fileExists = File.Exists(oldPathString);

                                                                        if (fileExists)
                                                                        {
                                                                            uploadStreamForDuplication = new FileStream(oldPathString, FileMode.Open, FileAccess.Read, FileShare.Read);

                                                                            long duplicateSize = 0;
                                                                            byte[] duplicateHash;
                                                                            MD5 duplicateHasher = MD5.Create();

                                                                            try
                                                                            {
                                                                                byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                                                                int fileReadBytes;

                                                                                while ((fileReadBytes = uploadStreamForDuplication.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                                                                {
                                                                                    duplicateSize += fileReadBytes;
                                                                                    duplicateHasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                                                                }

                                                                                duplicateHasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);
                                                                                duplicateHash = duplicateHasher.Hash;
                                                                            }
                                                                            finally
                                                                            {
                                                                                try
                                                                                {
                                                                                    if (uploadStreamForDuplication != null)
                                                                                    {
                                                                                        uploadStreamForDuplication.Seek(0, SeekOrigin.Begin);
                                                                                    }
                                                                                }
                                                                                catch
                                                                                {
                                                                                }

                                                                                duplicateHasher.Dispose();
                                                                            }

                                                                            FileChange duplicateChange =
                                                                                new FileChange()
                                                                                {
                                                                                    Direction = SyncDirection.To,
                                                                                    Metadata = new FileMetadata()
                                                                                    {
                                                                                        HashableProperties = new FileMetadataHashableProperties(
                                                                                            /*isFolder*/ false,
                                                                                            File.GetLastAccessTimeUtc(oldPathString),
                                                                                            File.GetLastWriteTimeUtc(oldPathString),
                                                                                            duplicateSize)
                                                                                    },
                                                                                    NewPath = oldPathString,
                                                                                    Type = FileChangeType.Created
                                                                                };
                                                                            CLError setDuplicateHash = duplicateChange.SetMD5(duplicateHash);
                                                                            if (setDuplicateHash != null)
                                                                            {
                                                                                throw new AggregateException("Error setting MD5 on duplicateChange: " + setDuplicateHash.errorDescription, setDuplicateHash.GrabExceptions());
                                                                            }

                                                                            CLHttpRestStatus postDuplicateChangeStatus;
                                                                            JsonContracts.Event postDuplicateChangeResult;
                                                                            CLError postDuplicateChange = httpRestClient.PostFileChange(
                                                                                duplicateChange,
                                                                                HttpTimeoutMilliseconds,
                                                                                out postDuplicateChangeStatus,
                                                                                out postDuplicateChangeResult);
                                                                            if (postDuplicateChangeStatus != CLHttpRestStatus.Success)
                                                                            {
                                                                                throw new AggregateException("Error adding duplicate file on server: " + postDuplicateChange.errorDescription, postDuplicateChange.GrabExceptions());
                                                                            }

                                                                            if (postDuplicateChangeResult == null)
                                                                            {
                                                                                throw new NullReferenceException("Null event response adding duplicate file");
                                                                            }
                                                                            if (postDuplicateChangeResult.Header == null)
                                                                            {
                                                                                throw new NullReferenceException("Null event response header adding duplicate file");
                                                                            }
                                                                            if (string.IsNullOrEmpty(postDuplicateChangeResult.Header.Status))
                                                                            {
                                                                                throw new NullReferenceException("Null event response header status adding duplicate file");
                                                                            }
                                                                            if (postDuplicateChangeResult.Metadata == null)
                                                                            {
                                                                                throw new NullReferenceException("Null event response metadata adding duplicate file");
                                                                            }

                                                                            duplicateChange.Metadata.Revision = postDuplicateChangeResult.Metadata.Revision;
                                                                            duplicateChange.Metadata.StorageKey = postDuplicateChangeResult.Metadata.StorageKey;

                                                                            if ((new[]
                                                                                {
                                                                                    CLDefinitions.CLEventTypeAccepted,
                                                                                    CLDefinitions.CLEventTypeNoOperation,
                                                                                    CLDefinitions.CLEventTypeExists,
                                                                                    CLDefinitions.CLEventTypeDuplicate,
                                                                                    CLDefinitions.CLEventTypeUploading
                                                                                }).Contains(postDuplicateChangeResult.Header.Status))
                                                                            {
                                                                                CLError mergeDuplicateChange = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(duplicateChange)));
                                                                                if (mergeDuplicateChange != null)
                                                                                {
                                                                                    throw new AggregateException("Error writing duplicate file change to database after communication: " + mergeDuplicateChange.errorDescription, mergeDuplicateChange.GrabExceptions());
                                                                                }

                                                                                CLError completeDuplicateChange = syncData.completeSingleEvent(duplicateChange.EventId);
                                                                                if (completeDuplicateChange != null)
                                                                                {
                                                                                    throw new AggregateException("Error marking duplicate file change complete in database: " + completeDuplicateChange.errorDescription, completeDuplicateChange.GrabExceptions());
                                                                                }
                                                                            }
                                                                            else if ((new[]
                                                                                {
                                                                                    CLDefinitions.CLEventTypeUpload,
                                                                                    CLDefinitions.CLEventTypeUploading
                                                                                }).Contains(postDuplicateChangeResult.Header.Status))
                                                                            {
                                                                                FileChangeWithDependencies copyDuplicateChange;
                                                                                // don't need to set optional parameter (fileDownloadMoveLocker: removeDependencies.fileDownloadMoveLocker) because this if condition is on "upload" status thus not a file download
                                                                                CLError createCopyDuplicateChange = FileChangeWithDependencies.CreateAndInitialize(duplicateChange, /* initialDependencies */ null, out copyDuplicateChange);

                                                                                if (createCopyDuplicateChange != null)
                                                                                {
                                                                                    throw new AggregateException("Error copying duplicate file change for upload processing: " + createCopyDuplicateChange.errorDescription, createCopyDuplicateChange.GrabExceptions());
                                                                                }

                                                                                AddToIncompleteChanges(incompleteChangesList, copyDuplicateChange, uploadStreamForDuplication, /* different metadata since this is new */true);

                                                                                uploadStreamForDuplication = null; // prevents disposal on finally since the Stream will now be sent off for async processing
                                                                            }
                                                                            else
                                                                            {
                                                                                throw new InvalidOperationException("Event response header status invalid for duplicating file: " + postDuplicateChangeResult.Header.Status);
                                                                            }
                                                                        }
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        try
                                                                        {
                                                                            MessageEvents.FireNewEventMessage(
                                                                                "Error occurred handling conflict for a file rename: " + ex.Message,
                                                                                EventMessageLevel.Regular,
                                                                                /*Error*/ new GeneralErrorInfo(),
                                                                                syncBox.SyncBoxId,
                                                                                syncBox.CopiedSettings.DeviceId);
                                                                        }
                                                                        catch
                                                                        {
                                                                        }

                                                                        fileExists = false;
                                                                    }

                                                                    if (fileExists)
                                                                    {
                                                                        try
                                                                        {
                                                                            MessageEvents.FireNewEventMessage(
                                                                                "File rename conflict handled through duplication",
                                                                                EventMessageLevel.Minor,
                                                                                /*Error*/ null,
                                                                                syncBox.SyncBoxId,
                                                                                syncBox.CopiedSettings.DeviceId);
                                                                        }
                                                                        catch
                                                                        {
                                                                        }
                                                                    }
                                                                }
                                                                finally
                                                                {
                                                                    if (uploadStreamForDuplication != null)
                                                                    {
                                                                        try
                                                                        {
                                                                            uploadStreamForDuplication.Dispose();
                                                                        }
                                                                        catch
                                                                        {
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            // Existing metadata for a client event was not found for the current rename's previous path and revision
                                                            notFoundRename = true;
                                                        }
                                                        // else if the change was a rename event with missing metadata on the client and the change originated on the client (Sync To), then throw an error
                                                        else
                                                        {
                                                            throw new NullReferenceException("syncStateMetadata must be found by getMetadataByPathAndRevision");
                                                        }
                                                    }
                                                    // else if metadata was found by querying the event source database and the current event is a Sync From, then set the dictionary for the new path so metadata can be searched on subsequent renames
                                                    else if (currentChange.Direction == SyncDirection.From)
                                                    {
                                                        // declare a hierarchy for the old path of the rename
                                                        FilePathHierarchicalNode<FileMetadata> renameHierarchy;
                                                        // grab the hierarchy for the old path of the rename from already visited renames, storing any error that occurred
                                                        CLError grabHierarchyError = alreadyVisitedRenames.GrabHierarchyForPath(currentChange.OldPath, out renameHierarchy, suppressException: true);
                                                        // if there was an error grabbing the hierarchy, then rethrow the error
                                                        if (grabHierarchyError != null)
                                                        {
                                                            throw new AggregateException("Error grabbing renameHierarchy from alreadyVisitedRenames", grabHierarchyError.GrabExceptions());
                                                        }

                                                        if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                                        {
                                                            // if there was a hierarchy found at the old path for the rename, then apply a rename to the dictionary based on the current rename
                                                            if (renameHierarchy != null)
                                                            {
                                                                alreadyVisitedRenames.Rename(currentChange.OldPath, currentChange.NewPath.Copy());
                                                            }

                                                            // add the currently found metadata to the rename dictionary so it can be searched for subsequent renames
                                                            alreadyVisitedRenames[currentChange.NewPath.Copy()] = syncStateMetadata;
                                                        }
                                                    }

                                                    // create a fake FileChange just to store found metadata for the current event which will be used if event processing continues (notFoundRename == false)
                                                    fileChangeForOriginalMetadata = new FileChange()
                                                    {
                                                        NewPath = new FilePath("Not a valid file change", new FilePath(string.Empty)), // no idea why I set a fake NewPath, it should not be read later for anything
                                                        Metadata = syncStateMetadata
                                                    };
                                                }
                                            }
                                            // else if a previous matched change for the current event was found, then set the FileChange to use for previous metadata from the matched change
                                            else
                                            {
                                                fileChangeForOriginalMetadata = ((PossiblyStreamableFileChange)matchedChange).FileChange;

                                                // puts back the latest updated revision from the server (which would have been lost when the metadata instance would be replaced next below), update revision changer for difference
                                                if (fileChangeForOriginalMetadata.Metadata != null
                                                    && string.IsNullOrEmpty(fileChangeForOriginalMetadata.Metadata.Revision)
                                                    && currentChange.Metadata != null
                                                    && !string.IsNullOrEmpty(currentChange.Metadata.Revision))
                                                {
                                                    fileChangeForOriginalMetadata.Metadata.Revision = currentChange.Metadata.Revision;

                                                    fileChangeForOriginalMetadata.Metadata.RevisionChanger.FireRevisionChanged(fileChangeForOriginalMetadata.Metadata);
                                                }
                                            }

                                            // set the metadata of the current FileChange as the metadata from the previous change (or fake previous change if server was queried for new metadata)
                                            currentChange.Metadata = fileChangeForOriginalMetadata.Metadata;

                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                            {
                                                try
                                                {
                                                    if (!pathsToServerUid.ContainsKey(currentChange.NewPath))
                                                    {
                                                        FilePathHierarchicalNode<string> oldPathsToRename;
                                                        CLError getOldPathsError = pathsToServerUid.GrabHierarchyForPath(currentChange.OldPath, out oldPathsToRename, suppressException: true);

                                                        if (getOldPathsError == null
                                                            && oldPathsToRename != null)
                                                        {
                                                            pathsToServerUid.Rename(currentChange.OldPath, currentChange.NewPath.Copy());
                                                        }
                                                    }

                                                    serverUidsToPath[currentChange.Metadata.ServerUid] = currentChange.NewPath.ToString();
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            break;

                                        case FileChangeType.Created:
                                        case FileChangeType.Modified:
                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                            {
                                                alreadyVisitedRenames[currentChange.NewPath.Copy()] = currentChange.Metadata;

                                                try
                                                {
                                                    pathsToServerUid[currentChange.NewPath.Copy()] = currentChange.Metadata.ServerUid;
                                                    serverUidsToPath[currentChange.Metadata.ServerUid] = currentChange.NewPath.ToString();
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            break;

                                        case FileChangeType.Deleted:
                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                            {
                                                alreadyVisitedRenames.Remove(currentChange.NewPath);

                                                try
                                                {
                                                    pathsToServerUid.Remove(currentChange.NewPath);
                                                    serverUidsToPath.Remove(currentChange.Metadata.ServerUid);
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            break;
                                    }

                                    // if a previous FileChange could be found or previous metadata could be found on the client, then determine what to do to about the current event

                                    if (!notFoundRename)
                                    {
                                        // define a bool for whether the creation time is the same from previous metadata and the current event
                                        bool sameCreationTime = false;
                                        // define a bool for whether the last modified time is the same from the previous metadata and the current event
                                        bool sameLastTime = false;

                                        // define a bool for whether the event source database will need to be updated with a change (either because the new event is entirely new, or because the data has changed)
                                        bool metadataIsDifferent = (matchedChange == null) // different because the event is entirely new
                                            || ((PossiblyStreamableFileChange)matchedChange).FileChange.Type != currentChange.Type // different if types are different
                                            || ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.HashableProperties.IsFolder != currentChange.Metadata.HashableProperties.IsFolder // different if one is a folder and the other is not

                                            // different if the new location is different
                                            || !((((PossiblyStreamableFileChange)matchedChange).FileChange.NewPath == null && currentChange.NewPath == null)
                                                || (((PossiblyStreamableFileChange)matchedChange).FileChange.NewPath != null && currentChange.NewPath != null && FilePathComparer.Instance.Equals(((PossiblyStreamableFileChange)matchedChange).FileChange.NewPath, currentChange.NewPath)))

                                            // different if the old location is different (for renames)
                                            || !((((PossiblyStreamableFileChange)matchedChange).FileChange.OldPath == null && currentChange.OldPath == null)
                                                || (((PossiblyStreamableFileChange)matchedChange).FileChange.OldPath != null && currentChange.OldPath != null && FilePathComparer.Instance.Equals(((PossiblyStreamableFileChange)matchedChange).FileChange.OldPath, currentChange.OldPath)))

                                            // different if FileChanges have mismatching unique server ids
                                            || storeOldServerUid != currentChange.Metadata.ServerUid

                                            // different if the revision is different
                                            || storeOldRevision != currentChange.Metadata.Revision

                                            // different if the change is not a rename and any remaining metadata is different (rename is not checked for other metadata here because the remaining metadata properties were copied from previous metadata and are therefore known to match)
                                            || (currentChange.Type != FileChangeType.Renamed

                                                // possible non-rename metadata that could still be different:
                                                && (((PossiblyStreamableFileChange)matchedChange).FileChange.Direction != currentChange.Direction // different by direction of the change (being Sync To or Sync From)
                                                    || ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.HashableProperties.Size != currentChange.Metadata.HashableProperties.Size // different by file size
                                                    || ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.StorageKey != currentChange.Metadata.StorageKey // different by storage key

                                                    || !(sameCreationTime = Helpers.DateTimesWithinOneSecond(((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.HashableProperties.CreationTime, currentChange.Metadata.HashableProperties.CreationTime)) // different by creation time; compare within 1 second since communication drops subseconds
                                                    || !(sameLastTime = Helpers.DateTimesWithinOneSecond(((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.HashableProperties.LastTime, currentChange.Metadata.HashableProperties.LastTime)))); // different by last modified time; compare within 1 second since communication drops subseconds

                                        // if something is new or different for the current FileChange, then keep associated metadata revisions up to date
                                        if (metadataIsDifferent
                                            || (matchedChange != null && ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.ServerUid == null))
                                        {
                                            // update associated metadata with the current revision
                                            currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);
                                        }
                                        
                                        // declare FileChange for casting matched change as one with Dependencies
                                        FileChangeWithDependencies castMatchedChange;
                                        // move dependencies from any matched changed to the current change, and recreate the metadata properties to drop subseconds if they matched within a second
                                        if (metadataIsDifferent)
                                        {
                                            // if a matched change was found, then move dependencies and replace the metadata properties for times within a second
                                            if (matchedChange != null)
                                            {
                                                // assign and check if the cast matched change (one with dependencies) exists after trying to cast from the matched change, then copy dependencies
                                                if ((castMatchedChange = ((PossiblyStreamableFileChange)matchedChange).FileChange as FileChangeWithDependencies) != null
                                                    && castMatchedChange.DependenciesCount > 0) // also only copy dependencies if there are dependencies to copy
                                                {
                                                    // loop through dependencies of the cast matched change
                                                    foreach (FileChange matchedDependency in castMatchedChange.Dependencies)
                                                    {
                                                        // copy current dependency
                                                        currentChange.AddDependency(matchedDependency);
                                                    }
                                                    if (DependencyDebugging)
                                                    {
                                                        Helpers.CheckFileChangeDependenciesForDuplicates(currentChange);
                                                    }
                                                }

                                                // if at least one of the times matched within a second, then rewrite the metadata properties to keep the previous time(s)
                                                if (sameCreationTime || sameLastTime)
                                                {
                                                    currentChange.Metadata.HashableProperties = new FileMetadataHashableProperties(currentChange.Metadata.HashableProperties.IsFolder,
                                                        (sameLastTime ? ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.HashableProperties.LastTime : currentChange.Metadata.HashableProperties.LastTime),
                                                        (sameCreationTime ? ((PossiblyStreamableFileChange)matchedChange).FileChange.Metadata.HashableProperties.CreationTime : currentChange.Metadata.HashableProperties.CreationTime),
                                                        currentChange.Metadata.HashableProperties.Size);
                                                }
                                            }
                                        }
                                        // else if nothing is new or different for the current FileChange, then use the matched change instead of the current change (reassign), making sure it is a FileChange with dependencies
                                        else
                                        {
                                            // assign and check if the cast matched change (one with dependencies) exists after trying to cast from the matched change, then set the current change as the cast matched change
                                            if ((castMatchedChange = ((PossiblyStreamableFileChange)matchedChange).FileChange as FileChangeWithDependencies) != null)
                                            {
                                                currentChange = castMatchedChange;
                                            }
                                            // else if the matched changed failed to be cast as one with dependencies, then create a new FileChange with dependencies from the matched changed and set it as the current change, rethrowing any errors that occur
                                            else
                                            {
                                                CLError convertMatchedChangeError = FileChangeWithDependencies.CreateAndInitialize(
                                                    ((PossiblyStreamableFileChange)matchedChange).FileChange,
                                                    /* initialDependencies */ null,
                                                    out currentChange,
                                                    fileDownloadMoveLocker: ((PossiblyStreamableFileChange)matchedChange).FileChange.fileDownloadMoveLocker);
                                                if (convertMatchedChangeError != null)
                                                {
                                                    throw new AggregateException("Error converting matchedChange to FileChangeWithDependencies", convertMatchedChangeError.GrabExceptions());
                                                }
                                            }
                                        }

                                        //ZW: this is where the Processing of the Event Header (Return ) Status is processed including conflict rename handling 
                                        //ZW: If Changes are triggered by muliple actions at the same time, async  processes may try to operate on the 
                                        //ZW: the same file before the previous change is complete. In this case there are dependencies, which arise for ordering the changes
                                        //ZW: Change 2 is Dependent on change 1. 
                                        // switch on direction of change first (Sync From versus Sync To)
                                        switch (currentChange.Direction)
                                        {
                                            case SyncDirection.From:
                                                // Sync From only requires adding the current change to the incomplete changes list (all Sync From changes are processed)
                                                AddToIncompleteChanges(incompleteChangesList, currentChange, currentStream, metadataIsDifferent);
                                                break;

                                            case SyncDirection.To:
                                                // Sync To has a status returned from the server which describes how to process the event, so switch on it
                                                switch (currentEvent.Header.Status)
                                                {
                                                    // cases that trigger a file upload
                                                    case CLDefinitions.CLEventTypeUpload:
                                                    case CLDefinitions.CLEventTypeUploading:
                                                        // Todo: need optimization to prevent uploading two identical files from the same client, the first of each storage key that gets uploaded will autocomplete all other events with the same storage key

                                                        // Sync To event did not complete with communication since it still requires a file upload so add it to incomplete changes list
                                                        AddToIncompleteChanges(incompleteChangesList, currentChange, currentStream, metadataIsDifferent);
                                                        break;

                                                    // group not found (a possible error case) with the immediately completed cases since "not_found" for a deletion requires no more action and can be presumed completed successfully
                                                    case CLDefinitions.CLEventTypeAccepted:
                                                    case CLDefinitions.CLEventTypeExists:
                                                    case CLDefinitions.CLEventTypeDuplicate:
                                                    case CLDefinitions.CLEventTypeNotFound:
                                                    case CLDefinitions.CLEventTypeDownload:
                                                    case CLDefinitions.CLEventTypeAlreadyDeleted:
                                                    case CLDefinitions.CLEventTypeNoOperation:
                                                        // if the file system object was not found on the server and was not marked for deletion, then convert the operation to a creation at the new path so it can still be processed on a future sync
                                                        if (currentEvent.Header.Status == CLDefinitions.CLEventTypeNotFound
                                                            && currentChange.Type != FileChangeType.Deleted)
                                                        {
                                                            // clear unique server id
                                                            currentChange.Metadata.ServerUid = null;
                                                            // convert change to creation
                                                            currentChange.Type = FileChangeType.Created;
                                                            // remove old path since creation does not have one
                                                            currentChange.OldPath = null;
                                                            // clear the server UID since it should be unique for a new creation
                                                            currentChange.Metadata.ServerUid = null;
                                                            // clear revision since new file system objects never need one
                                                            currentChange.Metadata.Revision = null;
                                                            // notify associated metadata with the change to the revision
                                                            currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);
                                                            // clear storage key since the server may need to assign a new one when creation is sent
                                                            currentChange.Metadata.StorageKey = null;
                                                            // clear mime type, which is not set on Windows anyways
                                                            currentChange.Metadata.MimeType = null;

                                                            // wrap the modified current change so it can be added to the changes in error list
                                                            PossiblyStreamableAndPossiblyChangedFileChangeWithError notFoundChange = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                                                true, // type was converted so database needs to be updated
                                                                currentChange, // the modified current change
                                                                currentStream, // any stream that needs to be disposed for the current change

                                                                // calculate dynamic exception message for the "not_found" status
                                                                new Exception(CLDefinitions.CLEventTypeNotFound + " " +
                                                                    (currentEvent.Header.Action ?? currentEvent.Action) +
                                                                    " " + currentChange.EventId + " " + currentChange.NewPath.ToString()));

                                                            // if the changes in error list already contains a mapping from the current change's event id to an array of errors, then expand the array and add the modified current change
                                                            if (changesInErrorList.ContainsKey(currentChange.EventId))
                                                            {
                                                                // store the previous array of errors
                                                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] previousErrors = changesInErrorList[currentChange.EventId];
                                                                // create a new array for error with a size expanded by one
                                                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] newErrors = new PossiblyStreamableAndPossiblyChangedFileChangeWithError[previousErrors.Length + 1];
                                                                // copy all the previous errors to the new array
                                                                previousErrors.CopyTo(newErrors, 0);
                                                                // put the modified current change as the last index of the new array
                                                                newErrors[previousErrors.Length] = notFoundChange;
                                                                // replace the value in the error mapping dictionary for the current event id with the expanded array
                                                                changesInErrorList[currentChange.EventId] = newErrors;
                                                            }
                                                            // else if a change in error does not already exist for the current event id, then add a new array with just the modified current change
                                                            else
                                                            {
                                                                // add a new array with just the modified current change to the error mapping dictionary for the current event id
                                                                changesInErrorList.Add(currentChange.EventId,
                                                                    new PossiblyStreamableAndPossiblyChangedFileChangeWithError[]
                                                                    {
                                                                        notFoundChange
                                                                    });
                                                            }
                                                        }
                                                        // else if the status was an accepted status or it was a "not_found" for a deletion event, then add the current change to the list of completed changes
                                                        else
                                                        {
                                                            // wrap the current change so it can be added to the list of completed changes
                                                            PossiblyChangedFileChange addCompletedChange = new PossiblyChangedFileChange(resultOrder++,
                                                                metadataIsDifferent, // whether the event source database needs to be updated
                                                                currentChange); // the current change

                                                            // if there is a Stream for the current change, then add it to the list of completed streams and dispose it
                                                            if (currentStream != null)
                                                            {
                                                                // add current stream to list of completed streams
                                                                completedStreams.Add(currentStream);

                                                                // try/catch to dispose the current stream, failing silently
                                                                try
                                                                {
                                                                    currentStream.Dispose();
                                                                }
                                                                catch
                                                                {
                                                                }
                                                            }

                                                            // if the mapping of event ids to arrays of completed changes contains the current change's event id, then expand the array and add the current change
                                                            if (completedChangesList.ContainsKey(currentChange.EventId))
                                                            {
                                                                // store the previous array of completed changes
                                                                PossiblyChangedFileChange[] previousCompleted = completedChangesList[currentChange.EventId];
                                                                // create a new array for completions with a size expanded by one
                                                                PossiblyChangedFileChange[] newCompleted = new PossiblyChangedFileChange[previousCompleted.Length + 1];
                                                                // copy all the previous completed changes to the new array
                                                                previousCompleted.CopyTo(newCompleted, 0);
                                                                // put the completed change at the last index of the new array
                                                                newCompleted[previousCompleted.Length] = addCompletedChange;
                                                                // replace the value in the completed changes mapping dictionary for the current event id with the expanded array
                                                                completedChangesList[currentChange.EventId] = newCompleted;
                                                            }
                                                            // else if the mapping of event ids to arrays of completed changes does not contain the current change's event id, then add a new array with just the current change
                                                            else
                                                            {
                                                                // add a new array with just the completed change to the completed changes mapping dictionary for the current event id
                                                                completedChangesList.Add(currentChange.EventId,
                                                                    new PossiblyChangedFileChange[]
                                                                    {
                                                                        addCompletedChange
                                                                    });
                                                            }
                                                        }
                                                        break;
                                                    //ZW: File Rename scheme for conflicting file names 
                                                    // case that triggers moving the local file to a new location in the same directory and processing it as a new file creation (the latest version of the file at the original location will likely be downloaded from a Sync From event)
                                                    case CLDefinitions.CLEventTypeConflict:
                                                        // store original path for current change (a new path with "CONFLICT" appended to the name will be calculated)
                                                        FilePath originalConflictPath = currentChange.NewPath;

                                                        // define an exception for storing any error that may occur while processing the conflict change 
                                                        Exception innerExceptionAppend = null;

                                                        // try/catch process the conflict change, on catch reassign the previous revision so the change won't succeed the next sync iteration and replace the conflicted file, also store the error
                                                        try
                                                        {
                                                            // TODO: The directory file enumeration in this function should be run via the event source (to remove the ties to the file system here)
                                                            // create a function to find the next available name for a conflicted file, will run again if the ending number can't be incremeneted and new counter has to be added, i.e. "Z (2147483647)" will need a " (2)" added
                                                            Func<FilePath, string, string, KeyValuePair<bool, string>> getNextName = (innerOriginalConflictPath, extension, mainName) =>
                                                            {
                                                                // if the main name of the file (before extension) has a positive length and ends with a right paranthesis, then try to process the ending of the name as a number surrounded by parenthesis so we can remove it to find the real name (without the incrementor)
                                                                if (mainName.Length > 0
                                                                    && mainName[mainName.Length - 1] == ')')
                                                                {
                                                                    // define a count for the number of numeric digits between the terminal parentheses in the file
                                                                    int numDigits = 0;
                                                                    // continue while there is still at least 3 non-digit characters left the name (enclosing parentheses plus a space before them)
                                                                    // and also only continue while the number of digits is within the count parsable to a 32-bit integer (10 digits)
                                                                    while (mainName.Length > numDigits + 2
                                                                        && numDigits < 11)
                                                                    {
                                                                        // if the next character to check is a digit, then increment the number of digits found
                                                                        if (char.IsDigit(mainName[mainName.Length - 2 - numDigits]))
                                                                        {
                                                                            numDigits++;
                                                                        }
                                                                        // else if at least one digit has been found and the characters before the digits are a space followed by a right parenthesis, then try to parse the digits as an integer to find if the ending can be chopped off
                                                                        else if (numDigits > 0
                                                                            && mainName[mainName.Length - 2 - numDigits] == '('
                                                                            && mainName[mainName.Length - 3 - numDigits] == ' ')
                                                                        {
                                                                            // take the substring for just the digits found
                                                                            string numPortion = mainName.Substring(mainName.Length - 1 - numDigits, numDigits);
                                                                            // declare an int for the parsed number
                                                                            int numPortionParsed;
                                                                            // try to parse the found digits into the number and if successful and the number is not the max value for a 32-bit int (thus cannot be further incremented), then chop off the digits to set the actual main name of the file
                                                                            if (int.TryParse(numPortion, out numPortionParsed)
                                                                                && numPortionParsed != int.MaxValue)
                                                                            {
                                                                                mainName = mainName.Substring(0, mainName.Length - 3 - numDigits);
                                                                            }

                                                                            // done checking (either digits were chopped off or the end was not parsable)
                                                                            break;
                                                                        }
                                                                        else
                                                                        {
                                                                            // done checking (either no digits were found or they weren't preceded with a space and left paranthesis
                                                                            break;
                                                                        }
                                                                    }
                                                                }

                                                                // declare an int for the highest number found between parentheses after the main name of the file (before extension and without existing incrementor)
                                                                int highestNumFound = 0;
                                                                // TODO: this is the part which needs to be performed via the event source since it access the file system
                                                                // loop through all sibling files within the same parent directory as the current file
                                                                foreach (string currentSibling in Directory.EnumerateFiles(innerOriginalConflictPath.Parent.ToString()))
                                                                {
                                                                    // if the sibling has the same exact file name (including extension) then increment the number found to 1 if it hasn't already been incremented
                                                                    if (currentSibling.Equals(mainName + extension, StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        // if number has not already been incremented, then increment to 1
                                                                        if (highestNumFound == 0)
                                                                        {
                                                                            highestNumFound = 1;
                                                                        }
                                                                    }
                                                                    // else if the sibling does not have the same exact file name but is named based on the name with an incrementor "Z (XXX).YYY",
                                                                    // then try to pull out the number value of the incrementor to use and use it as the highest number if greatest found so far
                                                                    else if (currentSibling.StartsWith(mainName + " (", StringComparison.InvariantCultureIgnoreCase) // "Z (..."
                                                                        && currentSibling.EndsWith(")" + extension, StringComparison.InvariantCultureIgnoreCase)) // "...).YYY"
                                                                    {
                                                                        // pull out the portion of the name between the parenteses
                                                                        string siblingNumberPortion = currentSibling.Substring(mainName.Length + 2,
                                                                            currentSibling.Length - mainName.Length - extension.Length - 3);
                                                                        // declare an int for the parsed number
                                                                        int siblingNumberParsed;
                                                                        // try to parse the portion of the name pulled out as an int and if successful, then see if it's the highest number found so far to set
                                                                        if (int.TryParse(siblingNumberPortion, out siblingNumberParsed))
                                                                        {
                                                                            if (siblingNumberParsed > highestNumFound)
                                                                            {
                                                                                highestNumFound = siblingNumberParsed;
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                // return with the incremented name (or stay at the max int value and return with a true as well if the highest number is at int.MaxValue and a new incrementor needs to be added)
                                                                return new KeyValuePair<bool, string>(highestNumFound == int.MaxValue,
                                                                    mainName + " (" + (highestNumFound == int.MaxValue ? highestNumFound : highestNumFound + 1).ToString() + ")");
                                                            };

                                                            // declare a string for the main name portion of the file name (before the last extension)
                                                            string findMainName;
                                                            // declare a string for the last extension of the file name, if any
                                                            string findExtension;

                                                            // define the index of the final period in the file name (marks the start of the extension)
                                                            int extensionIndex = originalConflictPath.Name.LastIndexOf('.');
                                                            // if an extension was not found, then set the extension as an empty string, and the entire file name as the main name
                                                            if (extensionIndex == -1)
                                                            {
                                                                findExtension = string.Empty;
                                                                findMainName = originalConflictPath.Name;
                                                            }
                                                            // else if an extension was found, then set the extension as the extesion part of the file name, and the rest as the main name
                                                            else
                                                            {
                                                                findExtension = currentChange.NewPath.Name.Substring(extensionIndex);
                                                                findMainName = currentChange.NewPath.Name.Substring(0, extensionIndex);
                                                            }

                                                            // define a portion of the name which needs to be added to describe the conflict state dynamic to the current friendly-named device
                                                            string deviceAppend = " CONFLICT " + syncBox.CopiedSettings.FriendlyName;
                                                            // declare a string to store the main name of the conflict file to create
                                                            string finalizedMainName;
                                                            // declare a FilePath to store the full path to the conflict file to create
                                                            FilePath finalizedNewPath;

                                                            // TODO: Need to check for file existance of the conflict path via the event source to remove dependence on the file system
                                                            // if the main name of the current file in conflict already contains the "CONFLICT" portion in the name
                                                            // or if a file already exists at the new conflict path, then need to run function to find the next available name
                                                            //
                                                            // this process also sets the full path to use for the new conflict FileChange (either in the second condition or in the code which may run)
                                                            if (findMainName.IndexOf(deviceAppend, 0, StringComparison.InvariantCultureIgnoreCase) != -1
                                                                || System.IO.File.Exists((finalizedNewPath = new FilePath((finalizedMainName = findMainName + deviceAppend) + findExtension, originalConflictPath.Parent)).ToString()))
                                                            {
                                                                // define a starting point for the name search iteration
                                                                KeyValuePair<bool, string> mainNameIteration = new KeyValuePair<bool, string>(
                                                                    true, // needs to iterate the first time
                                                                    findMainName); // starting name to search

                                                                // continue while the input or return value has the flag to continue
                                                                while (mainNameIteration.Key)
                                                                {
                                                                    // set the latest calculated name by the function which finds the next available name to use, if true is returned for the return Key, then a deeper name has to be searched again
                                                                    mainNameIteration = getNextName(originalConflictPath, findExtension, mainNameIteration.Value);
                                                                }

                                                                // take out the final calculated, available name for the conflict
                                                                finalizedMainName = mainNameIteration.Value;

                                                                // build the full path for the calculated conflict name
                                                                finalizedNewPath = new FilePath(finalizedMainName + findExtension, originalConflictPath.Parent);
                                                            }

                                                            // store the current type of the change to reset upon error
                                                            FileChangeType storeType = currentChange.Type;
                                                            // store the current path of the change to reset upon error
                                                            FilePath storePath = currentChange.NewPath;
                                                            // store the current server "uid" of the change to reset upon error
                                                            string storeServerUid = currentChange.Metadata.ServerUid;
                                                            // store the current server parent "uid" of the change to reset upon error
                                                            string storeServerParentUid = currentChange.Metadata.ParentFolderServerUid;
                                                            // store the current revision of the change to reset upon error
                                                            string storeRevision = currentChange.Metadata.Revision;

                                                            // try/catch to create a creation FileChange to process the conflict file to rename the file locally and add an event to upload it, on catch revert the modified event path and type
                                                            try
                                                            {
                                                                FileChange oldPathDownload = null;

                                                                try
                                                                {
                                                                    FileMetadata oldPathMetadata;
                                                                    CLError oldPathMetadataError = syncData.getMetadataByPathAndRevision(
                                                                        originalConflictPath.ToString(),
                                                                        /* revision */ null,
                                                                        out oldPathMetadata);

                                                                    if (oldPathMetadataError == null
                                                                        && oldPathMetadata != null)
                                                                    {
                                                                        if (string.IsNullOrEmpty(oldPathMetadata.Revision))
                                                                        {
                                                                            JsonContracts.Metadata oldPathMetadataRevision;
                                                                            CLHttpRestStatus oldPathMetadataRevisionStatus;
                                                                            CLError oldPathMetadataRevisionError = httpRestClient.GetMetadata(
                                                                                originalConflictPath,
                                                                                /* isFolder */ false,
                                                                                HttpTimeoutMilliseconds,
                                                                                out oldPathMetadataRevisionStatus,
                                                                                out oldPathMetadataRevision);

                                                                            if (oldPathMetadataRevisionStatus == CLHttpRestStatus.Success
                                                                                && oldPathMetadataRevision != null
                                                                                && oldPathMetadataRevision.Deleted != true)
                                                                            {
                                                                                oldPathMetadata.Revision = oldPathMetadataRevision.Revision;
                                                                            }
                                                                        }

                                                                        if (!string.IsNullOrEmpty(oldPathMetadata.Revision))
                                                                        {
                                                                            FileChangeWithDependencies oldPathDownloadNoDependencies;
                                                                            CLError oldPathDownloadNoDependenciesError = FileChangeWithDependencies.CreateAndInitialize(
                                                                                new FileChange(DelayCompletedLocker: null, fileDownloadMoveLocker: new object())
                                                                                {
                                                                                    Direction = SyncDirection.From,
                                                                                    Metadata = oldPathMetadata,
                                                                                    NewPath = originalConflictPath.Copy(),
                                                                                    Type = FileChangeType.Created
                                                                                }, /* initialDependencies */ null,
                                                                                out oldPathDownloadNoDependencies,
                                                                                fileDownloadMoveLocker: new object()); // Sync From file create is always a file download, so create its download locker

                                                                            if (oldPathDownloadNoDependenciesError == null)
                                                                            {
                                                                                CLError oldPathDownloadToSqlError = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(oldPathDownloadNoDependencies)));
                                                                                if (oldPathDownloadToSqlError == null)
                                                                                {
                                                                                    oldPathDownload = oldPathDownloadNoDependencies;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                }

                                                                // change conflict event into a creation for a new upload
                                                                currentChange.Type = FileChangeType.Created;
                                                                // change conflict path to the new conflict path
                                                                currentChange.NewPath = finalizedNewPath;
                                                                // <David fix for a file creation with an old path> file creations should not have an old path (only for renames)
                                                                currentChange.OldPath = null;
                                                                // clear the server UID since it should be unique for a new creation
                                                                currentChange.Metadata.ServerUid = null;
                                                                // clear the revision (since it will be a new file)
                                                                currentChange.Metadata.Revision = null;
                                                                // update associated Metadatas with the revision change
                                                                currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);
                                                                // clear the storage key (since it will be a new file)
                                                                currentChange.Metadata.StorageKey = null;

                                                                // make sure to add change to SQL
                                                                currentChange.DoNotAddToSQLIndex = false;

                                                                // declare a new FileChange with dependencies for moving the conflict file to the new conflict path
                                                                FileChangeWithDependencies reparentConflict;
                                                                // initialize and set the rename change with a new FileChange for moving the conflict file to the new conflict path and include the conflict creation change as a dependency, storing any error that occurs
                                                                CLError reparentCreateError = FileChangeWithDependencies.CreateAndInitialize(new FileChange()
                                                                    {
                                                                        Direction = SyncDirection.From, // rename the file locally (Sync From)
                                                                        Metadata = new FileMetadata()
                                                                        {
                                                                            HashableProperties = currentChange.Metadata.HashableProperties, // copy metadata from the conflicted file
                                                                            ServerUid = storeServerUid,
                                                                            ParentFolderServerUid = storeServerParentUid,
                                                                            Revision = storeRevision
                                                                        },
                                                                        NewPath = currentChange.NewPath, // use the new conflict path as the rename destination
                                                                        OldPath = originalConflictPath, // use the location of the current conflicted file as move from location
                                                                        Type = FileChangeType.Renamed // this operation is a move
                                                                    },
                                                                    (oldPathDownload == null
                                                                        ? Helpers.EnumerateSingleItem(currentChange)  // add the creation at the new location as a dependency to the rename
                                                                        : (IEnumerable<FileChange>)new[] { oldPathDownload, currentChange }), // in addition to the create at the new location in the line above, also download the original copy of the file to the old path
                                                                    out reparentConflict); // output the new rename change
                                                                // if an error occurred creating the FileChange for the rename operation, rethrow the error
                                                                if (reparentCreateError != null)
                                                                {
                                                                    throw new AggregateException("Error creating reparentConflict", reparentCreateError.GrabExceptions());
                                                                }
                                                                else if (DependencyDebugging)
                                                                {
                                                                    Helpers.CheckFileChangeDependenciesForDuplicates(reparentConflict);
                                                                }

                                                                // if the current change already exists in the database then it may have been in the initial changes to communicate,
                                                                // so the current change will need to be added to a list which will be checked to make sure all changes to communicate were processed
                                                                if (currentChange.EventId > 0)
                                                                {
                                                                    // if the current change had a stream (which it should for file uploads),
                                                                    // then it needs to be disposed because it is now a dependency which needs to go through sync to recommunicate for a new storage key
                                                                    if (currentStream != null)
                                                                    {
                                                                        try
                                                                        {
                                                                            currentStream.Dispose();
                                                                        }
                                                                        catch
                                                                        {
                                                                        }
                                                                    }

                                                                    // wrap the current event so it can be added to the converted dependencies list
                                                                    PossiblyStreamableFileChange dependencyHidden = new PossiblyStreamableFileChange(
                                                                        currentChange, // current event
                                                                        currentStream, // current stream, or null if it had not existed
                                                                        true); // this change will be added to errors anyways, so invalid nullability of the Stream is not important for reprocessing

                                                                    // declare array for grabbing converted dependencies for current event id
                                                                    PossiblyStreamableFileChange[] currentEventIdDependencies;
                                                                    // try to grab the converted dependencies for the current event id and if successful, then the existing array will need to be expanded with the current event
                                                                    if (changesConvertedToDependencies.TryGetValue(currentChange.EventId, out currentEventIdDependencies))
                                                                    {
                                                                        // create an array with length one larger than the previous array
                                                                        PossiblyStreamableFileChange[] previousDependenciesPlusOne = new PossiblyStreamableFileChange[currentEventIdDependencies.Length + 1];
                                                                        // copy everything from the previous array into the expanded array
                                                                        Array.Copy(currentEventIdDependencies, // previous array
                                                                            previousDependenciesPlusOne, // expanded array
                                                                            currentEventIdDependencies.Length); // grab everything from the previous array
                                                                        // set the added slot in the array as the current event
                                                                        previousDependenciesPlusOne[currentEventIdDependencies.Length] = dependencyHidden;
                                                                        // set the array for the current event id as the expanded array
                                                                        changesConvertedToDependencies[currentChange.EventId] = previousDependenciesPlusOne;
                                                                    }
                                                                    // else if converted dependencies could not be grabbed for the current event id, then add a new array with the current event
                                                                    else
                                                                    {
                                                                        changesConvertedToDependencies.Add(currentChange.EventId, new[]
                                                                        {
                                                                            dependencyHidden
                                                                        });
                                                                    }
                                                                }

                                                                // remove the previous conflict change from the event source database, storing any error that occurred
                                                                CLError removalOfPreviousChange = syncData.mergeToSql(new[] { new FileChangeMerge(null, currentChange) });
                                                                // if an error occurred removing the previous conflict change, then rethrow the error
                                                                if (removalOfPreviousChange != null)
                                                                {
                                                                    throw new AggregateException("Error removing the existing FileChange for a conflict", removalOfPreviousChange.GrabExceptions());
                                                                }

                                                                // add the local rename change to the event source database, storing any error that occurred
                                                                CLError addRenameToConflictPath = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(reparentConflict)));
                                                                // if there was an error adding the local rename change, then readd the reverted conflict change to the event source database and rethrow the error
                                                                if (addRenameToConflictPath != null)
                                                                {
                                                                    currentChange.Type = storeType;
                                                                    currentChange.NewPath = storePath;
                                                                    currentChange.Metadata.ServerUid = storeServerUid;
                                                                    currentChange.Metadata.ParentFolderServerUid = storeServerParentUid;
                                                                    currentChange.Metadata.Revision = storeRevision;
                                                                    syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(currentChange)));

                                                                    throw new AggregateException("Error adding a rename FileChange for a conflicted file", addRenameToConflictPath.GrabExceptions());
                                                                }

                                                                // store the current event id in case it needs to be reverted
                                                                long storeEventId = currentChange.EventId;
                                                                // wipe the event id so a new event can be added
                                                                currentChange.EventId = 0;

                                                                // write the original conflict as a file creation to upload to the server to the event source database, storing any error that occurred
                                                                CLError addModifiedConflictAsCreate = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(currentChange)));

                                                                // if an error occurred writing the original conflict as a file creation, then remove the added rename change and readd the reverted conflict change to the event source database and rethrow the error
                                                                if (addModifiedConflictAsCreate != null)
                                                                {
                                                                    syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(null, reparentConflict)));
                                                                    currentChange.EventId = storeEventId;
                                                                    currentChange.Type = storeType;
                                                                    currentChange.NewPath = storePath;
                                                                    currentChange.Metadata.ServerUid = storeServerUid;
                                                                    currentChange.Metadata.ParentFolderServerUid = storeServerParentUid;
                                                                    currentChange.Metadata.Revision = storeRevision;

                                                                    syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(currentChange)));

                                                                    throw new AggregateException("Error adding a new creation FileChange at the new conflict path", addModifiedConflictAsCreate.GrabExceptions());
                                                                }

                                                                // store the succesfully created rename change with the modified conflict change as the current change to process
                                                                currentChange = reparentConflict;
                                                                // since we updated the event source database already for the changes, treat the changes as not different
                                                                metadataIsDifferent = false;
                                                            }
                                                            catch
                                                            {
                                                                // revert the changes to the current conflict and rethrow the error

                                                                currentChange.Type = storeType;
                                                                currentChange.NewPath = storePath;
                                                                currentChange.Metadata.ServerUid = storeServerUid;
                                                                currentChange.Metadata.ParentFolderServerUid = storeServerParentUid;
                                                                currentChange.Metadata.Revision = storeRevision;
                                                                throw;
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            bool reversedRevision = currentChange.Metadata.Revision != previousRevisionOnConflictException;
                                                            // revert the revision to the value before communication so it will get a conflict the next iteration through Sync to attempt to handle it again
                                                            currentChange.Metadata.Revision = previousRevisionOnConflictException;
                                                            currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);
                                                            // update associated Metadatas with the change to the revision
                                                            currentChange.Metadata.RevisionChanger.FireRevisionChanged(currentChange.Metadata);

                                                            // store the exception that occurred processing the conflict
                                                            innerExceptionAppend = new Exception("Error creating local rename to apply for conflict", ex);
                                                        }

                                                        // wrap the conflict change so it can be added to the changes in error
                                                        PossiblyStreamableAndPossiblyChangedFileChangeWithError addErrorChange = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                                            metadataIsDifferent, // whether the event source database needs to be updated for the conflict change
                                                            currentChange, // the conflict change itself
                                                            currentStream, // any stream belonging to the conflict change

                                                            // dynamic conflict message for the exception from the conflict state, the original change action, and the conflict id and path; also add any inner exception from processing failures
                                                            new Exception(CLDefinitions.CLEventTypeConflict + " " +
                                                                (currentEvent.Header.Action ?? currentEvent.Action) +
                                                                " " + currentChange.EventId + " " + originalConflictPath.ToString(),
                                                                innerExceptionAppend));

                                                        // if a change in error already exists for the current event id, then expand the array of errors at this event id with the created FileChange
                                                        if (changesInErrorList.ContainsKey(currentChange.EventId))
                                                        {
                                                            // store the previous array of errors
                                                            PossiblyStreamableAndPossiblyChangedFileChangeWithError[] previousErrors = changesInErrorList[currentChange.EventId];
                                                            // create a new array for error with a size expanded by one
                                                            PossiblyStreamableAndPossiblyChangedFileChangeWithError[] newErrors = new PossiblyStreamableAndPossiblyChangedFileChangeWithError[previousErrors.Length + 1];
                                                            // copy all the previous errors to the new array
                                                            previousErrors.CopyTo(newErrors, 0);
                                                            // put the new error as the last index of the new array
                                                            newErrors[previousErrors.Length] = addErrorChange;
                                                            // replace the value in the error mapping dictionary for the current event id with the expanded array
                                                            changesInErrorList[currentChange.EventId] = newErrors;
                                                        }
                                                        // else if a change in error does not already exist for the current event id, then add a new array with just the current created FileChange
                                                        else
                                                        {
                                                            // add a new array with just the created FileChange to the error mapping dictionary for the current event id
                                                            changesInErrorList.Add(currentChange.EventId,
                                                                new PossiblyStreamableAndPossiblyChangedFileChangeWithError[]
                                                                {
                                                                    addErrorChange
                                                                });
                                                        }
                                                        break;

                                                    // "error"
                                                    case CLDefinitions.RESTResponseStatusFailed:
                                                        throw new Exception("Error response: " +
                                                            (currentEvent.Metadata == null
                                                                ? "{null Metadata}"
                                                                : (currentEvent.Metadata.ErrorMessage == null
                                                                    ? "{Metadata has null ErrorMessage}"
                                                                    : (currentEvent.Metadata.ErrorMessage.Length == 0
                                                                        ? "{Metadata error message array is empty}"
                                                                        : string.Join("; ", currentEvent.Metadata.ErrorMessage)))));
                                                                //: string.IsNullOrEmpty(currentEvent.Metadata.ErrorMessage)
                                                                //    ? "{Metadata has null ErrorMessage}"
                                                                //    : currentEvent.Metadata.ErrorMessage));

                                                    // server sent a new type of message that is not yet recognized
                                                    default:
                                                        throw new ArgumentException("Unknown SyncHeader Status: " + currentEvent.Header.Status);
                                                }
                                                break;

                                            // event is neither Sync From nor Sync To, do not know how to process
                                            default:
                                                throw new ArgumentException("Unknown SyncDirection in currentChange: " + currentChange.Direction.ToString());
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // wrap the current FileChange so it can be added to the changes in error
                                    PossiblyStreamableAndPossiblyChangedFileChangeWithError addErrorChange = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                        currentChange != null, // update database if a change exists
                                        currentChange // the current change in error
                                            ?? (toCommunicate.First(currentToCommunicate =>
                                                (currentEvent.EventId != null || currentEvent.Header.EventId != null)
                                                    && currentToCommunicate.FileChange.EventId == (long)(currentEvent.EventId ?? currentEvent.Header.EventId))).FileChange,
                                        currentStream, // any stream for the current change
                                        ex); // the error itself

                                    // if a change in error already exists for the current event id, then expand the array of errors at this event id with the created FileChange
                                    if (changesInErrorList.ContainsKey(currentChange == null ? 0 : currentChange.EventId))
                                    {
                                        // store the previous array of errors
                                        PossiblyStreamableAndPossiblyChangedFileChangeWithError[] previousErrors = changesInErrorList[currentChange == null ? 0 : currentChange.EventId];
                                        // create a new array for error with a size expanded by one
                                        PossiblyStreamableAndPossiblyChangedFileChangeWithError[] newErrors = new PossiblyStreamableAndPossiblyChangedFileChangeWithError[previousErrors.Length + 1];
                                        // copy all the previous errors to the new array
                                        previousErrors.CopyTo(newErrors, 0);
                                        // put the new error as the last index of the new array
                                        newErrors[previousErrors.Length] = addErrorChange;
                                        // replace the value in the error mapping dictionary for the current event id with the expanded array
                                        changesInErrorList[currentChange.EventId] = newErrors;
                                    }
                                    // else if a change in error does not already exist for the current event id, then add a new array with just the current created FileChange
                                    else
                                    {
                                        // add a new array with just the created FileChange to the error mapping dictionary for the current event id
                                        changesInErrorList.Add(currentChange == null ? 0 : currentChange.EventId,
                                            new PossiblyStreamableAndPossiblyChangedFileChangeWithError[]
                                            {
                                                addErrorChange
                                            });
                                    }
                                }
                            }
                        }
                    }

                    // clean up any events which were not included in the responses from the server (needs streams disposed and events to be reprocessed)

                    // loop through FileChanges in list to communicate
                    foreach (PossiblyStreamableFileChange currentOriginalChangeToFind in communicationArray)
                    {
                        // define a bool for whether an event with the current event id was found, defaulting to false
                        bool foundEventId = false;
                        // define a bool for whether an event with the current stream was found, defaulting to false
                        bool foundMatchedStream = false;

                        // declare a list for the incomplete events for the current event id
                        List<PossiblyStreamableAndPossiblyChangedFileChange> tryGetIncompletes;
                        // try to get incomplete events for the current event id and if successful, then the event was found and may need to check if the incomplete events include the current stream
                        if (incompleteChangesList.TryGetValue(currentOriginalChangeToFind.FileChange.EventId, out tryGetIncompletes))
                        {
                            // event id was found in the incomplete events
                            foundEventId = true;
                            // if the current event has a stream, then check the current event id incompletes for the same stream
                            if (currentOriginalChangeToFind.Stream != null)
                            {
                                // if the current event id incompletes contains the current stream, then mark the stream as found
                                if (tryGetIncompletes.Any(currentIncomplete => currentIncomplete.Stream == currentOriginalChangeToFind.Stream))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                            // else if the current event does not have a stream, there is no need to check for the stream in the other lists, so mark it found
                            else
                            {
                                foundMatchedStream = true;
                            }
                        }

                        // declare an array for the completed events for the current event id
                        PossiblyChangedFileChange[] tryGetCompletes;
                        // if we didn't already find both the current event id and the current stream in the incomplete changes,
                        // try to get the completed events for the current event id and if successful, then the event was found and may need to check if the completed events include the current stream
                        if ((!foundEventId && !foundMatchedStream)
                            && completedChangesList.TryGetValue(currentOriginalChangeToFind.FileChange.EventId, out tryGetCompletes))
                        {
                            // event id was found in the completed events
                            foundEventId = true;

                            // if the current event has a stream, then check the streams from completed events for the current stream
                            if (currentOriginalChangeToFind.Stream != null)
                            {
                                // if the streams from completed events contain the current stream, then mark the stream as found
                                if (completedStreams.Contains(currentOriginalChangeToFind.Stream))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                            // else if the current event does not have a stream, there is no need to check for the stream in the other lists, so mark it found
                            else
                            {
                                foundMatchedStream = true;
                            }
                        }

                        // declare an array for the events in error for the current event id
                        PossiblyStreamableAndPossiblyChangedFileChangeWithError[] tryGetErrors;
                        // if we didn't already find both the current event id and the current stream in either the incomplete changes or the completed changes,
                        // try to get the events in error for the current event id and if successful, then the event was found and may need to check if the events in error include the current stream
                        if ((!foundEventId && !foundMatchedStream)
                            && changesInErrorList.TryGetValue(currentOriginalChangeToFind.FileChange.EventId, out tryGetErrors))
                        {
                            // event id was found in the events in error
                            foundEventId = true;

                            // if the current event has a stream, then check the streams from the events in error for the current stream
                            if (currentOriginalChangeToFind.Stream != null)
                            {
                                // if the streams from events in error contain the current stream, then mark the stream as found
                                if (tryGetErrors.Any(currentError => currentError.Stream == currentOriginalChangeToFind.Stream))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                            // else if the current event does not have a stream, there is no need to check for the stream in the other lists, so mark it found
                            else
                            {
                                foundMatchedStream = true;
                            }
                        }

                        // declare an array for the events which do not appear in the output lists because they are dependencies for the current event id
                        PossiblyStreamableFileChange[] tryGetConvertedDependencies;
                        // if we didn't already find both the current event id and the current stream in either the incomplete changes or the completed changes,
                        // try to get the events which do not appear in the output lists for the current event id and if successful, then the event was found and may need to check if the events not in the output lists include the current stream
                        if ((!foundEventId && !foundMatchedStream)
                            && changesConvertedToDependencies.TryGetValue(currentOriginalChangeToFind.FileChange.EventId, out tryGetConvertedDependencies))
                        {
                            // event id was found in the events in error
                            foundEventId = true;

                            // if the current event has a stream, then check the streams from the events in error for the current stream
                            if (currentOriginalChangeToFind.Stream != null)
                            {
                                // if the streams from events in error contain the current stream, then mark the stream as found
                                if (tryGetConvertedDependencies.Any(currentConvertedDependency => currentConvertedDependency.Stream == currentOriginalChangeToFind.Stream))
                                {
                                    foundMatchedStream = true;
                                }
                            }
                            // else if the current event does not have a stream, there is no need to check for the stream in the other lists, so mark it found
                            else
                            {
                                foundMatchedStream = true;
                            }
                        }

                        // define a FileChange wrapped so it can be added to the changes in error which will contain the current event or stream if they weren't found
                        Nullable<PossiblyStreamableAndPossiblyChangedFileChangeWithError> missingEventOrStream = null;
                        // if an event was not found to match the event id for the current event, then add the current event and stream for a new change in error
                        if (!foundEventId)
                        {
                            // wrap the current change so it can be added to the changes in error (since it was not found)
                            missingEventOrStream = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                false, // event did not come back from the server, so it must not have changed and thus requires no update
                                currentOriginalChangeToFind.FileChange, // the current missing change
                                currentOriginalChangeToFind.Stream, // any stream for the current missing change
                                new Exception("Found unmatched FileChange in communicationArray in output lists")); // message that the current event was not found
                        }
                        // else if the current event had a stream but it was not found, then add the current stream for a new change in error
                        else if (currentOriginalChangeToFind.Stream != null
                            && !foundMatchedStream)
                        {
                            // wrap the current stream so it can be added to the changes in error (since it was not found)
                            missingEventOrStream = new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                false, // no event in this error, so cannot be added to the event source database
                                null, // do not copy FileChange since it already exists in a list
                                currentOriginalChangeToFind.Stream, // the missing stream
                                new Exception("Found unmatched Stream in communicationArray in output lists")); // message that the current stream was not found
                        }

                        // if either the event or stream was not found causing a change in error to be set, then add that change in error to the output list
                        if (missingEventOrStream != null)
                        {
                            // if a change in error already exists for the current event id, then expand the array of errors at this event id with the created FileChange
                            if (changesInErrorList.ContainsKey(currentOriginalChangeToFind.FileChange.EventId))
                            {
                                // store the previous array of errors
                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] previousErrors = changesInErrorList[currentOriginalChangeToFind.FileChange.EventId];
                                // create a new array for error with a size expanded by one
                                PossiblyStreamableAndPossiblyChangedFileChangeWithError[] newErrors = new PossiblyStreamableAndPossiblyChangedFileChangeWithError[previousErrors.Length + 1];
                                // copy all the previous errors to the new array
                                previousErrors.CopyTo(newErrors, 0);
                                // put the new error as the last index of the new array
                                newErrors[previousErrors.Length] = (PossiblyStreamableAndPossiblyChangedFileChangeWithError)missingEventOrStream;
                                // replace the value in the error mapping dictionary for the current event id with the expanded array
                                changesInErrorList[currentOriginalChangeToFind.FileChange.EventId] = newErrors;
                            }
                            // else if a change in error does not already exist for the current event id, then add a new array with just the current created FileChange
                            else
                            {
                                // add a new array with just the created FileChange to the error mapping dictionary for the current event id
                                changesInErrorList.Add(currentOriginalChangeToFind.FileChange.EventId,
                                    new PossiblyStreamableAndPossiblyChangedFileChangeWithError[]
                                        {
                                            (PossiblyStreamableAndPossiblyChangedFileChangeWithError)missingEventOrStream
                                        });
                            }
                        }
                    }

                    // set the output completed changes from the completed changes list
                    completedChanges = completedChangesList.SelectMany(currentCompleted =>
                        currentCompleted.Value);
                    // set the output incomplete changes from the incomplete changes list
                    incompleteChanges = incompleteChangesList.SelectMany(currentIncomplete =>
                        currentIncomplete.Value);
                    // set the output changes in error from the changes in error list
                    changesInError = changesInErrorList.SelectMany(currentError =>
                        currentError.Value);
                    #endregion
                }
                // else if there is not a change to communicate, then this is a Sync From  (when responding to a push notification or manual polling) or it does not require communication
                else
                {
                    // create a list to store errors on Sync From (when the previous file/folder for a rename was not found locally)
                    List<PossiblyStreamableAndPossiblyChangedFileChangeWithError> syncFromErrors = new List<PossiblyStreamableAndPossiblyChangedFileChangeWithError>(
                        changesInErrorList.SelectMany(currentChangeInError => currentChangeInError.Value));

                    // if responding to a push notification (or manual polling), then process as Sync From
                    if (respondingToPushNotification)
                    {
                        #region Sync From
                        // Run Sync From
                        // Any events should be output as incompleteChanges, return all with the true boolean (which will cause the event source database to add the new events)
                        // Contains errors only for rename changes where the original file/folder to rename was not found locally (can get converted to new create events)
                        // Should not give any completed changes

                        // status message
                        MessageEvents.FireNewEventMessage(
                            "Checking for any new changes to sync from server",
                            EventMessageLevel.Regular,
                            SyncBoxId: syncBox.SyncBoxId,
                            DeviceId: syncBox.CopiedSettings.DeviceId);

                        // declare the status of the sync from communication
                        CLHttpRestStatus syncFromStatus;
                        // declare the json contract object for the deserialized response
                        PushResponse deserializedResponse;
                        // perform the sync from communication, storing any error that occurs
                        CLError syncFromError = httpRestClient.SyncFromCloud(
                            new Push() // use a new push request
                            {
                                LastSyncId = syncString, // fill in the last sync id
                                DeviceId = syncBox.CopiedSettings.DeviceId, // fill in the device id
                                SyncBoxId = syncBox.SyncBoxId // fill in the sync box id
                            },
                            HttpTimeoutMilliseconds, // milliseconds before http communication will timeout on an operation
                            out syncFromStatus, // output the status of the communication
                            out deserializedResponse); // output the response object resulting from the operation

                        // depending on whether the communication status is a connection failure or not, either increment the failure count or clear it, respectively

                        if (syncFromStatus == CLHttpRestStatus.ConnectionFailed)
                        {
                            lock (MetadataConnectionFailures)
                            {
                                if (MetadataConnectionFailures.Value != ((byte)255))
                                {
                                    MetadataConnectionFailures.Value = (byte)(MetadataConnectionFailures.Value + 1);
                                }
                            }
                        }
                        else if (syncFromStatus == CLHttpRestStatus.NotAuthorized)
                        {
                            credentialsError = CredentialErrorType.OtherError;
                        }
                        else if (syncFromStatus == CLHttpRestStatus.NotAuthorizedExpiredCredentials)
                        {
                            credentialsError = CredentialErrorType.ExpiredCredentials;
                        }
                        else
                        {
                            lock (MetadataConnectionFailures)
                            {
                                if (MetadataConnectionFailures.Value != ((byte)0))
                                {
                                    MetadataConnectionFailures.Value = 0;
                                }
                            }
                        }

                        // if sync from produced an error, rethrow it
                        if (syncFromError != null)
                        {
                            // adding a null FileChange to queue will trigger a SyncFrom without an extra change (gets filtered out on "ProcessingChanges.DequeueAll()" in FileMonitor)
                            GenericHolder<List<FileChange>> errList = new GenericHolder<List<FileChange>>(); // no need to check the failed to add list since we won't add a null FileChange to the list of failed changes
                            CLError err = syncData.addChangesToProcessingQueue(new FileChange[] { null }, true, errList);

                            throw new AggregateException("An error occurred during SyncFrom" +
                                (err == null
                                    ? string.Empty
                                    : " and an error occurred queueing for a new SyncFrom"),
                                (err == null
                                    ? syncFromError.GrabExceptions()
                                    : syncFromError.GrabExceptions().Concat(
                                        err.GrabExceptions())));
                        }

                        if (deserializedResponse == null)
                        {
                            throw new NullReferenceException("SyncFrom deserializedResponse cannot be null");
                        }

                        if (deserializedResponse.Events == null)
                        {
                            throw new NullReferenceException("SyncFrom deserializedResponse cannot have null Events");
                        }

                        AppendRandomSubSecondTicksToSyncFromFolderCreationTimes(deserializedResponse.Events);

                        GenericHolder<string> storeSyncRootUid = new GenericHolder<string>(null);

                        Func<string, string, string, GenericHolder<string>, Dictionary<string, FilePath>, FilePathDictionary<string>, bool> checkRootFolder = (currentEventName, currentEventToName, currentEventUid, storeUid, innerServerUidsToPath, innerPathsToServerUid) =>
                            {
                                if (currentEventName == string.Empty && string.IsNullOrEmpty(currentEventToName))
                                {
                                    storeUid.Value = currentEventUid;
                                    innerServerUidsToPath[currentEventUid] = syncBox.CopiedSettings.SyncRoot;
                                    pathsToServerUid[syncBox.CopiedSettings.SyncRoot] = currentEventUid;
                                    return true;
                                }

                                return false;
                            };

                        // record the latest sync id from the deserialized response
                        newSyncId = deserializedResponse.SyncId;

                        // store all events from sync from as events which still need to be performed (set as the output parameter for incomplete changes)
                        incompleteChanges = deserializedResponse.Events
                            .Where(currentEvent => currentEvent.Metadata == null || !checkRootFolder(currentEvent.Metadata.Name, currentEvent.Metadata.ToName, currentEvent.Metadata.ServerUid, storeSyncRootUid, serverUidsToPath, pathsToServerUid)) // special condition on SID "0" for root folder path
                            .Select(currentEvent => 
                                {
                                    findPathsByUids.TypedData.currentEvent.Value = currentEvent;
                                    var findPathsResult = findPathsByUids.TypedProcess();

                                    PossiblyStreamableAndPossiblyChangedFileChange storeConvertedChange = new PossiblyStreamableAndPossiblyChangedFileChange(resultOrder++,
                                        /* needs to update SQL */ true, // all Sync From events are new and should thus be added to the event source database
                                        CreateFileChangeFromBaseChangePlusHash(new FileChange( // create a FileChange with dependencies and set the hash, start by creating a new FileChange input
                                                DelayCompletedLocker: null,
                                                fileDownloadMoveLocker:
                                                    ((CLDefinitions.SyncHeaderDeletions.Contains(currentEvent.Header.Action ?? currentEvent.Action)
                                                            || CLDefinitions.SyncHeaderRenames.Contains(currentEvent.Header.Action ?? currentEvent.Action))
                                                        ? null
                                                        : new object()))
                                                {
                                                    Direction = SyncDirection.From, // current communcation direction is Sync From (only Sync From events, not mixed like Sync To events)
                                                    NewPath = findPathsResult.findNewPath, // new location of change
                                                    OldPath = findPathsResult.findOldPath, // if the current event is a rename, grab the previous path
                                                    Type = ParseEventStringToType(currentEvent.Action ?? currentEvent.Header.Action), // grab the type of change from the action string
                                                    Metadata = new FileMetadata()
                                                    {
                                                        //Need to find what key this is //LinkTargetPath <-- what does this comment mean?

                                                        ServerUid = currentEvent.Metadata.ServerUid, // unique id on the server
                                                        HashableProperties = new FileMetadataHashableProperties((currentEvent.Metadata.IsFolder ?? ParseEventStringToIsFolder(currentEvent.Header.Action ?? currentEvent.Action)), // try to grab whether this event is a folder from the specified property, otherwise parse it from the action
                                                            currentEvent.Metadata.ModifiedDate, // grab the last modified time
                                                            currentEvent.Metadata.CreatedDate, // grab the time of creation
                                                            currentEvent.Metadata.Size), // grab the file size, or null for non-files
                                                        Revision = currentEvent.Metadata.Revision, // grab the revision, or null for non-files
                                                        StorageKey = currentEvent.Metadata.StorageKey, // grab the storage key, or null for non-files
                                                        MimeType = currentEvent.Metadata.MimeType, // never set on Windows
                                                        ParentFolderServerUid = currentEvent.Metadata.ToParentUid ?? currentEvent.Metadata.ParentUid
                                                    }
                                                },
                                            currentEvent.Metadata.Hash, // grab the MD5 hash
                                            DependencyDebugging),
                                            null);

                                    switch (storeConvertedChange.FileChange.Type)
                                    {
                                        case FileChangeType.Created:
                                        case FileChangeType.Modified:
                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                            {
                                                try
                                                {
                                                    pathsToServerUid[storeConvertedChange.FileChange.NewPath.Copy()] = storeConvertedChange.FileChange.Metadata.ServerUid;
                                                    serverUidsToPath[storeConvertedChange.FileChange.Metadata.ServerUid] = storeConvertedChange.FileChange.NewPath.ToString();
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            break;

                                        case FileChangeType.Renamed:
                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                            {
                                                try
                                                {
                                                    if (!pathsToServerUid.ContainsKey(storeConvertedChange.FileChange.NewPath))
                                                    {
                                                        FilePathHierarchicalNode<string> oldPathsToRename;
                                                        CLError getOldPathsError = pathsToServerUid.GrabHierarchyForPath(storeConvertedChange.FileChange.OldPath, out oldPathsToRename, suppressException: true);

                                                        if (getOldPathsError == null
                                                            && oldPathsToRename != null)
                                                        {
                                                            pathsToServerUid.Rename(storeConvertedChange.FileChange.OldPath, storeConvertedChange.FileChange.NewPath.Copy());
                                                        }
                                                    }

                                                    serverUidsToPath[storeConvertedChange.FileChange.Metadata.ServerUid] = storeConvertedChange.FileChange.NewPath.ToString();
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            break;

                                        case FileChangeType.Deleted:
                                            if (currentEvent.Header.Status != CLDefinitions.RESTResponseStatusFailed)
                                            {
                                                try
                                                {
                                                    pathsToServerUid.Remove(storeConvertedChange.FileChange.NewPath);
                                                    serverUidsToPath.Remove(storeConvertedChange.FileChange.Metadata.ServerUid);
                                                }
                                                catch
                                                {
                                                }
                                            }
                                            break;
                                    }

                                    return storeConvertedChange;

                                }) // no streams for Sync From events
                            .ToArray(); // select into an array to prevent reiteration of select logic

                        if (storeSyncRootUid.Value != null)
                        {
                            syncRootUid = storeSyncRootUid.Value;
                        }

                        // create a list for storing rename changes where the old file/folder was not found to rename
                        List<PossiblyStreamableAndPossiblyChangedFileChange> renameNotFounds = new List<PossiblyStreamableAndPossiblyChangedFileChange>();

                        // declare a dictionary for already visited Sync From renames so if metadata was found for a rename in an event then later renames can carry forward the metadata
                        FilePathDictionary<FileMetadata> alreadyVisitedRenames;
                        // initialize the visited renames dictionary, storing any error that occurs
                        CLError createVisitedRenames = FilePathDictionary<FileMetadata>.CreateAndInitialize((syncBox.CopiedSettings.SyncRoot ?? string.Empty),
                            out alreadyVisitedRenames);

                        // loop through the Sync From changes where the type is a rename event and select just the FileChange
                        foreach (FileChange currentChange in incompleteChanges
                            .Select(incompleteChange => incompleteChange.FileChange))
                        //.Where(incompleteChange => incompleteChange.FileChange.Type == FileChangeType.Renamed)
                        //.Select(incompleteChange => incompleteChange.FileChange))
                        {
                            // check for sync shutdown
                            Monitor.Enter(FullShutdownToken);
                            try
                            {
                                if (FullShutdownToken.Token.IsCancellationRequested)
                                {
                                    completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                                    incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                                    changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                                    newSyncId = Helpers.DefaultForType<string>();
                                    return new Exception("Shut down in the middle of communication");
                                }
                            }
                            finally
                            {
                                Monitor.Exit(FullShutdownToken);
                            }

                            switch (currentChange.Type)
                            {
                                case FileChangeType.Renamed:
                                    // the current event must contain an old path to search for metadata, otherwise throw an exception
                                    if (currentChange.OldPath == null)
                                    {
                                        throw new NullReferenceException("OldPath cannot be null if currentChange is of Type Renamed");
                                    }

                                    // define a change to store the change which contains the previous metadata for the rename event, defaulting to null
                                    FileChange originalMetadata = null;

                                    // declare a FileChange which will be used as temp storage upon iterations searching the current UpDownEvent changes and the current failures
                                    FileChange foundOldPath;
                                    // declare metadata which will be used as temp storage upon iterations searching the previously visited renames
                                    FileMetadata foundOldPathMetadataOnly;

                                    // loop through previously visited rename events, the FileChanges in the current UpDownEvent changes, and the failure changes where the OldPath matches the current event's previous path
                                    foreach (FileChange findMetadata in

                                        // search the already visited Sync From rename events by the current event's previous path for when multiple rename events in a communication batch keep moving the metadata forward
                                        (alreadyVisitedRenames.TryGetValue(currentChange.OldPath, out foundOldPathMetadataOnly)
                                            ? Helpers.EnumerateSingleItem(new FileChange() // if a match is found, then include the found result
                                                {
                                                    Metadata = foundOldPathMetadataOnly, // metadata to move forward
                                                })
                                            : Enumerable.Empty<FileChange>()) // else if a match is not found, then use an empty enumeration

                                        // search the current UpDownEvents for one matching the current event's previous path (comparing against the UpDownEvent's NewPath)
                                        .Concat(((getRunningUpDownChangesDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                            ? Helpers.EnumerateSingleItem(foundOldPath) // if a match is found, then include the found result
                                            : Enumerable.Empty<FileChange>())) // else if a match is not found, then use an empty enumeration

                                            // then search the current failures for the one matching the current event's previous path (comparing against the failed event's NewPath)
                                            .Concat(getFailuresDict().TryGetValue(currentChange.OldPath, out foundOldPath)
                                                ? Helpers.EnumerateSingleItem(foundOldPath) // if a match is found, then include the found result
                                                : Enumerable.Empty<FileChange>()) // else if a match is not found, then use an empty enumeration

                                            // order the results descending by EventId so the first match will be the latest event
                                            .OrderByDescending(currentOldPath => currentOldPath.EventId)))
                                    {
                                        // if the current matched change by path also matches by revision, then use the found change for the previous metadata and stop searching
                                        if (findMetadata.Metadata.Revision == currentChange.Metadata.Revision)
                                        {
                                            // use the found change for the previous metadata
                                            originalMetadata = findMetadata;

                                            // declare a hierarchy for the old path of the rename
                                            FilePathHierarchicalNode<FileMetadata> renameHierarchy;
                                            // grab the hierarchy for the old path of the rename from already visited renames, storing any error that occurred
                                            CLError grabHierarchyError = alreadyVisitedRenames.GrabHierarchyForPath(currentChange.OldPath, out renameHierarchy, suppressException: true);
                                            // if there was an error grabbing the hierarchy, then rethrow the error
                                            if (grabHierarchyError != null)
                                            {
                                                throw new AggregateException("Error grabbing renameHierarchy from alreadyVisitedRenames", grabHierarchyError.GrabExceptions());
                                            }
                                            // if there was a hierarchy found at the old path for the rename, then apply a rename to the dictionary based on the current rename
                                            if (renameHierarchy != null)
                                            {
                                                alreadyVisitedRenames.Rename(currentChange.OldPath, currentChange.NewPath.Copy());
                                            }

                                            // add the currently found metadata to the rename dictionary so it can be searched for subsequent renames
                                            alreadyVisitedRenames[currentChange.NewPath.Copy()] = findMetadata.Metadata;

                                            // stop searching for a match
                                            break;
                                        }
                                    }

                                    // if a change was not found by the rename event's old path and revision, then try to grab the previous metadata from the database
                                    if (originalMetadata == null)
                                    {
                                        // declare metadata which will be output from the database
                                        FileMetadata syncStateMetadata;
                                        // search the database for metadata for the event's previous path and revision, storing any error that occurred
                                        CLError queryMetadataError = syncData.getMetadataByPathAndRevision(currentChange.OldPath.ToString(),
                                            currentChange.Metadata.Revision,
                                            out syncStateMetadata);

                                        // if there was an error querying the database for existing metadata, then rethrow the error
                                        if (queryMetadataError != null)
                                        {
                                            throw new AggregateException("Error querying SqlIndexer for sync state by path: " + currentChange.OldPath.ToString() +
                                                " and revision: " + currentChange.Metadata.Revision, queryMetadataError.GrabExceptions());
                                        }

                                        // if no metadata was returned from the database, then throw an error if the change originated on the client or otherwise try to grab the metadata from the server for a new creation event at the final destination of the rename
                                        if (syncStateMetadata == null)
                                        {
                                            // declare the status of communication from getting metadata
                                            CLHttpRestStatus getNewMetadataStatus;
                                            // declare the response object of the actual metadata when returned
                                            JsonContracts.Metadata newMetadata;
                                            // grab the metadata from the server for the current path and whether or not the current event represents a folder, storing any error that occurs
                                            CLError getNewMetadataError = httpRestClient.GetMetadata(currentChange.NewPath,
                                                currentChange.Metadata.HashableProperties.IsFolder,
                                                HttpTimeoutMilliseconds,
                                                out getNewMetadataStatus,
                                                out newMetadata);

                                            // if an error occurred getting metadata, rethrow the error
                                            if (getNewMetadataError != null)
                                            {
                                                throw new AggregateException("An error occurred retrieving metadata", getNewMetadataError.GrabExceptions());
                                            }

                                            // if the communication was not successful, then throw an error with the bad status
                                            if (getNewMetadataStatus != CLHttpRestStatus.Success
                                                && getNewMetadataStatus != CLHttpRestStatus.NoContent)
                                            {
                                                throw new Exception("Retrieving metadata did not return successful status: CLHttpRestStatus." + getNewMetadataStatus.ToString());
                                            }

                                            // if there was no content, then the metadata was not found at the given path so throw an error
                                            if (getNewMetadataStatus == CLHttpRestStatus.NoContent
                                                || newMetadata.Deleted == true)
                                            {
                                                throw new Exception("Metadata not found for given path");
                                            }

                                            if (newMetadata.IsNotPending == false)
                                            {
                                                CLHttpRestStatus fileVersionsStatus;
                                                JsonContracts.FileVersion[] fileVersions;
                                                CLError fileVersionsError = httpRestClient.GetFileVersions(
                                                    newMetadata.ServerUid,
                                                    HttpTimeoutMilliseconds,
                                                    out fileVersionsStatus,
                                                    out fileVersions);

                                                if (fileVersionsStatus != CLHttpRestStatus.Success
                                                    && fileVersionsStatus != CLHttpRestStatus.NoContent)
                                                {
                                                    throw new AggregateException("An error occurred retrieving previous versions of a file", fileVersionsError.GrabExceptions());
                                                }

                                                JsonContracts.FileVersion lastNonPendingVersion = (fileVersions ?? Enumerable.Empty<JsonContracts.FileVersion>())
                                                    .OrderByDescending(fileVersion => (fileVersion.Version ?? -1))
                                                    .FirstOrDefault(fileVersion => fileVersion.IsDeleted != true
                                                        && fileVersion.IsNotPending != false);

                                                if (lastNonPendingVersion == null)
                                                {
                                                    throw new NullReferenceException("A previous non-pending file version was not found");
                                                }

                                                newMetadata.IsNotPending = true;

                                                // server does not version other metadata, so these are the only ones we can really use to update
                                                newMetadata.StorageKey = lastNonPendingVersion.StorageKey;
                                                newMetadata.Hash = lastNonPendingVersion.FileHash;
                                                newMetadata.Size = lastNonPendingVersion.FileSize;
                                            }

                                            // create and initialize the FileChange for the new file creation by combining data from the current rename event with the metadata from the server, also adds the hash
                                            FileChangeWithDependencies newPathCreation = CreateFileChangeFromBaseChangePlusHash(new FileChange(DelayCompletedLocker: null, fileDownloadMoveLocker: new object())
                                                {
                                                    Direction = SyncDirection.From, // emulate a new Sync From event so the client will try to download the file from the new location
                                                    NewPath = currentChange.NewPath, // new location only (no previous location since this is converted from a rename to a create)
                                                    Type = FileChangeType.Created, // a create to download a new file or process a new folder
                                                    Metadata = new FileMetadata()
                                                    {
                                                        //Need to find what key this is //LinkTargetPath <-- what does this comment mean?

                                                        ServerUid = newMetadata.ServerUid, // unique id on the server
                                                        HashableProperties = new FileMetadataHashableProperties(currentChange.Metadata.HashableProperties.IsFolder, // whether this creation is a folder
                                                            newMetadata.ModifiedDate, // last modified time for this file system object
                                                            newMetadata.CreatedDate, // creation time for this file system object
                                                            newMetadata.Size), // file size or null for folders
                                                        Revision = newMetadata.Revision, // file revision or null for folders
                                                        StorageKey = newMetadata.StorageKey, // file storage key or null for folders
                                                        MimeType = newMetadata.MimeType // never set on Windows
                                                    }
                                                },
                                                newMetadata.Hash, // file MD5 hash or null for folder
                                                DependencyDebugging);

                                            alreadyVisitedRenames[newPathCreation.NewPath.Copy()] = newPathCreation.Metadata;

                                            // make sure to add change to SQL
                                            newPathCreation.DoNotAddToSQLIndex = false;
                                            currentChange.DoNotAddToSQLIndex = false;

                                            // merge the creation of the new FileChange for a pseudo Sync From creation event with the event source database, storing any error that occurs
                                            CLError newPathCreationError = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(newPathCreation, currentChange)));
                                            // if an error occurred merging the new FileChange with the event source database, then rethrow the error
                                            if (newPathCreationError != null)
                                            {
                                                throw new AggregateException("Error merging new file creation change in response to not finding existing metadata at sync from rename old path", newPathCreationError.GrabExceptions());
                                            }

                                            // add the pseudo Sync From creation event to the list to be used as changes in error, wrap it first as the correct type
                                            syncFromErrors.Add(new PossiblyStreamableAndPossiblyChangedFileChangeWithError(resultOrder++,
                                                false, // mark as not changed since we already merged the path creation on top of the previous change into the event source database
                                                newPathCreation, // the change itself
                                                null, // no streams for Sync From changes
                                                new Exception("Unable to find metadata for file. May have been a rename on a local file path that does not exist. Created new FileChange to download file"))); // message for the type of error that occurred

                                            // a file may still exist at the old path on disk, so to make sure the server looks the same, we must duplicate the file on the server
                                            // TODO: all System.IO or disk access should be done through syncData ISyncDataObject interface object

                                            if (currentChange.OldPath != null)
                                            {
                                                FileStream uploadStreamForDuplication = null;
                                                try
                                                {
                                                    string oldPathString = currentChange.OldPath.ToString();
                                                    bool fileExists;
                                                    try
                                                    {
                                                        fileExists = File.Exists(oldPathString);

                                                        if (fileExists)
                                                        {
                                                            uploadStreamForDuplication = new FileStream(oldPathString, FileMode.Open, FileAccess.Read, FileShare.Read);

                                                            long duplicateSize = 0;
                                                            byte[] duplicateHash;
                                                            MD5 duplicateHasher = MD5.Create();

                                                            try
                                                            {
                                                                byte[] fileBuffer = new byte[FileConstants.BufferSize];
                                                                int fileReadBytes;

                                                                while ((fileReadBytes = uploadStreamForDuplication.Read(fileBuffer, 0, FileConstants.BufferSize)) > 0)
                                                                {
                                                                    duplicateSize += fileReadBytes;
                                                                    duplicateHasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
                                                                }

                                                                duplicateHasher.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);
                                                                duplicateHash = duplicateHasher.Hash;
                                                            }
                                                            finally
                                                            {
                                                                try
                                                                {
                                                                    if (uploadStreamForDuplication != null)
                                                                    {
                                                                        uploadStreamForDuplication.Seek(0, SeekOrigin.Begin);
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                }

                                                                duplicateHasher.Dispose();
                                                            }

                                                            FileChange duplicateChange =
                                                                new FileChange()
                                                                {
                                                                    Direction = SyncDirection.To,
                                                                    Metadata = new FileMetadata()
                                                                    {
                                                                        HashableProperties = new FileMetadataHashableProperties(
                                                                            /*isFolder*/ false,
                                                                            File.GetLastAccessTimeUtc(oldPathString),
                                                                            File.GetLastWriteTimeUtc(oldPathString),
                                                                            duplicateSize)
                                                                    },
                                                                    NewPath = oldPathString,
                                                                    Type = FileChangeType.Created
                                                                };
                                                            CLError setDuplicateHash = duplicateChange.SetMD5(duplicateHash);
                                                            if (setDuplicateHash != null)
                                                            {
                                                                throw new AggregateException("Error setting MD5 on duplicateChange: " + setDuplicateHash.errorDescription, setDuplicateHash.GrabExceptions());
                                                            }

                                                            CLHttpRestStatus postDuplicateChangeStatus;
                                                            JsonContracts.Event postDuplicateChangeResult;
                                                            CLError postDuplicateChange = httpRestClient.PostFileChange(
                                                                duplicateChange,
                                                                HttpTimeoutMilliseconds,
                                                                out postDuplicateChangeStatus,
                                                                out postDuplicateChangeResult);
                                                            if (postDuplicateChangeStatus != CLHttpRestStatus.Success)
                                                            {
                                                                throw new AggregateException("Error adding duplicate file on server: " + postDuplicateChange.errorDescription, postDuplicateChange.GrabExceptions());
                                                            }

                                                            if (postDuplicateChangeResult == null)
                                                            {
                                                                throw new NullReferenceException("Null event response adding duplicate file");
                                                            }
                                                            if (postDuplicateChangeResult.Header == null)
                                                            {
                                                                throw new NullReferenceException("Null event response header adding duplicate file");
                                                            }
                                                            if (string.IsNullOrEmpty(postDuplicateChangeResult.Header.Status))
                                                            {
                                                                throw new NullReferenceException("Null event response header status adding duplicate file");
                                                            }
                                                            if (postDuplicateChangeResult.Metadata == null)
                                                            {
                                                                throw new NullReferenceException("Null event response metadata adding duplicate file");
                                                            }

                                                            duplicateChange.Metadata.Revision = postDuplicateChangeResult.Metadata.Revision;
                                                            duplicateChange.Metadata.StorageKey = postDuplicateChangeResult.Metadata.StorageKey;

                                                            if ((new[]
                                                                {
                                                                    CLDefinitions.CLEventTypeAccepted,
                                                                    CLDefinitions.CLEventTypeNoOperation,
                                                                    CLDefinitions.CLEventTypeExists,
                                                                    CLDefinitions.CLEventTypeDuplicate,
                                                                    CLDefinitions.CLEventTypeUploading
                                                                }).Contains(postDuplicateChangeResult.Header.Status))
                                                            {
                                                                CLError mergeDuplicateChange = syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(duplicateChange)));
                                                                if (mergeDuplicateChange != null)
                                                                {
                                                                    throw new AggregateException("Error writing duplicate file change to database after communication: " + mergeDuplicateChange.errorDescription, mergeDuplicateChange.GrabExceptions());
                                                                }

                                                                CLError completeDuplicateChange = syncData.completeSingleEvent(duplicateChange.EventId);
                                                                if (completeDuplicateChange != null)
                                                                {
                                                                    throw new AggregateException("Error marking duplicate file change complete in database: " + completeDuplicateChange.errorDescription, completeDuplicateChange.GrabExceptions());
                                                                }
                                                            }
                                                            else if ((new[]
                                                                {
                                                                    CLDefinitions.CLEventTypeUpload,
                                                                    CLDefinitions.CLEventTypeUploading
                                                                }).Contains(postDuplicateChangeResult.Header.Status))
                                                            {
                                                                incompleteChanges = incompleteChanges.Concat(Helpers.EnumerateSingleItem(
                                                                        new PossiblyStreamableAndPossiblyChangedFileChange(
                                                                            resultOrder++,
                                                                            /*Changed*/ true,
                                                                            duplicateChange,
                                                                            uploadStreamForDuplication)
                                                                    ));

                                                                uploadStreamForDuplication = null; // prevents disposal on finally since the Stream will now be sent off for async processing
                                                            }
                                                            else
                                                            {
                                                                throw new InvalidOperationException("Event response header status invalid for duplicating file: " + postDuplicateChangeResult.Header.Status);
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        try
                                                        {
                                                            MessageEvents.FireNewEventMessage(
                                                                "Error occurred handling conflict for a file rename: " + ex.Message,
                                                                EventMessageLevel.Regular,
                                                                /*Error*/ new GeneralErrorInfo(),
                                                                syncBox.SyncBoxId,
                                                                syncBox.CopiedSettings.DeviceId);
                                                        }
                                                        catch
                                                        {
                                                        }

                                                        fileExists = false;
                                                    }

                                                    if (fileExists)
                                                    {
                                                        try
                                                        {
                                                            MessageEvents.FireNewEventMessage(
                                                                "File rename conflict handled through duplication",
                                                                EventMessageLevel.Minor,
                                                                /*Error*/ null,
                                                                syncBox.SyncBoxId,
                                                                syncBox.CopiedSettings.DeviceId);
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                finally
                                                {
                                                    if (uploadStreamForDuplication != null)
                                                    {
                                                        try
                                                        {
                                                            uploadStreamForDuplication.Dispose();
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                            }

                                            // find the original change that required the pseudo Sync From creation and add it to a list to exclude when returning incomplete changes (since now it is a change in error)
                                            renameNotFounds.Add(incompleteChanges.First(currentIncompleteChange => currentIncompleteChange.FileChange == currentChange));
                                        }
                                        // else if metadata was found for the current rename in the event source database, then add it to 
                                        else
                                        {
                                            // declare a hierarchy for the old path of the rename
                                            FilePathHierarchicalNode<FileMetadata> renameHierarchy;
                                            // grab the hierarchy for the old path of the rename from already visited renames, storing any error that occurred
                                            CLError grabHierarchyError = alreadyVisitedRenames.GrabHierarchyForPath(currentChange.OldPath, out renameHierarchy, suppressException: true);
                                            // if there was an error grabbing the hierarchy, then rethrow the error
                                            if (grabHierarchyError != null)
                                            {
                                                throw new AggregateException("Error grabbing renameHierarchy from alreadyVisitedRenames", grabHierarchyError.GrabExceptions());
                                            }
                                            // if there was a hierarchy found at the old path for the rename, then apply a rename to the dictionary based on the current rename
                                            if (renameHierarchy != null)
                                            {
                                                alreadyVisitedRenames.Rename(currentChange.OldPath, currentChange.NewPath.Copy());
                                            }

                                            // add the currently found metadata to the rename dictionary so it can be searched for subsequent renames
                                            alreadyVisitedRenames[currentChange.NewPath.Copy()] = syncStateMetadata;
                                        }

                                        // create a fake FileChange just to store found metadata for the current event which will be pulled out again and used to replace the current event metadata
                                        originalMetadata = new FileChange()
                                            {
                                                NewPath = new FilePath("Not a valid file change", new FilePath(string.Empty)), // no idea why I set a fake NewPath, it should not be read later for anything
                                                Metadata = syncStateMetadata
                                            };
                                    }

                                    // pull out the found metadata and use it for the current event
                                    currentChange.Metadata = originalMetadata.Metadata;
                                    break;

                                case FileChangeType.Created:
                                case FileChangeType.Modified:
                                    alreadyVisitedRenames[currentChange.NewPath.Copy()] = currentChange.Metadata;
                                    break;

                                case FileChangeType.Deleted:
                                    alreadyVisitedRenames.Remove(currentChange.NewPath);
                                    break;
                            }
                        }

                        // if any renames had metadata that could not be found, then they had been moved to the changes in error and should not be returned with the incomplete changes
                        if (renameNotFounds.Count > 0)
                        {
                            incompleteChanges = incompleteChanges.Except(renameNotFounds);
                        }
                        #endregion
                    }
                    // else if there are no changes to communicate and we're not responding to a push notification,
                    // do not process any communication (instead set output as empty)
                    else
                    {
                        // status message
                        MessageEvents.FireNewEventMessage(
                            "Nothing to communicate with server",
                            EventMessageLevel.Minor,
                            SyncBoxId: syncBox.SyncBoxId,
                            DeviceId: syncBox.CopiedSettings.DeviceId);

                        incompleteChanges = Enumerable.Empty<PossiblyStreamableAndPossiblyChangedFileChange>();
                        newSyncId = null; // the null sync id is used to check on the calling method whether communication occurred

                        RunLocker.Value = true;
                    }

                    // Sync From never has complete changes
                    completedChanges = Enumerable.Empty<PossiblyChangedFileChange>();
                    // the only errors on Sync From should be if there was a rename and local metadata at the previous location and revision was not found
                    changesInError = syncFromErrors;
                }
            }
            catch (Exception ex)
            {
                // status message
                MessageEvents.FireNewEventMessage(
                    Message: "Communication of changes with server had an error" +
                        (string.IsNullOrEmpty(ex.Message)
                            ? string.Empty  // if there is no exception message then do not append anything
                            : ": " + // else if there is an exception message at least something will be appended
                                ((ex.InnerException == null || string.IsNullOrEmpty(ex.InnerException.Message))
                                    ? ex.Message // if there is no inner exception with message then just append the outer exception message
                                    : ex.InnerException.Message)), // else if there is an inner exception with message then append the inner exception message
                    Level: EventMessageLevel.Important,
                    Error: new GeneralErrorInfo(),
                    SyncBoxId: syncBox.SyncBoxId,
                    DeviceId: syncBox.CopiedSettings.DeviceId);

                // default all ouputs

                completedChanges = Helpers.DefaultForType<IEnumerable<PossiblyChangedFileChange>>();
                incompleteChanges = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChange>>();
                changesInError = Helpers.DefaultForType<IEnumerable<PossiblyStreamableAndPossiblyChangedFileChangeWithError>>();
                newSyncId = Helpers.DefaultForType<string>();

                // return the error to the calling method
                return ex;
            }
            return null;
        }

        private static void AppendRandomSubSecondTicksToSyncFromFolderCreationTimes(Event[] deserializedResponseEvents)
        {
            // loop through events to add random sub-seconds to creation time for Sync From folder creations so that the time can be used by the
            // FileMonitor MonitorAgent later as a way to compare folder deletions versus creations to convert to moves
            foreach (JsonContracts.Event toModifyTime in deserializedResponseEvents)
            {
                if (toModifyTime.Header != null
                    && string.IsNullOrEmpty(toModifyTime.Header.Status) // condition for SyncFrom
                    && toModifyTime.Metadata != null
                    && (toModifyTime.Metadata.IsFolder ?? ParseEventStringToIsFolder(toModifyTime.Header.Action ?? toModifyTime.Action)) // condition for directories
                    && toModifyTime.Metadata.CreatedDate != null
                    && ((DateTime)toModifyTime.Metadata.CreatedDate).DropSubSeconds().Ticks == ((DateTime)toModifyTime.Metadata.CreatedDate).Ticks) // only if it does not already have sub-second detail
                {
                    toModifyTime.Metadata.CreatedDate = new DateTime(
                        ((DateTime)toModifyTime.Metadata.CreatedDate).Ticks
                            + ((long)Helpers.GetRandomNumberOfTicksLessThanASecond()), // random number of sub-second ticks appended for better comparisons in the FileMonitor later
                        ((DateTime)toModifyTime.Metadata.CreatedDate).Kind);
                }
            }
        }

        // helper method which adds a FileChange to a FilePathDictionary by its NewPath and recursively does the same for inner dependencies (if any)
        private static void AddChangeToDictionary(FilePathDictionary<FileChange> fillDict, FileChange currentChangeToAdd)
        {
            // add the current level change at its NewPath
            fillDict[currentChangeToAdd.NewPath.Copy()] = currentChangeToAdd;

            // try cast the current level change
            FileChangeWithDependencies currentChangeWithDependencies = currentChangeToAdd as FileChangeWithDependencies;

            // if the current level change successfully cast as one with dependencies and has at least one dependency, then recurse this method for each dependency
            if (currentChangeWithDependencies != null
                && currentChangeWithDependencies.DependenciesCount > 0)
            {
                // loop through the current level change's dependencies
                foreach (FileChange currentChangeDependency in currentChangeWithDependencies.Dependencies)
                {
                    // add the next level change (dependency of the current level) to the same dictionary via recursion
                    AddChangeToDictionary(fillDict, currentChangeDependency);
                }
            }
        }

        // helper method which takes a FileChange and a hash string and creates a new FileChange with dependencies and sets the hash as the MD5 bytes; also copies over dependencies if any
        private static FileChangeWithDependencies CreateFileChangeFromBaseChangePlusHash(FileChange baseChange, string hashString, bool DependencyDebugging)
        {
            // if baseChange was not set, then the wrapped change would also be null so just return that
            if (baseChange == null)
            {
                return null;
            }

            // set the MD5 in the baseChange from the hashString, storing any error that occurred
            CLError setHashError = baseChange.SetMD5(hashString);
            // if an error occurred setting the MD5, then rethrow the error
            if (setHashError != null)
            {
                throw new AggregateException("Error setting MD5 via hashString on baseChange", setHashError.GrabExceptions());
            }

            // try cast the input changes as one with dependencies
            FileChangeWithDependencies castBase = baseChange as FileChangeWithDependencies;

            // declare a FileChange with dependencies which will be created and returned
            FileChangeWithDependencies returnedChange;
            // initialize a new FileChange with dependencies from the baseChange using existing dependencies, if any; store any error that may occur
            CLError changeConversionError = FileChangeWithDependencies.CreateAndInitialize(baseChange, // change to copy
                (castBase == null || castBase.DependenciesCount <= 0 // test for previous dependencies
                    ? null // if there were no previous dependencies, then use none for the new FileChange
                    : castBase.Dependencies), // else if there were previous dependencies, have them copied to the new FileChange
                out returnedChange, // output the created FileChange with dependencies
                fileDownloadMoveLocker: baseChange.fileDownloadMoveLocker);
            // if an error occurred creating the FileChange with dependencies for return, then rethrow the error
            if (changeConversionError != null)
            {
                throw new AggregateException("Error converting baseChange to a FileChangeWithDependencies", changeConversionError.GrabExceptions());
            }
            else if (DependencyDebugging
                && castBase != null
                && castBase.DependenciesCount > 0)
            {
                Helpers.CheckFileChangeDependenciesForDuplicates(returnedChange);
            }

            // return the copied FileChange with dependencies
            return returnedChange;
        }

        // grab whether an action represents a folder action by checking if it is in the list of folder actions and return
        private static bool ParseEventStringToIsFolder(string actionString)
        {
            // check first if the input string represents a folder and if so, return true for isFolder
            if (CLDefinitions.SyncHeaderIsFolders.Contains(actionString))
            {
                return true;
            }

            // next check if the input string represents a file (including links) and if so, return false for isFolder
            if (CLDefinitions.SyncHeaderIsFiles.Contains(actionString))
            {
                return false;
            }

            // unable to parse action as either a folder or a file, throw error
            throw new ArgumentException("eventString was not parsable as a file or folder: " + (actionString ?? "{null}"));
        }

        // grab type of FileChange by action string
        private static FileChangeType ParseEventStringToType(string actionString)
        {
            // check if action string is a creation and if so, return that
            if (CLDefinitions.SyncHeaderCreations.Contains(actionString))
            {
                return FileChangeType.Created;
            }

            // check if action string is a deletion and if so, return that
            if (CLDefinitions.SyncHeaderDeletions.Contains(actionString))
            {
                return FileChangeType.Deleted;
            }

            // check if action string is a modification and if so, return that
            if (CLDefinitions.SyncHeaderModifications.Contains(actionString))
            {
                return FileChangeType.Modified;
            }

            // check if action string is a rename and if so, return that
            if (CLDefinitions.SyncHeaderRenames.Contains(actionString))
            {
                return FileChangeType.Renamed;
            }

            // unable to grab type of FileChange from action string, throw error
            throw new ArgumentException("eventString was not parsable to FileChangeType: " + (actionString ?? "{null}"));
        }

        // helper method which returns whether FileChange should still be added to FailureQueue for retrying;
        // in the process if the change is found to not need to continue then it should be removed/handled appropriately
        private static bool ContinueToRetry(PossiblyPreexistingFileChangeInError toRetry, ISyncDataObject syncData, byte MaxNumberOfFailureRetries, byte MaxNumberOfNotFounds, out IEnumerable<FileChange> NotFoundDependenciesToTry)
        {
            // if the current FileChange was marked as a preexisting error then this is not a new error to count, then return true so it can be reprocessed
            if (toRetry.IsPreexisting)
            {
                // did not cancel out this FileChange as one which exceeded the max count of NotFound attempts, so there were no relavent dependencies
                NotFoundDependenciesToTry = null;

                // FileChange should still be added to FailureQueue for retrying
                return true;
            }
            // else if the current FileChange was not marked as a preexisting error, then increment the failure counter and check the failure counts for whether removal is needed
            else
            {
                //// not necessary to check cause it's checked on struct wrapper construction
                //if (toRetry.FileChange == null)
                //{
                //    throw new NullReferenceException("toRetry's FileChange cannot be null");
                //}

                // if no errors have been recorded for this change previously, then mark the path state for the initial error
                if (toRetry.FileChange.FailureCounter == 0)
                {
                    // mark the path state for error
                    MessageEvents.SetPathState(toRetry.FileChange, // source of the event (the event itself)
                        new SetBadge(PathState.Failed, // state to set is failed
                            ((toRetry.FileChange.Direction == SyncDirection.From && toRetry.FileChange.Type == FileChangeType.Renamed)
                                    ? toRetry.FileChange.OldPath // if the change is a Sync From rename, then the location on disk is the previous location
                                    : toRetry.FileChange.NewPath))); // else if the change is not a Sync From rename, then the location on disk is the current location
                }

                // increment the count of failures
                toRetry.FileChange.FailureCounter++;

                // if the current failure count is less than the max number allowed and the current not found count is less than the max number allowed, then return true to continue reprocessing
                if (toRetry.FileChange.FailureCounter < MaxNumberOfFailureRetries
                    && toRetry.FileChange.NotFoundForStreamCounter < MaxNumberOfNotFounds)
                {
                    // did not cancel out this FileChange as one which exceeded the max count of NotFound attempts, so there were no relavent dependencies
                    NotFoundDependenciesToTry = null;

                    // FileChange should still be added to FailureQueue for retrying
                    return true;
                }

                // if the current not found count reached the max number allowed, then presume the event was cancelled out to clear the path state and remove the event from the event source database
                if (toRetry.FileChange.NotFoundForStreamCounter >= MaxNumberOfNotFounds)
                {
                    // remove the badge at the current path by setting it as synced
                    MessageEvents.SetPathState(toRetry.FileChange, new SetBadge(PathState.Synced, toRetry.FileChange.NewPath));

                    // make sure to add change to SQL
                    toRetry.FileChange.DoNotAddToSQLIndex = false;

                    // remove the cancelled out event from the event source database
                    syncData.mergeToSql(Helpers.EnumerateSingleItem(new FileChangeMerge(null, toRetry.FileChange)));

                    FileChangeWithDependencies castNotFound = toRetry.FileChange as FileChangeWithDependencies;
                    if (castNotFound != null
                        && castNotFound.DependenciesCount > 0)
                    {
                        // did cancel out this FileChange as one which exceeded the max count of NotFound attempts and 
                        NotFoundDependenciesToTry = castNotFound.Dependencies;
                    }
                    else
                    {
                        // did cancel out this FileChange as one which exceeded the max count of NotFound attempts but still had no dependencies to try
                        NotFoundDependenciesToTry = null;
                    }
                }
                // else if the current not found count did not reach the max number allowed (since instead the failure count reached the max number allowed),
                // then this failed change and all its dependencies cannot be processed so mark the dependencies with the failed badge
                else
                {
                    // by this point we know that the failure counter is greater than allowed (since either that or the not found counter was greater than allowed which would have 

                    // try cast the failed out change as one with dependencies
                    FileChangeWithDependencies castRetry = toRetry.FileChange as FileChangeWithDependencies;
                    // if the failed out change was one with dependencies, then by stopping processing all its dependencies won't process either so they should all be marked in error
                    if (castRetry != null
                        && castRetry.DependenciesCount > 0)
                    {
                        // call a recursive function which will take a list of failed dependencies to badge as failed and call itself for inner dependencies
                        BadgeDependenciesAsFailures(castRetry.Dependencies);
                    }

                    // did not cancel out this FileChange as one which exceeded the max count of NotFound attempts, so there were no relavent dependencies
                    NotFoundDependenciesToTry = null;
                }

                // stop retrying the FileChange via the FailureQueue
                return false;

            }
        }

        // recursive function which takes an enumeration of FileChanges to mark as failed along with their inner dependencies (via recursion)
        private static void BadgeDependenciesAsFailures(IEnumerable<FileChange> dependencies)
        {
            // loop through the current level of dependencies
            foreach (FileChange dependency in (dependencies ?? Enumerable.Empty<FileChange>()))
            {
                // mark the current FileChange as failed
                MessageEvents.SetPathState(dependency, // event source (the event itself)
                    new SetBadge(PathState.Failed, // mark as failed
                        ((dependency.Direction == SyncDirection.From && dependency.Type == FileChangeType.Renamed)
                            ? dependency.OldPath // if the current FileChange is a Sync From rename, then the current local location is the previous path
                            : dependency.NewPath))); // else if the current FileChange is not a Sync From rename, then use the current location

                // try cast the current FileChange as one with dependencies
                FileChangeWithDependencies castDependency = dependency as FileChangeWithDependencies;
                // if the current FileChange was one with dependencies and has at least one dependency, then recurse on this function to badge inner dependencies as failed
                if (castDependency != null
                    && castDependency.DependenciesCount > 0)
                {
                    BadgeDependenciesAsFailures(castDependency.Dependencies);
                }
            }
        }

        #endregion

        // the current purpose of the UpDownEvent is to fire the event to retrieve all FileChanges which are currently in the process of uploading or downloading,
        // such as for searching them for latest metadata for rename; another functionality TODO is to atomically perform renames with uploads/downloads so they do not conflict
        #region FileChange up-down events
        // attach a FileChange's UpDownEvent handler to the local UpDownEvent
        private void AddFileChangeToUpDownEvent(FileChange toAdd)
        {
            // lock on local UpDownEvent locker for modification of local UpDownEvent
            lock (UpDownEventLocker)
            {
                // Add the current FileChange's UpDown handler to the local UpDownEvent
                UpDownEvent += toAdd.FileChange_UpDown;
            }
        }
        // remove a FileChange's UpDownEvent handler from the local UpDownEvent
        private void RemoveFileChangeFromUpDownEvent(FileChange toRemove)
        {
            // lock on local UpDownEvent locker for modification of local UpDownEvent
            lock (UpDownEventLocker)
            {
                // Remove the current FileChange's UpDown handler from the local UpDownEvent
                UpDownEvent -= toRemove.FileChange_UpDown;
            }
        }
        // fire the UpDown event to receive the provided callback (passed in the provided event args on construction) for each FileChange subscribed to the event
        private void RunUpDownEvent(FileChange.UpDownEventArgs callback, object userState)
        {
            // lock on local UpDownEvent locker for firing local UpDownEvent
            lock (UpDownEventLocker)
            {
                // if there are any FileChanges subscribed to the UpDown event then fire the event
                if (UpDownEvent != null)
                {
                    UpDownEvent(userState, callback);
                }
            }
        }
        // the UpDown event itself, one event for each SyncEngine (hopefully a FileChange won't be subscribed to more than one of these events)
        private event EventHandler<FileChange.UpDownEventArgs> UpDownEvent;
        // lock for modification or firing the UpDown event
        private readonly object UpDownEventLocker = new object();
        #endregion
    }

    /// <summary>
    /// Delegate to FileTransferStatusUpdate in SyncEngine which fires status change callbacks to CLSyncEngine
    /// </summary>
    /// <param name="threadId">Unique id of the running thread calling back for status change</param>
    /// <param name="eventId">The unique ID of the event being transferred</param>
    /// <param name="direction">Direction of the event (To the server of From the server)</param>
    /// <param name="relativePath">Relative path of the file being transferred</param>
    /// <param name="byteProgress">Bytes already transferred for the file</param>
    /// <param name="totalByteSize">Total byte size of the file</param>
    /// <param name="isError">Whether or not the transfer has errored out</param>
    internal delegate void FileTransferStatusUpdateDelegate(Guid threadId, long eventId, SyncDirection direction, string relativePath, long byteProgress, long totalByteSize, bool isError);
}