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
using Cloud.Parameters;
using System.Threading.Tasks;
using Cloud.Parameters;
using Cloud.Callbacks;

namespace Cloud.REST
{
    // CLCredentials class has additional HTTP calls which do not require a Syncbox id
    /// <summary>
    /// Client for manual HTTP communication calls to the Cloud
    /// </summary>
    internal sealed class CLHttpRest
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

        // Syncbox associated with this CLHttpRest object.
        private CLSyncbox _syncbox;

        #endregion

        #region Constructors and Factories

        // private constructor requiring settings to copy and store for the life of this http client
        private CLHttpRest(
            CLSyncbox syncbox, 
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback,
            object getNewCredentialsCallbackUserState)
        {
            if (syncbox == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Arguments, Resources.SyncboxMustNotBeNull);
            }

            if (syncbox.Path == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Arguments, Resources.CLHttpRestSyncboxPathCannotBeNull);
            }

            if (syncbox.Credentials == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Arguments, Resources.CLHttpRestsyncboxCredentialCannotBeNull);
            }

            this._syncbox = syncbox;

            if (string.IsNullOrEmpty(this._syncbox.CopiedSettings.DeviceId))
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Arguments, Resources.CLHttpRestDeviceIDCannotBeNull);
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
        /// <param name="syncboxId">ID of sync box which can be manually synced</param>
        /// <param name="client">(output) Created CLHttpRest client</param>
        /// <returns>Returns any error creating the CLHttpRest client, if any</returns>
        internal static CLError CreateAndInitialize(
            CLSyncbox syncbox, 
            out CLHttpRest client, 
            Helpers.ReplaceExpiredCredentials getNewCredentialsCallback = null,
            object getNewCredentialsCallbackUserState = null)
        {
            try
            {
                client = new CLHttpRest(syncbox, getNewCredentialsCallback, getNewCredentialsCallbackUserState);
            }
            catch (Exception ex)
            {
                client = Helpers.DefaultForType<CLHttpRest>();
                return ex;
            }
            return null;
        }

        #endregion  // end Constructors and Factories

        #region Internal Methods Supporting Public API Calls
        #region DownloadFile
        /// <summary>
        /// Asynchronously starts downloading a file from a provided file download change
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire upon progress changes in download, make sure it processes quickly if the IAsyncResult IsCompleted is false</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="changeToDownload">File download change, requires Metadata.</param>
        /// <param name="moveFileUponCompletion">¡¡ Action required: move the completed download file from the temp directory to the final destination !! Callback fired when download completes</param>
        /// <param name="moveFileUponCompletionState">User state passed upon firing completed download callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file download</param>
        /// <param name="beforeDownload">(optional) Callback fired before a download starts</param>
        /// <param name="beforeDownloadState">User state passed upon firing before download callback</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the download</param>
        /// <param name="customDownloadFolderFullPath">(optional) Full path to a folder where temporary downloads will be stored to override default</param>
        /// <returns>Returns the asynchronous result which is used to retrieve progress and/or the result</returns>
        public IAsyncResult BeginDownloadFile(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
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
                asyncCallback,
                asyncCallbackUserState,
                progressHolder);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, 
                Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>> asyncParams =
                new Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, 
                    Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>>(
                    toReturn,
                    asyncCallback,
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
                Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, 
                    Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>> castState = state as 
                    Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, string, string, Helpers.AfterDownloadToTempFile, object, 
                        Tuple<int, Helpers.BeforeDownloadToTempFile, object, CancellationTokenSource, string>>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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

        // This is not currently used, but it could be used by forwarding it in CLFileItem to poll the progress of a Begin/EndDownload async operation.
        /// <summary>
        /// Outputs the latest progress from a file download, returning any error that occurs in the retrieval
        /// </summary>
        /// <param name="asyncResult">Asynchronous result originally returned by BeginDownloadFile</param>
        /// <param name="progress">(output) Latest progress from a file download, may be null if the download file hasn't started</param>
        /// <returns>Returns any error that occurred in retrieving the latest progress, if any</returns>
        public CLError GetProgressDownloadFile(IAsyncResult asyncResult, out TransferProgress progress)
        {
            // try/catch to retrieve the latest progress, on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type of file downloads
                GenericAsyncResult<DownloadFileResult> castAResult = asyncResult as GenericAsyncResult<DownloadFileResult>;

                // if try casting the asynchronous result failed, throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_ObjectNotExpectedType, Resources.CLAsyncResultInternalTypeMismatch);
                }

                // try to cast the asynchronous result internal state as the holder for the progress
                GenericHolder<TransferProgress> iState = castAResult.InternalState as GenericHolder<TransferProgress>;

                // if trying to cast the internal state as the holder for progress failed, then throw an error (non-descriptive since it's our error)
                if (iState == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_ObjectNotExpectedType, Resources.CLHttpRestInternalProgressRetrievalFailure1);
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
        /// <param name="asyncResult">The asynchronous result provided upon starting the file download</param>
        /// <param name="result">(output) The result from the file download</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDownloadFile(IAsyncResult asyncResult, out DownloadFileResult result)
        {
            return Helpers.EndAsyncOperation<DownloadFileResult>(asyncResult, out result);
        }

        /// <summary>
        /// Downloads a file from a provided file download change
        /// </summary>
        /// <param name="changeToDownload">File download change, requires Metadata.</param>
        /// <param name="moveFileUponCompletion">¡¡ Action required: move the completed download file from the temp directory to the final destination !! Callback fired when download completes</param>
        /// <param name="moveFileUponCompletionState">User state passed upon firing completed download callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file download</param>
        /// <param name="beforeDownload">(optional) Callback fired before a download starts</param>
        /// <param name="beforeDownloadState">User state passed upon firing before download callback</param>
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
            object statusUpdateUserState)
        {
            return DownloadFile(
                changeToDownload,
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
                statusUpdateUserState);
        }

        // private helper for DownloadFile which takes additional parameters we don't wish to expose; does the actual processing
        private CLError DownloadFile(
            FileChange changeToDownload,
            string serverUid,
            string revision,
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            Helpers.BeforeDownloadToTempFile beforeDownload,
            object beforeDownloadState,
            CancellationTokenSource shutdownToken,
            string customDownloadFolderFullPath,
            AsyncCallback asyncCallback,
            IAsyncResult asyncResult,
            GenericHolder<TransferProgress> progress,
            FileTransferStatusUpdateDelegate statusUpdate,
            object statusUpdateUserState)
        {
            // try/catch to process the file download, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private download class upon Helpers.ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                if (serverUid == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionCLHttpRestNullServerUid);
                }

                if (revision == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_InvalidParameters, Resources.CLHttpRestMetaDataRevisionCannotBeNull);
                }

                // declare the path for the folder which will store temp download files
                string currentDownloadFolder;

                // if a specific folder path was passed to use as an override, then store it as the one to use
                if (customDownloadFolderFullPath != null)
                {
                    currentDownloadFolder = customDownloadFolderFullPath;
                }
                // else if a specified folder path was not passed and a path was specified in settings, then store the one from settings as the one to use
                else if (!String.IsNullOrWhiteSpace(_syncbox.CopiedSettings.TempDownloadFolderFullPath))
                {
                    currentDownloadFolder = _syncbox.CopiedSettings.TempDownloadFolderFullPath;
                }
                // else if a specified folder path was not passed and one did not exist in settings, then build one dynamically to use
                else
                {
                    currentDownloadFolder = Helpers.GetTempFileDownloadPath(_syncbox.CopiedSettings, _syncbox.SyncboxId);
                }

                // check if the folder for temp downloads represents a bad path
                CLError badTempFolderError = Helpers.CheckForBadPath(currentDownloadFolder);

                // if the temp download folder is a bad path rethrow the error
                if (badTempFolderError != null)
                {
                    throw new CLException(CLExceptionCode.OnDemand_Settings, Resources.CLHttpRestThecustomDownloadFolderFullPathIsBad, badTempFolderError.Exceptions);
                }

                // if the folder path for downloads is too long, then throw an exception
                if (currentDownloadFolder.Length > 222) // 222 calculated by 259 max path length minus 1 character for a folder slash seperator plus 36 characters for (Guid).ToString(Resources.CLCredentialStringSettingsN)
                {
                    throw new CLException(CLExceptionCode.OnDemand_Settings, Resources.CLHttpRestFolderPathTooLong + (currentDownloadFolder.Length - 222).ToString());
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
                        Helpers.GetTempFileDownloadPath(_syncbox.CopiedSettings, _syncbox.SyncboxId), // if custom path not provided, null-coallesce to default
                    Helpers.HandleUploadDownloadStatus, // private event handler to relay status change events
                    changeToDownload, // the FileChange describing the download
                    shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                    _syncbox.Path, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    asyncCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                    asyncResult, // asynchronous result to pass when firing the asynchronous callback
                    progress, // holder for progress data which can be queried by user if called via async wrapper
                    statusUpdate, // callback to user to notify when a CLSyncEngine status has changed
                    statusUpdateUserState, // userstate to pass to the statusUpdate callback
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

                     // JSON contract to serialize
                     new Download()
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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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

        #region DownloadImageOfSize (Download an image file and return a Stream with the data)
        /// <summary>
        /// Asynchronously starts downloading an image file in the desired size.  Outputs a Stream.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="fileItem">The file item to download.</param>
        /// <param name="imageSize">The size of the image to download (small, medium, large or thumbnail).</param>
        /// <param name="transferStatusCallback">(optional) The transfer progress delegate to call.  May be null.</param>
        /// <param name="transferStatusCallbackUserState">(optional) The user state to pass to the transfer progress delegate above.  May be null.</param>
        /// <param name="cancellationSource">A cancellation token source object that can be used to cancel the download operation.  May be null</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginDownloadImageOfSize(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItem fileItem,
            Cloud.CLFileItem.CLFileItemImageSize imageSize,
            CLFileDownloadTransferStatusCallback transferStatusCallback, 
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<FileItemDownloadImageResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                        fileItem = fileItem,
                        imageSize = imageSize,
                        transferStatusCallback = transferStatusCallback,
                        transferStatusCallbackUserState = transferStatusCallbackUserState,
                        cancellationSource = cancellationSource,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        Stream stream;
                        CLError overallError = DownloadImageOfSize(
                            Data.fileItem,
                            Data.imageSize,
                            out stream,
                            Data.transferStatusCallback,
                            Data.transferStatusCallbackUserState,
                            Data.cancellationSource);

                        Data.toReturn.Complete(
                            new FileItemDownloadImageResult(overallError, stream),  // the result to return
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
        /// Finishes downloading the image file, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndDownloadImageOfSize(IAsyncResult asyncResult, out FileItemDownloadImageResult result)
        {
            return Helpers.EndAsyncOperation<FileItemDownloadImageResult>(asyncResult, out result);
        }

        /// <summary>
        /// Download an image file in the desired size.  Outputs a Stream.
        /// </summary>
        /// <param name="fileItem">The file item to download.</param>
        /// <param name="imageSize">The size of the image to download (small, medium, large or thumbnail).</param>
        /// <param name="imageStream">(Output) The returned Stream representing the image data.</param>
        /// <param name="transferStatusCallback">(optional) The transfer progress delegate to call.  May be null.</param>
        /// <param name="transferStatusCallbackUserState">(optional) The user state to pass to the transfer progress delegate above.  May be null.</param>
        /// <param name="cancellationSource">A cancellation token source object that can be used to cancel the download operation.  May be null</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError DownloadImageOfSize(
            CLFileItem fileItem,
            Cloud.CLFileItem.CLFileItemImageSize imageSize,
            out Stream imageStream, 
            CLFileDownloadTransferStatusCallback transferStatusCallback, 
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                // Determine the size to use
                char charSize;
                string generate = CLDefinitions.CLMetadataFalse;
                switch (imageSize)
                {
                    case CLFileItem.CLFileItemImageSize.CLFileItemImageSizeSmall:
                        charSize = (char)0x73 /* 's' */;
                        break;
                    case CLFileItem.CLFileItemImageSize.CLFileItemImageSizeMedium:
                        charSize = (char)0x6D /* 'm' */;
                        break;
                    case CLFileItem.CLFileItemImageSize.CLFileItemImageSizeLarge:
                        charSize = (char)0x6C /* 'l' */;
                        break;
                    case CLFileItem.CLFileItemImageSize.CLFileItemImageSizeFull:
                        charSize = (char)0x6F /* 'o' */;
                        break;
                    case CLFileItem.CLFileItemImageSize.CLFileItemImageSizeThumbnail:
                        charSize = (char)0x74 /* 't' */;
                        generate = CLDefinitions.CLMetadataInline;
                        break;
                    default:
                        throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandDownloadImageOfSizeInvalidImageSizeValue);
                }

                // Now make the REST request content.
                object requestContent = new JsonContracts.ImageRequest()
                {
                    SyncboxId = _syncbox.SyncboxId,
                    ServerUid = fileItem.ItemUid,
                    Revision = fileItem.Revision,
                    Generate = generate,
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathImage + ((char)0x2F /* '/' */) + charSize +
                    Helpers.QueryStringBuilder(new[] // the method grabs its parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });


                imageStream = Helpers.ProcessHttpRawStreamCopy(
                    requestContent, // JSON contract object to serialize and send up as the request content, if any
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // the server method path
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    /* uploadDownload */ null,  // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    isOneOff: true,  // On Demand call
                    transferStatusCallback: transferStatusCallback,  // the transfer progress callback
                    transferStatusCallbackUserState: transferStatusCallbackUserState,  // the transfer progress callback user state
                    cancellationSource: cancellationSource);  // the token to request cancellation

                // make sure response was returned
                if (imageStream == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandDownloadImageOfSizeFileNotFound);
                }
            }
            catch (Exception ex)
            {
                imageStream = Helpers.DefaultForType<Stream>();
                return ex;
            }

            return null;
        }

        #endregion  // end DownloadImageOfSize (Download an image file and return a Stream with the data)

        #region ItemForPath (Gets the metedata at a particular server syncbox path)
        /// <summary>
        /// Asynchronously starts querying the syncbox for an item at a given path (must be specified) for existing metadata at that path; outputs a CLFileItem object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="relativePath">Relative path in the syncbox to where file or folder would exist in the syncbox locally on disk.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginItemForPath(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string relativePath)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetItemAtPathResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    relativePath = relativePath,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem fileItem;
                        CLError overallError = ItemForPath(
                            Data.relativePath,
                            out fileItem);

                        Data.toReturn.Complete(
                            new SyncboxGetItemAtPathResult(overallError, fileItem),  // the result to return
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
        /// Finishes getting an item in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndItemForPath(IAsyncResult asyncResult, out SyncboxGetItemAtPathResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetItemAtPathResult>(asyncResult, out result);
        }

        /// <summary>
        /// Get an item at a particular path in the syncbox.
        /// </summary>
        /// <param name="relativePath">Relative path in the syncbox to where file or folder would exist in the syncbox locally on disk.</param>
        /// <param name="item">(output) The returned item.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError ItemForPath(string relativePath, out CLFileItem item)
        {
            CLFileItem toReturn;

            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.

                if (relativePath == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandPathMustNotBeNull);
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

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath = CLDefinitions.MethodPathGetItemMetadata +
                    Helpers.QueryStringBuilder(new[] // the method grabs its parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the path to query, built by turning the full path location into a relative path from the cloud root and then escaping the whole thing for a url
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(relativePath.Replace(((char)0x5C /* '\' */), ((char)0x2F /* '/' */)))),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMetadataResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMetadataResponse>(null, // no content body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert the metadata to the output item.
                if (responseFromServer != null)
                {
                    // Pass back the response as a CLFileItem.
                    toReturn = new CLFileItem(responseFromServer, _syncbox);
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandItemForPathNotFound);
                }
            }
            catch (Exception ex)
            {
                item = Helpers.DefaultForType<CLFileItem>();
                return ex;
            }

            item = toReturn;
            return null;
        }

        #endregion  // end ItemForPath (Gets the metedata at a particular server syncbox path)

        #region ItemForItemUid (Returns a CLFileItem for the syncbox item with the given UID.)
        /// <summary>
        /// Asynchronously starts querying the syncbox for an item with the given UID. Outputs a CLFileItem object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="itemUid">The UID to use in the query.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginItemForItemUid(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string itemUid)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxGetItemAtItemUidResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    itemUid = itemUid,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem fileItem;
                        CLError overallError = ItemForItemUid(
                            Data.itemUid,
                            out fileItem);

                        Data.toReturn.Complete(
                            new SyncboxGetItemAtItemUidResult(overallError, fileItem),  // the result to return
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
        /// Finishes getting an item in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndItemForItemUid(IAsyncResult asyncResult, out SyncboxGetItemAtItemUidResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxGetItemAtItemUidResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query the syncbox for an item with the given UID. Outputs a CLFileItem object.
        /// </summary>
        /// <param name="itemUid">The UID to use in the query.</param>
        /// <param name="item">(output) The returned item.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError ItemForItemUid(string itemUid, out CLFileItem item)
        {
            CLFileItem toReturn;

            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (itemUid == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandItemUidMustNotBeNull);
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

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath = CLDefinitions.MethodPathGetItemMetadata +
                    Helpers.QueryStringBuilder(new[] // the method grabs its parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the server UID to query
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(itemUid)),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString())
                    });

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMetadataResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMetadataResponse>(null, // no content body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert the metadata to the output item.
                if (responseFromServer != null)
                {
                    // Pass back the response as a CLFileItem.
                    toReturn = new CLFileItem(responseFromServer, _syncbox);
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandItemForPathNotFound);
                }
            }
            catch (Exception ex)
            {
                item = Helpers.DefaultForType<CLFileItem>();
                return ex;
            }

            item = toReturn;
            return null;
        }

        #endregion  // end ItemForItemUid (Returns a CLFileItem for the syncbox item with the given UID.)

        #region RenameFiles (Rename files in-place in the syncbox.)
        /// <summary>
        /// Asynchronously starts renaming files in-place in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemParams">One or more parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginRenameFiles(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params RenameItemParams[] itemParams)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
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
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.itemParams);

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
        /// Finishes renaming files in-place in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndRenameFiles(IAsyncResult asyncResult, out SyncboxRenameFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxRenameFilesResult>(asyncResult, out result);
        }

        /// <summary>
        /// Rename files in-place in the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToRename">One or more parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError RenameFiles(
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState,
            params RenameItemParams[] itemsToRename)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.
                if (itemsToRename == null
                    || itemsToRename.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[itemsToRename.Length];

                for (int paramIdx = 0; paramIdx < itemsToRename.Length; paramIdx++)
                {
                    RenameItemParams currentParams = itemsToRename[paramIdx];
                    if (currentParams == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FileRename, String.Format(Resources.ExceptionOnDemandFileItemNullAtIndexMsg0, paramIdx.ToString()));
                    }
                    if (currentParams.ItemToRename == null)
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandItemToRenameMustNotBeNull);
                    }
                    if (currentParams.ItemToRename.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (currentParams.ItemToRename.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FolderItemWhenFileItemExpected, String.Format(Resources.ExceptionOnDemandFolderItemFoundWhenFileItemExpectedMsg0, paramIdx));
                    }
                    if (currentParams.ItemToRename.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0 , paramIdx));
                    }
                    if (String.IsNullOrEmpty(currentParams.NewName))
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandNewNameMustBeSpecified);
                    }

                    // file move (rename) and folder move (rename) share a json contract object for move (rename)
                    jsonContractMoves[paramIdx] = new FileOrFolderMove()
                    {
                        ToName = currentParams.NewName,
                        ServerUid = currentParams.ItemToRename.ItemUid,
                    };
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    DeviceId = _syncbox.CopiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFileMoves;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(
                    requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.MoveResponses != null)
                {
                    if (responseFromServer.MoveResponses.Length != itemsToRename.Length)
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
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
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
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionCLHttpRestWithoutRenameResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end RenameFiles (Renames files in-place in the syncbox.)

        #region RenameFolders (Rename folders in-place in the syncbox.)
        /// <summary>
        /// Asynchronously starts renaming folders in-place in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToRename">One or more parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginRenameFolders(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params RenameItemParams[] itemsToRename)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    itemParams = itemsToRename
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = RenameFolders(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.itemParams);

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
        /// Finishes renaming folders in-place in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndRenameFolders(IAsyncResult asyncResult, out SyncboxRenameFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxRenameFoldersResult>(asyncResult, out result);
        }

        /// <summary>
        /// Rename folders in-place in the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToRename">One or more parameter pairs (item to rename and new name) to be used to rename each item in place.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError RenameFolders(
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params RenameItemParams[] itemsToRename)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.
                if (itemsToRename == null
                    || itemsToRename.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[itemsToRename.Length];

                for (int paramIdx = 0; paramIdx < itemsToRename.Length; paramIdx++)
                {
                    RenameItemParams currentParams = itemsToRename[paramIdx];
                    if (currentParams == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FileRename, String.Format(Resources.ExceptionOnDemandFolderItemNullAtIndexMsg0, paramIdx.ToString()));
                    }
                    if (currentParams.ItemToRename == null)
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandItemToRenameMustNotBeNull);
                    }
                    if (currentParams.ItemToRename.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (!currentParams.ItemToRename.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, String.Format(Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpectedMsg0, paramIdx));
                    }
                    if (currentParams.ItemToRename.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, paramIdx));
                    }
                    if (String.IsNullOrEmpty(currentParams.NewName))
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandNewNameMustBeSpecified);
                    }

                    // file move (rename) and folder move (rename) share a json contract object for move (rename)
                    jsonContractMoves[paramIdx] = new FileOrFolderMove()
                    {
                        ToName = currentParams.NewName,
                        ServerUid = currentParams.ItemToRename.ItemUid,
                    };
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    DeviceId = _syncbox.CopiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderMoves;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.MoveResponses != null)
                {
                    if (responseFromServer.MoveResponses.Length != itemsToRename.Length)
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
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
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
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end RenameFolders (Renames folders in-place in the syncbox.)

        #region MoveFiles (Move files in the syncbox.)
        /// <summary>
        /// Asynchronously starts moving files in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginMoveFiles(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params MoveItemParams[] itemsToMove)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    itemParams = itemsToMove
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = MoveFiles(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.itemParams);

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
        /// Finishes moving files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndMoveFiles(IAsyncResult asyncResult, out SyncboxMoveFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxMoveFilesResult>(asyncResult, out result);
        }

        /// <summary>
        /// Move files in the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError MoveFiles(
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params MoveItemParams[] itemsToMove)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.
                if (itemsToMove == null
                    || itemsToMove.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[itemsToMove.Length];

                for (int paramIdx = 0; paramIdx < itemsToMove.Length; paramIdx++)
                {
                    MoveItemParams currentParams = itemsToMove[paramIdx];
                    if (currentParams == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FileRename, String.Format(Resources.ExceptionOnDemandFileItemNullAtIndexMsg0, paramIdx));
                    }
                    if (currentParams.ItemToMove == null)
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MoveItemParamsMissingProperties, Resources.ExceptionOnDemandItemToMoveMustNotBeNull);
                    }
                    if (currentParams.ItemToMove.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (currentParams.ItemToMove.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FolderItemWhenFileItemExpected, String.Format(Resources.ExceptionOnDemandFolderItemFoundWhenFileItemExpectedMsg0, paramIdx));
                    }
                    if (currentParams.ItemToMove.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, paramIdx));
                    }
                    if (currentParams.NewParentFolderItem == null)
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MoveItemParamsMissingProperties, Resources.ExceptionOnDemandNewParentFolderItemMustBeSpecified);
                    }

                    // file move (rename) and folder move (rename) share a json contract object for move (rename)
                    jsonContractMoves[paramIdx] = new FileOrFolderMove()
                    {
                        ToParentUid = currentParams.NewParentFolderItem.ItemUid,
                        ServerUid = currentParams.ItemToMove.ItemUid,
                    };
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    DeviceId = _syncbox.CopiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFileMoves;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.MoveResponses != null)
                {
                    if (responseFromServer.MoveResponses.Length != itemsToMove.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

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
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
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
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionCLHttpRestWithoutRenameResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end MoveFiles (Move files in the syncbox.)

        #region MoveFolders (Move folders in the syncbox.)
        /// <summary>
        /// Asynchronously starts moving folders in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginMoveFolders(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params MoveItemParams[] itemsToMove)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    itemParams = itemsToMove
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = MoveFolders(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.itemParams);

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
        /// Finishes moving folders in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndMoveFolders(IAsyncResult asyncResult, out SyncboxMoveFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxMoveFoldersResult>(asyncResult, out result);
        }

        /// <summary>
        /// Move folders in the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToMove">One or more pairs of item to move and a folder item representing the new parent of the item being moved.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError MoveFolders(
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params MoveItemParams[] itemsToMove)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.
                if (itemsToMove == null
                    || itemsToMove.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                FileOrFolderMove[] jsonContractMoves = new FileOrFolderMove[itemsToMove.Length];

                for (int paramIdx = 0; paramIdx < itemsToMove.Length; paramIdx++)
                {
                    MoveItemParams currentParams = itemsToMove[paramIdx];
                    if (currentParams == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FileRename, String.Format(Resources.ExceptionOnDemandFolderItemNullAtIndexMsg0, paramIdx.ToString()));
                    }
                    if (currentParams.ItemToMove == null)
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MoveItemParamsMissingProperties, Resources.ExceptionOnDemandItemToMoveMustNotBeNull);
                    }
                    if (currentParams.ItemToMove.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (!currentParams.ItemToMove.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, String.Format(Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpectedMsg0, paramIdx));
                    }
                    if (currentParams.ItemToMove.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, paramIdx));
                    }
                    if (currentParams.NewParentFolderItem == null)
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MoveItemParamsMissingProperties, Resources.ExceptionOnDemandNewParentFolderItemMustBeSpecified);
                    }


                    // file move (rename) and folder move (rename) share a json contract object for move (rename)
                    jsonContractMoves[paramIdx] = new FileOrFolderMove()
                    {
                        ToParentUid = currentParams.NewParentFolderItem.ItemUid,
                        ServerUid = currentParams.ItemToMove.ItemUid,
                    };
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    DeviceId = _syncbox.CopiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderMoves;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxMoveFilesOrFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxMoveFilesOrFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.MoveResponses != null)
                {
                    if (responseFromServer.MoveResponses.Length != itemsToMove.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FolderRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

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
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
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
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end RenameFolders (Renames folders in the syncbox.)

        #region DeleteFiles (Delete files in the syncbox.)
        /// <summary>
        /// Asynchronously starts deleting files in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginDeleteFiles(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params CLFileItem[] itemsToDelete)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    itemsToDelete = itemsToDelete
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = DeleteFiles(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.itemsToDelete);

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
        /// Finishes deleting files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndDeleteFiles(IAsyncResult asyncResult, out SyncboxDeleteFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxDeleteFilesResult>(asyncResult, out result);
        }

        /// <summary>
        /// Delete files in the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more file items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError DeleteFiles(
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params CLFileItem[] itemsToDelete)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }

                IncrementModifyingSyncboxViaPublicAPICalls();
                // check input parameters.
                if (itemsToDelete == null
                    || itemsToDelete.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                string[] jsonContractDeletes = new string[itemsToDelete.Length];

                for (int paramIdx = 0; paramIdx < itemsToDelete.Length; paramIdx++)
                {
                    CLFileItem currentFileItem = itemsToDelete[paramIdx];
                    if (currentFileItem == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FileRename, String.Format(Resources.ExceptionOnDemandFileItemNullAtIndexMsg0, paramIdx.ToString()));
                    }
                    if (currentFileItem.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (currentFileItem.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FolderItemWhenFileItemExpected, String.Format(Resources.ExceptionOnDemandFolderItemFoundWhenFileItemExpectedMsg0, paramIdx));
                    }
                    if (currentFileItem.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, paramIdx));
                    }

                    jsonContractDeletes[paramIdx] = currentFileItem.ItemUid;
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                object requestContent = new JsonContracts.FileOrFolderDeletesRequest()
                {
                    SyncboxId = _syncbox.SyncboxId,
                    Deletes = jsonContractDeletes,
                    DeviceId = _syncbox.CopiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFileDeletes;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxDeleteFilesResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteFilesResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.DeleteResponses != null)
                {
                    if (responseFromServer.DeleteResponses.Length != itemsToDelete.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();

                    for (int responseIdx = 0; responseIdx < responseFromServer.DeleteResponses.Length; responseIdx++)
                    {
                        try
                        {
                            FileChangeResponse currentDeleteResponse = responseFromServer.DeleteResponses[responseIdx];

                            if (currentDeleteResponse == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                            }
                            if (currentDeleteResponse.Header == null || string.IsNullOrEmpty(currentDeleteResponse.Header.Status))
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                            }
                            if (currentDeleteResponse.Metadata == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                            }

                            switch (currentDeleteResponse.Header.Status)
                            {
                                case CLDefinitions.CLEventTypeAccepted:
                                case CLDefinitions.CLEventTypeAlreadyDeleted:   // user said delete, and it is deleted.  
                                    CLFileItem resultItem = new CLFileItem(currentDeleteResponse.Metadata, currentDeleteResponse.Header.Action, currentDeleteResponse.Action, _syncbox);
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    break;

                                case CLDefinitions.CLEventTypeNotFound:
                                    throw new CLException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandNotFound);

                                case CLDefinitions.RESTResponseStatusFailed:
                                    Exception innerEx;
                                    string errorMessageString;
                                    try
                                    {
                                        errorMessageString = string.Join(Environment.NewLine, currentDeleteResponse.Metadata.ErrorMessage);
                                        innerEx = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                        innerEx = ex;
                                    }

                                    throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                default:
                                    throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentDeleteResponse.Header.Status));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileDelete, Resources.ExceptionCLHttpRestWithoutDeleteResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end DeleteFiles (Deletes files in the syncbox.)

        #region DeleteFolders (Delete folders in the syncbox.)
        /// <summary>
        /// Asynchronously starts deleting folders in the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more folder items to delete.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginDeleteFolders(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params CLFileItem[] itemsToDelete)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    itemsToDelete = itemsToDelete
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = DeleteFolders(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.itemsToDelete);

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
        /// Finishes deleting folders in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndDeleteFolders(IAsyncResult asyncResult, out SyncboxDeleteFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxDeleteFoldersResult>(asyncResult, out result);
        }

        /// <summary>
        /// Delete folders in the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="itemsToDelete">One or more folder items to delete.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError DeleteFolders(
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params CLFileItem[] itemsToDelete)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.
                if (itemsToDelete == null
                    || itemsToDelete.Length == 0)
                {
                    throw new CLArgumentNullException(
                        CLExceptionCode.OnDemand_RenameMissingParameters,
                        Resources.ExceptionOnDemandRenameMissingParameters);
                }

                string[] jsonContractDeletes = new string[itemsToDelete.Length];

                for (int paramIdx = 0; paramIdx < itemsToDelete.Length; paramIdx++)
                {
                    CLFileItem currentFolderItem = itemsToDelete[paramIdx];
                    if (currentFolderItem == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FileRename, String.Format(Resources.ExceptionOnDemandFileItemNullAtIndexMsg0, paramIdx.ToString()));
                    }
                    if (currentFolderItem.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (!currentFolderItem.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, String.Format(Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpectedMsg0, paramIdx));
                    }
                    if (currentFolderItem.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, paramIdx));
                    }

                    jsonContractDeletes[paramIdx] = currentFolderItem.ItemUid;
                    //{
                        //DeviceId = _syncbox.CopiedSettings.DeviceId,
                        
                        //SyncboxId = _syncbox.SyncboxId
                    //};
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                object requestContent = new JsonContracts.FileOrFolderDeletesRequest()
                {
                    SyncboxId = _syncbox.SyncboxId,
                    Deletes = jsonContractDeletes,
                    DeviceId = _syncbox.CopiedSettings.DeviceId
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderDeletes;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxDeleteFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxDeleteFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.DeleteResponses != null)
                {
                    if (responseFromServer.DeleteResponses.Length != itemsToDelete.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();

                    for (int responseIdx = 0; responseIdx < responseFromServer.DeleteResponses.Length; responseIdx++)
                    {
                        try
                        {
                            FileChangeResponse currentDeleteResponse = responseFromServer.DeleteResponses[responseIdx];

                            if (currentDeleteResponse == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                            }
                            if (currentDeleteResponse.Header == null || string.IsNullOrEmpty(currentDeleteResponse.Header.Status))
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                            }
                            if (currentDeleteResponse.Metadata == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                            }

                            switch (currentDeleteResponse.Header.Status)
                            {
                                case CLDefinitions.CLEventTypeAccepted:
                                case CLDefinitions.CLEventTypeAlreadyDeleted:   // user said delete, and it is deleted.  
                                    CLFileItem resultItem = new CLFileItem(currentDeleteResponse.Metadata, currentDeleteResponse.Header.Action, currentDeleteResponse.Action, _syncbox);
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    break;

                                case CLDefinitions.CLEventTypeNotFound:
                                    throw new CLException(CLExceptionCode.OnDemand_NotFound, Resources.ExceptionOnDemandNotFound);

                                case CLDefinitions.RESTResponseStatusFailed:
                                    Exception innerEx;
                                    string errorMessageString;
                                    try
                                    {
                                        errorMessageString = string.Join(Environment.NewLine, currentDeleteResponse.Metadata.ErrorMessage);
                                        innerEx = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                        innerEx = ex;
                                    }

                                    throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                default:
                                    throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentDeleteResponse.Header.Status));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_FolderDelete, Resources.ExceptionCLHttpRestWithoutDeleteResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end DeleteFolders (Delete folders in the syncbox.)

        #region AddFolders (Add folders to a particular parent folder in the syncbox.)
        /// <summary>
        /// Asynchronously starts adding folders to the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="folderItemsToAdd">One or more pairs of parent folder item and folder name to add.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAddFolders(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params AddFolderItemParams[] folderItemsToAdd)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    foldersToAdd = folderItemsToAdd
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = AddFolders(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.foldersToAdd);

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
        /// Finishes adding folders to the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAddFolders(IAsyncResult asyncResult, out SyncboxAddFoldersResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAddFoldersResult>(asyncResult, out result);
        }

        /// <summary>
        /// Add folders to the syncbox.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="folderItemsToAdd">One or more pairs of parent folder item and folder name to add.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AddFolders(
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState, 
            params AddFolderItemParams[] folderItemsToAdd)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.
                if (folderItemsToAdd == null
                    || folderItemsToAdd.Length == 0)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandAddFoldersNoFoldersToAdd);
                }

                FolderAddRequest[] jsonContractAdds = new FolderAddRequest[folderItemsToAdd.Length];

                for (int paramIdx = 0; paramIdx < folderItemsToAdd.Length; paramIdx++)
                {
                    CLFileItem currentFolderItem = folderItemsToAdd[paramIdx].Parent;
                    string currentFolderName = folderItemsToAdd[paramIdx].Name;
                    if (currentFolderItem == null)
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_FolderRename, String.Format(Resources.ExceptionOnDemandFolderItemNullAtIndexMsg0, paramIdx.ToString()));
                    }
                    if (currentFolderItem.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, paramIdx));
                    }
                    if (!currentFolderItem.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, String.Format(Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpectedMsg0, paramIdx));
                    }
                    if (currentFolderItem.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, paramIdx));
                    }
                    if (String.IsNullOrEmpty(currentFolderName))
                    {
                        throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandAddItemNameMustBeSpecified);
                    }

                    jsonContractAdds[paramIdx] = new FolderAddRequest()
                    {
                        DeviceId = null,
                        SyncboxId = null,
                        CreatedDate = currentFolderItem.CreatedDate,
                        RelativePath = null,
                        Name = (string.IsNullOrEmpty(currentFolderName) ? null : currentFolderName),
                        ParentUid = (string.IsNullOrEmpty(currentFolderItem.ItemUid) ? null : currentFolderItem.ItemUid),
                    };
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                FolderAddsRequest requestContent = new JsonContracts.FolderAddsRequest()
                {
                    Adds = jsonContractAdds,
                    SyncboxId = _syncbox.SyncboxId,
                    DeviceId = _syncbox.CopiedSettings.DeviceId,
                };

                // server method path
                string serverMethodPath = CLDefinitions.MethodPathOneOffFolderAdds;

                // Communicate with the server to get the response.
                JsonContracts.SyncboxAddFoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxAddFoldersResponse>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.AddResponses != null)
                {
                    if (responseFromServer.AddResponses.Length != folderItemsToAdd.Length)
                    {
                        throw new CLException(CLExceptionCode.OnDemand_FileRename, Resources.ExceptionOnDemandResponseArrayLength);
                    }

                    List<CLFileItem> listFileItems = new List<CLFileItem>();
                    List<CLError> listErrors = new List<CLError>();

                    for (int responseIdx = 0; responseIdx < responseFromServer.AddResponses.Length; responseIdx++)
                    {
                        try
                        {
                            FileChangeResponse currentAddResponse = responseFromServer.AddResponses[responseIdx];

                            if (currentAddResponse == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                            }
                            if (currentAddResponse.Header == null || string.IsNullOrEmpty(currentAddResponse.Header.Status))
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                            }
                            if (currentAddResponse.Metadata == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                            }

                            switch (currentAddResponse.Header.Status)
                            {
                                case CLDefinitions.CLEventTypeAccepted:
                                    CLFileItem resultItem = new CLFileItem(currentAddResponse.Metadata, currentAddResponse.Header.Action, currentAddResponse.Action, _syncbox);
                                    if (itemCompletionCallback != null)
                                    {
                                        try
                                        {
                                            itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    break;

                                case CLDefinitions.CLEventTypeParentNotFound:
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ParentNotFound, Resources.ExceptionOnDemandAddParentFolderNotFound);

                                case CLDefinitions.CLEventTypeParentDeleted:
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandAddFoldersParentFolderDeleted);

                                case CLDefinitions.CLEventTypeExists:
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_AlreadyExists, Resources.ExceptionOnDemandAddFoldersTargetFolderAlreadyExists);

                                case CLDefinitions.RESTResponseStatusFailed:
                                    Exception innerEx;
                                    string errorMessageString;
                                    try
                                    {
                                        errorMessageString = string.Join(Environment.NewLine, currentAddResponse.Metadata.ErrorMessage);
                                        innerEx = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                        innerEx = ex;
                                    }

                                    throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                default:
                                    throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentAddResponse.Header.Status));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (itemCompletionCallback != null)
                            {
                                try
                                {
                                    itemCompletionCallback(responseIdx, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
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
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_AddFolders, Resources.ExceptionCLHttpRestWithoutAddFolderResponses);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end AddFolders (Adds folders in the syncbox.)

        #region AddFiles (Add files in the syncbox.)
        /// <summary>
        /// Asynchronously starts adding files in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method to fire when transfer status is updated for each active item.  Can be null.</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer status callback above is fired.  Can be null.</param>
        /// <param name="cancellationSource">The cancellation token which can be used to cancel the file upload operations.  Can be null.</param>
        /// <param name="filesToAdd">(params) An array of information for each file to add (full path of the file, parent folder in the syncbox and the name of the file in the syncbox).</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAddFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params AddFileItemParams[] filesToAdd)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAddFilesResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    transferStatusCallback = transferStatusCallback,
                    transferStatusCallbackUserState = transferStatusCallbackUserState,
                    cancellationSource = cancellationSource,
                    filesToAdd = filesToAdd
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = AddFiles(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.transferStatusCallback,
                            Data.transferStatusCallbackUserState,
                            Data.cancellationSource,
                            Data.filesToAdd);

                        Data.toReturn.Complete(
                            new SyncboxAddFilesResult(overallError), // any overall error that may have occurred during processing
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
        /// Finishes adding files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAddFiles(IAsyncResult asyncResult, out SyncboxAddFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAddFilesResult>(asyncResult, out result);
        }

        private sealed class StreamOrStreamContextHolder
        {
            public Stream baseStream { get; private set; }
            public StreamContext context { get; private set; }

            public void switchToContext(StreamContext context)
            {
                if (context != null)
                {
                    this.context = context;
                    this.baseStream = null;
                }
            }

            public StreamOrStreamContextHolder(Stream baseStream)
            {
                this.baseStream = baseStream;
            }

            public static void DisposeHolderInArray(StreamOrStreamContextHolder[] inputArray, int disposeIndex)
            {
                if (inputArray != null
                    && disposeIndex >= 0
                    && disposeIndex < inputArray.Length)
                {
                    StreamOrStreamContextHolder currentItem = inputArray[disposeIndex];

                    if (currentItem != null)
                    {
                        if (currentItem.context != null)
                        {
                            try
                            {
                                currentItem.context.Dispose();
                            }
                            catch
                            {
                            }
                            currentItem.context = null;
                        }
                        else if (currentItem.baseStream != null)
                        {
                            try
                            {
                                currentItem.baseStream.Dispose();
                            }
                            catch
                            {
                            }
                            currentItem.baseStream = null;
                        }

                        inputArray[disposeIndex] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Add files in the syncbox.  Uploads the files to the Cloud.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method to fire when transfer status is updated for each active item.  Can be null.</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer status callback above is fired.  Can be null.</param>
        /// <param name="cancellationSource">The cancellation token which can be used to cancel the file upload operations.  Can be null.</param>
        /// <param name="filesToAdd">(params) An array of information for each file to add (full path of the file, parent folder in the syncbox and the name of the file in the syncbox).</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AddFiles(
            bool reservedForActiveSync, 
            CLFileItemCompletionCallback itemCompletionCallback, 
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params AddFileItemParams[] filesToAdd)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.{
                if (filesToAdd == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandFilesToAddMustNotBeNull);
                }
                if (filesToAdd.Length < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.Http_BadRequest, Resources.ExceptionOnDemandFilesToAddLengthMustBeGtZero);
                }
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                StreamOrStreamContextHolder[] uploadStreams = new StreamOrStreamContextHolder[filesToAdd.Length];

                try
                {
                    List<KeyValuePair<FileChange, int>> addChanges = new List<KeyValuePair<FileChange, int>>();
                    List<KeyValuePair<CLError, int>> openStreamErrors = new List<KeyValuePair<CLError, int>>();

                    for (int currentNameAndParentIdx = 0; currentNameAndParentIdx < filesToAdd.Length; currentNameAndParentIdx++)
                    {
                        AddFileItemParams fullPathAndParentAndNewName = filesToAdd[currentNameAndParentIdx];
                        if (fullPathAndParentAndNewName == null)
                        {
                            throw new CLArgumentNullException(CLExceptionCode.OnDemand_InvalidParameters, String.Format(Resources.ExceptionOnDemandFilesToAddAtIndexMustNotBeNullMsg0, currentNameAndParentIdx));
                        }
                        if (fullPathAndParentAndNewName.ParentFolder == null)
                        {
                            throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, String.Format(Resources.ExceptionOnDemandFilesToAddAtIndexParentMustNotBeNullMsg0, currentNameAndParentIdx));
                        }
                        if (fullPathAndParentAndNewName.ParentFolder.Syncbox != _syncbox)
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, currentNameAndParentIdx));
                        }
                        if (!fullPathAndParentAndNewName.ParentFolder.IsFolder)
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, String.Format(Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpectedMsg0, currentNameAndParentIdx));
                        }
                        if (fullPathAndParentAndNewName.ParentFolder.IsDeleted)
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, String.Format(Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0, currentNameAndParentIdx));
                        }
                        if (String.IsNullOrEmpty(fullPathAndParentAndNewName.FullPath))
                        {
                            throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, String.Format(Resources.ExceptionOnDemandFilesToAddAtIndexFullPathMustNotBeNullMsg0, currentNameAndParentIdx));  //&&&& fix this
                        }
                        if (string.IsNullOrEmpty(fullPathAndParentAndNewName.ParentFolder.ItemUid))
                        {
                            throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandFilesToAddAtIndexParentFolderItemUidMissingMsg0);
                        }

                        FilePath fullPath = new FilePath(fullPathAndParentAndNewName.FullPath);

                        //TODO: need to add check for bad characters in name

                        //TODO: need to add check for length including name: do not use Helpers.CheckSyncboxPathLength since that only works for the syncbox root

                        FileChange addChange = new FileChange()
                        {
                            Direction = SyncDirection.To,
                            Metadata = new FileMetadata()
                            {
                                EventTime = DateTime.UtcNow,
                                HashableProperties = new FileMetadataHashableProperties(
                                    isFolder: false,
                                    lastTime: File.GetLastWriteTimeUtc(fullPathAndParentAndNewName.FullPath),
                                    creationTime: File.GetCreationTimeUtc(fullPathAndParentAndNewName.FullPath),
                                    size: null),
                                ParentFolderServerUid = fullPathAndParentAndNewName.ParentFolder.ItemUid
                            },
                            NewPath = fullPath,
                            Type = FileChangeType.Created
                        };
                        
                        FileStream OutputStream;
                        byte[][] intermediateHashes;
                        byte[] newMD5Bytes;
                        Nullable<long> finalFileSize;
                        bool dependencyFileChangeNotFound = false;
                        bool openStreamSucceeded;

                        try
                        {
                            Helpers.OpenFileStreamAndCalculateHashes(
                                out OutputStream,
                                out intermediateHashes,
                                out newMD5Bytes,
                                out finalFileSize,
                                addChange,
                                out dependencyFileChangeNotFound);

                            openStreamSucceeded = true;
                        }
                        catch (Exception ex)
                        {
                            if (dependencyFileChangeNotFound)
                            {
                                try
                                {
                                    throw new CLFileNotFoundException(CLExceptionCode.OnDemand_FileAddNotFound, Resources.ExceptionFileNotFound, ex);
                                }
                                catch (Exception innerEx)
                                {
                                    openStreamErrors.Add(
                                        new KeyValuePair<CLError, int>(innerEx, currentNameAndParentIdx));
                                }
                            }
                            else
                            {
                                openStreamErrors.Add(
                                    new KeyValuePair<CLError, int>(ex, currentNameAndParentIdx));
                            }

                            openStreamSucceeded = false;

                            OutputStream = Helpers.DefaultForType<FileStream>();
                            intermediateHashes = Helpers.DefaultForType<byte[][]>();
                            newMD5Bytes = Helpers.DefaultForType<byte[]>();
                            finalFileSize = Helpers.DefaultForType<Nullable<long>>();
                        }

                        if (openStreamSucceeded)
                        {
                            if (OutputStream == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileFileStreamDoesNotExist);
                            }
                            
                            uploadStreams[currentNameAndParentIdx] = new StreamOrStreamContextHolder(OutputStream);

                            if (intermediateHashes == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileIntermediateHashesDoesNotExist);
                            }
                            if (newMD5Bytes == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileNewMd5BytesDoesNotExist);
                            }
                            if (finalFileSize == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileFinalFileSizeDoesNotExist);
                            }

                            uploadStreams[currentNameAndParentIdx].switchToContext(
                                UploadStreamContext.Create(OutputStream, intermediateHashes, newMD5Bytes, finalFileSize));

                            addChanges.Add(
                                new KeyValuePair<FileChange, int>(
                                    addChange, currentNameAndParentIdx));
                        }
                    }

                    if (addChanges.Count > 0)
                    {
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
                        object requestContent = new JsonContracts.FileAdds()
                        {
                            DeviceId = _syncbox.CopiedSettings.DeviceId,
                            SyncboxId = _syncbox.SyncboxId,
                            Adds = addChanges.Select(currentAddChange => new JsonContracts.FileAdd()
                                {
                                    CreatedDate = currentAddChange.Key.Metadata.HashableProperties.CreationTime,
                                    Hash = currentAddChange.Key.GetMD5LowercaseString(),
                                    MimeType = currentAddChange.Key.Metadata.MimeType,
                                    ModifiedDate = currentAddChange.Key.Metadata.HashableProperties.LastTime,
                                    ParentUid = currentAddChange.Key.Metadata.ParentFolderServerUid,
                                    Size = currentAddChange.Key.Metadata.HashableProperties.Size,
                                    Name = filesToAdd[currentAddChange.Value].FileName,
                                }).ToArray()
                        };

                        // server method path switched on whether change is a file or not
                        string serverMethodPath = CLDefinitions.MethodPathOneOffFileAdds;

                        // Communicate with the server to get the response.
                        JsonContracts.SyncboxAddFilesResponse responseFromServer;
                        responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxAddFilesResponse>(requestContent, // dynamic type of request content based on method path
                            CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                            serverMethodPath, // dynamic path to appropriate one-off method
                            Helpers.requestMethod.post, // one-off methods are all posts
                            _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                            null, // not an upload or download
                            Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                            _syncbox.CopiedSettings, // pass the copied settings
                            _syncbox, // pass the syncbox
                            requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                            isOneOff: true); // one-offs bypass the halt all check

                        // Convert these items to the output array.
                        if (responseFromServer == null || responseFromServer.AddResponses == null)
                        {
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionCLHttpRestWithoutAddFilesResponses);
                        }

                        if (responseFromServer.AddResponses.Length != filesToAdd.Length)
                        {
                            throw new CLException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandResponseArrayLength);
                        }

                        // we know we don't have an overall error by now, so we can finally return the items for which we could not open streams
                        if (itemCompletionCallback != null
                            && openStreamErrors.Count > 0)
                        {
                            foreach (KeyValuePair<CLError, int> openStreamError in openStreamErrors)
                            {
                                itemCompletionCallback(itemIndex: openStreamError.Value, completedItem: null, error: openStreamError.Key, userState: itemCompletionCallbackUserState);
                            }
                        }

                        List<Tuple<CLFileItem, FileChange, EventWaitHandle, int>> filesLeftToUpload = new List<Tuple<CLFileItem, FileChange, EventWaitHandle, int>>();

                        for (int responseIdx = 0; responseIdx < responseFromServer.AddResponses.Length; responseIdx++)
                        {
                            try
                            {
                                FileChangeResponse currentAddResponse = responseFromServer.AddResponses[responseIdx];

                                if (currentAddResponse == null)
                                {
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                                }
                                if (currentAddResponse.Header == null || string.IsNullOrEmpty(currentAddResponse.Header.Status))
                                {
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                                }
                                if (currentAddResponse.Metadata == null)
                                {
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                                }

                                switch (currentAddResponse.Header.Status)
                                {
                                    case CLDefinitions.CLEventTypeDuplicate:
                                    case CLDefinitions.CLEventTypeExists:
                                        CLFileItem resultItem = new CLFileItem(currentAddResponse.Metadata, currentAddResponse.Header.Action, currentAddResponse.Action, _syncbox);
                                        if (itemCompletionCallback != null)
                                        {
                                            try
                                            {
                                                itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        StreamOrStreamContextHolder.DisposeHolderInArray(uploadStreams, addChanges[responseIdx].Value);
                                        break;

                                    case CLDefinitions.CLEventTypeUpload:
                                    case CLDefinitions.CLEventTypeUploading:
                                        addChanges[responseIdx].Key.Metadata.Revision = currentAddResponse.Metadata.Revision;

                                        // This is a little strange.  If the upload completes successfully, we will drive the item completion callback with a CLFileItem.
                                        // The CLFileItem is constructed below out of the currentAddResponse we just received from the server.  However, at this point,
                                        // The IsNotPending value in the response is false, because the server wants us to upload the file.  Two cases: 1) The upload fails.
                                        // In that case, we won't present the constructed CLFileItem to the callback.  2) The upload succeeds.  We need the CLFileItem.IsPending
                                        // flag to be false.  So, set the server's currentAddResponse.IsNotPending field to properly construct the CLFileItem assuming that
                                        // the upload will succeed.
                                        currentAddResponse.Metadata.IsNotPending = true;

                                        // Add this upload request.
                                        filesLeftToUpload.Add(
                                            new Tuple<CLFileItem, FileChange, EventWaitHandle, int>(
                                                new CLFileItem(currentAddResponse.Metadata, currentAddResponse.Header.Action, currentAddResponse.Action, _syncbox),
                                                addChanges[responseIdx].Key,
                                                new EventWaitHandle(initialState: false, mode: EventResetMode.ManualReset),
                                                addChanges[responseIdx].Value));
                                        break;

                                    case CLDefinitions.CLEventTypeDownload:
                                        throw new CLException(CLExceptionCode.OnDemand_NewerVersionAvailableForDownload, Resources.ExceptionCLHttpRestDownloadNewerVersion);

                                    case CLDefinitions.CLEventTypeParentNotFound:
                                        throw new CLException(CLExceptionCode.OnDemand_ParentNotFound, Resources.ExceptionOnDemandAddParentFolderNotFound);

                                    case CLDefinitions.CLEventTypeConflict:
                                        throw new CLException(CLExceptionCode.OnDemand_Conflict, Resources.ExceptionOnDemandConflict);

                                    case CLDefinitions.RESTResponseStatusFailed:
                                        Exception innerEx;
                                        string errorMessageString;
                                        try
                                        {
                                            errorMessageString = string.Join(Environment.NewLine, currentAddResponse.Metadata.ErrorMessage);
                                            innerEx = null;
                                        }
                                        catch (Exception ex)
                                        {
                                            errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                            innerEx = ex;
                                        }

                                        throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                    default:
                                        throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentAddResponse.Header.Status));
                                }
                            }
                            catch (Exception ex)
                            {
                                if (itemCompletionCallback != null)
                                {
                                    try
                                    {
                                        itemCompletionCallback(itemIndex: addChanges[responseIdx].Value, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
                                    }
                                    catch
                                    {
                                    }
                                }
                                StreamOrStreamContextHolder.DisposeHolderInArray(uploadStreams, addChanges[responseIdx].Value);
                            }
                        }

                        if (filesLeftToUpload.Count > 0)
                        {
                            foreach (Tuple<CLFileItem, FileChange, EventWaitHandle, int> fileLeftToUpload in filesLeftToUpload)
                            {
                                var uploadForTask = DelegateAndDataHolderBase.Create(
                                    new
                                    {
                                        inputItemIndex = fileLeftToUpload.Item4,
                                        uploadStreamContext = uploadStreams[fileLeftToUpload.Item4].context,
                                        uploadChange = fileLeftToUpload.Item2,
                                        cancellationSource = cancellationSource,
                                        completionHandle = fileLeftToUpload.Item3,
                                        itemCompletionCallback = itemCompletionCallback,
                                        itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                                        transferStatusCallback = transferStatusCallback,
                                        transferStatusCallbackUserState = transferStatusCallbackUserState,
                                        copiedSettings = _syncbox.CopiedSettings,
                                        completedItem = fileLeftToUpload.Item1,
                                        uploadStreams = uploadStreams
                                    },
                                    (Data, errorToAccumulate) =>
                                    {
                                        try
                                        {
                                            var statusConversionDelegate = DelegateAndDataHolderBase<object, long, SyncDirection, string, long, long, bool>.Create(
                                                new
                                                {
                                                    transferStatusCallback = Data.transferStatusCallback,
                                                    transferStatusCallbackUserState = Data.transferStatusCallbackUserState,
                                                    inputItemIndex = Data.inputItemIndex
                                                },
                                                (innerData, userState, eventId, direction, relativePath, byteProgress, totalByteSize, isError, innerErrorToAccumulate) =>
                                                {
                                                    if (innerData.transferStatusCallback != null)
                                                    {
                                                        innerData.transferStatusCallback(
                                                            innerData.inputItemIndex,
                                                            byteProgress,
                                                            totalByteSize,
                                                            innerData.transferStatusCallbackUserState);
                                                    }
                                                },
                                                null);

                                            string unusedMessage;
                                            bool hashMismatchFound;
                                            CLError uploadError = UploadFile(
                                                Data.uploadStreamContext,
                                                Data.uploadChange,
                                                Data.completedItem.ItemUid,
                                                Data.completedItem.Revision,
                                                Data.copiedSettings.HttpTimeoutMilliseconds,
                                                out unusedMessage,
                                                out hashMismatchFound,
                                                Data.cancellationSource,
                                                /* asyncCallback: */ null,
                                                /* asyncResult: */ null,
                                                /* progress: */ null,
                                                new FileTransferStatusUpdateDelegate(statusConversionDelegate.VoidProcess),
                                                /* statusUpdateId: */ Guid.Empty);

                                            if (uploadError != null)
                                            {
                                                throw new CLException(CLExceptionCode.OnDemand_Upload, Resources.ExceptionCLHttpRestAddFilesUploadError, uploadError.Exceptions);
                                            }

                                            if (Data.itemCompletionCallback != null)
                                            {
                                                Data.itemCompletionCallback(Data.inputItemIndex, Data.completedItem, /* error: */ null, Data.itemCompletionCallbackUserState);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (Data.itemCompletionCallback != null)
                                            {
                                                Data.itemCompletionCallback(Data.inputItemIndex, /* completedItem: */ null, ex, Data.itemCompletionCallbackUserState);
                                            }
                                        }
                                        finally
                                        {
                                            // UploadFile should dispose the stream, but do it anyways just in case
                                            StreamOrStreamContextHolder.DisposeHolderInArray(Data.uploadStreams, Data.inputItemIndex);

                                            Data.completionHandle.Set();
                                        }
                                    },
                                    errorToAccumulate: null);

                                (new Task(new Action(uploadForTask.VoidProcess))).Start(HttpScheduler.GetSchedulerByDirection(SyncDirection.To, _syncbox.CopiedSettings));
                            }

                            WaitHandle[] allWaitHandles = filesLeftToUpload.Select(fileToUpload => fileToUpload.Item3).ToArray();
                            WaitHandle.WaitAll(allWaitHandles);
                        }
                    }
                    // else there was nothing in addChanges since every item in the input parameters could not have its stream opened
                    // also, only need to fire completion routines for errors if the completion callback was set
                    else if (itemCompletionCallback != null)
                    {
                        foreach (KeyValuePair<CLError, int> openStreamError in openStreamErrors)
                        {
                            itemCompletionCallback(itemIndex: openStreamError.Value, completedItem: null, error: openStreamError.Key, userState: itemCompletionCallbackUserState);
                        }
                    }
                }
                catch
                {
                    for (int currentDiposalIndex = 0; currentDiposalIndex < uploadStreams.Length; currentDiposalIndex++)
                    {
                        StreamOrStreamContextHolder.DisposeHolderInArray(uploadStreams, currentDiposalIndex);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }
        #endregion  // end AddFiles (Adds files in the syncbox.)

        #region ModifyFiles (Modify files in the syncbox.)
        /// <summary>
        /// Asynchronously starts modifying files in the syncbox.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method to fire when transfer status is updated for each active item.  Can be null.</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer status callback above is fired.  Can be null.</param>
        /// <param name="cancellationSource">The cancellation token which can be used to cancel the file upload operations.  Can be null.</param>
        /// <param name="filesToModify">(params) An array of parameters.  Each parameter contains the CLFileItem representing the file in the syncbox, and the full path on disk of the modified file.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginModifyFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params ModifyFileItemParams[] filesToModify)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxModifyFilesResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    itemCompletionCallback = itemCompletionCallback,
                    itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                    transferStatusCallback = transferStatusCallback,
                    transferStatusCallbackUserState = transferStatusCallbackUserState,
                    cancellationSource = cancellationSource,
                    filesToModify = filesToModify
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = ModifyFiles(
                            Data.reservedForActiveSync,
                            Data.itemCompletionCallback,
                            Data.itemCompletionCallbackUserState,
                            Data.transferStatusCallback,
                            Data.transferStatusCallbackUserState,
                            Data.cancellationSource,
                            Data.filesToModify);

                        Data.toReturn.Complete(
                            new SyncboxModifyFilesResult(overallError), // any overall error that may have occurred during processing
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
        /// Finishes modifying files in the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndModifyFiles(IAsyncResult asyncResult, out SyncboxModifyFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxModifyFilesResult>(asyncResult, out result);
        }

        /// <summary>
        /// Modify files in the syncbox.  Uploads the files to the Cloud.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="itemCompletionCallback">Callback method to fire for each item completion.</param>
        /// <param name="itemCompletionCallbackUserState">User state to be passed whenever the item completion callback above is fired.</param>
        /// <param name="transferStatusCallback">Callback method to fire when transfer status is updated for each active item.  Can be null.</param>
        /// <param name="transferStatusCallbackUserState">User state to be passed whenever the transfer status callback above is fired.  Can be null.</param>
        /// <param name="cancellationSource">The cancellation token which can be used to cancel the file upload operations.  Can be null.</param>
        /// <param name="filesToModify">(params) An array of parameters.  Each parameter contains the CLFileItem representing the file in the syncbox, and the full path on disk of the modified file.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError ModifyFiles(
            bool reservedForActiveSync,
            CLFileItemCompletionCallback itemCompletionCallback,
            object itemCompletionCallbackUserState,
            CLFileUploadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource,
            params ModifyFileItemParams[] filesToModify)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters.{
                if (filesToModify == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandFilesToModifyMustNotBeNull);
                }
                if (filesToModify.Length < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.Http_BadRequest, Resources.ExceptionOnDemandFilesToModifyLengthMustBeGtZero);
                }
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                StreamOrStreamContextHolder[] uploadStreams = new StreamOrStreamContextHolder[filesToModify.Length];

                try
                {
                    // The modify changes will hold the FileChange, the ServerUid and the index in filesToModify for each file that will be communicated.
                    List<Tuple<FileChange, string, int>> modifyChanges = new List<Tuple<FileChange, string, int>>();
                    List<KeyValuePair<CLError, int>> openStreamErrors = new List<KeyValuePair<CLError, int>>();

                    for (int currentFileItemIdx = 0; currentFileItemIdx < filesToModify.Length; currentFileItemIdx++)
                    {
                        ModifyFileItemParams currentParam = filesToModify[currentFileItemIdx];
                        if (currentParam == null)
                        {
                            throw new CLArgumentNullException(CLExceptionCode.OnDemand_InvalidParameters, String.Format(Resources.ExceptionOnDemandFilesToModifyAtIndexMustNotBeNullMsg0, currentFileItemIdx));
                        }
                        if (currentParam.FileItem.IsFolder)
                        {
                            throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters,  String.Format(Resources.ExceptionOnDemandFilesToModifyAtIndexItemMustBeAFileItemMsg0, currentFileItemIdx));
                        }
                        if (String.IsNullOrEmpty(currentParam.FileItem.ItemUid))
                        {
                            throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, String.Format(Resources.ExceptionOnDemandFilesToModifyAtIndexItemUidMustBeSpecifiedMsg0, currentFileItemIdx));
                        }
                        if (currentParam.FileItem.Syncbox != _syncbox)
                        {
                            throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, String.Format(Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncboxMsg0, currentFileItemIdx));
                        }

                        FilePath fullPath = new FilePath(currentParam.FullPath);

                        FileChange modifyChange = new FileChange()
                        {
                            Direction = SyncDirection.To,
                            Metadata = new FileMetadata()
                            {
                                EventTime = DateTime.UtcNow,
                                HashableProperties = new FileMetadataHashableProperties(
                                    isFolder: false,
                                    lastTime: File.GetLastWriteTimeUtc(currentParam.FullPath),
                                    creationTime: File.GetCreationTimeUtc(currentParam.FullPath),
                                    size: null),
                                Revision = currentParam.FileItem.Revision,
                            },
                            NewPath = fullPath,
                            Type = FileChangeType.Created
                        };

                        FileStream OutputStream;
                        byte[][] intermediateHashes;
                        byte[] newMD5Bytes;
                        Nullable<long> finalFileSize;
                        bool dependencyFileChangeNotFound = false;
                        bool openStreamSucceeded;

                        try
                        {
                            Helpers.OpenFileStreamAndCalculateHashes(
                                out OutputStream,
                                out intermediateHashes,
                                out newMD5Bytes,
                                out finalFileSize,
                                modifyChange,
                                out dependencyFileChangeNotFound);

                            openStreamSucceeded = true;
                        }
                        catch (Exception ex)
                        {
                            if (dependencyFileChangeNotFound)
                            {
                                try
                                {
                                    throw new CLFileNotFoundException(CLExceptionCode.OnDemand_FileAddNotFound, Resources.ExceptionFileNotFound, ex);
                                }
                                catch (Exception innerEx)
                                {
                                    openStreamErrors.Add(
                                        new KeyValuePair<CLError, int>(innerEx, currentFileItemIdx));
                                }
                            }
                            else
                            {
                                openStreamErrors.Add(
                                    new KeyValuePair<CLError, int>(ex, currentFileItemIdx));
                            }

                            openStreamSucceeded = false;

                            OutputStream = Helpers.DefaultForType<FileStream>();
                            intermediateHashes = Helpers.DefaultForType<byte[][]>();
                            newMD5Bytes = Helpers.DefaultForType<byte[]>();
                            finalFileSize = Helpers.DefaultForType<Nullable<long>>();
                        }

                        if (openStreamSucceeded)
                        {
                            if (OutputStream == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileFileStreamDoesNotExist);
                            }

                            uploadStreams[currentFileItemIdx] = new StreamOrStreamContextHolder(OutputStream);

                            if (intermediateHashes == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileIntermediateHashesDoesNotExist);
                            }
                            if (newMD5Bytes == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileNewMd5BytesDoesNotExist);
                            }
                            if (finalFileSize == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandAddFileFinalFileSizeDoesNotExist);
                            }

                            uploadStreams[currentFileItemIdx].switchToContext(
                                UploadStreamContext.Create(OutputStream, intermediateHashes, newMD5Bytes, finalFileSize));

                            modifyChanges.Add(
                                new Tuple<FileChange, string, int>(
                                    modifyChange, currentParam.FileItem.ItemUid, currentFileItemIdx));
                        }
                    }

                    if (modifyChanges.Count > 0)
                    {
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
                        object requestContent = new JsonContracts.FileModifies()
                        {
                            DeviceId = _syncbox.CopiedSettings.DeviceId,
                            SyncboxId = _syncbox.SyncboxId,
                            Modifies = modifyChanges.Select(currentModifyChange => new JsonContracts.FileModify()
                            {
                                CreatedDate = currentModifyChange.Item1.Metadata.HashableProperties.CreationTime,
                                Hash = currentModifyChange.Item1.GetMD5LowercaseString(),
                                MimeType = currentModifyChange.Item1.Metadata.MimeType,
                                ModifiedDate = currentModifyChange.Item1.Metadata.HashableProperties.LastTime,
                                ServerUid = filesToModify[currentModifyChange.Item3].FileItem.ItemUid,
                                Revision = filesToModify[currentModifyChange.Item3].FileItem.Revision,
                                Size = currentModifyChange.Item1.Metadata.HashableProperties.Size,
                            }).ToArray()
                        };

                        // server method path switched on whether change is a file or not
                        string serverMethodPath = CLDefinitions.MethodPathOneOffFileModifies;

                        // Communicate with the server to get the response.
                        JsonContracts.SyncboxModifyFilesResponse responseFromServer;
                        responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxModifyFilesResponse>(requestContent, // dynamic type of request content based on method path
                            CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                            serverMethodPath, // dynamic path to appropriate one-off method
                            Helpers.requestMethod.post, // one-off methods are all posts
                            _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                            null, // not an upload or download
                            Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                            _syncbox.CopiedSettings, // pass the copied settings
                            _syncbox, // pass the syncbox
                            requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                            isOneOff: true); // one-offs bypass the halt all check

                        // Convert these items to the output array.
                        if (responseFromServer == null || responseFromServer.ModifyResponses == null)
                        {
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionCLHttpRestWithoutModifyFilesResponses);
                        }

                        if (responseFromServer.ModifyResponses.Length != filesToModify.Length)
                        {
                            throw new CLException(CLExceptionCode.OnDemand_FileAdd, Resources.ExceptionOnDemandResponseArrayLength);
                        }

                        // we know we don't have an overall error by now, so we can finally return the items for which we could not open streams
                        if (itemCompletionCallback != null
                            && openStreamErrors.Count > 0)
                        {
                            foreach (KeyValuePair<CLError, int> openStreamError in openStreamErrors)
                            {
                                itemCompletionCallback(itemIndex: openStreamError.Value, completedItem: null, error: openStreamError.Key, userState: itemCompletionCallbackUserState);
                            }
                        }

                        List<Tuple<CLFileItem, FileChange, EventWaitHandle, int>> filesLeftToUpload = new List<Tuple<CLFileItem, FileChange, EventWaitHandle, int>>();

                        for (int responseIdx = 0; responseIdx < responseFromServer.ModifyResponses.Length; responseIdx++)
                        {
                            try
                            {
                                FileChangeResponse currentModifyResponse = responseFromServer.ModifyResponses[responseIdx];

                                if (currentModifyResponse == null)
                                {
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullItem);
                                }
                                if (currentModifyResponse.Header == null || string.IsNullOrEmpty(currentModifyResponse.Header.Status))
                                {
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullStatus);
                                }
                                if (currentModifyResponse.Metadata == null)
                                {
                                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingResponseField, Resources.ExceptionOnDemandNullMetadata);
                                }

                                switch (currentModifyResponse.Header.Status)
                                {
                                    case CLDefinitions.CLEventTypeDuplicate:
                                    case CLDefinitions.CLEventTypeExists:
                                        CLFileItem resultItem = new CLFileItem(currentModifyResponse.Metadata, currentModifyResponse.Header.Action, currentModifyResponse.Action, _syncbox);
                                        if (itemCompletionCallback != null)
                                        {
                                            try
                                            {
                                                itemCompletionCallback(responseIdx, resultItem, error: null, userState: itemCompletionCallbackUserState);
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        StreamOrStreamContextHolder.DisposeHolderInArray(uploadStreams, modifyChanges[responseIdx].Item3);
                                        break;

                                    case CLDefinitions.CLEventTypeUpload:
                                    case CLDefinitions.CLEventTypeUploading:
                                        modifyChanges[responseIdx].Item1.Metadata.Revision = currentModifyResponse.Metadata.Revision;

                                        // This is a little strange.  If the upload completes successfully, we will drive the item completion callback with a CLFileItem.
                                        // The CLFileItem is constructed below out of the currentAddResponse we just received from the server.  However, at this point,
                                        // The IsNotPending value in the response is false, because the server wants us to upload the file.  Two cases: 1) The upload fails.
                                        // In that case, we won't present the constructed CLFileItem to the callback.  2) The upload succeeds.  We need the CLFileItem.IsPending
                                        // flag to be false.  So, set the server's currentAddResponse.IsNotPending field to properly construct the CLFileItem assuming that
                                        // the upload will succeed.
                                        currentModifyResponse.Metadata.IsNotPending = true;

                                        // Add this upload request.
                                        filesLeftToUpload.Add(
                                            new Tuple<CLFileItem, FileChange, EventWaitHandle, int>(
                                                new CLFileItem(currentModifyResponse.Metadata, currentModifyResponse.Header.Action, currentModifyResponse.Action, _syncbox),
                                                modifyChanges[responseIdx].Item1,
                                                new EventWaitHandle(initialState: false, mode: EventResetMode.ManualReset),
                                                modifyChanges[responseIdx].Item3));
                                        break;

                                    case CLDefinitions.CLEventTypeConflict:
                                        throw new CLException(CLExceptionCode.OnDemand_Conflict, Resources.ExceptionOnDemandConflict);

                                    case CLDefinitions.CLEventTypeAlreadyDeleted:
                                        throw new CLException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandAlreadyDeleted);

                                    case CLDefinitions.RESTResponseStatusFailed:
                                        Exception innerEx;
                                        string errorMessageString;
                                        try
                                        {
                                            errorMessageString = string.Join(Environment.NewLine, currentModifyResponse.Metadata.ErrorMessage);
                                            innerEx = null;
                                        }
                                        catch (Exception ex)
                                        {
                                            errorMessageString = Resources.ExceptionOnDemandDeserializeErrorMessage;
                                            innerEx = ex;
                                        }

                                        throw new CLException(CLExceptionCode.OnDemand_ItemError, Resources.ExceptionOnDemandItemError, new Exception(errorMessageString, innerEx));

                                    default:
                                        throw new CLException(CLExceptionCode.OnDemand_UnknownItemStatus, string.Format(Resources.ExceptionOnDemandUnknownItemStatus, currentModifyResponse.Header.Status));
                                }
                            }
                            catch (Exception ex)
                            {
                                if (itemCompletionCallback != null)
                                {
                                    try
                                    {
                                        itemCompletionCallback(itemIndex: modifyChanges[responseIdx].Item3, completedItem: null, error: ex, userState: itemCompletionCallbackUserState);
                                    }
                                    catch
                                    {
                                    }
                                }
                                StreamOrStreamContextHolder.DisposeHolderInArray(uploadStreams, modifyChanges[responseIdx].Item3);
                            }
                        }

                        if (filesLeftToUpload.Count > 0)
                        {
                            foreach (Tuple<CLFileItem, FileChange, EventWaitHandle, int> fileLeftToUpload in filesLeftToUpload)
                            {
                                var uploadForTask = DelegateAndDataHolderBase.Create(
                                    new
                                    {
                                        inputItemIndex = fileLeftToUpload.Item4,
                                        uploadStreamContext = uploadStreams[fileLeftToUpload.Item4].context,
                                        uploadChange = fileLeftToUpload.Item2,
                                        cancellationSource = cancellationSource,
                                        completionHandle = fileLeftToUpload.Item3,
                                        itemCompletionCallback = itemCompletionCallback,
                                        itemCompletionCallbackUserState = itemCompletionCallbackUserState,
                                        transferStatusCallback = transferStatusCallback,
                                        transferStatusCallbackUserState = transferStatusCallbackUserState,
                                        copiedSettings = _syncbox.CopiedSettings,
                                        completedItem = fileLeftToUpload.Item1,
                                        uploadStreams = uploadStreams
                                    },
                                    (Data, errorToAccumulate) =>
                                    {
                                        try
                                        {
                                            var statusConversionDelegate = DelegateAndDataHolderBase<object, long, SyncDirection, string, long, long, bool>.Create(
                                                new
                                                {
                                                    transferStatusCallback = Data.transferStatusCallback,
                                                    transferStatusCallbackUserState = Data.transferStatusCallbackUserState,
                                                    inputItemIndex = Data.inputItemIndex
                                                },
                                                (innerData, userState, eventId, direction, relativePath, byteProgress, totalByteSize, isError, innerErrorToAccumulate) =>
                                                {
                                                    if (innerData.transferStatusCallback != null)
                                                    {
                                                        innerData.transferStatusCallback(
                                                            innerData.inputItemIndex,
                                                            byteProgress,
                                                            totalByteSize,
                                                            innerData.transferStatusCallbackUserState);
                                                    }
                                                },
                                                null);

                                            string unusedMessage;
                                            bool hashMismatchFound;
                                            CLError uploadError = UploadFile(
                                                Data.uploadStreamContext,
                                                Data.uploadChange,
                                                Data.completedItem.ItemUid,
                                                Data.completedItem.Revision,
                                                Data.copiedSettings.HttpTimeoutMilliseconds,
                                                out unusedMessage,
                                                out hashMismatchFound,
                                                Data.cancellationSource,
                                                /* asyncCallback: */ null,
                                                /* asyncResult: */ null,
                                                /* progress: */ null,
                                                new FileTransferStatusUpdateDelegate(statusConversionDelegate.VoidProcess),
                                                /* statusUpdateId: */ Guid.Empty);

                                            if (uploadError != null)
                                            {
                                                throw new CLException(CLExceptionCode.OnDemand_Upload, Resources.ExceptionCLHttpRestAddFilesUploadError, uploadError.Exceptions);
                                            }

                                            if (Data.itemCompletionCallback != null)
                                            {
                                                Data.itemCompletionCallback(Data.inputItemIndex, Data.completedItem, /* error: */ null, Data.itemCompletionCallbackUserState);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (Data.itemCompletionCallback != null)
                                            {
                                                Data.itemCompletionCallback(Data.inputItemIndex, /* completedItem: */ null, ex, Data.itemCompletionCallbackUserState);
                                            }
                                        }
                                        finally
                                        {
                                            // UploadFile should dispose the stream, but do it anyways just in case
                                            StreamOrStreamContextHolder.DisposeHolderInArray(Data.uploadStreams, Data.inputItemIndex);

                                            Data.completionHandle.Set();
                                        }
                                    },
                                    errorToAccumulate: null);

                                (new Task(new Action(uploadForTask.VoidProcess))).Start(HttpScheduler.GetSchedulerByDirection(SyncDirection.To, _syncbox.CopiedSettings));
                            }

                            WaitHandle[] allWaitHandles = filesLeftToUpload.Select(fileToUpload => fileToUpload.Item3).ToArray();
                            WaitHandle.WaitAll(allWaitHandles);
                        }
                    }
                    // else there was nothing in addChanges since every item in the input parameters could not have its stream opened
                    // also, only need to fire completion routines for errors if the completion callback was set
                    else if (itemCompletionCallback != null)
                    {
                        foreach (KeyValuePair<CLError, int> openStreamError in openStreamErrors)
                        {
                            itemCompletionCallback(itemIndex: openStreamError.Value, completedItem: null, error: openStreamError.Key, userState: itemCompletionCallbackUserState);
                        }
                    }
                }
                catch
                {
                    for (int currentDiposalIndex = 0; currentDiposalIndex < uploadStreams.Length; currentDiposalIndex++)
                    {
                        StreamOrStreamContextHolder.DisposeHolderInArray(uploadStreams, currentDiposalIndex);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }
        #endregion  // end AddFiles (Adds files in the syncbox.)

        #region UndoDeletionFileChange
        /// <summary>
        /// Asynchronously starts posting a single FileChange to the server
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="deletionChange">Deletion change which needs to be undone</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="serverUid">Unique server "uid" for the file or folder</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUndoDeletionFileChange(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            FileChange deletionChange,
            int timeoutMilliseconds,
            string serverUid)
        {
            // create the asynchronous result to return
            GenericAsyncResult<UndoDeletionFileChangeResult> toReturn = new GenericAsyncResult<UndoDeletionFileChangeResult>(
                asyncCallback,
                asyncCallbackUserState);

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
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
        /// <param name="asyncResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUndoDeletionFileChange(IAsyncResult asyncResult, out UndoDeletionFileChangeResult result)
        {
            // declare the specific type of asynchronous result for undoing deletion
            GenericAsyncResult<UndoDeletionFileChangeResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for undoing deletion and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for undoing deletion
                castAResult = asyncResult as GenericAsyncResult<UndoDeletionFileChangeResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
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

                //TODO: Rework this function and its async counterparts to use CLFileItem.
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }
                if (deletionChange == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_MissingParameters, Resources.CLHttpRestDeletionChangeCannotBeNull);
                }
                if (deletionChange.Direction == SyncDirection.From)
                {
                    throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestChangeDirectionIsNotToServer);
                }
                if (deletionChange.Metadata == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestMetadataCannotBeNull);
                }
                if (deletionChange.Type != FileChangeType.Deleted)
                {
                    throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestChangeIsNotOfTypeDeletion);
                }
                if (serverUid == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestDeletionChangeMetadataServerUidMustnotBeNull);
                }
                if (string.IsNullOrEmpty(_syncbox.CopiedSettings.DeviceId))
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestDeviceIDCannotBeNull);
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
                        DeviceId = _syncbox.CopiedSettings.DeviceId, // device id
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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
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

        #region CopyFile (needs to be updated before working, see comment inside)
        // !!
        // Needs to be updated before uncommenting! Other on-demand calls have been updated to use CLFileItem and path usages are now difference since the user may not put in a full path for the syncbox root
        // !!

        ///// <summary>
        ///// Asynchronously copies a file on the server to another location
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="fileServerId">Unique id to the file on the server</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="copyTargetPath">Location where file shoule be copied to</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginCopyFile(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    string fileServerId,
        //    int timeoutMilliseconds,
        //    FilePath copyTargetPath)
        //{
        //    return BeginCopyFile(asyncCallback,
        //        asyncCallbackUserState,
        //        fileServerId,
        //        timeoutMilliseconds,
        //        null,
        //        copyTargetPath);
        //}

        ///// <summary>
        ///// Asynchronously copies a file on the server to another location
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="pathToFile">Location of existing file to copy from</param>
        ///// <param name="copyTargetPath">Location where file shoule be copied to</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginCopyFile(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    int timeoutMilliseconds,
        //    FilePath pathToFile,
        //    FilePath copyTargetPath)
        //{
        //    return BeginCopyFile(asyncCallback,
        //        asyncCallbackUserState,
        //        null,
        //        timeoutMilliseconds,
        //        pathToFile,
        //        copyTargetPath);
        //}

        ///// <summary>
        ///// Asynchronously copies a file on the server to another location
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="fileServerId">Unique id to the file on the server</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="pathToFile">Location of existing file to copy from</param>
        ///// <param name="copyTargetPath">Location where file shoule be copied to</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginCopyFile(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    string fileServerId,
        //    int timeoutMilliseconds,
        //    FilePath pathToFile,
        //    FilePath copyTargetPath)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<CopyFileResult> toReturn = new GenericAsyncResult<CopyFileResult>(
        //        asyncCallback,
        //        asyncCallbackUserState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath> asyncParams =
        //        new Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath>(
        //            toReturn,
        //            fileServerId,
        //            timeoutMilliseconds,
        //            pathToFile,
        //            copyTargetPath);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath> castState = state as Tuple<GenericAsyncResult<CopyFileResult>, string, int, FilePath, FilePath>;
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
        //                // declare the specific type of result for this operation
        //                JsonContracts.FileChangeResponse result;
        //                // run the download of the file with the passed parameters, storing any error that occurs
        //                CLError processError = CopyFile(
        //                    castState.Item2,
        //                    castState.Item3,
        //                    castState.Item4,
        //                    out result);

        //                // if there was an asynchronous result in the parameters, then complete it with a new result object
        //                if (castState.Item1 != null)
        //                {
        //                    castState.Item1.Complete(
        //                        new CopyFileResult(
        //                            processError, // any error that may have occurred during processing
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

        ///// <summary>
        ///// Finishes copying a file on the server to another location if it has not already finished via its asynchronous result and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="asyncResult">The asynchronous result provided upon starting copying the file</param>
        ///// <param name="result">(output) The result from copying the file</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //internal CLError EndCopyFile(IAsyncResult asyncResult, out CopyFileResult result)
        //{
        //    // declare the specific type of asynchronous result for copying the file
        //    GenericAsyncResult<CopyFileResult> castAResult;

        //    // try/catch to try casting the asynchronous result as the type for copying the file and pull the result (possibly incomplete), on catch default the output and return the error
        //    try
        //    {
        //        // try cast the asynchronous result as the type for copying the file
        //        castAResult = asyncResult as GenericAsyncResult<CopyFileResult>;

        //        // if trying to cast the asynchronous result failed, then throw an error
        //        if (castAResult == null)
        //        {
        //            throw new NullReferenceException(Resources.CLAsyncResultInternalTypeMismatch);
        //        }

        //        // pull the result for output (may not yet be complete)
        //        result = castAResult.Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        result = Helpers.DefaultForType<CopyFileResult>();
        //        return ex;
        //    }

        //    // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
        //    try
        //    {
        //        // This method assumes that only 1 thread calls EndInvoke 
        //        // for this object
        //        if (!castAResult.IsCompleted)
        //        {
        //            // If the operation isn't done, wait for it
        //            castAResult.AsyncWaitHandle.WaitOne();
        //            castAResult.AsyncWaitHandle.Close();
        //        }

        //        // re-pull the result for output in case it was not completed when it was pulled before
        //        result = castAResult.Result;

        //        // Operation is done: if an exception occurred, return it
        //        if (castAResult.Exception != null)
        //        {
        //            return castAResult.Exception;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex;
        //    }
        //    return null;
        //}

        ///// <summary>
        ///// Copies a file on the server to another location
        ///// </summary>
        ///// <param name="fileServerId">Unique id to the file on the server</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="copyTargetPath">Location where file shoule be copied to</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //internal CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath copyTargetPath, out JsonContracts.FileChangeResponse response)
        //{
        //    return CopyFile(fileServerId, timeoutMilliseconds, null, copyTargetPath, out response);
        //}

        ///// <summary>
        ///// Copies a file on the server to another location
        ///// </summary>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="pathToFile">Location of existing file to copy from</param>
        ///// <param name="copyTargetPath">Location where file shoule be copied to</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //internal CLError CopyFile(int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out JsonContracts.FileChangeResponse response)
        //{
        //    return CopyFile(null, timeoutMilliseconds, pathToFile, copyTargetPath, out response);
        //}

        ///// <summary>
        ///// Copies a file on the server to another location
        ///// </summary>
        ///// <param name="fileServerId">Unique id to the file on the server</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="pathToFile">Location of existing file to copy from</param>
        ///// <param name="copyTargetPath">Location where file shoule be copied to</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //internal CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out JsonContracts.FileChangeResponse response)
        //{
        //    // try/catch to process the undeletion, on catch return the error
        //    try
        //    {
        //        // check input parameters

        //        if (!(timeoutMilliseconds > 0))
        //        {
        //            throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
        //        }
        //        if (_syncbox.Path == null)
        //        {
        //            throw new NullReferenceException(Resources.CLHttpRestSyncboxPathCannotBeNull);
        //        }
        //        if (copyTargetPath == null)
        //        {
        //            throw new NullReferenceException(Resources.CLHttpRestCopyPathCannotBeNull);
        //        }
        //        if (pathToFile == null
        //            && string.IsNullOrEmpty(fileServerId))
        //        {
        //            throw new NullReferenceException(Resources.CLHttpRestXOROldPathServerUidCannotBeNull);
        //        }
        //        if (string.IsNullOrEmpty(_syncbox.CopiedSettings.DeviceId))
        //        {
        //            throw new NullReferenceException(Resources.CLHttpRestDeviceIDCannotBeNull);
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

        //        // run the HTTP communication and store the response object to the output parameter
        //        response = Helpers.ProcessHttp<JsonContracts.FileChangeResponse>(new JsonContracts.FileCopy() // object for file copy
        //            {
        //                DeviceId = _syncbox.CopiedSettings.DeviceId, // device id
        //                ServerId = fileServerId, // unique id on server
        //                RelativePath = (pathToFile == null
        //                    ? null
        //                    : pathToFile.GetRelativePath(_syncbox.Path, true)), // path of existing file to copy
        //                RelativeToPath = copyTargetPath.GetRelativePath(_syncbox.Path, true), // location to copy file to
        //                SyncboxId = _syncbox.SyncboxId // id of sync box
        //            },
        //            CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
        //            CLDefinitions.MethodPathFileCopy, // path for file copy
        //            Helpers.requestMethod.post, // file copy is a post
        //            timeoutMilliseconds, // time before communication timeout
        //            null, // not an upload or download
        //            Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
        //            _syncbox.CopiedSettings, // pass the copied settings
        //            _syncbox.SyncboxId, // pass the unique id of the sync box on the server
        //            requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
        //            true);
        //    }
        //    catch (Exception ex)
        //    {
        //        response = Helpers.DefaultForType<JsonContracts.FileChangeResponse>();
        //        return ex;
        //    }
        //    return null;
        //}
        #endregion

        #region AllImageItems (Get image items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying image items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllImageItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllImageItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllImageItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllImageItemsResult(overallError, items),  // the result
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
        /// Finishes querying image items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllImageItems(IAsyncResult asyncResult, out SyncboxAllImageItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllImageItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query image items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllImageItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the location of the pictures retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPictures + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllImageItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllImageItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    items = Helpers.DefaultForType<CLFileItem[]>();
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllImageItems (Get image items from this syncbox)

        #region AllVideoItems (Get video items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying video items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllVideoItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllVideoItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllVideoItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllVideoItemsResult(overallError, items),
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
        /// Finishes querying video items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllVideoItems(IAsyncResult asyncResult, out SyncboxAllVideoItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllVideoItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query video items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllVideoItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetVideos + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllVideoItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllVideoItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllVideoItems (Get video items from this syncbox)

        #region AllAudioItems (Get audio items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying audio items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllAudioItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllAudioItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllAudioItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllAudioItemsResult(overallError, items),
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
        /// Finishes querying audio items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllAudioItems(IAsyncResult asyncResult, out SyncboxAllAudioItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllAudioItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query audio items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllAudioItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetAudios + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllAudioItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllAudioItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllAudioItems (Get audio items from this syncbox)

        #region AllDocumentItems (Get document items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying document items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllDocumentItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllDocumentItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllDocumentItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllDocumentItemsResult(overallError, items),
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
        /// Finishes querying document items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllDocumentItems(IAsyncResult asyncResult, out SyncboxAllDocumentItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllDocumentItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query document items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllDocumentItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetDocuments + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllDocumentItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllDocumentItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllDocumentItems (Get document items from this syncbox)

        #region AllPresentationItems (Get presentation items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying presentation items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllPresentationItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllPresentationItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllPresentationItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllPresentationItemsResult(overallError, items), // the result
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
        /// Finishes querying presentation items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllPresentationItems(IAsyncResult asyncResult, out SyncboxAllPresentationItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllPresentationItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query presentation items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllPresentationItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPresentations + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllPresentationItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllPresentationItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllPresentationItems (Get presentation items from this syncbox)

        #region AllPlainTextItems (Get text items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying text items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllPlainTextItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllTextItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllPlainTextItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllTextItemsResult(overallError, items),  // the result
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
        /// Finishes querying text items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllPlainTextItems(IAsyncResult asyncResult, out SyncboxAllTextItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllTextItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query text items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllPlainTextItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetTexts + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllTextItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllTextItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllTextItems (Get text items from this syncbox)

        #region AllArchiveItems (Get archive items from this syncbox)
        /// <summary>
        /// Asynchronously starts querying archive items from the syncbox.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllArchiveItems(AsyncCallback asyncCallback, object asyncCallbackUserState, long pageNumber, long itemsPerPage)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllArchiveItemsResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllArchiveItems(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxAllArchiveItemsResult(overallError, items), // the result
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
        /// Finishes querying archive items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllArchiveItems(IAsyncResult asyncResult, out SyncboxAllArchiveItemsResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllArchiveItemsResult>(asyncResult, out result);
        }

        /// <summary>
        /// Query archive items from the syncbox.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllArchiveItems(long pageNumber, long itemsPerPage, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetArchives + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });


                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllArchiveItemsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllArchiveItemsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllArchiveItems (Get archive items from this syncbox)

        #region AllItemsOfTypes (Get file items with various extensions from this syncbox)
        /// <summary>
        /// Asynchronously starts retrieving the <CLFileItems>s of all of the file items contained in the syncbox that have the specified file extensions.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="extensions">The array of file extensions the item type should belong to. I.E txt, jpg, pdf, etc.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginAllItemsOfTypes(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState, 
            long pageNumber, 
            long itemsPerPage,
            params string[] extensions)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxAllItemsOfTypesResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                    extensions = extensions
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = AllItemsOfTypes(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items,
                            Data.extensions);

                        Data.toReturn.Complete(
                            new SyncboxAllItemsOfTypesResult(overallError, items),  // result
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
        /// Finishes retrieving the <CLFileItems>s of all of the file items contained in the syncbox that have the specified file extensions, 
        /// if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndAllItemsOfTypes(IAsyncResult asyncResult, out SyncboxAllItemsOfTypesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxAllItemsOfTypesResult>(asyncResult, out result);
        }

        /// <summary>
        /// Retrieves the <CLFileItems>s of all of the file items contained in the syncbox that have the specified file extensions.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The resulting file items.</param>
        /// <param name="extensions">The array of file extensions the item type should belong to. I.E txt, jpg, pdf, etc.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError AllItemsOfTypes(long pageNumber, long itemsPerPage, out CLFileItem[] items, params string[] extensions)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }
                if (extensions == null
                    || extensions.Length < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidExtensions);
                }

                // Check for null extensions
                foreach (string extension in extensions)
                {
                    if (String.IsNullOrEmpty(extension))
                    {
                        throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidExtension);
                    }
                }

                // Build an escaped list of the extensions in JSON format.  e.g.: escaped("[\"abc\", \"xyz\"]"
                string sExtensionArray = "[";
                foreach (string extension in extensions)
                {
                    sExtensionArray += "\"" + extension + "\"";
                }
                sExtensionArray += "\"";

                // Escape the extensions array.
                sExtensionArray = Uri.EscapeUriString(sExtensionArray);

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetExtensions + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                        // sExtensionArray has already been escaped.
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringExtensions, sExtensionArray),
                    });

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetAllItemsForTypesResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetAllItemsForTypesResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end AllItemsForTypes (Get file items with various extensions from this syncbox)

        #region RecentFilesSinceDate (Retrieves recently modified <CLFileItems>s since a particular date.)
        /// <summary>
        /// Asynchronously starts retrieving the recently modified files (<CLFileItems>s) from the syncbox since a particular date.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="sinceDate">(optional) null to retrieve all of the recents, or specify a date to retrieve items from that date forward.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginRecentFilesSinceDate(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState, 
            long pageNumber, 
            long itemsPerPage,
            Nullable<DateTime> sinceDate = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxRecentFilesSinceDateResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    pageNumber = pageNumber,
                    itemsPerPage = itemsPerPage,
                    sinceDate = sinceDate
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = RecentFilesSinceDate(
                            Data.pageNumber,
                            Data.itemsPerPage,
                            out items,
                            Data.sinceDate);

                        Data.toReturn.Complete(
                            new SyncboxRecentFilesSinceDateResult(
                                overallError, // any overall error that may have occurred during processing
                                items),
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
        /// Finishes retrieving recent file items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndRecentFilesSinceDate(IAsyncResult asyncResult, out SyncboxRecentFilesSinceDateResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxRecentFilesSinceDateResult>(asyncResult, out result);
        }

        /// <summary>
        /// Rretrieve the recently modified files (<CLFileItems>s) from the syncbox since a particular date.
        /// </summary>
        /// <param name="pageNumber">Beginning page number.  The first page is page 1.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="items">(output) The retrieved items.</param>
        /// <param name="sinceDate">(optional) null to retrieve all of the recents, or specify a date to retrieve items from that date forward.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError RecentFilesSinceDate(long pageNumber, long itemsPerPage, out CLFileItem[] items, Nullable<DateTime> sinceDate = null)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (pageNumber < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidPageNumber);
                }
                if (itemsPerPage < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidItemsPerPage);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetRecents + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, pageNumber.ToString()),
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, itemsPerPage.ToString()),
                    });

                // Add the UTC date if specified.
                if (sinceDate != null)
                {
                    string updatedAfter = Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.CLSyncboxUpdatedAfter, 
                            ((DateTime)sinceDate).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    });
                    serverMethodPath += "&" + updatedAfter.Substring(1);  // skip the leading "?" from QueryStringBuilder.
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetRecentsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetRecentsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end RecentFilesSinceDate (Retrieves recently modified <CLFileItems>s since a particular date.)

        #region RecentFiles (Retrieves the specified number of recently modified <CLFileItems>s.)
        /// <summary>
        /// Asynchronously starts retrieving up to the given number of recently modified syncbox files.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="returnLimit">The maximum number of file items to retrieve.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginRecentFiles(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            long returnLimit)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxRecentFilesResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    returnLimit = returnLimit
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLFileItem[] items;
                        CLError overallError = RecentFiles(
                            Data.returnLimit,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxRecentFilesResult(
                                overallError, // any overall error that may have occurred during processing
                                items),
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
        /// Finishes retrieving recent file items from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndRecentFiles(IAsyncResult asyncResult, out SyncboxRecentFilesResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxRecentFilesResult>(asyncResult, out result);
        }

        /// <summary>
        /// Retrieve up to the given number of recently modified syncbox files.
        /// </summary>
        /// <param name="returnLimit">The maximum number of file items to retrieve.</param>
        /// <param name="items">(output) The retrieved items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError RecentFiles(long returnLimit, out CLFileItem[] items)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters.
                if (returnLimit < 1)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_InvalidParameters, Resources.ExceptionOnDemandInvalidReturnLimit);
                }

                // build the URL with query string dynamically.
                string serverMethodPath =
                    CLDefinitions.MethodPathGetRecents + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // pageNumber should not need escaping since it is an integer.  Get page 1.
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPageNumber, ((byte)1).ToString()), // query string parameter for optional depth limit
                        // itemsPerPage should not need escaping since it is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringPerPage, returnLimit.ToString()),
                    });

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                // Communicate with the server to get the response.
                JsonContracts.SyncboxGetRecentsResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxGetRecentsResponse>(null, // no request body for get
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.get, // one-off methods are all posts
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, //use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                // Convert these items to the output array.
                if (responseFromServer != null && responseFromServer.Metadata != null && responseFromServer.TotalCount != null)
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }

                    // No error.  Pass back the data.
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }

            return null;
        }

        #endregion  // end RecentFiles (Retrieves the specified number of recently modified <CLFileItems>s.)

        #region GetDataUsage (get the usage information for this syncbox)
        /// <summary>
        /// Asynchronously starts getting the syncbox usage information.  
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="completionCallback">Callback method to fire when the operation is complete.  Returns the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginGetDataUsage<T>(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            Action<JsonContracts.SyncboxUsageResponse, T> completionCallback,
            T completionCallbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    completionCallback = completionCallback,
                    completionCallbackUserState = completionCallbackUserState,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = GetDataUsage(
                            Data.completionCallback,
                            Data.completionCallbackUserState);

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
        /// Finishes getting the syncbox usage information, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndGetDataUsage(IAsyncResult asyncResult, out SyncboxUsageResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxUsageResult>(asyncResult, out result);
        }

        /// <summary>
        /// Get the syncbox usage information.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="completionCallback">Callback method to fire when the operation is complete.  Returns the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetDataUsage<T>(
            Action<JsonContracts.SyncboxUsageResponse, T> completionCallback,
            T completionCallbackUserState)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // build the location of the sync box usage retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathSyncboxUsage + // path
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
                SyncboxUsageResponse responseFromServer = Helpers.ProcessHttp<JsonContracts.SyncboxUsageResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query synx box usage (dynamic adding query string)
                    Helpers.requestMethod.get, // query sync box usage is a get
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);
                if (responseFromServer != null 
                    && responseFromServer.Limit != null
                    && responseFromServer.Local != null
                    && responseFromServer.Shared != null)
                {
                    // No error.  Pass back the data via the completion callback.
                    if (completionCallback != null)
                    {
                        completionCallback(responseFromServer, completionCallbackUserState);
                    }
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        #endregion

        #region ItemsForPath (Query the server for the folder contents at a path)
        /// <summary>
        /// Asynchronously starts querying folder contents at a relative syncbox path.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="relativePath">(optional) relative root path of contents query.  If this is null or empty, the syncbox root folder will be queried.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginItemsForPath(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            string relativePath = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxItemsAtPathResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    relativePath = relativePath
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
                        CLError processError = ItemsForPath(
                            Data.relativePath,
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxItemsAtPathResult(
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
        /// <param name="asyncResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndItemsForPath(IAsyncResult asyncResult, out SyncboxItemsAtPathResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxItemsAtPathResult>(asyncResult, out result);
        }

        /// <summary>
        /// Queries server for folder contents at a relative syncbox path.
        /// </summary>
        /// <param name="relativePath">(optional) relative root path of contents query.  If this is null or empty, the syncbox root folder will be queried.</param>
        /// <param name="items">(output) resulting items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError ItemsForPath(
            string relativePath,
            out CLFileItem[] items)
        {
            // try/catch to process the folder contents query, on catch return the error
            try
            {
                // check input parameters

                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = "/";         // assume the syncbox root
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // build the location of the folder contents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderContents + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDepth, ((byte)0).ToString()), // query string parameter for optional depth limit

                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(relativePath.Replace(((char)0x5C /* '\' */), ((char)0x2F /* '/' */)))), // query string parameter for optional path with escaped value

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
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end ItemsForPath (Query the server for the folder contents at a path)

        #region ItemsForFolderItem (Query the server for the folder contents at a folder item)
        /// <summary>
        /// Asynchronously starts querying folder contents at a relative syncbox path.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the contents of the synbox root folder will be returned.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginItemsForFolderItem(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItem folderItem,
            bool includePending,
            bool includeDeleted)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxItemsForFolderItemResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    folderItem = folderItem,
                    includePending = includePending,
                    includeDeleted = includeDeleted
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
                        CLError processError = ItemsForFolderItem(
                            Data.folderItem,
                            out response,
                            Data.includePending,
                            Data.includeDeleted);

                        Data.toReturn.Complete(
                            new SyncboxItemsForFolderItemResult(
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
        /// <param name="asyncResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndItemsForFolderItem(IAsyncResult asyncResult, out SyncboxItemsForFolderItemResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxItemsForFolderItemResult>(asyncResult, out result);
        }

        /// <summary>
        /// Queries server for folder contents at a relative syncbox path.
        /// </summary>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the syncbox root folder will be queried.</param>
        /// <param name="items">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError ItemsForFolderItem(
            CLFileItem folderItem,
            out CLFileItem[] items,
            bool includePending,
            bool includeDeleted)
        {
            // try/catch to process the folder contents query, on catch return the error
            try
            {
                // check input parameters
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }
                if (folderItem != null)
                {
                    if (folderItem.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncbox);
                    }
                    if (!folderItem.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpected);
                    }
                    if (folderItem.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandItemWasPreviouslyDeleted);
                    }
                }

                // build the location of the folder contents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderContents + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDepth, ((byte)0).ToString()), // query string parameter for optional depth limit

                        // Fill in the uid only if it is supplied.
                        (folderItem == null || folderItem.ItemUid == null)
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(folderItem.ItemUid)),

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeDeleted, includeDeleted.ToString()), // query string parameter for not including deleted objects

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeCount, "true"), // query string parameter for including counts within each folder

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeFolders, "true"), // query string parameter for including folders in the list

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeStoredOnly, (!includePending).ToString()) // query string parameter for including only stored items in the list
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
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end ItemsForFolderItem (Query the server for the folder contents at a folder item)

        #region HierarchyOfFolderAtPath (Gets the items that represent the specified folder's folder hierarchy)
        /// <summary>
        /// Asynchronously starts getting the syncbox items that represent the specified folder's folder hierarchy.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="relativePath">(optional) relative root path of contents query.  If this is null or empty, the syncbox root folder will be queried.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginHierarchyOfFolderAtPath(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string relativePath = null)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxHierarchyOfFolderAtPathResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    relativePath = relativePath
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
                        CLError processError = HierarchyOfFolderAtPath(
                            Data.relativePath,
                            out response);

                        Data.toReturn.Complete(
                            new SyncboxHierarchyOfFolderAtPathResult(
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
        /// Finishes getting the folder hierarchy, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndHierarchyOfFolderAtPath(IAsyncResult asyncResult, out SyncboxHierarchyOfFolderAtPathResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxHierarchyOfFolderAtPathResult>(asyncResult, out result);
        }

        /// <summary>
        /// Gets the syncbox items that represent the specified folder's folder hierarchy.
        /// </summary>
        /// <param name="relativePath">(optional) relative root path of contents query.  If this is null or empty, the syncbox root folder will be queried.</param>
        /// <param name="items">(output) resulting items.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError HierarchyOfFolderAtPath(
            string relativePath,
            out CLFileItem[] items)
        {
            // try/catch to process the folder contents query, on catch return the error
            try
            {
                // check input parameters

                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = "/";         // assume the syncbox root
                }

                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // build the location of the folder contents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderHierarchy + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDepth, ((byte)0).ToString()), // query string parameter for optional depth limit

                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(relativePath.Replace(((char)0x5C /* '\' */), ((char)0x2F /* '/' */)))), // query string parameter for optional path with escaped value

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
                JsonContracts.FoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.FoldersResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder contents (dynamic adding query string)
                    Helpers.requestMethod.get, // query folder contents is a get
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end HierarchyOfFolderAtPath (Gets the items that represent the specified folder's folder hierarchy)

        #region HierarchyOfFolderAtFolderItem (Query the server for the folder hierarchy at a folder item)
        /// <summary>
        /// Asynchronously starts querying the syncbox folder hierarchy at a particular folder item.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the hierarchy of the synbox root folder will be returned.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginHierarchyOfFolderAtFolderItem(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItem folderItem)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<SyncboxHierarchyOfFolderAtFolderItemResult>(
                        asyncCallback,
                        asyncCallbackUserState),
                    folderItem = folderItem,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // declare the specific type of result for this operation
                        CLFileItem[] items;
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError processError = HierarchyOfFolderAtFolderItem(
                            Data.folderItem,
                            out items);

                        Data.toReturn.Complete(
                            new SyncboxHierarchyOfFolderAtFolderItemResult(
                                processError, // any error that may have occurred during processing
                                items), // the specific type of result for this operation
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
        /// Finishes getting the folder hierarchy if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndHierarchyOfFolderAtFolderItem(IAsyncResult asyncResult, out SyncboxHierarchyOfFolderAtFolderItemResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxHierarchyOfFolderAtFolderItemResult>(asyncResult, out result);
        }

        /// <summary>
        /// Queries the syncbox folder hierarchy at a particular folder item.
        /// </summary>
        /// <param name="folderItem">The CLFileItem representing the folder to query.  If folderItem is null, the syncbox root folder will be queried.</param>
        /// <param name="items">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError HierarchyOfFolderAtFolderItem(
            CLFileItem folderItem,
            out CLFileItem[] items)
        {
            // try/catch to process the folder contents query, on catch return the error
            try
            {
                // check input parameters
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }
                if (folderItem != null)
                {
                    if (folderItem.Syncbox != _syncbox)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_NotCreatedInThisSyncbox, Resources.ExceptionOnDemandCLFileItemNotCreatedInThisSyncbox);
                    }
                    if (!folderItem.IsFolder)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_FileItemWhenFolderItemExpected, Resources.ExceptionOnDemandFileItemFoundWhenFolderItemExpected);
                    }
                    if (folderItem.IsDeleted)
                    {
                        throw new CLInvalidOperationException(CLExceptionCode.OnDemand_AlreadyDeleted, Resources.ExceptionOnDemandItemWasPreviouslyDeletedMsg0);
                    }
                }

                // build the location of the folder contents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderHierarchy + // path
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),

                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDepth, ((byte)0).ToString()), // query string parameter for optional depth limit

                        // Fill in the uid only if it is supplied.
                        (folderItem == null || folderItem.ItemUid == null)
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(folderItem.ItemUid)),

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
                JsonContracts.FoldersResponse responseFromServer;
                responseFromServer = Helpers.ProcessHttp<JsonContracts.FoldersResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder contents (dynamic adding query string)
                    Helpers.requestMethod.get, // query folder contents is a get
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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
                            throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionCLHttpRestWithoutMetadata);
                        }
                    }
                    items = listFileItems.ToArray();
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionCLHttpRestWithoutMetadata);
                }
            }
            catch (Exception ex)
            {
                items = Helpers.DefaultForType<CLFileItem[]>();
                return ex;
            }
            return null;
        }
        #endregion  // end HierarchyOfFolderAtFolderItem (Query the server for the folder hierarchy at a folder item)

        #region UpdateSyncboxExtendedMetadata
        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateSyncboxExtendedMetadata<T>(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            IDictionary<string, T> metadata,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUpdateExtendedMetadataResult> toReturn = new GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>(
                asyncCallback,
                asyncCallbackUserState);

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
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateSyncboxExtendedMetadata(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            MetadataDictionary metadata,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncboxUpdateExtendedMetadataResult> toReturn = new GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>(
                asyncCallback,
                asyncCallbackUserState);

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
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
        /// <param name="asyncResult">The asynchronous result provided upon starting updating extended metadata</param>
        /// <param name="result">(output) The result from updating extended metadata</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUpdateSyncboxExtendedMetadata(IAsyncResult asyncResult, out SyncboxUpdateExtendedMetadataResult result)
        {
            // declare the specific type of asynchronous result for updating extended metadata
            GenericAsyncResult<SyncboxUpdateExtendedMetadataResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating extended metadata and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for updating extended metadata
                castAResult = asyncResult as GenericAsyncResult<SyncboxUpdateExtendedMetadataResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
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
        internal CLError UpdateSyncboxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
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
        internal CLError UpdateSyncboxExtendedMetadata(MetadataDictionary metadata, int timeoutMilliseconds, out JsonContracts.SyncboxResponse response)
        {
            // try/catch to process setting extended metadata, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
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

        #region UpdateStoragePlan (change the storage plan associated with this syncbox)
        /// <summary>
        /// Asynchronously starts changing the storage plan associated with this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Return the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="storagePlan">The new storage plan to use for this syncbox)</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateStoragePlan<T>(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T> completionCallback,
            T completionCallbackUserState,
            bool reservedForActiveSync,
            CLStoragePlan storagePlan)
        {
            GenericAsyncResult<CLError> toReturn = new GenericAsyncResult<CLError>(asyncCallback, asyncCallbackUserState);

            if (reservedForActiveSync)
            {
                toReturn.Complete(
                        UpdateStoragePlan<T>(
                            completionCallback,
                            completionCallbackUserState,
                            reservedForActiveSync,
                            storagePlan),
                    sCompleted: true);

                return toReturn;
            }

            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = toReturn,
                    completionCallback = completionCallback,
                    completionCallbackUserState = completionCallbackUserState,
                    reservedForActiveSync = reservedForActiveSync,
                    storagePlan = storagePlan,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = UpdateStoragePlan(
                            Data.completionCallback,
                            Data.completionCallbackUserState,
                            Data.reservedForActiveSync,
                            Data.storagePlan);

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
        /// Finishes changing the storage plan associated with this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUpdateStoragePlan(IAsyncResult asyncResult, out SyncboxUpdateStoragePlanResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxUpdateStoragePlanResult>(asyncResult, out result);
        }

        /// <summary>
        /// Changes the storage plan associated with this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Return the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="storagePlan">The new storage plan to use for this syncbox)</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateStoragePlan<T>(
            Action<JsonContracts.SyncboxUpdateStoragePlanResponse, T> completionCallback,
            T completionCallbackUserState,
            bool reservedForActiveSync,
            CLStoragePlan storagePlan)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                if (reservedForActiveSync)
                {
                    return new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }
                if (storagePlan == null)
                {
                    throw new CLArgumentNullException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandUpdateStoragePlanStoragePlanMustNotBeNull);
                }

                // build the location of the sync box usage retrieval method on the server dynamically
                string serverMethodPath = CLDefinitions.MethodPathAuthSyncboxUpdatePlan;

                // If the user wants to handle temporary tokens, we will build the extra optional parameters to pass to ProcessHttp.
                Helpers.RequestNewCredentialsInfo requestNewCredentialsInfo = new Helpers.RequestNewCredentialsInfo()
                {
                    ProcessingStateByThreadId = _processingStateByThreadId,
                    GetNewCredentialsCallback = _getNewCredentialsCallback,
                    GetNewCredentialsCallbackUserState = _getNewCredentialsCallbackUserState,
                    GetCurrentCredentialsCallback = GetCurrentCredentialsCallback,
                    SetCurrentCredentialsCallback = SetCurrentCredentialCallback,
                };

                SyncboxUpdateStoragePlanResponse serverResponse = Helpers.ProcessHttp<JsonContracts.SyncboxUpdateStoragePlanResponse>(
                    new JsonContracts.SyncboxUpdateStoragePlanRequest() // json contract object for sync box update plan request
                    {
                        SyncboxId = _syncbox.SyncboxId,
                        PlanId = storagePlan.PlanId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    serverMethodPath,   // method path
                    Helpers.requestMethod.post, // sync box update plan is a post operation
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box update plan should give OK or Accepted
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                if (serverResponse == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionOnDemandUpdateStoragePlanStoragePlanServerReturnedNullResponse);
                }
                if (serverResponse.Syncbox == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandUpdateStoragePlanStoragePlanServerReturnedNullResponseSyncbox);
                }
                if (serverResponse.Syncbox.PlanId == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandUpdateStoragePlanStoragePlanServerReturnedNullResponseSyncboxPlanId);
                }

                //// todo: "success" does not compare with "ok", but do not want to compare strings anyways since some methods are "success" and some are "ok"
                //if (serverResponse.Status != CLDefinitions.CLEventTypeAccepted)
                //{
                //    throw new Exception(String.Format("server returned error status {0}, message {1}.", serverResponse.Status, serverResponse.Message));  //&&&& fix this
                //}

                if (completionCallback != null)
                {
                    try
                    {
                        completionCallback(serverResponse, completionCallbackUserState);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end UpdateStoragePlan (change the storage plan associated with this syncbox)

        #region UpdateFriendlyName (change the friendly name of this syncbox)
        /// <summary>
        /// Asynchronously starts changing the friendly name of this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Return the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <param name="friendlyName">The new friendly name of this syncbox)</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateFriendlyName<T>(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            bool reservedForActiveSync, 
            Action<JsonContracts.SyncboxResponse, T> completionCallback,
            T completionCallbackUserState,
            string friendlyName)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    reservedForActiveSync = reservedForActiveSync,
                    completionCallback = completionCallback,
                    completionCallbackUserState = completionCallbackUserState,
                    friendlyName = friendlyName,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = UpdateFriendlyName(
                            Data.reservedForActiveSync,
                            Data.completionCallback,
                            Data.completionCallbackUserState,
                            Data.friendlyName);

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
        /// Finishes changing the friendly name of this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUpdateFriendlyName(IAsyncResult asyncResult, out SyncboxUpdateFriendlyNameResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxUpdateFriendlyNameResult>(asyncResult, out result);
        }

        /// <summary>
        /// Changes the friendly name of this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="reservedForActiveSync">true: Live sync is active.  User calls are not allowed.</param>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Return the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <param name="friendlyName">The new friendly name of this syncbox)</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateFriendlyName<T>(
            bool reservedForActiveSync, 
            Action<JsonContracts.SyncboxResponse, T> completionCallback,
            T completionCallbackUserState,
            string friendlyName)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // This method modifies the syncbox.  It is incompatible with live sync.
                if (reservedForActiveSync)
                {
                    throw new CLInvalidOperationException(CLExceptionCode.OnDemand_LiveSyncIsActive, Resources.CLHttpRestCurrentSyncboxCannotBeModifiedWhileSyncing);
                }
                IncrementModifyingSyncboxViaPublicAPICalls();

                // check input parameters
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }
                if (String.IsNullOrEmpty(friendlyName))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandUpdateFriendlyNameFriendlyNameMustBeSpecified);
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

                string serverMethodPath = CLDefinitions.MethodPathAuthSyncboxUpdate;
                SyncboxUpdateFriendlyNameRequest friendlyNameRequest = new SyncboxUpdateFriendlyNameRequest()
                {
                    FriendlyName = friendlyName
                };
                SyncboxResponse serverResponse = Helpers.ProcessHttp<JsonContracts.SyncboxResponse>(
                    new JsonContracts.SyncboxUpdateRequest() // json contract object for sync box update plan request
                    {
                        SyncboxId = _syncbox.SyncboxId,
                        Syncbox = friendlyNameRequest,
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    serverMethodPath,   // method path
                    Helpers.requestMethod.post, // sync box update plan is a post operation
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box update plan should give OK or Accepted
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                if (serverResponse == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionOnDemandUpdateFriendlyNameServerReturnedNullResponse);
                }
                if (serverResponse.Syncbox == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandUpdateFriendlyNameServerReturnedNullResponseSyncbox);
                }
                if (serverResponse.Syncbox.PlanId == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandUpdateFriendlyNameServerReturnedNullResponseSyncboxPlanId);
                }

                //// todo: "success" does not compare with "ok", but do not want to compare strings anyways since some methods are "success" and some are "ok"
                //if (serverResponse.Status != CLDefinitions.CLEventTypeAccepted)
                //{
                //    throw new Exception(String.Format("server returned error status {0}, message {1}.", serverResponse.Status, serverResponse.Message));  //&&&& fix this
                //}

                if (completionCallback != null)
                {
                    try
                    {
                        completionCallback(serverResponse, completionCallbackUserState);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }

        #endregion  // end UpdateFriendlyName (change the friendly name of this syncbox)

        #region GetCurrentStatus (Get the current status of this syncbox)
        /// <summary>
        /// Asynchronously starts getting the current status of this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing the async callback above.</param>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Return the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginGetCurrentStatus<T>(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            Action<JsonContracts.SyncboxStatusResponse, T> completionCallback,
            T completionCallbackUserState)
        {
            var asyncThread = DelegateAndDataHolderBase.Create(
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                new
                {
                    // create the asynchronous result to return
                    toReturn = new GenericAsyncResult<CLError>(
                        asyncCallback,
                        asyncCallbackUserState),
                    completionCallback = completionCallback,
                    completionCallbackUserState = completionCallbackUserState,
                },
                (Data, errorToAccumulate) =>
                {
                    // The ThreadProc.
                    // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
                    try
                    {
                        // alloc and init the syncbox with the passed parameters, storing any error that occurs
                        CLError overallError = GetCurrentStatus(
                            Data.completionCallback,
                            Data.completionCallbackUserState);

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
        /// Finishes getting the current statu of this syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting the request</param>
        /// <param name="result">(output) An overall error which occurred during processing, if any</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndGetCurrentStatus(IAsyncResult asyncResult, out SyncboxStatusResult result)
        {
            return Helpers.EndAsyncOperation<SyncboxStatusResult>(asyncResult, out result);
        }

        /// <summary>
        /// Gets ths curret statu of this syncbox.  Updates the information in this syncbox object.
        /// </summary>
        /// <param name="completionCallback">Callback method to fire when a page of items is complete.  Return the result.</param>
        /// <param name="completionCallbackUserState">User state to be passed whenever the completion callback above is fired.</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        /// 

        // /cond
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] //Hides From Intelisense
        public CLError GetCurrentStatus<T>(
            Action<JsonContracts.SyncboxStatusResponse, T> completionCallback,
            T completionCallbackUserState)
        {
            // try/catch to process the request,  On catch return the error
            try
            {
                // check input parameters
                if (!(_syncbox.CopiedSettings.HttpTimeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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

                SyncboxStatusResponse serverResponse = Helpers.ProcessHttp<JsonContracts.SyncboxStatusResponse>(new JsonContracts.SyncboxIdOnly() // json contract object for purge pending method
                    {
                        Id = _syncbox.SyncboxId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncboxStatus, // sync box status address
                    Helpers.requestMethod.post, // sync box status is a post operation
                    _syncbox.CopiedSettings.HttpTimeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box status should give OK or Accepted
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    true);

                if (serverResponse == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_NoServerResponse, Resources.ExceptionOnDemandNullServerResponse);
                }
                if (serverResponse.Syncbox == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandGetCurrentStatusServerResponseNullSyncbox);
                }
                if (serverResponse.Syncbox.PlanId == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandGetCurrentStatusServerResponseNullSyncboxPlanId);
                }
                if (serverResponse.Syncbox.CreatedAt == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_ServerReturnedInvalidItem, Resources.ExceptionOnDemandGetCurrentStatusServerResponseNullSyncboxCreatedAt);
                }

                //// todo: "success" does not compare with "ok", but do not want to compare strings anyways since some methods are "success" and some are "ok"
                //if (serverResponse.Status != CLDefinitions.CLEventTypeAccepted)
                //{
                //    throw new Exception(String.Format("server returned error status {0}, message {1}.", serverResponse.Status, serverResponse.Message));  //&&&& fix this
                //}

                if (completionCallback != null)
                {
                    try
                    {
                        completionCallback(serverResponse, completionCallbackUserState);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DecrementModifyingSyncboxViaPublicAPICalls();
            }

            return null;
        }
        // /endcond
        #endregion  // end GetCurrentStatus (Get the current status of this syncbox)

        #endregion

        #region internal API calls
        #region unregioned
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
                    throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestSyncToRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox to use
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
                    throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestPushRequestMustNotBeNull);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                JsonContracts.NotificationUnsubscribeRequest request = new JsonContracts.NotificationUnsubscribeRequest()
                {
                    DeviceId = _syncbox.CopiedSettings.DeviceId,
                    SyncboxId = _syncbox.SyncboxId
                };

                // Build the query string.
                string query = Helpers.QueryStringBuilder(
                    new[]
                    {
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()), // no need to escape string characters since the source is an integer
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSender, Uri.EscapeDataString(_syncbox.CopiedSettings.DeviceId)) // possibly user-provided string, therefore needs escaping
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
                    _syncbox.CopiedSettings,
                    _syncbox, // pass the syncbox to use
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

        #endregion

        #region GetMetadata (partially commented out since methods have not been updated for CLFileItem and usage with a possibly null or empty syncbox path, currently used in the previous manner by sync engine, see comment inside)
        // !!
        // Needs to be updated before uncommenting! Other on-demand calls have been updated to use CLFileItem and path usages are now difference since the user may not put in a full path for the syncbox root
        // Also, sync engine is currently using the pieces left uncommented internally so if changed to allow public access make sure not to break sync engine usage
        // !!

        ///// <summary>
        ///// Asynchronously starts querying the server at a given file or folder path (must be specified) for existing metadata at that path
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        ///// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginGetMetadata(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    FilePath fullPath,
        //    bool isFolder,
        //    int timeoutMilliseconds)
        //{
        //    return BeginGetMetadata(asyncCallback, asyncCallbackUserState, fullPath, /*serverId*/ null, isFolder, timeoutMilliseconds);
        //}

        ///// <summary>
        ///// Asynchronously starts querying the server at a given file or folder server id (must be specified) for existing metadata at that id
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        ///// <param name="serverId">Unique id of the item on the server</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginGetMetadata(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    bool isFolder,
        //    string serverId,
        //    int timeoutMilliseconds)
        //{
        //    return BeginGetMetadata(asyncCallback, asyncCallbackUserState, /*fullPath*/ null, serverId, isFolder, timeoutMilliseconds);
        //}

        ///// <summary>
        ///// Private helper to combine two overloaded public versions: Asynchronously starts querying the server at a given file or folder path (must be specified) for existing metadata at that path
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        ///// <param name="serverId">Unique id of the item on the server</param>
        ///// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //private IAsyncResult BeginGetMetadata(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    FilePath fullPath,
        //    string serverId,
        //    bool isFolder,
        //    int timeoutMilliseconds)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<GetMetadataResult> toReturn = new GenericAsyncResult<GetMetadataResult>(
        //        asyncCallback,
        //        asyncCallbackUserState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int> asyncParams =
        //        new Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int>(
        //            toReturn,
        //            fullPath,
        //            serverId,
        //            isFolder,
        //            timeoutMilliseconds);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int> castState = state as Tuple<GenericAsyncResult<GetMetadataResult>, FilePath, string, bool, int>;
        //        // if the try cast failed, then show a message box for this unrecoverable error
        //        if (castState == null)
        //        {
        //            MessageEvents.FireNewEventMessage(
        //                Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
        //                EventMessageLevel.Important,
        //                new HaltAllOfCloudSDKErrorInfo());
        //        }
        //        // else if the try cast did not fail, then start processing with the input parameters
        //        else
        //        {
        //            // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
        //            try
        //            {
        //                // declare the specific type of result for this operation
        //                JsonContracts.SyncboxMetadataResponse result;
        //                // run the download of the file with the passed parameters, storing any error that occurs
        //                CLError processError = GetMetadata(
        //                    castState.Item2,
        //                    castState.Item3,
        //                    castState.Item4,
        //                    castState.Item5,
        //                    out result);

        //                // if there was an asynchronous result in the parameters, then complete it with a new result object
        //                if (castState.Item1 != null)
        //                {
        //                    castState.Item1.Complete(
        //                        new GetMetadataResult(
        //                            processError, // any error that may have occurred during processing
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

        ///// <summary>
        ///// Finishes a metadata query if it has not already finished via its asynchronous result and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="asyncResult">The asynchronous result provided upon starting the metadata query</param>
        ///// <param name="result">(output) The result from the metadata query</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //internal CLError EndGetMetadata(IAsyncResult asyncResult, out GetMetadataResult result)
        //{
        //    // declare the specific type of asynchronous result for metadata query
        //    GenericAsyncResult<GetMetadataResult> castAResult;

        //    // try/catch to try casting the asynchronous result as the type for metadata query and pull the result (possibly incomplete), on catch default the output and return the error
        //    try
        //    {
        //        // try cast the asynchronous result as the type for metadata query
        //        castAResult = asyncResult as GenericAsyncResult<GetMetadataResult>;

        //        // if trying to cast the asynchronous result failed, then throw an error
        //        if (castAResult == null)
        //        {
        //            throw new NullReferenceException(Resources.CLAsyncResultInternalTypeMismatch);
        //        }

        //        // pull the result for output (may not yet be complete)
        //        result = castAResult.Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        result = Helpers.DefaultForType<GetMetadataResult>();
        //        return ex;
        //    }

        //    // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
        //    try
        //    {
        //        // This method assumes that only 1 thread calls EndInvoke 
        //        // for this object
        //        if (!castAResult.IsCompleted)
        //        {
        //            // If the operation isn't done, wait for it
        //            castAResult.AsyncWaitHandle.WaitOne();
        //            castAResult.AsyncWaitHandle.Close();
        //        }

        //        // re-pull the result for output in case it was not completed when it was pulled before
        //        result = castAResult.Result;

        //        // Operation is done: if an exception occurred, return it
        //        if (castAResult.Exception != null)
        //        {
        //            return castAResult.Exception;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex;
        //    }
        //    return null;
        //}

        /// <summary>
        /// Private helper to combine two overloaded public versions: Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="serverUid">Unique id of the item on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError GetMetadata(bool isFolder, string serverUid, int timeoutMilliseconds, out JsonContracts.SyncboxMetadataResponse response)
        {
            return GetMetadata(/*fullPath*/ null, serverUid, isFolder, timeoutMilliseconds, out response);
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
        /// <param name="serverUid">Unique id of the item on the server</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        private CLError GetMetadata(FilePath fullPath, string serverUid, bool isFolder, int timeoutMilliseconds, out JsonContracts.SyncboxMetadataResponse response)
        {
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (fullPath == null
                    && string.IsNullOrEmpty(serverUid))
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestFullPathorServerUidRequired);
                }
                if (fullPath != null)
                {
                    CLError pathError = Helpers.CheckForBadPath(fullPath);
                    if (pathError != null)
                    {
                        throw new CLException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestFullPathBadFormat, pathError.Exceptions);
                    }

                    if (string.IsNullOrEmpty(_syncbox.Path))
                    {
                        throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestSyncboxPathCannotBeNull);
                    }

                    if (!fullPath.Contains(_syncbox.Path))
                    {
                        throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestFullPathDoesNotContainSettingsSyncboxPath);
                    }
                } 

                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    (isFolder
                        ? CLDefinitions.MethodPathGetFolderMetadata // if the current metadata is for a folder, then retrieve it from the folder method
                        : CLDefinitions.MethodPathGetFileMetadata) + // else if the current metadata is for a file, then retrieve it from the file method
                    Helpers.QueryStringBuilder(new[] // both methods grab their parameters by query string (since this method is an HTTP GET)
                    {
                        (string.IsNullOrEmpty(serverUid)
                            ? // query string parameter for the path to query, built by turning the full path location into a relative path from the cloud root and then escaping the whole thing for a url
                                new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(fullPath.GetRelativePath((_syncbox.Path ?? string.Empty), true) + (isFolder ? ((char)0x2F).ToString() /* '/' */ : string.Empty)))

                            : // query string parameter for the unique id to the file or folder on the server, escaped since it is a server opaque field of undefined format
                                new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(serverUid))),

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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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

        #region UploadFile
        /// <summary>
        /// Asynchronously starts uploading a file from a provided stream and file upload change
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire upon progress changes in upload, make sure it processes quickly if the IAsyncResult IsCompleted is false</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="uploadStream">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, and MD5 hash to be set</param>
        /// <param name="uid">The server UID of the file being uploaded.</param>
        /// <param name="revision">The revision of the file being uploaded.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <returns>Returns the asynchronous result which is used to retrieve progress and/or the result</returns>
        internal IAsyncResult BeginUploadFile(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            Stream uploadStream,
            FileChange changeToUpload,
            string uid,
            string revision,
            int timeoutMilliseconds,
            CancellationTokenSource shutdownToken = null)
        {
            // create a holder for the changing progress of the transfer
            GenericHolder<TransferProgress> progressHolder = new GenericHolder<TransferProgress>(null);

            // create the asynchronous result to return
            GenericAsyncResult<UploadFileResult> toReturn = new GenericAsyncResult<UploadFileResult>(
                asyncCallback,
                asyncCallbackUserState,
                progressHolder);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, Stream, FileChange, string, string, int, CancellationTokenSource> asyncParams =
                new Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, Stream, FileChange, string, string, int, CancellationTokenSource>(
                    toReturn,
                    asyncCallback,
                    uploadStream,
                    changeToUpload,
                    uid,
                    revision,
                    timeoutMilliseconds,
                    shutdownToken);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, StreamContext, FileChange, string, string, int, CancellationTokenSource> castState = 
                    state as Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, StreamContext, FileChange, string, string, int, CancellationTokenSource>;
                // if the try cast failed, then show a message box for this unrecoverable error
                if (castState == null)
                {
                    MessageEvents.FireNewEventMessage(
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
                            castState.Item6,
                            castState.Item7,
                            out message,
                            out hashMismatchFound,
                            castState.Rest,   // the 8th item.  We can't support any more with this architecture
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
        /// <param name="asyncResult">Asynchronous result originally returned by BeginUploadFile</param>
        /// <param name="progress">(output) Latest progress from a file upload, may be null if the upload file hasn't started</param>
        /// <returns>Returns any error that occurred in retrieving the latest progress, if any</returns>
        internal CLError GetProgressUploadFile(IAsyncResult asyncResult, out TransferProgress progress)
        {
            // try/catch to retrieve the latest progress, on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type of file uploads
                GenericAsyncResult<UploadFileResult> castAResult = asyncResult as GenericAsyncResult<UploadFileResult>;

                // if try casting the asynchronous result failed, throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
                }

                // try to cast the asynchronous result internal state as the holder for the progress
                GenericHolder<TransferProgress> iState = castAResult.InternalState as GenericHolder<TransferProgress>;

                // if trying to cast the internal state as the holder for progress failed, then throw an error (non-descriptive since it's our error)
                if (iState == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLHttpRestInternalPRogressRetreivalFailure2);
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
        /// <param name="asyncResult">The asynchronous result provided upon starting the file upload</param>
        /// <param name="result">(output) The result from the file upload</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        internal CLError EndUploadFile(IAsyncResult asyncResult, out UploadFileResult result)
        {
            // declare the specific type of asynchronous result for file uploads
            GenericAsyncResult<UploadFileResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for file uploads and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for file uploads
                castAResult = asyncResult as GenericAsyncResult<UploadFileResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
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
        /// <param name="streamContext">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, and MD5 hash to be set</param>
        /// <param name="uid">The server UID of the file being uploaded.</param>
        /// <param name="revision">The revision of the file being uploaded.</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="message">(output) upload response message</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UploadFile(
            StreamContext streamContext,
            FileChange changeToUpload,
            string uid,
            string revision,
            int timeoutMilliseconds,
            out string message,
            out bool hashMismatchFound,
            CancellationTokenSource shutdownToken = null)
        {
            return UploadFile(
                streamContext,
                changeToUpload,
                uid,
                revision,
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
            string uid,
            string revision,
            int timeoutMilliseconds,
            out string message,
            out bool hashMismatchFound,
            CancellationTokenSource shutdownToken,
            FileTransferStatusUpdateDelegate statusUpdate,
            object statusUpdateUserState)
        {
            return UploadFile(
                streamContext,
                changeToUpload,
                uid,
                revision,
                timeoutMilliseconds,
                out message,
                out hashMismatchFound,
                shutdownToken,
                null,
                null,
                null,
                statusUpdate,
                statusUpdateUserState);
        }

        // private helper for UploadFile which takes additional parameters we don't wish to expose; does the actual processing
        private CLError UploadFile(StreamContext streamContext,
            FileChange changeToUpload,
            string uid,
            string revision,
            int timeoutMilliseconds,
            out string message,
            out bool hashMismatchFound,
            CancellationTokenSource shutdownToken,
            AsyncCallback asyncCallback,
            IAsyncResult asyncResult,
            GenericHolder<TransferProgress> progress,
            FileTransferStatusUpdateDelegate statusUpdate,
            object statusUpdateUserState)
        {
            message = Helpers.DefaultForType<string>();

            // try/catch to process the file upload, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private upload class upon Helpers.ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathUpload + // path to upload
                    Helpers.QueryStringBuilder(new[] // add SyncboxId and DeviceId for file upload
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncboxId, _syncbox.SyncboxId.ToString()),
                        // query string parameter for the device id, needs to be escaped since it's client-defined
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_syncbox.CopiedSettings.DeviceId)),
                        // query string parameter for the UID
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataFileDownloadServerUid, uid),
                        // query string parameter for the revision
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataFileRevision, revision),
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
                        asyncCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                        asyncResult, // asynchronous result to pass when firing the asynchronous callback
                        progress, // holder for progress data which can be queried by user if called via async wrapper
                        statusUpdate, // callback to user to notify when a CLSyncEngine status has changed
                        statusUpdateUserState), // userstate to pass to the statusUpdate callback
                    Helpers.HttpStatusesOkCreatedNotModified, // use the hashset for ok/created/not modified as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
                    requestNewCredentialsInfo,   // pass the optional parameters to support temporary token reallocation.
                    false);

                hashMismatchFound = false;
            }
            catch (Exception ex)
            {
                hashMismatchFound = (ex is HashMismatchException);

                return ex;
            }
            return null;
        }
        #endregion

        #region GetAllPending
        /// <summary>
        /// Asynchronously starts querying for all pending files
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAllPending(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetAllPendingResult> toReturn = new GenericAsyncResult<GetAllPendingResult>(
                asyncCallback,
                asyncCallbackUserState);

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
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
        /// <param name="asyncResult">The asynchronous result provided upon starting the pending query</param>
        /// <param name="result">(output) The result from the pending query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAllPending(IAsyncResult asyncResult, out GetAllPendingResult result)
        {
            // declare the specific type of asynchronous result for pending query
            GenericAsyncResult<GetAllPendingResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for pending query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for pending query
                castAResult = asyncResult as GenericAsyncResult<GetAllPendingResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
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
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }

                // build the location of the pending retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPending + // get pending
                    Helpers.QueryStringBuilder(new[] // grab parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the id of the device, escaped as needed for the URI
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_syncbox.CopiedSettings.DeviceId)),
                        
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
                response = Helpers.ProcessHttp<JsonContracts.PendingResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to get pending
                    Helpers.requestMethod.get, // get pending is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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

        #region PostFileChange (partially commented out since methods have not been updated for CLFileItem and usage with a possibly null or empty syncbox path, currently used in the previous manner by sync engine, see comment inside)
        // !!
        // Needs to be updated before uncommenting! Other on-demand calls have been updated to use CLFileItem and path usages are now difference since the user may not put in a full path for the syncbox root
        // Also, sync engine is currently using the pieces left uncommented internally so if changed to allow public access make sure not to break sync engine usage
        // !!

        ///// <summary>
        ///// Asynchronously starts posting a single FileChange to the server
        ///// </summary>
        ///// <param name="asyncCallback">Callback method to fire when operation completes</param>
        ///// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        ///// <param name="toCommunicate">Single FileChange to send</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginPostFileChange(AsyncCallback asyncCallback,
        //    object asyncCallbackUserState,
        //    FileChange toCommunicate,
        //    int timeoutMilliseconds,
        //    string serverUid,
        //    string revision)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<FileChangeResult> toReturn = new GenericAsyncResult<FileChangeResult>(
        //        asyncCallback,
        //        asyncCallbackUserState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string> asyncParams =
        //        new Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string>(
        //            toReturn,
        //            toCommunicate,
        //            timeoutMilliseconds,
        //            serverUid,
        //            revision);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string> castState = state as Tuple<GenericAsyncResult<FileChangeResult>, FileChange, int, string, string>;
        //        // if the try cast failed, then show a message box for this unrecoverable error
        //        if (castState == null)
        //        {
        //            MessageEvents.FireNewEventMessage(
        //                Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
        //                EventMessageLevel.Important,
        //                new HaltAllOfCloudSDKErrorInfo());
        //        }
        //        // else if the try cast did not fail, then start processing with the input parameters
        //        else
        //        {
        //            // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
        //            try
        //            {
        //                // declare the specific type of result for this operation
        //                JsonContracts.FileChangeResponse response;
        //                // run the download of the file with the passed parameters, storing any error that occurs
        //                CLError processError = PostFileChange(
        //                    castState.Item2,
        //                    castState.Item3,
        //                    out response,
        //                    castState.Item4,
        //                    castState.Item5);

        //                // if there was an asynchronous result in the parameters, then complete it with a new result object
        //                if (castState.Item1 != null)
        //                {
        //                    castState.Item1.Complete(
        //                        new FileChangeResult(
        //                            processError, // any error that may have occurred during processing
        //                            response), // the specific type of result for this operation
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

        ///// <summary>
        ///// Finishes posting a FileChange if it has not already finished via its asynchronous result and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="asyncResult">The asynchronous result provided upon starting the FileChange post</param>
        ///// <param name="result">(output) The result from the FileChange post</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //internal CLError EndPostFileChange(IAsyncResult asyncResult, out FileChangeResult result)
        //{
        //    // declare the specific type of asynchronous result for FileChange post
        //    GenericAsyncResult<FileChangeResult> castAResult;

        //    // try/catch to try casting the asynchronous result as the type for FileChange post and pull the result (possibly incomplete), on catch default the output and return the error
        //    try
        //    {
        //        // try cast the asynchronous result as the type for FileChange post
        //        castAResult = asyncResult as GenericAsyncResult<FileChangeResult>;

        //        // if trying to cast the asynchronous result failed, then throw an error
        //        if (castAResult == null)
        //        {
        //            throw new NullReferenceException(Resources.CLAsyncResultInternalTypeMismatch);
        //        }

        //        // pull the result for output (may not yet be complete)
        //        result = castAResult.Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        result = Helpers.DefaultForType<FileChangeResult>();
        //        return ex;
        //    }

        //    // try/catch to finish the asynchronous operation if necessary, re-pull the result for output, and rethrow any exception which may have occurred; on catch, return the error
        //    try
        //    {
        //        // This method assumes that only 1 thread calls EndInvoke 
        //        // for this object
        //        if (!castAResult.IsCompleted)
        //        {
        //            // If the operation isn't done, wait for it
        //            castAResult.AsyncWaitHandle.WaitOne();
        //            castAResult.AsyncWaitHandle.Close();
        //        }

        //        // re-pull the result for output in case it was not completed when it was pulled before
        //        result = castAResult.Result;

        //        // Operation is done: if an exception occurred, return it
        //        if (castAResult.Exception != null)
        //        {
        //            return castAResult.Exception;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex;
        //    }
        //    return null;
        //}

        /// <summary>
        /// Posts a single FileChange to the server to update the sync box in the syncbox.
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
                    throw new CLArgumentNullException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestCommunicateCannotBeNull);
                }
                if (toCommunicate.Direction == SyncDirection.From)
                {
                    throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestToCommunicateDirectionisNotToServer);
                }
                if (toCommunicate.Metadata == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestToCommunicateMetedataCannotBeNull);
                }
                if (toCommunicate.Type == FileChangeType.Modified
                    && toCommunicate.Metadata.HashableProperties.IsFolder)
                {
                    throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestToCommunicateCannotBeFolderandModified);
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestNewPathCannotBeNull);
                        }

                        // if change is a folder, set path and create request content for folder creation
                        if (toCommunicate.Metadata.HashableProperties.IsFolder)
                        {
                            serverMethodPath = CLDefinitions.MethodPathOneOffFolderCreate;

                            requestContent = new JsonContracts.FolderAddRequest()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _syncbox.CopiedSettings.DeviceId,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true) + ((char)0x2F /* '/' */),
                                SyncboxId = _syncbox.SyncboxId,
                                Name = (string.IsNullOrEmpty(toCommunicate.Metadata.ParentFolderServerUid) ? null : toCommunicate.NewPath.Name),
                                ParentUid = (string.IsNullOrEmpty(toCommunicate.Metadata.ParentFolderServerUid) ? null : toCommunicate.Metadata.ParentFolderServerUid)
                            };
                        }
                        // else if change is a file, set path and create request content for file creation
                        else
                        {
                            string addHashString = toCommunicate.GetMD5LowercaseString();

                            // check additional parameters for file creation

                            if (string.IsNullOrEmpty(addHashString))
                            {
                                throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestMD5LowerCaseStringSet);
                            }
                            if (toCommunicate.Metadata.HashableProperties.Size == null)
                            {
                                throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestMetadataHashablePropertiesSizeCannotBeNull);
                            }

                            serverMethodPath = CLDefinitions.MethodPathOneOffFileCreate;

                            requestContent = new JsonContracts.FileAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _syncbox.CopiedSettings.DeviceId,
                                Hash = addHashString,
                                MimeType = toCommunicate.Metadata.MimeType,
                                ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true),
                                Size = toCommunicate.Metadata.HashableProperties.Size,
                                SyncboxId = _syncbox.SyncboxId,
                                Name = (string.IsNullOrEmpty(toCommunicate.Metadata.ParentFolderServerUid) ? null : toCommunicate.NewPath.Name),
                                ParentUid = (string.IsNullOrEmpty(toCommunicate.Metadata.ParentFolderServerUid) ? null : toCommunicate.Metadata.ParentFolderServerUid)
                            };
                        }
                        break;

                    case FileChangeType.Deleted:

                        // check additional parameters for file or folder deletion

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(serverUid))
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestXORNewPathServerUidCannotBeNull);
                        }

                        // file deletion and folder deletion share a json contract object for deletion
                        requestContent = new JsonContracts.FileOrFolderDeleteRequest()
                        {
                            //DeviceId = _syncbox.CopiedSettings.DeviceId,
                            ServerUid = (serverUid == string.Empty ? null : serverUid),
                            SyncboxId = _syncbox.SyncboxId,
                            RelativePath = (toCommunicate.NewPath == null ? null : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true))
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
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestMD5LowerCaseStringSet);
                        }
                        if (toCommunicate.Metadata.HashableProperties.Size == null)
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestMetadataHashablePropertiesSizeCannotBeNull);
                        }
                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(serverUid))
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestXORNewPathServerUidCannotBeNull);
                        }
                        if (string.IsNullOrEmpty(revision))
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestMetaDataRevisionCannotBeNull);
                        }

                        // there is no folder modify, so json contract object and server method path for modify are only for files

                        requestContent = new JsonContracts.FileModify()
                        {
                            CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                            DeviceId = _syncbox.CopiedSettings.DeviceId,
                            Hash = modifyHashString,
                            MimeType = toCommunicate.Metadata.MimeType,
                            ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true)),
                            Revision = revision,
                            ServerUid = (serverUid == string.Empty ? null : serverUid),
                            Size = toCommunicate.Metadata.HashableProperties.Size,
                            SyncboxId = _syncbox.SyncboxId
                        };

                        serverMethodPath = CLDefinitions.MethodPathOneOffFileModify;
                        break;

                    case FileChangeType.Renamed:
                        // check additional parameters for file or folder move (rename)

                        #region checks for old path

                        if (toCommunicate.OldPath == null
                            && string.IsNullOrEmpty(serverUid))
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestXOROldPathServerUidCannotBeNull);
                        }

                        #endregion

                        #region checks for new path

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ParentFolderServerUid))
                        {
                            throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestXORNewPathParentFolderServerUidCannotBeNull);
                        }

                        #endregion

                        // file move (rename) and folder move (rename) share a json contract object for move (rename)
                        requestContent = new JsonContracts.FileOrFolderMove()
                        {
                            RelativeFromPath = (toCommunicate.OldPath == null ? null : toCommunicate.OldPath.GetRelativePath(_syncbox.Path, true)),
                            RelativeToPath = (toCommunicate.NewPath == null ? null : toCommunicate.NewPath.GetRelativePath(_syncbox.Path, true)),
                            ServerUid = (serverUid == string.Empty ? null : serverUid),

                            // check on ParentFolderServerUid is intended and correct (server checks "to_name" instead of "to_path" if set, thus possibly losing a move to a new folder if you set "to_name" without the proper "to_parent_uid")
                            ToName = ((string.IsNullOrEmpty(toCommunicate.Metadata.ParentFolderServerUid) || toCommunicate.NewPath == null) ? null : toCommunicate.NewPath.Name),
                            ToParentUid = (toCommunicate.Metadata.ParentFolderServerUid == string.Empty ? null : toCommunicate.Metadata.ParentFolderServerUid)
                        };

                        // server method path switched on whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderMove
                            : CLDefinitions.MethodPathOneOffFileMove);
                        break;

                    default:
                        throw new CLArgumentException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestToCommunicateTypeIsUnknownFileChangeType + toCommunicate.Type.ToString());
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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string fileServerId,
            int timeoutMilliseconds,
            bool includeDeletedVersions = false)
        {
            return BeginGetFileVersions(asyncCallback,
                asyncCallbackUserState,
                fileServerId,
                timeoutMilliseconds,
                null,
                includeDeletedVersions);
        }

        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions = false)
        {
            return BeginGetFileVersions(asyncCallback,
                asyncCallbackUserState,
                null,
                timeoutMilliseconds,
                pathToFile,
                includeDeletedVersions);
        }

        /// <summary>
        /// Asynchronously starts querying the server for all versions of a given file
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFileVersions(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            string fileServerId,
            int timeoutMilliseconds,
            FilePath pathToFile,
            bool includeDeletedVersions = false)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetFileVersionsResult> toReturn = new GenericAsyncResult<GetFileVersionsResult>(
                asyncCallback,
                asyncCallbackUserState);

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
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
        /// <param name="asyncResult">The asynchronous result provided upon starting undoing the deletion</param>
        /// <param name="result">(output) The result from undoing the deletion</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFileVersions(IAsyncResult asyncResult, out GetFileVersionsResult result)
        {
            // declare the specific type of asynchronous result for querying file versions
            GenericAsyncResult<GetFileVersionsResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for querying file versions and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for querying file versions
                castAResult = asyncResult as GenericAsyncResult<GetFileVersionsResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
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
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new CLNullReferenceException(CLExceptionCode.Syncing_LiveSyncEngine, Resources.CLHttpRestXORPathtoFileFileServerUidMustNotBeNull);
                }

                // build the location of the file versions retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathFileGetVersions + // get file versions
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the device id
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_syncbox.CopiedSettings.DeviceId)),

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
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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
        /// <param name="asyncCallback">Callback method to fire when operation completes</param>
        /// <param name="asyncCallbackUserState">User state to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginPurgePending(AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<PurgePendingResult> toReturn = new GenericAsyncResult<PurgePendingResult>(
                asyncCallback,
                asyncCallbackUserState);

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
                        Resources.CLCannotCastStateAs + Helpers.GetTypeNameEvenForNulls(castState),
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
        /// <param name="asyncResult">The asynchronous result provided upon starting purging pending</param>
        /// <param name="result">(output) The result from purging pending</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndPurgePending(IAsyncResult asyncResult, out PurgePendingResult result)
        {
            // declare the specific type of asynchronous result for purging pending
            GenericAsyncResult<PurgePendingResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for purging pending and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for purging pending
                castAResult = asyncResult as GenericAsyncResult<PurgePendingResult>;

                // if trying to cast the asynchronous result failed, then throw an error
                if (castAResult == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.General_Miscellaneous, Resources.CLAsyncResultInternalTypeMismatch);
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
                    throw new CLArgumentException(CLExceptionCode.OnDemand_TimeoutMilliseconds, Resources.CLMSTimeoutMustBeGreaterThanZero);
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
                    DeviceId = _syncbox.CopiedSettings.DeviceId,
                    SyncboxId = _syncbox.SyncboxId
                },
                    CLDefinitions.CLMetaDataServerURL,      // MDS server URL
                    CLDefinitions.MethodPathPurgePending, // purge pending address
                    Helpers.requestMethod.post, // purge pending is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // purge pending should give OK or Accepted
                    _syncbox.CopiedSettings, // pass the copied settings
                    _syncbox, // pass the syncbox
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
