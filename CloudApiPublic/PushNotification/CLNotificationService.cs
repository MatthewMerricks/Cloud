//  CLNotificationService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

#if TRASH2
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
            // check input parameters

            if (syncBox == null)
            {
                throw new NullReferenceException("syncBox cannot be null");
            }
            if (string.IsNullOrEmpty(syncBox.CopiedSettings.DeviceId))
            {
                throw new NullReferenceException("syncBox CopiedSettings DeviceId cannot be null");
            }

            // sync settings are copied so that changes require stopping and starting notification services
            this._syncBox = syncBox;

            // Initialize trace in case it is not already initialized.
            CLTrace.Initialize(syncBox.CopiedSettings.TraceLocation, "Cloud", "log", syncBox.CopiedSettings.TraceLevel, syncBox.CopiedSettings.LogErrors);
            CLTrace.Instance.writeToLog(9, "CLNotificationService: CLNotificationService: Entry");

            // Initialize members, etc. here (at static initialization time).
            //&&&&&ConnectPushNotificationServer();  //TODO: DEBUG ONLY.  REMOVE.
            //&&&&&ConnectPushNotificationServerSse();
        }

        public void DisconnectPushNotificationServer()
        {
        }

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
#endif // TRASH2