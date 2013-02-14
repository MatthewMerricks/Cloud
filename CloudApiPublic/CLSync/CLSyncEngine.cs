//
// CLSyncEngine.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.FileMonitor;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using CloudApiPublic.SQLIndexer;
using CloudApiPublic.PushNotification;
using CloudApiPublic.Static;
using CloudApiPublic.Sync;
using CloudApiPublic.BadgeNET;
using CloudApiPublic.REST;
using System.Collections;

namespace CloudApiPublic
{
    /// <summary>
    /// A class used to create a SyncBox to synchronize the contents of a local disk directory.
    /// </summary>
    public sealed class CLSyncEngine
    {
        private IconOverlay _iconOverlay = null;
        private MonitorAgent _monitor = null;
        private IndexingAgent _indexer = null;
        private CLNotification _notifier = null;
        private bool _isStarted = false;
        private static CLTrace _trace = CLTrace.Instance;
        private SyncEngine _syncEngine = null;
        private static readonly HashSet<SyncEngine> NetworkMonitoredEngines = new HashSet<SyncEngine>();
        private System.Threading.WaitCallback statusUpdated = null;
        private object statusUpdatedUserState = null;
        private readonly object _locker = new object();

        //// Not sure if we want to expose access to the contained SyncBox since it is set on Start and not on construction (it changes during usage)s
        //
        ///// <summary>
        ///// Retrieves a currently attached SyncBox, or null if one isn't attached
        ///// </summary>
        //public CLSyncBox SyncBox
        //{
        //    get
        //    {
        //        lock (_locker)
        //        {
        //            return _syncBox;
        //        }
        //    }
        //}
        private CLSyncBox _syncBox = null;
        /// <summary>
        /// Event fired when a serious notification error has occurred.  Push notification is
        /// no longer functional.
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs> PushNotificationError;

        /// <summary>
        /// Output the current status of syncing
        /// </summary>
        /// <param name="status">(output) Current status of syncing</param>
        /// <returns>Returns any error which occurred in retrieving the sync status, if any</returns>
        public CLError GetEngineCurrentStatus(out CLSyncCurrentStatus status)
        {
            try
            {
                lock (_locker)
                {
                    if (_syncEngine == null)
                    {
                        //throw new NullReferenceException("Sync not started");
                        status = new CLSyncCurrentStatus(CLSyncCurrentState.Idle, null);
                        return null;
                    }
                    else
                    {
                        return _syncEngine.GetCurrentStatus(out status);
                    }
                }
            }
            catch (Exception ex)
            {
                status = Helpers.DefaultForType<CLSyncCurrentStatus>();
                return ex;
            }
        }

        /// <summary>
        /// Writes a new set of sync states to the database after a sync completes,
        /// requires newRootPath to be set on the first sync or on any sync with a new root path
        /// </summary>
        /// <param name="syncId">New sync Id from server</param>
        /// <param name="syncedEventIds">Enumerable of event ids processed in sync</param>
        /// <param name="syncCounter">Output sync counter local identity</param>
        /// <returns>Returns an error that occurred during recording the sync, if any</returns>
        public CLError RecordCompletedSync(string syncId, IEnumerable<long> syncedEventIds, out long syncCounter)
        {
            lock (_locker)
            {
                if (_monitor != null)
                {
                    try
                    {
                        return _monitor.SyncData.RecordCompletedSync(syncId, syncedEventIds, out syncCounter, _monitor.GetCurrentPath());
                    }
                    catch (Exception ex)
                    {
                        CLError error = ex;
                        error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
                        syncCounter = Helpers.DefaultForType<long>();
                        return ex;
                    }
                }
                else
                {
                    syncCounter = Helpers.DefaultForType<long>();
                    return new NullReferenceException("Monitor cannot be null");
                }
            }
        }

        /// <summary>
        /// ¡¡ Call this carefully, completely wipes index database (use when user deletes local repository or relinks) !!
        /// </summary>
        /// <param name="newRootPath">Full path string to directory to sync without any trailing slash (except for drive letter root)</param>
        /// <returns>Returns any error that occurred while wiping the database index</returns>
        public CLError WipeIndex()
        {
            lock (_locker)
            {
                if (_monitor != null)
                {
                    try
                    {
                        return _monitor.SyncData.WipeIndex(_monitor.GetCurrentPath());
                    }
                    catch (Exception ex)
                    {
                        CLError error = ex;
                        error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
                        return ex;
                    }
                }
                else
                {
                    return new NullReferenceException("Monitor cannot be null");
                }
            }
        }

