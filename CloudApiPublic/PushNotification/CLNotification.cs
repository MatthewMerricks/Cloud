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

namespace CloudApiPublic.PushNotification
{
    public class NotificationEventArgs : EventArgs
    {
        public NotificationResponse Message;
    }

    public class NotificationErrorEventArgs : EventArgs
    {
        public CLError ErrorWebSockets;
        public CLError ErrorManualPoll;
    }
    
    public sealed class CLNotification
    {
        /// <summary>
        /// Event fired when a push notification message is received from the server.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationReceived;

        /// <summary>
        /// Event fired when manual polling is being used.  The application should send
        /// a Sync_From request to the server.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationPerformManualSyncFrom;

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
        private readonly ISyncSettingsAdvanced _syncSettings;
        private bool _serviceStarted;               // True: the push notification service has been started.
        private bool _pushConnected = false;
        private int _faultCount = 0;

        /// <summary>
        /// Tracks the subscribed clients via their Settings AKey.
        /// </summary>
        private static readonly Dictionary<string, CLNotification> NotificationClientsRunning = new Dictionary<string, CLNotification>();


        private const int MillisecondManualPollingInterval = 20000;

        /// <summary>
        /// Access Instance to get the push notification server object for this client.
        /// Then call methods on that instance.
        /// </summary>
        /// <param name="syncSettings">The settings that identify the calling client.  Specifically the AKey differentiates clients.</param>
        public static CLNotification GetInstance(ISyncSettings syncSettings)
        {
            if (syncSettings == null)
            {
                throw new NullReferenceException("syncSettings cannot be null");
            }

            if (string.IsNullOrWhiteSpace(syncSettings.Akey))
            {
                throw new NullReferenceException("syncSettings Akey cannot be null");
            }

            lock (NotificationClientsRunning)
            {
                string storeAKey = syncSettings.Akey;
                CLNotification toReturn;
                if (!NotificationClientsRunning.TryGetValue(storeAKey, out toReturn))
                {
                    NotificationClientsRunning.Add(storeAKey, toReturn = new CLNotification(syncSettings));
                }
                return toReturn;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLNotification(ISyncSettings syncSettings)
        {
            if (syncSettings == null)
            {
                throw new NullReferenceException("syncSettings cannot be null");
            }

            // sync settings are copied so that changes require stopping and starting notification services
            this._syncSettings = Sync.SyncSettingsExtensions.CopySettings(syncSettings);

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(_syncSettings.TraceLocation, "Cloud", "log", _syncSettings.TraceLevel, _syncSettings.LogErrors);
            CLTrace.Instance.writeToLog(9, "CLNotification: CLNotification: Entry");

            // Initialize members, etc. here (at static initialization time).
            ConnectPushNotificationServer();
        }

        /// <summary>
        /// Call to initialize and make a connection to the push notification server.
        /// </summary>
        /// <param name="syncSettings">Settings that contain the connection parameters.</param>
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
                    string url = String.Format("{0}?channel=/channel_{1}&sender={2}", CLDefinitions.CLNotificationServerURL, _syncSettings.Uuid, _syncSettings.Udid);
                    _trace.writeToLog(9, "CLNotification: ConnectPushNotificationServer: Establish connection with push server. url: <{0}>.", url);

                    //¡¡ Remember to exclude authentication from trace once web socket authentication is implemented based on _syncSettings.TraceExcludeAuthorization !!
                    if ((_syncSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                    {
                        Trace.LogCommunication(_syncSettings.TraceLocation,
                            _syncSettings.Udid,
                            _syncSettings.Uuid,
                            CommunicationEntryDirection.Request,
                            url,
                            true,
                            null,
                            (string)null,
                            null,
                            _syncSettings.TraceExcludeAuthorization);
                    }

                    string webSocketOpenStatus = "Entered action to open WebSocket";
                    lock (this)
                    {
                        _connection = new WebSocket(url, null, WebSocketVersion.Rfc6455);
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
                                    urlReceiver = new MessageReceiver(url, _syncSettings, (sender, e) =>
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
                                        _connection.Open();
                                        _pushConnected = true;
                                        _serviceStarted = true;
                                    }
                                    catch
                                    {
                                        _connection.MessageReceived -= urlReceiver.OnConnectionReceived;
                                        throw;
                                    }
                                }
                                catch
                                {
                                    _connection.Closed -= OnConnectionClosed;
                                    throw;
                                }
                            }
                            catch
                            {
                                _connection.Error -= OnConnectionError;
                                throw;
                            }
                        }
                        catch (Exception ex)
                        {
                            _connection.Opened -= OnConnectionOpened;
                            try
                            {
                                _connection.Close();
                            }
                            catch
                            {
                            }
                            _connection = null;
                            throw new AggregateException("Error creating and opening WebSocket with last successful state: " + webSocketOpenStatus, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    _trace.writeToLog(1, "CLNotification: ConnectPushNotificationServer: ERROR: Exception connecting with the push server. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                    error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);

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
                _isInitialized = true;
            }
        }

        private void FallbackToManualPolling(object state)
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
                if (!servicesStartedSet)
                {
                    _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: !servicesStartedSet.");
                    if (castState != null)
                    {
                        lock (castState)
                        {
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Set _serviceStarted.");
                            castState._serviceStarted = true;
                        }
                    }
                    servicesStartedSet = true;
                }
                else if (castState != null
                    && !castState._serviceStarted)
                {
                    _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Return.");
                    return;
                }

                CLError storeManualPollingError = null;
                try
                {
                    _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Call PerformManualSyncFrom.");
                    PerformManualSyncFrom();

                    manualPollSuccessful = true;
                }
                catch (Exception ex)
                {
                    storeManualPollingError = ex;
                    _trace.writeToLog(1, "CLNotification: FallbackToManualPolling: ERROR: Exception occurred trying to reconnect to push after manually polling. Msg: <{0}>, Code: {1}.", storeManualPollingError.errorDescription, storeManualPollingError.errorCode);
                    storeManualPollingError.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
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
                        _trace.writeToLog(1, "CLNotification: FallbackToManualPolling: ERROR: Exception occurred during manual polling. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                        if (!manualPollSuccessful
                            && storeManualPollingError != null)
                        {
                            // Force logging errors in the serious case where a message had to be displayed
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Put up an ugly MessageBox to the user.");
                            forceErrors = true;
                            if (!_syncSettings.LogErrors)
                            {
                                storeManualPollingError.LogErrors(_syncSettings.TraceLocation, true);
                            }

                            // Serious error, unable to reconnect to push notification AND unable to manually poll
                            if (ConnectionError != null)
                            {
                                NotificationErrorEventArgs err = new NotificationErrorEventArgs();
                                err.ErrorWebSockets = innerEx;
                                err.ErrorManualPoll = storeManualPollingError;
                                ConnectionError(this, err);
                            }
                        }
                        else
                        {
                            manualPollSuccessful = false;
                            manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1;
                            _trace.writeToLog(9, "CLNotification: FallbackToManualPolling: Decremented manualPollingIteration count: {0}.", manualPollingIteration);
                            Thread.Sleep(MillisecondManualPollingInterval);
                        }

                        error.LogErrors(_syncSettings.TraceLocation, forceErrors || _syncSettings.LogErrors);
                    }
                }
                else
                {
                    Thread.Sleep(MillisecondManualPollingInterval);
                }
            }
        }

