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

        #region Private Constructors

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

        /// <summary>
        /// Private constructor to create CLCredentials from JsonContracts.Session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="settings"></param>
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

        #endregion  // end Private Constructors

        #region Public Utilities

        /// <summary>
        /// Determine whether the credentials were instantiated with a temporary token.
        /// </summary>
        /// <returns>bool: true: The token exists.</returns>
        public bool IsSessionToken()
        {
            return !String.IsNullOrEmpty(_token);
        }

        #endregion  // end Public Utilities

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

        #region ListAllActiveSessions (query the cloud for all active sessions for these credentials)
        /// <summary>
        /// Asynchronously starts listing the sessions on the server for the current credentials.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="settings">(optional) Settings to use with this request.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginListAllActiveSessions(
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
                        // declare the specific type of response for this operation
                        CLCredentials [] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = ListAllActiveSessions(
                            out status,
                            out response,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsListSessionsResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
                                response), // the specific type of response for this operation
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
        public CLError EndListAllActiveSessions(IAsyncResult aResult, out CredentialsListSessionsResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsListSessionsResult>(aResult, out result);
        }

        /// <summary>
        /// Lists the sessions boxes on the server for the current application
        /// </summary>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) An array of CLCredential objects representing the sessions in the cloud.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ListAllActiveSessions(out CLHttpRestStatus status, out CLCredentials [] response, ICLCredentialsSettings settings = null)
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
                JsonContracts.CredentialsListSessionsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.CredentialsListSessionsResponse>(
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

                // Convert the server response to the requested output format.
                if (responseFromServer != null && responseFromServer.Sessions != null)
                {
                    List<CLCredentials> listCredentials = new List<CLCredentials>();
                    foreach (JsonContracts.Session session in responseFromServer.Sessions)
                    {
                        if (session != null)
                        {
                            listCredentials.Add(new CLCredentials(session, copiedSettings));
                        }
                        else
                        {
                            listCredentials.Add(null);
                        }
                    }
                    response = listCredentials.ToArray();
                }
                else
                {
                    throw new NullReferenceException("Server responded without an array of Sessions");
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLCredentials []>();
                return ex;
            }
            return null;
        }
        #endregion (query the cloud for all active sessions for these credentials)

        #region CreateSessionForSyncboxIds (create a new set of session credentials for a list of syncbox IDs using the current credentials)
        /// <summary>
        /// Asynchronously starts creating a session on the server for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginCreateSessionForSyncboxIds(AsyncCallback callback,
            object callbackUserState,
            HashSet<long> syncboxIds = null,
            Nullable<long> tokenDurationMinutes = null,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolder.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsSessionCreateResult>(
                        callback,
                        callbackUserState),
                    settings = settings,
                    syncboxIds = syncboxIds,
                    tokenDurationMinutes = tokenDurationMinutes
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        CLHttpRestStatus status;    // &&&&& Fix this
                        // declare the specific type of response for this operation
                        CLCredentials response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = CreateSessionForSyncboxIds(
                            out status,
                            out response,
                            Data.syncboxIds,
                            Data.tokenDurationMinutes,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsSessionCreateResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
                                response), // the specific type of response for this operation
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
        /// Finishes creating the session on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the creation of the session</param>
        /// <param name="result">(output) The result from creating the session</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndCreateSessionForSyncboxIds(IAsyncResult aResult, out CredentialsSessionCreateResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsSessionCreateResult>(aResult, out result);
        }

        /// <summary>
        /// Creates a session on the server for the current application
        /// </summary>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CreateSessionForSyncboxIds(
                    out CLHttpRestStatus status, 
                    out CLCredentials response,
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

                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Determine the request JSON contract to use.  If the syncboxIds parameter is null, use the "all"
                // contract.  Otherwise, build the contract that includes an array of SyncboxIds.
                object requestContract = null;
                if (syncboxIds == null)
                {
                    Cloud.JsonContracts.CredentialsSessionCreateAllRequest sessionCreateAll = new JsonContracts.CredentialsSessionCreateAllRequest()
                    {
                        SessionIds = CLDefinitions.RESTRequestSession_SyncboxIdsAll,
                        TokenDuration = tokenDurationMinutes
                    };
                    requestContract = sessionCreateAll;
                }
                else
                {
                    Cloud.JsonContracts.CredentialsSessionCreateRequest sessionCreate = new JsonContracts.CredentialsSessionCreateRequest()
                    {
                        SessionIds = syncboxIds.ToArray<long>(),
                        TokenDuration = tokenDurationMinutes
                    };
                    requestContract = sessionCreate;
                }

                // Communicate with the server
                JsonContracts.CredentialsSessionCreateResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.CredentialsSessionCreateResponse>(
                    requestContract, 
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthCreateSession,
                    Helpers.requestMethod.post,
                    copiedSettings.HttpTimeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkCreatedNotModifiedNoContent,
                    ref status,
                    copiedSettings,
                    this,
                    null);

                // Convert the server response to a CLCredentials object and pass that back as the response.
                if (responseFromServer != null && responseFromServer.Session != null)
                {
                    response = new CLCredentials(responseFromServer.Session);
                }
                else
                {
                    throw new Exception("No session returned from server");
                }

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }
            return null;
        }
        #endregion  // end CreateSessionForSyncboxIds (create a new set of session credentials using the current credentials)

        #region GetSessionForKey (get and existing session from the server using its key)
        /// <summary>
        /// Asynchronously starts creating a session on the server for the current application
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginGetSessionForKey(
            AsyncCallback callback,
            object callbackUserState,
            string key,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolder.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsSessionGetForKeyResult>(
                        callback,
                        callbackUserState),
                    key = key,
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
                        // declare the specific type of response for this operation
                        CLCredentials response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetSessionForKey(
                            out status,
                            out response,
                            Data.key,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsSessionGetForKeyResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
                                response), // the specific type of response for this operation
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
        /// Finishes creating the session on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the creation of the session</param>
        /// <param name="result">(output) The result from creating the session</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSessionForKey(IAsyncResult aResult, out CredentialsSessionGetForKeyResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsSessionGetForKeyResult>(aResult, out result);
        }

        /// <summary>
        /// Creates a session on the server for the current application
        /// </summary>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetSessionForKey(
                    out CLHttpRestStatus status,
                    out CLCredentials response,
                    string key,
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

                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    Helpers.EnumerateSingleItem(new KeyValuePair<string, string>(CLDefinitions.RESTRequestSession_KeyId, Uri.EscapeDataString(key))));

                // Communicate with the server
                JsonContracts.CredentialsSessionGetForKeyResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.CredentialsSessionGetForKeyResponse>(
                    null,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthShowSession + query,
                    Helpers.requestMethod.get,
                    copiedSettings.HttpTimeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    this,
                    null);

                // Convert the server response to a CLCredentials object and pass that back as the response.
                if (responseFromServer != null && responseFromServer.Session != null)
                {
                    response = new CLCredentials(responseFromServer.Session);
                }
                else
                {
                    throw new Exception("No session returned from server");
                }

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetSessionForKey (get and existing session from the server using its key)

        #region DeleteSessionWithKey (delete a session in the cloud)
        /// <summary>
        /// Asynchronously starts deleting a session on the server for the current credentials.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="key">Key of the session to delete.</param>
        /// <param name="settings">(optional) Settings to use with this request.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginDeleteSessionWithKey(
            AsyncCallback callback,
            object callbackUserState,
            string key,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolder.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsSessionDeleteResult>(
                        callback,
                        callbackUserState),
                    key = key,
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
                        // declare the specific type of response for this operation
                        JsonContracts.CredentialsSessionDeleteResponse response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = DeleteSessionWithKey(
                            Data.key,
                            out status,
                            out response,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsSessionDeleteResult(
                                processError, // any error that may have occurred during processing
                                status, // the output status of communication
                                response), // the specific type of response for this operation
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
        /// Finishes deleting a session on the server for the current credentials if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sessions</param>
        /// <param name="result">(output) The result from listing the sessions</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteSessionWithKey(IAsyncResult aResult, out CredentialsSessionDeleteResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsSessionDeleteResult>(aResult, out result);
        }

        /// <summary>
        /// Deletes a session on the server for the current credentials.
        /// </summary>
        /// <param name="key">The key of the session to delete.</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) An array of CLCredential objects representing the sessions in the cloud.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteSessionWithKey(
            string key, 
            out CLHttpRestStatus status, 
            out JsonContracts.CredentialsSessionDeleteResponse response, 
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
                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Build the query string.
                Cloud.JsonContracts.CredentialsSessionDeleteRequest sessionDeleteRequest = new JsonContracts.CredentialsSessionDeleteRequest()
                {
                    Key = key
                };

                // Communicate with the server.
                response = Helpers.ProcessHttp<JsonContracts.CredentialsSessionDeleteResponse>(
                    sessionDeleteRequest,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthDeleteSession,
                    Helpers.requestMethod.post,
                    copiedSettings.HttpTimeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    this,
                    null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.CredentialsSessionDeleteResponse>();
                return ex;
            }
            return null;
        }
        #endregion (delete a session in the cloud)

        #endregion  // public authorization HTTP API calls

        #region Public, But Hidden HTTP API calls for CloudApp

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
                        // declare the specific type of response for this operation
                        JsonContracts.LinkDeviceFirstTimeResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = LinkDeviceFirstTime(
                            request,
                            castState.Item2,
                            out status,
                            out response,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new LinkDeviceFirstTimeResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    response), // the specific type of response for this operation
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
            return Helpers.EndAsyncOperation<LinkDeviceFirstTimeResult>(aResult, out result);
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
                        // declare the specific type of response for this operation
                        JsonContracts.LinkDeviceResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = LinkDevice(
                            request,
                            castState.Item2,
                            out status,
                            out response,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new LinkDeviceResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    response), // the specific type of response for this operation
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
            return Helpers.EndAsyncOperation<LinkDeviceResult>(aResult, out result);
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
                        // declare the specific type of response for this operation
                        JsonContracts.UnlinkDeviceResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UnlinkDevice(
                            request,
                            castState.Item2,
                            out status,
                            out response,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new UnlinkDeviceResult(
                                    processError, // any error that may have occurred during processing
                                    status, // the output status of communication
                                    response), // the specific type of response for this operation
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
            return Helpers.EndAsyncOperation<UnlinkDeviceResult>(aResult, out result);
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

        #endregion  // end Public, But Hidden HTTP API calls for CloudApp
    }

    #region Public Enums

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

    #endregion  // end Public Enums
}