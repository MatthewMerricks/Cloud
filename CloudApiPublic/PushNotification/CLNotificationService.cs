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
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Interfaces;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.REST;

namespace CloudApiPublic.PushNotification
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

        private static CLNotificationService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private readonly CLSyncBox _syncBox;
        private bool _serviceStarted;               // True: the push notification service has been started.
        private readonly GenericHolder<Thread> _engineThread = new GenericHolder<Thread>(null);
        private Dictionary<NotificationEngines, ICLNotificationEngine> _dictEngines;  // holds the various push notification engines
        private Timer _timerEngine = null;
        private ICLNotificationEngine _currentEngine = null;

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

                    // sync settings are copied so that changes require stopping and starting notification services
                    this._syncBox = syncBox;

                    this._dictEngines = new Dictionary<NotificationEngines, ICLNotificationEngine>();

                    // Construct the engines
                    CLNotificationSseEngine engineSse = new CLNotificationSseEngine(
                                syncBox: this._syncBox,
                                delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                    _dictEngines.Add(NotificationEngines.NotificationEngine_SSE, engineSse);

                    //CLNotificationWebSocketsEngine engineWebSockets = new CLNotificationWebSocketseEngine(
                    //            syncBox: this._syncBox,
                    //            delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                    //            delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                    //_dictEngines.Add(NotificationEngines.NotificationEngine_WebSockets, engineWebSockets);

                    //CLNotificationLongPollingEngine engineLongPolling = new CLNotificationLongPollingEngine(
                    //            syncBox: this._syncBox,
                    //            delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                    //            delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback);
                    //_dictEngines.Add(NotificationEngines.NotificationEngine_LongPolling, engineLongPolling);

                    CLNotificationManualPollingEngine engineManualPolling = new CLNotificationManualPollingEngine(
                                syncBox: this._syncBox,
                                delegateStartEngineTimeout: this.StartEngineTimeoutCallback,
                                delegateCancelEngineTimeout: this.CancelEngineTimeoutCallback,
                                delegateSendManualPoll: this.SendManualPollCallback);
                    _dictEngines.Add(NotificationEngines.NotificationEngine_ManualPolling, engineManualPolling);

                    // Start the thread that will run the engines
                    StartEngineThread();

                    // Initialized now
                    _serviceStarted = true;
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
                    if (_syncBox != null)
                    {
                        string syncBoxDeviceIdCombined = _syncBox.SyncBoxId.ToString() + " " + (_syncBox.CopiedSettings.DeviceId ?? string.Empty);

                        NotificationClientsRunning.Remove(syncBoxDeviceIdCombined);
                    }

                    if (_timerEngine != null)
                    {
                        _timerEngine.Dispose();
                        _timerEngine = null;
                    }

                    StopEngineThread();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "DisconnectPushNotificationServer: DisconnectPushNotificationServer: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        private void StartEngineThread()
        {
            try
            {
                lock (this)
                {
                    if (_engineThread.Value == null)
                    {
                        _engineThread.Value = new Thread(new ParameterizedThreadStart(this.EngineThreadProc));
                        _engineThread.Value.Name = "Notification Engine";
                        _engineThread.Value.IsBackground = true;
                        _engineThread.Value.Start(this);
                    }
                }

            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "StartEngineThread: StartEngineThread: ERROR: Exception: Msg: {0}.", ex.Message);
                throw;
            }
        }

        private void StopEngineThread()
        {
            try
            {
                lock (this)
                {
                    if (_engineThread.Value != null)
                    {
                        try
                        {
                            _engineThread.Value.Abort();
                        }
                        catch
                        {
                        }
                        _engineThread.Value = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "StartEngineThread: StopEngineThread: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        private void EngineThreadProc(object obj)
        {
            // Initialize
            CLNotificationService castState = obj as CLNotificationService;
            if (castState == null)
            {
                throw new InvalidCastException("obj must be a CLNotificationService");
            }

            try
            {
                // Loop processing forever until we have a serious error and have to stop.
                while (true)
                {
                    bool fBackToTopOfList = false;

                    // Loop through the engines (first to last, highest priority to lowest priority).
                    IEnumerable<NotificationEngines> engineIndices = Enum.GetValues(typeof(NotificationEngines)).Cast<NotificationEngines>();
                    foreach (NotificationEngines engineIndex in engineIndices)
                    {
                        int successes = 0;
                        int failures = 0;

                        // Loop running this particular engine
                        while (true)
                        {
                            // Start this engine
                            bool engineStartDidReturnSuccess = _dictEngines[engineIndex].Start();
                            if (engineStartDidReturnSuccess)
                            {
                                ++successes;
                                if (successes >= _dictEngines[engineIndex].MaxSuccesses)
                                {
                                    fBackToTopOfList = true;
                                    break;          // start back at the top of the list again.
                                }
                            }
                            else
                            {
                                // Start failed for this engine
                                ++failures;
                                if (failures >= _dictEngines[engineIndex].MaxFailures)
                                {
                                    // DO NOTHING. Iterate to the next engine down the list.
                                }
                            }
                        }

                        if (fBackToTopOfList)
                        {
                            break;              // back to the top of the engine list
                        }
                    }

                    // If all of the engines failed, stop the service.
                    if (!fBackToTopOfList)
                    {
                        //TODO: Notify sync push notification stopped.
                        return;     // exit this thread
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "StartEngineThread: EngineThreadProc: ERROR: Exception: Msg: {0}.", ex.Message);
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

        void StartEngineTimeoutCallback(int timeoutMilliseconds, object userState)
        {
            try
            {
                lock (this)
                {
                    if (_timerEngine != null)
                    {
                        _timerEngine.Dispose();
                        _timerEngine = new Timer(callback: TimerCallback, state: userState, dueTime: 0, period: timeoutMilliseconds);
                    }
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
                    if (_timerEngine != null)
                    {
                        _timerEngine.Dispose();
                        _timerEngine = null;
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
                    if (_timerEngine != null)
                    {
                        _timerEngine.Dispose();
                        _timerEngine = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: CancelEngineTimeoutCallback: ERROR: Exception: Msg: {0}.", ex.Message);
            }
        }

        #endregion





#if TRASH
        public void ConnectPushNotificationServerSse()
        {
            try
            {
                _trace.writeToLog(9, "CLNotificationService: ConnectPushNotifriicationServerSse: Entry.");
                string url = CLDefinitions.HttpPrefix + CLDefinitions.SubDomainPrefix + CLDefinitions.Domain;
                string pathAndQueryStringAndFragment = String.Format(CLDefinitions.MethodPathPushSubscribe + "?sync_box_id={0}&device={1}", _syncBox.SyncBoxId, _syncBox.CopiedSettings.DeviceId);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add(
                    CLDefinitions.HeaderKeyAuthorization,
                    CLDefinitions.HeaderAppendCWS0 +
                                            CLDefinitions.HeaderAppendKey +
                                            _syncBox.Credential.Key + ", " +
                                            CLDefinitions.HeaderAppendSignature +
                                            Helpers.GenerateAuthorizationHeaderToken(
                                                secret: _syncBox.Credential.Secret,
                                                httpMethod: CLDefinitions.HeaderAppendMethodGet,
                                                pathAndQueryStringAndFragment: pathAndQueryStringAndFragment) +
                    // Add token if specified
                                                (!String.IsNullOrEmpty(_syncBox.Credential.Token) ?
                                                    CLDefinitions.HeaderAppendToken + _syncBox.Credential.Token :
                                                    String.Empty));

                if ((_syncBox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    ComTrace.LogCommunication(_syncBox.CopiedSettings.TraceLocation,
                        _syncBox.CopiedSettings.DeviceId,
                        _syncBox.SyncBoxId,
                        CommunicationEntryDirection.Request,
                        url + pathAndQueryStringAndFragment,
                        true,
                        null,
                        null,
                        null,
                        null,
                        _syncBox.CopiedSettings.TraceExcludeAuthorization);
                }

                SseRequest sseRequest = new SseRequest();
                sseRequest.DeviceId = _syncBox.CopiedSettings.DeviceId;
                sseRequest.SyncBoxId = _syncBox.SyncBoxId;


                CLHttpRestStatus status;
                JsonContracts.SseResponse response;
                CLError errorFromConnectServerSentEvents = _httpRestClient.TestConnectServerSentEvents(sseRequest, CLDefinitions.HttpTimeoutDefaultMilliseconds, out status, out response);
                if (errorFromConnectServerSentEvents != null)
                {
                    _trace.writeToLog(1, "CLNotificationService: ConnectPushNotificationServer: ERROR: From TestConnectServerSentEvents. Msg: <{0}>.", errorFromConnectServerSentEvents.errorDescription);
                    errorFromConnectServerSentEvents.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
                }

            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "CLNotificationService: ConnectPushNotificationServer: ERROR: Exception connecting with the push server. Msg: <{0}>.", ex.Message);
                error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
            }
        }
#endif // TRASH
    }
}
