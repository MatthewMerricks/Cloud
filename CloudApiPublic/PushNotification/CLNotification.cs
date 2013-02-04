﻿//  CLNotification.cs
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
    internal sealed class CLNotification
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
 
        private static CLNotification _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private WebSocket _connection = null;
        private MessageReceiver urlReceiver = null;
        private bool _isInitialized = false;
        private readonly CLSyncBox _syncBox;
        private bool _serviceStarted;               // True: the push notification service has been started.
        private bool _pushConnected = false;
        private int _faultCount = 0;
        private int _manualPollIntervalSeconds = 0;     // the actual number of seconds to use.  This is random between the max and min.

        /// <summary>
        /// Tracks the subscribed clients via their SyncBoxId/DeviceId combination.
        /// </summary>
        private static readonly Dictionary<string, CLNotification> NotificationClientsRunning = new Dictionary<string, CLNotification>();

        /// <summary>
        /// Outputs the push notification server object for this client
        /// </summary>
        /// <param name="syncBox">SyncBox of this client</param>
        /// <param name="notifications">(output) The found or constructed notification server object</param>
        /// <returns>Returns any error that occurred retrieving the notification server object, if any</returns>
        public static CLError GetInstance(CLSyncBox syncBox, out CLNotification notifications)
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

                    if (!NotificationClientsRunning.TryGetValue(syncBoxDeviceCombination, out notifications))
                    {
                        NotificationClientsRunning.Add(syncBoxDeviceCombination, notifications = new CLNotification(syncBox));
                    }
                }
            }
            catch (Exception ex)
            {
                notifications = Helpers.DefaultForType<CLNotification>();
                return ex;
            }
            return null;
        }

        // This is a private constructor, meaning no outsiders have access.
        private CLNotification(CLSyncBox syncBox)
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

            // Determine the manual polling interval to use for this instance.
            Random rnd = new Random();
            _manualPollIntervalSeconds = rnd.Next(CLDefinitions.MinManualPollingPeriodSeconds, CLDefinitions.MaxManualPollingPeriodSeconds);

            // sync settings are copied so that changes require stopping and starting notification services
            this._syncBox = syncBox;

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(syncBox.CopiedSettings.TraceLocation, "Cloud", "log", syncBox.CopiedSettings.TraceLevel, syncBox.CopiedSettings.LogErrors);
            CLTrace.Instance.writeToLog(9, "CLNotification: CLNotification: Entry");

            // Initialize members, etc. here (at static initialization time).
            //&&&&&ConnectPushNotificationServer();  //TODO: DEBUG ONLY.  REMOVE.
            ConnectPushNotificationServerSse();
        }

        public void ConnectPushNotificationServerSse()
        {
            try
            {

                _trace.writeToLog(9, "CLNotification: ConnectPushNotifriicationServerSse: Entry.");
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

                CLError errorFromConnectServerSentEvents = TestConnectServerSentEvents(sseRequest, HttpTimeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SseResponse response)

            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Call to initialize and make a connection to the push notification server.
        /// </summary>
        public void ConnectPushNotificationServer()
        {
            _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Entry.");
            bool fallbackToManualPolling = false;

            if (_faultCount >= CLDefinitions.PushNotificationFaultLimitBeforeFallback)
            {
                _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Set fallbackToManualPolling.");
                fallbackToManualPolling = true;
            }
            else
            {
                // WebSocket4Net implementation.
                try
                {
                    string url = CLDefinitions.CLNotificationServerURL;
                    string pathAndQueryStringAndFragment = String.Format(CLDefinitions.MethodPathPushSubscribe + "?sync_box_id={0}&device={1}", _syncBox.SyncBoxId, _syncBox.CopiedSettings.DeviceId);
                    _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Establish connection with push server. url: <{0}>. QueryString: {1}.", url, pathAndQueryStringAndFragment);

                    //¡¡ Remember to exclude authentication from trace once web socket authentication is implemented based on _syncSettings.TraceExcludeAuthorization !!
                    if ((_syncBox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                    {
                        ComTrace.LogCommunication(_syncBox.CopiedSettings.TraceLocation,
                            _syncBox.CopiedSettings.DeviceId,
                            _syncBox.SyncBoxId,
                            CommunicationEntryDirection.Request,
                            url + pathAndQueryStringAndFragment,
                            true,
                            null,
                            (string)null,
                            null,
                            _syncBox.CopiedSettings.TraceExcludeAuthorization);
                    }

                    string webSocketOpenStatus = "Entered action to open WebSocket";
                    lock (this)
                    {
                        _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Allocate WebSocket.");
                        _connection = new WebSocket(
                            uri: url + pathAndQueryStringAndFragment,
                            subProtocol: null,
                            cookies: null,
                            customHeaderItems: new List<KeyValuePair<string, string>>()
                                {
                                    new KeyValuePair<string, string>(
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
                                                    String.Empty))
                                },
                            userAgent: String.Empty,
                            origin: String.Empty,
                            version: WebSocketVersion.Rfc6455);

                        webSocketOpenStatus = "Instantiated new WebSocket";
                        _connection.Opened += OnConnectionOpened;
                        webSocketOpenStatus = "Attached connection opened handler";
                        try
                        {
                            _connection.Error += OnConnectionError;
                            webSocketOpenStatus = "Attached connection error handler";
                            try
                            {
                                _connection.Closed += OnConnectionClosed;
                                webSocketOpenStatus = "Attached connection closed handler";
                                try
                                {
                                    _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Allocate MessageReceiver.");
                                    urlReceiver = new MessageReceiver(url, _syncBox, (sender, e) =>
                                    {
                                        if (NotificationReceived != null)
                                        {
                                            NotificationReceived(sender, e);
                                        }
                                    });
                                    webSocketOpenStatus = "Instantiated new MessageReceiver";
                                    _connection.MessageReceived += urlReceiver.OnConnectionReceived;
                                    webSocketOpenStatus = "Attached connection received handler";
                                    try
                                    {
                                        _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Open the connection.");
                                        _connection.Open();
                                        _pushConnected = true;
                                        _serviceStarted = true;
                                        _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Connection opened.");
                                    }
                                    catch
                                    {
                                        _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: ERROR. Exception on connection open.");
                                        _connection.MessageReceived -= urlReceiver.OnConnectionReceived;
                                        throw;
                                    }
                                }
                                catch
                                {
                                    _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: ERROR. Exception allocation MessageReceiver.");
                                    _connection.Closed -= OnConnectionClosed;
                                    throw;
                                }
                            }
                            catch
                            {
                                _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: ERROR. Exception subscribing to ConnectionClosed.");
                                _connection.Error -= OnConnectionError;
                                throw;
                            }
                        }
                        catch (Exception ex)
                        {
                            _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: ERROR. Exception subscribing to ConnectionError.");
                            _connection.Opened -= OnConnectionOpened;
                            try
                            {
                                _connection.Close();
                            }
                            catch
                            {
                                _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: ERROR. Exception from connection Close.");
                            }
                            _connection = null;
                            throw new AggregateException("Error creating and opening WebSocket with last successful state: " + webSocketOpenStatus, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    _trace.writeToLog(1, "CLNotification: ConnectPushNotificationServer: ERROR: Exception connecting with the push server. Msg: <{0}>, Code: {1}.", error.errorDescription, ((int)error.code).ToString());
                    error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);

                    fallbackToManualPolling = true;
                }
            }

            if (fallbackToManualPolling)
            {
                _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Queue FallbackToManualPolling.");
                ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
            }

            lock (this)
            {
                _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Mark _isInitialized.");
                _isInitialized = true;
            }
        }

        private static void FallbackToManualPolling(object state)
        {
            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Entry.");
            CLNotification castState = state as CLNotification;

            if (castState != null)
            {
                _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Set faultCount to zero.");
                castState._faultCount = 0;
            }

            bool manualPollSuccessful = false;

            bool servicesStartedSet = false;
            for (int manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1; manualPollingIteration >= 0; manualPollingIteration--)
            {
                _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Top of manualPollingIteration loop.");

                // Loop checking for termination.  This loop allows this thread pool thread to exit earlier, rather than waiting for a long delay to check at the next poll
                int innerLoopCount = castState._manualPollIntervalSeconds;   // number of one-second sleeps before the next poll
                for (int i = 0; i < innerLoopCount; ++i)
                {
                    lock (castState)
                    {
                        // Only the first time in, indicate that our push service is started.
                        if (!servicesStartedSet)
                        {
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: !servicesStartedSet.");
                            if (castState != null)
                            {
                                _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Set _serviceStarted.");
                                castState._serviceStarted = true;
                            }
                            servicesStartedSet = true;
                        }
                        // Other iterations will exit the thread if there was an error.
                        else if (castState != null
                            && !castState._serviceStarted)
                        {
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Exit thread.");
                            return;
                        }

                        // Exit if we have been closed or not initialized.
                        if (!castState._isInitialized)
                        {
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling (2): Exit thread.");

                            castState._pushConnected = false;
                            castState._serviceStarted = false;
                            castState.CleanWebSocketAndRestart(castState._connection, doNotRestart: true);

                            return;
                        }
                    }

                    Thread.Sleep(1000);
                }

                // Time to perform a manual sync_from to the MDS server.
                CLError storeManualPollingError = null;
                try
                {
                    _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Call PerformManualSyncFrom.");
                    castState.PerformManualSyncFrom();

                    manualPollSuccessful = true;
                }
                catch (Exception ex)
                {
                    storeManualPollingError = ex;
                    _trace.writeToLog(1, "CLNotification: FallbackToManualPolling: ERROR: Exception occurred trying to reconnect to push after manually polling. Msg: <{0}>, Code: {1}.", storeManualPollingError.errorDescription, ((int)storeManualPollingError.code).ToString());
                    storeManualPollingError.LogErrors(castState._syncBox.CopiedSettings.TraceLocation, castState._syncBox.CopiedSettings.LogErrors);
                }

                if (manualPollingIteration == 0)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: manualPollingIteration is zero.");
                        castState.ConnectPushNotificationServer();
                    }
                    catch (Exception innerEx)
                    {
                        _trace.writeToLog(1, "CLNotification: FallbackToManualPolling: ERROR: Exception.  Msg: <{0}>.", innerEx.Message);
                        if (castState != null)
                        {
                            lock (castState)
                            {
                                _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Reset _serviceStarted.");
                                castState._serviceStarted = false;
                            }
                        }

                        bool forceErrors = false;

                        CLError error = innerEx;
                        _trace.writeToLog(1, "CLNotification: FallbackToManualPolling: ERROR: Exception occurred during manual polling. Msg: <{0}>, Code: {1}.", error.errorDescription, ((int)error.code).ToString());
                        if (!manualPollSuccessful
                            && storeManualPollingError != null)
                        {
                            // Force logging errors in the serious case where a message had to be displayed
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Put up an ugly MessageBox to the user.");
                            forceErrors = true;
                            if (!castState._syncBox.CopiedSettings.LogErrors)
                            {
                                storeManualPollingError.LogErrors(castState._syncBox.CopiedSettings.TraceLocation, true);
                            }

                            // Serious error, unable to reconnect to push notification AND unable to manually poll
                            if (castState.ConnectionError != null)
                            {
                                NotificationErrorEventArgs err = new NotificationErrorEventArgs(
                                    innerEx,
                                    storeManualPollingError);
                                castState.ConnectionError(castState, err);
                            }
                        }
                        else
                        {
                            manualPollSuccessful = false;
                            manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1;
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Decremented manualPollingIteration count: {0}.", manualPollingIteration);
                            Thread.Sleep(castState._manualPollIntervalSeconds * 1000);
                        }

                        error.LogErrors(castState._syncBox.CopiedSettings.TraceLocation, forceErrors || castState._syncBox.CopiedSettings.LogErrors);
                    }
                }
                else
                {
                    Thread.Sleep(castState._manualPollIntervalSeconds * 1000);
                }
            }
            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Exit thread (3).");
        }

        private void PerformManualSyncFrom()
        {
            if (NotificationStillDisconnectedPing != null)
            {
                _trace.writeToLog(9, "CLNotification: PerformManualSyncFrom: Fire event to request the application to send a Sync_From request.");
                NotificationEventArgs args = new NotificationEventArgs(
                    new NotificationResponse()
                    {
                        Body = CLDefinitions.CLNotificationTypeNew
                    });
                NotificationStillDisconnectedPing(this, args);
            }
        }

        private void OnConnectionOpened(object sender, EventArgs e)
        {
            _trace.writeToLog(9, "CLNotification: OnConnectionOpened: Connection opened.  Set faultCount to zero.");
            _faultCount = 0;
        }

        delegate void OnConnectionErrorDelegate(object sender, WebSocket4NetBase.SuperSocket.ClientEngine.ErrorEventArgs e);
        private void OnConnectionError(object sender, WebSocket4NetBase.SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            bool forceErrors = false;
            try
            {
                _trace.writeToLog(1, "CLNotification: OnConnectionError: Connection error.  Message: <{0}>. Set faultCount to zero.", e.Exception.Message);
                CleanWebSocketAndRestart((WebSocket)sender);
            }
            catch (Exception ex)
            {
                // Override error logging because we had a serious case where we had to display a message
                forceErrors = true;

                CLError innerError = ex;
                innerError.LogErrors(_syncBox.CopiedSettings.TraceLocation, true);
                _trace.writeToLog(1, "CLNotification: OnConnectionError: ERROR. Error while restarting WebSocket.  Msg: <{0}>, Code: {1}.", innerError.errorDescription, ((int)innerError.code).ToString());

                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with errors:" + Environment.NewLine +
                    e.Exception.Message + Environment.NewLine +
                    "AND" + Environment.NewLine + ex.Message);
            }
            CLError error = e.Exception;
            error.LogErrors(_syncBox.CopiedSettings.TraceLocation, forceErrors || _syncBox.CopiedSettings.LogErrors);
            _trace.writeToLog(1, "CLNotification: OnConnectionError: ERROR.  Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, ((int)error.code).ToString());
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            try
            {
                _trace.writeToLog(9, "CLNotification: OnConnectionClosed: Entry. Call CleanWebSocketAndRestart.");
                CleanWebSocketAndRestart((WebSocket)sender);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                // Always log errors here because we had a serious case where we had to display a message
                error.LogErrors(_syncBox.CopiedSettings.TraceLocation, true);
                _trace.writeToLog(1, "CLNotification: OnConnectionClosed: ERROR. Error while restarting WebSocket.  Msg: <{0}>, Code: {1}.", error.errorDescription, ((int)error.code).ToString());

                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with error:" + Environment.NewLine +
                    ex.Message);
            }
        }

        private void CleanWebSocketAndRestart(WebSocket sender, bool doNotRestart = false)
        {
            if (sender != null)
            {
                lock (this)
                {
                    _faultCount++;
                    _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Entry. doNotRestart: {0}. faultCount: {1}.", doNotRestart, _faultCount);
                    if (_faultCount >= CLDefinitions.PushNotificationFaultLimitBeforeFallback)
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: Set doNotRestart.");
                        doNotRestart = true;
                    }

                    if (urlReceiver != null)
                    {
                        try
                        {
                            _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Remove OnConnectionReceived.");
                            sender.MessageReceived -= urlReceiver.OnConnectionReceived;
                            urlReceiver = null;
                        }
                        catch
                        {
                            _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR: Exception.");
                        }
                    }

                    try
                    {
                        _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Removed OnConnectionClosed.");
                        sender.Closed -= OnConnectionClosed;
                    }
                    catch
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR: Exception(2).");
                    }

                    try
                    {
                        _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Remove OnConnectionError.");
                        sender.Error -= OnConnectionError;
                    }
                    catch
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR: Exception(3).");
                    }

                    try
                    {
                        _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Remove OnConnectionOpened.");
                        sender.Opened -= OnConnectionOpened;
                    }
                    catch
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR: Exception(4).");
                    }

                    try
                    {
                        _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Close Sender.");
                        sender.Close();
                    }
                    catch
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR: Exception(5).");
                    }

                    try
                    {
                        _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Set pushConnected and _serviceStarted false.");
                        _pushConnected = false;
                        _serviceStarted = false;
                        if (_connection != null
                            && _connection == sender)
                        {
                            _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Set _connection null.");
                            _connection = null;
                        }
                    }
                    catch
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR: Exception(6).");
                    }

                    if (doNotRestart)
                    {
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: doNotRestart (FallbackToManualPolling)");

                        ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
                    }
                    else
                    {
                        try
                        {
                            _trace.writeToLog(9, "CLNotification: CleanWebSocketAndRestart: Attempt restart.  Call ConnectPushNotificationServer.");
                            ConnectPushNotificationServer();
                        }
                        catch (Exception ex)
                        {
                            CLError error = ex;
                            error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
                            _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR. Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, ((int)error.code).ToString());

                            ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
                        }
                    }
                }
            }
        }

        private class MessageReceiver
        {
            private readonly string _url;
            private readonly CLSyncBox _syncBox;
            private readonly EventHandler<NotificationEventArgs> _notificationReceived;

            public MessageReceiver(string url, CLSyncBox syncBox, EventHandler<NotificationEventArgs> notificationReceived)
            {
                this._url = url;
                this._syncBox = syncBox;
                this._notificationReceived = notificationReceived;
            }

            public void OnConnectionReceived(object sender, MessageReceivedEventArgs e)
            {
                _trace.writeToLog(1, "CLNotification: OnConnectionReceived: Received msg: <{0}>.", e.Message);

                if ((_syncBox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    ComTrace.LogCommunication(_syncBox.CopiedSettings.TraceLocation,
                        _syncBox.CopiedSettings.DeviceId,
                        _syncBox.SyncBoxId,
                        CommunicationEntryDirection.Response,
                        this._url,
                        true,
                        null,
                        e.Message,
                        null, //<-- actually this is the valid response, but push doesn't exactly give a 200 that I can detect
                        _syncBox.CopiedSettings.TraceExcludeAuthorization);
                }

                try
                {
                    NotificationResponse parsedResponse = JsonContractHelpers.ParseNotificationResponse(e.Message);
                    if (parsedResponse == null
                        || parsedResponse.Body != CLDefinitions.CLNotificationTypeNew
                        || parsedResponse.Author.ToUpper() != _syncBox.CopiedSettings.DeviceId.ToUpper())
                    {
                        _trace.writeToLog(9, "CLNotification: OnConnectionReceived: Send DidReceivePushNotificationFromServer.");
                        if (_notificationReceived != null)
                        {
                            _trace.writeToLog(9, "CLNotification: PerformManualSyncFrom: Fire event to notify the application  Msg: {0}.", e.Message);
                            NotificationEventArgs args = new NotificationEventArgs(
                                parsedResponse);
                            _notificationReceived(this, args);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, "CLNotification: OnConnectionReceived: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                }
            }
        }

        /// <summary>
        /// Call to terminate and disconnect from the push notification server.
        /// </summary>
        public void DisconnectPushNotificationServer()
        {
            lock (this)
            {
                if (!_isInitialized)
                {
                    throw new Exception("Call ConnectPushNotificationServer first.");
                }
            }

            try
            {
                _trace.writeToLog(9, "CLNotification: DisconnectPushNotificationServer: Entry.");
                if (_serviceStarted)
                {
                    _trace.writeToLog(9, "CLNotification: DisconnectPushNotificationServer: Service started.");
                    if (_pushConnected)
                    {
                        _trace.writeToLog(9, "CLNotification: DisconnectPushNotificationServer: Push connected.");
                        if (_connection != null)
                        {
                            _trace.writeToLog(9, "CLNotification: DisconnectPushNotificationServer: Call CleanWebSocketAndRestart.");
                            CleanWebSocketAndRestart(_connection, doNotRestart: true);
                            _trace.writeToLog(1, "CLNotification: DisconnectPushNotificationServer: After call to CleanWebSocketAndRestart.");
                        }
                    }
                    else
                    {
                        _trace.writeToLog(9, "CLNotification: DisconnectPushNotificationServer: Clear _serviceStarted.");
                        _serviceStarted = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotification: DisconnectPushNotificationServer: ERROR: Exception.  Msg: <{0}>.", ex.Message);
            }

            if (_syncBox != null)
            {
                string syncBoxDeviceIdCombined = _syncBox.SyncBoxId.ToString() + " " + (_syncBox.CopiedSettings.DeviceId ?? string.Empty);

                NotificationClientsRunning.Remove(syncBoxDeviceIdCombined);
            }

            lock (this)
            {
                _isInitialized = false;
            }
        }
    }
}