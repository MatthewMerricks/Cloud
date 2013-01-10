﻿//
// CLSync.cs
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

namespace CloudApiPublic
{
    /// <summary>
    /// A class used to create a SyncBox to synchronize the contents of a local disk directory.
    /// </summary>
    public class CLSync
    {
        private IconOverlay _iconOverlay = null;
        private MonitorAgent _monitor = null;
        private IndexingAgent _indexer = null;
        private CLNotification _notifier = null;
        private bool _isStarted = false;
        private static CLTrace _trace = CLTrace.Instance;
        private SyncEngine _syncEngine = null;
        private ISyncSettingsAdvanced _syncSettings = null;
        private readonly object _locker = new object();

        /// <summary>
        /// Event fired when a serious notification error has occurred.  Push notification is
        /// no longer functional.
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs> PushNotificationError;

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
                        error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
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
                        error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
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
        /// Call this function before calling Start() whenever it is possible that the SyncBox
        /// folder has changed.  Sync.Start() will rescan the folder and sync the current contents.
        /// </summary>
        /// <param name="settings">Settings to use.</param>
        /// <returns>CLError: A possible error, or null.</returns>
        public CLError SyncReset(ISyncSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    throw new NullReferenceException("settings cannot be null");
                }

                lock (_locker)
                {
                    _syncSettings = settings.CopySettings();
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(_syncSettings.TraceLocation, "Cloud", "log", _syncSettings.TraceLevel, _syncSettings.LogErrors);
                _trace.writeToLog(1, "CLSync: SyncReset: Entry.");

                CLError checkBadPath = Helpers.CheckForBadPath(_syncSettings.SyncRoot);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, "CLSync: SyncReset: ERROR: {0}.", checkBadPath.errorDescription);
                    return new ArgumentException("CloudRoot in settings represents a bad path, check it first via Helpers.CheckForBadPath", checkBadPath.GrabFirstException());
                }

                if (_syncSettings.SyncBoxId == null)
                {
                    _trace.writeToLog(1, "CLSync: SyncReset: ERROR: SyncBoxId must be specified in settings.");
                    return new ArgumentException("SyncBoxId must be specified in settings");
                }

                if (String.IsNullOrWhiteSpace(_syncSettings.DeviceId))
                {
                    _trace.writeToLog(1, "CLSync: SyncReset: ERROR: Udid must be specified in settings.");
                    return new ArgumentException("Udid must be specified in settings");
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncRootLength(_syncSettings.SyncRoot, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, "CLSync: SyncReset: ERROR: {0}.", checkPathLength.errorDescription);
                    return new ArgumentException("CloudRoot in settings is too long, check it first via Helpers.CheckSyncRootLength", checkPathLength.GrabFirstException());
                }

                // Determine the database file with full path
                string sDatabaseDirectoryToUse = Helpers.GetDatabasePath(_syncSettings);
                string sDatabaseFile = sDatabaseDirectoryToUse + "\\" + CLDefinitions.kSyncDatabaseFileName;

                // Delete the database file
                if (System.IO.File.Exists(sDatabaseFile))
                {
                    System.IO.File.Delete(sDatabaseFile);
                }

