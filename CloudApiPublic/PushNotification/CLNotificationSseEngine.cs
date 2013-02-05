//  ICLNotificationSseEngine.cs
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

namespace CloudApiPublic.PushNotification
{
    internal sealed class CLNotificationSseEngine : ICLNotificationEngine
    {
        #region Private fields

        private static CLTrace _trace = CLTrace.Instance;
        private const int _maxFailures = 1;
        private const int _maxReconnects = 5;
        private int _currentOpenAttemptCount = 0;
        private int _currentReconnectAttemptCount = 0;
        private readonly GenericHolder<Thread> engineThread = new GenericHolder<Thread>(null);
        private readonly object _locker = new object();
        private bool _IsInitialized = false;
        private CLSyncBox _syncBox = null;
        private ICLSyncSettingsAdvanced _copiedSettings = null;
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        const int BUFFER_SIZE = 1024;

        #endregion

        #region Private classes

        private class RequestState
        {
            // This class stores the State of the request. 
            const int BUFFER_SIZE = 1024;
            public StringBuilder requestData;
            public byte[] BufferRead;
            public HttpWebRequest request;
            public HttpWebResponse response;
            public Stream streamResponse;
            public RequestState()
            {
                BufferRead = new byte[BUFFER_SIZE];
                requestData = new StringBuilder("");
                request = null;
                streamResponse = null;
            }
        }
        
        #endregion

        #region Public properties

        public NotificationEngineStates State
        {
            get
            {
                return _state;
            }
            private set
            {
                _state = value;
            }
        }
        private NotificationEngineStates _state = NotificationEngineStates.NotificationEngineState_Idle;

        public event EventHandler<EventArgs> EvtFailureCountReached;
    
        #endregion

        #region Constructors

        public CLNotificationSseEngine(CLSyncBox syncBox)
        {
            _syncBox = syncBox;
            _copiedSettings = syncBox.CopiedSettings;
        }

        public CLNotificationSseEngine()
        {
            throw new NotSupportedException("Default constructor not supported");
        }

        #endregion

        #region Public methods

        public void Open()
        {
            lock (_locker)
            {
                if (_IsInitialized)
                {
                    throw new InvalidOperationException("Already initialized");
                }

                StartEngineThread();
                _IsInitialized = true;
            }
        }
        public void Close()
        {
            lock (_locker)
            {
                StopEngineThread();
                _IsInitialized = false;
            }
        }
        
        #endregion


        #region Private methods
        
        private void StartEngineThread()
        {
            lock (engineThread)
            {
                if (engineThread.Value == null)
                {
                    engineThread.Value = new Thread(new ParameterizedThreadStart(this.OpenThreadProc));
                    engineThread.Value.Name = "Notification Engine";
                    engineThread.Value.IsBackground = true;
                    engineThread.Value.Start(this);
                }
            }
        }

        private void StopEngineThread()
        {
            lock (engineThread)
            {
                if (engineThread.Value != null)
                {
                    try
                    {
                        engineThread.Value.Abort();
                    }
                    catch
                    {
                    }
                    engineThread.Value = null;
                    // Call event here: NativeMethods.WSALookupServiceEnd(monitorLookup);
                }
            }
        }

