//  CLNotificationService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
//using WebSocket4Net;
//using WebSocket4Net.Command;
//using WebSocket4Net.Protocol;
//using SuperSocket.ClientEngine;
using Cloud.Support;
using Cloud.Model;
using Cloud.Static;
using Cloud.Interfaces;
using Cloud.JsonContracts;
using Cloud.REST;

namespace Cloud.PushNotification
{
    //extern alias WebSocket4NetBase;

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
        public event EventHandler<NotificationEventArgs> NotificationReceived
        {
            add
            {
                lock (NotificationReceivedQueue)
                {
                    _notificationReceived += value;

                    while (NotificationReceivedQueue.Count > 0)
                    {
                        KeyValuePair<object, NotificationEventArgs> dequeuedNotification = NotificationReceivedQueue.Dequeue();
                        try
                        {
                            value(dequeuedNotification.Key, dequeuedNotification.Value);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            remove
            {
                lock (NotificationReceivedQueue)
                {
                    _notificationReceived -= value;
                }
            }
        }
        private event EventHandler<NotificationEventArgs> _notificationReceived;
        private readonly Queue<KeyValuePair<object, NotificationEventArgs>> NotificationReceivedQueue = new Queue<KeyValuePair<object, NotificationEventArgs>>();

        /// <summary>
        /// Event fired when manual polling is being used.  The application should send
        /// a Sync_From request to the server.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationStillDisconnectedPing
        {
            add
            {
                lock (NotificationStillDisconnectedPingQueue)
                {
                    _notificationStillDisconnectedPing += value;

                    while (NotificationStillDisconnectedPingQueue.Count > 0)
                    {
                        KeyValuePair<object, NotificationEventArgs> dequeuedNotification = NotificationStillDisconnectedPingQueue.Dequeue();
                        try
                        {
                            value(dequeuedNotification.Key, dequeuedNotification.Value);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            remove
            {
                lock (NotificationStillDisconnectedPingQueue)
                {
                    _notificationStillDisconnectedPing -= value;
                }
            }
        }
        private event EventHandler<NotificationEventArgs> _notificationStillDisconnectedPing;
        private readonly Queue<KeyValuePair<object, NotificationEventArgs>> NotificationStillDisconnectedPingQueue = new Queue<KeyValuePair<object, NotificationEventArgs>>();

        /// <summary>
        /// Event fired when a serious error has occurred.  Push notification is
        /// no longer functional.
        /// </summary>
        public event EventHandler<NotificationErrorEventArgs> ConnectionError
        {
            add
            {
                lock (ConnectionErrorQueue)
                {
                    _connectionError += value;

                    while (ConnectionErrorQueue.Count > 0)
                    {
                        KeyValuePair<object, NotificationErrorEventArgs> dequeuedNotification = ConnectionErrorQueue.Dequeue();
                        try
                        {
                            value(dequeuedNotification.Key, dequeuedNotification.Value);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            remove
            {
                lock (ConnectionErrorQueue)
                {
                    _connectionError -= value;
                }
            }
        }
        private event EventHandler<NotificationErrorEventArgs> _connectionError;
        private readonly Queue<KeyValuePair<object, NotificationErrorEventArgs>> ConnectionErrorQueue = new Queue<KeyValuePair<object, NotificationErrorEventArgs>>();

        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private readonly CLSyncbox _syncbox;
        private bool _isServiceStarted;               // True: the push notification service has been started.
        private readonly GenericHolder<Thread> _serviceManagerThread = new GenericHolder<Thread>(null);
        private Timer _timerEngineWatcher = null;
        private ICLNotificationEngine _currentEngine = null;
        private NotificationEngines _currentEngineIndex;

        /// <summary>
        /// Tracks the subscribed clients via their SyncboxId/DeviceId combination.
        /// </summary>
        private static readonly Dictionary<string, CLNotificationService> NotificationClientsRunning = new Dictionary<string, CLNotificationService>();

        /// <summary>
        /// Outputs the push notification server object for this client
        /// </summary>
        /// <param name="syncbox">Syncbox of this client</param>
        /// <param name="notificationServer">(output) The found or constructed notification server object</param>
        /// <returns>Returns any error that occurred retrieving the notification server object, if any</returns>
        public static CLError GetInstance(CLSyncbox syncbox, out CLNotificationService notificationServer)
        {
            try
            {
                if (syncbox == null)
                {
                    throw new NullReferenceException(Resources.CLNotificationServiceSyncBoxCannotBeNull);
                }

                lock (NotificationClientsRunning)
                {
                    string syncboxDeviceCombination = syncbox.SyncboxId.ToString() + " " + (syncbox.CopiedSettings.DeviceId ?? string.Empty);

                    if (!NotificationClientsRunning.TryGetValue(syncboxDeviceCombination, out notificationServer))
                    {
                        NotificationClientsRunning.Add(syncboxDeviceCombination, notificationServer = new CLNotificationService(syncbox));
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
        private CLNotificationService(CLSyncbox syncbox)
        {
            try
            {
                // check input parameters

                if (syncbox == null)
                {
                    throw new NullReferenceException(Resources.CLNotificationServiceSyncBoxCannotBeNull);
                }
                if (string.IsNullOrEmpty(syncbox.CopiedSettings.DeviceId))
                {
                    throw new NullReferenceException(Resources.CLNotificationServiceSyncBoxCopiedSettingsCannotBeNull);
                }

                lock (this)
                {
                    // Initialize trace in case it is not already initialized.
                    CLTrace.Initialize(syncbox.CopiedSettings.TraceLocation, "Cloud", Resources.IconOverlayLog, syncbox.CopiedSettings.TraceLevel, syncbox.CopiedSettings.LogErrors);
                    _trace.writeToLog(9, Resources.CLNotificationServiceEntry);

                    // We should not already be started
                    if (_isServiceStarted)
                    {
                        throw new InvalidOperationException(Resources.CLSyncEngineAlreadyStarted);
                    }

                    // sync settings are copied so that changes require stopping and starting notification services
                    this._syncbox = syncbox;

                    // Start the thread that will run the engines
                    StartServiceManagerThread();

                    // Initialized now
                    _isServiceStarted = true;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceErrorExceptionMsg0, ex.Message);
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
                string syncboxDeviceIdCombined = null;
                bool shouldStopEngine = false;

                lock (this)
                {
                    _trace.writeToLog(9, Resources.CLNotificationServiceDisconnectPushNotificationServerEntry);
                    _isServiceStarted = false;

                    if (_syncbox != null)
                    {
                        syncboxDeviceIdCombined = _syncbox.SyncboxId.ToString() + " " + _syncbox.CopiedSettings.DeviceId;
                    }
                }

                if (syncboxDeviceIdCombined != null)
                {
                    lock (NotificationClientsRunning)
                    {
                        _trace.writeToLog(9, Resources.CLNotificationServiceDisconnectNotificationServerRemoveClient0, syncboxDeviceIdCombined);
                        NotificationClientsRunning.Remove(syncboxDeviceIdCombined);
                    }
                }

                lock (this)
                {
                    if (_currentEngine != null)
                    {
                        shouldStopEngine = true;
                    }
                }

                // Stop the current engine if it is running
                if (shouldStopEngine)
                {
                    _trace.writeToLog(9, Resources.CLNotificationServiceDisconnectPushNotificationServerStopEngine);
                    _currentEngine.Stop();
                }

                // Dispose the timer if we should
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        _trace.writeToLog(9, Resources.CLNotificationServiceDisconnectPushNotificationServerDisposeEngineWatcher);
                        _timerEngineWatcher.Dispose();
                        _timerEngineWatcher = null;
                    }
                }

                lock (this)
                {
                    _trace.writeToLog(9, Resources.CLNotificationServiceDisconnectPushNotificationServerStopServiceManagerThread);
                    StopServiceManagerThread();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceDisconnectPushNotificationServerErrorExceptionMsg0, ex.Message);
            }
            _trace.writeToLog(9, Resources.CLNotificationServiceDisconnectPushNotificationServerExit);
        }

        /// <summary>
        /// Start the service manager thread.
        /// </summary>
        /// <remarks>Assumes already locked.</remarks>
        private void StartServiceManagerThread()
        {
            try
            {
                _trace.writeToLog(9, Resources.CLNotificationServiceStartServiceManagerThreadEntry);
                if (_serviceManagerThread.Value == null)
                {
                    _trace.writeToLog(9, Resources.CLNotificationServiceStartServiceManagerThreadStartTheManagerThread);
                    _serviceManagerThread.Value = new Thread(new ParameterizedThreadStart(this.ServiceManagerThreadProc));
                    _serviceManagerThread.Value.Name = "Notification Engine";
                    _serviceManagerThread.Value.IsBackground = true;
                    _serviceManagerThread.Value.Start(this);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceStartServiceManagerThreadErrorExceptionMsg0, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Stopo the service manager thread.
        /// </summary>
        /// <remarks>Assumes already locked.</remarks>
        private void StopServiceManagerThread()
        {
            try
            {
                _trace.writeToLog(9, Resources.CLNotificationServiceStopManagerThreadEntry);
                if (_serviceManagerThread.Value != null)
                {
                    try
                    {
                        _trace.writeToLog(9, Resources.CLNotificationServiceStopManagerThreadAbortTheServiceManagerThread);
                        _serviceManagerThread.Value.Abort();
                    }
                    catch
                    {
                    }
                    _serviceManagerThread.Value = null;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceStopEngineThreadErrorExceptionMsg0, ex.Message);
            }
        }

        private void ServiceManagerThreadProc(object obj)
        {
            bool wasThreadAborted = false;

            try
            {
                // Initialize
                _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcEntry);
                CLNotificationService castState = obj as CLNotificationService;
                if (castState == null)
                {
                    throw new InvalidCastException(Resources.CLNotificationServiceObjectMustBeACLNotificationService);
                }

                // Loop processing forever until we have a serious error and have to stop.  Start each loop at the top of the engine list.
                while (true)
                {
                    bool fBackToTopOfList = false;

                    // Loop through the engines (first to last, highest priority to lowest priority).
                    _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcRestartAtTopOfList);
                    IEnumerable<NotificationEngines> engineIndices = Enum.GetValues(typeof(NotificationEngines)).Cast<NotificationEngines>();
                    foreach (NotificationEngines engineIndex in engineIndices)
                    {
                        int successes = 0;
                        int failures = 0;

                        // Loop running this particular engine
                        while (true)
                        {
                            // Construct a new instance and start this engine.
                            _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcTopOfLoopRunningEngine0, engineIndex.ToString());
                            lock (this)
                            {
                                // Don't run another engine if we have been stopped.
                                if (!_isServiceStarted)
                                {
                                    _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcServiceRequestedToStopExitThread);
                                    return;             // exit this thread now
                                }

                                // Set the engine to run
                                switch (engineIndex)
                                {
                                    case NotificationEngines.NotificationEngine_SSE:
                                        _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcInstantiateSSEEngine);
                                        CLNotificationSseEngine engineSse = new CLNotificationSseEngine(
                                                    syncbox: this._syncbox,
                                                    delegateCreateEngineTimer: this.CreateEngineTimer,
                                                    delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                                    delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback,
                                                    delegateDisposeEngineTimer: this.DisposeEngineTimer,
                                                    delegateSendNotificationEvent: this.SendNotificationEventCallback,
                                                    delegateSendManualPoll: this.SendManualPollCallback);
                                        _currentEngine = engineSse;
                                        _currentEngineIndex = engineIndex;
                                        break;

                                    //case NotificationEngines.NotificationEngine_ManualPolling:
                                    //CLNotificationWebSocketsEngine engineWebSockets = new CLNotificationWebSocketseEngine(
                                    //            syncbox: this._syncbox,
                                    //            delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                    //            delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                                    //break;

                                    //case NotificationEngines.NotificationEngine_LongPolling:
                                    //CLNotificationLongPollingEngine engineLongPolling = new CLNotificationLongPollingEngine(
                                    //            syncbox: this._syncbox,
                                    //            delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                    //            delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                                    //break;

                                    case NotificationEngines.NotificationEngine_ManualPolling:
                                        _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcInstantiateManualPolling);
                                        CLNotificationManualPollingEngine engineManualPolling = new CLNotificationManualPollingEngine(
                                                    syncbox: this._syncbox,
                                                    delegateSendManualPoll: this.SendManualPollCallback);
                                        _currentEngine = engineManualPolling;
                                        _currentEngineIndex = engineIndex;
                                        break;

                                    default:
                                        throw new InvalidOperationException(Resources.CLNotificationUnknownEngineIndex);
                                }
                            }

                            // Start the engine and run it on this thread (might not return for a very long time)
                            _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcStartEngine);
                            bool engineStartDidReturnSuccess = _currentEngine.Start();
                            _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcbackFromEngineStart);

                            // Cancel any outstanding engine watcher timer.
                            DisposeEngineTimer();


                            // Determine which engine will run next
                            if (engineStartDidReturnSuccess)
                            {
                                _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcEngine0ReturnedSuccess, engineIndex.ToString());
                                ++successes;
                                if (successes >= _currentEngine.MaxSuccesses)
                                {
                                    if (_currentEngineIndex == NotificationEngines.NotificationEngine_SSE)
                                    {
                                        // Do nothing in this case.  We will go down the list if SSE has had MaxSuccesses reconnections.
                                        _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcSelectNextInList);
                                    }
                                    else
                                    {
                                        _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcSelectBackToTopOfList);
                                        fBackToTopOfList = true;
                                    }
                                    break;    // stop running this engine.  Select the next in the list, or start at the top of the list.
                                }
                            }
                            else
                            {
                                // The engine returned failure.
                                _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcEngineReturnedFailure, engineIndex.ToString());
                                ++failures;
                                if (failures >= _currentEngine.MaxFailures)
                                {
                                    _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcSelectNextInList2);
                                }
                                break;    // stop running this engine.  Select the next in the list.
                            }
                        }   // end loop running this particular engint

                        if (fBackToTopOfList)
                        {
                            _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcBreakToGoToTopOfList);
                            break;
                        }
                    }  // end loop through the engines to run.

                    // If all of the engines failed, stop the service.
                    if (!fBackToTopOfList)
                    {
                        // Let everyone know that we have had a serious error.
                        _trace.writeToLog(9, Resources.CLNotificationServiceServiceManagerThreadProcErrorAllEnginesFailed);
                        NotificationErrorEventArgs err = new NotificationErrorEventArgs(new Exception(Resources.CLNotificationServiceHasFailed), null);
                        lock (castState.ConnectionErrorQueue)
                        {
                            if (castState._connectionError != null)
                            {
                                castState._connectionError(castState, err);
                            }
                            else
                            {
                                castState.ConnectionErrorQueue.Enqueue(
                                    new KeyValuePair<object, NotificationErrorEventArgs>(
                                        castState, err));
                            }
                        }

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
            catch (ThreadAbortException)
            {
                wasThreadAborted = true;
            }
            catch (Exception ex)
            {
                if (!wasThreadAborted)
                {
                    _trace.writeToLog(1, Resources.CLNotificationServiceServiceManagerThreadProcErrorExceptionMsg0, ex.Message);
                }
            }
        }

        #region Callbacks

        private void SendManualPollCallback()
        {
            _trace.writeToLog(9, Resources.CLNotificationServicePerformMannualSyncFromFireEvent);
            NotificationEventArgs args = new NotificationEventArgs(
                new NotificationResponse()
                {
                    Body = CLDefinitions.CLNotificationTypeNew
                });
            lock (NotificationStillDisconnectedPingQueue)
            {
                if (_notificationStillDisconnectedPing != null)
                {
                    _notificationStillDisconnectedPing(this, args);
                }
                else
                {
                    NotificationStillDisconnectedPingQueue.Enqueue(
                        new KeyValuePair<object, NotificationEventArgs>(
                            this,
                            args));
                }
            }
        }

        void CreateEngineTimer(object userState)
        {
            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher == null)
                    {
                        _timerEngineWatcher = new Timer(callback: TimerCallback, state: userState, dueTime: Timeout.Infinite, period: Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceCreateEngineTimer, ex.Message);
            }
        }

        void StartEngineTimeoutCallback(int timeoutMilliseconds)
        {
            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        _timerEngineWatcher.Change(dueTime: timeoutMilliseconds, period: timeoutMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceStartEngineTimeoutCallbackError0ExMsg0, ex.Message);
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
                _trace.writeToLog(1, Resources.CLNotificationServiceTimerCallbackErrorExceptionMsg0, ex.Message);
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
                _trace.writeToLog(1, Resources.CLNotificationServiceCancelEngineTimeoutCallbackErrorExceptionMsg0, ex.Message);
            }
        }

        void DisposeEngineTimer()
        {
            bool wasThreadAborted = false;

            try
            {
                lock (this)
                {
                    if (_timerEngineWatcher != null)
                    {
                        _trace.writeToLog(1, Resources.CLNotificationServiceDisposeEngineTimerDisposeTimer);
                        _timerEngineWatcher.Dispose();
                        _trace.writeToLog(1, Resources.CLNotificationServiceDisposeEngineTimerBackFromDispose);
                        _timerEngineWatcher = null;
                    }
                }
            }
            catch (ThreadAbortException)
            {
                wasThreadAborted = true;
            }
            catch (Exception ex)
            {
                if (!wasThreadAborted)
                {
                    _trace.writeToLog(1, Resources.CLNotificationServiceDisposeEngineTimerErrorExceptionMsg0, ex.Message);
                }
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
                _trace.writeToLog(1, Resources.CLNotificationServiceSendNotificationEventCallbackSendNotificationMessage, evt.Data);

                if ((_syncbox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    ComTrace.LogCommunication(_syncbox.CopiedSettings.TraceLocation,
                        _syncbox.CopiedSettings.DeviceId,
                        _syncbox.SyncboxId,
                        CommunicationEntryDirection.Response,
                        evt.Origin,
                        true,
                        null,
                        evt.Data,
                        null, //<-- actually this is the valid response, but push doesn't exactly give a 200 that I can detect
                        _syncbox.CopiedSettings.TraceExcludeAuthorization);
                }

                try
                {
                    NotificationResponse parsedResponse = JsonContractHelpers.ParseNotificationResponse(evt.Data);
                    if (parsedResponse == null
                        || parsedResponse.Body != CLDefinitions.CLNotificationTypeNew
                        || parsedResponse.Author.ToUpper() != _syncbox.CopiedSettings.DeviceId.ToUpper())
                    {
                        _trace.writeToLog(9, Resources.CLNotificationServiceSendNotificationEventCallbackSendDidReceicePushNotificationFromServer);
                        lock (NotificationReceivedQueue)
                        {
                            _trace.writeToLog(9, Resources.CLNotificationServicePerformManualSyncFromFireEventToNotifyApp, evt.Data);
                            NotificationEventArgs args = new NotificationEventArgs(
                                parsedResponse);
                            if (_notificationReceived != null)
                            {
                                _notificationReceived(this, args);
                            }
                            else
                            {
                                NotificationReceivedQueue.Enqueue(new KeyValuePair<object, NotificationEventArgs>(this, args));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, Resources.CLNotificationServiceSendNotificationEventCallbackErrorExceptionMsg0, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationServiceSendNotificationEventCallbackErrorException2Msg0, ex.Message);
            }
        }

        #endregion
    }
}