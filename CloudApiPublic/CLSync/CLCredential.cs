// 
// CLCredential.cs
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
    /// The CLCredential class declares the interface used for authentication and authorization to Cloud.com <http://Cloud.com>.
    ///
    /// The CLCredential class allows the developer to represent both the Application’s credential as well as temporary session credential. The Application’s credential provides access to all of your Application’s Syncboxes. Using a temporary credential, access can be limited to an individual Syncbox.
    ///
    /// If the CLCredential object does not contain a token, all authentication and authorization attempts will be made by looking up the credential in the Application space.
    ///
    /// If the CLCredential object contains a token, all authentication and authorization attempts will be made by looking up the credential in the temporary session space.
    /// </summary>
    public sealed class CLCredential
    {
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
        /// Outputs a new credential object from key/secret
        /// </summary>
        /// <param name="key">The public key that identifies this application.</param>
        /// <param name="secret">The application secret private key.</param>
        /// <param name="credential">(output) Created credential object</param>
        /// <param name="status">(output) Status of creation, check this for Success</param>
        /// <param name="token">(optional) The temporary token to use.  Default: null.</param>
        /// <returns>Returns any error that occurred in construction, if any, or null.</returns>
        public static CLError CreateAndInitialize(
            string key,
            string secret,
            out CLCredential credential,
            out CLCredentialCreationStatus status,
            string token = null)
        {
            status = CLCredentialCreationStatus.ErrorUnknown;

            try
            {
                if (Helpers.AllHaltedOnUnrecoverableError)
                {
                    throw new InvalidOperationException(Resources.CLCredentialHelpersAllHaltedOnUnrecoverableErrorIsSet);
                }

                credential = new CLCredential(
                    key,
                    secret,
                    token,
                    ref status);
            }
            catch (Exception ex)
            {
                credential = Helpers.DefaultForType<CLCredential>();
                return ex;
            }

            status = CLCredentialCreationStatus.Success;
            return null;
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private CLCredential(
            string key,
            string secret,
            string token, 
            ref CLCredentialCreationStatus status)
        {
            // check input parameters

            if (string.IsNullOrEmpty(key))
            {
                status = CLCredentialCreationStatus.ErrorNullKey;
                throw new NullReferenceException(Resources.CLCredentialKeyCannotBeNull);
            }
            if (string.IsNullOrEmpty(secret))
            {
                status = CLCredentialCreationStatus.ErrorNullSecret;
                throw new NullReferenceException(Resources.CLCredentialSecretCannotBeNull);
            }

            // Since we allow null then reverse-null coalesce from empty string
            if (token == string.Empty)
            {
                token = null;
            }

            this._key = key;
            this._secret = secret;
            
            this._token = token;
        }

        /// <summary>
        /// Determine whether the credential was instantiated with a temporary token.
        /// </summary>
        /// <returns>bool: true: The token exists.</returns>
        public bool CredentialHasToken()
        {
            return !String.IsNullOrEmpty(_token);
        }

        #region public authorization HTTP API calls
        #region default settings for CLCredential HTTP calls
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

        #region AddSyncboxOnServer
        //// The following method is not useful since AddSyncboxOnServer does not yet support initial metadata, and this was simply an overload which accepted metadata in a different format
        //
        ///// <summary>
        ///// Asynchronously starts adding a Sync box on the server for the current application
        ///// </summary>
        ///// <param name="aCallback">Callback method to fire when operation completes</param>
        ///// <param name="aState">Userstate to pass when firing async callback</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        ///// <param name="friendlyName">(optional) friendly name of the Sync box</param>
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //public IAsyncResult BeginAddSyncboxOnServer<T>(AsyncCallback aCallback,
        //    object aState,
        //    int timeoutMilliseconds,
        //    ICLCredentialSettings settings = null,
        //    string friendlyName = null,
        //    IDictionary<string, T> metadata = null)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<AddSyncboxOnServerResult> toReturn = new GenericAsyncResult<AddSyncboxOnServerResult>(
        //        aCallback,
        //        aState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>> asyncParams =
        //        new Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>>(
        //            toReturn,
        //            timeoutMilliseconds,
        //            settings,
        //            friendlyName,
        //            metadata);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>> castState = state as Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>>;
        //        // if the try cast failed, then show a message box for this unrecoverable error
        //        if (castState == null)
        //        {
        //            MessageEvents.FireNewEventMessage(
        //                "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
        //                EventMessageLevel.Important,
        //                new HaltAllOfCloudSDKErrorInfo());
        //        }
        //        // else if the try cast did not fail, then start processing with the input parameters
        //        else
        //        {
        //            // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
        //            try
        //            {
        //                // declare the output status for communication
        //                CLHttpRestStatus status;
        //                // declare the specific type of result for this operation
        //                JsonContracts.CreateSyncbox result;
        //                // run the download of the file with the passed parameters, storing any error that occurs
        //                CLError processError = AddSyncboxOnServer(
        //                    castState.Item2,
        //                    out status,
        //                    out result,
        //                    castState.Item3,
        //                    castState.Item4,
        //                    castState.Item5);

        //                // if there was an asynchronous result in the parameters, then complete it with a new result object
        //                if (castState.Item1 != null)
        //                {
        //                    castState.Item1.Complete(
        //                        new AddSyncboxOnServerResult(
        //                            processError, // any error that may have occurred during processing
        //                            status, // the output status of communication
        //                            result), // the specific type of result for this operation
        //                            sCompleted: false); // processing did not complete synchronously
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // if there was an asynchronous result in the parameters, then pass through the exception to it
        //                if (castState.Item1 != null)
        //                {
        //                    castState.Item1.HandleException(
        //                        ex, // the exception which was not handled correctly by the CLError wrapping
        //                        sCompleted: false); // processing did not complete synchronously
        //                }
        //            }
        //        }
        //    }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

        //    // return the asynchronous result
        //    return toReturn;
        //}

        /// <summary>
        /// Asynchronously starts adding a Sync box on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="friendlyName">(optional) friendly name of the Sync box</param>
        /// <param name="planId">(optional) The ID of the plan to use with this Syncbox</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginAddSyncboxOnServer(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            string friendlyName = null,
            Nullable<long> planId = null,
            ICLCredentialSettings settings = null/*,  \/ last parameter temporarily removed since the server is not checking for it for this call; add back wherever commented out within this method when it works
            JsonContracts.MetadataDictionary metadata = null*/) 
        {
            // create the asynchronous result to return
            GenericAsyncResult<AddSyncboxOnServerResult> toReturn = new GenericAsyncResult<AddSyncboxOnServerResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    timeoutMilliseconds,
                    friendlyName,
                    planId,
                    settings/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/> castState =
                    state as Tuple<GenericAsyncResult<AddSyncboxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                        JsonContracts.SyncboxHolder result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = AddSyncboxOnServer(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3,
                            castState.Item4,
                            castState.Item5/*,
                            castState.Item6*/);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new AddSyncboxOnServerResult(
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
        /// Finishes adding a Sync box on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting adding the sync box</param>
        /// <param name="result">(output) The result from adding the sync box</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddSyncboxOnServer(IAsyncResult aResult, out AddSyncboxOnServerResult result)
        {
            // declare the specific type of asynchronous result for sync box add
            GenericAsyncResult<AddSyncboxOnServerResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for adding sync boxes and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for adding sync boxes
                castAResult = aResult as GenericAsyncResult<AddSyncboxOnServerResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<AddSyncboxOnServerResult>();
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

        //// The following method is not useful since AddSyncboxOnServer does not yet support initial metadata, and this was simply an overload which accepted metadata in a different format
        //
        ///// <summary>
        ///// Add a Sync box on the server for the current application
        ///// </summary>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="status">(output) success/failure status of communication</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        ///// <param name="friendlyName">(optional) friendly name of the Sync box</param>
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //public CLError AddSyncboxOnServer<T>(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response, ICLCredentialSettings settings = null, string friendlyName = null, IDictionary<string, T> metadata = null)
        //{
        //    try
        //    {
        //        return AddSyncboxOnServer(timeoutMilliseconds, out status, out response, settings, friendlyName,
        //            (metadata == null
        //                ? null
        //                : new JsonContracts.MetadataDictionary(
        //                    ((metadata is IDictionary<string, object>)
        //                        ? (IDictionary<string, object>)metadata
        //                        : new JsonContracts.MetadataDictionary.DictionaryWrapper<T>(metadata)))));
        //    }
        //    catch (Exception ex)
        //    {
        //        status = CLHttpRestStatus.BadRequest;
        //        response = Helpers.DefaultForType<JsonContracts.SyncboxHolder>();
        //        return ex;
        //    }
        //}

        /// <summary>
        /// Add a Sync box on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="friendlyName">(optional) friendly name of the Sync box</param>
        /// <param name="planId">(optional) the ID of the plan to use with this Syncbox</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddSyncboxOnServer(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncboxHolder response, 
                    string friendlyName = null,
                    Nullable<long> planId = null,
                    ICLCredentialSettings settings = null/*, JsonContracts.MetadataDictionary metadata = null*/)
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                JsonContracts.SyncboxHolder inputBox = (/*metadata == null
                        && */string.IsNullOrEmpty(friendlyName)
                    ? null
                    : new JsonContracts.SyncboxHolder()
                    {
                        Syncbox = new JsonContracts.Syncbox()
                        {
                            FriendlyName = (string.IsNullOrEmpty(friendlyName)
                                ? null
                                : friendlyName),
                            PlanId = planId/*,
                            Metadata = metadata*/
                        }
                    });

                response = Helpers.ProcessHttp<JsonContracts.SyncboxHolder>(
                    requestContent: inputBox,
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthCreateSyncbox,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: timeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    status: ref status,
                    CopiedSettings: copiedSettings,
                    Credential: this,
                    SyncboxId: null);

            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxHolder>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ListSyncboxes
        /// <summary>
        /// Asynchronously starts listing the Sync boxes on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginListSyncboxes(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<ListSyncboxesResult> toReturn = new GenericAsyncResult<ListSyncboxesResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<ListSyncboxesResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<ListSyncboxesResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<ListSyncboxesResult>, int, ICLCredentialSettings> castState = state as Tuple<GenericAsyncResult<ListSyncboxesResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                        JsonContracts.ListSyncboxes result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = ListSyncboxes(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new ListSyncboxesResult(
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
        /// Finishes listing Sync boxes on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sync boxes</param>
        /// <param name="result">(output) The result from listing the sync boxes</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndListSyncboxes(IAsyncResult aResult, out ListSyncboxesResult result)
        {
            // declare the specific type of asynchronous result for sync boxes listing
            GenericAsyncResult<ListSyncboxesResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for sync boxes listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sync boxes
                castAResult = aResult as GenericAsyncResult<ListSyncboxesResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<ListSyncboxesResult>();
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
        /// Lists the Sync boxes on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ListSyncboxes(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.ListSyncboxes response, ICLCredentialSettings settings = null)
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                response = Helpers.ProcessHttp<JsonContracts.ListSyncboxes>(
                    requestContent: null, // no request body for listing sync boxes
                    serverUrl: CLDefinitions.CLPlatformAuthServerURL,
                    serverMethodPath: CLDefinitions.MethodPathAuthListSyncboxes,
                    method: Helpers.requestMethod.post,
                    timeoutMilliseconds: timeoutMilliseconds,
                    uploadDownload: null, // not an upload nor download
                    validStatusCodes: Helpers.HttpStatusesOkAccepted,
                    status: ref status,
                    CopiedSettings: copiedSettings,
                    Credential: this,
                    SyncboxId: null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.ListSyncboxes>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ListPlans
        /// <summary>
        /// Asynchronously starts listing the plans on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginListPlans(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<ListPlansResult> toReturn = new GenericAsyncResult<ListPlansResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<ListPlansResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<ListPlansResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<ListPlansResult>, int, ICLCredentialSettings> castState = state as Tuple<GenericAsyncResult<ListPlansResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                        JsonContracts.ListPlansResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = ListPlans(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new ListPlansResult(
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
        /// Finishes listing plans on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the plans</param>
        /// <param name="result">(output) The result from listing the plans</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndListPlans(IAsyncResult aResult, out ListPlansResult result)
        {
            // declare the specific type of asynchronous result for plan listing
            GenericAsyncResult<ListPlansResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for plan listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing plans
                castAResult = aResult as GenericAsyncResult<ListPlansResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<ListPlansResult>();
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
        /// Lists the plans on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ListPlans(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.ListPlansResponse response, ICLCredentialSettings settings = null)
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                response = Helpers.ProcessHttp<JsonContracts.ListPlansResponse>(
                    null, // no request body for listing plans
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListPlans,
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
                response = Helpers.DefaultForType<JsonContracts.ListPlansResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ListSessions
        /// <summary>
        /// Asynchronously starts listing the sessions on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginListSessions(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<ListSessionsResult> toReturn = new GenericAsyncResult<ListSessionsResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<ListSessionsResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<ListSessionsResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<ListSessionsResult>, int, ICLCredentialSettings> castState = state as Tuple<GenericAsyncResult<ListSessionsResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                        JsonContracts.ListSessionsResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = ListSessions(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new ListSessionsResult(
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
        /// Finishes listing sessions on the server for the current application if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting listing the sessions</param>
        /// <param name="result">(output) The result from listing the sessions</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndListSessions(IAsyncResult aResult, out ListSessionsResult result)
        {
            // declare the specific type of asynchronous result for session listing
            GenericAsyncResult<ListSessionsResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for session listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sessions
                castAResult = aResult as GenericAsyncResult<ListSessionsResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<ListSessionsResult>();
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
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError ListSessions(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.ListSessionsResponse response, ICLCredentialSettings settings = null)
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                response = Helpers.ProcessHttp<JsonContracts.ListSessionsResponse>(
                    /* requestContent */ null, // no request body for listing sessions
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListSessions,
                    Helpers.requestMethod.post,
                    timeoutMilliseconds,
                    /* uploadDownload */ null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    Credential: this,
                    SyncboxId: null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.ListSessionsResponse>();
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
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SessionCreateResult> toReturn = new GenericAsyncResult<SessionCreateResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    syncboxIds,
                    tokenDurationMinutes,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialSettings> castState =
                            state as Tuple<GenericAsyncResult<SessionCreateResult>, int, HashSet<long>, Nullable<long>, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
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
                    ICLCredentialSettings settings = null)
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
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
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SessionShowResult> toReturn = new GenericAsyncResult<SessionShowResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    key,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialSettings> castState =
                            state as Tuple<GenericAsyncResult<SessionShowResult>, int, string, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
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
                    ICLCredentialSettings settings = null
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
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
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SessionDeleteResult> toReturn = new GenericAsyncResult<SessionDeleteResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    key,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialSettings> castState =
                            state as Tuple<GenericAsyncResult<SessionDeleteResult>, int, string, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
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
                    ICLCredentialSettings settings = null
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
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
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
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<LinkDeviceFirstTimeResult> toReturn = new GenericAsyncResult<LinkDeviceFirstTimeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialSettings> castState = 
                            state as Tuple<GenericAsyncResult<LinkDeviceFirstTimeResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
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
                    ICLCredentialSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new ArgumentException(Resources.CLCredentialPushRequestCannotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // Set the Key and Secret from this credential.
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
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<LinkDeviceResult> toReturn = new GenericAsyncResult<LinkDeviceResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialSettings> castState =
                            state as Tuple<GenericAsyncResult<LinkDeviceResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
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
                    ICLCredentialSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new ArgumentException(Resources.CLCredentialPushRequestCannotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // Set the Key and Secret from this credential.
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
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<UnlinkDeviceResult> toReturn = new GenericAsyncResult<UnlinkDeviceResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialSettings> castState =
                            state as Tuple<GenericAsyncResult<UnlinkDeviceResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCredentialCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                    throw new NullReferenceException(Resources.CLCredentialaResultInternalTypeMismatch);
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
                    ICLCredentialSettings settings = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (request == null)
                {
                    throw new ArgumentException(Resources.CLCredentialPushRequestCannotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
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
    /// Status of creation of <see cref="CLCredential"/>
    /// </summary>
    public enum CLCredentialCreationStatus : byte
    {
        Success = 0,
        ErrorNullKey = 1,
        ErrorNullSecret = 2,
        ErrorUnknown = 3,
    }
}