                // Delete the temp download directory recursively, but not the directory itself.
                string sTempDownloadFolderToUse = Helpers.GetTempFileDownloadPath(_syncSettings);
                CLError errorFromDelete = Helpers.DeleteEverythingInDirectory(sTempDownloadFolderToUse);
                if (errorFromDelete != null)
                {
                    // Just trace this error
                    _trace.writeToLog(1, "CLSync: SyncReset: ERROR: From DeleteEverythingInDirectory.  Message: {0}.", errorFromDelete.errorDescription);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, "CLSync: SyncReset: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Initialize the SyncBox and start syncing its contents to the Cloud server, and to other devices
        /// registering the same SyncBox.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public CLError Start(ISyncSettings settings, out CLSyncStartStatus status)
        {
            try
            {
                if (settings == null)
                {
                    throw new NullReferenceException("settings cannot be null");
                }

                lock (_locker)
                {
                    _syncSettings = settings.CopySettings();
                }

                // Check the TraceLocation vs. LogErrors
                if (string.IsNullOrWhiteSpace(_syncSettings.TraceLocation) && _syncSettings.LogErrors)
                {
                    throw new ArgumentException("TraceLocation must be set if LogErrors is checked");
                }

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(_syncSettings.TraceLocation, "Cloud", "log", _syncSettings.TraceLevel, _syncSettings.LogErrors);
                _trace.writeToLog(1, "CLSync: Starting...");

                CLError checkBadPath = Helpers.CheckForBadPath(_syncSettings.SyncRoot);
                if (checkBadPath != null)
                {
                    _trace.writeToLog(1, "CLSync: ERROR: {0}.", checkBadPath.errorDescription);
                    status = CLSyncStartStatus.ErrorBadRootPath;
                    return new ArgumentException("CloudRoot in settings represents a bad path, check it first via Helpers.CheckForBadPath", checkBadPath.GrabFirstException());
                }

                int tooLongChars;
                CLError checkPathLength = Helpers.CheckSyncRootLength(_syncSettings.SyncRoot, out tooLongChars);
                if (checkPathLength != null)
                {
                    _trace.writeToLog(1, "CLSync: ERROR: {0}.", checkPathLength.errorDescription);
                    status = CLSyncStartStatus.ErrorLongRootPath;
                    return new ArgumentException("CloudRoot in settings is too long, check it first via Helpers.CheckSyncRootLength", checkPathLength.GrabFirstException());
                }

                System.IO.DirectoryInfo rootInfo = new System.IO.DirectoryInfo(_syncSettings.SyncRoot);
                if (!rootInfo.Exists)
                {
                    rootInfo.Create();
                }

                // Don't start twice.
                _trace.writeToLog(1, "CLSync: Start: Entry.");
                if (_isStarted)
                {
                    CLError error = new Exception("Already started");
                    _trace.writeToLog(1, "CLSync: Start: ERROR: {0}.", error.errorDescription);
                    status = CLSyncStartStatus.ErrorAlreadyStarted;
                    return error;
                }

                // Start badging
                lock (_locker)
                {
                    _iconOverlay = new IconOverlay();
                }
                CLError iconOverlayError = _iconOverlay.Initialize(settings);
                if (iconOverlayError != null)
                {
                    // Failure to start badging does not prevent syncing.  Just log it.
                    _trace.writeToLog(1, "CLSync: Start: ERROR: Exception. Msg: {0}. Code: {1}.", iconOverlayError.errorDescription, iconOverlayError.errorCode);
                }

                // Start the indexer.
                _trace.writeToLog(9, "CLSync: Start: Start the indexer.");
                CLError indexCreationError;
                lock (_locker)
                {
                    indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer, _syncSettings);
                }
                if (indexCreationError != null)
                {
                    _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(2). Msg: {0}. Code: {1}.", indexCreationError.errorDescription, indexCreationError.errorCode);
                    ReleaseResources();
                    status = CLSyncStartStatus.ErrorIndexCreation;
                    return indexCreationError;
                }

                // Start the push notification.
                _trace.writeToLog(9, "CLSync: Start: Start the notifier.");
                lock (_locker)
                {
                    _notifier = CLNotification.GetInstance(_syncSettings);
                    if (_notifier == null)
                    {
                        CLError error = new Exception("Error starting push notification");
                        _trace.writeToLog(1, "CLSync: Start: ERROR(2): {0}.", error.errorDescription);
                        ReleaseResources();
                        status = CLSyncStartStatus.ErrorStartingNotification;
                        return error;
                    }
                }

                // Hook up the events
                _trace.writeToLog(9, "CLSync: Start: Hook up events.");
                _notifier.NotificationReceived += OnNotificationReceived;
                _notifier.NotificationPerformManualSyncFrom += OnNotificationPerformManualSyncFrom;
                _notifier.ConnectionError += OnNotificationConnectionError;

                // Create the http rest client
                _trace.writeToLog(9, "CLSync: Start: Create rest client.");
                CLHttpRest httpRestClient;
                CLError createRestClientError = CLHttpRest.CreateAndInitialize(_syncSettings, out httpRestClient);
                if (createRestClientError != null)
                {
                    _trace.writeToLog(1, "CLSync: Start: ERROR(3): Msg: {0}. Code: {1}.", createRestClientError.errorDescription, createRestClientError.errorCode);
                    lock (_locker)
                    {
                        _indexer.Dispose();
                        _indexer = null;
                    }
                    ReleaseResources();
                    status = CLSyncStartStatus.ErrorCreatingRestClient;
                    return createRestClientError;
                }

                // Start the monitor
                CLError fileMonitorCreationError;
                lock (_locker)
                {
                    fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(_syncSettings,
                        _indexer,
                        httpRestClient,
                        out _monitor,
                        out _syncEngine);
                }

                if (fileMonitorCreationError != null)
                {
                    _trace.writeToLog(1, "CLSync: Start: ERROR(4): Msg: {0}. Code: {1}.", fileMonitorCreationError.errorDescription, fileMonitorCreationError.errorCode);
                    lock (_locker)
                    {
                        _indexer.Dispose();
                        _indexer = null;
                    }
                    ReleaseResources();
                    status = CLSyncStartStatus.ErrorCreatingFileMonitor;
                    return fileMonitorCreationError;
                }
                else
                {
                    lock (_locker)
                    {
                        if (_monitor != null)
                        {
                            try
                            {
                                MonitorStatus returnStatus;
                                CLError fileMonitorStartError = _monitor.Start(out returnStatus);
                                if (fileMonitorStartError != null)
                                {
                                    _trace.writeToLog(1, "CLSync: Start: ERROR: Starting the MonitorAgent.  Msg: <{0}>. Code: {1}.", fileMonitorStartError.errorDescription, fileMonitorStartError.errorCode);
                                    ReleaseResources();
                                    status = CLSyncStartStatus.ErrorStartingFileMonitor;
                                    return fileMonitorStartError;
                                }

                                CLError indexerStartError = _indexer.StartInitialIndexing(
                                                _monitor.BeginProcessing,
                                                _monitor.GetCurrentPath);
                                if (indexerStartError != null)
                                {
                                    _trace.writeToLog(1, "CLSync: Start: ERROR: Starting the initial indexing.  Msg: <{0}>. Code: {1}.", indexerStartError.errorDescription, indexerStartError.errorCode);
                                    ReleaseResources();
                                    status = CLSyncStartStatus.ErrorStartingInitialIndexing;
                                    return indexerStartError;
                                }
                            }
                            catch (Exception ex)
                            {
                                CLError error = ex;
                                error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                                _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(5).  Msg: <{0}>.", ex.Message);
                                ReleaseResources();
                                status = CLSyncStartStatus.ErrorExceptionStartingFileMonitor;
                                return ex;
                            }
                        }
                    }
                }

                status = CLSyncStartStatus.Successful;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(6).  Msg: <{0}>.", ex.Message);
                ReleaseResources();
                status = CLSyncStartStatus.ErrorGeneralSyncStartException;
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
            _trace.writeToLog(1, "CLSync: OnConnectionError: Entry. ERROR: Manual poll error: <{0}>. Web socket error: <{1}>.", e.ErrorManualPoll.errorDescription, e.ErrorWebSockets.errorDescription);
            if (PushNotificationError != null)
            {
                _trace.writeToLog(1, "CLSync: OnConnectionError: Notify the application.");
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
                    _trace.writeToLog(9, "CLSync: OnNotificationPerformManualSyncFrom: Send a Perform Manual SyncFrom to monitor.");
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
                    _trace.writeToLog(9, "CLSync: OnNotificationPerformManualSyncFrom: Send a Perform PushNotification to monitor.");
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

            lock (_locker)
            {
                if (_monitor != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Stop the file monitor.");
                        MonitorStatus monitorIsStopped;
                        toReturn = _monitor.Stop(out monitorIsStopped);
                        _monitor.Dispose();
                        _monitor = null;
                        _trace.writeToLog(9, "CLSync: ReleaseResources: File monitor stopped.");
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
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Disconnect PushNotificationServer.");
                        _notifier.DisconnectPushNotificationServer();
                        _notifier = null;
                        _trace.writeToLog(9, "CLSync: ReleaseResources: PushNotificationServer disconnected.");
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
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Stop the Indexer.");
                        _indexer.Dispose();
                        _indexer = null;
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Indexer stopped.");
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
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Stop the sync engine.");
                        _syncEngine.Dispose();
                        _syncEngine = null;
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Sync engine stopped.");
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
                        _trace.writeToLog(9, "CLSync: ReleaseResources: Stop IconOverlay.");
                        _iconOverlay.Shutdown();
                        _iconOverlay = null;
                        _trace.writeToLog(9, "CLSync: ReleaseResources: IconOverlay stopped.");
                    }
                    catch (Exception ex)
                    {
                        toReturn += ex;
                    }
                }
            }

            if (toReturn != null)
            {
                toReturn.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
            }
            return toReturn;
        }

        /// <summary>
        /// Stop syncing the SyncBox and free resources.
        /// </summary>
        public void Stop()
        {
            _trace.writeToLog(1, "CLSync: Stop: Entry.");
            ReleaseResources();
        }

        /// <summary>
        /// Call when application is shutting down.
        /// </summary>
        public static void ShutdownSchedulers()
        {
            // Shuts down the HttpScheduler; after shutdown it cannot be used again
            HttpScheduler.DisposeBothSchedulers();

            // Shuts down the sync FileChange delay processing
            DelayProcessable<FileChange>.TerminateAllProcessing();
        }
    }
}