        /// <summary>
        /// ¡¡ Do not use this method. Besides just completely wiping the index database, this also removes the database file which may be important for tracing/debugging; instead use WipeIndex !!
        /// </summary>
        /// <param name="syncBox">SyncBox to reset</param>
        /// <returns>Returns any error that occurred deleting the index database file, if any</returns>
        public CLError SyncReset(CLSyncBox syncBox)
        {
            try
            {
                if (syncBox == null)
                {
                    throw new NullReferenceException("syncBox cannot be null");
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(syncBox.CopiedSettings.TraceLocation, "Cloud", "log", syncBox.CopiedSettings.TraceLevel, syncBox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncEngine: SyncReset: Entry.");

                CLError checkBadPath = Helpers.CheckForBadPath(syncBox.CopiedSettings.SyncRoot);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, "CLSyncEngine: SyncReset: ERROR: {0}.", checkBadPath.errorDescription);
                    return new ArgumentException("CloudRoot in settings represents a bad path, check it first via Helpers.CheckForBadPath", checkBadPath.GrabFirstException());
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncRootLength(syncBox.CopiedSettings.SyncRoot, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, "CLSyncEngine: SyncReset: ERROR: {0}.", checkPathLength.errorDescription);
                    return new ArgumentException("CloudRoot in settings is too long, check it first via Helpers.CheckSyncRootLength", checkPathLength.GrabFirstException());
                }

                // Determine the database file with full path
                string sDatabaseDirectoryToUse = (string.IsNullOrEmpty(syncBox.CopiedSettings.DatabaseFolder)
                    ? Helpers.GetDefaultDatabasePath(syncBox.CopiedSettings.DeviceId, syncBox.SyncBoxId)
                    : syncBox.CopiedSettings.DatabaseFolder.Trim());
                string sDatabaseFile = sDatabaseDirectoryToUse + "\\" + CLDefinitions.kSyncDatabaseFileName;

                // Delete the database file
                if (System.IO.File.Exists(sDatabaseFile))
                {
                    System.IO.File.Delete(sDatabaseFile);
                }

                // Delete the temp download directory recursively, but not the directory itself.
                string sTempDownloadFolderToUse = Helpers.GetTempFileDownloadPath(syncBox.CopiedSettings, syncBox.SyncBoxId);
                CLError errorFromDelete = Helpers.DeleteEverythingInDirectory(sTempDownloadFolderToUse);
                if (errorFromDelete != null)
                {
                    // Just trace this error
                    _trace.writeToLog(1, "CLSyncEngine: SyncReset: ERROR: From DeleteEverythingInDirectory.  Message: {0}.", errorFromDelete.errorDescription);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(syncBox.CopiedSettings.TraceLocation, syncBox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncEngine: SyncReset: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                return ex;
            }

            return null;
        }

        ///// <summary>
        ///// Simplest initialization of a SyncBox to start syncing its contents to the Cloud server, and to other devices registering the same SyncBox.
        ///// </summary>
        ///// <param name="ApplicationKey">The public key that identifies this application.</param>
        ///// <param name="ApplicationSecret">The application secret private key.</param>
        ///// <param name="SyncBoxId">The unique ID of this SyncBox assigned by the auth server.</param>
        ///// <param name="SyncRoot">Full path to the directory to be synced (do not include a trailing slash except for a drive root)</param>
        ///// <param name="Status">(output) State of starting SyncBox, check this to make sure it was successful</param>
        ///// <param name="StatusUpdated">(optional) Callback to fire whenever the status of the SyncBox has been updated</param>
        ///// <param name="StatusUpdatedUserState">(optional) Userstate to pass when firing the statusUpdated callback</param>
        ///// <returns>Returns any error which occurred starting the SyncBox</returns>
        //public CLError Start(string ApplicationKey,
        //    string ApplicationSecret,
        //    long SyncBoxId,
        //    string SyncRoot,
        //    out CLSyncStartStatus Status,
        //    System.Threading.WaitCallback StatusUpdated = null,
        //    object StatusUpdatedUserState = null)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(ApplicationKey)
        //            || string.IsNullOrEmpty(ApplicationSecret))
        //        {
        //            const string settingsError = "settings cannot be null";
        //            _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", settingsError);
        //            Status = CLSyncStartStatus.ErrorMissingSettings;
        //            return new NullReferenceException(settingsError);
        //        }

        //        return Start(
        //            new CLSyncSettings(
        //                Helpers.GetComputerFriendlyName() + Guid.NewGuid().ToString("N"),
        //                ApplicationKey,
        //                ApplicationSecret,
        //                SyncBoxId,
        //                "SimpleWinClient01",
        //                SyncRoot),
        //            out Status,
        //            StatusUpdated,
        //            StatusUpdatedUserState);
        //    }
        //    catch (Exception ex)
        //    {
        //        CLError error = ex;
        //        error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
        //        _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Exception(7).  Msg: <{0}>.", ex.Message);
        //        ReleaseResources();
        //        Status = CLSyncStartStatus.ErrorGeneralSyncStartException;
        //        return ex;
        //    }
        //}

        /// <summary>
        /// Start the SyncEngine with a SyncBox to start syncing contents to the Cloud server, and to other devices registering the same SyncBox.
        /// </summary>
        /// <param name="SyncBox">SyncBox to sync</param>
        /// <param name="Status">(output) State of starting SyncEngine, check this to make sure it was successful</param>
        /// <param name="StatusUpdated">(optional) Callback to fire whenever the status of the SyncEngine has been updated</param>
        /// <param name="StatusUpdatedUserState">(optional) Userstate to pass when firing the statusUpdated callback</param>
        /// <returns>Returns any error which occurred starting to sync, if any</returns>
        public CLError Start(
			CLSyncBox SyncBox,
			out CLSyncStartStatus Status,
			System.Threading.WaitCallback StatusUpdated = null,
			object StatusUpdatedUserState = null)
        {
            bool reservedSyncBox = false;
            try
            {
                if (SyncBox == null)
                {
                    const string settingsError = "syncBox cannot be null";
                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", settingsError);
                    Status = CLSyncStartStatus.ErrorNullSyncBox;
                    return new NullReferenceException(settingsError);
                }

                if (!SyncBox.TryReserveForActiveSync())
                {
                    const string modificationError = "syncBox cannot be modifying server SyncBox via public API calls (i.e. DeleteSyncBox or SyncBoxUpdateQuota)";
                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", modificationError);
                    Status = CLSyncStartStatus.ErrorInProcessOfModification;
                    return new ArgumentException(modificationError);
                }

                reservedSyncBox = true;

                if (string.IsNullOrEmpty(SyncBox.CopiedSettings.DeviceId))
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    const string settingsError = "syncBox CopiedSettings DeviceId cannot be null";
                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", settingsError);
                    Status = CLSyncStartStatus.ErrorNullDeviceId;
                    return new NullReferenceException(settingsError);
                }

                lock (_locker)
                {
                    this._syncBox = SyncBox;
                    this.statusUpdated = StatusUpdated;
                    if (StatusUpdated == null)
                    {
                        this.statusUpdatedUserState = null;
                    }
                    else
                    {
                        this.statusUpdatedUserState = StatusUpdatedUserState;
                    }
                }

                // Check the TraceLocation vs. LogErrors
                if (string.IsNullOrWhiteSpace(this._syncBox.CopiedSettings.TraceLocation) && this._syncBox.CopiedSettings.LogErrors)
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    const string verifyTraceError = "TraceLocation must be set if LogErrors is checked";
                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", verifyTraceError);
                    Status = CLSyncStartStatus.ErrorTraceEnabledWithoutDirectory;
                    return new ArgumentException(verifyTraceError);
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(this._syncBox.CopiedSettings.TraceLocation, "Cloud", "log", this._syncBox.CopiedSettings.TraceLevel, this._syncBox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncEngine: Starting...");

                CLError checkBadPath = Helpers.CheckForBadPath(this._syncBox.CopiedSettings.SyncRoot);
                if (checkBadPath != null)
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", checkBadPath.errorDescription);
                    Status = CLSyncStartStatus.ErrorBadRootPath;
                    return new ArgumentException("CloudRoot in settings represents a bad path, check it first via Helpers.CheckForBadPath", checkBadPath.GrabFirstException());
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncRootLength(this._syncBox.CopiedSettings.SyncRoot, out tooLongChars);
                if (checkPathLength != null)
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", checkPathLength.errorDescription);
                    Status = CLSyncStartStatus.ErrorLongRootPath;
                    return new ArgumentException("CloudRoot in settings is too long, check it first via Helpers.CheckSyncRootLength", checkPathLength.GrabFirstException());
                }

                // Don't start twice.
                _trace.writeToLog(1, "CLSyncEngine: Start: Entry.");
                if (_isStarted)
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    CLError error = new Exception("Already started");
                    _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: {0}.", error.errorDescription);
                    Status = CLSyncStartStatus.ErrorAlreadyStarted;
                    return error;
                }

                // Create the SyncBox directory if it doesn't exist
                bool alreadyExists = true;
                System.IO.DirectoryInfo rootInfo = new System.IO.DirectoryInfo(_syncBox.CopiedSettings.SyncRoot);
                if (!rootInfo.Exists)
                {
                    alreadyExists = false;
                    rootInfo.Create();
                }

                bool caseMatches;
                CLError caseCheckError = Helpers.DirectoryMatchesCaseWithDisk(_syncBox.CopiedSettings.SyncRoot,
                    out caseMatches);

                if (caseCheckError != null)
                {
                    _trace.writeToLog(1, "CLSyncEngine: ERROR: {0}.", checkBadPath.errorDescription);
                    Status = CLSyncStartStatus.ErrorBadRootPath;
                    return new ArgumentException("CloudRoot in settings represents a path which cannot be queried, check it first via Helpers.DirectoryMatchesCaseWithDisk", caseCheckError.GrabFirstException());
                }

                if (!caseMatches)
                {
                    const string badCaseErrorCreated = "A new directory was created on disk at the specified settings SyncRoot, but its resulting path does not match case";
                    const string badCaseErrorExists = "An existing directory was found at the specified settings SyncRoot, but its path does not match case";
                    _trace.writeToLog(1, "CLSyncEngine: ERROR: BadCase (1). {0}.", (alreadyExists ? badCaseErrorExists : badCaseErrorCreated));
                    Status = CLSyncStartStatus.ErrorBadRootPath;
                    if (alreadyExists)
                    {
                        return new Exception(badCaseErrorExists);
                    }
                    else
                    {
                        return new Exception(badCaseErrorCreated);
                    }
                }
				
                // Start badging
                lock (_locker)
                {
                    _iconOverlay = new IconOverlay();
                }
                CLError iconOverlayError = _iconOverlay.Initialize(this._syncBox.CopiedSettings, this._syncBox.SyncBoxId);
                if (iconOverlayError != null)
                {
                    // Failure to start badging does not prevent syncing.  Just log it.
                    _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Exception. Msg: {0}. Code: {1}.", iconOverlayError.errorDescription, ((int)iconOverlayError.code).ToString());
                }

                // Start the indexer.
                _trace.writeToLog(9, "CLSyncEngine: Start: Start the indexer.");
                CLError indexCreationError;
                lock (_locker)
                {
                    indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer, this._syncBox);
                }
                if (indexCreationError != null)
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Exception(2). Msg: {0}. Code: {1}.", indexCreationError.errorDescription, ((int)indexCreationError.code).ToString());
                    ReleaseResources();
                    Status = CLSyncStartStatus.ErrorIndexCreation;
                    return indexCreationError;
                }

