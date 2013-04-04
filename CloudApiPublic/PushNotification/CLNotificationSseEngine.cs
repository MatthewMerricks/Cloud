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
        private CLSyncBox _syncBox = null;
        private ICLSyncSettingsAdvanced _copiedSettings = null;
        private CreateEngineTimer _delegateCreateEngineTimer = null;
        private StartEngineTimeout _delegateStartEngineTimeout = null;
        private CancelEngineTimeout _delegateCancelEngineTimeout = null;
        private DisposeEngineTimer _delegateDisposeEngineTimer = null;
        private SendNotificationEvent _delegateSendNotificationEvent = null;
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
                        CLSyncBox syncBox, 
                        CreateEngineTimer delegateCreateEngineTimer,
                        StartEngineTimeout delegateStartEngineTimeout, 
                        CancelEngineTimeout delegateCancelEngineTimeout,
                        DisposeEngineTimer delegateDisposeEngineTimer,
                        SendNotificationEvent delegateSendNotificationEvent)
        {
            if (syncBox == null)
            {
                throw new ArgumentNullException("syncBox must not be null");
            }
            if (delegateCreateEngineTimer == null)
            {
                throw new ArgumentNullException("delegateCreateEngineTimer must not be null");
            }
            if (delegateStartEngineTimeout == null)
            {
                throw new ArgumentNullException("delegateStartEngineTimeout must not be null");
            }
            if (delegateCancelEngineTimeout == null)
            {
                throw new ArgumentNullException("delegateCancelEngineTimeout must not be null");
            }
            if (delegateDisposeEngineTimer == null)
            {
                throw new ArgumentNullException("delegateDisposeEngineTimer must not be null");
            }
            if (delegateSendNotificationEvent == null)
            {
                throw new ArgumentNullException("delegateSendNotificationEvent must not be null");
            }

            _syncBox = syncBox;
            _copiedSettings = syncBox.CopiedSettings;
            _delegateCreateEngineTimer = delegateCreateEngineTimer;
            _delegateStartEngineTimeout = delegateStartEngineTimeout;
            _delegateCancelEngineTimeout = delegateCancelEngineTimeout;
            _delegateDisposeEngineTimer= delegateDisposeEngineTimer;
            _delegateSendNotificationEvent = delegateSendNotificationEvent;
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
                _trace.writeToLog(9, "CLNotificationSseEngine: Start: Entry.");
                lock (_locker)
                {
                    if (_isEngineThreadStarted)
                    {
                        throw new InvalidOperationException("Already initialized");
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
                _trace.writeToLog(1, "CLNotificationSseEngine: Start: ERROR: Exception: Msg: {0}.", ex.Message);
                fToReturnIsSuccess = false;
            }

            _trace.writeToLog(9, "CLNotificationSseEngine: Start: Exit.  Return: {0}.", fToReturnIsSuccess);
            return fToReturnIsSuccess;
        }

        public void Stop()
        {
            // Stop this engine's thread
            _trace.writeToLog(9, "CLNotificationSseEngine: Stop: Entry.");
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

        private void StartThreadProc(object obj)
        {
            bool wasThreadAborted = false;

            #region Build HTTP SSE Request

            string query = String.Empty;
            HttpWebRequest sseRequest = null;
            try
            {
                // Initialize
                _trace.writeToLog(9, "CLNotificationSseEngine: StartThreadProc: Entry.");
                CLNotificationSseEngine castState = obj as CLNotificationSseEngine;
                if (castState == null)
                {
                    throw new InvalidCastException("obj must be a CLNotificationSseEngine");
                }

                // Build the query string.
                query = Helpers.QueryStringBuilder(
                    new[]
                    {
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBox.SyncBoxId.ToString()), // no need to escape string characters since the source is an integer
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
                    _trace.writeToLog(1, "CLNotificationSseEngine: StartThreadProc: ERROR: Exception (3): Msg: {0}.", ex.Message);
                    error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);

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
                            _trace.writeToLog(1, "CLNotificationSseEngine: StartThreadProc: ERROR: Exception (4): Msg: {0}.", ex.Message);
                            error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
                        }
                        else
                        {
                            throw new Exception("WebException thrown without a Response", ex);
                        }
                    }
                }

                _delegateCancelEngineTimeout();

                // check response status code == 200 here
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK: // continue reconnecting case, may or may not have data

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
                                        _trace.writeToLog(9, "CLNotificationSseEngine: StartThreadProc: Start reading data from the response stream.");
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

                        _trace.writeToLog(1, "CLNotificationSseEngine: StartThreadProc: Received 204 no content.");
                        if ((_syncBox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            using (Stream responseStream = response.GetResponseStream())
                            {

                                ComTrace.LogCommunication(
                                    traceLocation: _syncBox.CopiedSettings.TraceLocation,
                                    UserDeviceId: _syncBox.CopiedSettings.DeviceId,
                                    SyncBoxId: _syncBox.SyncBoxId,
                                    Direction: CommunicationEntryDirection.Response,
                                    DomainAndMethodUri: CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe + query,
                                    traceEnabled: true,
                                    headers: (WebHeaderCollection)null,
                                    body: responseStream,
                                    statusCode: (int)HttpStatusCode.NoContent,
                                    excludeAuthorization: _syncBox.CopiedSettings.TraceExcludeAuthorization);
                            }
                            _trace.writeToLog(9, "CLNotificationSseEngine: StartThreadProc: Received no content from server.");
                        }

                        // In this case, the server has told us positively not to reconnect.  Return failure to the manager.
                        _fToReturnIsSuccess = false;
                        _startComplete.Set();

                        break;

                    default: // invalid status code

                        _trace.writeToLog(1, "CLNotificationSseEngine: StartThreadProc: Received invalid status code.");
                        if ((_syncBox.CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            using (Stream responseStream = response.GetResponseStream())
                            {
                                ComTrace.LogCommunication(
                                    traceLocation: _syncBox.CopiedSettings.TraceLocation,
                                    UserDeviceId: _syncBox.CopiedSettings.DeviceId,
                                    SyncBoxId: _syncBox.SyncBoxId,
                                    Direction: CommunicationEntryDirection.Response,
                                    DomainAndMethodUri: CLDefinitions.CLNotificationServerSseURL + CLDefinitions.MethodPathPushSubscribe + query,
                                    traceEnabled: true,
                                    headers: (WebHeaderCollection)null,
                                    body: responseStream,
                                    statusCode: (int)response.StatusCode,
                                    excludeAuthorization: _syncBox.CopiedSettings.TraceExcludeAuthorization);
                            }
                        }

                        throw storeWebEx; // rethrow the stored exception from above, also may be wrapped with an outer exception message and passed via InnerException
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
                    _trace.writeToLog(1, "CLNotificationSseEngine: StartThreadProc: ERROR: Exception: Msg: {0}.", ex.Message);
                    error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);

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
            _trace.writeToLog(9, "CLNotificationSseEngine: StartEngineThread: Exit.");
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
                    _trace.writeToLog(1, "CLNotificationSseEngine: ProcessReceivedCharacter: ERROR. Not expecting char: {0}.", cCharRead);
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
                    _trace.writeToLog(1, "CLNotificationSseEngine: ProcessReceivedCharacter: Value is not an int: {0}.", _value);
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
                _trace.writeToLog(1, "CLNotificationSseEngine: ProcessReceivedCharacter: Value is not an int: {0}.", _value);
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
                _trace.writeToLog(9, "CLNotificationSseEngine: TimerExpired: Entry.");
                CLNotificationSseEngine castState = userState as CLNotificationSseEngine;
                if (castState != null)
                {
                    // Send an unsubscribe to the server.  Allow just 200 ms for this to complete.
                    CLHttpRestStatus status;
                    JsonContracts.NotificationUnsubscribeResponse response;
                    CLError errorFromUnsubscribe = SendUnsubscribeToServer(200, out status, out response, castState._syncBox);
                    if (errorFromUnsubscribe != null)
                    {
                        _trace.writeToLog(1, "CLNotificationSseEngine: TimerExpired: ERROR: Msg: {0}.", errorFromUnsubscribe.errorDescription);
                        errorFromUnsubscribe.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
                    }

                    // Fail the service manager thread.
                    _fToReturnIsSuccess = false;
                    _startComplete.Set();

                    // Kill the engine thread
                    if (_engineThread.Value != null)
                    {
                        _trace.writeToLog(9, "CLNotificationSseEngine: TimerExpired: Abort the engine thread.");
                        _engineThread.Value.Abort();
                    }

                    // Clean up any resources left over.
                    lock (_locker)
                    {
                        foreach (IDisposable toDispose in _resourcesToCleanUp)
                        {
                            try
                            {
                                _trace.writeToLog(9, "CLNotificationSseEngine: TimerExpired: Clean up resource: {0}.", toDispose.ToString());
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
                    _trace.writeToLog(1, "CLNotificationSseEngine: TimerExpired: ERROR: Improper user state.");
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "CLNotificationSseEngine: TimerExpired: ERROR: Exception: Msg: {0}.", ex.Message);
                error.LogErrors(_syncBox.CopiedSettings.TraceLocation, _syncBox.CopiedSettings.LogErrors);
            }
            _trace.writeToLog(9, "CLNotificationSseEngine: TimerExpired: Exit.");
        }

        /// <summary>
        /// Unsubscribe this SyncBox/Device ID from Sync notifications.Add a Sync box on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="syncBox">the SyncBox to use.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        private CLError SendUnsubscribeToServer(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.NotificationUnsubscribeResponse response,
                    CLSyncBox syncBox)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters
                _trace.writeToLog(9, "CLNotificationSseEngine: SendUnsubscribeToServer: Entry.");
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                if (syncBox == null)
                {
                    throw new ArgumentException("syncBox must not be null");
                }

                if (syncBox.CopiedSettings == null)
                {
                    throw new NullReferenceException("syncBox.CopiedSettings must not be null");
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = syncBox.CopiedSettings.CopySettings();

                JsonContracts.NotificationUnsubscribeRequest request = new JsonContracts.NotificationUnsubscribeRequest()
                {
                    DeviceId = copiedSettings.DeviceId,
                    SyncBoxId = syncBox.SyncBoxId
                };


                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    new[]
                    {
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBox.SyncBoxId.ToString()), // no need to escape string characters since the source is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSender, Uri.EscapeDataString(_copiedSettings.DeviceId)) // possibly user-provided string, therefore needs escaping
                    });


                _trace.writeToLog(9, "CLNotificationSseEngine: SendUnsubscribeToServer: Send unsubscribe.");
                response = Helpers.ProcessHttp<JsonContracts.NotificationUnsubscribeResponse>(
                    null,           // no body needed
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathPushUnsubscribe + query,
                    Helpers.requestMethod.post,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    syncBox.Credential,
                    syncBox.SyncBoxId);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLNotificationSseEngine: SendUnsubscribeToServer: ERROR: Exception: Msg: {0}.", ex.Message);
                response = Helpers.DefaultForType<JsonContracts.NotificationUnsubscribeResponse>();
                return ex;
            }

            _trace.writeToLog(9, "CLNotificationSseEngine: SendUnsubscribeToServer: Return OK.");
            return null;
        }

        #endregion
    }
}
