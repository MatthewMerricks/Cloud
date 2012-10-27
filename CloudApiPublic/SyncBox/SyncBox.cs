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
    public class SyncBox
    {
        private MonitorAgent _monitor = null;
        private IndexingAgent _indexer = null;
        private CLNotification _notifier = null;
        private bool _isStarted = false;
        private static CLTrace _trace = CLTrace.Instance;

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
                    ReleaseResources();
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
                _notifier.ConnectionError += OnConnectionError;

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
                            CLError fileMonitorStartError = MonitorAgent.Start(out returnStatus);
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

        private void OnConnectionError(object sender, NotificationErrorEventArgs e)
        {
            //TODO: Raise an asynchronous error event to the application.
            throw new NotImplementedException();
        }

        private void OnNotificationPerformManualSyncFrom(object sender, NotificationEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnNotificationReceived(object sender, NotificationEventArgs e)
        {
            // Let the file monitor know about this event.
            _monitor.PushNotification(e.Message);
        }

        public void Stop()
        {
        }
    }
}
