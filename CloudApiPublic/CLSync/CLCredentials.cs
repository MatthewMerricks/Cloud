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

        /// <summary>
        /// The session token expiration date.
        /// </summary>
        internal Nullable<DateTime> ExpirationDate
        {
            get
            {
                return _expirationDate;
            }
        }
        private readonly Nullable<DateTime> _expirationDate;

        /// <summary>
        /// The syncbox IDs associated with these credentials.
        /// </summary>
        internal HashSet<long> SyncboxIds
        {
            get
            {
                return _syncboxIds;
            }
        }
        private readonly HashSet<long> _syncboxIds;

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
        /// <param name="token">(optional) The temporary token to use.  Default: null.</param>
        /// <returns>Returns any error that occurred in construction or initialization, if any, or null.</returns>
        public static CLError AllocAndInit(
            string key,
            string secret,
            out CLCredentials credentials,
            string token = null,
            ICLCredentialsSettings settings = null)
        {
            try
            {
                credentials = new CLCredentials(
                    key,
                    secret,
                    token,
                    settings);
            }
            catch (Exception ex)
            {
                credentials = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }

            return null;
        }

        #endregion

        #region Private Constructors

        /// <summary>
        /// Private constructor
        /// </summary>
        private CLCredentials(
            string key,
            string secret,
            string token, 
            ICLCredentialsSettings settings = null)
        {
            // check input parameters
            if (string.IsNullOrEmpty(key))
            {
                throw new CLArgumentNullException(CLExceptionCode.Credentials_NullKey, Resources.CLCredentialKeyCannotBeNull);
            }
            if (string.IsNullOrEmpty(secret))
            {
                throw new CLArgumentNullException(CLExceptionCode.Credentials_NullSecret, Resources.CLCredentialSecretCannotBeNull);
            }

            // Since we allow null then reverse-null coalesce from empty string
            if (token == string.Empty)
            {
                token = null;
            }

            this._key = key;
            this._secret = secret;
            
            this._token = token;

            // copy settings so they don't change while processing; this also defaults some values
            _copiedSettings = (settings == null
                ? NullDeviceId.Instance.CopySettings()
                : settings.CopySettings());

            // setup ServicePointManager as needed here (to be shared with all communication calls everywhere)
            lock (servicePointManagerConfigured)
            {
                if (!servicePointManagerConfigured.Value)
                {
                    // 12 is Default Connection Limit (6 up/6 down)
                    ServicePointManager.DefaultConnectionLimit = CLDefinitions.MaxNumberOfConcurrentDownloads + CLDefinitions.MaxNumberOfConcurrentUploads;

                    ServicePointManager.UseNagleAlgorithm = true;
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.CheckCertificateRevocationList = true;

                    servicePointManagerConfigured.Value = true;
                }
            }
        }

        private static readonly GenericHolder<bool> servicePointManagerConfigured = new GenericHolder<bool>(false);

        /// <summary>
        /// Private constructor to create CLCredentials from JsonContracts.Session.
        /// </summary>
        /// <param name="session">The JSON contract to use to construct this CLCredentials object.</param>
        /// <param name="settings">The settings to use.</param>
        private CLCredentials(JsonContracts.Session session, ICLCredentialsSettings settings = null)
        {
            // check input parameters
            if (string.IsNullOrEmpty(session.Key))
            {
                throw new CLArgumentNullException(CLExceptionCode.Credentials_NullKey, Resources.CLCredentialKeyCannotBeNull);
            }
            if (string.IsNullOrEmpty(session.Secret))
            {
                throw new CLArgumentNullException(CLExceptionCode.Credentials_NullSecret, Resources.CLCredentialSecretCannotBeNull);
            }

            // Since we allow null then reverse-null coalesce from empty string
            string token = session.Token;
            if (token == string.Empty)
            {
                token = null;
            }

            this._key = session.Key;
            this._secret = session.Secret;

            this._token = token;
            this._expirationDate = session.ExpiresAt;
            this._syncboxIds = session.SyncboxIds;

            // copy settings so they don't change while processing; this also defaults some values
            _copiedSettings = (settings == null
                ? NullDeviceId.Instance.CopySettings()
                : settings.CopySettings());
        }

        #endregion  // end Private Constructors

        #region Public Utilities

        /// <summary>
        /// Determine whether the credentials were instantiated with a temporary token.
        /// </summary>
        /// <returns>bool: true: The token exists.</returns>
        public bool IsSessionCredentials()
        {
            return !String.IsNullOrEmpty(_token);
        }

        /// <summary>
        /// Determine whether the session credentials have expired (!IsValid).  Requires session credentials.
        /// </summary>
        /// <param name="isValid">(output) The result.  True: The session credentials have not expired.</param>
        /// <returns>Any error that occurs, or null.</returns>
        public CLError IsValid(out bool isValid)
        {
            try
            {
                if (!IsSessionCredentials())
                {
                    throw new CLException(CLExceptionCode.Credentials_NotSessionCredentials, Resources.ExceptionCredentialsIsValidRequiresSessionCredentials);
                }

                if (ExpirationDate == null)
                {
                    throw new CLException(CLExceptionCode.Credentials_ExpirationDateMustNotBeNull, Resources.ExceptionCredentialsExpirationDateMustNotBeNull);
                }

                if (ExpirationDate < DateTime.UtcNow)
                {
                    isValid = true;
                }

                isValid = false;
            }
            catch (Exception ex)
            {
                isValid = false;
                return ex;
            }

            return null;
        }

        #endregion  // end Public Utilities

        #region public authorization HTTP API calls
        #region default settings for CLCredentials HTTP calls
        private sealed class NullDeviceId : ICLSyncSettings
        {
            public string DeviceId
            {
                get
                {
                    return null;
                }
            }

            public static readonly NullDeviceId Instance = new NullDeviceId();

            private NullDeviceId() { }
        }
        #endregion

        #region ListAllActiveSessionCredentials (query the cloud for all active sessions for these credentials)
        /// <summary>
        /// Asynchronously starts listing the sessions on the server for the current credentials.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback above.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginListAllActiveSessionCredentials(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsListSessionsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    settings = settings
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of response for this operation
                        CLCredentials [] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = ListAllActiveSessionCredentials(
                            out response,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsListSessionsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes listing sessions on the server for the current application, if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndListAllActiveSessionCredentials(IAsyncResult asyncResult, out CredentialsListSessionsResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsListSessionsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Lists the sessions on the server for the current application
        /// </summary>
        /// <param name="activeSessionCredentials">(output) An array of CLCredential objects representing the sessions related to these credentials.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ListAllActiveSessionCredentials(out CLCredentials [] activeSessionCredentials, ICLCredentialsSettings settings = null)
        {
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters
                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    copiedSettings,
                    Credentials: this,
                    SyncboxId: null, 
                    isOneOff: false);

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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandListAllSessionCredentialsOneSessionResponseWasInvalid);
                        }
                    }
                    activeSessionCredentials = listCredentials.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutSessions);
                }
            }
            catch (Exception ex)
            {
                activeSessionCredentials = Helpers.DefaultForType<CLCredentials []>();
                return ex;
            }
            return null;
        }
        #endregion (query the cloud for all active sessions for these credentials)

        #region CreateSessionCredentialsForSyncboxIds (create a new set of session credentials for a list of syncbox IDs using the current credentials)
        /// <summary>
        /// Asynchronously starts creating a session on the server for the current application
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="timeToLiveMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginCreateSessionCredentialsForSyncboxIds(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            HashSet<long> syncboxIds = null,
            Nullable<long> timeToLiveMinutes = null,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsSessionCreateResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    settings = settings,
                    syncboxIds = syncboxIds,
                    tokenDurationMinutes = timeToLiveMinutes
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of response for this operation
                        CLCredentials response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = CreateSessionCredentialsForSyncboxIds(
                            out response,
                            Data.syncboxIds,
                            Data.tokenDurationMinutes,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsSessionCreateResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes creating the session on the server for the current application, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndCreateSessionCredentialsForSyncboxIds(IAsyncResult asyncResult, out CredentialsSessionCreateResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsSessionCreateResult>(asyncResult, out result);
        }

        /// <summary>
        /// Creates a session on the server for the current application, and activates the session for a list of syncboxIds.
        /// </summary>
        /// <param name="sessionCredentials">(output) The output session credentials.</param>
        /// <param name="syncboxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="timeToLiveMinutes">(optional) The number of minutes before the token expires. Default: 2160 minutes (36 hours).  Maximum: 7200 minutes (120 hours).</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CreateSessionCredentialsForSyncboxIds(
                    out CLCredentials sessionCredentials,
                    HashSet<long> syncboxIds = null,
                    Nullable<long> timeToLiveMinutes = null,
                    ICLCredentialsSettings settings = null)
        {
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // Determine the request JSON contract to use.  If the syncboxIds parameter is null, use the "all"
                // contract.  Otherwise, build the contract that includes an array of SyncboxIds.
                object requestContract = null;
                if (syncboxIds == null)
                {
                    Cloud.JsonContracts.CredentialsSessionCreateAllRequest sessionCreateAll = new JsonContracts.CredentialsSessionCreateAllRequest()
                    {
                        SessionIds = CLDefinitions.RESTRequestSession_SyncboxIdsAll,
                        TokenDuration = timeToLiveMinutes
                    };
                    requestContract = sessionCreateAll;
                }
                else
                {
                    Cloud.JsonContracts.CredentialsSessionCreateRequest sessionCreate = new JsonContracts.CredentialsSessionCreateRequest()
                    {
                        SessionIds = syncboxIds.ToArray<long>(),
                        TokenDuration = timeToLiveMinutes
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
                    copiedSettings,
                    this,
                    null, 
                    false);

                // Convert the server response to a CLCredentials object and pass that back as the response.
                if (responseFromServer != null && responseFromServer.Session != null)
                {
                    sessionCredentials = new CLCredentials(responseFromServer.Session);
                }
                else
                {
                    throw new CLException(CLExceptionCode.OnDemand_ServerResponseNoSession, Resources.ExceptionOnDemandServerResponseNoSession);
                }

            }
            catch (Exception ex)
            {
                sessionCredentials = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }
            return null;
        }
        #endregion  // end CreateSessionForSyncboxIds (create a new set of session credentials using the current credentials)

        #region GetSessionForKey (get and existing session from the server using its key)
        /// <summary>
        /// Asynchronously requests credentials information from the server.  This method takes the application's credentials and requests
        /// the credentials for a specific session, identified by the session key.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="sessionKey">The key to use to query the session on the server.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginSessionCredentialsForKey(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string sessionKey,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsSessionGetForKeyResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    key = sessionKey,
                    settings = settings
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of response for this operation
                        CLCredentials response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = SessionCredentialsForKey(
                            out response,
                            Data.key,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsSessionGetForKeyResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying the session by key for the current application from the server, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndSessionCredentialsForKey(IAsyncResult asyncResult, out CredentialsSessionGetForKeyResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsSessionGetForKeyResult>(asyncResult, out result);
        }

        /// <summary>
        /// Asynchronously requests credentials information from the server.  This method takes the application's credentials and requests
        /// the credentials for a specific session, identified by the session key.
        /// </summary>
        /// <param name="sessionCredentials">(output) response object from communication</param>
        /// <param name="sessionKey">The key to use to query the session on the server.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError SessionCredentialsForKey(
                    out CLCredentials sessionCredentials,
                    string sessionKey,
                    ICLCredentialsSettings settings = null)
        {
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    Helpers.EnumerateSingleItem(new KeyValuePair<string, string>(CLDefinitions.RESTRequestSession_KeyId, Uri.EscapeDataString(sessionKey))));

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
                    copiedSettings,
                    this,
                    null, 
                    true);

                // Convert the server response to a CLCredentials object and pass that back as the response.
                if (responseFromServer != null && responseFromServer.Session != null)
                {
                    sessionCredentials = new CLCredentials(responseFromServer.Session);
                }
                else
                {
                    throw new CLException(CLExceptionCode.OnDemand_ServerResponseNoSession, Resources.ExceptionOnDemandServerResponseNoSession);
                }
            }
            catch (Exception ex)
            {
                sessionCredentials = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetSessionForKey (get and existing session from the server using its key)

        #region DeleteSessionCredentialsWithKey (delete a session in the cloud)
        /// <summary>
        /// Asynchronously starts invalidating a set of session credentials on the server for the current credentials.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback above.</param>
        /// <param name="key">Key of the session to invalidate.</param>
        /// <param name="settings">(optional) Settings to use with this request.</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
        public IAsyncResult BeginDeleteSessionCredentialsWithKey(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string key,
            ICLCredentialsSettings settings = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CredentialsSessionDeleteResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    key = key,
                    settings = settings
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of response for this operation
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = DeleteSessionCredentialsWithKey(
                            Data.key,
                            Data.settings);

                        Data.toReturn.Complete(
                            new CredentialsSessionDeleteResult(
                                processError), // any error that may have occurred during processing
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
        /// Finishes deleting a session on the server for the current credentials, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteSessionCredentialsWithKey(IAsyncResult asyncResult, out CredentialsSessionDeleteResult result)
        {
            return Helpers.EndAsyncOperation<CredentialsSessionDeleteResult>(asyncResult, out result);
        }

        /// <summary>
        /// Invalidates a set of session credentials on the server for the current credentials.
        /// </summary>
        /// <param name="key">The key of the session to invalidate.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteSessionCredentialsWithKey(
            string key, 
            ICLCredentialsSettings settings = null)
        {
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters
                if (!(copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // Build the query string.
                Cloud.JsonContracts.CredentialsSessionDeleteRequest sessionDeleteRequest = new JsonContracts.CredentialsSessionDeleteRequest()
                {
                    Key = key
                };

                // Communicate with the server.
                JsonContracts.CredentialsSessionDeleteResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.CredentialsSessionDeleteResponse>(
                    sessionDeleteRequest,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthDeleteSession,
                    Helpers.requestMethod.post,
                    copiedSettings.HttpTimeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    copiedSettings,
                    this,
                    null, 
                    false);

                // Convert the server response to a CLCredentials object and pass that back as the response.
                if (responseFromServer == null && responseFromServer.Status != null)
                {
                    throw new CLException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionOnDemandNoServerResponse);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion (delete a session in the cloud)

        #endregion  // public authorization HTTP API calls

        #region Public, But Hidden HTTP API calls for CloudApp

        //// --------- adding \cond and \endcond makes the section in between hidden from doxygen

        // \cond
        #region LinkDeviceFirstTime

        /// <summary>
        /// Asynchronously starts a request to create an account with a device and a new Syncbox.
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="request">The request.  Note: It is not necessary to set Key and Secret.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
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
                        // declare the specific type of response for this operation
                        JsonContracts.LinkDeviceFirstTimeResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = LinkDeviceFirstTime(
                            request,
                            castState.Item2,
                            out response,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new LinkDeviceFirstTimeResult(
                                    processError, // any error that may have occurred during processing
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
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError EndLinkDeviceFirstTime(IAsyncResult asyncResult, out LinkDeviceFirstTimeResult result)
        {
            return Helpers.EndAsyncOperation<LinkDeviceFirstTimeResult>(asyncResult, out result);
        }

        /// <summary>
        /// Registers a user, links a device and creates a Syncbox, all at the same time.
        /// </summary>
        /// <param name="request">The parameters to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        /// <remarks>400 Bad Request is accepted without error.  Check for this code.  It generally means the account already exits.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError LinkDeviceFirstTime(
                    JsonContracts.LinkDeviceFirstTimeRequest request, 
                    int timeoutMilliseconds, 
                    out JsonContracts.LinkDeviceFirstTimeResponse response,
                    ICLCredentialsSettings settings = null)
        {
            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.ExceptionOnDemandLinkDeviceFirstTimeRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
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
                    copiedSettings, // pass the copied settings
                    this, // pass the key/secret
                    null,
                    false); // no unique id of the sync box on the server
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
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="request">The request.  Note: It is not necessary to set Key or Secret.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
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
                        CLExceptionCode status;
                        // declare the specific type of response for this operation
                        JsonContracts.LinkDeviceResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = LinkDevice(
                            request,
                            castState.Item2,
                            out response,
                            castState.Item3);

                        if (processError != null)
                        {
                            status = processError.PrimaryException.Code;
                        }

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new LinkDeviceResult(
                                    processError, // any error that may have occurred during processing
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
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError EndLinkDevice(IAsyncResult asyncResult, out LinkDeviceResult result)
        {
            return Helpers.EndAsyncOperation<LinkDeviceResult>(asyncResult, out result);
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
                    out JsonContracts.LinkDeviceResponse response,
                    ICLCredentialsSettings settings = null)
        {
            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.ExceptionOnDemandLinkDeviceRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
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
                    copiedSettings, // pass the copied settings
                    this, // pass the key/secret
                    null, // no unique id of the sync box on the server
                    false);
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
        /// <param name="aState">User state to pass when firing async callback</param>
        /// <param name="request">The request.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns IAsyncResult, which can be used to interact with the asynchronous task.</returns>
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
                        // declare the specific type of response for this operation
                        JsonContracts.UnlinkDeviceResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UnlinkDevice(
                            request,
                            castState.Item2,
                            out response,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new UnlinkDeviceResult(
                                    processError, // any error that may have occurred during processing
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
        /// <param name="aResult">The asynchronous result provided upon starting the async operation.</param>
        /// <param name="result">(output) The result from the async operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CLError EndUnlinkDevice(IAsyncResult asyncResult, out UnlinkDeviceResult result)
        {
            return Helpers.EndAsyncOperation<UnlinkDeviceResult>(asyncResult, out result);
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
                    out JsonContracts.UnlinkDeviceResponse response,
                    ICLCredentialsSettings settings = null)
        {
            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.ExceptionOnDemandUnlinkDeviceRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullDeviceId.Instance.CopySettings()
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
                    copiedSettings, // pass the copied settings
                    this, // pass the key/secret
                    null, // no unique id of the sync box on the server
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.UnlinkDeviceResponse>();
                return ex;
            }

            return null;
        }
        #endregion  // Unlink Device

        // \endcond
        #endregion  // end Public, But Hidden HTTP API calls for CloudApp
    }
}