                // Start the push notification.
                _trace.writeToLog(9, "CLSyncEngine: Start: Start the notifier.");
                lock (_locker)
                {
                    CLError getNotificationError = CLNotification.GetInstance(this._syncBox, out _notifier);
                    if (getNotificationError != null
                        || _notifier == null)
                    {
                        if (reservedSyncBox
                            && SyncBox != null)
                        {
                            SyncBox.ResetReserveForActiveSync();
                        }

                        CLError error;
                        if (getNotificationError == null)
                        {
                            error = new Exception("Error starting push notification");
                        }
                        else
                        {
                            error = new AggregateException("An error occurred starting push notification", getNotificationError.GrabExceptions());
                        }
                        _trace.writeToLog(1, "CLSyncEngine: Start: ERROR(2): {0}.", error.errorDescription);
                        ReleaseResources();
                        Status = CLSyncStartStatus.ErrorStartingNotification;
                        return error;
                    }
                }

                // Hook up the events
                _trace.writeToLog(9, "CLSyncEngine: Start: Hook up events.");
                _notifier.NotificationReceived += OnNotificationReceived;
                _notifier.NotificationStillDisconnectedPing += OnNotificationPerformManualSyncFrom;
                _notifier.ConnectionError += OnNotificationConnectionError;

                // Start the monitor
                CLError fileMonitorCreationError;
                lock (_locker)
                {
                    fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(this._syncBox,
                        _indexer,
                        this._syncBox.HttpRestClient,
                        statusUpdated,
                        statusUpdatedUserState,
                        out _monitor,
                        out _syncEngine);
                }

