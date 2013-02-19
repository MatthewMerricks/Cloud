// 
// CLCredential.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.REST;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace CloudApiPublic
{
    /// <summary>
    /// Contains authentication information required for all communication and services
    /// 
    /// The CLCredential class declares the interface used for authentication and authorization to Cloud.com <http://Cloud.com>.
    ///
    /// The CLCredential class allows the developer to represent both the Application’s credential as well as temporary session credential. The Application’s credential provides access to all of your Application’s SyncBoxes. Using a temporary credential, access can be limited to an individual SyncBox.
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
        /// <param name="Key">The public key that identifies this application.</param>
        /// <param name="Secret">The application secret private key.</param>
        /// <param name="credential">(output) Created credential object</param>
        /// <param name="status">(output) Status of creation, check this for Success</param>
        /// <param name="Token">(optional) The temporary token to use.  Default: null.</param>
        /// <returns>Returns any error that occurred in construction, if any, or null.</returns>
        public static CLError CreateAndInitialize(
            string Key,
            string Secret,
            out CLCredential credential,
            out CLCredentialCreationStatus status,
            string Token = null)
        {
            status = CLCredentialCreationStatus.ErrorUnknown;

            try
            {
                credential = new CLCredential(
                    Key,
                    Secret,
                    Token,
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
            string Key,
            string Secret,
            string Token, 
            ref CLCredentialCreationStatus status)
        {
            // check input parameters

            if (string.IsNullOrEmpty(Key))
            {
                status = CLCredentialCreationStatus.ErrorNullKey;
                throw new NullReferenceException("Key cannot be null");
            }
            if (string.IsNullOrEmpty(Secret))
            {
                status = CLCredentialCreationStatus.ErrorNullSecret;
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

            public static readonly NullSyncRoot Instance = new NullSyncRoot();

            private NullSyncRoot() { }
        }
        #endregion

        #region AddSyncBoxOnServer
        //// The following method is not useful since AddSyncBoxOnServer does not yet support initial metadata, and this was simply an overload which accepted metadata in a different format
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
        //public IAsyncResult BeginAddSyncBoxOnServer<T>(AsyncCallback aCallback,
        //    object aState,
        //    int timeoutMilliseconds,
        //    ICLCredentialSettings settings = null,
        //    string friendlyName = null,
        //    IDictionary<string, T> metadata = null)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<AddSyncBoxOnServerResult> toReturn = new GenericAsyncResult<AddSyncBoxOnServerResult>(
        //        aCallback,
        //        aState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>> asyncParams =
        //        new Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>>(
        //            toReturn,
        //            timeoutMilliseconds,
        //            settings,
        //            friendlyName,
        //            metadata);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>> castState = state as Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, T>>;
        //        // if the try cast failed, then show a message box for this unrecoverable error
        //        if (castState == null)
        //        {
        //            MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
        //                JsonContracts.CreateSyncBox result;
        //                // run the download of the file with the passed parameters, storing any error that occurs
        //                CLError processError = AddSyncBoxOnServer(
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
        //                        new AddSyncBoxOnServerResult(
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
        /// <param name="planId">(optional) The ID of the plan to use with this SyncBox</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginAddSyncBoxOnServer(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            string friendlyName = null,
            Nullable<long> planId = null,
            ICLCredentialSettings settings = null/*,  \/ last parameter temporarily removed since the server is not checking for it for this call; add back wherever commented out within this method when it works
            JsonContracts.MetadataDictionary metadata = null*/) 
        {
            // create the asynchronous result to return
            GenericAsyncResult<AddSyncBoxOnServerResult> toReturn = new GenericAsyncResult<AddSyncBoxOnServerResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/>(
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
                Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/> castState =
                    state as Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, string, Nullable<long>, ICLCredentialSettings/*, JsonContracts.MetadataDictionary*/>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                        JsonContracts.SyncBoxHolder result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = AddSyncBoxOnServer(
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
                                new AddSyncBoxOnServerResult(
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
        public CLError EndAddSyncBoxOnServer(IAsyncResult aResult, out AddSyncBoxOnServerResult result)
        {
            // declare the specific type of asynchronous result for sync box add
            GenericAsyncResult<AddSyncBoxOnServerResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for adding sync boxes and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for adding sync boxes
                castAResult = aResult as GenericAsyncResult<AddSyncBoxOnServerResult>;

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
                result = Helpers.DefaultForType<AddSyncBoxOnServerResult>();
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

        //// The following method is not useful since AddSyncBoxOnServer does not yet support initial metadata, and this was simply an overload which accepted metadata in a different format
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
        //public CLError AddSyncBoxOnServer<T>(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response, ICLCredentialSettings settings = null, string friendlyName = null, IDictionary<string, T> metadata = null)
        //{
        //    try
        //    {
        //        return AddSyncBoxOnServer(timeoutMilliseconds, out status, out response, settings, friendlyName,
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
        //        response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
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
        /// <param name="planId">(optional) the ID of the plan to use with this SyncBox</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddSyncBoxOnServer(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response, 
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
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                JsonContracts.SyncBoxHolder inputBox = (/*metadata == null
                        && */string.IsNullOrEmpty(friendlyName)
                    ? null
                    : new JsonContracts.SyncBoxHolder()
                    {
                        SyncBox = new JsonContracts.SyncBox()
                        {
                            FriendlyName = (string.IsNullOrEmpty(friendlyName)
                                ? null
                                : friendlyName),
                            PlanId = planId/*,
                            Metadata = metadata*/
                        }
                    });

                response = Helpers.ProcessHttp<JsonContracts.SyncBoxHolder>(
                    inputBox,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthCreateSyncBox,
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
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return ex;
            }
            return null;
        }
        #endregion

        #region ListSyncBoxes
        /// <summary>
        /// Asynchronously starts listing the Sync boxes on the server for the current application
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginListSyncBoxes(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            ICLCredentialSettings settings = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<ListSyncBoxesResult> toReturn = new GenericAsyncResult<ListSyncBoxesResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<ListSyncBoxesResult>, int, ICLCredentialSettings> asyncParams =
                new Tuple<GenericAsyncResult<ListSyncBoxesResult>, int, ICLCredentialSettings>(
                    toReturn,
                    timeoutMilliseconds,
                    settings);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<ListSyncBoxesResult>, int, ICLCredentialSettings> castState = state as Tuple<GenericAsyncResult<ListSyncBoxesResult>, int, ICLCredentialSettings>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                        JsonContracts.ListSyncBoxes result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = ListSyncBoxes(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new ListSyncBoxesResult(
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
        public CLError EndListSyncBoxes(IAsyncResult aResult, out ListSyncBoxesResult result)
        {
            // declare the specific type of asynchronous result for sync boxes listing
            GenericAsyncResult<ListSyncBoxesResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for sync boxes listing and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for listing sync boxes
                castAResult = aResult as GenericAsyncResult<ListSyncBoxesResult>;

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
                result = Helpers.DefaultForType<ListSyncBoxesResult>();
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
        public CLError ListSyncBoxes(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.ListSyncBoxes response, ICLCredentialSettings settings = null)
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

                response = Helpers.ProcessHttp<JsonContracts.ListSyncBoxes>(
                    null, // no request body for listing sync boxes
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListSyncBoxes,
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
                response = Helpers.DefaultForType<JsonContracts.ListSyncBoxes>();
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
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
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
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.ListSessionsResponse>(
                    null, // no request body for listing sessions
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthListSessions,
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
        /// <param name="syncBoxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 36 hours.  Maximum: 120 hours.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginCreateSession(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            HashSet<long> syncBoxIds = null,
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
                    syncBoxIds,
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
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
        /// <param name="syncBoxIds">(optional) IDs of sync boxes to associate with this session.  A null value causes all syncboxes defined for the application to be associated with this session.</param>
        /// <param name="tokenDurationMinutes">(optional) The number of minutes before the token expires. Default: 36 hours.  Maximum: 120 hours.</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CreateSession(int timeoutMilliseconds, out CLHttpRestStatus status, 
                    out JsonContracts.SessionCreateResponse response, 
                    HashSet<long> syncBoxIds = null,
                    Nullable<long> tokenDurationMinutes = null,
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
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Determine the request JSON contract to use.  If the syncBoxIds parameter is null, use the "all"
                // contract.  Otherwise, build the contract that includes an array of SyncBoxIds.
                object requestContract = null;
                if (syncBoxIds == null)
                {
                    CloudApiPublic.JsonContracts.SessionCreateAllRequest sessionCreateAll = new JsonContracts.SessionCreateAllRequest()
                    {
                        SessionIds = CLDefinitions.RESTRequestSession_SyncBoxIdsAll,
                        TokenDuration = tokenDurationMinutes
                    };
                    requestContract = sessionCreateAll;
                }
                else
                {
                    CloudApiPublic.JsonContracts.SessionCreateRequest sessionCreate = new JsonContracts.SessionCreateRequest()
                    {
                        SessionIds = syncBoxIds.ToArray<long>(),
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
                    Helpers.HttpStatusesOkAccepted,
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
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    new[]
                    {
                        new KeyValuePair<string, string>(CLDefinitions.RESTRequestSession_KeyId, Uri.EscapeDataString(key))
                    });

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
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                CloudApiPublic.JsonContracts.SessionDeleteRequest sessionDeleteRequest = new JsonContracts.SessionDeleteRequest()
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

        #endregion
    }
    /// <summary>
    /// Status of creation of <see cref="CLCredential"/>
    /// </summary>
    public enum CLCredentialCreationStatus : byte
    {
        Success,
        ErrorNullKey,
        ErrorNullSecret,
        ErrorUnknown
    }
}