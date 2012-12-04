//
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

        /// <summary>
        /// ¡¡ Call this carefully, completely wipes index database (use when user deletes local repository or relinks) !!
        /// </summary>
        /// <param name="newRootPath">Full path string to directory to sync without any trailing slash (except for drive letter root)</param>
        /// <returns>Returns any error that occurred while wiping the database index</returns>
        public CLError WipeIndex()
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

        /// <summary>
        /// Initialize the SyncBox and start syncing its contents to the Cloud server, and to other devices
        /// registering the same SyncBox.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public CLError Start(ISyncSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    throw new NullReferenceException("settings cannot be null");
                }

                _syncSettings = settings.CopySettings();

                // Initialize trace in case it is not already initialized.
                CLTrace.Initialize(_syncSettings.TraceLocation, "Cloud", "log", _syncSettings.TraceLevel, _syncSettings.LogErrors);

                System.IO.DirectoryInfo rootInfo = new System.IO.DirectoryInfo(_syncSettings.CloudRoot);
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
                    return error;
                }

                // Start badging
                _iconOverlay = new IconOverlay();
                CLError iconOverlayError = _iconOverlay.Initialize(settings);
                if (iconOverlayError != null)
                {
                    _trace.writeToLog(1, "CLSync: Start: ERROR: Exception. Msg: {0}. Code: {1}.", iconOverlayError.errorDescription, iconOverlayError.errorCode);
                    ReleaseResources();
                    return iconOverlayError;
                }

                // Start the indexer.
                _trace.writeToLog(9, "CLSync: Start: Start the indexer.");
                CLError indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer, _syncSettings.Uuid, _syncSettings.DatabaseFile);
                if (indexCreationError != null)
                {
                    _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(2). Msg: {0}. Code: {1}.", indexCreationError.errorDescription, indexCreationError.errorCode);
                    ReleaseResources();
                    return indexCreationError;
                }

                // Start the push notification.
                _trace.writeToLog(9, "CLSync: Start: Start the notifier.");
                _notifier = CLNotification.GetInstance(_syncSettings);
                if (_notifier == null)
                {
                    CLError error = new Exception("Error starting push notification");
                    _trace.writeToLog(1, "CLSync: Start: ERROR(2): {0}.", error.errorDescription);
                    ReleaseResources();
                    return error;
                }

                // Hook up the events
                _notifier.NotificationReceived += OnNotificationReceived;
                _notifier.NotificationPerformManualSyncFrom += OnNotificationPerformManualSyncFrom;
                _notifier.ConnectionError += OnNotificationConnectionError;

                // Start the monitor
                CLError fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(_syncSettings,
                    _indexer,
                    out _monitor,
                    out _syncEngine);

                if (fileMonitorCreationError != null)
                {
                    _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(3). Msg: {0}. Code: {1}.", fileMonitorCreationError.errorDescription, fileMonitorCreationError.errorCode);
                    _indexer.Dispose();
                    _indexer = null;
                    ReleaseResources();
                    return fileMonitorCreationError;
                }
                else
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
                                return fileMonitorStartError;
                            }

                            CLError indexerStartError = _indexer.StartInitialIndexing(
                                            _monitor.BeginProcessing,
                                            _monitor.GetCurrentPath);
                            if (indexerStartError != null)
                            {
                                _trace.writeToLog(1, "CLSync: Start: ERROR: Starting the initial indexing.  Msg: <{0}>. Code: {1}.", indexerStartError.errorDescription, indexerStartError.errorCode);
                                ReleaseResources();
                                return indexerStartError;
                            }
                        }
                        catch (Exception ex)
                        {
                            CLError error = ex;
                            error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                            _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(4).  Msg: <{0}>.", ex.Message);
                            ReleaseResources();
                            return ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                _trace.writeToLog(1, "CLSync: Start: ERROR: Exception(5).  Msg: <{0}>.", ex.Message);
                ReleaseResources();
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
            if (_monitor != null)
            {
                _monitor.PushNotification(e.Message);
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
            if (_monitor != null)
            {
                _monitor.PushNotification(e.Message);
            }
        }

        /// <summary>
        /// Stop syncing the SyncBox, and free all resources.
        /// </summary>
        private CLError ReleaseResources()
        {
            CLError toReturn = null;

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
        /// Shuts down the HttpScheduler; after shutdown it cannot be used again
        /// </summary>
        public static void PermanentShutdownHttpSchedulers()
        {
            HttpScheduler.DisposeBothSchedulers();
        }
    }
}