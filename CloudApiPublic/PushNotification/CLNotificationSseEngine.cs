//  CLNotificationSseEngine.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using Cloud.Static;
using Cloud.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Cloud.PushNotification
{
    internal sealed class CLNotificationSseEngine : ICLNotificationEngine
    {
        #region Private fields

        private static CLTrace _trace = CLTrace.Instance;
        private CLSyncbox _syncbox = null;
        private ICLSyncSettingsAdvanced _copiedSettings = null;
        private CreateEngineTimer _delegateCreateEngineTimer = null;
        private StartEngineTimeout _delegateStartEngineTimeout = null;
        private CancelEngineTimeout _delegateCancelEngineTimeout = null;
        private DisposeEngineTimer _delegateDisposeEngineTimer = null;
        private SendNotificationEvent _delegateSendNotificationEvent = null;
        private SendManualPoll _delegateSendManualPoll = null;
        private bool _isEngineThreadStarted = false;
        private bool _fToReturnIsSuccess = false;
        private readonly object _locker = new object();
        private readonly GenericHolder<Thread> _engineThread = new GenericHolder<Thread>(null);
        private readonly ManualResetEvent _startComplete = new ManualResetEvent(false);
        private StringBuilder _sbCurrentLine = new StringBuilder(null);
        private StringBuilder _sbData = new StringBuilder(null);
        private string _field = String.Empty;
        private string _value = String.Empty;
        private string _eventName = String.Empty;
        private string _lastEventId = String.Empty;
        private int _reconnectionTime = 0;
        private EnumSseStates _stateParse = EnumSseStates.SseState_Idle;
        private HashSet<IDisposable> _resourcesToCleanUp = new HashSet<IDisposable>();

        const int BUFFER_SIZE = 1024;
        const int DELAY_RECONNECT_MILLISECONDS = 3000; // default 3 second delay

        #endregion  // Private fields

        #region Private Enumerations

        private enum EnumSseStates : uint
        {
            SseState_Idle = 0,
            SseState_LookForLineEndingChar,
            SseState_LookForLineEndingGotCR
        }

        private enum SseEngineStates : uint
        {
            SseEngineState_Idle = 0,
            SseEngineState_Starting,
            SseEngineState_Started,
            SseEngineState_Cancelled,
            SseEngineState_Failed,
        }

        #endregion  // Private enumerations

        #region Public properties

        public int MaxSuccesses
        {
            get
            {
                return _maxSuccesses;
            }
        }
        private int _maxSuccesses = 5;

        public int MaxFailures
        {
            get
            {
                return _maxFailures;
            }
        }
        private int _maxFailures = 1;

        #endregion  // Public properties

        #region Constructors

        public CLNotificationSseEngine(
                        CLSyncbox syncbox, 
                        CreateEngineTimer delegateCreateEngineTimer,
                        StartEngineTimeout delegateStartEngineTimeout, 
                        CancelEngineTimeout delegateCancelEngineTimeout,
                        DisposeEngineTimer delegateDisposeEngineTimer,
                        SendNotificationEvent delegateSendNotificationEvent,
                        SendManualPoll delegateSendManualPoll)
        {
            if (syncbox == null)
            {
                throw new ArgumentNullException(Resources.SyncboxMustNotBeNull);
            }
            if (delegateCreateEngineTimer == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationSseEngineDelegateCreateEngineTimerMustNotBeNull);
            }
            if (delegateStartEngineTimeout == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationSseEnginedelegateStartEngineTimeoutMustNotBeNull);
            }
            if (delegateCancelEngineTimeout == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationSseEngineDelegateCancelEngineTimeoutMustNotBeNull);
            }
            if (delegateDisposeEngineTimer == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationSseEngineDelegateDisposeEngineTimerMustNotBeNull);
            }
            if (delegateSendNotificationEvent == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationSseEngineDelegateSendNotificationEventMustNotBeNull);
            }
            if (delegateSendManualPoll == null)
            {
                throw new ArgumentNullException(Resources.CLNotificationSseEngineDelegateSendNotificationsDelegateSendManualPollMustNotBeNull);
            }

            _syncbox = syncbox;
            _copiedSettings = syncbox.CopiedSettings;
            _delegateCreateEngineTimer = delegateCreateEngineTimer;
            _delegateStartEngineTimeout = delegateStartEngineTimeout;
            _delegateCancelEngineTimeout = delegateCancelEngineTimeout;
            _delegateDisposeEngineTimer= delegateDisposeEngineTimer;
            _delegateSendNotificationEvent = delegateSendNotificationEvent;
            _delegateSendManualPoll = delegateSendManualPoll;
        }

        public CLNotificationSseEngine()
        {
            throw new NotSupportedException(Resources.CLNotificationsSseEngineDefaultConstructorNotSupported);
        }

        #endregion

        #region Public methods

        public bool Start()
        {
            bool fToReturnIsSuccess = true;     // assume success
            try
            {
                _trace.writeToLog(9, Resources.CLNotificationSseEngineStartEntry);
                lock (_locker)
                {
                    if (_isEngineThreadStarted)
                    {
                        throw new InvalidOperationException(Resources.CLNotificationSseEngineAlreadyInitialized);
                    }

                    _delegateCreateEngineTimer(this);

                    // Start the engine.
                    StartEngineThread();
                    _isEngineThreadStarted = true;
                }

                // Wait here for the thread to finish.
                _startComplete.WaitOne();
                fToReturnIsSuccess = _fToReturnIsSuccess;
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationSseEngineStartErrorExceptionMsg0, ex.Message);
                fToReturnIsSuccess = false;
            }

            _trace.writeToLog(9, Resources.CLNotificationSseEngineStartExitReturn0, fToReturnIsSuccess);
            return fToReturnIsSuccess;
        }

        public void Stop()
        {
            // Stop this engine's thread
            _trace.writeToLog(9, Resources.CLNotificationSseEngineStopEntry);
            TimerExpired(this);
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
                        _engineThread.Value.Name = Resources.CLNotificationSseEngineSSEEngine;
                        _engineThread.Value.IsBackground = true;
                        _engineThread.Value.Start(this);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, Resources.CLNotificationSseEngineStartEngineThreadErrorExceptionMsg0, ex.Message);
                throw;
            }
        }

        private void StartThreadProc(object obj)
        {
            bool wasThreadAborted = false;

            #region Build HTTP SSE Request

            string query = String.Empty;
            HttpWebRequest sseRequest = null;
            try
            {
                // Initialize
                _trace.writeToLog(9, Resources.CLNotificationSseEngineStartThreadProcEntry);
                CLNotificationSseEngine castState = obj as CLNotificationSseEngine;
                if (castState == null)
                {
                    throw new InvalidCastException(Resources.CLNotificationSseEngineObjectMustBeCLNNotificationService);
                }

                // Build the query string.
                query = Helpers.QueryStringBuilder(
                    new[]
                    {
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()), // no need to escape string characters since the source is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSender, Uri.EscapeDataString(_copiedSettings.DeviceId)) // possibly user-provided string, therefore needs escaping
                    });

                sseRequest = (HttpWebRequest)HttpWebRequest.Create(
                                    CLDefinitions.CLNotificationServerSseURL +
                                    CLDefinitions.MethodPathPushSubscribe +
                                    query);

                sseRequest.Method = CLDefinitions.HeaderAppendMethodGet;
                sseRequest.Accept = CLDefinitions.HeaderSseEventStreamValue;
                sseRequest.Referer = CLDefinitions.CLNotificationServerSseURL;
                sseRequest.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                sseRequest.Headers[CLDefinitions.HeaderAcceptLanguage] = CLDefinitions.HeaderSseAcceptLanguageValue;
                sseRequest.KeepAlive = true; // <-- Connection
                sseRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient; // set client


                // Send a Last-Event-ID header on a reconnect.
                if (_lastEventId.Length != 0)
                {
                    sseRequest.Headers[CLDefinitions.HeaderLastEventId] = _lastEventId;
                }

                // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
                sseRequest.Headers[CLDefinitions.CLClientVersionHeaderName] = OSVersionInfo.GetClientVersionHttpHeader(_copiedSettings.ClientDescription); // set client version
                sseRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendCWS0 +
                                    CLDefinitions.HeaderAppendKey +
                                    _syncbox.Credentials.Key + ", " +
                                    CLDefinitions.HeaderAppendSignature +
                                            Helpers.GenerateAuthorizationHeaderToken(
                                                _syncbox.Credentials.Secret,
                                                httpMethod: sseRequest.Method,
                                                pathAndQueryStringAndFragment: CLDefinitions.MethodPathPushSubscribe + query) +
                    // Add token if specified
                                                (!String.IsNullOrEmpty(_syncbox.Credentials.Token) ?
                                                        CLDefinitions.HeaderAppendToken + _syncbox.Credentials.Token :
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
                        _syncbox.SyncboxId, // syncbox id
                        CommunicationEntryDirection.Request, // direction is request
                        CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe, // location for the server method
                        true, // trace is enabled
                        sseRequest.Headers, // headers of request
                        (string)null,  // no body
                        null, // no status code for requests
                        _copiedSettings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                        sseRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                        null,  // no content length header
                        (sseRequest.Expect == null ? Resources.NotTranslatedHttpContinue : sseRequest.Expect), // expect value which would be part of the headers (but cannot be pulled from headers directly)
                        (sseRequest.KeepAlive ? Resources.NotTranslatedHttpKeepAlive : Resources.NotTranslatedHttpClose)); // keep-alive value which would be part of the headers (but cannot be pulled from headers directly)
                }
                #endregion
            }
            catch (ThreadAbortException)
            {
                wasThreadAborted = true;
            }
            catch (Exception ex)
            {
                if (!wasThreadAborted)
                {
                    CLError error = ex;
                    _trace.writeToLog(1, Resources.CLNotificationSseEngineStartThreadProcErrorException3Msg0, ex.Message);
                    error.Log(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.LogErrors);

                    // This exception probably occurred because we had difficulty reaching the server.  In that case, we should
                    // probably retry soon because it may just be a temporary inability to communication.  So, we have to reverse
                    // the logic of "success" vs. "failure" as far as the service manager is concerned.
                    // Wait here to add some delay.
                    int reconnectionTimeout = DELAY_RECONNECT_MILLISECONDS;     // default from SSE spec
                    if (_reconnectionTime != 0)
                    {
                        reconnectionTimeout = _reconnectionTime;
                    }
                    Thread.Sleep(reconnectionTimeout);

                    // Tell the manager we succeeded.
                    _fToReturnIsSuccess = true;
                    _startComplete.Set();
                }
            }

            #endregion  

            #region Send the request and get the response.

            // Send the request and receive the immediate response.
            WebResponse boxedResponse = null;
            HttpWebResponse response = null;
            WebException storeWebEx = null;

            try
            {
                _delegateStartEngineTimeout(timeoutMilliseconds: CLDefinitions.HttpTimeoutDefaultMilliseconds);
                try
                {
                    boxedResponse = sseRequest.GetResponse();   // sends the request and blocks for the response
                    lock (_locker)
                    {
                        _resourcesToCleanUp.Add(boxedResponse);
                    }
                    response = (HttpWebResponse)boxedResponse;
                }
                catch (ThreadAbortException)
                {
                    wasThreadAborted = true;
                }
                catch (WebException ex)
                {
                    if (!wasThreadAborted)
                    {
                        if (ex.Response != null)
                        {
                            boxedResponse = ex.Response;
                            lock (_locker)
                            {
                                _resourcesToCleanUp.Add(boxedResponse);
                            }
                            response = (HttpWebResponse)boxedResponse;
                            storeWebEx = ex;

                            CLError error = ex;
                            _trace.writeToLog(1, Resources.CLNotificationSseEngineStartThreadProcErrorException4Msg0, ex.Message);
                            error.Log(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.LogErrors);
                        }
                        else
                        {
                            throw new Exception(Resources.CLNotificationSseEngineWebExceptionThrownWithoutAResponse, ex);
                        }
                    }
                }

                _delegateCancelEngineTimeout();

                // check response status code == 200 here
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK: // continue reconnecting case, may or may not have data

                        // We subscribed.
                        // The push server can lose events because they are not queued.  Events can occur from other devices while we are momentarily disconnected from the push server.
                        // The server regularly disconnects us, and we will resubscribe.  Send a manual poll every time we are reconnected.
                        _delegateSendManualPoll();

                        // Get the stream associated with the response.
                        _delegateStartEngineTimeout(timeoutMilliseconds: CLDefinitions.HttpTimeoutDefaultMilliseconds);
                        Stream receiveStream = null;
                        try
                        {
                            using (receiveStream = response.GetResponseStream())
                            {
                                lock (_locker)
                                {
                                    _resourcesToCleanUp.Add(receiveStream);
                                }
                                _delegateCancelEngineTimeout();

                                StreamReader readStream = null;
                                try
                                {
                                    using (readStream = new StreamReader(receiveStream, Encoding.UTF8))
                                    {
                                        _resourcesToCleanUp.Add(readStream);
                                        _trace.writeToLog(9, Resources.CLNotificationSseEngineStartThreadProcStartReadingDataFromResponseStream);
                                        while (true)
                                        {
                                            char[] unicodeCharBuffer = new char[1];
                                            _delegateStartEngineTimeout(timeoutMilliseconds: CLDefinitions.HttpTimeoutDefaultMilliseconds);
                                            int bytesRead = readStream.Read(unicodeCharBuffer, 0, 1);        // read one byte
                                            _delegateCancelEngineTimeout();

                                            if (bytesRead != 0)
                                            {
                                                // Got a byte.  Process it.
                                                ProcessReceivedCharacter(unicodeCharBuffer[0]);
                                            }
                                            else
                                            {
                                                // We are at the end of the stream
                                                ProcessEndOfStream();
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (ThreadAbortException)
                                {
                                    wasThreadAborted = true;
                                }
                                finally
                                {
                                    if (!wasThreadAborted)
                                    {
                                        _delegateCancelEngineTimeout();
                                        if (readStream != null)
                                        {
                                            lock (_locker)
                                            {
                                                _resourcesToCleanUp.Remove(readStream);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            wasThreadAborted = true;
                        }
                        finally
                        {
                            if (!wasThreadAborted)
                            {
                                _delegateCancelEngineTimeout();
                                if (receiveStream != null)
                                {
                                    lock (_locker)
                                    {
                                        _resourcesToCleanUp.Remove(receiveStream);
                                    }
                                }
                            }
                        }


                        // At this point the server has normally closed the session for some reason.  We will wait the specified amount of time, then
                        // post the service manager thread to continue with a success, and this thread will exit, freeing any resources.  The service
                        // manager will decide which engine to restart.
                        int reconnectionTimeout = DELAY_RECONNECT_MILLISECONDS;     // default from SSE spec
                        if (_reconnectionTime != 0)
                        {
                            reconnectionTimeout = _reconnectionTime;
                        }
                        Thread.Sleep(reconnectionTimeout);

                        // Successful.  Post the waiting event.
                        _fToReturnIsSuccess = true;
                        _startComplete.Set();

                        break;

                    case HttpStatusCode.NoContent: // do not continue reconnecting, do not check for data

                        _trace.writeToLog(1, Resources.CLNotificationSseEngineStartThreadProcReceived204NoContent);
                        if ((_syncbox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            using (Stream responseStream = response.GetResponseStream())
                            {

                                ComTrace.LogCommunication(
                                    traceLocation: _syncbox.CopiedSettings.TraceLocation,
                                    UserDeviceId: _syncbox.CopiedSettings.DeviceId,
                                    SyncboxId: _syncbox.SyncboxId,
                                    Direction: CommunicationEntryDirection.Response,
                                    DomainAndMethodUri: CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe + query,
                                    traceEnabled: true,
                                    headers: (WebHeaderCollection)null,
                                    body: responseStream,
                                    statusCode: (int)HttpStatusCode.NoContent,
                                    excludeAuthorization: _syncbox.CopiedSettings.TraceExcludeAuthorization);
                            }
                            _trace.writeToLog(9, Resources.CLNotificationSseEngineReceivedNoContentFromServer);
                        }

                        // In this case, the server has told us positively not to reconnect.  Return failure to the manager.
                        _fToReturnIsSuccess = false;
                        _startComplete.Set();

                        break;

                    default: // invalid status code

                        _trace.writeToLog(1, Resources.CLNotificationSseEngineReceivedInvalidStatusCode);
                        if ((_syncbox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            using (Stream responseStream = response.GetResponseStream())
                            {
                                ComTrace.LogCommunication(
                                    traceLocation: _syncbox.CopiedSettings.TraceLocation,
                                    UserDeviceId: _syncbox.CopiedSettings.DeviceId,
                                    SyncboxId: _syncbox.SyncboxId,
                                    Direction: CommunicationEntryDirection.Response,
                                    DomainAndMethodUri: CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe + query,
                                    traceEnabled: true,
                                    headers: (WebHeaderCollection)null,
                                    body: responseStream,
                                    statusCode: (int)response.StatusCode,
                                    excludeAuthorization: _syncbox.CopiedSettings.TraceExcludeAuthorization);
                            }
                        }

                        throw new Exception(string.Format("Invalid status code on starting SSE engine: {0}", ((int)response.StatusCode)), storeWebEx); // rethrow the stored exception from above, also may be wrapped with an outer exception message and passed via InnerException
                }
            }
            catch (ThreadAbortException)
            {
                wasThreadAborted = true;
            }
            catch (Exception ex)
            {
                if (!wasThreadAborted)
                {
                    CLError error = ex;
                    _trace.writeToLog(1, Resources.CLNotificationSseEngineErrorExceptionMsg0, ex.Message);
                    error.Log(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.LogErrors);

                    // This exception probably occurred because we had difficulty reaching the server.  In that case, we should
                    // probably retry soon because it may just be a temporary inability to communication.  So, we have to reverse
                    // the logic of "success" vs. "failure" as far as the service manager is concerned.
                    // Wait here to add some delay.
                    int reconnectionTimeout = DELAY_RECONNECT_MILLISECONDS;     // default from SSE spec
                    if (_reconnectionTime != 0)
                    {
                        reconnectionTimeout = _reconnectionTime;
                    }
                    Thread.Sleep(reconnectionTimeout);

                    // Tell the manager we succeeded so this engine will usually be retried.
                    _fToReturnIsSuccess = true;
                    _startComplete.Set();
                }
            }
            finally
            {
                if (!wasThreadAborted)
                {
                        _delegateCancelEngineTimeout();
                        if (boxedResponse != null)
                        {
                            try
                            {
                                ((IDisposable)boxedResponse).Dispose();
                            }
                            catch (ThreadAbortException)
                            {
                                wasThreadAborted = true;
                            }
                            catch
                            {
                            }

                            lock (_locker)
                            {
                                _resourcesToCleanUp.Remove(boxedResponse);
                            }
                            boxedResponse = null;
                            response = null;
                        }
                }
            }

            // Exit the thread here
            _trace.writeToLog(9, Resources.CLNotificationSseEngineStartEngineThreadExit);
            #endregion
        }

        private void ProcessReceivedCharacter(char cCharRead)
        {
            // The first character received might be a BYTE ORDER MARK.  If we find one, ignore it.
            if (_stateParse == EnumSseStates.SseState_Idle)
            {
                if (cCharRead == 0xFEFF)
                {
                    return;       // ignore this BYTE ORDER MARK character
                }
                _stateParse = EnumSseStates.SseState_LookForLineEndingChar;
            }

            // Process by state
            switch (_stateParse)
            {
                case EnumSseStates.SseState_LookForLineEndingChar:
                    if (cCharRead == '\r')
                    {
                        _stateParse = EnumSseStates.SseState_LookForLineEndingGotCR;
                    }
                    else if (cCharRead == '\n')
                    {
                        ProcessCurrentLine();
                    }
                    else
                    {
                        _sbCurrentLine.Append(cCharRead);
                    }
                    break;
                case EnumSseStates.SseState_LookForLineEndingGotCR:
                    if (cCharRead == '\r')
                    {
                        ProcessCurrentLine();
                        _stateParse = EnumSseStates.SseState_LookForLineEndingGotCR;
                    }
                    else if (cCharRead == '\n')
                    {
                        ProcessCurrentLine();
                        _stateParse = EnumSseStates.SseState_LookForLineEndingChar;
                    }
                    else
                    {
                        ProcessCurrentLine();
                        _sbCurrentLine.Append(cCharRead);
                        _stateParse = EnumSseStates.SseState_LookForLineEndingChar;
                    }
                    break;
                default:
                    _trace.writeToLog(1, Resources.CLNotificationSseEngineProcessReceivedCharacterErrorNotExpectingChar0, cCharRead);
                    break;
            }
        }

        private void ProcessCurrentLine()
        {
            string currentLine = _sbCurrentLine.ToString().Trim();
            if (currentLine.Length == 0)
            {
                DispatchEvent();
            }
            else if (currentLine[0] == ':')
            {
                // Specifically do nothing (ignore this line).
            }
            else if (currentLine.Contains(':'))
            {
                _field = currentLine.Substring(0, currentLine.IndexOf(':'));
                _value = currentLine.Substring(currentLine.IndexOf(':') + 1);
                ProcessField();
            }
            else
            {
                _field = currentLine;
                _value = string.Empty;
                ProcessField();
            }

            _sbCurrentLine.Clear();
        }

        private void ProcessField()
        {
            if (_field == "event")
            {
                _eventName = _value;
            }
            else if (_field == "data")
            {
                _sbData.Append(_value);
                _sbData.Append("\n");
            }
            else if (_field == "id")
            {
                _lastEventId = _value;
            }
            else if (_field == "retry")
            {
                int result;
                if (!int.TryParse(_value, out result))
                {
                    _trace.writeToLog(1, Resources.CLNotificationSseEngineProcessReceivedCharacterValueIsNotAnInt0, _value);
                }
                else
                {
                    _reconnectionTime = result;
                }
            }
            else
            {
                // Intentionally do nothing (ignore the field).
            }
        }

        private void DispatchEvent()
        {
            if (_sbData.Length == 0)
            {
                _sbData.Clear();
                _eventName = String.Empty;
                return;
            }

            if (_sbData[_sbData.Length - 1] == '\n')
            {
                _sbData.Length = _sbData.Length - 1;    // remove the newline
            }

            if (_eventName.Length > 0 && !IsEventNameRecognized(_eventName))
            {
                _trace.writeToLog(1, Resources.CLNotificationSseEngineProcessReceivedCharacterValueIsNotAnInt0, _value);
                return;
            }

            // Create a new notification event
            CLNotificationEvent evt = new CLNotificationEvent();
            evt.Name = _eventName;
            evt.Data = _sbData.ToString().Trim();
            evt.Origin = CLDefinitions.CLNotificationServerSseURL;
            evt.LastEventId = _lastEventId;

            // Reset for the next event
            _sbData.Clear();
            _eventName = string.Empty;

            // Send the event
            if (_delegateSendNotificationEvent != null)
            {
                _delegateSendNotificationEvent(evt);
            }
        }

        private bool IsEventNameRecognized(string _eventName)
        {
            //TODO: Fix this when we need formal events.
            return true;
        }

        private void ProcessEndOfStream()
        {
            ProcessCurrentLine();
            DispatchEvent();
        }

        /// <summary>
        /// This method handles the situation where this engine's thread becomes stuck.  Any disposable resources
        /// are cleaned up, and then the ManualResetEvent is set to allow the service manager thread to exit with a failure indication.
        /// </summary>
        /// <param name="userState">The state of this object.</param>
        public void TimerExpired(object userState)
        {

            try
            {
                // Get back the state.
                _trace.writeToLog(9, Resources.CLNotificationSseEngineTimerExpiredEntry);
                CLNotificationSseEngine castState = userState as CLNotificationSseEngine;
                if (castState != null)
                {
                    if (castState._syncbox == null)
                    {
                        throw new NullReferenceException(Resources.CLNotificationSseEngineSyncboxMustNotBeNull);
                    }
                    if (castState._syncbox.HttpRestClient == null)
                    {
                        throw new NullReferenceException(Resources.CLNotificationSseEngineSyncboxHttpRestCannotBeNull);
                    }

                    // Send an unsubscribe to the server.  Allow just 10 seconds for this to complete.
                    JsonContracts.NotificationUnsubscribeResponse response;
                    CLError errorFromUnsubscribe = _syncbox.HttpRestClient.SendUnsubscribeToServer(10000, out response);
                    if (errorFromUnsubscribe != null)
                    {
                        _trace.writeToLog(1, Resources.CLNotificationSseEngineTimerExpiredErrorMsg0, errorFromUnsubscribe.PrimaryException.Message);
                        errorFromUnsubscribe.Log(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.LogErrors);
                    }

                    // Fail the service manager thread.
                    _fToReturnIsSuccess = false;
                    _startComplete.Set();

                    // Kill the engine thread
                    if (_engineThread.Value != null)
                    {
                        _trace.writeToLog(9, Resources.CLNotificationSseEngineTimerExpiredAbortTheEngineThread);

                        // a ThreadAbortException is expected here, silence it
                        try 
                        {
                            _engineThread.Value.Abort();
                        }
                        catch
                        {
                        }
                    }

                    // Clean up any resources left over.
                    lock (_locker)
                    {
                        foreach (IDisposable toDispose in _resourcesToCleanUp)
                        {
                            try
                            {
                                _trace.writeToLog(9, Resources.CLNotificationSseEngineTimerExpiredCleanUpResource0, toDispose.ToString());
                                toDispose.Dispose();
                            }
                            catch
                            {
                            }
                        }

                        _resourcesToCleanUp.Clear();
                    }
                }
                else
                {
                    _trace.writeToLog(1, Resources.CLNotificationSseEngineTimerExpiredErrorImproperUserState);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, Resources.CLNotificationSseEngineTimerExpiredErrorExceptionMsg0, ex.Message);
                error.Log(_syncbox.CopiedSettings.TraceLocation, _syncbox.CopiedSettings.LogErrors);
            }
            _trace.writeToLog(9, Resources.CLNotificationSseEngineTimerExpiredExit);
        }
        #endregion
    }
}
