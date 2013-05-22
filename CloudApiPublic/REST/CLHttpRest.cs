//
// CLHttpRest.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Sync;
using System.IO;
using System.Threading;
using System.Net;
using Cloud.Model;
using Cloud.JsonContracts;
using Cloud.Static;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using Cloud.Support;
using System.Linq.Expressions;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.CLSync;
using Cloud.CLSync.CLSyncboxParameters;

namespace Cloud.REST
{
    // CLCredentials class has additional HTTP calls which do not require a Syncbox id
    /// <summary>
    /// Client for manual HTTP communication calls to the Cloud
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class CLHttpRest
    {
        #region Private fields

        private readonly Dictionary<int, EnumRequestNewCredentialsStates> _processingStateByThreadId;
        private Helpers.ReplaceExpiredCredentials _getNewCredentialsCallback = null;
        private object _getNewCredentialsCallbackUserState = null;

        #endregion

        #region Private helper functions

        private CLCredentials GetCurrentCredentialsCallback()
        {
            return _syncbox.Credentials;
        }

        private void SetCurrentCredentialCallback(CLCredentials credentials)
        {
            _syncbox.Credentials = credentials;
        }

        private void CheckPath(FilePath pathToCheck, CLExceptionCode codeOnError)
        {
            if (pathToCheck == null)
            {
                throw new CLArgumentNullException(codeOnError, Resources.ExceptionOnDemandCheckPathNull);
            }

            CLError pathError = Helpers.CheckForBadPath(pathToCheck);
            if (pathError != null)
            {
                throw new CLArgumentException(codeOnError, Resources.ExceptionOnDemandCheckPathBad, pathError.Exceptions);
            }

            if (string.IsNullOrEmpty(_syncbox.Path))
            {
                throw new CLArgumentNullException(codeOnError, Resources.ExceptionOnDemandCheckPathSyncboxPathNull);
            }

            if (!pathToCheck.Contains(_syncbox.Path))
            {
                throw new CLArgumentException(codeOnError, Resources.ExceptionOnDemandCheckPathNotContained);
            }
        }

        #endregion

        #region Internal Reference Counters for Knowing When an API call is modifying the syncbox

        public bool IsModifyingSyncboxViaPublicAPICalls
        {
            get
            {
                lock (_isModifyingSyncboxViaPublicAPICalls)
                {
                    return _isModifyingSyncboxViaPublicAPICalls.Value > 0;
                }
            }
        }
        private void IncrementModifyingSyncboxViaPublicAPICalls()
        {
            lock (_isModifyingSyncboxViaPublicAPICalls)
            {
                _isModifyingSyncboxViaPublicAPICalls.Value = _isModifyingSyncboxViaPublicAPICalls.Value + 1;
            }
        }
        private void DecrementModifyingSyncboxViaPublicAPICalls()
        {
            lock (_isModifyingSyncboxViaPublicAPICalls)
            {
                _isModifyingSyncboxViaPublicAPICalls.Value = _isModifyingSyncboxViaPublicAPICalls.Value - 1;
            }
        }
        private readonly GenericHolder<int> _isModifyingSyncboxViaPublicAPICalls = new GenericHolder<int>(0);

        #endregion  // end Internal Reference Counters for Knowing When an API call is modifying the syncbox

        #region construct with settings so they do not always need to be passed in

        // storage of settings, which should be a copy of settings passed in on construction so they do not change throughout communication
        private readonly ICLSyncSettingsAdvanced _copiedSettings;

        // Syncbox associated with this CLHttpRest object.
        private CLSyncbox _syncbox;

        #endregion

        #region Constructors and Factories

        // private constructor requiring settings to copy and store for the life of this http client
        private CLHttpRest(CLCredentials credentials, CLSyncbox syncbox, ICLSyncSettings settings,
                                Helpers.ReplaceExpiredCredentials getNewCredentialsCallback,
                                object getNewCredentialsCallbackUserState)
        {
            if (syncbox == null)
            {
                throw new NullReferenceException(Resources.ExceptionCLHttpRestNullSyncbox);
            }

            if (string.IsNullOrEmpty(syncbox.Path))
            {
                throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
            }

            if (syncbox.Credentials == null)
            {
                throw new NullReferenceException(Resources.CLHttpRestsyncboxCredentialCannotBeNull);
            }

            this._syncbox = syncbox;
            if (settings == null)
            {
                this._copiedSettings = AdvancedSyncSettings.CreateDefaultSettings();
            }
            else
            {
                this._copiedSettings = settings.CopySettings();
            }

            if (!string.IsNullOrEmpty(this._syncbox.Path))
            {
                CLError syncRootError = Helpers.CheckForBadPath(this._syncbox.Path);
                if (syncRootError != null)
                {
                    throw new AggregateException(Resources.CLHttpRestSyncboxBadPath, syncRootError.Exceptions);
                }
            }

            if (string.IsNullOrEmpty(this._copiedSettings.DeviceId))
            {
                throw new NullReferenceException(Resources.CLHttpRestDeviceIDCannotBeNull);
            }

            _getNewCredentialsCallback = getNewCredentialsCallback;
            _getNewCredentialsCallbackUserState = getNewCredentialsCallbackUserState;
            _processingStateByThreadId = (getNewCredentialsCallback == null
                ? null
                : new Dictionary<int, EnumRequestNewCredentialsStates>());
        }

        /// <summary>
        /// Creates a CLHttpRest client object for HTTP REST calls to the server
        /// </summary>
        /// <param name="credentials">Contains authentication information required for communication</param>
        /// <param name="syncboxId">ID of sync box which can be manually synced</param>
        /// <param name="client">(output) Created CLHttpRest client</param>
        /// <param name="settings">(optional) Additional settings to override some defaulted parameters</param>
        /// <returns>Returns any error creating the CLHttpRest client, if any</returns>
        internal static CLError CreateAndInitialize(CLCredentials credentials, CLSyncbox syncbox, out CLHttpRest client, 
                    ICLSyncSettings settings = null,
                    Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
                    object getNewCredentialsCallbackUserState = null)
        {
            try
            {
                client = new CLHttpRest(credentials, syncbox, settings, getNewCredentialsCallback, getNewCredentialsCallbackUserState);
            }
            catch (Exception ex)
            {
                client = Helpers.DefaultForType<CLHttpRest>();
                return ex;
            }
            return null;
        }

        #endregion  // end Constructors and Factories

        #region public API calls
        #region GetItemAtPath (Gets the metedata at a particular server syncbox path)
        /// <summary>
        /// Asynchronously starts querying the server at a given path for existing metadata at that path.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">Full path to where file or folder would exist locally on disk</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetItemAtPath(AsyncCallback callback, object callbackUserState, string path)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetItemAtPathResult>(
                        callback,
                        callbackUserState),
                    path
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetItemAtPath(
                            Data.path,
                            out response, 
                            true);

