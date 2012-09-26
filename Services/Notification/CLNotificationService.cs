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
        private WebSocket _connection;

        // True: the push notification service has been started.
        private bool _serviceStarted;
        public bool ServiceStarted
        {
            get { return _serviceStarted; }
            set { _serviceStarted = value; }
        }


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

                _connection = new WebSocket(url, null, WebSocketVersion.Rfc6455);
                _connection.Opened += OnConnectionOpened;
                _connection.Error += OnConnectionError;
                _connection.Closed += OnConnectionClosed;
                _connection.MessageReceived += (new MessageReceiver(url)).OnConnectionReceived;
                _connection.Open();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "CLNotificationService: ConnectPushNotificationServer: ERROR: Exception connecting with the push server. Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        private void OnConnectionOpened(object sender, EventArgs e)
        {
            _trace.writeToLog(1, "CLNotificationService: OnConnectionError: Connection opened.");
        }

        private void OnConnectionError(object sender, ErrorEventArgs e)
        {
            CLError error = e.Exception;
            error.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
            _trace.writeToLog(1, "CLNotificationService: OnConnectionError: ERROR.  Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            _trace.writeToLog(1, "CLNotificationService: OnConnectionClosed: Entry.");
            ServiceStarted = false;
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
                _trace.writeToLog(1, "CLNotificationService: OnConnectionReceived: Received msg: <{0}.", e.Message);

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

                JsonNotificationResponse parsedResponse = StaticSync.ParseNotificationResponse(e.Message);
                if (parsedResponse == null
                    || parsedResponse.Body != CLDefinitions.CLNotificationTypeNew
                    || parsedResponse.Author != Helpers.GetComputerFriendlyName())
                {
                    CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(e.Message);
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
            _trace.writeToLog(1, "CLNotificationService: DisconnectPushNotificationServer: Entry.");
            if (ServiceStarted)
            {
                _connection.Close();
                _trace.writeToLog(1, "CLNotificationService: DisconnectPushNotificationServer: Entry.");
            }
        }
    }
}

