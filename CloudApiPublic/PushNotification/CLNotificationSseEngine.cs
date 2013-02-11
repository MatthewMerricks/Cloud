//  CLNotificationSseEngine.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

//#if TRASH

namespace CloudApiPublic.PushNotification
{
    internal sealed class CLNotificationSseEngine : ICLNotificationEngine
    {
        #region Private fields

        private static CLTrace _trace = CLTrace.Instance;
        private CLSyncBox _syncBox = null;
        private ICLSyncSettingsAdvanced _copiedSettings = null;
        private StartEngineTimeout _delegateStartEngineTimeout = null;
        private CancelEngineTimeout _delegateCancelEngineTimeout = null;
        private bool _isStarted = false;
        private bool _isConnectionSuccesful = false;
        private readonly object _locker = new object();
        private readonly GenericHolder<Thread> _engineThread = new GenericHolder<Thread>(null);
        private static readonly ManualResetEvent _startComplete = new ManualResetEvent(false);
        const int BUFFER_SIZE = 1024;
        const int DELAY_RECONNECT_MILLISECONDS = 3000; // default 3 second delay

        #endregion

        #region Public properties

        public NotificationEngineStates State
        {
            get
            {
                return _state;
            }
        }
        private NotificationEngineStates _state = NotificationEngineStates.NotificationEngineState_Idle;

        public int MaxSuccesses
        {
            get
            {
                return _maxSuccesses;
            }
        }
        private int _maxSuccesses = 10;

        public int MaxFailures
        {
            get
            {
                return _maxFailures;
            }
        }
        private int _maxFailures = 1;

        #endregion

        #region Constructors

        public CLNotificationSseEngine(
                        CLSyncBox syncBox, 
                        StartEngineTimeout delegateStartEngineTimeout, 
                        CancelEngineTimeout delegateCancelEngineTimeout,
                        SendManualPoll delegateSendManualPoll = null)   // not used
        {
            if (syncBox == null)
            {
                throw new ArgumentNullException("syncBox must not be null");
            }
            if (delegateStartEngineTimeout == null)
            {
                throw new ArgumentNullException("delegateStartEngineTimeout must not be null");
            }
            if (delegateCancelEngineTimeout == null)
            {
                throw new ArgumentNullException("delegateCancelEngineTimeout must not be null");
            }

            _syncBox = syncBox;
            _copiedSettings = syncBox.CopiedSettings;
            _delegateStartEngineTimeout = delegateStartEngineTimeout;
            _delegateCancelEngineTimeout = delegateCancelEngineTimeout;
        }

        public CLNotificationSseEngine()
        {
            throw new NotSupportedException("Default constructor not supported");
        }

        #endregion

        #region Public methods

