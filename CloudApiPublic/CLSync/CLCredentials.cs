// 
// CLCredentials.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using Cloud.REST;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace Cloud
{
    /// <summary>
    /// Contains authentication information required for all communication and services
    /// 
    /// The CLCredentials class declares the interface used for authentication and authorization to Cloud.com <http://Cloud.com>.
    ///
    /// The CLCredentials class allows the developer to represent both the Application’s credentials as well as temporary session credentials. The Application’s credentials provide access to all of your Application’s Syncboxes. Using temporary credentials, access can be limited to an individual Syncbox.
    ///
    /// If the CLCredentials object does not contain a token, all authentication and authorization attempts will be made by looking up the credentials in the Application space.
    ///
    /// If the CLCredentials object contains a token, all authentication and authorization attempts will be made by looking up the credentials in the temporary session space.
    /// </summary>
    public sealed class CLCredentials
    {
        #region Internal Properties
        /// <summary>
        /// The public key that identifies this application or session.
        /// </summary>
        internal string Key
        {
            get
            {
                return _key;
            }
        }
        private readonly string _key;

        /// <summary>
        /// The application or session secret private key.
        /// </summary>
        internal string Secret
        {
            get
            {
                return _secret;
            }
        }
        private readonly string _secret;

        /// <summary>
        /// The session token.
        /// </summary>
        internal string Token
        {
            get
            {
                return _token;
            }
        }
        private readonly string _token;

        #endregion

        #region Private Fields

        private ICLSyncSettingsAdvanced _copiedSettings = null;

        #endregion

        #region Public Credentials Factory

        /// <summary>
        /// Outputs a new credentials object from key/secret
        /// </summary>
        /// <param name="key">The public key that identifies this application or session.</param>
        /// <param name="secret">The application or session private secret.</param>
        /// <param name="credentials">(output) Created credentials object</param>
        /// <param name="status">(output) Status of creation, check this for Success</param>
        /// <param name="token">(optional) The temporary token to use.  Default: null.</param>
        /// <returns>Returns any error that occurred in construction, if any, or null.</returns>
        public static CLError AllocAndInit(
            string key,
            string secret,
            out CLCredentials credentials,
            out CLCredentialsCreationStatus status,
            string token = null,
            ICLCredentialsSettings settings = null)
        {
            status = CLCredentialsCreationStatus.ErrorUnknown;

            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
                }

                credentials = new CLCredentials(
                    key,
                    secret,
                    token,
                    ref status,
                    settings);
            }
            catch (Exception ex)
            {
                credentials = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }

            status = CLCredentialsCreationStatus.Success;
            return null;
        }

        #endregion


        /// <summary>
        /// Private constructor
        /// </summary>
        private CLCredentials(
            string Key,
            string Secret,
            string Token, 
            ref CLCredentialsCreationStatus status,
            ICLCredentialsSettings settings = null)
        {
            // check input parameters
            if (string.IsNullOrEmpty(Key))
            {
                status = CLCredentialsCreationStatus.ErrorNullKey;
                throw new NullReferenceException("Key cannot be null");
            }
            if (string.IsNullOrEmpty(Secret))
            {
                status = CLCredentialsCreationStatus.ErrorNullSecret;
                throw new NullReferenceException("Secret cannot be null");
            }

            // Since we allow null then reverse-null coalesce from empty string
            if (Token == string.Empty)
            {
                Token = null;
            }

            this._key = Key;
            this._secret = Secret;
            
            this._token = Token;

            // copy settings so they don't change while processing; this also defaults some values
            _copiedSettings = (settings == null
                ? NullSyncRoot.Instance.CopySettings()
                : settings.CopySettings());
        }

        private CLCredentials(JsonContracts.Session session, ICLCredentialsSettings settings = null)
        {
            // check input parameters
            if (string.IsNullOrEmpty(session.Key))
            {
                throw new NullReferenceException("Key cannot be null");
            }
            if (string.IsNullOrEmpty(session.Secret))
            {
                throw new NullReferenceException("Secret cannot be null");
            }

            // Since we allow null then reverse-null coalesce from empty string
            string token = session.Token;
            if (token == string.Empty)
            {
                token = null;
            }

            this._key = Key;
            this._secret = Secret;

            this._token = token;

            // copy settings so they don't change while processing; this also defaults some values
            _copiedSettings = (settings == null
                ? NullSyncRoot.Instance.CopySettings()
                : settings.CopySettings());
        }

        /// <summary>
        /// Determine whether the credentials were instantiated with a temporary token.
        /// </summary>
        /// <returns>bool: true: The token exists.</returns>
        public bool CredentialsHasToken()
        {
            return !String.IsNullOrEmpty(_token);
        }

        #region public authorization HTTP API calls
        #region default settings for CLCredentials HTTP calls
        private sealed class NullSyncRoot : ICLSyncSettings
        {
            public string SyncRoot
            {
                get
                {
                    return null;
                }
            }

            public string DeviceId
            {
                get
                {
                    return null;
                }
            }

            public static readonly NullSyncRoot Instance = new NullSyncRoot();

            private NullSyncRoot() { }
        }
        #endregion

        #region ListSessions
        /// <summary>
        /// Asynchronously starts listing the sessions on the server for the current credentials.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="settings">(optional) Settings to use with this request.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginListSessions(
            AsyncCallback callback,
            object callbackUserState,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolder.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsListSessionsResult>(
                        callback,
                        callbackUserState),
                    settings = settings
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;    // &&&&& Fix this
                        // declare the specific type of result for this operation
                        CLCredentials [] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = ListSessions(
                            out status,
                            out response,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsListSessionsResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
                                response), // the specific type of result for this operation
                            sCompleted: false); // processing did not complete synchronously
                    }
                    catch (Exception ex)
                    {
                        Data.toReturn.HandleException(
                            ex, // the exception which was not handled correctly by the CLError wrapping
                            sCompleted: false); // processing did not complete synchronously
                    }
                },
                null);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ThreadStart(asyncThread.VoidProcess))).Start(); // start the asynchronous processing thread which is attached to its data

            // return the asynchronous result
            return asyncThread.TypedData.toReturn;
        }

        /// <summary>
        /// Finishes listing sessions on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sessions</param>
        /// <param name="result">(output) The result from listing the sessions</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndListSessions(IAsyncResult aResult, out CredentialsListSessionsResult result)
        {
            // declare the specific type of asynchronous result for session listing
            GenericAsyncResult<CredentialsListSessionsResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sessions
                castAResult = aResult as GenericAsyncResult<CredentialsListSessionsResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<CredentialsListSessionsResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Lists the sessions boxes on the server for the current application
        /// </summary>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) An array of CLCredential objects representing the sessions in the cloud.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ListSessions(out CLHttpRestStatus status, out CLCredentials [] response, ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters
                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Communicate with the server.
                JsonContracts.CredentialsListSessionsResponse serverResponse;
                serverResponse = Helpers.ProcessHttp<JsonContracts.CredentialsListSessionsResponse>(
                    /* requestContent */ null, // no request body for listing sessions
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListSessions,
                    Helpers.requestMethod.post,
                    copiedSettings.HttpTimeoutMilliseconds,
                    /* uploadDownload */ null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    Credentials: this,
                    SyncboxId: null);

                // Convert the server response into an array of CLCredentials to return.
                List<CLCredentials> listCredentials = new List<CLCredentials>();
                if (serverResponse != null && serverResponse.Sessions != null)
                {
                    foreach (JsonContracts.Session session in serverResponse.Sessions)
                    {
                        listCredentials.Add(new CLCredentials(session, copiedSettings));
                    }
                }
                response = listCredentials.ToArray();

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLCredentials []>();
                return ex;
            }
            return null;
        }
        #endregion

        #region CreateSession
        /// <summary>
        /// Asynchronously starts creating a session on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginCreateSession(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            HashSet<long> syncboxIds = null,
            Nullable<long> tokenDurationMinutes = null,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SessionCreateResult> toReturn = new GenericAsyncResult<SessionCreateResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    syncboxIds,
                    tokenDurationMinutes,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialsSettings> castState =
                            state as Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.SessionCreateResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = CreateSession(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3,
                            castState.Item4,
                            castState.Item5);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SessionCreateResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes creating the session on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the creation of the session</param>
        /// <param name="result">(output) The result from creating the session</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndCreateSession(IAsyncResult aResult, out SessionCreateResult result)
        {
            // declare the specific type of asynchronous result for session creation
            GenericAsyncResult<SessionCreateResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session create result and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for creating a session
                castAResult = aResult as GenericAsyncResult<SessionCreateResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SessionCreateResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Creates a session on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CreateSession(int timeoutMilliseconds, out CLHttpRestStatus status, 
                    out JsonContracts.SessionCreateResponse response, 
                    HashSet<long> syncboxIds = null,
                    Nullable<long> tokenDurationMinutes = null,
                    ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Determine the request JSON contract to use.  If the syncboxIds parameter is null, use the "all"
                // contract.  Otherwise, build the contract that includes an array of SyncboxIds.
                object requestContract = null;
                if (syncboxIds == null)
                {
                    Cloud.JsonContracts.SessionCreateAllRequest sessionCreateAll = new JsonContracts.SessionCreateAllRequest()
                    {
                        SessionIds = CLDefinitions.RESTRequestSession_SyncboxIdsAll,
                        TokenDuration = tokenDurationMinutes
                    };
                    requestContract = sessionCreateAll;
                }
                else
                {
                    Cloud.JsonContracts.SessionCreateRequest sessionCreate = new JsonContracts.SessionCreateRequest()
                    {
                        SessionIds = syncboxIds.ToArray<long>(),
                        TokenDuration = tokenDurationMinutes
                    };
                    requestContract = sessionCreate;
                }

                response = Helpers.ProcessHttp<JsonContracts.SessionCreateResponse>(
                    requestContract, 
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthCreateSession,
                    Helpers.requestMethod.post,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkCreatedNotModifiedNoContent,
                    ref status,
                    copiedSettings,
                    this,
                    null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SessionCreateResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ShowSession
        /// <summary>
        /// Asynchronously starts showing a session on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="key">The key of the session to show.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginShowSession(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            string key,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SessionShowResult> toReturn = new GenericAsyncResult<SessionShowResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    key,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialsSettings> castState =
                            state as Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.SessionShowResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = ShowSession(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3,
                            castState.Item4);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SessionShowResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes creating the session on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the creation of the session</param>
        /// <param name="result">(output) The result from creating the session</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndShowSession(IAsyncResult aResult, out SessionShowResult result)
        {
            // declare the specific type of asynchronous result for session creation
            GenericAsyncResult<SessionShowResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session show result and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for creating a session
                castAResult = aResult as GenericAsyncResult<SessionShowResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SessionShowResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Shows a session on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="key">The key of the session to show.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ShowSession(int timeoutMilliseconds, out CLHttpRestStatus status,
                    out JsonContracts.SessionShowResponse response,
                    string key,
                    ICLCredentialsSettings settings = null
            )
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    Helpers.EnumerateSingleItem(new KeyValuePair<string, string>(CLDefinitions.RESTRequestSession_KeyId, Uri.EscapeDataString(key))));

                response = Helpers.ProcessHttp<JsonContracts.SessionShowResponse>(
                    null,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthShowSession + query,
                    Helpers.requestMethod.get,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    this,
                    null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SessionShowResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region DeleteSession
        /// <summary>
        /// Asynchronously starts deleting a session on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="key">The key of the session to delete.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginDeleteSession(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            string key,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SessionDeleteResult> toReturn = new GenericAsyncResult<SessionDeleteResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    key,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialsSettings> castState =
                            state as Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.SessionDeleteResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = DeleteSession(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3,
                            castState.Item4);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SessionDeleteResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes deleting the session on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the creation of the session</param>
        /// <param name="result">(output) The result from creating the session</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteSession(IAsyncResult aResult, out SessionDeleteResult result)
        {
            // declare the specific type of asynchronous result for session creation
            GenericAsyncResult<SessionDeleteResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session delete result and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for creating a session
                castAResult = aResult as GenericAsyncResult<SessionDeleteResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SessionDeleteResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Deletes a session on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="key">The key of the session to delete.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteSession(int timeoutMilliseconds, out CLHttpRestStatus status,
                    out JsonContracts.SessionDeleteResponse response,
                    string key,
                    ICLCredentialsSettings settings = null
            )
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                Cloud.JsonContracts.SessionDeleteRequest sessionDeleteRequest = new JsonContracts.SessionDeleteRequest()
                {
                    Key = key
                };

                response = Helpers.ProcessHttp<JsonContracts.SessionDeleteResponse>(
                    sessionDeleteRequest,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthDeleteSession,
                    Helpers.requestMethod.post,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    this,
                    null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SessionDeleteResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region LinkDeviceFirstTime

        /// <summary>
        /// Asynchronously starts a request to create an account with a device and a new Syncbox.
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="request">The request.  Note: It is not necessary to set Key and Secret.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IAsyncResult BeginLinkDeviceFirstTime(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            JsonContracts.LinkDeviceFirstTimeRequest request,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<LinkDeviceFirstTimeResult> toReturn = new GenericAsyncResult<LinkDeviceFirstTimeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialsSettings> castState = 
                            state as Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.LinkDeviceFirstTimeResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = LinkDeviceFirstTime(
                            request,
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new LinkDeviceFirstTimeResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes creating a new user account on the server with a device and new syncbox, if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sessions</param>
        /// <param name="result">(output) The result from listing the sessions</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError EndLinkDeviceFirstTime(IAsyncResult aResult, out LinkDeviceFirstTimeResult result)
        {
            // declare the specific type of asynchronous result for session listing
            GenericAsyncResult<LinkDeviceFirstTimeResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sessions
                castAResult = aResult as GenericAsyncResult<LinkDeviceFirstTimeResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<LinkDeviceFirstTimeResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Registers a user, links a device and creates a Syncbox, all at the same time.
        /// </summary>
        /// <param name="request">The parameters to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        /// <remarks>400 Bad Request is accepted without error.  Check for this code.  It generally means the account already exits.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError LinkDeviceFirstTime(
                    JsonContracts.LinkDeviceFirstTimeRequest request, 
                    int timeoutMilliseconds, 
                    out CLHttpRestStatus status, 
                    out JsonContracts.LinkDeviceFirstTimeResponse response,
                    ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new ArgumentException("pushRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // Set the Key and Secret from these credentials.
                request.Key = this.Key;
                request.Secret = this.Secret;

                // Note the 400 Bad Request.  Must check this.
                HashSet<HttpStatusCode> httpStatusCodesAccepted = new HashSet<HttpStatusCode>(new[]
                {
                    HttpStatusCode.OK,
                    HttpStatusCode.Accepted,
                    HttpStatusCode.Created,
                    HttpStatusCode.BadRequest
                });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.LinkDeviceFirstTimeResponse>(
                    request, // object to write as request content to the server
                    CLDefinitions.HttpPrefix + CLDefinitions.CloudAppSubDomainPrefix + CLDefinitions.Domain, // base domain is the registration server
                    CLDefinitions.MethodPathAuthDeviceLinkFirstTime, // path to /device/link/first_time
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    httpStatusCodesAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes.
                    ref status, // reference to update the output success/failure status for the communication
                    copiedSettings, // pass the copied settings
                    this, // pass the key/secret
                    null); // no unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.LinkDeviceFirstTimeResponse>();
                return ex;
            }

            return null;
        }
        #endregion

        #region LinkDevice

        /// <summary>
        /// Asynchronously starts a request to log in to an account with a device.
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="request">The request.  Note: It is not necessary to set Key or Secret.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IAsyncResult BeginLinkDevice(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            JsonContracts.LinkDeviceRequest request,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<LinkDeviceResult> toReturn = new GenericAsyncResult<LinkDeviceResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialsSettings> castState =
                            state as Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.LinkDeviceResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = LinkDevice(
                            request,
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new LinkDeviceResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes loggin into an account with a device, if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sessions</param>
        /// <param name="result">(output) The result from listing the sessions</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError EndLinkDevice(IAsyncResult aResult, out LinkDeviceResult result)
        {
            // declare the specific type of asynchronous result for session listing
            GenericAsyncResult<LinkDeviceResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sessions
                castAResult = aResult as GenericAsyncResult<LinkDeviceResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<LinkDeviceResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Links a device (logs in).
        /// </summary>
        /// <param name="request">The parameters to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError LinkDevice(
                    JsonContracts.LinkDeviceRequest request, 
                    int timeoutMilliseconds, 
                    out CLHttpRestStatus status, 
                    out JsonContracts.LinkDeviceResponse response,
                    ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new ArgumentException("pushRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // Set the Key and Secret from these credentials.
                request.Key = this.Key;
                request.Secret = this.Secret;

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.LinkDeviceResponse>(
                    request, // object to write as request content to the server
                    CLDefinitions.HttpPrefix + CLDefinitions.CloudAppSubDomainPrefix + CLDefinitions.Domain, // base domain is the registration server
                    CLDefinitions.MethodPathAuthDeviceLink, // path to /device/link
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    copiedSettings, // pass the copied settings
                    this, // pass the key/secret
                    null); // no unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.LinkDeviceResponse>();
                return ex;
            }

            return null;
        }
        #endregion

        #region UnlinkDevice

        /// <summary>
        /// Asynchronously starts a request to unlink a device (and log out of the user account).
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="request">The request.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IAsyncResult BeginUnlinkDevice(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            JsonContracts.UnlinkDeviceRequest request,
            ICLCredentialsSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<UnlinkDeviceResult> toReturn = new GenericAsyncResult<UnlinkDeviceResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialsSettings> asyncParams =
                new Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialsSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialsSettings> castState =
                            state as Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialsSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.UnlinkDeviceResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UnlinkDevice(
                            request,
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new UnlinkDeviceResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    result), // the specific type of result for this operation
                                    sCompleted: false); // processing did not complete synchronously
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                sCompleted: false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes logging out of an account (and unlinking the device), if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sessions</param>
        /// <param name="result">(output) The result from listing the sessions</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError EndUnlinkDevice(IAsyncResult aResult, out UnlinkDeviceResult result)
        {
            // declare the specific type of asynchronous result for session listing
            GenericAsyncResult<UnlinkDeviceResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sessions
                castAResult = aResult as GenericAsyncResult<UnlinkDeviceResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<UnlinkDeviceResult>();
                return ex;
            }

            // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
            try
            {
                // This method assumes that only 1 thread calls EndInvoke 
                // for this object
                if (!castAResult.IsCompleted)
                {
                    // If the operation isn't done, wait for it
                    castAResult.AsyncWaitHandle.WaitOne();
                    castAResult.AsyncWaitHandle.Close();
                }

                // re-pull the result for output in case it was not completed when it was pulled before
                result = castAResult.Result;

                // Operation is done: if an exception occurred, return it
                if (castAResult.Exception != null)
                {
                    return castAResult.Exception;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Unlinks a device (logs out).
        /// </summary>
        /// <param name="request">The parameters to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError UnlinkDevice(
                    JsonContracts.UnlinkDeviceRequest request, 
                    int timeoutMilliseconds, 
                    out CLHttpRestStatus status, 
                    out JsonContracts.UnlinkDeviceResponse response,
                    ICLCredentialsSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new ArgumentException("pushRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.UnlinkDeviceResponse>(
                    request, // object to write as request content to the server
                    CLDefinitions.HttpPrefix + CLDefinitions.CloudAppSubDomainPrefix + CLDefinitions.Domain, // base domain is the registration server
                    CLDefinitions.MethodPathAuthDeviceUnlink, // path to /device/unlink
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    copiedSettings, // pass the copied settings
                    this, // pass the key/secret
                    null); // no unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.UnlinkDeviceResponse>();
                return ex;
            }

            return null;
        }
        #endregion  // Unlink Device
        #endregion  // public authorization HTTP API calls
    }
    /// <summary>
    /// Status of creation of <see cref="CLCredentials"/>
    /// </summary>
    public enum CLCredentialsCreationStatus : byte
    {
        Success = 0,
        ErrorNullKey = 1,
        ErrorNullSecret = 2,
        ErrorUnknown = 3,
    }
}