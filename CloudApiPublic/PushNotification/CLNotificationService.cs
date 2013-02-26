//  CLNotificationService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WebSocket4Net;
using WebSocket4Net.Command;
using WebSocket4Net.Protocol;
using SuperSocket.ClientEngine;
using Cloud.Support;
using Cloud.Model;
using Cloud.Static;
using Cloud.Interfaces;
using Cloud.JsonContracts;
using Cloud.REST;

namespace Cloud.PushNotification
{
    extern alias WebSocket4NetBase;

    /// <summary>
    /// Properties for a received notification message
    /// </summary>
    internal sealed class NotificationEventArgs : EventArgs
    {
        public NotificationResponse Message
        {
            get
            {
                return _message;
            }
        }
        private readonly NotificationResponse _message;

        internal NotificationEventArgs(NotificationResponse Message)
        {
            this._message = Message;
        }
    }

    /// <summary>
    /// Properties for a notification connection error
    /// </summary>
    public sealed class NotificationErrorEventArgs : EventArgs
    {
        public CLError ErrorWebSockets
        {
            get
            {
                return _errorWebSockets;
            }
        }
        private readonly CLError _errorWebSockets;

        public CLError ErrorStillDisconnectedPing
        {
            get
            {
                return _errorStillDisconnectedPing;
            }
        }
        private readonly CLError _errorStillDisconnectedPing;

        internal NotificationErrorEventArgs(CLError ErrorWebSockets, CLError ErrorStillDisconnectedPing)
        {
            this._errorWebSockets = ErrorWebSockets;
            this._errorStillDisconnectedPing = ErrorStillDisconnectedPing;
        }
    }

    /// <summary>
    /// Used to establish a connection to server notifications and provides events for notifications\errors
    /// </summary>
    internal sealed class CLNotificationService
    {
        /// <summary>
        /// Event fired when a push notification message is received from the server.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationReceived;

        /// <summary>
        /// Event fired when manual polling is being used.  The application should send
        /// a Sync_From request to the server.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationStillDisconnectedPing;

        /// <summary>
        /// Event fired when a serious error has occurred.  Push notification is
        /// no longer functional.
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs> ConnectionError;

        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private readonly CLSyncBox _syncBox;
        private bool _isServiceStarted;               // True: the push notification service has been started.
        private readonly GenericHolder<Thread> _serviceManagerThread = new GenericHolder<Thread>(null);
        private Timer _timerEngineWatcher = null;
        private ICLNotificationEngine _currentEngine = null;
        private NotificationEngines _currentEngineIndex;

        /// <summary>
        /// Tracks the subscribed clients via their SyncBoxId/DeviceId combination.
        /// </summary>
        private static readonly Dictionary<string, CLNotificationService> NotificationClientsRunning = new Dictionary<string, CLNotificationService>();

        /// <summary>
        /// Outputs the push notification server object for this client
        /// </summary>
        /// <param name="syncBox">SyncBox of this client</param>
        /// <param name="notificationServer">(output) The found or constructed notification server object</param>
        /// <returns>Returns any error that occurred retrieving the notification server object, if any</returns>
        public static CLError GetInstance(CLSyncBox syncBox, out CLNotificationService notificationServer)
        {
            try
            {
                if (syncBox == null)
                {
                    throw new NullReferenceException("syncBox cannot be null");
                }

                lock (NotificationClientsRunning)
                {
                    string syncBoxDeviceCombination = syncBox.SyncBoxId.ToString() + " " + (syncBox.CopiedSettings.DeviceId ?? string.Empty);

                    if (!NotificationClientsRunning.TryGetValue(syncBoxDeviceCombination, out notificationServer))
                    {
                        NotificationClientsRunning.Add(syncBoxDeviceCombination, notificationServer = new CLNotificationService(syncBox));
                    }
                }
            }
            catch (Exception ex)
            {
                notificationServer = Helpers.DefaultForType<CLNotificationService>();
                return ex;
            }
            return null;
        }

