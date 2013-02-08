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
        //public IAsyncResult BeginAddSyncBoxOnServer(AsyncCallback aCallback,
        //    object aState,
        //    int timeoutMilliseconds,
        //    ICLCredentialSettings settings = null,
        //    string friendlyName = null,
        //    IDictionary<string, object> metadata = null)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<AddSyncBoxOnServerResult> toReturn = new GenericAsyncResult<AddSyncBoxOnServerResult>(
        //        aCallback,
        //        aState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, object>> asyncParams =
        //        new Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, object>>(
        //            toReturn,
        //            timeoutMilliseconds,
        //            settings,
        //            friendlyName,
        //            metadata);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, object>> castState = state as Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string, IDictionary<string, object>>;
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
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <param name="friendlyName">(optional) friendly name of the Sync box</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public IAsyncResult BeginAddSyncBoxOnServer(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            ICLCredentialSettings settings = null,
            string friendlyName = null/*,  \/ last parameter temporarily removed since the server is not checking for it for this call; add back wherever commented out within this method when it works
            JsonContracts.MetadataDictionary metadata = null*/) 
        {
            // create the asynchronous result to return
            GenericAsyncResult<AddSyncBoxOnServerResult> toReturn = new GenericAsyncResult<AddSyncBoxOnServerResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string/*, JsonContracts.MetadataDictionary*/> asyncParams =
                new Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string/*, JsonContracts.MetadataDictionary*/>(
                    toReturn,
                    timeoutMilliseconds,
                    settings,
                    friendlyName/*,
                    metadata*/);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string/*, JsonContracts.MetadataDictionary*/> castState = state as Tuple<GenericAsyncResult<AddSyncBoxOnServerResult>, int, ICLCredentialSettings, string/*, JsonContracts.MetadataDictionary*/>;
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
                        JsonContracts.CreateSyncBox result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = AddSyncBoxOnServer(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3,
                            castState.Item4/*,
                            castState.Item5*/);

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
        //public CLError AddSyncBoxOnServer(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.CreateSyncBox response, ICLCredentialSettings settings = null, string friendlyName = null, IDictionary<string, object> metadata = null)
        //{
        //    return AddSyncBoxOnServer(timeoutMilliseconds, out status, out response, settings, friendlyName, (metadata == null ? null : new JsonContracts.MetadataDictionary(metadata)));
        //}

        /// <summary>
        /// Add a Sync box on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="settings">(optional) settings for optional tracing and specifying the client version to the server</param>
        /// <param name="friendlyName">(optional) friendly name of the Sync box</param>
        //
        //// The following metadata parameter was temporarily removed until the server checks for it for this call
        //
        ///// <param name="metadata">(optional) string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddSyncBoxOnServer(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.CreateSyncBox response, ICLCredentialSettings settings = null, string friendlyName = null/*, JsonContracts.MetadataDictionary metadata = null*/)
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

                JsonContracts.CreateSyncBox inputBox = (/*metadata == null
                        && */string.IsNullOrEmpty(friendlyName)
                    ? null
                    : new JsonContracts.CreateSyncBox()
                    {
                        SyncBox = new JsonContracts.SyncBox()
                        {
                            FriendlyName = (string.IsNullOrEmpty(friendlyName)
                                ? null
                                : friendlyName)/*,
                            Metadata = metadata*/
                        }
                    });

                response = Helpers.ProcessHttp<JsonContracts.CreateSyncBox>(
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
                response = Helpers.DefaultForType<JsonContracts.CreateSyncBox>();
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