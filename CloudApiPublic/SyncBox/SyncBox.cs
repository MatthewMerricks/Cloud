//
// SyncBox.cs
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

namespace CloudApiPublic.SyncBox
{
    /// <summary>
    /// A class used to create a SyncBox to synchronize the contents of a local disk directory.
    /// </summary>
    public class SyncBox
    {
        private MonitorAgent _monitor = null;
        private IndexingAgent _indexer = null;
        private CLNotification _notifier = null;
        private bool _isStarted = false;
        private static CLTrace _trace = CLTrace.Instance;

        /// <summary>
        /// Event fired when a serious notification error has occurred.  Push notification is
        /// no longer functional.
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs> PushNotificationError;

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
                // Don't start twice.
                _trace.writeToLog(1, "SyncBox: Start: Entry.");
                if (_isStarted)
                {
                    CLError error = new Exception("Already started");
                    _trace.writeToLog(1, "SyncBox: Start: ERROR: {0}.", error.errorDescription);
                    return error;
                }

                // Start the indexer.
                _trace.writeToLog(9, "SyncBox: Start: Start the indexer.");
                CLError indexCreationError = IndexingAgent.CreateNewAndInitialize(out _indexer);
                if (indexCreationError != null)
                {
                    _trace.writeToLog(1, "SyncBox: Start: ERROR: Exception. Msg: {0}. Code: {1}.", indexCreationError.errorDescription, indexCreationError.errorCode);
                    ReleaseResources();
                    return indexCreationError;
                }

                // Start the push notification.
                _trace.writeToLog(9, "SyncBox: Start: Start the notifier.");
                _notifier = CLNotification.GetInstance(settings);
                if (_notifier == null)
                {
                    CLError error = new Exception("Error starting push notification");
                    _trace.writeToLog(1, "SyncBox: Start: ERROR(2): {0}.", error.errorDescription);
                    ReleaseResources();
                    return error;
                }

                // Hook up the events
                _notifier.NotificationReceived += OnNotificationReceived;
                _notifier.NotificationPerformManualSyncFrom += OnNotificationPerformManualSyncFrom;
                _notifier.ConnectionError += OnNotificationConnectionError;

                // Start the monitor
                CLError fileMonitorCreationError = MonitorAgent.CreateNewAndInitialize(
                                            settings,
                                            _indexer,
                                            out _monitor,
                                            global::CloudApiPublic.Sync.SyncEngine.Run);

                if (fileMonitorCreationError != null)
                {
                    _trace.writeToLog(1, "SyncBox: Start: ERROR: Exception(2). Msg: {0}. Code: {1}.", fileMonitorCreationError.errorDescription, fileMonitorCreationError.errorCode);
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
                                _trace.writeToLog(1, "SyncBox: Start: ERROR: Starting the MonitorAgent.  Msg: <{0}>. Code: {1}.", fileMonitorStartError.errorDescription, fileMonitorStartError.errorCode);
                                ReleaseResources();
                                return fileMonitorStartError;
                            }

                            CLError indexerStartError = _indexer.StartInitialIndexing(
                                            _monitor.BeginProcessing,
                                            _monitor.GetCurrentPath);
                            if (indexerStartError != null)
                            {
                                _trace.writeToLog(1, "SyncBox: Start: ERROR: Starting the initial indexing.  Msg: <{0}>. Code: {1}.", indexerStartError.errorDescription, indexerStartError.errorCode);
                                ReleaseResources();
                                return indexerStartError;
                            }
                        }
                        catch (Exception ex)
                        {
                            _trace.writeToLog(1, "SyncBox: Start: ERROR: Exception(2).  Msg: <{0}>.", ex.Message);
                            ReleaseResources();
                            return ex;
                        }
                    }
                }
	        }
	        catch (Exception ex)
	        {
                _trace.writeToLog(1, "SyncBox: Start: ERROR: Exception(3).  Msg: <{0}>.", ex.Message);
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
            _trace.writeToLog(1, "SyncBox: OnConnectionError: Entry. ERROR: Manual poll error: <{0}>. Web socket error: <{1}>.", e.ErrorManualPoll.errorDescription, e.ErrorWebSockets.errorDescription);
            if (PushNotificationError != null)
            {
                _trace.writeToLog(1, "SyncBox: OnConnectionError: Notify the application.");
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
            _monitor.PushNotification(e.Message);
        }

        /// <summary>
        /// The notifier received a push notification message from the server.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">Arguments containing the push notification message received.</param>
        private void OnNotificationReceived(object sender, NotificationEventArgs e)
        {
            // Let the file monitor know about this event.
            _monitor.PushNotification(e.Message);
        }

        /// <summary>
        /// Stop syncing the SyncBox, and free all resources.
        /// </summary>
        private void ReleaseResources()
        {
            if (_monitor != null)
            {
                MonitorStatus monitorIsStopped;
                _monitor.Stop(out monitorIsStopped);
                _monitor.Dispose();
                _monitor = null;
            }

            if (_notifier != null)
            {
                _notifier.DisconnectPushNotificationServer();
                _notifier = null;
            }

            if (_indexer != null)
            {
                _indexer.Dispose();
                _indexer = null;
            }
        }

        /// <summary>
        /// Stop syncing the SyncBox and free resources.
        /// </summary>
        public void Stop()
        {
            _trace.writeToLog(1, "SyncBox: Stop: Entry.");
            ReleaseResources();
        }
    }
}
