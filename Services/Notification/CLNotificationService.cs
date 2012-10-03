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

            bool fallbackToManualPolling = false;

            if (faultCount >= CLDefinitions.PushNotificationFaultLimitBeforeFallback)
            {
                fallbackToManualPolling = true;
            }
            else
            {
                // WebSocket4Net implementation.
                try
                {
                    string url = String.Format("{0}?channel=/channel_{1}&sender={2}", CLDefinitions.CLNotificationServerURL, Settings.Instance.Uuid, Settings.Instance.Udid);
                    _trace.writeToLog(1, "CLNotificationService: ConnectPushNotificationServer: Establish connection with push server. url: <{0}>.", url);

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
                ThreadPool.UnsafeQueueUserWorkItem(FallbackToManualPolling, this);
            }
        }

        private static void FallbackToManualPolling(object state)
        {
            CLNotificationService castState = state as CLNotificationService;

            if (castState != null)
            {
                castState.faultCount = 0;
            }

            bool servicesStartedSet = false;
            for (int manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1; manualPollingIteration >= 0; manualPollingIteration--)
            {
                if (!servicesStartedSet)
                {
                    if (castState != null)
                    {
                        lock (castState)
                        {
                            castState._serviceStarted = true;
                        }
                    }
                    servicesStartedSet = true;
                }
                else if (castState != null
                    && !castState._serviceStarted)
                {
                    return;
                }

                CLError storeManualPollingError = null;
                try
                {
                    PerformManualPoll();
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
                        castState.ConnectPushNotificationServer();
                    }
                    catch (Exception innerEx)
                    {
                        if (castState != null)
                        {
                            lock (castState)
                            {
                                castState._serviceStarted = false;
                            }
                        }

                        bool forceErrors = false;

                        CLError error = innerEx;
                        _trace.writeToLog(1, "CLNotificationService: FallbackToManualPolling: ERROR: Exception occurred during manual polling. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                        if (storeManualPollingError != null)
                        {
                            // Force logging errors in the serious case where a message had to be displayed
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
                            manualPollingIteration = CLDefinitions.ManualPollingIterationsBeforeConnectingPush - 1;
                        }

                        error.LogErrors(Settings.Instance.ErrorLogLocation, forceErrors || Settings.Instance.LogErrors);
                    }
                }
            }
        }

        private static void PerformManualPoll()
        {
            CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(StaticSync.NotificationResponseToJSON(new JsonNotificationResponse()
                {
                    Body = CLDefinitions.CLNotificationTypeNew
                }));
        }

        private void OnConnectionOpened(object sender, EventArgs e)
        {
            _trace.writeToLog(1, "CLNotificationService: OnConnectionError: Connection opened.");
            faultCount = 0;
        }

        private void OnConnectionError(object sender, ErrorEventArgs e)
        {
            bool forceErrors = false;
            try
            {
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
            _trace.writeToLog(1, "CLNotificationService: OnConnectionClosed: Entry.");

            try
            {
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

                if (urlReceiver != null)
                {
                    try
                    {
                        sender.MessageReceived -= urlReceiver.OnConnectionReceived;
                        urlReceiver = null;
                    }
                    catch
                    {
                    }
                }

                try
                {
                    sender.Closed -= OnConnectionClosed;
                }
                catch
                {
                }

                try
                {
                    sender.Error -= OnConnectionError;
                }
                catch
                {
                }

                try
                {
                    sender.Opened -= OnConnectionOpened;
                }
                catch
                {
                }

                try
                {
                    sender.Close();
                }
                catch
                {
                }

                try
                {
                    pushConnected = false;
                    _serviceStarted = false;
                    if (_connection != null
                        && _connection == sender)
                    {
                        _connection = null;
                    }
                }
                catch
                {
                }

                if (!doNotRestart)
                {
                    try
                    {
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
                        || parsedResponse.Author != Helpers.GetComputerFriendlyName())
                    {
                        CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(e.Message);
                    }
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, String.Format("CLNotificationService: OnConnectionReceived: ERROR: Exception.  Msg: <{0}>.", ex.Message));
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
                if (_serviceStarted)
                {
                    if (pushConnected)
                    {
                        if (_connection != null)
                        {
                            CleanWebSocketAndRestart(_connection, true);
                            _trace.writeToLog(1, "CLNotificationService: DisconnectPushNotificationServer: Cleaned WebSocket.");
                        }
                    }
                    else
                    {
                        _serviceStarted = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLNotificationService: DisconnectPushNotificationServer: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }
    }
}