        private void PerformManualSyncFrom()
        {
            if (NotificationPerformManualSyncFrom != null)
            {
                _trace.writeToLog(9, "CLNotification: PerformManualSyncFrom: Fire event to request the application to send a Sync_From request.");
                NotificationEventArgs args = new NotificationEventArgs();
                args.Message = new NotificationResponse()
                {
                    Body = CLDefinitions.CLNotificationTypeNew
                };
                NotificationPerformManualSyncFrom(this, args);
            }
        }

        private void OnConnectionOpened(object sender, EventArgs e)
        {
            _trace.writeToLog(9, "CLNotification: OnConnectionOpened: Connection opened.  Set faultCount to zero.");
            _faultCount = 0;
        }

        private void OnConnectionError(object sender, ErrorEventArgs e)
        {
            bool forceErrors = false;
            try
            {
                _trace.writeToLog(1, "CLNotification: OnConnectionError: Connection error.  Set faultCount to zero.");
                CleanWebSocketAndRestart((WebSocket)sender);
            }
            catch (Exception ex)
            {
                // Override error logging because we had a serious case where we had to display a message
                forceErrors = true;

                CLError innerError = ex;
                innerError.LogErrors(_syncSettings.TraceLocation, true);
                _trace.writeToLog(1, "CLNotification: OnConnectionError: ERROR. Error while restarting WebSocket.  Msg: <{0}>, Code: {1}.", innerError.errorDescription, innerError.errorCode);

                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with errors:" + Environment.NewLine +
                    e.Exception.Message + Environment.NewLine +
                    "AND" + Environment.NewLine + ex.Message);
            }
            CLError error = e.Exception;
            error.LogErrors(_syncSettings.TraceLocation, forceErrors || _syncSettings.LogErrors);
            _trace.writeToLog(1, "CLNotification: OnConnectionError: ERROR.  Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
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
                error.LogErrors(_syncSettings.TraceLocation, true);
                _trace.writeToLog(1, "CLNotification: OnConnectionClosed: ERROR. Error while restarting WebSocket.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);

                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with error:" + Environment.NewLine +
                    ex.Message);
            }
        }

