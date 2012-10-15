////  CLNotificationService.cs
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
using CloudApiPublic.Support;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Media;
using win_client.ViewModels;
using win_client.Common;
using win_client.Services.Sync;
using Microsoft.Practices.ServiceLocation;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Threading;
using System.Windows.Forms;
using WebSocket4Net;
using WebSocket4Net.Command;
using WebSocket4Net.Protocol;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using SuperSocket.ClientEngine;
using CloudApiPublic.Static;
using JsonNotificationResponse = global::Sync.JsonContracts.NotificationResponse;
using StaticSync = global::Sync.Sync;

namespace win_client.Services.Notification
{
    public sealed class CLNotificationService
    {
        private static CLNotificationService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private WebSocket _connection = null;
        private MessageReceiver urlReceiver = null;

        private const int MillisecondManualPollingInterval = 10000;

        // True: the push notification service has been started.
        private bool _serviceStarted;
        public bool ServiceStarted
        {
            get { return _serviceStarted; }
            set { _serviceStarted = value; }
        }

        private bool pushConnected = false;
        private int faultCount = 0;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLNotificationService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLNotificationService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLNotificationService()
        {
            // Initialize members, etc. here (at static initialization time).
        }

        //- (void)connectPushNotificationServer
        public void ConnectPushNotificationServer()
        {
            // Merged 7/16/12
            // // avoid attempt to multiple websocket connections
            // if (self.webSocket != nil) {
            //     if ([self pushNotificationServerConnected]) {
            //         [self.webSocket setDelegate:nil];
            //         [self.webSocket close];
            //     }
            //     self.webSocket = nil;
            // }

            // NSLog(@"%s - Attempting to connect to Push Notification Services.", __FUNCTION__);
            // NSString *subscriptionURL = [NSString stringWithFormat:@"%@?channel=/channel_%@&sender=%@", CLNotificationServerURL, [[CLSettings sharedSettings] uuid], [[CLSettings sharedSettings] udid]];
            // NSLog(@"Notification URL: %@", subscriptionURL);

            // self.webSocket = [[SRWebSocket alloc] initWithURLRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:subscriptionURL]]];
            // [self.webSocket setDelegate:self];
            // [self.webSocket open];
            // self.serviceStarted = YES;
            //&&&&

            // // avoid attempt to multiple websocket connections
            // if (self.webSocket != nil) {
            //     if ([self pushNotificationServerConnected]) {
            //         [self.webSocket setDelegate:nil];
            //         [self.webSocket close];
            //     }
            //     self.webSocket = nil;
            // }

            // NSLog(@"%s - Attempting to connect to Push Notification Services.", __FUNCTION__);
            // NSString *subscriptionURL = [NSString stringWithFormat:@"%@?channel=/channel_%@&sender=%@", CLNotificationServerURL, [[CLSettings sharedSettings] uuid], [[CLSettings sharedSettings] udid]];
            // NSLog(@"Notification URL: %@", subscriptionURL);

            // self.webSocket = [[SRWebSocket alloc] initWithURLRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:subscriptionURL]]];
            // [self.webSocket setDelegate:self];
            // [self.webSocket open];
            // self.serviceStarted = YES;
            //&&&&

            _trace.writeToLog(9, "CLNotificationService: ConnectPushNotificationServer: Entry.");
            bool fallbackToManualPolling = false;

            if (faultCount >= CLDefinitions.PushNotificationFaultLimitBeforeFallback)
            {
                _trace.writeToLog(9, "CLNotificationService: ConnectPushNotificationServer: Set fallbackToManualPolling.");
                fallbackToManualPolling = true;
            }
            else
            {
                // WebSocket4Net implementation.
                try
                {
                    string url = String.Format("{0}?channel=/channel_{1}&sender={2}", CLDefinitions.CLNotificationServerURL, Settings.Instance.Uuid, Settings.Instance.Udid);
                    _trace.writeToLog(9, "CLNotificationService: ConnectPushNotificationServer: Establish connection with push server. url: <{0}>.", url);

                    //¡¡ Remember to exclude authentication from trace once web socket authentication is implemented based on Settings.Instance.TraceExcludeAuthorization !!
                    if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                    {
                        Trace.LogCommunication(Settings.Instance.TraceLocation,
                            Settings.Instance.Udid,
                            Settings.Instance.Uuid,
                            CommunicationEntryDirection.Request,
                            url,
                            true,
                            null,
                            (string)null,
                            null,
                            Settings.Instance.TraceExcludeAuthorization);
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
                                    urlReceiver = new MessageReceiver(url);
                                    webSocketOpenStatus = "Instantiated new MessageReceiver";
                                    _connection.MessageReceived += urlReceiver.OnConnectionReceived;
                                    webSocketOpenStatus = "Attached connection received handler";
                                    try
                                    {
                                            _connection.Open();
                                            pushConnected = true;
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
                    _trace.writeToLog(1, "CLNotificationService: ConnectPushNotificationServer: ERROR: Exception connecting with the push server. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                    error.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);

                    fallbackToManualPolling = true;
                }
            }

            if (fallbackToManualPolling)
            {
                _trace.writeToLog(9, "CLNotificationService: ConnectPushNotificationServer: Queue FallbackToManualPolling.");
                ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
            }
        }

        private static void FallbackToManualPolling(object state)
        {
            _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Entry.");
            CLNotificationService castState = state as CLNotificationService;

            if (castState != null)
            {
                _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Set faultCount to zero.");
                castState.faultCount = 0;
            }

            bool manualPollSuccessful = false;

            bool servicesStartedSet = false;
            for (int manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1; manualPollingIteration >= 0; manualPollingIteration--)
            {
                _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Top of manualPollingIteration loop.");
                if (!servicesStartedSet)
                {
                    _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: !servicesStartedSet.");
                    if (castState != null)
                    {
                        lock (castState)
                        {
                            _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Set _serviceStarted.");
                            castState._serviceStarted = true;
                        }
                    }
                    servicesStartedSet = true;
                }
                else if (castState != null
                    && !castState._serviceStarted)
                {
                    _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Return.");
                    return;
                }

                CLError storeManualPollingError = null;
                try
                {
                    _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Call PerformManualPoll.");
                    PerformManualPoll();

                    manualPollSuccessful = true;
                }
                catch (Exception ex)
                {
                    storeManualPollingError = ex;
                    _trace.writeToLog(1, "CLNotificationService: FallbackToManualPolling: ERROR: Exception occurred trying to reconnect to push after manually polling. Msg: <{0}>, Code: {1}.", storeManualPollingError.errorDescription, storeManualPollingError.errorCode);
                    storeManualPollingError.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                }

                if (manualPollingIteration == 0)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: manualPollingIteration is zero.");
                        castState.ConnectPushNotificationServer();
                    }
                    catch (Exception innerEx)
                    {
                        _trace.writeToLog(1, "CLNotificationService: FallbackToManualPolling: ERROR: Exception.  Msg: <{0}>.", innerEx.Message);
                        if (castState != null)
                        {
                            lock (castState)
                            {
                                _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Reset _serviceStarted.");
                                castState._serviceStarted = false;
                            }
                        }

                        bool forceErrors = false;

                        CLError error = innerEx;
                        _trace.writeToLog(1, "CLNotificationService: FallbackToManualPolling: ERROR: Exception occurred during manual polling. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                        if (!manualPollSuccessful
                            && storeManualPollingError != null)
                        {
                            // Force logging errors in the serious case where a message had to be displayed
                            _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Put up an ugly MessageBox to the user.");
                            forceErrors = true;
                            if (!Settings.Instance.LogErrors)
                            {
                                storeManualPollingError.LogErrors(Settings.Instance.ErrorLogLocation, true);
                            }

                            // Serious error, unable to reconnect to push notification AND unable to manually poll
                            global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with errors:" + Environment.NewLine +
                                storeManualPollingError.errorDescription + Environment.NewLine +
                                "AND" + Environment.NewLine + innerEx.Message);
                        }
                        else
                        {
                            manualPollSuccessful = false;
                            manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1;
                            _trace.writeToLog(9, "CLNotificationService: FallbackToManualPolling: Decremented manualPollingIteration count: {0}.", manualPollingIteration);
                            Thread.Sleep(MillisecondManualPollingInterval);
                        }

                        error.LogErrors(Settings.Instance.ErrorLogLocation, forceErrors || Settings.Instance.LogErrors);
                    }
                }
                else
                {
                    Thread.Sleep(MillisecondManualPollingInterval);
                }
            }
        }

