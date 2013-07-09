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
using System.IO;

namespace Cloud
{
    /// <summary>
    /// A class used to create a Syncbox to synchronize the contents of a local disk directory.
    /// </summary>
    internal sealed class CLSyncEngine
    {
        private IconOverlay _iconOverlay = null;
        private MonitorAgent _monitor = null;
        private IndexingAgent _indexer = null;
        private Microsoft.Win32.SafeHandles.SafeFileHandle _syncboxPathCreateFile = null;
        private CLNotificationService _notifier = null;
        private bool _isStarted = false;
        private static CLTrace _trace = CLTrace.Instance;
        private SyncEngine _syncEngine = null;
        private static readonly HashSet<SyncEngine> NetworkMonitoredEngines = new HashSet<SyncEngine>();
        private System.Threading.WaitCallback statusUpdated = null;
        private object statusUpdatedUserState = null;
        private readonly object _locker = new object();

        private readonly CLSyncbox _syncbox;
        private readonly bool debugDependencies;
        private readonly bool copyDatabaseBetweenChanges;
        private readonly bool debugFileMonitorMemory;

        public CLSyncEngine(CLSyncbox syncbox,
            bool debugDependencies = false,
            bool copyDatabaseBetweenChanges = false,
            bool debugFileMonitorMemory = false)
        {
            if (syncbox == null)
            {
                const string settingsError = "syncbox must not be null";
                _trace.writeToLog(1, Resources.CLSyncEngineError0, settingsError);
                throw new CLNullReferenceException(CLExceptionCode.General_Arguments, settingsError);
            }

            this._syncbox = syncbox;
            this.debugDependencies = debugDependencies;
            this.copyDatabaseBetweenChanges = copyDatabaseBetweenChanges;
            this.debugFileMonitorMemory = debugFileMonitorMemory;
        }