        private void CleanWebSocketAndRestart(WebSocket sender, bool doNotRestart = false)
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
                        error.LogErrors(_syncSettings.TraceLocation, _syncSettings.LogErrors);
                        _trace.writeToLog(1, "CLNotification: CleanWebSocketAndRestart: ERROR. Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);

                        ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
                    }
                }
            }
        }

        private class MessageReceiver
        {
            private string _url;
            private ISyncSettingsAdvanced _innerSyncSettings;
            private EventHandler<NotificationEventArgs> _notificationReceived;

            public MessageReceiver(string url, ISyncSettingsAdvanced syncSettings, EventHandler<NotificationEventArgs> notificationReceived)
            {
                this._url = url;
                _innerSyncSettings = syncSettings;
                _notificationReceived = notificationReceived;
            }

            public void OnConnectionReceived(object sender, MessageReceivedEventArgs e)
            {
                _trace.writeToLog(1, "CLNotification: OnConnectionReceived: Received msg: <{0}>.", e.Message);

                if ((_innerSyncSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    Trace.LogCommunication(_innerSyncSettings.TraceLocation,
                        _innerSyncSettings.Udid,
                        _innerSyncSettings.Uuid,
                        CommunicationEntryDirection.Response,
                        this._url,
                        true,
                        null,
                        e.Message,
                        null, //<-- actually this is the valid response, but push doesn't exactly give a 200 that I can detect
                        _innerSyncSettings.TraceExcludeAuthorization);
                }

                try
                {
                    NotificationResponse parsedResponse = JsonContractHelpers.ParseNotificationResponse(e.Message);
                    if (parsedResponse == null
                        || parsedResponse.Body != CLDefinitions.CLNotificationTypeNew
                        || parsedResponse.Author.ToUpper() != _innerSyncSettings.Udid.ToUpper())
                    {
                        _trace.writeToLog(9, "CLNotification: OnConnectionReceived: Send DidReceivePushNotificationFromServer.");
                        if (_notificationReceived != null)
                        {
                            _trace.writeToLog(9, "CLNotification: PerformManualSyncFrom: Fire event to notify the application  Msg: {0}.", e.Message);
                            NotificationEventArgs args = new NotificationEventArgs();
                            args.Message = parsedResponse;
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
                            CleanWebSocketAndRestart(_connection, true);
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

            if (_syncSettings != null && !String.IsNullOrWhiteSpace(_syncSettings.Akey))
            {
                NotificationClientsRunning.Remove(_syncSettings.Akey);
            }
            _isInitialized = false;
        }
    }
}