                if (fileMonitorCreationError != null)
                {
                    if (reservedSyncBox
                        && SyncBox != null)
                    {
                        SyncBox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1, "CLSyncEngine: Start: ERROR(4): Msg: {0}. Code: {1}.", fileMonitorCreationError.errorDescription, ((int)fileMonitorCreationError.code).ToString());
                    lock (_locker)
                    {
                        _indexer.Dispose();
                        _indexer = null;
                    }
                    ReleaseResources();
                    Status = CLSyncStartStatus.ErrorCreatingFileMonitor;
                    return fileMonitorCreationError;
                }
                else
                {
                    lock (_locker)
                    {
                        lock (NetworkMonitoredEngines)
                        {
                            bool startNetworkMonitor = NetworkMonitoredEngines.Count == 0;

                            if (NetworkMonitoredEngines.Add(_syncEngine))
                            {
                                if (startNetworkMonitor)
                                {
                                    NetworkMonitor.Instance.NetworkChanged += NetworkChanged;
                                    NetworkMonitor.Instance.StartNetworkMonitor();
                                    SyncEngine.InternetConnected = NetworkMonitor.Instance.CheckInternetIsConnected();
                                }
                            }
                        }

                        if (_monitor != null)
                        {
                            try
                            {
                                MonitorStatus returnStatus;
                                CLError fileMonitorStartError = _monitor.Start(out returnStatus);
                                if (fileMonitorStartError != null)
                                {
                                    if (reservedSyncBox
                                        && SyncBox != null)
                                    {
                                        SyncBox.ResetReserveForActiveSync();
                                    }

                                    _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Starting the MonitorAgent.  Msg: <{0}>. Code: {1}.", fileMonitorStartError.errorDescription, ((int)fileMonitorStartError.code).ToString());
                                    ReleaseResources();
                                    Status = CLSyncStartStatus.ErrorStartingFileMonitor;
                                    return fileMonitorStartError;
                                }

                                CLError indexerStartError = _indexer.StartInitialIndexing(
                                                _monitor.BeginProcessing,
                                                _monitor.GetCurrentPath);
                                if (indexerStartError != null)
                                {
                                    if (reservedSyncBox
                                        && SyncBox != null)
                                    {
                                        SyncBox.ResetReserveForActiveSync();
                                    }

                                    _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Starting the initial indexing.  Msg: <{0}>. Code: {1}.", indexerStartError.errorDescription, ((int)indexerStartError.code).ToString());
                                    ReleaseResources();
                                    Status = CLSyncStartStatus.ErrorStartingInitialIndexing;
                                    return indexerStartError;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (reservedSyncBox
                                    && SyncBox != null)
                                {
                                    SyncBox.ResetReserveForActiveSync();
                                }

                                CLError error = ex;
                                error.LogErrors(this._syncBox.CopiedSettings.TraceLocation, this._syncBox.CopiedSettings.LogErrors);
                                _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Exception(5).  Msg: <{0}>.", ex.Message);
                                ReleaseResources();
                                Status = CLSyncStartStatus.ErrorExceptionStartingFileMonitor;
                                return ex;
                            }
                        }
                    }
                }

                Status = CLSyncStartStatus.Success;
            }
            catch (Exception ex)
            {
                if (reservedSyncBox
                    && SyncBox != null)
                {
                    SyncBox.ResetReserveForActiveSync();
                }

                CLError error = ex;
                error.LogErrors(this._syncBox.CopiedSettings.TraceLocation, this._syncBox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Exception(6).  Msg: <{0}>.", ex.Message);
                ReleaseResources();
                Status = CLSyncStartStatus.ErrorGeneralSyncStartException;
                return ex;
            }

            return null;
        }