        private static void PerformManualPoll()
        {
            _trace.writeToLog(9, "CLNotificationService: PerformManualPoll: Send manual poll message.");
            CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(StaticSync.NotificationResponseToJSON(new JsonNotificationResponse()
                {
                    Body = CLDefinitions.CLNotificationTypeNew
                }));
        }

        private void OnConnectionOpened(object sender, EventArgs e)
        {
            _trace.writeToLog(9, "CLNotificationService: OnConnectionOpened: Connection opened.  Set faultCount to zero.");
            faultCount = 0;
        }

        private void OnConnectionError(object sender, ErrorEventArgs e)
        {
            bool forceErrors = false;
            try
            {
                _trace.writeToLog(1, "CLNotificationService: OnConnectionError: Connection error.  Set faultCount to zero.");
                CleanWebSocketAndRestart((WebSocket)sender);
            }
            catch (Exception ex)
            {
                // Override error logging because we had a serious case where we had to display a message
                forceErrors = true;

                CLError innerError = ex;
                innerError.LogErrors(Settings.Instance.ErrorLogLocation, true);
                _trace.writeToLog(1, "CLNotificationService: OnConnectionError: ERROR. Error while restarting WebSocket.  Msg: <{0}>, Code: {1}.", innerError.errorDescription, innerError.errorCode);

                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with errors:" + Environment.NewLine +
                    e.Exception.Message + Environment.NewLine +
                    "AND" + Environment.NewLine + ex.Message);
            }
            CLError error = e.Exception;
            error.LogErrors(Settings.Instance.ErrorLogLocation, forceErrors || Settings.Instance.LogErrors);
            _trace.writeToLog(1, "CLNotificationService: OnConnectionError: ERROR.  Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            try
            {
                _trace.writeToLog(9, "CLNotificationService: OnConnectionClosed: Entry. Call CleanWebSocketAndRestart.");
                CleanWebSocketAndRestart((WebSocket)sender);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                // Always log errors here because we had a serious case where we had to display a message
                error.LogErrors(Settings.Instance.ErrorLogLocation, true);
                _trace.writeToLog(1, "CLNotificationService: OnConnectionClosed: ERROR. Error while restarting WebSocket.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);

                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with error:" + Environment.NewLine +
                    ex.Message);
            }
        }

