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
using Cloud.FileMonitor;
using Cloud.Interfaces;
using Cloud.Model;
using Cloud.Support;
using Cloud.SQLIndexer;
using Cloud.PushNotification;
using Cloud.Static;
using Cloud.Sync;
using Cloud.BadgeNET;
using Cloud.REST;
using System.Collections;

namespace Cloud
{
    /// <summary>
    /// A class used to create a Syncbox to synchronize the contents of a local disk directory.
    /// </summary>
    public sealed class CLSyncEngine
    {
        private IconOverlay _iconOverlay = null;
        private MonitorAgent _monitor = null;
        private IndexingAgent _indexer = null;
        private CLNotificationService _notifier = null;
        private bool _isStarted = false;
        private static CLTrace _trace = CLTrace.Instance;
        private SyncEngine _syncEngine = null;
        private static readonly HashSet<SyncEngine> NetworkMonitoredEngines = new HashSet<SyncEngine>();
        private System.Threading.WaitCallback statusUpdated = null;
        private object statusUpdatedUserState = null;
        private readonly object _locker = new object();

        // following flag should always be false except for when debugging dependencies
        private readonly GenericHolder<bool> debugDependencies = new GenericHolder<bool>(false);

        #region hidden Dependencies debug
        //// --------- adding \cond and \endcond makes the section in between hidden from doxygen

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool DependenciesDebug
        {
            get
            {
                lock (debugDependencies)
                {
                    return debugDependencies.Value;
                }
            }
            set
            {
                lock (debugDependencies)
                {
                    debugDependencies.Value = value;
                }
            }
        }
        // \endcond
        #endregion

        // following flag should always be false except for when debugging FileMonitor memory
        private readonly GenericHolder<bool> debugFileMonitorMemory = new GenericHolder<bool>(false);

        #region hidden FileMonitor debug
        //// --------- adding \cond and \endcond makes the section in between hidden from doxygen

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool FileMonitorMemoryDebug
        {
            get
            {
                lock (debugFileMonitorMemory)
                {
                    return debugFileMonitorMemory.Value;
                }
            }
            set
            {
                lock (debugFileMonitorMemory)
                {
                    if (debugFileMonitorMemory.Value
                        && !value)
                    {
                        FileMonitor.MonitorAgent.memoryDebugger.Instance.wipeMemory();
                    }

                    debugFileMonitorMemory.Value = value;
                }
            }
        }
        // \endcond

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string FileMonitorMemory
        {
            get
            {
                lock (debugFileMonitorMemory)
                {
                    if (!debugFileMonitorMemory.Value)
                    {
                        return null;
                    }
                }

                return FileMonitor.MonitorAgent.memoryDebugger.Instance.serializeMemory();
            }
        }
        // \endcond

        // \cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool WipeFileMonitorDebugMemory
        {
            set
            {
                if (value)
                {
                    bool needsWipe;

                    lock (debugFileMonitorMemory)
                    {
                        needsWipe = debugFileMonitorMemory.Value;
                    }

                    if (needsWipe)
                    {
                        FileMonitor.MonitorAgent.memoryDebugger.Instance.wipeMemory();
                    }
                }
            }
        }
        // \endcond
        #endregion