        /// <summary>
        /// A serious notification error has occurred.  Push notification is no longer functioning.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Arguments including the manual poll and/or web sockets errors (possibly aggregated).</param>
        private void OnNotificationConnectionError(object sender, NotificationErrorEventArgs e)
        {
            // Tell the application
            _trace.writeToLog(1, "CLSyncEngine: OnConnectionError: Entry. ERROR: Manual poll error: <{0}>. Web socket error: <{1}>.", e.ErrorStillDisconnectedPing.errorDescription, e.ErrorWebSockets.errorDescription);
            if (PushNotificationError != null)
            {
                _trace.writeToLog(1, "CLSyncEngine: OnConnectionError: Notify the application.");
                PushNotificationError(this, e);
            }
        }

        /// <summary>
        /// Notification says to send a manual Sync_From request to the server.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Event arguments containing the push notification message.</param>
        private void OnNotificationPerformManualSyncFrom(object sender, NotificationEventArgs e)
        {
            // Pass this event on to the file monitor.
            lock (_locker)
            {
                if (_monitor != null)
                {
                    _trace.writeToLog(9, "CLSyncEngine: OnNotificationPerformManualSyncFrom: Send a Perform Manual SyncFrom to monitor.");
                    _monitor.PushNotification(e.Message);
                }
            }
        }

