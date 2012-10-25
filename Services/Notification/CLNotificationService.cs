//  CLNotificationService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Windows;
using System.Linq;
using CloudApiPublic.Support;
using CloudApiPublic.PushNotification;
using FileMonitor.SyncImplementation;
using win_client.Common;
using CloudApiPublic.Model;
using JsonContracts = CloudApiPublic.JsonContracts;

namespace win_client.Services.Notification
{
    public sealed class CLNotificationService
    {
        private static CLNotificationService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private static CLNotification _notificationService = null;

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


        /// <summary>
        /// Initialize the push notification service
        /// </summary>
        public void ConnectPushNotificationServer()
        {
            try
            {
                lock (_instanceLocker)
                {
                    _trace.writeToLog(9, "CLNotificationService: ConnectPushNotificationServer: Entry.");
                    if (!_serviceStarted)
                    {
                        _notificationService = CLNotification.GetInstance(SyncSettings.Instance);
                    }

                    // Hook up the events
                    _notificationService.NotificationReceived += OnNotificationReceived;
                    _notificationService.NotificationPerformManualSyncFrom += OnNotificationPerformManualSyncFrom;
                    _notificationService.ConnectionError += OnConnectionError;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: ConnectPushNotificationServer: ERROR. Exception: Msg: <{0}>.", ex.Message);
                throw;
            }

            _serviceStarted = true;
        }

        /// <summary>
        /// Event that is fired asynchronously to inform us of a serious error.  Push Notification is no longer working.
        /// </summary>
        void OnConnectionError(object sender, NotificationErrorEventArgs e)
        {
            try
            {
                _trace.writeToLog(1, "CLNotificationService: OnConnectionError: ERROR: WebSocketsError: {0}. PerformSyncFromError: {1}.", 
                    e.ErrorWebSockets == null ? "None" : e.ErrorWebSockets.errorDescription,
                    e.ErrorManualPoll == null ? "None" : e.ErrorManualPoll.errorDescription);

                // Serious error, unable to reconnect to push notification AND unable to manually poll
                global::System.Windows.MessageBox.Show("Cloud has stopped receiving sync events from other devices with errors:" + Environment.NewLine +
                    e.ErrorManualPoll == null ? "None" : e.ErrorManualPoll.errorDescription + Environment.NewLine +
                    "AND" + Environment.NewLine + 
                    e.ErrorWebSockets == null ? "None" : e.ErrorWebSockets.errorDescription);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: OnConnectionError: ERROR: Exception. Msg: <{0}>.", ex.Message); 
            }
        }

        /// <summary>
        /// Event that is fired when we should send a Sync_From request to the server.
        /// </summary>
        void OnNotificationPerformManualSyncFrom(object sender, NotificationEventArgs e)
        {
            _trace.writeToLog(9, "CLNotificationService: OnNotificationPerformManualSyncFrom: Send manual poll message.");
            CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(new JsonContracts.NotificationResponse()
            {
                Body = CLDefinitions.CLNotificationTypeNew
            });
        }

        /// <summary>
        /// Event that is fired when a push notification is received from the server.
        /// </summary>
        void OnNotificationReceived(object sender, NotificationEventArgs e)
        {
            _trace.writeToLog(9, "CLNotificationService: OnNotificationReceived: Entry.  Received server push message.  Send it on.");
            CLAppMessages.Message_DidReceivePushNotificationFromServer.Send(e.Message);
        }

        /// <summary>
        /// Terminate the push notification service
        /// </summary>
        public void DisconnectPushNotificationServer()
        {
            try
            {
                _trace.writeToLog(9, "CLNotificationService: DisconnectPushNotificationServer: Entry.");
                lock (_instanceLocker)
                {
                    if (_serviceStarted)
                    {
                        if (_notificationService != null)
                        {
                            _notificationService.DisconnectPushNotificationServer();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationService: DisconnectPushNotificationServer: ERROR: Exception.  Msg: <{0}>.", ex.Message);
            }
            finally
            {
                _serviceStarted = false;
                _notificationService = null;
            }
        }
    }
}