        private void OpenThreadProc(object obj)
        {
            // Initialize
            CLNotificationSseEngine castState = obj as CLNotificationSseEngine;
            if (castState == null)
            {
                throw new InvalidCastException("obj must be a CLNotificationSseEngine");
            }
            castState._currentOpenAttemptCount = 0;
            castState._currentReconnectAttemptCount = 0;
            castState.State = NotificationEngineStates.NotificationEngineState_Starting;

            // Loop attempting to open to the server
            for (_currentOpenAttemptCount = 0; _currentOpenAttemptCount < _maxFailures; ++_currentOpenAttemptCount)
            {
                HttpWebRequest sseRequest = (HttpWebRequest)HttpWebRequest.Create(CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe);
                sseRequest.Method = CLDefinitions.HeaderAppendMethodGet;
                sseRequest.Accept = "text/event-stream";
                sseRequest.Referer = CLDefinitions.CLNotificationServerSseURL;
                sseRequest.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                sseRequest.Headers["Accept-Language"] = "en-us,en;q=0.9";
                sseRequest.KeepAlive = true; // <-- Connection
                sseRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient; // set client
                // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                sseRequest.Headers[CLDefinitions.CLClientVersionHeaderName] =  _copiedSettings.ClientVersion; // set client version
                sseRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendCWS0 +
                                    CLDefinitions.HeaderAppendKey +
                                    _syncBox.Credential.Key + ", " +
                                    CLDefinitions.HeaderAppendSignature +
                                            Helpers.GenerateAuthorizationHeaderToken(
                                                _syncBox.Credential.Secret,
                                                httpMethod: sseRequest.Method,
                                                pathAndQueryStringAndFragment: CLDefinitions.MethodPathPushSubscribe) +
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
                        (string) null,  // no body
                        null, // no status code for requests
                        _copiedSettings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                        sseRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                        null,  // no content length header
                        (sseRequest.Expect == null ? "100-continue" : sseRequest.Expect), // expect value which would be part of the headers (but cannot be pulled from headers directly)
                        (sseRequest.KeepAlive ? "Keep-Alive" : "Close")); // keep-alive value which would be part of the headers (but cannot be pulled from headers directly)
                }
                #endregion

                #region Send the request
		 
                RequestState requestState = new RequestState();  
                requestState.request = sseRequest;


                // Start the asynchronous request.
                IAsyncResult result=
                (IAsyncResult) sseRequest.BeginGetResponse(new AsyncCallback(RespCallback), requestState);

                // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), sseRequest, CLDefinitions.HttpTimeoutDefaultMilliseconds, true);

                // The response came in the allowed time. The work processing will happen in the  callback function.
                allDone.WaitOne();

                // Release the HttpWebResponse resource.
                requestState.response.Close();

            	#endregion
            }

            // Did we fail?
            if (_currentOpenAttemptCount >= _maxFailures)
            {
                castState.State = NotificationEngineStates.NotificationEngineState_Failed;
                if (EvtFailureCountReached != null)
                {
                    EvtFailureCountReached(this, new EventArgs());
                }
            }

            return;  // exit thread
        }

        // Abort the request if the timer fires. 
        private static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        private static void RespCallback(IAsyncResult asynchronousResult)
        {  
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState=(RequestState) asynchronousResult.AsyncState;
                HttpWebRequest  myHttpWebRequest=myRequestState.request;
                myRequestState.response = (HttpWebResponse) myHttpWebRequest.EndGetResponse(asynchronousResult);

                // Read the response into a Stream object.
                Stream responseStream = myRequestState.response.GetResponseStream();
                myRequestState.streamResponse=responseStream;

                // Begin the Reading of the contents of the HTML page and print it to the console.
                IAsyncResult asynchronousInputRead = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                return;
            }
            catch(WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}",e.Message);
                Console.WriteLine("\nStatus:{0}",e.Status);
            }

            allDone.Set();
        }

        private static   void ReadCallBack(IAsyncResult asyncResult)
        {
            try
            {

            RequestState myRequestState = (RequestState)asyncResult.AsyncState;
            Stream responseStream = myRequestState.streamResponse;
            int read = responseStream.EndRead( asyncResult );

            // Start reading the actual response data.
            if (read > 0)
            {
                myRequestState.requestData.Append(Encoding.ASCII.GetString(myRequestState.BufferRead, 0, read));
                IAsyncResult asynchronousResult = responseStream.BeginRead( myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                return;
            }
            else
            {
                Console.WriteLine("\nThe contents of the Html page are : ");
                if(myRequestState.requestData.Length>1)
                {
                    string stringContent;
                    stringContent = myRequestState.requestData.ToString();
                    Console.WriteLine(stringContent);
                }

                Console.WriteLine("Press any key to continue..........");
                Console.ReadLine();

                responseStream.Close();
            }

            }
            catch(WebException e)
            {
                Console.WriteLine("\nReadCallBack Exception raised!");
                Console.WriteLine("\nMessage:{0}",e.Message);
                Console.WriteLine("\nStatus:{0}",e.Status);
            }

            allDone.Set();
        }
        #endregion
    }
}