        /// <summary>
        /// The notifier received a push notification message from the server.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Arguments containing the push notification message received.</param>
        private void OnNotificationReceived(object sender, NotificationEventArgs e)
        {
            // Let the file monitor know about this event.
            lock (_locker)
            {
                if (_monitor != null)
                {
                    _trace.writeToLog(9, "CLSyncEngine: OnNotificationPerformManualSyncFrom: Send a Perform PushNotification to monitor.");
                    _monitor.PushNotification(e.Message);
                }
            }
        }

        /// <summary>
        /// Stop syncing the SyncBox, and free all resources.
        /// </summary>
        private CLError ReleaseResources()
        {
            CLError toReturn = null;

            CLSyncBox storeSyncBox;

            lock (_locker)
            {
                if (_monitor != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Stop the file monitor.");
                        MonitorStatus monitorIsStopped;
                        toReturn = _monitor.Stop(out monitorIsStopped);
                        _monitor.Dispose();
                        _monitor = null;
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: File monitor stopped.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                if (_notifier != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Disconnect PushNotificationServer.");
                        _notifier.DisconnectPushNotificationServer();
                        _notifier = null;
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: PushNotificationServer disconnected.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                if (_indexer != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Stop the Indexer.");
                        _indexer.Dispose();
                        _indexer = null;
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Indexer stopped.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                if (_syncEngine != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Stop the sync engine.");
                        _syncEngine.Dispose();

                        // detach sync engine from network monitoring
                        lock (NetworkMonitoredEngines)
                        {
                            if (NetworkMonitoredEngines.Remove(_syncEngine))
                            {
                                if (NetworkMonitoredEngines.Count == 0)
                                {
                                    NetworkMonitor.Instance.NetworkChanged -= NetworkChanged;
                                    NetworkMonitor.Instance.StopNetworkMonitor();
                                }
                            }
                        }

                        _syncEngine = null;
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Sync engine stopped.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                if (_iconOverlay != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: Stop IconOverlay.");
                        _iconOverlay.Shutdown();
                        _iconOverlay = null;
                        _trace.writeToLog(9, "CLSyncEngine: ReleaseResources: IconOverlay stopped.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                if (statusUpdated != null)
                {
                    try
                    {
                        statusUpdated = null;
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                if (statusUpdatedUserState != null)
                {
                    try
                    {
                        statusUpdatedUserState = null;
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }

                storeSyncBox = _syncBox;
            }

            if (toReturn != null)
            {
                toReturn.LogErrors(storeSyncBox.CopiedSettings.TraceLocation, storeSyncBox.CopiedSettings.LogErrors);
            }

            lock (_locker)
            {
                _syncBox = null; // set this to null after logging errors (which requires the settings)
            }
            return toReturn;
        }

        private static void NetworkChanged(object sender, NetworkChangedEventArgs e)
        {
            SyncEngine.InternetConnected = e.IsConnected;
        }

        /// <summary>
        /// Stop syncing the SyncBox and free resources.
        /// </summary>
        public void Stop()
        {
            lock (_locker)
            {
                if (this._syncBox != null)
                {
                    this._syncBox.ResetReserveForActiveSync();
                }
            }

            _trace.writeToLog(1, "CLSyncEngine: Stop: Entry.");
            ReleaseResources();
        }

        /// <summary>
        /// Call when application is shutting down.
        /// </summary>
        public static void Shutdown()
        {
            // Shuts down the HttpScheduler; after shutdown it cannot be used again
            HttpScheduler.DisposeBothSchedulers();

            // Shuts down the sync FileChange delay processing
            DelayProcessable<FileChange>.TerminateAllProcessing();

            // Stops network change monitoring
            NetworkMonitor.DisposeInstance();
        }
    }
}