        // This is a private constructor, meaning no outsiders have access.
        private CLNotificationService(CLSyncBox syncBox)
        {
            try
            {
                // check input parameters

                if (syncBox == null)
                {
                    throw new NullReferenceException("syncBox cannot be null");
                }
                if (string.IsNullOrEmpty(syncBox.CopiedSettings.DeviceId))
                {
                    throw new NullReferenceException("syncBox CopiedSettings DeviceId cannot be null");
                }

                lock (this)
                {
                    // Initialize trace in case it is not already initialized.
                    CLTrace.Initialize(syncBox.CopiedSettings.TraceLocation, "Cloud", "log", syncBox.CopiedSettings.TraceLevel, syncBox.CopiedSettings.LogErrors);
                    _trace.writeToLog(9, "CLNotificationService: CLNotificationService: Entry");

                    // We should not already be started
                    if (_isServiceStarted)
                    {
                        throw new InvalidOperationException("Already started");
                    }

                    // sync settings are copied so that changes require stopping and starting notification services
                    this._syncBox = syncBox;

                    // Start the thread that will run the engines
                    StartServiceManagerThread();

                    // Initialized now
                    _isServiceStarted = true;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: CLNotificationService: ERROR: Exception: Msg: {0}.", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Call to terminate and disconnect from the push notification server.
        /// </summary>
        public void DisconnectPushNotificationServer()
        {
            try
            {
                lock (this)
                {
                    _trace.writeToLog(9, "StartEngineThread: DisconnectPushNotificationServer: Entry.");
                    _isServiceStarted = false;

                    if (_syncBox != null)
                    {
                        string syncBoxDeviceIdCombined = _syncBox.SyncBoxId.ToString() + " " + (_syncBox.CopiedSettings.DeviceId ?? string.Empty);

                        NotificationClientsRunning.Remove(syncBoxDeviceIdCombined);
                    }

                    if (_timerEngineWatcher != null)
                    {
                        _timerEngineWatcher.Dispose();
                        _timerEngineWatcher = null;
                    }

                    // Stop the current engine if it is running
                    if (_currentEngine != null)
                    {
                        _currentEngine.Stop();
                        _currentEngine = null;
                    }

                    StopServiceManagerThread();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "DisconnectPushNotificationServer: DisconnectPushNotificationServer: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        private void StartServiceManagerThread()
        {
            try
            {
                lock (this)
                {
                    _trace.writeToLog(9, "StartEngineThread: StartServiceManagerThread: Entry.");
                    if (_serviceManagerThread.Value == null)
                    {
                        _trace.writeToLog(9, "StartEngineThread: StartServiceManagerThread: Start the service manager thread.");
                        _serviceManagerThread.Value = new Thread(new ParameterizedThreadStart(this.ServiceManagerThreadProc));
                        _serviceManagerThread.Value.Name = "Notification Engine";
                        _serviceManagerThread.Value.IsBackground = true;
                        _serviceManagerThread.Value.Start(this);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "StartEngineThread: StartEngineThread: ERROR: Exception: Msg: {0}.", ex.Message);
                throw;
            }
        }

        private void StopServiceManagerThread()
        {
            try
            {
                lock (this)
                {
                    _trace.writeToLog(9, "StartEngineThread: StopServiceManagerThread: Entry.");
                    if (_serviceManagerThread.Value != null)
                    {
                        try
                        {
                            _trace.writeToLog(9, "StartEngineThread: StopServiceManagerThread: Abort the service manager thread.");
                            _serviceManagerThread.Value.Abort();
                        }
                        catch
                        {
                        }
                        _serviceManagerThread.Value = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "StartEngineThread: StopEngineThread: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        private void ServiceManagerThreadProc(object obj)
        {
            bool wasThreadAborted = false;

            try
            {
                // Initialize
                _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Entry.");
                CLNotificationService castState = obj as CLNotificationService;
                if (castState == null)
                {
                    throw new InvalidCastException("obj must be a CLNotificationService");
                }

                // Loop processing forever until we have a serious error and have to stop.  Start each loop at the top of the engine list.
                while (true)
                {
                    bool fBackToTopOfList = false;

                    // Loop through the engines (first to last, highest priority to lowest priority).
                    _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Restart at top of list.");
                    IEnumerable<NotificationEngines> engineIndices = Enum.GetValues(typeof(NotificationEngines)).Cast<NotificationEngines>();
                    foreach (NotificationEngines engineIndex in engineIndices)
                    {
                        int successes = 0;
                        int failures = 0;

                        // Loop running this particular engine
                        while (true)
                        {
                            // Construct a new instance and start this engine.
                            _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Top of loop running engine {0}.", engineIndex.ToString());
                            lock (this)
                            {
                                switch (engineIndex)
                                {
                                    case NotificationEngines.NotificationEngine_SSE:
                                        _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Instantiate SSE engine.");
                                        CLNotificationSseEngine engineSse = new CLNotificationSseEngine(
                                                    syncBox: this._syncBox,
                                                    delegateCreateEngineTimer: this.CreateEngineTimer,
                                                    delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                                    delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback,
                                                    delegateDisposeEngineTimer: this.DisposeEngineTimer,
                                                    delegateSendNotificationEvent: this.SendNotificationEventCallback);
                                        _currentEngine = engineSse;
                                        _currentEngineIndex = engineIndex;
                                        break;

                                    //case NotificationEngines.NotificationEngine_ManualPolling:
                                    //CLNotificationWebSocketsEngine engineWebSockets = new CLNotificationWebSocketseEngine(
                                    //            syncBox: this._syncBox,
                                    //            delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                    //            delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                                    //break;

                                    //case NotificationEngines.NotificationEngine_LongPolling:
                                    //CLNotificationLongPollingEngine engineLongPolling = new CLNotificationLongPollingEngine(
                                    //            syncBox: this._syncBox,
                                    //            delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                    //            delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                                    //break;

                                    case NotificationEngines.NotificationEngine_ManualPolling:
                                        _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Instantiate manual polling engine.");
                                        CLNotificationManualPollingEngine engineManualPolling = new CLNotificationManualPollingEngine(
                                                    syncBox: this._syncBox,
                                                    delegateSendManualPoll: this.SendManualPollCallback);
                                        _currentEngine = engineManualPolling;
                                        _currentEngineIndex = engineIndex;
                                        break;

                                    default:
                                        throw new InvalidOperationException("Unknown engine index");
                                }
                            }

                            // Start the engine and run it on this thread (might not return for a very long time)
                            _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Start the engine.");
                            bool engineStartDidReturnSuccess = _currentEngine.Start();

                            // Cancel any outstanding engine watcher timer.
                            DisposeEngineTimer();


                            // Determine which engine will run next
                            if (engineStartDidReturnSuccess)
                            {
                                _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Engine {0} returned success.", engineIndex.ToString());
                                ++successes;
                                if (successes >= _currentEngine.MaxSuccesses)
                                {
                                    if (_currentEngineIndex == NotificationEngines.NotificationEngine_SSE)
                                    {
                                        // Do nothing in this case.  We will go down the list if SSE has had MaxSuccesses reconnections.
                                        _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Select next in list.");
                                    }
                                    else
                                    {
                                        _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Select back to top of list.");
                                        fBackToTopOfList = true;
                                    }
                                    break;    // stop running this engine.  Select the next in the list, or start at the top of the list.
                                }
                            }
                            else
                            {
                                // The engine returned failure.
                                _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Engine {0} returned failure.", engineIndex.ToString());
                                ++failures;
                                if (failures >= _currentEngine.MaxFailures)
                                {
                                    _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Select next in list (2).");
                                }
                                break;    // stop running this engine.  Select the next in the list.
                            }
                        }   // end loop running this particular engint

                        if (fBackToTopOfList)
                        {
                            _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: Break to go to top of list.");
                            break;
                        }
                    }  // end loop through the engines to run.

                    // If all of the engines failed, stop the service.
                    if (!fBackToTopOfList)
                    {
                        // Let everyone know that we have had a serious error.
                        _trace.writeToLog(9, "StartEngineThread: ServiceManagerThreadProc: ERROR: All engines failed.");
                        NotificationErrorEventArgs err = new NotificationErrorEventArgs(new Exception("Notification service has failed"), null);
                        castState.ConnectionError(castState, err);

                        // Not running now
                        lock (this)
                        {
                            _currentEngine = null;
                            _serviceManagerThread.Value = null;
                        }

                        // Free resources
                        DisconnectPushNotificationServer();

                        return;     // exit this thread
                    }
                }
            }
            catch (ThreadAbortException ex)
            {
                wasThreadAborted = true;
            }
            catch (Exception ex)
            {
                if (!wasThreadAborted)
                {
                    _trace.writeToLog(1, "StartEngineThread: EngineThreadProc: ERROR: Exception: Msg: {0}.", ex.Message);
                }
            }
        }

        #region Callbacks

        private void SendManualPollCallback()
        {
            if (NotificationStillDisconnectedPing != null)
            {
                _trace.writeToLog(9, "CLNotificationService: PerformManualSyncFrom: Fire event to request the application to send a Sync_From request.");
                NotificationEventArgs args = new NotificationEventArgs(
                    new NotificationResponse()
                    {
                        Body = CLDefinitions.CLNotificationTypeNew
                    });
                NotificationStillDisconnectedPing(this, args);
            }
        }

        void CreateEngineTimer(object userState)
        {
            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        throw new InvalidOperationException("Already created");
                    }

                    _timerEngineWatcher = new Timer(callback: TimerCallback, state: userState, dueTime: Timeout.Infinite, period: Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: CreateEngineTimer: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        void StartEngineTimeoutCallback(int timeoutMilliseconds)
        {
            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher == null)
                    {
                        throw new InvalidOperationException("CreateEngineTimer first");
                    }

                    _timerEngineWatcher.Change(dueTime: timeoutMilliseconds, period: timeoutMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: StartEngineTimeoutCallback: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        void TimerCallback(object userState)
        {
            try
            {
                ICLNotificationEngine engineToCall = null;
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        _timerEngineWatcher.Dispose();
                        _timerEngineWatcher = null;
                    }

                    if (_currentEngine != null)
                    {
                        engineToCall = _currentEngine;
                    }
                }

                // Call the engine with this timer expiration.
                if (engineToCall != null)
                {
                    engineToCall.TimerExpired(userState);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: TimerCallback: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        void CancelEngineTimeoutCallback()
        {
            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        _timerEngineWatcher.Change(dueTime: Timeout.Infinite, period: Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: CancelEngineTimeoutCallback: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        void DisposeEngineTimer()
        {
            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        _timerEngineWatcher.Dispose();
                        _timerEngineWatcher = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: DisposeEngineTimer: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Handle a notification message from sync push notification.  The evt.data property is JSON and looks like:
        ///     {"message_body":"new","message_author":"BobSamsung"}
        /// Deserialize the JSON to a class and send the message on to its destination.
        /// </summary>
        /// <param name="evt"></param>
        void SendNotificationEventCallback(CLNotificationEvent evt)
        {
            try
            {
                _trace.writeToLog(1, "CLNotificationService: SendNotificationEventCallback: Send notification msg: <{0}>.", evt.Data);

                if ((_syncBox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    ComTrace.LogCommunication(_syncBox.CopiedSettings.TraceLocation,
                        _syncBox.CopiedSettings.DeviceId,
                        _syncBox.SyncBoxId,
                        CommunicationEntryDirection.Response,
                        evt.Origin,
                        true,
                        null,
                        evt.Data,
                        null, //<-- actually this is the valid response, but push doesn't exactly give a 200 that I can detect
                        _syncBox.CopiedSettings.TraceExcludeAuthorization);
                }

                try
                {
                    NotificationResponse parsedResponse = JsonContractHelpers.ParseNotificationResponse(evt.Data);
                    if (parsedResponse == null
                        || parsedResponse.Body != CLDefinitions.CLNotificationTypeNew
                        || parsedResponse.Author.ToUpper() != _syncBox.CopiedSettings.DeviceId.ToUpper())
                    {
                        _trace.writeToLog(9, "CLNotificationService: SendNotificationEventCallback: Send DidReceivePushNotificationFromServer.");
                        if (NotificationReceived != null)
                        {
                            _trace.writeToLog(9, "CLNotificationService: PerformManualSyncFrom: Fire event to notify the application  Msg: {0}.", evt.Data);
                            NotificationEventArgs args = new NotificationEventArgs(
                                parsedResponse);
                            NotificationReceived(this, args);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, "CLNotificationService: SendNotificationEventCallback: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: SendNotificationEventCallback: ERROR: Exception (2): Msg: {0}.", ex.Message);
            }
        }

        #endregion
    }
}