                        Data.toReturn.Complete(
                            new SyncboxGetItemAtPathResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes getting the item at the input path, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetItemAtPath(IAsyncResult aResult, out SyncboxGetItemAtPathResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetItemAtPathResult>(aResult, out result);
        }

        /// <summary>
        /// Query the server at a given path for existing metadata at that path.
        /// </summary>
        /// <param name="path">Full path to where file or folder would exist locally on disk</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetItemAtPath(string path, out CLFileItem response, bool isOneOff)
        {
            // try/catch to process the request. On catch return the error
            try
            {
                // check input parameters

                if (path == null)
                {
                    throw new NullReferenceException("path must not be null");
                }

                CLError pathError = Helpers.CheckForBadPath(path);
                if (pathError != null)
                {
                    throw new AggregateException("path is not in the proper format", pathError.Exceptions);
                }

                if (string.IsNullOrEmpty(_syncbox.Path))
                {
                    throw new NullReferenceException("the syncbox path cannot be null");
                }

                if (!path.Contains(_syncbox.Path))
                {
                    throw new ArgumentException("path does not contain the syncbox path");
                }
                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the metadata retrieval method on the server dynamically
                FilePath filePath = new FilePath(path);
                string serverMethodPath = CLDefinitions.MethodPathGetItemMetadata + 
                    Helpers.QueryStringBuilder(new[] // the method grabs its parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the path to query, built by turning the full path location into a relative path from the cloud root and then escaping the whole thing for a url
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(filePath.GetRelativePath((_syncbox.Path ?? string.Empty), true))),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMetadataResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMetadataResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    Helpers.requestMethod.get, // query metadata is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    isOneOff);

                // Convert the response to the expected output object.
                if (responseFromServer != null)
                {
                    response = new CLFileItem(responseFromServer,  _syncbox);
                }
                else
                {
                    throw new NullReferenceException("response from server was null");
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetItemAtPath (Gets the metedata at a particular server syncbox path)

        #region RenameFiles (Rename files in the cloud.)
        /// <summary>
        /// Asynchronously starts renaming files in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="itemParams">An array of parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <param name="completionCallback">Delegate which will be fired upon successful communication for every response item</param>
        /// <param name="completionCallbackUserState">Userstate to be passed whenever the completion delegate is fired</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginRenameFiles(AsyncCallback callback, object callbackUserState, RenameItemParams[] itemParams, CLFileItemCompletion completionCallback, object completionCallbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        callback,
                        callbackUserState),
                    itemParams = itemParams
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = RenameFiles(
                            Data.itemParams,
                            completionCallback,
                            completionCallbackUserState);

                        Data.toReturn.Complete(overallError, // any overall error that may have occurred during processing
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
        /// Finishes renaming files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndRenameFiles(IAsyncResult aResult, out SyncboxRenameFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxRenameFilesResult>(aResult, out result);
        }

        /// <summary>
        /// Rename files in the cloud.
        /// </summary>
        /// <param name="itemParams">An array of parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <param name="completionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="completionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError RenameFiles(RenameItemParams[] itemParams, CLFileItemCompletion completionCallback, object completionCallbackUserState)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.

                if (itemParams == null
                    || itemParams.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[itemParams.Length];
                FilePath syncboxPathObject = _syncbox.Path;

                for (int paramIdx = 0; paramIdx < itemParams.Length; paramIdx++)
                {
                    RenameItemParams currentParams = itemParams[paramIdx];

                    // The CLFileItem represents an existing file or folder, and should be valid because we created it.  The new full path must
                    // fit the specs for the Windows client.  Form the new full path and check its validity.
                    if (String.IsNullOrWhiteSpace(currentParams.ItemToRename.Path))
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidExistingPath, String.Format(Resources.ExceptionOnDemandRenameFilesInvalidExistingPathInItemMsg0, paramIdx.ToString()));
                    }
                    FilePath fullPathExisting = new FilePath(currentParams.ItemToRename.Path, _syncbox.Path);
                    FilePath fullPathNew = new FilePath(currentParams.NewName, fullPathExisting.Parent);
                    CheckPath(fullPathNew, CLExceptionCode.OnDemand_RenameNewName);


                    // file move (rename) and folder move (rename) share a json contract object for move (rename)
                    jsonContractMoves[paramIdx] = new FileOrFolderMove()
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        RelativeToPath = fullPathNew.GetRelativePath(_syncbox.Path, true),
                        ServerUid = currentParams.ItemToRename.Uid,
                        SyncboxId = _syncbox.SyncboxId
                    };
                }

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Now make the REST request content.
                object requestContent = new JsonContracts.FileOrFolderMoves()
                {
                    SyncboxId = _syncbox.SyncboxId,
                    Moves = jsonContractMoves,
                    DeviceId = _copiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFileMoves;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.MoveResponses != null)
                {
                    if (responseFromServer.MoveResponses.Length != itemParams.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();

                    for (int responseIdx = 0; responseIdx < responseFromServer.MoveResponses.Length; responseIdx++)
                    {
                        try
                        {
                            FileChangeResponse currentMoveResponse = responseFromServer.MoveResponses[responseIdx];

                            if (currentMoveResponse == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                            }
                            if (currentMoveResponse.Header == null || string.IsNullOrEmpty(currentMoveResponse.Header.Status))
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                            }
                            if (currentMoveResponse.Metadata == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                            }

                            switch (currentMoveResponse.Header.Status)
                            {
                                case CLDefinitions.CLEventTypeNoOperation:
                                case CLDefinitions.CLEventTypeAccepted:
                                    CLFileItem resultItem = new CLFileItem(currentMoveResponse.Metadata, currentMoveResponse.Header.Action, currentMoveResponse.Action, _syncbox);
                                    if (completionCallback != null)
                                    {
                                        try
                                        {
                                            completionCallback(responseIdx, resultItem, error: null, userState: completionCallbackUserState);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    break;

                                case CLDefinitions.CLEventTypeAlreadyDeleted:
                                    throw new CLException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandAlreadyDeleted);

                                //// to_parent_uid not an input parameter, we do not expect to see it in the status so it is commented out:
                                //case CLDefinitions.CLEventTypeToParentNotFound:
                                case CLDefinitions.CLEventTypeNotFound:
                                    throw new CLException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandNotFound);

                                case CLDefinitions.CLEventTypeConflict:
                                    throw new CLException(CLExceptionCode.OnDemand_Conflict, Resources.ExceptionOnDemandConflict);

                                case CLDefinitions.RESTResponseStatusFailed:
                                    Exception innerEx;
                                    string errorMessageString;
                                    try
                                    {
                                        errorMessageString = string.Join(Environment.NewLine, currentMoveResponse.Metadata.ErrorMessage);
                                        innerEx = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                        innerEx = ex;
                                    }

                                    throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                default:
                                    throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentMoveResponse.Header.Status));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (completionCallback != null)
                            {
                                try
                                {
                                    completionCallback(responseIdx, completedItem: null, error: ex, userState: completionCallbackUserState);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionCLHttpRestWithoutMoveResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        ///// <summary>
        ///// Rename files in the cloud.
        ///// </summary>
        ///// <param name="pathParams">An array of old paths to new paths for renaming each item</param>
        ///// <param name="completion">Delegate which will be fired upon successful communication for every response item</param>
        ///// <param name="completionState">Userstate to be passed whenever the completion delegate is fired</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //internal CLError RenameFiles(RenamePathParams[] pathParams, CLFileItemCompletion completion, object completionState)
        //{
        //    // try/catch to process the request,  On catch return the error
        //    try
        //    {
        //        // check input parameters.

        //        if (pathParams == null
        //            || pathParams.Length == 0)
        //        {
        //            throw new CLArgumentNullException(
        //                CLExceptionCode.OnDemand_RenameMissingParameters,
        //                Resources.ExceptionOnDemandRenameMissingParameters);
        //        }

        //        FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[pathParams.Length];
        //        FilePath syncboxPathObject = _syncbox.Path;

        //        for (int paramIdx = 0; paramIdx < pathParams.Length; paramIdx++)
        //        {
        //            RenamePathParams currentParams = pathParams[paramIdx];

        //            FilePath currentOldPath = currentParams.OldPath;
        //            CheckPath(currentOldPath, CLExceptionCode.OnDemand_RenameOldPath);

        //            FilePath currentNewPath = currentParams.NewPath;
        //            CheckPath(currentNewPath, CLExceptionCode.OnDemand_RenameNewPath);

        //            jsonContractMoves[paramIdx] = new FileOrFolderMove()
        //            {
        //                RelativeFromPath = currentOldPath.GetRelativePath(syncboxPathObject, replaceWithForwardSlashes: true),
        //                RelativeToPath = currentNewPath.GetRelativePath(syncboxPathObject, replaceWithForwardSlashes: true)
        //            };
        //        }

        //        if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
        //        {
        //            throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
        //        }

        //        // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
        //        Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
        //        {
        //            ProcessingStateByThreadId = _processingStateByThreadId,
        //            GetNewCredentialsCallback = _getNewCredentialsCallback,
        //            GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
        //            GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
        //            SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
        //        };

        //        // Build the REST content dynamically.
        //        // File move (rename) and folder move (rename) share a json contract object for move (rename).
        //        // This will be an array of contracts.

        //        //// Old code:
        //        //
        //        //List<FileOrFolderMove> listMoveContract = new List<FileOrFolderMove>();
        //        //for (int i = 0; i < numberOfFiles; ++i)
        //        //{
        //        //    FilePath filePath = new FilePath(paths[i]);
        //        //    FilePath newFilePath = new FilePath(newPaths[i]);

        //        //    FileOrFolderMove thisMove = new FileOrFolderMove()
        //        //    {
        //        //        //DeviceId = _copiedSettings.DeviceId,
        //        //        RelativeFromPath = filePath.GetRelativePath(_syncbox.Path, true),
        //        //        RelativeToPath = newFilePath.GetRelativePath(_syncbox.Path, true),
        //        //        //SyncboxId = _syncbox.SyncboxId
        //        //    };

        //        //    listMoveContract.Add(thisMove);
        //        //}

        //        // Now make the REST request content.
        //        object requestContent = new JsonContracts.FileOrFolderMoves()
        //        {
        //            SyncboxId = _syncbox.SyncboxId,
        //            Moves = jsonContractMoves,
        //            //listMoveContract.ToArray(),
        //            DeviceId = _copiedSettings.DeviceId
        //        };

        //        // server method path switched on whether change is a folder or not
        //        string serverMethodPath = CLDefinitions.MethodPathOneOffFileMoves;

        //        // Communicate with the server to get the response.
        //        JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
        //        responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
        //            CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
        //            serverMethodPath, // dynamic path to appropriate one-off method
        //            Helpers.requestMethod.post, // one-off methods are all posts
        //            _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
        //            null, // not an upload or download
        //            Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
        //            _copiedSettings, // pass the copied settings
        //            _syncbox.SyncboxId, // pass the unique id of the sync box on the server
        //            requestNewCredentialsInfo);   // pass the optional parameters to support temporary token reallocation.

        //        // Convert these items to the output array.
        //        if (responseFromServer != null && responseFromServer.MoveResponses != null)
        //        {
        //            if (responseFromServer.MoveResponses.Length != pathParams.Length)
        //            {
        //                throw new CLException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionOnDemandResponseArrayLength);
        //            }

        //            List<CLFileItem> listFileItems = new List<CLFileItem>();
        //            List<CLError> listErrors = new List<CLError>();

        //            for (int responseIdx = 0; responseIdx < responseFromServer.MoveResponses.Length; responseIdx++)
        //            {
        //                try
        //                {
        //                    FileChangeResponse currentMoveResponse = responseFromServer.MoveResponses[responseIdx];

        //                    if (currentMoveResponse == null)
        //                    {
        //                        throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
        //                    }
        //                    if (currentMoveResponse.Header == null || string.IsNullOrEmpty(currentMoveResponse.Header.Status))
        //                    {
        //                        throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
        //                    }
        //                    if (currentMoveResponse.Metadata == null)
        //                    {
        //                        throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
        //                    }

        //                    switch (currentMoveResponse.Header.Status)
        //                    {
        //                        case CLDefinitions.CLEventTypeNoOperation:
        //                        case CLDefinitions.CLEventTypeAccepted:
        //                            CLFileItem resultItem = new CLFileItem(currentMoveResponse.Metadata, currentMoveResponse.Header.Action, currentMoveResponse.Action, _syncbox);
        //                            if (completion != null)
        //                            {
        //                                try
        //                                {
        //                                    completion(responseIdx, resultItem, error: null, userState: completionState);
        //                                }
        //                                catch
        //                                {
        //                                }
        //                            }
        //                            break;

        //                        case CLDefinitions.CLEventTypeAlreadyDeleted:
        //                            throw new CLException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandAlreadyDeleted);

        //                        //// to_parent_uid not an input parameter, we do not expect to see it in the status so it is commented out:
        //                        //case CLDefinitions.CLEventTypeToParentNotFound:
        //                        case CLDefinitions.CLEventTypeNotFound:
        //                            throw new CLException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandNotFound);

        //                        case CLDefinitions.CLEventTypeConflict:
        //                            throw new CLException(CLExceptionCode.OnDemand_Conflict, Resources.ExceptionOnDemandConflict);

        //                        case CLDefinitions.RESTResponseStatusFailed:
        //                            Exception innerEx;
        //                            string errorMessageString;
        //                            try
        //                            {
        //                                errorMessageString = string.Join(Environment.NewLine, currentMoveResponse.Metadata.ErrorMessage);
        //                                innerEx = null;
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
        //                                innerEx = ex;
        //                            }

        //                            throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

        //                        default:
        //                            throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentMoveResponse.Header.Status));
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    if (completion != null)
        //                    {
        //                        try
        //                        {
        //                            completion(responseIdx, completedItem: null, error: ex, userState: completionState);
        //                        }
        //                        catch
        //                        {
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionCLHttpRestWithoutMoveResponses);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex;
        //    }

        //    return null;
        //}

        #endregion  // end RenameFiles (Renames files in the cloud.)

        #region RenameFolders (Rename folders in the cloud.)
        /// <summary>
        /// Asynchronously starts renaming folders in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="itemParams">An array of parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <param name="completionCallback">Delegate which will be fired upon successful communication for every response item</param>
        /// <param name="completionCallbackUserState">Userstate to be passed whenever the completion delegate is fired</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginRenameFolders(AsyncCallback callback, object callbackUserState, RenameItemParams[] itemParams, CLFileItemCompletion completionCallback, object completionCallbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        callback,
                        callbackUserState),
                    itemParams = itemParams
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = RenameFolders(
                            Data.itemParams,
                            completionCallback,
                            completionCallbackUserState);

                        Data.toReturn.Complete(overallError, // any overall error that may have occurred during processing
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
        /// Finishes renaming folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndRenameFolders(IAsyncResult aResult, out SyncboxRenameFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxRenameFoldersResult>(aResult, out result);
        }

        /// <summary>
        /// Rename folders in the cloud.
        /// </summary>
        /// <param name="itemParams">An array of parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <param name="completionCallback">Delegate which will be fired upon successful communication for every response item.</param>
        /// <param name="completionCallbackUserState">Userstate to be passed whenever the completion delegate is fired.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError RenameFolders(RenameItemParams[] itemParams, CLFileItemCompletion completionCallback, object completionCallbackUserState)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.

                if (itemParams == null
                    || itemParams.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[itemParams.Length];
                FilePath syncboxPathObject = _syncbox.Path;

                for (int paramIdx = 0; paramIdx < itemParams.Length; paramIdx++)
                {
                    RenameItemParams currentParams = itemParams[paramIdx];

                    // The CLFileItem represents an existing file or folder, and should be valid because we created it.  The new full path must
                    // fit the specs for the Windows client.  Form the new full path and check its validity.
                    if (String.IsNullOrWhiteSpace(currentParams.ItemToRename.Path))
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidExistingPath, String.Format(Resources.ExceptionOnDemandRenameFilesInvalidExistingPathInItemMsg0, paramIdx.ToString()));
                    }
                    FilePath fullPathExisting = new FilePath(currentParams.ItemToRename.Path, _syncbox.Path);
                    FilePath fullPathNew = new FilePath(currentParams.NewName, fullPathExisting.Parent);
                    CheckPath(fullPathNew, CLExceptionCode.OnDemand_RenameNewName);


                    // file move (rename) and folder move (rename) share a json contract object for move (rename)
                    jsonContractMoves[paramIdx] = new FileOrFolderMove()
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        RelativeToPath = fullPathNew.GetRelativePath(_syncbox.Path, true),
                        ServerUid = currentParams.ItemToRename.Uid,
                        SyncboxId = _syncbox.SyncboxId
                    };
                }

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLCredentialMSTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Now make the REST request content.
                object requestContent = new JsonContracts.FileOrFolderMoves()
                {
                    SyncboxId = _syncbox.SyncboxId,
                    Moves = jsonContractMoves,
                    DeviceId = _copiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderMoves;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.MoveResponses != null)
                {
                    if (responseFromServer.MoveResponses.Length != itemParams.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FolderRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();

                    for (int responseIdx = 0; responseIdx < responseFromServer.MoveResponses.Length; responseIdx++)
                    {
                        try
                        {
                            FileChangeResponse currentMoveResponse = responseFromServer.MoveResponses[responseIdx];

                            if (currentMoveResponse == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                            }
                            if (currentMoveResponse.Header == null || string.IsNullOrEmpty(currentMoveResponse.Header.Status))
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                            }
                            if (currentMoveResponse.Metadata == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                            }

                            switch (currentMoveResponse.Header.Status)
                            {
                                case CLDefinitions.CLEventTypeNoOperation:
                                case CLDefinitions.CLEventTypeAccepted:
                                    CLFileItem resultItem = new CLFileItem(currentMoveResponse.Metadata, currentMoveResponse.Header.Action, currentMoveResponse.Action, _syncbox);
                                    if (completionCallback != null)
                                    {
                                        try
                                        {
                                            completionCallback(responseIdx, resultItem, error: null, userState: completionCallbackUserState);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    break;

                                case CLDefinitions.CLEventTypeAlreadyDeleted:
                                    throw new CLException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandAlreadyDeleted);

                                //// to_parent_uid not an input parameter, we do not expect to see it in the status so it is commented out:
                                //case CLDefinitions.CLEventTypeToParentNotFound:
                                case CLDefinitions.CLEventTypeNotFound:
                                    throw new CLException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandNotFound);

                                case CLDefinitions.CLEventTypeConflict:
                                    throw new CLException(CLExceptionCode.OnDemand_Conflict, Resources.ExceptionOnDemandConflict);

                                case CLDefinitions.RESTResponseStatusFailed:
                                    Exception innerEx;
                                    string errorMessageString;
                                    try
                                    {
                                        errorMessageString = string.Join(Environment.NewLine, currentMoveResponse.Metadata.ErrorMessage);
                                        innerEx = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                        innerEx = ex;
                                    }

                                    throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                default:
                                    throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentMoveResponse.Header.Status));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (completionCallback != null)
                            {
                                try
                                {
                                    completionCallback(responseIdx, completedItem: null, error: ex, userState: completionCallbackUserState);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_FolderRename, Resources.ExceptionCLHttpRestWithoutMoveResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        ///// <summary>
        ///// Asynchronously starts renaming folders in the cloud.
        ///// </summary>
        ///// <param name="callback">Callback method to fire when operation completes</param>
        ///// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        ///// <param name="paths">An array of full paths to where the folders would exist locally on disk</param>
        ///// <param name="newPaths">Array of new full paths corresponding to the paths array</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //public IAsyncResult BeginRenameFolders(AsyncCallback callback, object callbackUserState, string[] paths, string[] newPaths)
        //{
        //    var asyncThread = DelegateAndDataHolderBase.Create(
        //        // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //        new
        //        {
        //            // create the asynchronous result to return
        //            toReturn = new GenericAsyncResult<SyncboxRenameFoldersResult>(
        //                callback,
        //                callbackUserState),
        //            paths,
        //            newPaths
        //        },
        //        (Data, errorToAccumulate) =>
        //        {
        //            // The ThreadProc.
        //            // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
        //            try
        //            {
        //                // declare the output status for communication
        //                // declare the specific type of result for this operation
        //                CLFileItem[] responses;
        //                CLError[] errors;
        //                // alloc and init the syncbox with the passed parameters, storing any error that occurs
        //                CLError overallError = RenameFolders(
        //                    Data.paths,
        //                    Data.newPaths,
        //                    out responses,
        //                    out errors);

        //                Data.toReturn.Complete(
        //                    new SyncboxRenameFoldersResult(
        //                        overallError, // any overall error that may have occurred during processing
        //                        errors,     // any item erros that may have occurred during processing
        //                        responses), // the specific type of result for this operation
        //                    sCompleted: false); // processing did not complete synchronously
        //            }
        //            catch (Exception ex)
        //            {
        //                Data.toReturn.HandleException(
        //                    ex, // the exception which was not handled correctly by the CLError wrapping
        //                    sCompleted: false); // processing did not complete synchronously
        //            }
        //        },
        //        null);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ThreadStart(asyncThread.VoidProcess))).Start(); // start the asynchronous processing thread which is attached to its data

        //    // return the asynchronous result
        //    return asyncThread.TypedData.toReturn;
        //}

        ///// <summary>
        ///// Finishes renaming folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="aResult">The asynchronous result provided upon starting the request</param>
        ///// <param name="result">(output) The result from the request</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //public CLError EndRenameFolders(IAsyncResult aResult, out SyncboxRenameFoldersResult result)
        //{
        //    return Helpers.EndAsyncOperation<SyncboxRenameFoldersResult>(aResult, out result);
        //}

        ///// <summary>
        ///// Rename folders in the cloud.
        ///// </summary>
        ///// <param name="paths">An array of full paths to where the folders would exist locally on disk</param>
        ///// <param name="newPaths">Array of new full paths corresponding to the paths array</param>
        ///// <param name="responses">(output) An array of response objects from communication</param>
        ///// <param name="errors">(output) An array of errors from communication.</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //public CLError RenameFolders(string[] paths, string[] newPaths, out CLFileItem[] responses, out CLError[] errors)
        //{
        //    // try/catch to process the request,  On catch return the error
        //    try
        //    {
        //        // check input parameters.
        //        if (paths.Length != newPaths.Length)
        //        {
        //            throw new ArgumentException("The paths and newPaths arrays must have the same number of elements");
        //        }
        //        for (int i = 0; i < paths.Length; ++i)
        //        {
        //            CheckPath(paths[i], CLExceptionCode.OnDemand_InvalidExistingPath);
        //            CheckPath(newPaths[i], CLExceptionCode.OnDemand_RenameNewName);
        //        }

        //        if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
        //        {
        //            throw new ArgumentException("timeoutMilliseconds must be greater than zero");
        //        }

        //        // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
        //        Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
        //        {
        //            ProcessingStateByThreadId = _processingStateByThreadId,
        //            GetNewCredentialsCallback = _getNewCredentialsCallback,
        //            GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
        //            GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
        //            SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
        //        };

        //        // Build the REST content dynamically.
        //        // Folder move (rename) and folder move (rename) share a json contract object for move (rename).
        //        // This will be an array of contracts.
        //        int numberOfFolders = paths.Length;
        //        List<FileOrFolderMove> listMoveContract = new List<FileOrFolderMove>();
        //        for (int i = 0; i < numberOfFolders; ++i)
        //        {
        //            FilePath folderPath = new FilePath(paths[i]);
        //            FilePath newFolderPath = new FilePath(newPaths[i]);

        //            FileOrFolderMove thisMove = new FileOrFolderMove()
        //            {
        //                //DeviceId = _copiedSettings.DeviceId,
        //                RelativeFromPath = folderPath.GetRelativePath(_syncbox.Path, true),
        //                RelativeToPath = newFolderPath.GetRelativePath(_syncbox.Path, true),
        //                //SyncboxId = _syncbox.SyncboxId
        //            };

        //            listMoveContract.Add(thisMove);
        //        }

        //        // Now make the REST request content.
        //        object requestContent = new JsonContracts.FileOrFolderMoves()
        //        {
        //            SyncboxId = _syncbox.SyncboxId,
        //            DeviceId = _copiedSettings.DeviceId,
        //            Moves = listMoveContract.ToArray()
        //        };

        //        // server method path switched on whether change is a folder or not
        //        string serverMethodPath = CLDefinitions.MethodPathOneOffFolderMoves;

        //        // Communicate with the server to get the response.
        //        JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
        //        responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
        //            CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
        //            serverMethodPath, // dynamic path to appropriate one-off method
        //            Helpers.requestMethod.post, // one-off methods are all posts
        //            _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
        //            null, // not an upload or download
        //            Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
        //            _copiedSettings, // pass the copied settings
        //            _syncbox.SyncboxId, // pass the unique id of the sync box on the server
        //            requestNewCredentialsInfo);   // pass the optional parameters to support temporary token reallocation.

        //        // Convert these items to the output array.
        //        if (responseFromServer != null && responseFromServer.MoveResponses != null)
        //        {
        //            List<CLFileItem> listFileItems = new List<CLFileItem>();
        //            List<CLError> listErrors = new List<CLError>();
        //            foreach (FileChangeResponse fileChangeResponse in responseFromServer.MoveResponses)
        //            {
        //                if (fileChangeResponse != null && fileChangeResponse.Metadata != null)
        //                {
        //                    try
        //                    {
        //                        listFileItems.Add(new CLFileItem(fileChangeResponse.Metadata, fileChangeResponse.Header.Action, fileChangeResponse.Action, _syncbox));
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        CLException exInner = new CLException(CLExceptionCode.OnDemand_FolderRenameInvalidMetadata, ex.Message, ex);
        //                        listErrors.Add(new CLError(exInner));
        //                    }
        //                }
        //                else
        //                {
        //                    string msg = "<Unknown>";
        //                    if (fileChangeResponse.Header.Status != null)
        //                    {
        //                        msg = fileChangeResponse.Header.Status;
        //                    }

        //                    CLException ex = new CLException(CLExceptionCode.OnDemand_FolderRename, msg);
        //                    listErrors.Add(new CLError(ex));
        //                }
        //            }
        //            responses = listFileItems.ToArray();
        //            errors = listErrors.ToArray();
        //        }
        //        else
        //        {
        //            throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMoveResponses);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        responses = Helpers.DefaultForType<CLFileItem[]>();
        //        errors = Helpers.DefaultForType<CLError[]>();
        //        return ex;
        //    }
        //    return null;
        //}
        #endregion  // end RenameFolders (Renames folders in the cloud.)

        #region DeleteFiles (Delete files in the cloud.)
        /// <summary>
        /// Asynchronously starts deleting files in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFiles(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxDeleteFilesResult>(
                        callback,
                        callbackUserState),
                    paths
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        // declare the specific type of result for this operation
                        CLFileItem[] responses;
                        CLError[] errors;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = DeleteFiles(
                            Data.paths,
                            out responses,
                            out errors);

                        Data.toReturn.Complete(
                            new SyncboxDeleteFilesResult(
                                overallError, // any overall error that may have occurred during processing
                                errors,     // any item erros that may have occurred during processing
                                responses), // the specific type of result for this operation
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
        /// Finishes deleting files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFiles(IAsyncResult aResult, out SyncboxDeleteFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxDeleteFilesResult>(aResult, out result);
        }

        /// <summary>
        /// Delete files in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk</param>
        /// <param name="responses">(output) An array of response objects from communication</param>
        /// <param name="errors">(output) An array of errors from communication.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFiles(string[] paths, out CLFileItem[] responses, out CLError[] errors)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // Check the input parameters
                for (int i = 0; i < paths.Length; ++i)
                {
                    CheckPath(paths[i], CLExceptionCode.OnDemand_FileDeleteBadPath);
                }

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Build the REST content dynamically.
                // File move (rename) and folder move (rename) share a json contract object for move (rename).
                // This will be an array of contracts.
                int numberOfFiles = paths.Length;
                List<string> listDeleteContract = new List<string>();
                for (int i = 0; i < numberOfFiles; ++i)
                {
                    FilePath filePath = new FilePath(paths[i]);

                    //FileDeleteRequest thisDelete = new FileOrFolderDelete()
                    //{
                    //    //DeviceId = _copiedSettings.DeviceId,
                    //    RelativePath = filePath.GetRelativePath(_syncbox.Path, true),
                    //    //SyncboxId = _syncbox.SyncboxId
                    //};

                    listDeleteContract.Add(filePath.GetRelativePath(_syncbox.Path, true));
                }

                // Now make the REST request content.
                object requestContent = new JsonContracts.FileDeleteRequest()
                {
                    DeviceId = _copiedSettings.DeviceId,
                    SyncboxId = _syncbox.SyncboxId,
                    Deletes = listDeleteContract.ToArray()
                };

                // server method path switched on whether change is a folder or not
                string serverMethodPath = CLDefinitions.MethodPathOneOffFileDeletes;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxDeleteFilesResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteFilesResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.DeleteResponses != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();
                    foreach (FileChangeResponse fileChangeResponse in responseFromServer.DeleteResponses)
                    {
                        if (fileChangeResponse != null && fileChangeResponse.Metadata != null)
                        {
                            try
                            {
                                listFileItems.Add(new CLFileItem(fileChangeResponse.Metadata, fileChangeResponse.Header.Action, fileChangeResponse.Action, _syncbox));
                            }
                            catch (Exception ex)
                            {
                                CLException exInner = new CLException(CLExceptionCode.OnDemand_FileDeleteInvalidMetadata, ex.Message, ex);
                                listErrors.Add(new CLError(exInner));
                            }
                        }
                        else
                        {
                            string msg = "<Unknown>";
                            if (fileChangeResponse.Header.Status != null)
                            {
                                msg = fileChangeResponse.Header.Status;
                            }

                            CLException ex = new CLException(CLExceptionCode.OnDemand_FileDelete, msg);
                            listErrors.Add(new CLError(ex));
                        }
                    }
                    responses = listFileItems.ToArray();
                    errors = listErrors.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMoveResponses);
                }
            }
            catch (Exception ex)
            {
                responses = Helpers.DefaultForType<CLFileItem[]>();
                errors = Helpers.DefaultForType<CLError[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end DeleteFiles (Deletes files in the cloud.)

        #region DeleteFolders (Delete folders in the cloud.)
        /// <summary>
        /// Asynchronously starts deleting folders in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteFolders(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxDeleteFoldersResult>(
                        callback,
                        callbackUserState),
                    paths
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        // declare the specific type of result for this operation
                        CLFileItem[] responses;
                        CLError[] errors;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = DeleteFolders(
                            Data.paths,
                            out responses,
                            out errors);

                        Data.toReturn.Complete(
                            new SyncboxDeleteFoldersResult(
                                overallError, // any overall error that may have occurred during processing
                                errors,     // any item erros that may have occurred during processing
                                responses), // the specific type of result for this operation
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
        /// Finishes deleting folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteFolders(IAsyncResult aResult, out SyncboxDeleteFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxDeleteFoldersResult>(aResult, out result);
        }

        /// <summary>
        /// Delete folders in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk</param>
        /// <param name="responses">(output) An array of response objects from communication</param>
        /// <param name="errors">(output) An array of errors from communication.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteFolders(string[] paths, out CLFileItem[] responses, out CLError[] errors)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                for (int i = 0; i < paths.Length; ++i)
                {
                    CheckPath(paths[i], CLExceptionCode.OnDemand_FolderDeleteBadPath);
                }

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Build the REST content dynamically.
                // Folder move (rename) and folder move (rename) share a json contract object for move (rename).
                // This will be an array of contracts.
                int numberOfFolders = paths.Length;
                List<FileOrFolderDelete> listDeleteContract = new List<FileOrFolderDelete>();
                for (int i = 0; i < numberOfFolders; ++i)
                {
                    FilePath folderPath = new FilePath(paths[i]);

                    FileOrFolderDelete thisDelete = new FileOrFolderDelete()
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        RelativePath = folderPath.GetRelativePath(_syncbox.Path, true),
                        SyncboxId = _syncbox.SyncboxId
                    };

                    listDeleteContract.Add(thisDelete);
                }

                // Now make the REST request content.
                object requestContent = new JsonContracts.FileOrFolderDeletes()
                {
                    Deletes = listDeleteContract.ToArray()
                };

                // server method path switched on whether change is a folder or not
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderDeletes;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxDeleteFilesResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteFilesResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.DeleteResponses != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();
                    foreach (FileChangeResponse fileChangeResponse in responseFromServer.DeleteResponses)
                    {
                        if (fileChangeResponse != null && fileChangeResponse.Metadata != null)
                        {
                            try
                            {
                                listFileItems.Add(new CLFileItem(fileChangeResponse.Metadata, fileChangeResponse.Header.Action, fileChangeResponse.Action, _syncbox));
                            }
                            catch (Exception ex)
                            {
                                CLException exInner = new CLException(CLExceptionCode.OnDemand_FolderDeleteInvalidMetadata, ex.Message, ex);
                                listErrors.Add(new CLError(exInner));
                            }
                        }
                        else
                        {
                            string msg = "<Unknown>";
                            if (fileChangeResponse.Header.Status != null)
                            {
                                msg = fileChangeResponse.Header.Status;
                            }

                            CLException ex = new CLException(CLExceptionCode.OnDemand_FolderDelete, msg);
                            listErrors.Add(new CLError(ex));
                        }
                    }
                    responses = listFileItems.ToArray();
                    errors = listErrors.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMoveResponses);
                }
            }
            catch (Exception ex)
            {
                responses = Helpers.DefaultForType<CLFileItem[]>();
                errors = Helpers.DefaultForType<CLError[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end DeleteFolders (Deletes folders in the cloud.)

        #region AddFolders (Add folders in the cloud.)
        /// <summary>
        /// Asynchronously starts adding folders in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFolders(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAddFoldersResult>(
                        callback,
                        callbackUserState),
                    paths
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        // declare the specific type of result for this operation
                        CLFileItem[] responses;
                        CLError[] errors;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = AddFolders(
                            Data.paths,
                            out responses,
                            out errors);

                        Data.toReturn.Complete(
                            new SyncboxAddFoldersResult(
                                overallError, // any overall error that may have occurred during processing
                                errors,     // any item erros that may have occurred during processing
                                responses), // the specific type of result for this operation
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
        /// Finishes adding folders in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFolders(IAsyncResult aResult, out SyncboxAddFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAddFoldersResult>(aResult, out result);
        }

        /// <summary>
        /// Add folders in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the folders would exist locally on disk</param>
        /// <param name="responses">(output) An array of response objects from communication</param>
        /// <param name="errors">(output) An array of errors from communication.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFolders(string[] paths, out CLFileItem[] responses, out CLError[] errors)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                for (int i = 0; i < paths.Length; ++i)
                {
                    
                    CheckPath(paths[i], CLExceptionCode.OnDemand_FolderAddBadPath);
                }

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Build the REST content dynamically.
                // Folder move (rename) and folder move (rename) share a json contract object for move (rename).
                // This will be an array of contracts.
                int numberOfFolders = paths.Length;
                List<FolderAdd> listAddContract = new List<FolderAdd>();
                for (int i = 0; i < numberOfFolders; ++i)
                {
                    FilePath folderPath = new FilePath(paths[i]);

                    FolderAdd thisAdd = new FolderAdd()
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        RelativePath = folderPath.GetRelativePath(_syncbox.Path, true),
                        SyncboxId = _syncbox.SyncboxId
                    };

                    listAddContract.Add(thisAdd);
                }

                // Now make the REST request content.
                object requestContent = new JsonContracts.FolderAdds()
                {
                    Adds = listAddContract.ToArray()
                };

                // server method path switched on whether change is a folder or not
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderAdds;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxAddFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxAddFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.AddResponses != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();
                    foreach (FileChangeResponse fileChangeResponse in responseFromServer.AddResponses)
                    {
                        if (fileChangeResponse != null && fileChangeResponse.Metadata != null)
                        {
                            try
                            {
                                listFileItems.Add(new CLFileItem(fileChangeResponse.Metadata, fileChangeResponse.Header.Action, fileChangeResponse.Action, _syncbox));
                            }
                            catch (Exception ex)
                            {
                                CLException exInner = new CLException(CLExceptionCode.OnDemand_FolderAddInvalidMetadata, ex.Message, ex);
                                listErrors.Add(new CLError(exInner));
                            }
                        }
                        else
                        {
                            string msg = "<Unknown>";
                            if (fileChangeResponse.Header.Status != null)
                            {
                                msg = fileChangeResponse.Header.Status;
                            }

                            CLException ex = new CLException(CLExceptionCode.OnDemand_FolderAdd, msg);
                            listErrors.Add(new CLError(ex));
                        }
                    }
                    responses = listFileItems.ToArray();
                    errors = listErrors.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMoveResponses);
                }
            }
            catch (Exception ex)
            {
                responses = Helpers.DefaultForType<CLFileItem[]>();
                errors = Helpers.DefaultForType<CLError[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end AddFolders (Adds folders in the cloud.)

        #region AddFiles (Add files in the cloud.)
        /// <summary>
        /// Asynchronously starts adding files in the cloud.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginAddFiles(AsyncCallback callback, object callbackUserState, string[] paths)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAddFilesResult>(
                        callback,
                        callbackUserState),
                    paths
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the output status for communication
                        // declare the specific type of result for this operation
                        CLFileItem[] responses;
                        CLError[] errors;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = AddFiles(
                            Data.paths,
                            out responses,
                            out errors);

                        Data.toReturn.Complete(
                            new SyncboxAddFilesResult(
                                overallError, // any overall error that may have occurred during processing
                                errors,     // any item erros that may have occurred during processing
                                responses), // the specific type of result for this operation
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
        /// Finishes adding files in the cloud, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndAddFiles(IAsyncResult aResult, out SyncboxAddFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAddFilesResult>(aResult, out result);
        }

        /// <summary>
        /// Add files in the cloud.
        /// </summary>
        /// <param name="paths">An array of full paths to where the files would exist locally on disk</param>
        /// <param name="responses">(output) An array of response objects from communication</param>
        /// <param name="errors">(output) An array of errors from communication.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddFiles(string[] paths, out CLFileItem[] responses, out CLError[] errors)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                for (int i = 0; i < paths.Length; ++i)
                {
                    CheckPath(paths[i], CLExceptionCode.OnDemand_FileAddBadPath);
                }

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Build the REST content dynamically.
                // File move (rename) and file move (rename) share a json contract object for move (rename).
                // This will be an array of contracts.
                int numberOfFiles = paths.Length;
                List<FileAdd> listAddContract = new List<FileAdd>();
                for (int i = 0; i < numberOfFiles; ++i)
                {
                    FilePath filePath = new FilePath(paths[i]);

                    FileAdd thisAdd = new FileAdd()
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        RelativePath = filePath.GetRelativePath(_syncbox.Path, true),
                        SyncboxId = _syncbox.SyncboxId
                    };

                    listAddContract.Add(thisAdd);
                }

                // Now make the REST request content.
                object requestContent = new JsonContracts.FileAdds()
                {
                    Adds = listAddContract.ToArray()
                };

                // server method path switched on whether change is a file or not
                string serverMethodPath = CLDefinitions.MethodPathOneOffFileAdds;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxAddFilesResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxAddFilesResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.AddResponses != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();
                    foreach (FileChangeResponse fileChangeResponse in responseFromServer.AddResponses)
                    {
                        if (fileChangeResponse != null && fileChangeResponse.Metadata != null)
                        {
                            try
                            {
                                listFileItems.Add(new CLFileItem(fileChangeResponse.Metadata, fileChangeResponse.Header.Action, fileChangeResponse.Action, _syncbox));
                            }
                            catch (Exception ex)
                            {
                                CLException exInner = new CLException(CLExceptionCode.OnDemand_FileAddInvalidMetadata, ex.Message, ex);
                                listErrors.Add(new CLError(exInner));
                            }
                        }
                        else
                        {
                            string msg = "<Unknown>";
                            if (fileChangeResponse.Header.Status != null)
                            {
                                msg = fileChangeResponse.Header.Status;
                            }

                            CLException ex = new CLException(CLExceptionCode.OnDemand_FileAdd, msg);
                            listErrors.Add(new CLError(ex));
                        }
                    }
                    responses = listFileItems.ToArray();
                    errors = listErrors.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMoveResponses);
                }
            }
            catch (Exception ex)
            {
                responses = Helpers.DefaultForType<CLFileItem[]>();
                errors = Helpers.DefaultForType<CLError[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end AddFiles (Adds files in the cloud.)

        #region UndoDeletionFileChange
        /// <summary>
        /// Asynchronously starts posting a single FileChange to the server
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="deletionChange">Deletion change which needs to be undone</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="serverUid">Unique server "uid" for the file or folder</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUndoDeletionFileChange(AsyncCallback aCallback,
            object aState,
            FileChange deletionChange,
            int timeoutMilliseconds,
            string serverUid)
        {
            // create the asynchronous result to return
            GenericAsyncResult<UndoDeletionFileChangeResult> toReturn = new GenericAsyncResult<UndoDeletionFileChangeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int, string> asyncParams =
                new Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int, string>(
                    toReturn,
                    deletionChange,
                    timeoutMilliseconds,
                    serverUid);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int, string> castState = state as Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int, string>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.FileChangeResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UndoDeletionFileChange(
                            castState.Item2,
                            castState.Item3,
                            out result,
                            castState.Item4);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new UndoDeletionFileChangeResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes undoing a deletion FileChange if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUndoDeletionFileChange(IAsyncResult aResult, out UndoDeletionFileChangeResult result)
        {
            // declare the specific type of asynchronous result for undoing deletion
            GenericAsyncResult<UndoDeletionFileChangeResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for undoing deletion and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for undoing deletion
                castAResult = aResult as GenericAsyncResult<UndoDeletionFileChangeResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<UndoDeletionFileChangeResult>();
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
        /// Undoes a previously posted deletion change. Folder undeletion is non-recursive and will not undelete inner files or folders.
        /// </summary>
        /// <param name="deletionChange">Deletion change which needs to be undone</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="serverUid">Unique server "uid" for the file or folder</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UndoDeletionFileChange(FileChange deletionChange, int timeoutMilliseconds, out JsonContracts.FileChangeResponse response, string serverUid)
        {
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }
                if (deletionChange == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestDeletionChangeCannotBeNull);
                }
                if (deletionChange.Direction == SyncDirection.From)
                {
                    throw new ArgumentException(Resources.CLHttpRestChangeDirectionIsNotToServer);
                }
                if (deletionChange.Metadata == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestMetadataCannotBeNull);
                }
                if (deletionChange.Type != FileChangeType.Deleted)
                {
                    throw new ArgumentException(Resources.CLHttpRestChangeIsNotOfTypeDeletion);
                }
                if (_syncbox.Path == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
                }
                if (serverUid == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestDeletionChangeMetadataServerIDMustnotBeNull);
                }
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException(Resources.CLHttpRestDeviceIDCannotBeNull);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FileChangeResponse>(new JsonContracts.FileOrFolderUndelete() // files and folders share a request content object for undelete
                    {
                        DeviceId = _copiedSettings.DeviceId, // device id
                        ServerUid = serverUid, // unique id on server
                        SyncboxId = _syncbox.SyncboxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    (deletionChange.Metadata.HashableProperties.IsFolder // folder/file switch
                        ? CLDefinitions.MethodPathFolderUndelete // path for folder undelete
                        : CLDefinitions.MethodPathFileUndelete), // path for file undelete
                    Helpers.requestMethod.post, // undelete file or folder is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileChangeResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region CopyFile
        /// <summary>
        /// Asynchronously copies a file on the server to another location
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginCopyFile(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath copyTargetPath)
        {
            return BeginCopyFile(aCallback,
                aState,
                fileServerId,
                timeoutMilliseconds,
                null,
                copyTargetPath);
        }

        /// <summary>
        /// Asynchronously copies a file on the server to another location
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginCopyFile(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath pathToFile,
            FilePath copyTargetPath)
        {
            return BeginCopyFile(aCallback,
                aState,
                null,
                timeoutMilliseconds,
                pathToFile,
                copyTargetPath);
        }

        /// <summary>
        /// Asynchronously copies a file on the server to another location
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginCopyFile(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile,
            FilePath copyTargetPath)
        {
            // create the asynchronous result to return
            GenericAsyncResult<CopyFileResult> toReturn = new GenericAsyncResult<CopyFileResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath> asyncParams =
                new Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath>(
                    toReturn,
                    fileServerId,
                    timeoutMilliseconds,
                    pathToFile,
                    copyTargetPath);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath> castState = state as Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath>;
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
                        // declare the specific type of result for this operation
                        JsonContracts.FileChangeResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = CopyFile(
                            castState.Item2,
                            castState.Item3,
                            castState.Item4,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new CopyFileResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes copying a file on the server to another location if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting copying the file</param>
        /// <param name="result">(output) The result from copying the file</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndCopyFile(IAsyncResult aResult, out CopyFileResult result)
        {
            // declare the specific type of asynchronous result for copying the file
            GenericAsyncResult<CopyFileResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for copying the file and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for copying the file
                castAResult = aResult as GenericAsyncResult<CopyFileResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<CopyFileResult>();
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
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath copyTargetPath, out JsonContracts.FileChangeResponse response)
        {
            return CopyFile(fileServerId, timeoutMilliseconds, null, copyTargetPath, out response);
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out JsonContracts.FileChangeResponse response)
        {
            return CopyFile(null, timeoutMilliseconds, pathToFile, copyTargetPath, out response);
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out JsonContracts.FileChangeResponse response)
        {
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }
                if (_syncbox.Path == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
                }
                if (copyTargetPath == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestCopyPathCannotBeNull);
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new NullReferenceException(Resources.CLHttpRestXORPathtoFileFileServerIDMustNotBeNull);
                }
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException(Resources.CLHttpRestDeviceIDCannotBeNull);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FileChangeResponse>(new JsonContracts.FileCopy() // object for file copy
                    {
                        DeviceId = _copiedSettings.DeviceId, // device id
                        ServerId = fileServerId, // unique id on server
                        RelativePath = (pathToFile == null
                            ? null
                            : pathToFile.GetRelativePath(_syncbox.Path, true)), // path of existing file to copy
                        RelativeToPath = copyTargetPath.GetRelativePath(_syncbox.Path, true), // location to copy file to
                        SyncboxId = _syncbox.SyncboxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathFileCopy, // path for file copy
                    Helpers.requestMethod.post, // file copy is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileChangeResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetAllImageItems (Get all the pictures from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for all image items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllImageItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllImageItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllImageItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllImageItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for pictures if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pictures query</param>
        /// <param name="result">(output) The result from the query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllImageItems(IAsyncResult aResult, out SyncboxGetAllImageItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllImageItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for all image items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllImageItems(out CLFileItem[] response)
        {
            // try/catch to process the pictures query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the pictures retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPictures + // path for getting pictures
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllImageItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllImageItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query pictures (dynamic adding query string)
                    Helpers.requestMethod.get, // query pictures is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllImageItems (Get all the pictures from the server for this syncbox)

        #region GetAllVideoItems (Get all of the video files from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for videos
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllVideoItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllVideoItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllVideoItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllVideoItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for videos if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the videos query</param>
        /// <param name="result">(output) The result from the videos query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllVideoItems(IAsyncResult aResult, out SyncboxGetAllVideoItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllVideoItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for videos
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllVideoItems(out CLFileItem[] response)
        {
            // try/catch to process the videos query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds> 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the videos retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetVideos + // path for getting videos
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllVideoItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllVideoItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query videos (dynamic adding query string)
                    Helpers.requestMethod.get, // query videos is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllVideoItems (Get all of the video files from the server for this syncbox)

        #region GetAllAudioItems (Get all of the video files from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for audio items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllAudioItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllAudioItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllAudioItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllAudioItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for audios if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllAudioItems(IAsyncResult aResult, out SyncboxGetAllAudioItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllAudioItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for audios
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllAudioItems(out CLFileItem[] response)
        {
            // try/catch to process the audios query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the audios retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetAudios + // path for getting audios
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllAudioItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllAudioItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query audios (dynamic adding query string)
                    Helpers.requestMethod.get, // query audios is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllAudioItems (Get all of the video files from the server for this syncbox)

        #region GetAllDocumentItems (Get all of the document files from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for document items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllDocumentItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllDocumentItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllDocumentItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllDocumentItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for document items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllDocumentItems(IAsyncResult aResult, out SyncboxGetAllDocumentItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllDocumentItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for document items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllDocumentItems(out CLFileItem[] response)
        {
            // try/catch to process the audios query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the audios retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetDocuments + // path for getting documents
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                    // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllDocumentItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllDocumentItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query audios (dynamic adding query string)
                    Helpers.requestMethod.get, // query audios is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllDocumentItems (Get all of the document files from the server for this syncbox)

        #region GetAllPresentationItems (Get all of the presentation files from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for presentation items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllPresentationItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllPresentationItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllPresentationItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllPresentationItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for presentation items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPresentationItems(IAsyncResult aResult, out SyncboxGetAllPresentationItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllPresentationItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for presentation items.
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPresentationItems(out CLFileItem[] response)
        {
            // try/catch to process the audios query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the audios retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPresentations + // method path
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                    // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllPresentationItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllPresentationItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query audios (dynamic adding query string)
                    Helpers.requestMethod.get, // query audios is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllPresentationItems (Get all of the presentation files from the server for this syncbox)

        #region GetAllTextItems (Get all of the text files from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for text items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllTextItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllTextItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllTextItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllTextItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for text items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllTextItems(IAsyncResult aResult, out SyncboxGetAllTextItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllTextItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for audios
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllTextItems(out CLFileItem[] response)
        {
            // try/catch to process the audios query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the audios retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetTexts + // method path
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                    // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllTextItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllTextItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query audios (dynamic adding query string)
                    Helpers.requestMethod.get, // query audios is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllTextItems (Get all of the text files from the server for this syncbox)

        #region GetAllArchiveItems (Get all of the archive files from the server for this syncbox)
        /// <summary>
        /// Asynchronously starts querying the server for archive items.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllArchiveItems(AsyncCallback callback, object callbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetAllArchiveItemsResult>(
                        callback,
                        callbackUserState)
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetAllArchiveItems(
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetAllArchiveItemsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for archive items, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllArchiveItems(IAsyncResult aResult, out SyncboxGetAllArchiveItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetAllArchiveItemsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for audios
        /// </summary>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllArchiveItems(out CLFileItem[] response)
        {
            // try/catch to process the audios query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the audios retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetArchives + // method path
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                    // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetAllArchiveItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllArchiveItemsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query audios (dynamic adding query string)
                    Helpers.requestMethod.get, // query audios is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetAllArchiveItems (Get all of the archive files from the server for this syncbox)

        #region GetRecents
        /// <summary>
        /// Asynchronously starts querying the server for recents
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="sinceDate">null to retrieve all of the recents, or specify a date to retrieve items since that date.</param>
        /// <param name="returnLimit">null to retrieve all of the recents, or specify a limit for the number of items to be returned.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetRecents(
            AsyncCallback callback,
            object callbackUserState,
            Nullable<DateTime> sinceDate,
            Nullable<int> returnLimit)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetRecentsResult>(
                        callback,
                        callbackUserState),
                    sinceDate = sinceDate,
                    returnLimit = returnLimit
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetRecents(
                            Data.sinceDate,
                            Data.returnLimit,
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetRecentsResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes querying for recents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the recents query</param>
        /// <param name="result">(output) The result from the recents query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetRecents(IAsyncResult aResult, out SyncboxGetRecentsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetRecentsResult>(aResult, out result);
        }

        /// <summary>
        /// Queries the server for recents
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetRecents(
            Nullable<DateTime> sinceDate,
            Nullable<int> returnLimit,
            out CLFileItem[] response)
        {
            // try/catch to process the recents query, on catch return the error
            try
            {
                // check input parameters

                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the recents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetRecents + // path for getting recents
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                JsonContracts.SyncboxGetRecentsResponse  responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetRecentsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query recents (dynamic adding query string)
                    Helpers.requestMethod.get, // query recents is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert the server response to the requested output format.
                if (responseFromServer != null && responseFromServer.Metadata != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Metadata)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetSyncboxUsage
        /// <summary>
        /// Asynchronously starts getting sync box usage
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetSyncboxUsage(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUsageResult> toReturn = new GenericAsyncResult<SyncboxUsageResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SyncboxUsageResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<SyncboxUsageResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SyncboxUsageResult>, int> castState = state as Tuple<GenericAsyncResult<SyncboxUsageResult>, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxUsageResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetSyncboxUsage(
                            castState.Item2,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SyncboxUsageResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes getting sync box usage if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting sync box usage</param>
        /// <param name="result">(output) The result from getting sync box usage</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSyncboxUsage(IAsyncResult aResult, out SyncboxUsageResult result)
        {
            // declare the specific type of asynchronous result for getting sync box usage
            GenericAsyncResult<SyncboxUsageResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for getting sync box usage and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for getting sync box usage
                castAResult = aResult as GenericAsyncResult<SyncboxUsageResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxUsageResult>();
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
        /// Queries the server for sync box usage
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetSyncboxUsage(int timeoutMilliseconds, out JsonContracts.SyncboxUsageResponse response)
        {
            // try/catch to process the sync box usage query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the sync box usage retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathSyncboxUsage + // path for getting sync box usage
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem(
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringInsideSyncSyncbox_SyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.SyncboxUsageResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query synx box usage (dynamic adding query string)
                    Helpers.requestMethod.get, // query sync box usage is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxUsageResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetFolderHierarchy
        /// <summary>
        /// Asynchronously starts querying folder hierarchy with optional path
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderHierarchy(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath hierarchyRoot = null)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetFolderHierarchyResult> toReturn = new GenericAsyncResult<GetFolderHierarchyResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetFolderHierarchyResult>, int, FilePath> asyncParams =
                new Tuple<GenericAsyncResult<GetFolderHierarchyResult>, int, FilePath>(
                    toReturn,
                    timeoutMilliseconds,
                    hierarchyRoot);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetFolderHierarchyResult>, int, FilePath> castState = state as Tuple<GenericAsyncResult<GetFolderHierarchyResult>, int, FilePath>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.Folders result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFolderHierarchy(
                            castState.Item2,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetFolderHierarchyResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes getting folder hierarchy if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting folder hierarchy</param>
        /// <param name="result">(output) The result from folder hierarchy</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFolderHierarchy(IAsyncResult aResult, out GetFolderHierarchyResult result)
        {
            // declare the specific type of asynchronous result for getting folder hierarchy
            GenericAsyncResult<GetFolderHierarchyResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for getting folder hierarchy and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for getting folder hierarchy
                castAResult = aResult as GenericAsyncResult<GetFolderHierarchyResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<GetFolderHierarchyResult>();
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
        /// Queries server for folder hierarchy with an optional path
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds, out JsonContracts.Folders response, FilePath hierarchyRoot = null)
        {
            // try/catch to process the folder hierarchy query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }
                if (string.IsNullOrEmpty(_syncbox.Path))
                {
                    throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
                }

                // build the location of the folder hierarchy retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderHierarchy + // path for getting folder hierarchy
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),

                        (hierarchyRoot == null
                            ? new KeyValuePair<string, string>() // do not add extra query string parameter if path is not set
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(hierarchyRoot.GetRelativePath(_syncbox.Path, true) + "/"))) // query string parameter for optional path with escaped value
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Folders>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder hierarchy (dynamic adding query string)
                    Helpers.requestMethod.get, // query folder hierarchy is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Folders>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetFolderContentsAtPath (Query the server for the folder contents at a path)
        /// <summary>
        /// Asynchronously starts querying folder contents at a path.
        /// </summary>
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <param name="path">(optional) root path of contents query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContentsAtPath(
            AsyncCallback callback, 
            object callbackUserState,
            string path = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetFolderContentsAtPathResult>(
                        callback,
                        callbackUserState),
                    path
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] response;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = GetFolderContentsAtPath(
                            Data.path,
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxGetFolderContentsAtPathResult(
                                processError, // any error that may have occurred during processing
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
        /// Finishes getting folder contents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFolderContents(IAsyncResult aResult, out SyncboxGetFolderContentsAtPathResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetFolderContentsAtPathResult>(aResult, out result);
        }

        /// <summary>
        /// Queries server for folder contents at a path.
        /// </summary>
        /// <param name="path">The full path of the folder that would be on disk in the syncbox folder.</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContentsAtPath(
            string path,
            out CLFileItem[] response)
        {
            // try/catch to process the folder contents query, on catch return the error
            try
            {
                // check input parameters

                if (path == null)
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestNullPath);
                }

                CLError pathError = Helpers.CheckForBadPath(path);
                if (pathError != null)
                {
                    throw new AggregateException("path is not in the proper format", pathError.Exceptions);
                }

                if (string.IsNullOrEmpty(_syncbox.Path))
                {
                    throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
                }

                if (!path.Contains(_syncbox.Path))
                {
                    throw new ArgumentException("path does not contain syncbox path");
                }
                if (!(_copiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the folder contents retrieval method on the server dynamically
                FilePath contentsRoot = new FilePath(path);
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderContents + // path for getting folder contents
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDepth, ((byte)0).ToString()), // query string parameter for optional depth limit

                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(contentsRoot.GetRelativePath(_syncbox.Path, true) + "/")), // query string parameter for optional path with escaped value

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeDeleted, "false"), // query string parameter for not including deleted objects

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeCount, "true"), // query string parameter for including counts within each folder

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeFolders, "true"), // query string parameter for including folders in the list

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeStoredOnly, "true") // query string parameter for including only stored items in the list
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Communicate with the server to get the response.
                JsonContracts.SyncboxFolderContentsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxFolderContentsResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder contents (dynamic adding query string)
                    Helpers.requestMethod.get, // query folder contents is a get
                    _copiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Objects != null)
                {
                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    foreach (SyncboxMetadataResponse metadata in responseFromServer.Objects)
                    {
                        if (metadata != null)
                        {
                            listFileItems.Add(new CLFileItem(metadata, _syncbox));
                        }
                        else
                        {
                            listFileItems.Add(null);
                        }
                    }
                    response = listFileItems.ToArray();
                }
                else
                {
                    throw new NullReferenceException(Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end GetFolderContentsAtPath (Query the server for the folder contents at a path)

        #region UpdateSyncboxExtendedMetadata
        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncboxExtendedMetadata<T>(AsyncCallback aCallback,
            object aState,
            IDictionary<string, T> metadata,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUpdateExtendedMetadataResult> toReturn = new GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, IDictionary<string, T>, int> asyncParams =
                new Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, IDictionary<string, T>, int>(
                    toReturn,
                    metadata,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, IDictionary<string, T>, int> castState = state as Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, IDictionary<string, T>, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxResponse result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = UpdateSyncboxExtendedMetadata(
                            castState.Item2,
                            castState.Item3,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SyncboxUpdateExtendedMetadataResult(
                                    processError, // any error that may have occurred during processing
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
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncboxExtendedMetadata(AsyncCallback aCallback,
            object aState,
            MetadataDictionary metadata,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUpdateExtendedMetadataResult> toReturn = new GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, MetadataDictionary, int> asyncParams =
                new Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, MetadataDictionary, int>(
                    toReturn,
                    metadata,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, MetadataDictionary, int> castState = state as Tuple<GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>, MetadataDictionary, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxResponse result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = UpdateSyncboxExtendedMetadata(
                            castState.Item2,
                            castState.Item3,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SyncboxUpdateExtendedMetadataResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes updating the extended metadata on a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating extended metadata</param>
        /// <param name="result">(output) The result from updating extended metadata</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateSyncboxExtendedMetadata(IAsyncResult aResult, out SyncboxUpdateExtendedMetadataResult result)
        {
            // declare the specific type of asynchronous result for updating extended metadata
            GenericAsyncResult<SyncboxUpdateExtendedMetadataResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating extended metadata and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for updating extended metadata
                castAResult = aResult as GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxUpdateExtendedMetadataResult>();
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
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncboxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            try
            {
                return UpdateSyncboxExtendedMetadata((metadata == null
                        ? null
                        : new JsonContracts.MetadataDictionary(
                            ((metadata is IDictionary<string, object>)
                                ? (IDictionary<string, object>)metadata
                                : new JsonContracts.MetadataDictionary.DictionaryWrapper<T>(metadata)))),
                    timeoutMilliseconds, out response);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxResponse>();
                return ex;
            }
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncboxExtendedMetadata(MetadataDictionary metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            // try/catch to process setting extended metadata, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.SyncboxResponse>(new JsonContracts.SyncboxMetadata() // json contract object for extended sync box metadata
                    {
                        Id = _syncbox.SyncboxId,
                        Metadata = metadata
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncboxExtendedMetadata, // sync box extended metadata path
                    Helpers.requestMethod.post, // sync box extended metadata is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box extended metadata should give OK or Accepted
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region SyncboxUpdateStoragePlan

        /// <summary>
        /// Asynchronously updates the storage plan on a syncbox.
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">UserState to pass when firing async callback</param>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginSyncboxUpdateStoragePlan<T>(AsyncCallback aCallback,
            object aState,
            long planId,
            int timeoutMilliseconds,
            bool reservedForActiveSync,
            Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T> completionCallback,
            T completionCallbackUserState)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUpdateStoragePlanResult> toReturn = new GenericAsyncResult<SyncboxUpdateStoragePlanResult>(
                aCallback,
                aState);

            if (reservedForActiveSync)
            {
                JsonContracts.SyncboxUpdateStoragePlanResponse unusedResponse;
                toReturn.Complete(
                    new SyncboxUpdateStoragePlanResult(
                        UpdateSyncboxStoragePlan<T>(
                            planId,
                            timeoutMilliseconds,
                            out unusedResponse,
                            reservedForActiveSync,
                            completionCallback,
                            completionCallbackUserState),
                        unusedResponse),
                    sCompleted: true);
            }
            else
            {
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                Tuple<GenericAsyncResult<SyncboxUpdateStoragePlanResult>, long, int, bool, Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T>, T> asyncParams =
                    new Tuple<GenericAsyncResult<SyncboxUpdateStoragePlanResult>, long, int, bool, Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T>, T>(
                        toReturn,
                        planId,
                        timeoutMilliseconds,
                        reservedForActiveSync,
                        completionCallback,
                        completionCallbackUserState);

                // create the thread from a void (object) parameterized start which wraps the synchronous method call
                (new Thread(new ParameterizedThreadStart(state =>
                {
                    // try cast the state as the object with all the input parameters
                    Tuple<GenericAsyncResult<SyncboxUpdateStoragePlanResult>, long, int, bool, Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T>, T> castState = 
                            state as Tuple<GenericAsyncResult<SyncboxUpdateStoragePlanResult>, long, int, bool, Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T>, T>;
                    // if the try cast failed, then show a message box for this unrecoverable error
                    if (castState == null)
                    {
                        MessageEvents.FireNewEventMessage(
                            Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());
                    }
                    // else if the try cast did not fail, then start processing with the input parameters
                    else
                    {
                        // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                        try
                        {
                            // declare the specific type of result for this operation
                            JsonContracts.SyncboxUpdateStoragePlanResponse response;
                            // purge pending files with the passed parameters, storing any error that occurs
                            CLError processError = UpdateSyncboxStoragePlan<T>(
                                castState.Item2,
                                castState.Item3,
                                out response,
                                castState.Item4,
                                castState.Item5,
                                castState.Item6);

                            // if there was an asynchronous result in the parameters, then complete it with a new result object
                            if (castState.Item1 != null)
                            {
                                castState.Item1.Complete(
                                    new SyncboxUpdateStoragePlanResult(
                                        processError, // any error that may have occurred during processing
                                        response), // the specific type of result for this operation
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
            }

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes updating the plan on a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating the plan</param>
        /// <param name="result">(output) The result from updating the plan</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndSyncboxUpdateStoragePlan(IAsyncResult aResult, out SyncboxUpdateStoragePlanResult result)
        {
            // declare the specific type of asynchronous result for updating the plan
            GenericAsyncResult<SyncboxUpdateStoragePlanResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating the plan and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for updating the plan
                castAResult = aResult as GenericAsyncResult<SyncboxUpdateStoragePlanResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxUpdateStoragePlanResult>();
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
        /// Updates the storage plan on a syncbox.
        /// </summary>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="completionCallback">Delegate to call with the response.  May be null.</param>
        /// <param name="completionCallbackUserState">User state to pass to the completionCallback delegate.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateSyncboxStoragePlan<T>(
                long planId, 
                int timeoutMilliseconds,  
                out JsonContracts.SyncboxUpdateStoragePlanResponse response,
                bool reservedForActiveSync,
                Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T> completionCallback,
                T completionCallbackUserState)
        {
            if (reservedForActiveSync)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxUpdateStoragePlanResponse>();
                return new Exception(Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
            }

            IncrementModifyingSyncboxViaPublicAPICalls();

            // try/catch to process updating plan, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                if (planId == 0)
                {
                    throw new ArgumentException(Resources.CLHttpRestPlanIDCannotBeZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.SyncboxUpdateStoragePlanResponse>(
                    new JsonContracts.SyncboxUpdateStoragePlanRequest() // json contract object for sync box update plan request
                    {
                        SyncboxId = _syncbox.SyncboxId,
                        PlanId = planId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncboxUpdatePlan, // sync box update plan path
                    Helpers.requestMethod.post, // sync box update plan is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box update plan should give OK or Accepted
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                if (completionCallback != null)
                {
                    try
                    {
                        completionCallback(response,
                            completionCallbackUserState);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxUpdateStoragePlanResponse>();
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }
            return null;
        }
        #endregion

        #region UpdateSyncbox
        /// <summary>
        /// Asynchronously updates the properties of a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateSyncbox(AsyncCallback aCallback,
            object aState,
            string friendlyName,
            int timeoutMilliseconds)
        {
            return BeginUpdateSyncbox(aCallback, aState, friendlyName, timeoutMilliseconds, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): Asynchronously updates the properties of a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateSyncbox(AsyncCallback aCallback,
            object aState,
            string friendlyName,
            int timeoutMilliseconds,
            bool reservedForActiveSync)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUpdateFriendlyNameResult> toReturn = new GenericAsyncResult<SyncboxUpdateFriendlyNameResult>(
                aCallback,
                aState);

            if (reservedForActiveSync)
            {
                JsonContracts.SyncboxResponse unusedResult;
                toReturn.Complete(
                    new SyncboxUpdateFriendlyNameResult(
                        UpdateSyncbox(
                            friendlyName,
                            timeoutMilliseconds,
                            out unusedResult,
                            reservedForActiveSync),
                        unusedResult),
                    sCompleted: true);
            }
            else
            {
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                Tuple<GenericAsyncResult<SyncboxUpdateFriendlyNameResult>, string, int> asyncParams =
                    new Tuple<GenericAsyncResult<SyncboxUpdateFriendlyNameResult>, string, int>(
                        toReturn,
                        friendlyName,
                        timeoutMilliseconds);

                // create the thread from a void (object) parameterized start which wraps the synchronous method call
                (new Thread(new ParameterizedThreadStart(state =>
                {
                    // try cast the state as the object with all the input parameters
                    Tuple<GenericAsyncResult<SyncboxUpdateFriendlyNameResult>, string, int> castState = state as Tuple<GenericAsyncResult<SyncboxUpdateFriendlyNameResult>, string, int>;
                    // if the try cast failed, then show a message box for this unrecoverable error
                    if (castState == null)
                    {
                        MessageEvents.FireNewEventMessage(
                            Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());
                    }
                    // else if the try cast did not fail, then start processing with the input parameters
                    else
                    {
                        // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                        try
                        {
                            // declare the specific type of result for this operation
                            JsonContracts.SyncboxResponse result;
                            // purge pending files with the passed parameters, storing any error that occurs
                            CLError processError = UpdateSyncbox(
                                castState.Item2,
                                castState.Item3,
                                out result);

                            // if there was an asynchronous result in the parameters, then complete it with a new result object
                            if (castState.Item1 != null)
                            {
                                castState.Item1.Complete(
                                    new SyncboxUpdateFriendlyNameResult(
                                        processError, // any error that may have occurred during processing
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
            }

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes updating the properties of a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating the properties</param>
        /// <param name="result">(output) The result from updating the properties</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUpdateSyncbox(IAsyncResult aResult, out SyncboxUpdateFriendlyNameResult result)
        {
            // declare the specific type of asynchronous result for updating the properties
            GenericAsyncResult<SyncboxUpdateFriendlyNameResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating the properties and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for setting the properties of the syncbox
                castAResult = aResult as GenericAsyncResult<SyncboxUpdateFriendlyNameResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxUpdateFriendlyNameResult>();
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
        /// Updates the the properties of a sync box
        /// </summary>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateSyncbox(string friendlyName, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            return UpdateSyncbox(friendlyName, timeoutMilliseconds, out response, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): Updates the properties of a sync box
        /// </summary>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateSyncbox(string friendlyName, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response, bool reservedForActiveSync)
        {
            if (reservedForActiveSync)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxResponse>();
                return new Exception(Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
            }

            IncrementModifyingSyncboxViaPublicAPICalls();

            // try/catch to process updating the properties, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                if (String.IsNullOrWhiteSpace(friendlyName))
                {
                    throw new ArgumentException(Resources.CLHttpRestFriendlyNameNotSpecified);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.SyncboxResponse>(new JsonContracts.SyncboxUpdateRequest() // json contract object for sync box update request
                {
                    SyncboxId = _syncbox.SyncboxId,
                    Syncbox = new JsonContracts.SyncboxUpdateFriendlyNameRequest()
                    {
                        FriendlyName = friendlyName
                    }
                },
                CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                CLDefinitions.MethodPathAuthSyncboxUpdate, // sync box update
                Helpers.requestMethod.post, // sync box update is a post operation
                timeoutMilliseconds, // set the timeout for the operation
                null, // not an upload or download
                Helpers.HttpStatusesOkAccepted, // sync box update should give OK or Accepted
                _copiedSettings, // pass the copied settings
                _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                true);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxResponse>();
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }
            return null;
        }
        #endregion

        #region DeleteSyncbox
        /// <summary>
        /// ¡¡ Do not use lightly !! Asynchronously deletes a syncbox.  This method deletes the syncbox
        /// that created this CLHttpRest instance.
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteSyncbox(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return BeginDeleteSyncbox(aCallback, aState, timeoutMilliseconds, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): ¡¡ Do not use lightly !! Asynchronously deletes a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginDeleteSyncbox(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool reservedForActiveSync)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxDeleteResult> toReturn = new GenericAsyncResult<SyncboxDeleteResult>(
                aCallback,
                aState);

            if (reservedForActiveSync)
            {
                JsonContracts.SyncboxDeleteResponse unusedResponse;
                toReturn.Complete(
                    new SyncboxDeleteResult(
                        DeleteSyncbox(
                            timeoutMilliseconds,
                            out unusedResponse,
                            reservedForActiveSync),
                        unusedResponse),
                    sCompleted: true);
            }
            else
            {
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                Tuple<GenericAsyncResult<SyncboxDeleteResult>, int, bool> asyncParams =
                    new Tuple<GenericAsyncResult<SyncboxDeleteResult>, int, bool>(
                        toReturn,
                        timeoutMilliseconds,
                        reservedForActiveSync);

                // create the thread from a void (object) parameterized start which wraps the synchronous method call
                (new Thread(new ParameterizedThreadStart(state =>
                {
                    // try cast the state as the object with all the input parameters
                    Tuple<GenericAsyncResult<SyncboxDeleteResult>, int, bool> castState = state as Tuple<GenericAsyncResult<SyncboxDeleteResult>, int, bool>;
                    // if the try cast failed, then show a message box for this unrecoverable error
                    if (castState == null)
                    {
                        MessageEvents.FireNewEventMessage(
                            Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                            EventMessageLevel.Important,
                            new HaltAllOfCloudSDKErrorInfo());
                    }
                    // else if the try cast did not fail, then start processing with the input parameters
                    else
                    {
                        // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                        try
                        {
                            // declare the specific type of result for this operation
                            JsonContracts.SyncboxDeleteResponse response;
                            // purge pending files with the passed parameters, storing any error that occurs
                            CLError processError = DeleteSyncbox(
                                timeoutMilliseconds: castState.Item2,
                                response: out response,
                                reservedForActiveSync: castState.Item3);

                            // if there was an asynchronous result in the parameters, then complete it with a new result object
                            if (castState.Item1 != null)
                            {
                                castState.Item1.Complete(
                                    new SyncboxDeleteResult(
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
            }

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Finishes deleting a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting deleting the sync box</param>
        /// <param name="result">(output) The result from deleting the sync box</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDeleteSyncbox(IAsyncResult aResult, out SyncboxDeleteResult result)
        {
            // declare the specific type of asynchronous result for sync box deletion
            GenericAsyncResult<SyncboxDeleteResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for sync box deletion and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for sync box deletion
                castAResult = aResult as GenericAsyncResult<SyncboxDeleteResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxDeleteResult>();
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
        /// ¡¡ Do not use lightly !! Deletes a syncbox.  This method deletes the syncbox
        /// that created this CLHttpRest instance.
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError DeleteSyncbox(int timeoutMilliseconds, out JsonContracts.SyncboxDeleteResponse response, bool reservedForActiveSync)
        {
            if (reservedForActiveSync)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxDeleteResponse>();
                return new Exception(Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
            }

            IncrementModifyingSyncboxViaPublicAPICalls();

            // try/catch to process deleting sync box, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteResponse>(new JsonContracts.SyncboxIdOnly() // json contract object for deleting sync boxes
                    {
                        Id = _syncbox.SyncboxId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthDeleteSyncbox, // delete sync box path
                    Helpers.requestMethod.post, // delete sync box is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // delete sync box should give OK or Accepted
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxDeleteResponse>();
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }
            return null;
        }
        #endregion

        #region GetSyncboxStatus

        /// <summary>
        /// Asynchronously gets the status of this Syncbox
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="completionCallback">Delegate to call with the response.  May be null.</param>
        /// <param name="completionCallbackUserState">User state to pass to the completionCallback delegate.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginGetSyncboxStatus<T>(
            AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            Action<JsonContracts.SyncboxStatusResponse, T> completionCallback,
            T completionCallbackUserState)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxStatusResult> toReturn = new GenericAsyncResult<SyncboxStatusResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SyncboxStatusResult>, int, Action<JsonContracts.SyncboxStatusResponse, T>, T> asyncParams =
                new Tuple<GenericAsyncResult<SyncboxStatusResult>, int, Action<JsonContracts.SyncboxStatusResponse, T>, T>(
                    toReturn,
                    timeoutMilliseconds,
                    completionCallback,
                    completionCallbackUserState);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SyncboxStatusResult>, int, Action<JsonContracts.SyncboxStatusResponse, T>, T> castState = state as Tuple<GenericAsyncResult<SyncboxStatusResult>, int, Action<JsonContracts.SyncboxStatusResponse, T>, T>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxStatusResponse response;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = GetSyncboxStatus(
                            castState.Item2,
                            out response,
                            castState.Item3,
                            castState.Item4);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SyncboxStatusResult(
                                    processError, // any error that may have occurred during processing
                                    response), // the specific type of result for this operation
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
        /// Finishes getting sync box status if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting sync box status</param>
        /// <param name="result">(output) The result from getting sync box status</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSyncboxStatus(IAsyncResult aResult, out SyncboxStatusResult result)
        {
            // declare the specific type of asynchronous result for sync box status
            GenericAsyncResult<SyncboxStatusResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for getting sync box status and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for getting sync box status
                castAResult = aResult as GenericAsyncResult<SyncboxStatusResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<SyncboxStatusResult>();
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
        /// Gets the status of this Syncbox
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="completionCallback">Delegate to call with the response.  May be null.</param>
        /// <param name="completionCallbackUserState">User state to pass to the completionCallback delegate.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetSyncboxStatus<T>(
            int timeoutMilliseconds, 
            out JsonContracts.SyncboxStatusResponse response,
            Action<JsonContracts.SyncboxStatusResponse, T> completionRoutine, 
            T completionState)
        {
            // try/catch to process purging pending, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.SyncboxStatusResponse>(new JsonContracts.SyncboxIdOnly() // json contract object for purge pending method
                    {
                        Id = _syncbox.SyncboxId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncboxStatus, // sync box status address
                    Helpers.requestMethod.post, // sync box status is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box status should give OK or Accepted
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                if (completionRoutine != null)
                {
                    try
                    {
                        completionRoutine(response,
                            completionState);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxStatusResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #endregion

        #region internal API calls
        /// <summary>
        /// Sends a list of sync events to the server.  The events must be batched in groups of 1,000 or less.
        /// </summary>
        /// <param name="syncToRequest">The array of events to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        internal CLError SyncToCloud(To syncToRequest, int timeoutMilliseconds, out JsonContracts.To response)
        {
            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (syncToRequest == null)
                {
                    throw new ArgumentException(Resources.CLHttpRestSyncToRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.To>(
                    syncToRequest, // object for request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathSyncTo, // path to sync to
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.To>();
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Sends a list of sync events to the server.  The events must be batched in groups of 1,000 or less.
        /// </summary>
        /// <param name="pushRequest">The parameters to send to the server.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        internal CLError SyncFromCloud(Push pushRequest, int timeoutMilliseconds, out JsonContracts.PushResponse response)
        {
            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (pushRequest == null)
                {
                    throw new ArgumentException(Resources.CLHttpRestPushRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.PushResponse>(
                    pushRequest, // object to write as request content to the server
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathSyncFrom, // path to sync from
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PushResponse>();
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Unsubscribe this Syncbox/Device ID from Sync notifications.Add a Sync box on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError SendUnsubscribeToServer(int timeoutMilliseconds, out JsonContracts.NotificationUnsubscribeResponse response)
        {
            // try/catch to process the request. On catch return the error
            try
            {
                // check input parameters
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                JsonContracts.NotificationUnsubscribeRequest request = new JsonContracts.NotificationUnsubscribeRequest()
                {
                    DeviceId = _copiedSettings.DeviceId,
                    SyncboxId = _syncbox.SyncboxId
                };

                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    new[]
                    {
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()), // no need to escape string characters since the source is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSender, Uri.EscapeDataString(_copiedSettings.DeviceId)) // possibly user-provided string, therefore needs escaping
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.NotificationUnsubscribeResponse>(
                    null, // no body needed
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathPushUnsubscribe + query,
                    Helpers.requestMethod.post,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    _copiedSettings,
                    _syncbox.SyncboxId,
                    requestNewCredentialsInfo,
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.NotificationUnsubscribeResponse>();
                return ex;
            }

            return null;
        }

        #region GetMetadata
        /// <summary>
        /// Asynchronously starts querying the server at a given file or folder path (must be specified) for existing metadata at that path
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
            object aState,
            FilePath fullPath,
            bool isFolder,
            int timeoutMilliseconds)
        {
            return BeginGetMetadata(aCallback, aState, fullPath, /*serverId*/ null, isFolder, timeoutMilliseconds);
        }

        /// <summary>
        /// Asynchronously starts querying the server at a given file or folder server id (must be specified) for existing metadata at that id
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
            object aState,
            bool isFolder,
            string serverId,
            int timeoutMilliseconds)
        {
            return BeginGetMetadata(aCallback, aState, /*fullPath*/ null, serverId, isFolder, timeoutMilliseconds);
        }

        /// <summary>
        /// Private helper to combine two overloaded public versions: Asynchronously starts querying the server at a given file or folder path (must be specified) for existing metadata at that path
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        private IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
            object aState,
            FilePath fullPath,
            string serverId,
            bool isFolder,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetMetadataResult> toReturn = new GenericAsyncResult<GetMetadataResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int> asyncParams =
                new Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int>(
                    toReturn,
                    fullPath,
                    serverId,
                    isFolder,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int> castState = state as Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.SyncboxMetadataResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetMetadata(
                            castState.Item2,
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetMetadataResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes a metadata query if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndGetMetadata(IAsyncResult aResult, out GetMetadataResult result)
        {
            // declare the specific type of asynchronous result for metadata query
            GenericAsyncResult<GetMetadataResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for metadata query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for metadata query
                castAResult = aResult as GenericAsyncResult<GetMetadataResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<GetMetadataResult>();
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
        /// Private helper to combine two overloaded public versions: Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetMetadata(bool isFolder, string serverId, int timeoutMilliseconds, out JsonContracts.SyncboxMetadataResponse response)
        {
            return GetMetadata(/*fullPath*/ null, serverId, isFolder, timeoutMilliseconds, out response);
        }

        /// <summary>
        /// Private helper to combine two overloaded public versions: Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetMetadata(FilePath fullPath, bool isFolder, int timeoutMilliseconds, out JsonContracts.SyncboxMetadataResponse response)
        {
            return GetMetadata(fullPath, /*serverId*/ null, isFolder, timeoutMilliseconds, out response);
        }

        /// <summary>
        /// Private helper to combine two overloaded public versions: Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        private CLError GetMetadata(FilePath fullPath, string serverId, bool isFolder, int timeoutMilliseconds, out JsonContracts.SyncboxMetadataResponse response)
        {
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (fullPath == null
                    && string.IsNullOrEmpty(serverId))
                {
                    throw new NullReferenceException(Resources.CLHttpRestFullPathorServerIDRequired);
                }
                if (fullPath != null)
                {
                    CLError pathError = Helpers.CheckForBadPath(fullPath);
                    if (pathError != null)
                    {
                        throw new AggregateException(Resources.CLHttpRestFullPathBadFormat, pathError.Exceptions);
                    }

                    if (string.IsNullOrEmpty(_syncbox.Path))
                    {
                        throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
                    }

                    if (!fullPath.Contains(_syncbox.Path))
                    {
                        throw new ArgumentException(Resources.CLHttpRestFullPathDoesNotContainSettingsSyncboxPath);
                    }
                } 

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    (isFolder
                        ? CLDefinitions.MethodPathGetFolderMetadata // if the current metadata is for a folder, then retrieve it from the folder method
                        : CLDefinitions.MethodPathGetFileMetadata) + // else if the current metadata is for a file, then retrieve it from the file method
                    Helpers.QueryStringBuilder(new[] // both methods grab their parameters by query string (since this method is an HTTP GET)
                    {
                        (string.IsNullOrEmpty(serverId)
                            ? // query string parameter for the path to query, built by turning the full path location into a relative path from the cloud root and then escaping the whole thing for a url
                                new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(fullPath.GetRelativePath((_syncbox.Path ?? string.Empty), true) + (isFolder ? "/" : string.Empty)))

                            : // query string parameter for the unique id to the file or folder on the server, escaped since it is a server opaque field of undefined format
                                new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(serverId))),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.SyncboxMetadataResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    Helpers.requestMethod.get, // query metadata is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncboxMetadataResponse>();
                return ex;
            }
            return null;
        }

        #endregion

        #region DownloadFile
        /// <summary>
        /// Asynchronously starts downloading a file from a provided file download change
        /// </summary>
        /// <param name="aCallback">Callback method to fire upon progress changes in download, make sure it processes quickly if the IAsyncResult IsCompleted is false</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="changeToDownload">File download change, requires Metadata.</param>
        /// <param name="moveFileUponCompletion">¡¡ Action required: move the completed download file from the temp directory to the final destination !! Callback fired when download completes</param>
        /// <param name="moveFileUponCompletionState">Userstate passed upon firing completed download callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file download</param>
        /// <param name="beforeDownload">(optional) Callback fired before a download starts</param>
        /// <param name="beforeDownloadState">Userstate passed upon firing before download callback</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the download</param>
        /// <param name="customDownloadFolderFullPath">(optional) Full path to a folder where temporary downloads will be stored to override default</param>
        /// <returns>Returns the asynchronous result which is used to retrieve progress and/or the result</returns>
        public IAsyncResult BeginDownloadFile(AsyncCallback aCallback,
            object aState,
            FileChange changeToDownload,
            string serverUid,
            string revision,
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            Helpers.BeforeDownloadToTempFile beforeDownload = null,
            object beforeDownloadState = null,
            CancellationTokenSource shutdownToken = null,
            string customDownloadFolderFullPath = null)
        {
            // create a holder for the changing progress of the transfer
            GenericHolder<TransferProgress> progressHolder = new GenericHolder<TransferProgress>(null);

            // create the asynchronous result to return
            GenericAsyncResult<DownloadFileResult> toReturn = new GenericAsyncResult<DownloadFileResult>(
                aCallback,
                aState,
                progressHolder);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>> asyncParams =
                new Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>>(
                    toReturn,
                    aCallback,
                    changeToDownload,
                    serverUid,
                    revision,
                    moveFileUponCompletion,
                    moveFileUponCompletionState,
                    new Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>(
                        timeoutMilliseconds,
                        beforeDownload,
                        beforeDownloadState,
                        shutdownToken,
                        customDownloadFolderFullPath));

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>> castState =
                    state as Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the holder for transfer progress changes
                        GenericHolder<TransferProgress> progress;
                        // if there was no asynchronous result in the parameters, then the progress holder cannot be grabbed so set it to null
                        if (castState.Item1 == null)
                        {
                            progress = null;
                        }
                        // else if there was an asynchronous result in the parameters, then pull the progress holder by try casting the internal state
                        else
                        {
                            progress = castState.Item1.InternalState as GenericHolder<TransferProgress>;
                        }

                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = DownloadFile(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            castState.Item6,
                            castState.Item7,
                            castState.Rest.Item1,
                            castState.Rest.Item2,
                            castState.Rest.Item3,
                            castState.Rest.Item4,
                            castState.Rest.Item5,
                            castState.Item2,
                            castState.Item1,
                            progress,
                            null,
                            null);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new DownloadFileResult(
                                    processError), // any error that may have occurred during processing
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
        /// Outputs the latest progress from a file download, returning any error that occurs in the retrieval
        /// </summary>
        /// <param name="aResult">Asynchronous result originally returned by BeginDownloadFile</param>
        /// <param name="progress">(output) Latest progress from a file download, may be null if the download file hasn't started</param>
        /// <returns>Returns any error that occurred in retrieving the latest progress, if any</returns>
        public CLError GetProgressDownloadFile(IAsyncResult aResult, out TransferProgress progress)
        {
            // try/catch to retrieve the latest progress, on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type of file downloads
                GenericAsyncResult<DownloadFileResult> castAResult = aResult as GenericAsyncResult<DownloadFileResult>;

                // if try casting the asynchronous result failed, throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // try to cast the asynchronous result internal state as the holder for the progress
                GenericHolder<TransferProgress> iState = castAResult.InternalState as GenericHolder<TransferProgress>;

                // if trying to cast the internal state as the holder for progress failed, then throw an error (non-descriptive since it's our error)
                if (iState == null)
                {
                    throw new Exception(Resources.CLHttpRestInternalPRogressRetrievalFailure1);
                }

                // lock on the holder and retrieve the progress for output
                lock (iState)
                {
                    progress = iState.Value;
                }
            }
            catch (Exception ex)
            {
                progress = Helpers.DefaultForType<TransferProgress>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Finishes a file download if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the file download</param>
        /// <param name="result">(output) The result from the file download</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDownloadFile(IAsyncResult aResult, out DownloadFileResult result)
        {
            // declare the specific type of asynchronous result for file downloads
            GenericAsyncResult<DownloadFileResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for file downloads and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for file downloads
                castAResult = aResult as GenericAsyncResult<DownloadFileResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<DownloadFileResult>();
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
        /// Downloads a file from a provided file download change
        /// </summary>
        /// <param name="changeToDownload">File download change, requires Metadata.</param>
        /// <param name="moveFileUponCompletion">¡¡ Action required: move the completed download file from the temp directory to the final destination !! Callback fired when download completes</param>
        /// <param name="moveFileUponCompletionState">Userstate passed upon firing completed download callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file download</param>
        /// <param name="beforeDownload">(optional) Callback fired before a download starts</param>
        /// <param name="beforeDownloadState">Userstate passed upon firing before download callback</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the download</param>
        /// <param name="customDownloadFolderFullPath">(optional) Full path to a folder where temporary downloads will be stored to override default</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DownloadFile(FileChange changeToDownload,
            string serverUid,
            string revision,
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            Helpers.BeforeDownloadToTempFile beforeDownload = null,
            object beforeDownloadState = null,
            CancellationTokenSource shutdownToken = null,
            string customDownloadFolderFullPath = null)
        {
            // pass through input parameters to the private call (which takes additional parameters we don't wish to expose)
            return DownloadFile(changeToDownload,
                serverUid,
                revision,
                moveFileUponCompletion,
                moveFileUponCompletionState,
                timeoutMilliseconds,
                beforeDownload,
                beforeDownloadState,
                shutdownToken,
                customDownloadFolderFullPath,
                null,
                null,
                null,
                null,
                null);

        }

        // internal version with added action for status update
        internal CLError DownloadFile(FileChange changeToDownload,
            string serverUid,
            string revision,
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            Helpers.BeforeDownloadToTempFile beforeDownload,
            object beforeDownloadState,
            CancellationTokenSource shutdownToken,
            string customDownloadFolderFullPath,
            FileTransferStatusUpdateDelegate statusUpdate,
            Guid statusUpdateId)
        {
            return DownloadFile(changeToDownload,
                serverUid,
                revision,
                moveFileUponCompletion,
                moveFileUponCompletionState,
                timeoutMilliseconds,
                beforeDownload,
                beforeDownloadState,
                shutdownToken,
                customDownloadFolderFullPath,
                null,
                null,
                null,
                statusUpdate,
                statusUpdateId);
        }

        // private helper for DownloadFile which takes additional parameters we don't wish to expose; does the actual processing
        private CLError DownloadFile(FileChange changeToDownload,
            string serverUid,
            string revision,
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            Helpers.BeforeDownloadToTempFile beforeDownload,
            object beforeDownloadState,
            CancellationTokenSource shutdownToken,
            string customDownloadFolderFullPath,
            AsyncCallback aCallback,
            IAsyncResult aResult,
            GenericHolder<TransferProgress> progress,
            FileTransferStatusUpdateDelegate statusUpdate,
            Nullable<Guid> statusUpdateId)
        {
            // try/catch to process the file download, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private download class upon Helpers.ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                if (serverUid == null)
                {
                    throw new ArgumentNullException(Resources.ExceptionCLHttpRestNullServerUid);
                }

                if (revision == null)
                {
                    throw new ArgumentNullException(Resources.CLHttpRestRevisionCannotBeNull);
                }

                // declare the path for the folder which will store temp download files
                string currentDownloadFolder;

                // if a specific folder path was passed to use as an override, then store it as the one to use
                if (customDownloadFolderFullPath != null)
                {
                    currentDownloadFolder = customDownloadFolderFullPath;
                }
                // else if a specified folder path was not passed and a path was specified in settings, then store the one from settings as the one to use
                else if (!String.IsNullOrWhiteSpace(_copiedSettings.TempDownloadFolderFullPath))
                {
                    currentDownloadFolder = _copiedSettings.TempDownloadFolderFullPath;
                }
                // else if a specified folder path was not passed and one did not exist in settings, then build one dynamically to use
                else
                {
                    currentDownloadFolder = Helpers.GetTempFileDownloadPath(_copiedSettings, _syncbox.SyncboxId);
                }

                // check if the folder for temp downloads represents a bad path
                CLError badTempFolderError = Helpers.CheckForBadPath(currentDownloadFolder);

                // if the temp download folder is a bad path rethrow the error
                if (badTempFolderError != null)
                {
                    throw new AggregateException(Resources.CLHttpRestThecustomDownloadFolderFullPathIsBad, badTempFolderError.Exceptions);
                }

                // if the folder path for downloads is too long, then throw an exception
                if (currentDownloadFolder.Length > 222) // 222 calculated by 259 max path length minus 1 character for a folder slash seperator plus 36 characters for (Guid).ToString("N")
                {
                    throw new ArgumentException(Resources.CLHttpRestFolderPathTooLong + (currentDownloadFolder.Length - 222).ToString());
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathDownload + // download method path
                    Helpers.QueryStringBuilder(Helpers.EnumerateSingleItem( // add SyncboxId for file download
                    // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    ));

                // prepare the downloadParams before the Helpers.ProcessHttp because it does additional parameter checks first
                Helpers.downloadParams currentDownload = new Helpers.downloadParams( // this is a special communication method and requires passing download parameters
                    moveFileUponCompletion, // callback which should move the file to final location
                    moveFileUponCompletionState, // userstate for the move file callback
                    customDownloadFolderFullPath ?? // first try to use a provided custom folder full path
                        Helpers.GetTempFileDownloadPath(_copiedSettings, _syncbox.SyncboxId), // if custom path not provided, null-coallesce to default
                    Helpers.HandleUploadDownloadStatus, // private event handler to relay status change events
                    changeToDownload, // the FileChange describing the download
                    shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                    _syncbox.Path, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    aCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                    aResult, // asynchronous result to pass when firing the asynchronous callback
                    progress, // holder for progress data which can be queried by user if called via async wrapper
                    statusUpdate, // callback to user to notify when a CLSyncEngine status has changed
                    statusUpdateId, // userstate to pass to the statusUpdate callback
                    beforeDownload, // optional callback fired before download starts
                    beforeDownloadState); // userstate passed when firing download start callback

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the actual communication
                Helpers.ProcessHttp<object>(
                    new Download() // JSON contract to serialize
                    {
                        Uid = serverUid,
                        Revision = revision
                    },
                    CLDefinitions.CLUploadDownloadServerURL, // server for download
                    serverMethodPath, // dynamic method path to incorporate query string parameters
                    Helpers.requestMethod.post, // download is a post
                    timeoutMilliseconds, // time before communication timeout (does not restrict time
                    currentDownload, // download-specific parameters holder constructed directly above
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo, // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region UploadFile
        /// <summary>
        /// Asynchronously starts uploading a file from a provided stream and file upload change
        /// </summary>
        /// <param name="aCallback">Callback method to fire upon progress changes in upload, make sure it processes quickly if the IAsyncResult IsCompleted is false</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="uploadStream">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, Metadata.StorageKey, and MD5 hash to be set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <returns>Returns the asynchronous result which is used to retrieve progress and/or the result</returns>
        internal IAsyncResult BeginUploadFile(AsyncCallback aCallback,
            object aState,
            Stream uploadStream,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            CancellationTokenSource shutdownToken = null)
        {
            // create a holder for the changing progress of the transfer
            GenericHolder<TransferProgress> progressHolder = new GenericHolder<TransferProgress>(null);

            // create the asynchronous result to return
            GenericAsyncResult<UploadFileResult> toReturn = new GenericAsyncResult<UploadFileResult>(
                aCallback,
                aState,
                progressHolder);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, Stream, FileChange, int, CancellationTokenSource> asyncParams =
                new Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, Stream, FileChange, int, CancellationTokenSource>(
                    toReturn,
                    aCallback,
                    uploadStream,
                    changeToUpload,
                    timeoutMilliseconds,
                    shutdownToken);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, StreamContext, FileChange, int, CancellationTokenSource> castState = state as Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, StreamContext, FileChange, int, CancellationTokenSource>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the holder for transfer progress changes
                        GenericHolder<TransferProgress> progress;
                        // if there was no asynchronous result in the parameters, then the progress holder cannot be grabbed so set it to null
                        if (castState.Item1 == null)
                        {
                            progress = null;
                        }
                        // else if there was an asynchronous result in the parameters, then pull the progress holder by try casting the internal state
                        else
                        {
                            progress = castState.Item1.InternalState as GenericHolder<TransferProgress>;
                        }

                        // declare the output message for upload
                        string message;
                        bool hashMismatchFound;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UploadFile(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            out message,
                            out hashMismatchFound,
                            castState.Item6,
                            castState.Item2,
                            castState.Item1,
                            progress,
                            null,
                            null);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(new UploadFileResult(processError,
                                message,
                                hashMismatchFound),
                                sCompleted: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // if there was an asynchronous result in the parameters, then pass through the exception to it
                        if (castState.Item1 != null)
                        {
                            castState.Item1.HandleException(
                                ex, // the exception which was not handled correctly by the CLError wrapping
                                false); // processing did not complete synchronously
                        }
                    }
                }
            }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object

            // return the asynchronous result
            return toReturn;
        }

        /// <summary>
        /// Outputs the latest progress from a file upload, returning any error that occurs in the retrieval
        /// </summary>
        /// <param name="aResult">Asynchronous result originally returned by BeginUploadFile</param>
        /// <param name="progress">(output) Latest progress from a file upload, may be null if the upload file hasn't started</param>
        /// <returns>Returns any error that occurred in retrieving the latest progress, if any</returns>
        internal CLError GetProgressUploadFile(IAsyncResult aResult, out TransferProgress progress)
        {
            // try/catch to retrieve the latest progress, on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type of file uploads
                GenericAsyncResult<UploadFileResult> castAResult = aResult as GenericAsyncResult<UploadFileResult>;

                // if try casting the asynchronous result failed, throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // try to cast the asynchronous result internal state as the holder for the progress
                GenericHolder<TransferProgress> iState = castAResult.InternalState as GenericHolder<TransferProgress>;

                // if trying to cast the internal state as the holder for progress failed, then throw an error (non-descriptive since it's our error)
                if (iState == null)
                {
                    throw new Exception("There was an internal error attempting to retrieve the progress, Error 2");
                }

                // lock on the holder and retrieve the progress for output
                lock (iState)
                {
                    progress = iState.Value;
                }
            }
            catch (Exception ex)
            {
                progress = Helpers.DefaultForType<TransferProgress>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Finishes a file upload if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the file upload</param>
        /// <param name="result">(output) The result from the file upload</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUploadFile(IAsyncResult aResult, out UploadFileResult result)
        {
            // declare the specific type of asynchronous result for file uploads
            GenericAsyncResult<UploadFileResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for file uploads and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for file uploads
                castAResult = aResult as GenericAsyncResult<UploadFileResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<UploadFileResult>();
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
        /// Uploads a file from a provided stream and file upload change
        /// </summary>
        /// <param name="uploadStream">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, Metadata.StorageKey, and MD5 hash to be set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="message">(output) upload response message</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UploadFile(StreamContext streamContext,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out string message,
            out bool hashMismatchFound,
            CancellationTokenSource shutdownToken = null)
        {
            return UploadFile(
                streamContext,
                changeToUpload,
                timeoutMilliseconds,
                out message,
                out hashMismatchFound,
                shutdownToken,
                null,
                null,
                null,
                null,
                null);
        }

        // internal version with added action for status update
        internal CLError UploadFile(StreamContext streamContext,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out string message,
            out bool hashMismatchFound,
            CancellationTokenSource shutdownToken,
            FileTransferStatusUpdateDelegate statusUpdate,
            Guid statusUpdateId)
        {
            return UploadFile(
                streamContext,
                changeToUpload,
                timeoutMilliseconds,
                out message,
                out hashMismatchFound,
                shutdownToken,
                null,
                null,
                null,
                statusUpdate,
                statusUpdateId);
        }

        // private helper for UploadFile which takes additional parameters we don't wish to expose; does the actual processing
        private CLError UploadFile(StreamContext streamContext,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out string message,
            out bool hashMismatchFound,
            CancellationTokenSource shutdownToken,
            AsyncCallback aCallback,
            IAsyncResult aResult,
            GenericHolder<TransferProgress> progress,
            FileTransferStatusUpdateDelegate statusUpdate,
            Nullable<Guid> statusUpdateId)
        {
            // try/catch to process the file upload, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private upload class upon Helpers.ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathUpload + // path to upload
                    Helpers.QueryStringBuilder(new[] // add SyncboxId and DeviceId for file upload
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // query string parameter for the device id, needs to be escaped since it's client-defined
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId))
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication
                message = Helpers.ProcessHttp<string>(null, // the stream inside the upload parameter object is the request content, so no JSON contract object
                    CLDefinitions.CLUploadDownloadServerURL,  // Server URL
                    serverMethodPath, // dynamic upload path to add device id
                    Helpers.requestMethod.put, // upload is a put
                    timeoutMilliseconds, // time before communication timeout (does not restrict time for the actual file upload)
                    new Cloud.Static.Helpers.uploadParams( // this is a special communication method and requires passing upload parameters
                        streamContext, // stream for file to upload
                        Helpers.HandleUploadDownloadStatus, // private event handler to relay status change events
                        changeToUpload, // the FileChange describing the upload
                        shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                        _syncbox.Path, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                        aCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                        aResult, // asynchronous result to pass when firing the asynchronous callback
                        progress, // holder for progress data which can be queried by user if called via async wrapper
                        statusUpdate, // callback to user to notify when a CLSyncEngine status has changed
                        statusUpdateId), // userstate to pass to the statusUpdate callback
                    Helpers.HttpStatusesOkCreatedNotModified, // use the hashset for ok/created/not modified as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);

                hashMismatchFound = false;
            }
            catch (Exception ex)
            {
                hashMismatchFound = (ex is HashMismatchException);

                message = Helpers.DefaultForType<string>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetAllPending
        /// <summary>
        /// Asynchronously starts querying for all pending files
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllPending(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetAllPendingResult> toReturn = new GenericAsyncResult<GetAllPendingResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetAllPendingResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetAllPendingResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetAllPendingResult>, int> castState = state as Tuple<GenericAsyncResult<GetAllPendingResult>, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.PendingResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetAllPending(
                            castState.Item2,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetAllPendingResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes a query for all pending files if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pending query</param>
        /// <param name="result">(output) The result from the pending query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPending(IAsyncResult aResult, out GetAllPendingResult result)
        {
            // declare the specific type of asynchronous result for pending query
            GenericAsyncResult<GetAllPendingResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for pending query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for pending query
                castAResult = aResult as GenericAsyncResult<GetAllPendingResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<GetAllPendingResult>();
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
        /// Queries the server for a given sync box and device to get all files which are still pending upload
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPending(int timeoutMilliseconds, out JsonContracts.PendingResponse response)
        {
            // try/catch to process the pending query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the pending retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPending + // get pending
                    Helpers.QueryStringBuilder(new[] // grab parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the id of the device, escaped as needed for the URI
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)),
                        
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo();

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.PendingResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to get pending
                    Helpers.requestMethod.get, // get pending is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PendingResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region PostFileChange
        /// <summary>
        /// Asynchronously starts posting a single FileChange to the server
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="toCommunicate">Single FileChange to send</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginPostFileChange(AsyncCallback aCallback,
            object aState,
            FileChange toCommunicate,
            int timeoutMilliseconds,
            string serverUid,
            string revision)
        {
            // create the asynchronous result to return
            GenericAsyncResult<FileChangeResult> toReturn = new GenericAsyncResult<FileChangeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string> asyncParams =
                new Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string>(
                    toReturn,
                    toCommunicate,
                    timeoutMilliseconds,
                    serverUid,
                    revision);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string> castState = state as Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.FileChangeResponse response;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = PostFileChange(
                            castState.Item2,
                            castState.Item3,
                            out response,
                            castState.Item4,
                            castState.Item5);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new FileChangeResult(
                                    processError, // any error that may have occurred during processing
                                    response), // the specific type of result for this operation
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
        /// Finishes posting a FileChange if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the FileChange post</param>
        /// <param name="result">(output) The result from the FileChange post</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndPostFileChange(IAsyncResult aResult, out FileChangeResult result)
        {
            // declare the specific type of asynchronous result for FileChange post
            GenericAsyncResult<FileChangeResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for FileChange post and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for FileChange post
                castAResult = aResult as GenericAsyncResult<FileChangeResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<FileChangeResult>();
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
        /// Posts a single FileChange to the server to update the sync box in the cloud.
        /// May still require uploading a file with a returned storage key if the Header.Status property in response is "upload" or "uploading".
        /// Check Header.Status property in response for errors or conflict.
        /// </summary>
        /// <param name="toCommunicate">Single FileChange to send</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError PostFileChange(FileChange toCommunicate, int timeoutMilliseconds, out JsonContracts.FileChangeResponse response, string serverUid, string revision)
        {
            // try/catch to process the file change post, on catch return the error
            try
            {
                // check input parameters

                if (toCommunicate == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestCommunicateCannotBeNull);
                }
                if (toCommunicate.Direction == SyncDirection.From)
                {
                    throw new ArgumentException(Resources.CLHttpRestToCommunicateDirectionisNotToServer);
                }
                if (toCommunicate.Metadata == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestToCommunicateMetedataCannotBeNull);
                }
                if (toCommunicate.Type == FileChangeType.Modified
                    && toCommunicate.Metadata.HashableProperties.IsFolder)
                {
                    throw new ArgumentException(Resources.CLHttpRestToCommunicateCannotBeFolderandModified);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // build the location of the one-off method on the server dynamically
                string serverMethodPath;
                object requestContent;

                // set server method path and the request content dynamically based on whether change is a file or folder and based on the type of change
                switch (toCommunicate.Type)
                {
                    // file or folder created
                    case FileChangeType.Created:

                        // check additional parameters for file or folder creation

                        if (toCommunicate.NewPath == null)
                        {
                            throw new NullReferenceException(Resources.CLHttpRestNewPathCannotBeNull);
                        }

                        // if change is a folder, set path and create request content for folder creation
                        if (toCommunicate.Metadata.HashableProperties.IsFolder)
                        {
                            serverMethodPath = CLDefinitions.MethodPathOneOffFolderCreate;

                            requestContent = new JsonContracts.FolderAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _copiedSettings.DeviceId,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true) + "/",
                                SyncboxId = _syncbox.SyncboxId
                            };
                        }
                        // else if change is a file, set path and create request content for file creation
                        else
                        {
                            string addHashString = toCommunicate.GetMD5LowercaseString();

                            // check additional parameters for file creation

                            if (string.IsNullOrEmpty(addHashString))
                            {
                                throw new NullReferenceException(Resources.CLHttpRestMD5LowerCaseStringSet);
                            }
                            if (toCommunicate.Metadata.HashableProperties.Size == null)
                            {
                                throw new NullReferenceException(Resources.CLHttpRestMetadataHashablePropertiesSizeCannotBeNull);
                            }

                            serverMethodPath = CLDefinitions.MethodPathOneOffFileCreate;

                            requestContent = new JsonContracts.FileAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _copiedSettings.DeviceId,
                                Hash = addHashString,
                                MimeType = toCommunicate.Metadata.MimeType,
                                ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true),
                                Size = toCommunicate.Metadata.HashableProperties.Size,
                                SyncboxId = _syncbox.SyncboxId
                            };
                        }
                        break;

                    case FileChangeType.Deleted:

                        // check additional parameters for file or folder deletion

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(serverUid))
                        {
                            throw new NullReferenceException(Resources.CLHttpRestXORNewPathServerIDCannotBeNull);
                        }

                        // file deletion and folder deletion share a json contract object for deletion
                        requestContent = new JsonContracts.FileOrFolderDelete()
                        {
                            DeviceId = _copiedSettings.DeviceId,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true) +
                                    (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerUid = serverUid,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        // server method path switched from whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderDelete
                            : CLDefinitions.MethodPathOneOffFileDelete);
                        break;

                    case FileChangeType.Modified:

                        // grab MD5 hash string and rethrow any error that occurs

                        string modifyHashString = toCommunicate.GetMD5LowercaseString();

                        // check additional parameters for file modification

                        if (string.IsNullOrEmpty(modifyHashString))
                        {
                            throw new NullReferenceException(Resources.CLHttpRestMD5LowerCaseStringSet);
                        }
                        if (toCommunicate.Metadata.HashableProperties.Size == null)
                        {
                            throw new NullReferenceException(Resources.CLHttpRestMetadataHashablePropertiesSizeCannotBeNull);
                        }
                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(serverUid))
                        {
                            throw new NullReferenceException(Resources.CLHttpRestXORNewPathServerIDCannotBeNull);
                        }
                        if (string.IsNullOrEmpty(revision))
                        {
                            throw new NullReferenceException(Resources.CLHttpRestRevisionCannotBeNull);
                        }

                        // there is no folder modify, so json contract object and server method path for modify are only for files

                        requestContent = new JsonContracts.FileModify()
                        {
                            CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                            DeviceId = _copiedSettings.DeviceId,
                            Hash = modifyHashString,
                            MimeType = toCommunicate.Metadata.MimeType,
                            ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true)),
                            Revision = revision,
                            ServerUid = serverUid,
                            Size = toCommunicate.Metadata.HashableProperties.Size,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        serverMethodPath = CLDefinitions.MethodPathOneOffFileModify;
                        break;

                    case FileChangeType.Renamed:
                        // check additional parameters for file or folder move (rename)

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(serverUid))
                        {
                            throw new NullReferenceException(Resources.CLHttpRestXORNewPathServerIDCannotBeNull);
                        }
                        if (toCommunicate.OldPath == null)
                        {
                            throw new NullReferenceException(Resources.CLHttpRestOldPathCannotBeNull);
                        }

                        // file move (rename) and folder move (rename) share a json contract object for move (rename)
                        requestContent = new JsonContracts.FileOrFolderMove()
                        {
                            DeviceId = _copiedSettings.DeviceId,
                            RelativeFromPath = toCommunicate.OldPath.GetRelativePath(_syncbox.Path, true) +
                                (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty),
                            RelativeToPath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true)
                                    + (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerUid = serverUid,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        // server method path switched on whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderMove
                            : CLDefinitions.MethodPathOneOffFileMove);
                        break;

                    default:
                        throw new ArgumentException(Resources.CLHttpRestToCommunicateTypeIsUnknownFileChangeType + toCommunicate.Type.ToString());
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FileChangeResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileChangeResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetFileVersions
        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            bool includeDeletedVersions = false)
        {
            return BeginGetFileVersions(aCallback,
                aState,
                fileServerId,
                timeoutMilliseconds,
                null,
                includeDeletedVersions);
        }

        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions = false)
        {
            return BeginGetFileVersions(aCallback,
                aState,
                null,
                timeoutMilliseconds,
                pathToFile,
                includeDeletedVersions);
        }

        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback aCallback,
            object aState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions = false)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetFileVersionsResult> toReturn = new GenericAsyncResult<GetFileVersionsResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetFileVersionsResult>, string, int, FilePath, bool> asyncParams =
                new Tuple<GenericAsyncResult<GetFileVersionsResult>, string, int, FilePath, bool>(
                    toReturn,
                    fileServerId,
                    timeoutMilliseconds,
                    pathToFile,
                    includeDeletedVersions);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetFileVersionsResult>, string, int, FilePath, bool> castState = state as Tuple<GenericAsyncResult<GetFileVersionsResult>, string, int, FilePath, bool>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.FileVersions result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFileVersions(
                            castState.Item2,
                            castState.Item3,
                            castState.Item4,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetFileVersionsResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes querying for all versions of a given file if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFileVersions(IAsyncResult aResult, out GetFileVersionsResult result)
        {
            // declare the specific type of asynchronous result for querying file versions
            GenericAsyncResult<GetFileVersionsResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for querying file versions and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for querying file versions
                castAResult = aResult as GenericAsyncResult<GetFileVersionsResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<GetFileVersionsResult>();
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
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out JsonContracts.FileVersions response, bool includeDeletedVersions = false)
        {
            return GetFileVersions(fileServerId, timeoutMilliseconds, null, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        /* when they determine how they want to expose undelete, make this file versions public again */ internal CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersions response, bool includeDeletedVersions = false)
        {
            return GetFileVersions(null, timeoutMilliseconds, pathToFile, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out JsonContracts.FileVersions response, bool includeDeletedVersions = false)
        {
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new NullReferenceException(Resources.CLHttpRestXORPathtoFileFileServerIDMustNotBeNull);
                }

                // build the location of the file versions retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathFileGetVersions + // get file versions
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the device id
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)),

                        // query string parameter for the server id for the file to check, only filled in if it's not null
                        (string.IsNullOrEmpty(fileServerId)
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(fileServerId))),

                        // query string parameter for the path to the file to check, only filled in if it's not null
                        (pathToFile == null
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(pathToFile.GetRelativePath(_syncbox.Path, true)))),

                        // query string parameter for whether to include delete versions in the check, but only set if it's not default (if it's false)
                        (includeDeletedVersions
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeDeleted, "false")),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FileVersions>(null, // get file versions has no request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // use a dynamic method path because it needs query string parameters
                    Helpers.requestMethod.get, // get file versions is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileVersions>();
                return ex;
            }
            return null;
        }
        #endregion

        #region PurgePending
        /// <summary>
        /// Asynchronously purges any pending changes (pending file uploads) and outputs the files which were purged
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginPurgePending(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<PurgePendingResult> toReturn = new GenericAsyncResult<PurgePendingResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<PurgePendingResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<PurgePendingResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<PurgePendingResult>, int> castState = state as Tuple<GenericAsyncResult<PurgePendingResult>, int>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLHttpRestCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo());
                }
                // else if the try cast did not fail, then start processing with the input parameters
                else
                {
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        JsonContracts.PendingResponse result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = PurgePending(
                            castState.Item2,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new PurgePendingResult(
                                    processError, // any error that may have occurred during processing
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
        /// Finishes purging pending changes if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting purging pending</param>
        /// <param name="result">(output) The result from purging pending</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndPurgePending(IAsyncResult aResult, out PurgePendingResult result)
        {
            // declare the specific type of asynchronous result for purging pending
            GenericAsyncResult<PurgePendingResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for purging pending and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for purging pending
                castAResult = aResult as GenericAsyncResult<PurgePendingResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException(Resources.CLHttpRestaResultInternalTypeMismatch);
                }

                // pull the result for output (may not yet be complete)
                result = castAResult.Result;
            }
            catch (Exception ex)
            {
                result = Helpers.DefaultForType<PurgePendingResult>();
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
        /// Purges any pending changes (pending file uploads) and outputs the files which were purged
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError PurgePending(int timeoutMilliseconds, out JsonContracts.PendingResponse response)
        {
            // try/catch to process purging pending, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException(Resources.CLHttpRestTimeoutMustBeGreaterThanZero);
                }

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                response = Helpers.ProcessHttp<JsonContracts.PendingResponse>(new JsonContracts.PurgePending() // json contract object for purge pending method
                {
                    DeviceId = _copiedSettings.DeviceId,
                    SyncboxId = _syncbox.SyncboxId
                },
                    CLDefinitions.CLMetaDataServerURL,      // MDS server URL
                    CLDefinitions.MethodPathPurgePending, // purge pending address
                    Helpers.requestMethod.post, // purge pending is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // purge pending should give OK or Accepted
                    _copiedSettings, // pass the copied settings
                    _syncbox.SyncboxId, // pass the unique id of the sync box on the server
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PendingResponse>();
                return ex;
            }
            return null;
        }
        #endregion
        #endregion
    }
}