        /// <summary>
        /// Queries database by eventId to return latest metadata and path as a FileChange and whether or not the event is still pending
        /// </summary>
        /// <param name="eventId">EventId key to lookup</param>
        /// <param name="queryResult">(output) Result FileChange from EventId lookup</param>
        /// <param name="isPending">(output) Result whether event is pending from EventId lookup</param>
        /// <param name="status">(output) Status of quering the database</param>
        /// <returns>Returns any error which occurred querying the database, if any</returns>
        public CLError QueryFileChangeByEventId(long eventId, out FileChange queryResult, out bool isPending)
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
                        try
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncbox_NotStarted, Resources.CLSyncEngineIndexerCannotBeNull);
                        }
                        catch (Exception ex)
                        {
                            return ex;
                        }
                    }

                    return _indexer.QueryFileChangeByEventId(eventId, out queryResult, out isPending);
                }
            }
            catch (Exception ex)
            {
                queryResult = Helpers.DefaultForType<FileChange>();
                isPending = Helpers.DefaultForType<bool>();
                return ex;
            }
        }

        /// <summary>
        /// Forwards to WipeIndex which resets the database file
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
                    throw new NullReferenceException(Resources.SyncboxMustNotBeNull);
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(syncbox.CopiedSettings.TraceLocation, "Cloud", Resources.IconOverlayLog, syncbox.CopiedSettings.TraceLevel, syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineSyncResetEntry);

                CLError checkBadPath = Helpers.CheckForBadPath(syncbox.Path);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineResetError0, checkBadPath.PrimaryException.Message);
                    throw new ArgumentException(Resources.CLSyncEngineSyncboxPathRepsBadPath, checkBadPath.PrimaryException);
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncboxPathLength(syncbox.Path, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineResetError0, checkPathLength.PrimaryException.Message);
                    throw new ArgumentException(Resources.CLSyncEngineSyncboxPathSettingsTooLong, checkPathLength.PrimaryException);
                }

                IndexingAgent deleteAgent;
                CLError createIndexerError = IndexingAgent.CreateNewAndInitialize(
                    out deleteAgent,
                    syncbox);

                try
                {
                    // indexing agent will not be disposed upon construction, so no need to check for CLObjectDisposedException here

                    // DO NOT CHECK ERROR (if deleteAgent was returned), we may wish to delete the database BECAUSE it is corrupted
                    if (createIndexerError != null
                        && deleteAgent == null)
                    {
                        try
                        {
                            throw new AggregateException("Error creating the local indexer to wipe its backing database", createIndexerError.Exceptions);
                        }
                        catch (Exception ex)
                        {
                            return ex;
                        }
                    }

                    CLError deleteDatabaseError = deleteAgent.WipeIndex(syncbox.Path);

                    if (deleteDatabaseError != null)
                    {
                        throw new AggregateException("Error wiping the backing database", deleteDatabaseError.Exceptions);
                    }
                }
                finally
                {
                    if (deleteAgent != null)
                    {
                        try
                        {
                            deleteAgent.Dispose();
                        }
                        catch
                        {
                        }
                    }
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

        /// <summary>
        /// Forwards to ChangeSyncboxPath in the indexer
        /// </summary>
        public CLError UpdatePath(CLSyncbox syncbox, string newPath)
        {
            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                if (syncbox == null)
                {
                    throw new NullReferenceException(Resources.SyncboxMustNotBeNull);
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(syncbox.CopiedSettings.TraceLocation, "Cloud", Resources.IconOverlayLog, syncbox.CopiedSettings.TraceLevel, syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineSyncResetEntry);

                CLError checkBadPath = Helpers.CheckForBadPath(syncbox.Path)
                    ?? Helpers.CheckForBadPath(newPath);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineResetError0, checkBadPath.PrimaryException.Message);
                    throw new ArgumentException(Resources.CLSyncEngineSyncboxPathRepsBadPath, checkBadPath.PrimaryException);
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncboxPathLength(syncbox.Path, out tooLongChars)
                    ?? Helpers.CheckSyncboxPathLength(newPath, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineResetError0, checkPathLength.PrimaryException.Message);
                    throw new ArgumentException(Resources.CLSyncEngineSyncboxPathSettingsTooLong, checkPathLength.PrimaryException);
                }

                IndexingAgent updateAgent;
                CLError createIndexerError = IndexingAgent.CreateNewAndInitialize(
                    out updateAgent,
                    syncbox,
                    copyDatabaseBetweenChanges: this.copyDatabaseBetweenChanges);

                try
                {
                    if (createIndexerError != null)
                    {
                        // indexing agent will not be disposed upon construction, so no need to check for CLObjectDisposedException here

                        throw new AggregateException(Resources.ExceptionSyncboxCreateIndex, createIndexerError.Exceptions);
                    }

                    CLError updatePathError = updateAgent.ChangeSyncboxPath(newPath);
                    if (updatePathError != null)
                    {
                        throw new AggregateException(Resources.ExceptionCLSyncEngineUpdatePath, updatePathError.Exceptions);
                    }
                }
                finally
                {
                    if (updateAgent != null)
                    {
                        try
                        {
                            updateAgent.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
        ///// <param name="SyncboxPath">Full path to the directory to be synced (do not include a trailing slash except for a drive root)</param>
        ///// <param name="Status">(output) State of starting Syncbox, check this to make sure it was successful</param>
        ///// <param name="StatusUpdated">(optional) Callback to fire whenever the status of the Syncbox has been updated</param>
        ///// <param name="StatusUpdatedUserState">(optional) User state to pass when firing the statusUpdated callback</param>
        ///// <returns>Returns any error which occurred starting the Syncbox</returns>
        //public CLError Start(string ApplicationKey,
        //    string ApplicationSecret,
        //    long SyncboxId,
        //    string SyncboxPath,
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
        //                Helpers.GetComputerFriendlyName() + Guid.NewGuid().ToString(Resources.CLCredentialStringSettingsN),
        //                ApplicationKey,
        //                ApplicationSecret,
        //                SyncboxId,
        //                "SimpleWinClient01",
        //                SyncboxPath),
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
        /// <param name="status">(output) State of starting SyncEngine, check this to make sure it was successful</param>
        /// <param name="statusUpdated">(optional) Callback to fire whenever the status of the SyncEngine has been updated</param>
        /// <param name="statusUpdatedUserState">(optional) User state to pass when firing the statusUpdated callback</param>
        /// <returns>Returns any error which occurred starting to sync, if any</returns>
        public CLError Start(
            long quotaUsage,
            long storageQuota,
            SyncEngine.OnGetDataUsageCompletionDelegate OnGetDataUsageCompletion,
			System.Threading.WaitCallback statusUpdated = null,
			object statusUpdatedUserState = null)
        {
            bool reservedSyncbox = false;
            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLSyncEngineHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                if (!_syncbox.TryReserveForActiveSync())
                {
                    const string modificationError = "syncbox cannot be modifying server Syncbox via public API calls (i.e. DeleteSyncbox)";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, modificationError);
                    throw new CLArgumentException(CLExceptionCode.Syncbox_InProcessOfModification, modificationError);
                }

                reservedSyncbox = true;

                object ObCaseInsensitiveValue = null;
                try
                {
                    const string systemKeyString = "SYSTEM";
                    const string controlSetKeyString = "CurrentControlSet";
                    const string controlKeyString = "Control";
                    const string sessionManagerKeyString = "Session Manager";
                    const string kernelKeyString = "kernel";
                    const string ObCaseInsensitiveValueString = "obcaseinsensitive";
                    Microsoft.Win32.RegistryKey systemKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(systemKeyString);
                    Microsoft.Win32.RegistryKey controlSetKey = systemKey.OpenSubKey(controlSetKeyString);
                    Microsoft.Win32.RegistryKey controlKey = controlSetKey.OpenSubKey(controlKeyString);
                    Microsoft.Win32.RegistryKey sessionManagerKey = controlKey.OpenSubKey(sessionManagerKeyString);
                    Microsoft.Win32.RegistryKey kernelKey = sessionManagerKey.OpenSubKey(kernelKeyString);
                    ObCaseInsensitiveValue = kernelKey.GetValue(ObCaseInsensitiveValueString);
                }
                catch
                {
                }
                if (ObCaseInsensitiveValue != null && ObCaseInsensitiveValue.GetType() == typeof(int) && ((int)ObCaseInsensitiveValue) == 0)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.Syncbox_GeneralStart, Resources.ExceptionCLSyncEngineObCaseInsensitive);
                }

                if (string.IsNullOrEmpty(_syncbox.CopiedSettings.DeviceId))
                {
                    const string settingsError = "syncbox CopiedSettings DeviceId cannot be null";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, settingsError);
                    throw new CLNullReferenceException(CLExceptionCode.Syncbox_DeviceId, settingsError);
                }

                lock (_locker)
                {
                    this.statusUpdated = statusUpdated;
                    if (statusUpdated == null)
                    {
                        this.statusUpdatedUserState = null;
                    }
                    else
                    {
                        this.statusUpdatedUserState = statusUpdatedUserState;
                    }
                }

                // Check the TraceLocation vs. LogErrors
                if (string.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.TraceLocation) && this._syncbox.CopiedSettings.LogErrors)
                {
                    const string verifyTraceError = "TraceLocation must be set if LogErrors is checked";
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyTraceError);
                    throw new CLArgumentException(CLExceptionCode.Syncbox_TraceEnabledWithoutDirectory, verifyTraceError);
                }

                //// DO NOT MOVE THIS EARLIER EVEN THOUGH EARLIER STATEMENTS HAVE TRACE
                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(this._syncbox.CopiedSettings.TraceLocation, "Cloud", Resources.IconOverlayLog, this._syncbox.CopiedSettings.TraceLevel, this._syncbox.CopiedSettings.LogErrors);
                _trace.writeToLog(1, Resources.CLSyncEngineStarting);

                if (!String.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.DatabaseFolder))
                {
                    FilePath fpDatabase = this._syncbox.CopiedSettings.DatabaseFolder;
                    FilePath fpSyncbox = this._syncbox.Path;
                    if (fpDatabase.Contains(fpSyncbox))
                    {
                        const string verifyDatabaseError = "Syncbox settings DatabaseFolder cannot be inside the Syncbox path";
                        _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyDatabaseError);
                        throw new CLArgumentException(CLExceptionCode.Syncbox_DatabaseInsideSyncboxPath, verifyDatabaseError);
                    }
                }

                if (!string.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.TraceLocation))
                {
                    FilePath fpTraceLocation = this._syncbox.CopiedSettings.TraceLocation;
                    FilePath fpSyncbox = this._syncbox.Path;
                    if (fpTraceLocation.Contains(fpSyncbox))
                    {
                        const string verifyTraceLocationError = "Syncbox settings TraceLocation cannot be inside the Syncbox path";
                        _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyTraceLocationError);
                        throw new CLArgumentException(CLExceptionCode.Syncbox_TraceInsideSyncboxPath, verifyTraceLocationError);
                    }
                }

                if (!String.IsNullOrWhiteSpace(this._syncbox.CopiedSettings.TempDownloadFolderFullPath))
                {
                    FilePath fpTemp = this._syncbox.CopiedSettings.TempDownloadFolderFullPath;
                    FilePath fpSyncbox = this._syncbox.Path;
                    if (fpTemp.Contains(fpSyncbox))
                    {
                        const string verifyTempDownloadFolderError = "Syncbox settings TempDownloadFolderFullPath cannot be inside the Syncbox path";
                        _trace.writeToLog(1, Resources.CLSyncEngineError0, verifyTempDownloadFolderError);
                        throw new CLArgumentException(CLExceptionCode.Syncbox_TempDownloadsInsideSyncboxPath, verifyTempDownloadFolderError);
                    }
                }

                CLError checkBadPath = Helpers.CheckForBadPath(this._syncbox.Path);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, checkBadPath.PrimaryException.Message);
                    throw new CLArgumentException(CLExceptionCode.Syncbox_BadPath, Resources.CLSyncEngineSyncboxPathRepsBadPath, checkBadPath.Exceptions);
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncboxPathLength(this._syncbox.Path, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineError0, checkPathLength.PrimaryException.Message);
                    throw new CLArgumentException(CLExceptionCode.Syncbox_LongPath, Resources.CLSyncEngineSyncboxPathSettingsTooLong, checkPathLength.Exceptions);
                }

                // Don't start twice.
                _trace.writeToLog(1, Resources.CLSyncEngineStartEntry);
                if (_isStarted)
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineStartError, Resources.CLSyncEngineAlreadyStarted);
                    throw new CLInvalidOperationException(CLExceptionCode.Syncbox_AlreadyStarted, Resources.CLSyncEngineAlreadyStarted);
                }

                // If the database file will be created, then we will create the syncbox root folder if it does not exist.
                // Otherwise, the database is active, and the syncbox root folder must already exist at the specified location.
                // Determine if the database file will be created.
                bool dbNeedsDeletion;
                bool dbNeedsCreation;
                FileInfo dbInfo;
                string notUsedExistingFullPath;

                string indexDBLocation = Helpers.CalculateDatabasePath(this._syncbox);
                IndexingAgent.CheckDatabaseFileState(createEvenIfExisting: false, dbInfo: out dbInfo, dbNeedsDeletion: out dbNeedsDeletion, dbNeedsCreation: out dbNeedsCreation, indexDBLocation: indexDBLocation, rootObjectCalculatedFullPath: out notUsedExistingFullPath);

                System.IO.DirectoryInfo rootInfo = new System.IO.DirectoryInfo(_syncbox.Path);
                bool alreadyExists = rootInfo.Exists;
                if (dbNeedsCreation)
                {
                    // The database file will be created.  We can create the syncbox root folder if it doesn't exist.  The sync engine will be sending a sync_from with SID zero.  We will merge the cloud syncbox with the syncbox folder on disk.
                    if (!alreadyExists)
                    {
                        rootInfo.Create();
                    }
                }
                else
                {
                    // The database contains valid state information.  If the syncbox root directory is not there, letting the sync engine use it would delete all of the files and folders in the cloud.
                    if (!alreadyExists)
                    {
                        string msg = String.Format(Resources.ExceptionCLSyncEngineMissingSyncFolder, _syncbox.Path);
                        _trace.writeToLog(1, msg);
                        throw new CLArgumentException(CLExceptionCode.Syncbox_PathNotFound, msg);
                    }
                }

                lock (_locker)
                {
                    if (_syncboxPathCreateFile != null)
                    {
                        try
                        {
                            _syncboxPathCreateFile.Dispose();
                        }
                        catch
                        {
                        }
                    }

                    try
                    {
                        _syncboxPathCreateFile = NativeMethods.CreateFile(
                            _syncbox.Path,
                            FileAccess.Read,
                            FileShare.Read | FileShare.Write,
                            /* securityAttributes: */ IntPtr.Zero,
                            FileMode.Open,
                            (FileAttributes)NativeMethods.FileAttributesFileFlagBackupSemantics,
                            /* template: */ IntPtr.Zero);
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, Resources.ExceptionSyncboxLockPathTrace, ex.Message);
                        throw new CLArgumentException(CLExceptionCode.Syncbox_PathNotFound, Resources.ExceptionSyncboxLockPath, ex);
                    }
                }

                // Start badging
                lock (_locker)
                {
                    _iconOverlay = new IconOverlay();
                }
                CLError iconOverlayError = _iconOverlay.Initialize(this._syncbox.CopiedSettings, this._syncbox);
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
                    indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer, this._syncbox, this.copyDatabaseBetweenChanges);
                }
                if (indexCreationError != null)
                {
                    // indexing agent will not be disposed upon construction, so no need to check for CLObjectDisposedException here

	                _trace.writeToLog(1,
    	                Resources.CLSyncEngineErrorException2Msg0Code1,
        	            indexCreationError.PrimaryException.Message,
            	        indexCreationError.PrimaryException.Code);

                    throw new CLException(CLExceptionCode.Syncbox_IndexCreation, Resources.ExceptionSyncboxCreateIndex, indexCreationError.Exceptions);
                }

                // Start the push notification.
	            _trace.writeToLog(9, Resources.CLSyncEngineStartNotifier);
                lock (_locker)
                {
                    CLError getNotificationError = CLNotificationService.GetInstance(this._syncbox, out _notifier);
                    if (getNotificationError != null
                        || _notifier == null)
                    {
                        string notificationStartErrorString = (getNotificationError == null
                            ? Resources.CLSyncEngineErrorStartingNotification
                            : Resources.CLSyncEngineErrorOccurredStartingNotification);

                        _trace.writeToLog(1, Resources.CLSyncEngineStartError2, notificationStartErrorString + (getNotificationError == null ? string.Empty : getNotificationError.PrimaryException.Message));

                        throw new CLException(CLExceptionCode.Syncbox_StartingNotifications, notificationStartErrorString, (getNotificationError == null ? null : getNotificationError.Exceptions));
                    }
                }

                // Start the monitor
                CLError fileMonitorCreationError;
                lock (_locker)
                {
                    fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(
                        this._syncbox,
                        _indexer,
                        this._syncbox.HttpRestClient,
                        this.debugDependencies,
                        statusUpdated,
                        statusUpdatedUserState,
                        out _monitor,
                        out _syncEngine,
                        this.debugFileMonitorMemory,
                        quotaUsage,
                        storageQuota,
                        OnGetDataUsageCompletion);
                }

                // Hook up the events
	            _trace.writeToLog(9, Resources.CLSyncEngineStartHookupEvents);
                _notifier.NotificationReceived += OnNotificationReceived;
                _notifier.NotificationStillDisconnectedPing += OnNotificationPerformManualSyncFrom;
                _notifier.ConnectionError += OnNotificationConnectionError;

                if (fileMonitorCreationError != null
                    || _monitor == null)
                {
                    if (fileMonitorCreationError != null)
                    {
                        _trace.writeToLog(1,
                            Resources.CLSyncEngineStartError4Msg0Code1,
                            fileMonitorCreationError.PrimaryException.Message,
                            fileMonitorCreationError.PrimaryException.Code);
                    }

                    throw new CLException(CLExceptionCode.Syncbox_FileMonitorCreation, Resources.ExceptionSyncboxCreateFileMonitor, (fileMonitorCreationError == null ? null : fileMonitorCreationError.Exceptions));
                }
                else if (_monitor != null)
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
                                        NetworkMonitoredEngines.Remove(_syncEngine);
                                    }
                                }
                            }
                        }

                        MonitorStatus returnStatus;
                        CLError fileMonitorStartError = _monitor.Start(out returnStatus);
                        if (fileMonitorStartError != null
                            || (returnStatus != MonitorStatus.Started
                                && returnStatus != MonitorStatus.AlreadyStarted))
                        {
                            if (fileMonitorStartError != null)
                            {
	                        _trace.writeToLog(1,
    	                        Resources.CLSyncEngineErrorStartingMonitorAgentMsg0Code1,
        	                    fileMonitorStartError.PrimaryException.Message,
            	                fileMonitorCreationError.PrimaryException.Code);
                            }

                            throw new CLException(CLExceptionCode.Syncbox_StartingFileMonitor, Resources.ExceptionSyncboxStartFileMonitor, (fileMonitorStartError == null ? null : fileMonitorStartError.Exceptions));
                        }

                        CLError indexerStartError = _indexer.StartInitialIndexing(
                                        _monitor.BeginProcessing,
                                        _monitor.GetCurrentPath);
                        if (indexerStartError != null)
                        {
            	            _trace.writeToLog(1,
        	                    Resources.CLSyncEngineErrorStartingInitialIndexingMsg0Code1,
    	                        indexerStartError.PrimaryException.Message,
	                            indexerStartError.PrimaryException.Code);

                            throw new CLException(CLExceptionCode.Syncbox_StartingInitialIndexing, Resources.ExceptionSyncboxStartInitialIndexing, indexerStartError.Exceptions);
                        }

                        _isStarted = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (reservedSyncbox
                    && _syncbox != null)
                {
                    try
                    {
                        _syncbox.ResetReserveForActiveSync();
                    }
                    catch
                    {
                    }
                }

                try
                {
                    ReleaseResources();
                }
                catch
                {
                }

                CLError error;

                if (ex is CLException)
                {
                    error = ex;
                }
                else
                {
                    _trace.writeToLog(1, Resources.CLSyncEngineException6Msg0, ex.Message);
                    try
                    {
                        throw new CLException(CLExceptionCode.Syncbox_GeneralStart, Resources.ExceptionSyncboxStartGeneral);
                    }
                    catch (Exception innerEx)
                    {
                        error = innerEx;
                    }
                }

                error.Log(this._syncbox.CopiedSettings.TraceLocation, this._syncbox.CopiedSettings.LogErrors);
                return error;
            }

            return null;
        }

        /// <summary>
        /// Get the current status of this sync engine
        /// </summary>
        /// <param name="status">(output) Status of this SyncEngine</param>
        /// <returns>Returns any error that occurred retrieving the status (usually shutdown), if any</returns>
        internal CLError GetCurrentStatus(out CLSyncCurrentStatus status)
        {
            if (_syncEngine == null)
            {
                throw new InvalidOperationException("Start syncing first");
            }

            return (_syncEngine.GetCurrentStatus(out status));
        }

        /// <summary>
        /// A serious notification error has occurred.  Push notification is no longer functioning.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Arguments including the manual poll and/or web sockets errors (possibly aggregated).</param>
        private void OnNotificationConnectionError(object sender, NotificationErrorEventArgs e)
        {
            // Forward this notification to the syncbox.
            if (_syncbox != null)
            {
                _syncbox.OnPushNotificationConnectionError(sender, e);
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
                    _trace.writeToLog(9, Resources.CLSyncEngineOnNotificationPerformManualSyncFrom);
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
                        _notifier.NotificationReceived -= OnNotificationReceived;
                        _notifier.NotificationStillDisconnectedPing -= OnNotificationPerformManualSyncFrom;
                        _notifier.ConnectionError -= OnNotificationConnectionError;
                    }
                    catch
                    {
                    }
                    try
                    {
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesDisconnectPushNotificationServer);
                        _notifier.DisconnectPushNotificationServer();
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesPushNotificationServerDisconnected);
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                    _notifier = null;
                }

                if (_indexer != null)
                {
                    try
                    {
                        _trace.writeToLog(9, Resources.CLsyncEngineReleaseResourcesStopTheIndexer);
                        _indexer.Dispose();
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesIndexerStopped);
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                    _indexer = null;
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
                                        try
                                        {
                                            NetworkMonitor.Instance.NetworkChanged -= NetworkChanged;
                                        }
                                        catch
                                        {
                                        }
                                        NetworkMonitor.Instance.StopNetworkMonitor();
                                    }
                                }
                            }
                        }

                        _trace.writeToLog(9, Resources.CLSyncEngineSyncEngineStopped);
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                    _syncEngine = null;
                }

                if (_iconOverlay != null)
                {
                    try
                    {
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesStoppingIconOverlay);
                        _iconOverlay.Shutdown();
                        _trace.writeToLog(9, Resources.CLSyncEngineReleaseResourcesIconOverlayStopped);
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                    _iconOverlay = null;
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

                if (_syncboxPathCreateFile != null)
                {
                    try
                    {
                        _syncboxPathCreateFile.Dispose();
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                    _syncboxPathCreateFile = null;
                }
            }

            if (toReturn != null)
            {
                toReturn.Log(storeSyncbox.CopiedSettings.TraceLocation, storeSyncbox.CopiedSettings.LogErrors);
            }

            lock (_locker)
            {
                _isStarted = false;
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
                // Notify the components that we are stopping.
                if (_monitor != null)
                {
                    _monitor.Stopping();
                }

                // Reset the syncbox reserve for live sync.
                if (this._syncbox != null)
                {
                    this._syncbox.ResetReserveForActiveSync();
                }
            }

            _trace.writeToLog(1, Resources.CLSyncEngineStopEntry);
            ReleaseResources();
        }

    }
}