        public bool Start()
        {
            bool fToReturnIsSuccess = true;     // assume success
            try
            {
                lock (_locker)
                {
                    if (_isStarted)
                    {
                        throw new InvalidOperationException("Already initialized");
                    }

                    // Start the engine.
                    StartEngineThread();
                    _isStarted = true;

                    // Wait here for the thread to finish.
                    _startComplete.WaitOne();
                    fToReturnIsSuccess = _isConnectionSuccesful;
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationSseEngine: Start: ERROR: Exception: Msg: {0}.", ex.Message);
                fToReturnIsSuccess = false;
            }

            return fToReturnIsSuccess;
        }

        public void Close()
        {
            lock (_locker)
            {
                StopEngineThread();
                _isStarted = false;
            }
        }
        
        #endregion

        #region Private methods

        private void StartEngineThread()
        {
            try
            {
                lock (_engineThread)
                {
                    if (_engineThread.Value == null)
                    {
                        _engineThread.Value = new Thread(new ParameterizedThreadStart(this.StartThreadProc));
                        _engineThread.Value.Name = "SSE Engine";
                        _engineThread.Value.IsBackground = true;
                        _engineThread.Value.Start(this);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationSseEngine: StartEngineThread: ERROR: Exception: Msg: {0}.", ex.Message);
                throw;
            }
        }

        private void StopEngineThread()
        {
            lock (_engineThread)
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
                    // Call event here: NativeMethods.WSALookupServiceEnd(monitorLookup);
                }
            }
        }

        private void StartThreadProc(object obj)
        {
            // Initialize
            CLNotificationSseEngine castState = obj as CLNotificationSseEngine;
            if (castState == null)
            {
                throw new InvalidCastException("obj must be a CLNotificationSseEngine");
            }
            castState._state = NotificationEngineStates.NotificationEngineState_Starting;

            // Build the query string.
            string query = Helpers.QueryStringBuilder(
                new[]
                {
                    new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBox.SyncBoxId.ToString()), // no need to escape string characters since the source is an integer
                    new KeyValuePair<string, string>(CLDefinitions.QueryStringSender, Uri.EscapeDataString(_copiedSettings.DeviceId)) // possibly user-provided string, therefore needs escaping
                });

            bool continueReconnecting = true;
            while (continueReconnecting)
            {
                HttpWebRequest sseRequest = (HttpWebRequest)HttpWebRequest.Create(
                                    CLDefinitions.CLNotificationServerSseURL +
                                    CLDefinitions.MethodPathPushSubscribe +
                                    query);

                sseRequest.Method = CLDefinitions.HeaderAppendMethodGet;
                sseRequest.Accept = "text/event-stream";
                sseRequest.Referer = CLDefinitions.CLNotificationServerSseURL;
                sseRequest.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                sseRequest.Headers["Accept-Language"] = "en-us,en;q=0.9";
                sseRequest.KeepAlive = true; // <-- Connection
                sseRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient; // set client

                // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                sseRequest.Headers[CLDefinitions.CLClientVersionHeaderName] = _copiedSettings.ClientVersion; // set client version
                sseRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendCWS0 +
                                    CLDefinitions.HeaderAppendKey +
                                    _syncBox.Credential.Key + ", " +
                                    CLDefinitions.HeaderAppendSignature +
                                            Helpers.GenerateAuthorizationHeaderToken(
                                                _syncBox.Credential.Secret,
                                                httpMethod: sseRequest.Method,
                                                pathAndQueryStringAndFragment: CLDefinitions.MethodPathPushSubscribe + query) +
                                               // Add token if specified
                                                (!String.IsNullOrEmpty(_syncBox.Credential.Token) ?
                                                        CLDefinitions.HeaderAppendToken + _syncBox.Credential.Token :
                                                        String.Empty);
                sseRequest.SendChunked = false; // do not send chunked
                sseRequest.Timeout = CLDefinitions.HttpTimeoutDefaultMilliseconds; // set timeout.  The timeout does not apply to the amount of time the readStream stays open to read server events.

                #region trace request
                // if communication is supposed to be traced, then trace it
                if ((_copiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    // trace communication for the current request
                    ComTrace.LogCommunication(_copiedSettings.TraceLocation, // location of trace file
                        _copiedSettings.DeviceId, // device id
                        _syncBox.SyncBoxId, // syncbox id
                        CommunicationEntryDirection.Request, // direction is request
                        CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe, // location for the server method
                        true, // trace is enabled
                        sseRequest.Headers, // headers of request
                        (string)null,  // no body
                        null, // no status code for requests
                        _copiedSettings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                        sseRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                        null,  // no content length header
                        (sseRequest.Expect == null ? "100-continue" : sseRequest.Expect), // expect value which would be part of the headers (but cannot be pulled from headers directly)
                        (sseRequest.KeepAlive ? "Keep-Alive" : "Close")); // keep-alive value which would be part of the headers (but cannot be pulled from headers directly)
                }
                #endregion

                #region Send the request and get the response.

                // Send the request and receive the immediate response.
                _delegateStartEngineTimeout(timeoutMilliseconds: CLDefinitions.HttpTimeoutDefaultMilliseconds, userState: this);
                HttpWebResponse response;
                WebException storeWebEx = null;
                try
                {
                    response = (HttpWebResponse)sseRequest.GetResponse();
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        response = (HttpWebResponse)ex.Response;
                        storeWebEx = ex;
                    }
                    else
                    {
                        throw new Exception("WebException thrown without a Response", ex);
                    }
                }

                try
                {
                    _delegateCancelEngineTimeout();

                    // check response status code == 200 here
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK: // continue reconnecting case, may or may not have data

                            // start engine timeout delegate

                            // Get the stream associated with the response.
                            using (Stream receiveStream = response.GetResponseStream())
                            {
                                // cancel engine timeout delegate

                                // Loop reading commands from the server.
                                while (true)
                                {
                                    // Start the engine timeout delegate

                                    Console.WriteLine("Start reading the data");
                                    StringBuilder sbReceived = new StringBuilder(null);
                                    while (true)
                                    {
                                        int intByteRead = receiveStream.ReadByte();

                                        if (intByteRead != -1)
                                        {
                                            // Got a byte.  Add it to the received string.
                                            sbReceived.Append(intByteRead);
                                        }
                                        else
                                        {
                                            // We are at the end of the stream
                                            break;
                                        }
                                    }

                                    // Cancel the engine timeout delegate.

                                    // We have a buffer full of data that should represent one or more commands from the server.
                                    Console.WriteLine("Data read: {" + sbReceived + "}");

                                    // process events here, but be careful of splits between buffers
                                    // follow specifications at http://www.w3.org/TR/2009/WD-eventsource-20091029/#parsing-an-event-stream
                                    // the character combination "\r\n" for carriage return plus line feed is the exact split between events in a stream, but may be omitted for the final event so check once more at the end
                                    // store excess characters from the end of the buffer (may be unlimitted size if a single event can be at least the size of one buffer) to be processed seperately at the end
                                }
                            }

                            // Successful.  Post the waiting event.
                            _isConnectionSuccesful = true;
                            _startComplete.Set();

                            Thread.Sleep(DELAY_RECONNECT_MILLISECONDS); // delay as per SSE specification

                            break;

                        case HttpStatusCode.NoContent: // do not continue reconnecting, do not check for data

                            continueReconnecting = false; // no content status code is used by the server to end a connection

                            // force close the underlying connection somehow (don't know how)

                            // for tracing purposes only, may still want to 'try' to pull out response stream here if there is any response body content (which is incorrect given the no content status code!)

                            // Successful.  Post the waiting event.
                            _isConnectionSuccesful = true;
                            _startComplete.Set();

                            break;

                        default: // invalid status code

                            // pull out response stream here for trace output to see the actual error message from the server

                            throw storeWebEx; // rethrow the stored exception from above, also may be wrapped with an outer exception message and passed via InnerException
                    }
                }
                finally
                {
                    response.Close();
                }
                #endregion
            }

            return;  // exit thread
        }

        public void TimerExpired(object userState)
        {
            _trace.writeToLog(1, "CLNotificationSseEngine: TimerExpired: Entry.");
        }

        #endregion
    }
}
//#endif // TRASH