        //// Not sure if we want to expose access to the contained Syncbox since it is set on Start and not on construction (it changes during usage)s
        //
        ///// <summary>
        ///// Retrieves a currently attached Syncbox, or null if one isn't attached
        ///// </summary>
        //public CLSyncbox Syncbox
        //{
        //    get
        //    {
        //        lock (_locker)
        //        {
        //            return _syncbox;
        //        }
        //    }
        //}
        private CLSyncbox _syncbox = null;
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
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

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
        /// Queries database by eventId to return latest metadata and path as a FileChange and whether or not the event is still pending
        /// </summary>
        /// <param name="eventId">EventId key to lookup</param>
        /// <param name="queryResult">(output) Result FileChange from EventId lookup</param>
        /// <param name="isPending">(output) Result whether event is pending from EventId lookup</param>
        /// <param name="status">(output) Status of quering the database</param>
        /// <returns>Returns any error which occurred querying the database, if any</returns>
        public CLError QueryFileChangeByEventId(long eventId, out FileChange queryResult, out bool isPending, out FileChangeQueryStatus status)
        {
            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                lock (_locker)
                {
                    if (_indexer == null)
                    {
                        queryResult = Helpers.DefaultForType<FileChange>();
                        isPending = Helpers.DefaultForType<bool>();
                        status = FileChangeQueryStatus.ErrorNoIndexer;
                        return new NullReferenceException(Resources.CLSyncEngineIndexerCannotBeNull);
                    }
                    else
                    {
                        return _indexer.QueryFileChangeByEventId(eventId, out queryResult, out isPending, out status);
                    }
                }
            }
            catch (Exception ex)
            {
                queryResult = Helpers.DefaultForType<FileChange>();
                isPending = Helpers.DefaultForType<bool>();
                status = FileChangeQueryStatus.ErrorUnknown;
                return ex;
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
                        if (Helpers.AllHaltedOnUnrecoverableError)
                        {
                            throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                        }

                        return _monitor.SyncData.WipeIndex(_monitor.GetCurrentPath());
                    }
                    catch (Exception ex)
                    {
                        CLError error = ex;
                        error.Log(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.LogErrors);
                        return ex;
                    }
                }
                else
                {
                    return new NullReferenceException(Resources.CLSyncEngineMonitorCannotBeNull);
                }
            }
        }

        /// <summary>
        /// ¡¡ Do not use this method. Besides just completely wiping the index database, this also removes the database file which may be important for tracing/debugging; instead use WipeIndex !!
        /// </summary>
        /// <param name="syncbox">Syncbox to reset</param>
        /// <returns>Returns any error that occurred deleting the index database file, if any</returns>
        public CLError SyncReset(CLSyncbox syncbox)
        {
            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                if (syncbox == null)
                {
                    throw new NullReferenceException(Resources.CLEngineSyncboxCannotBeNull);
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(syncbox.CopiedSettings.TraceLocation, "Cloud", "log", syncbox.CopiedSettings.TraceLevel, syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineSyncResetEntry);

                CLError checkBadPath = Helpers.CheckForBadPath(syncbox.CopiedSettings.SyncRoot);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineResetError0, checkBadPath.PrimaryException.Message);
                    return new ArgumentException(Resources.CLSyncEngineCloudRootRepsBadPath, checkBadPath.PrimaryException);
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncRootLength(syncbox.CopiedSettings.SyncRoot, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineResetError0, checkPathLength.PrimaryException.Message);
                    return new ArgumentException(Resources.CLSyncEngineCloudRootSettingsTooLong, checkPathLength.PrimaryException);
                }

                // Determine the database file with full path
                string sDatabaseDirectoryToUse = (string.IsNullOrEmpty(syncbox.CopiedSettings.DatabaseFolder)
                    ? Helpers.GetDefaultDatabasePath(syncbox.CopiedSettings.DeviceId, syncbox.SyncboxId)
                    : syncbox.CopiedSettings.DatabaseFolder.Trim());
                string sDatabaseFile = sDatabaseDirectoryToUse + "\\" + CLDefinitions.kSyncDatabaseFileName;

                // Delete the database file
                if (System.IO.File.Exists(sDatabaseFile))
                {
                    System.IO.File.Delete(sDatabaseFile);
                }

                // Delete the temp download directory recursively, but not the directory itself.
                string sTempDownloadFolderToUse = Helpers.GetTempFileDownloadPath(syncbox.CopiedSettings, syncbox.SyncboxId);
                CLError errorFromDelete = Helpers.DeleteEverythingInDirectory(sTempDownloadFolderToUse);
                if (errorFromDelete != null)
                {
                    // Just trace this error
                    _trace.writeToLog(1, Resources.CLSyncEngineSyncResetDeleteEverythingInDirMessage0, errorFromDelete.PrimaryException.Message);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(syncbox.CopiedSettings.TraceLocation, syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineSyncResetERRORExceptionMsg0, ex.Message);
                return ex;
            }

            return null;
        }

        ///// <summary>
        ///// Simplest initialization of a Syncbox to start syncing its contents to the Cloud server, and to other devices registering the same Syncbox.
        ///// </summary>
        ///// <param name="ApplicationKey">The public key that identifies this application.</param>
        ///// <param name="ApplicationSecret">The application secret private key.</param>
        ///// <param name="SyncboxId">The unique ID of this Syncbox assigned by the auth server.</param>
        ///// <param name="SyncRoot">Full path to the directory to be synced (do not include a trailing slash except for a drive root)</param>
        ///// <param name="Status">(output) State of starting Syncbox, check this to make sure it was successful</param>
        ///// <param name="StatusUpdated">(optional) Callback to fire whenever the status of the Syncbox has been updated</param>
        ///// <param name="StatusUpdatedUserState">(optional) Userstate to pass when firing the statusUpdated callback</param>
        ///// <returns>Returns any error which occurred starting the Syncbox</returns>
        //public CLError Start(string ApplicationKey,
        //    string ApplicationSecret,
        //    long SyncboxId,
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
        //                SyncboxId,
        //                "SimpleWinClient01",
        //                SyncRoot),
        //            out Status,
        //            StatusUpdated,
        //            StatusUpdatedUserState);
        //    }
        //    catch (Exception ex)
        //    {
        //        CLError error = ex;
        //        error.Log(_syncSettings.TraceLocation, _syncSettings.LogErrors);
        //        _trace.writeToLog(1, "CLSyncEngine: Start: ERROR: Exception(7).  Msg: <{0}>.", ex.Message);
        //        ReleaseResources();
        //        Status = CLSyncStartStatus.ErrorGeneralSyncStartException;
        //        return ex;
        //    }
        //}

        /// <summary>
        /// Start the SyncEngine with a Syncbox to start syncing contents to the Cloud server, and to other devices registering the same Syncbox.
        /// </summary>
        /// <param name="Syncbox">Syncbox to sync</param>
        /// <param name="Status">(output) State of starting SyncEngine, check this to make sure it was successful</param>
        /// <param name="StatusUpdated">(optional) Callback to fire whenever the status of the SyncEngine has been updated</param>
        /// <param name="StatusUpdatedUserState">(optional) Userstate to pass when firing the statusUpdated callback</param>
        /// <returns>Returns any error which occurred starting to sync, if any</returns>
        public CLError Start(
			CLSyncbox Syncbox,
			out CLSyncStartStatus Status,
			System.Threading.WaitCallback StatusUpdated = null,
			object StatusUpdatedUserState = null)
        {
            bool reservedSyncbox = false;
            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                if (Syncbox == null)
                {
                    const string settingsError = "syncbox cannot be null";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, settingsError);
                    Status = CLSyncStartStatus.ErrorNullSyncbox;
                    return new NullReferenceException(settingsError);
                }

                if (!Syncbox.TryReserveForActiveSync())
                {
                    const string modificationError = "syncbox cannot be modifying server Syncbox via public API calls (i.e. DeleteSyncbox)";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, modificationError);
                    Status = CLSyncStartStatus.ErrorInProcessOfModification;
                    return new ArgumentException(modificationError);
                }

                reservedSyncbox = true;

                if (string.IsNullOrEmpty(Syncbox.CopiedSettings.DeviceId))
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    const string settingsError = "syncbox CopiedSettings DeviceId cannot be null";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, settingsError);
                    Status = CLSyncStartStatus.ErrorNullDeviceId;
                    return new NullReferenceException(settingsError);
                }

                lock (_locker)
                {
                    this._syncbox = Syncbox;
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

                //// DO NOT MOVE THIS EARLIER EVEN THOUGH EARLIER STATEMENTS HAVE TRACE
                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(this._syncbox.CopiedSettings.TraceLocation, "Cloud", "log", this._syncbox.CopiedSettings.TraceLevel, this._syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineStarting);

                // Check the TraceLocation vs. LogErrors
                if (string.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.TraceLocation) && this._syncbox.CopiedSettings.LogErrors)
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    const string verifyTraceError = "TraceLocation must be set if LogErrors is checked";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyTraceError);
                    Status = CLSyncStartStatus.ErrorTraceEnabledWithoutDirectory;
                    return new ArgumentException(verifyTraceError);
                }

                if (!String.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.DatabaseFolder))
                {
                    FilePath fpDatabase = this._syncbox.CopiedSettings.DatabaseFolder;
                    FilePath fpSyncbox = this._syncbox.CopiedSettings.SyncRoot;
                    if (fpDatabase.Contains(fpSyncbox, insensitiveNameSearch: true))
                    {
                        const string verifyDatabaseError = "Syncbox settings DatabaseFolder cannot be inside the Syncbox settings SyncRoot";
                        _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyDatabaseError);
                        Status = CLSyncStartStatus.ErrorDatabaseFolderInsideSyncboxFolder;
                        return new ArgumentException(verifyDatabaseError);
                    }
                }

                if (!string.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.TraceLocation))
                {
                    FilePath fpTraceLocation = this._syncbox.CopiedSettings.TraceLocation;
                    FilePath fpSyncbox = this._syncbox.CopiedSettings.SyncRoot;
                    if (fpTraceLocation.Contains(fpSyncbox, insensitiveNameSearch: true))
                    {
                        const string verifyTraceLocationError = "Syncbox settings TraceLocation cannot be inside the Syncbox settings SyncRoot";
                        _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyTraceLocationError);
                        Status = CLSyncStartStatus.ErrorTraceFolderInsideSyncboxFolder;
                        return new ArgumentException(verifyTraceLocationError);
                    }
                }

                if (!String.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.TempDownloadFolderFullPath))
                {
                    FilePath fpTemp = this._syncbox.CopiedSettings.TempDownloadFolderFullPath;
                    FilePath fpSyncbox = this._syncbox.CopiedSettings.SyncRoot;
                    if (fpTemp.Contains(fpSyncbox, insensitiveNameSearch: true))
                    {
                        const string verifyTempDownloadFolderError = "Syncbox settings TempDownloadFolderFullPath cannot be inside the Syncbox settings SyncRoot";
                        _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyTempDownloadFolderError);
                        Status = CLSyncStartStatus.ErrorTempDownloadFolderInsideSyncboxFolder;
                        return new ArgumentException(verifyTempDownloadFolderError);
                    }
                }

                CLError checkBadPath = Helpers.CheckForBadPath(this._syncbox.CopiedSettings.SyncRoot);
                if (checkBadPath != null)
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1, Resources.CLSyncEngineError0, checkBadPath.PrimaryException.Message);
                    Status = CLSyncStartStatus.ErrorBadRootPath;
                    return new ArgumentException(Resources.CLSyncEngineCloudRootRepsBadPath, checkBadPath.PrimaryException);
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncRootLength(this._syncbox.CopiedSettings.SyncRoot, out tooLongChars);
                if (checkPathLength != null)
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1, Resources.CLSyncEngineError0, checkPathLength.PrimaryException.Message);
                    Status = CLSyncStartStatus.ErrorLongRootPath;
                    return new ArgumentException(Resources.CLSyncEngineCloudRootSettingsTooLong, checkPathLength.PrimaryException);
                }

                // Don't start twice.
                _trace.writeToLog(1, Resources.CLSyncEngineStartEntry);
                if (_isStarted)
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    CLError error = new Exception(Resources.CLSyncEngineAlreadyStarted);
                    _trace.writeToLog(1, Resources.CLSyncEngineStartError, error.PrimaryException.Message);
                    Status = CLSyncStartStatus.ErrorAlreadyStarted;
                    return error;
                }

                // Create the Syncbox directory if it doesn't exist
                bool alreadyExists = true;
                System.IO.DirectoryInfo rootInfo = new System.IO.DirectoryInfo(_syncbox.CopiedSettings.SyncRoot);
                if (!rootInfo.Exists)
                {
                    alreadyExists = false;
                    rootInfo.Create();
                }

                bool caseMatches;
                CLError caseCheckError = Helpers.DirectoryMatchesCaseWithDisk(_syncbox.CopiedSettings.SyncRoot,
                    out caseMatches);

                if (caseCheckError != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, checkBadPath.PrimaryException.Message);
                    Status = CLSyncStartStatus.ErrorBadRootPath;
                    return new ArgumentException(Resources.CLSyncEngineCloudRootRepresentsAPathThatCannotBeQueried, caseCheckError.PrimaryException);
                }

                if (!caseMatches)
                {
                    const string badCaseErrorCreated = "A new directory was created on disk at the specified settings SyncRoot, but its resulting path does not match case";
                    const string badCaseErrorExists = "An existing directory was found at the specified settings SyncRoot, but its path does not match case";
                    _trace.writeToLog(1, Resources.CLSyncEngineErrorBadCase, (alreadyExists ? badCaseErrorExists : badCaseErrorCreated));
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
                CLError iconOverlayError = _iconOverlay.Initialize(this._syncbox.CopiedSettings, this._syncbox.SyncboxId);
                if (iconOverlayError != null)
                {
                    // Failure to start badging does not prevent syncing.  Just log it.
                    _trace.writeToLog(1, Resources.CLSyncEngineStartErrorExceptionMsg1Code1,
                        iconOverlayError.PrimaryException.Message,
                        iconOverlayError.PrimaryException.Code);
                }

                // Start the indexer.
                _trace.writeToLog(9, Resources.CLSyncEngineStartIndexer);
                CLError indexCreationError;
                lock (_locker)
                {
                    indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer, this._syncbox);
                }
                if (indexCreationError != null)
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1,
                        Resources.CLSyncEngineErrorException2Msg0Code1,
                        indexCreationError.PrimaryException.Message,
                        indexCreationError.PrimaryException.Code);
                    ReleaseResources();
                    Status = CLSyncStartStatus.ErrorIndexCreation;
                    return indexCreationError;
                }

                // Start the push notification.
                _trace.writeToLog(9, Resources.CLSyncEngineStartNotifier);
                lock (_locker)
                {
                    CLError getNotificationError = CLNotificationService.GetInstance(this._syncbox, out _notifier);
                    if (getNotificationError != null
                        || _notifier == null)
                    {
                        if (reservedSyncbox
                            && Syncbox != null)
                        {
                            Syncbox.ResetReserveForActiveSync();
                        }

                        CLError error;
                        if (getNotificationError == null)
                        {
                            error = new Exception(Resources.CLSyncEngineErrorStartingNotification);
                        }
                        else
                        {
                            error = new AggregateException(Resources.CLSyncEngineErrorOccurredStarrtingNotification, getNotificationError.Exceptions);
                        }
                        _trace.writeToLog(1, Resources.CLSyncEngineStartError2, error.PrimaryException.Message);
                        ReleaseResources();
                        Status = CLSyncStartStatus.ErrorStartingNotification;
                        return error;
                    }
                }

                bool debugMemory;
                lock (debugFileMonitorMemory)
                {
                    debugMemory = debugFileMonitorMemory.Value;
                }

                bool DependencyDebugging;
                lock (debugDependencies)
                {
                    DependencyDebugging = debugDependencies.Value;
                }

                // Start the monitor
                CLError fileMonitorCreationError;
                lock (_locker)
                {
                    fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(this._syncbox,
                        _indexer,
                        this._syncbox.HttpRestClient,
                        DependencyDebugging,
                        statusUpdated,
                        statusUpdatedUserState,
                        out _monitor,
                        out _syncEngine,
                        debugMemory);
                }

                // Hook up the events
                _trace.writeToLog(9, Resources.CLSyncEngineStartHookupEvents);
                _notifier.NotificationReceived += OnNotificationReceived;
                _notifier.NotificationStillDisconnectedPing += OnNotificationPerformManualSyncFrom;
                _notifier.ConnectionError += OnNotificationConnectionError;

                if (fileMonitorCreationError != null)
                {
                    if (reservedSyncbox
                        && Syncbox != null)
                    {
                        Syncbox.ResetReserveForActiveSync();
                    }

                    _trace.writeToLog(1,
                        Resources.CLSyncEngineStartError4Msg0Code1,
                        fileMonitorCreationError.PrimaryException.Message,
                        fileMonitorCreationError.PrimaryException.Code);
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
                                    try
                                    {
                                        NetworkMonitor.Instance.StartNetworkMonitor();
                                        SyncEngine.InternetConnected = NetworkMonitor.Instance.CheckInternetIsConnected();
                                    }
                                    catch
                                    {
                                        SyncEngine.InternetConnected = true;
                                        NetworkMonitor.Instance.NetworkChanged -= NetworkChanged;
                                    }
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
                                    if (reservedSyncbox
                                        && Syncbox != null)
                                    {
                                        Syncbox.ResetReserveForActiveSync();
                                    }

                                    _trace.writeToLog(1,
                                        Resources.CLSyncEngineErrorStartingMonitorAgentMsg0Code1,
                                        fileMonitorStartError.PrimaryException.Message,
                                        fileMonitorCreationError.PrimaryException.Code);
                                    ReleaseResources();
                                    Status = CLSyncStartStatus.ErrorStartingFileMonitor;
                                    return fileMonitorStartError;
                                }

                                CLError indexerStartError = _indexer.StartInitialIndexing(
                                                _monitor.BeginProcessing,
                                                _monitor.GetCurrentPath);
                                if (indexerStartError != null)
                                {
                                    if (reservedSyncbox
                                        && Syncbox != null)
                                    {
                                        Syncbox.ResetReserveForActiveSync();
                                    }

                                    _trace.writeToLog(1,
                                        Resources.CLSyncEngineErrorStartingInitialIndexingMsg0Code1,
                                        indexerStartError.PrimaryException.Message,
                                        indexerStartError.PrimaryException.Code);
                                    ReleaseResources();
                                    Status = CLSyncStartStatus.ErrorStartingInitialIndexing;
                                    return indexerStartError;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (reservedSyncbox
                                    && Syncbox != null)
                                {
                                    Syncbox.ResetReserveForActiveSync();
                                }

                                CLError error = ex;
                                error.Log(this._syncbox.CopiedSettings.TraceLocation, this._syncbox.CopiedSettings.LogErrors);
                                _trace.writeToLog(1, Resources.CLSyncEngineException5Msg0, ex.Message);
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
                if (reservedSyncbox
                    && Syncbox != null)
                {
                    Syncbox.ResetReserveForActiveSync();
                }

                CLError error = ex;
                error.Log(this._syncbox.CopiedSettings.TraceLocation, this._syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineException6Msg0, ex.Message);
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
            _trace.writeToLog(1, Resources.CLSyncEngineOnConnectionErrorEntryErrorManualPollErrorWebSocketError, e.ErrorStillDisconnectedPing.PrimaryException.Message, e.ErrorWebSockets.PrimaryException.Message);
            if (PushNotificationError != null)
            {
                _trace.writeToLog(1, Resources.CLSyncEngineOnConnectionErrorNotifyApplication);
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
                    _trace.writeToLog(9, Resources.CLSynceEngineOnNotificationPerformManualSyncFrom);
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
                    _trace.writeToLog(9, Resources.CLSyncEngineSendAPerformPushNotificationToMonitor);
                    _monitor.PushNotification(e.Message);
                }
            }
        }

        /// <summary>
        /// Stop syncing the Syncbox, and free all resources.
        /// </summary>
        private CLError ReleaseResources()
        {
            CLError toReturn = null;

            CLSyncbox storeSyncbox;

            lock (_locker)
            {
                if (_monitor != null)
                {
                    try
                    {
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesStopFileMonitor);
                        MonitorStatus monitorIsStopped;
                        toReturn = _monitor.Stop(out monitorIsStopped);
                        _monitor.Dispose();
                        _monitor = null;
                        _trace.writeToLog(9, Resources.CLSyncEngineFileMonitorStopped);
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
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesDisconnectPushNotificationServer);
                        _notifier.DisconnectPushNotificationServer();
                        _notifier = null;
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesPushNotificationServerDisconnected);
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
                        _trace.writeToLog(9, Resources.CLsyncEngineReleaseResourcesStopTheIndexer);
                        _indexer.Dispose();
                        _indexer = null;
                        _trace.writeToLog(9, Resources.CLsyncEngineReleaseResourcesIndexerStopped);
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
                        _trace.writeToLog(9, Resources.CLSyncEngineStopTheSyncEngine);
                        _syncEngine.Dispose();

                        // detach sync engine from network monitoring
                        lock (NetworkMonitoredEngines)
                        {
                            if (NetworkMonitoredEngines.Remove(_syncEngine))
                            {
                                if (NetworkMonitoredEngines.Count == 0)
                                {
                                    if (NetworkMonitor.Instance != null)
                                    {
                                        NetworkMonitor.Instance.NetworkChanged -= NetworkChanged;
                                        NetworkMonitor.Instance.StopNetworkMonitor();
                                    }
                                }
                            }
                        }

                        _syncEngine = null;
                        _trace.writeToLog(9, Resources.CLSyncEngineSyncEngineStopped);
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
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesStoppingIconOverlay);
                        _iconOverlay.Shutdown();
                        _iconOverlay = null;
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesIconOverlayStopped);
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

                storeSyncbox = _syncbox;
            }

            if (toReturn != null)
            {
                toReturn.Log(storeSyncbox.CopiedSettings.TraceLocation, storeSyncbox.CopiedSettings.LogErrors);
            }

            lock (_locker)
            {
                _syncbox = null; // set this to null after logging errors (which requires the settings)
            }
            return toReturn;
        }

        private static void NetworkChanged(object sender, NetworkChangedEventArgs e)
        {
            SyncEngine.InternetConnected = e.IsConnected;
        }

        /// <summary>
        /// Stop syncing the Syncbox and free resources.
        /// </summary>
        public void Stop()
        {
            lock (_locker)
            {
                if (this._syncbox != null)
                {
                    this._syncbox.ResetReserveForActiveSync();
                }
            }

            _trace.writeToLog(1, Resources.CLSyncEngineStopEntry);
            ReleaseResources();
        }

        /// <summary>
        /// Call when application is shutting down.
        /// </summary>
        public static void Shutdown()
        {
            // Write out any development debug traces
            _trace.closeMemoryTrace();

            // Shuts down the HttpScheduler; after shutdown it cannot be used again
            HttpScheduler.DisposeBothSchedulers();

            // Shuts down the sync FileChange delay processing
            DelayProcessable<FileChange>.TerminateAllProcessing();

            // Stops network change monitoring
            NetworkMonitor.DisposeInstance();
        }
    }
}