        private void CleanWebSocketAndRestart(WebSocket sender, bool doNotRestart = false)
        {
            lock (this)
            {
                faultCount++;
                _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Entry. doNotRestart: {0}. faultCount: {1}.", doNotRestart, faultCount);
                if (faultCount >= CLDefinitions.PushNotificationFaultLimitBeforeFallback)
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: Set doNotRestart.");
                    doNotRestart = true;
                }

                if (urlReceiver != null)
                {
                    try
                    {
                        _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Remove OnConnectionReceived.");
                        sender.MessageReceived -= urlReceiver.OnConnectionReceived;
                        urlReceiver = null;
                    }
                    catch
                    {
                        _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR: Exception.");
                    }
                }

                try
                {
                    _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Removed OnConnectionClosed.");
                    sender.Closed -= OnConnectionClosed;
                }
                catch
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR: Exception(2).");
                }

                try
                {
                    _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Remove OnConnectionError.");
                    sender.Error -= OnConnectionError;
                }
                catch
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR: Exception(3).");
                }

                try
                {
                    _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Remove OnConnectionOpened.");
                    sender.Opened -= OnConnectionOpened;
                }
                catch
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR: Exception(4).");
                }

                try
                {
                    _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Close Sender.");
                    sender.Close();
                }
                catch
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR: Exception(5).");
                }

                try
                {
                    _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Set pushConnected and _serviceStarted false.");
                    pushConnected = false;
                    _serviceStarted = false;
                    if (_connection != null
                        && _connection == sender)
                    {
                        _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Set _connection null.");
                        _connection = null;
                    }
                }
                catch
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR: Exception(6).");
                }

                if (doNotRestart)
                {
                    _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: doNotRestart (FallbackToManualPolling)");

                    ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
                }
                else
                {
                    try
                    {
                        _trace.writeToLog(9, "CLNotificationService: CleanWebSocketAndRestart: Attempt restart.  Call ConnectPushNotificationServer.");
                        ConnectPushNotificationServer();
                    }
                    catch (Exception ex)
                    {
                        CLError error = ex;
                        error.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                        _trace.writeToLog(1, "CLNotificationService: CleanWebSocketAndRestart: ERROR. Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                    
                        ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
                    }
                }
            }
        }

        private class MessageReceiver
        {
            private string url;

            public MessageReceiver(string url)
            {
                this.url = url;
            }

            public void OnConnectionReceived(object sender, MessageReceivedEventArgs e)
            {
                _trace.writeToLog(1, "CLNotificationService: OnConnectionReceived: Received msg: <{0}>.", e.Message);

                if ((Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    Trace.LogCommunication(Settings.Instance.TraceLocation,
                        Settings.Instance.Udid,
                        Settings.Instance.Uuid,
                        CommunicationEntryDirection.Response,
                        this.url,
                        true,
                        null,
                        e.Message,
                        null, //<-- actually this is the valid response, but push doesn't exactly give a 200 that I can detect
                        Settings.Instance.TraceExcludeAuthorization);
                }

                try
                {
                    JsonNotificationResponse parsedResponse = StaticSync.ParseNotificationResponse(e.Message);
                    if (parsedResponse == null
                        || parsedResponse.Body != CLDefinitions.CLNotificationTypeNew
                        || parsedResponse.Author.ToUpper() != Settings.Instance.Udid.ToUpper())
                    {
                        _trace.writeToLog(9, "CLNotificationService: OnConnectionReceived: Send DidReceivePushNotificationFromServer.");
                        CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(e.Message);
                    }
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, "CLNotificationService: OnConnectionReceived: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                }
            }
        }

        //- (void)disconnectPushNotificationServer
        public void DisconnectPushNotificationServer()
        {
            // Merged 7/16/12
            // self.serviceStarted = NO;
            // [self.webSocket close];
            // [self stopPoolingServices];

            // NSLog(@"%s - Connection to Push Notification Services Ended.", __FUNCTION__);
            //&&&&

            // self.serviceStarted = NO;
            // [self.webSocket close];
            // [self stopPoolingServices];

            // NSLog(@"%s - Connection to Push Notification Services Ended.", __FUNCTION__);
            try
            {
                _trace.writeToLog(9, "CLNotificationService: DisconnectPushNotificationServer: Entry.");
                if (_serviceStarted)
                {
                    _trace.writeToLog(9, "CLNotificationService: DisconnectPushNotificationServer: Service started.");
                    if (pushConnected)
                    {
                        _trace.writeToLog(9, "CLNotificationService: DisconnectPushNotificationServer: Push connected.");
                        if (_connection != null)
                        {
                            _trace.writeToLog(9, "CLNotificationService: DisconnectPushNotificationServer: Call CleanWebSocketAndRestart.");
                            CleanWebSocketAndRestart(_connection, true);
                            _trace.writeToLog(1, "CLNotificationService: DisconnectPushNotificationServer: After call to CleanWebSocketAndRestart.");
                        }
                    }
                    else
                    {
                        _trace.writeToLog(9, "CLNotificationService: DisconnectPushNotificationServer: Clear _serviceStarted.");
                        _serviceStarted = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: DisconnectPushNotificationServer: ERROR: Exception.  Msg: <{0}>.", ex.Message);
            }
        }
    }
}