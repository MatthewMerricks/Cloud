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
using CloudApiPublic.Model.EventMessages.ErrorInfo;

namespace Cloud.REST
{
    // CLCredential class has additional HTTP calls which do not require a SyncBox id
    /// <summary>
    /// Client for manual HTTP communication calls to the Cloud
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class CLHttpRest
    {
        public bool IsModifyingSyncBoxViaPublicAPICalls
        {
            get
            {
                lock (_isModifyingSyncBoxViaPublicAPICalls)
                {
                    return _isModifyingSyncBoxViaPublicAPICalls.Value > 0;
                }
            }
        }
        private void IncrementModifyingSyncBoxViaPublicAPICalls()
        {
            lock (_isModifyingSyncBoxViaPublicAPICalls)
            {
                _isModifyingSyncBoxViaPublicAPICalls.Value = _isModifyingSyncBoxViaPublicAPICalls.Value + 1;
            }
        }
        private void DecrementModifyingSyncBoxViaPublicAPICalls()
        {
            lock (_isModifyingSyncBoxViaPublicAPICalls)
            {
                _isModifyingSyncBoxViaPublicAPICalls.Value = _isModifyingSyncBoxViaPublicAPICalls.Value - 1;
            }
        }
        private readonly GenericHolder<int> _isModifyingSyncBoxViaPublicAPICalls = new GenericHolder<int>(0);

        #region construct with settings so they do not always need to be passed in
        /// <summary>
        /// Settings copied upon creation of this REST client
        /// </summary>
        public ICLSyncSettingsAdvanced CopiedSettings
        {
            get
            {
                return _copiedSettings;
            }
        }
        // storage of settings, which should be a copy of settings passed in on construction so they do not change throughout communication
        private readonly ICLSyncSettingsAdvanced _copiedSettings;

        /// <summary>
        /// Contains authentication information required for all communication and services
        /// </summary>
        public CLCredential Credential
        {
            get
            {
                return _credential;
            }
        }
        private readonly CLCredential _credential;

        /// <summary>
        /// The unique ID of this SyncBox assigned by Cloud
        /// </summary>
        public long SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private readonly long _syncBoxId;

        // private constructor requiring settings to copy and store for the life of this http client
        private CLHttpRest(CLCredential credential, long syncBoxId, ICLSyncSettings settings)
        {
            if (credential == null)
            {
                throw new NullReferenceException("credential cannot be null");
            }

            this._credential = credential;
            this._syncBoxId = syncBoxId;
            if (settings == null)
            {
                this._copiedSettings = AdvancedSyncSettings.CreateDefaultSettings();
            }
            else
            {
                this._copiedSettings = settings.CopySettings();
            }

            if (!string.IsNullOrEmpty(this._copiedSettings.SyncRoot))
            {
                CLError syncRootError = Helpers.CheckForBadPath(this._copiedSettings.SyncRoot);
                if (syncRootError != null)
                {
                    throw new AggregateException("settings SyncRoot represents a bad path", syncRootError.GrabExceptions());
                }


            }
        }

        /// <summary>
        /// Creates a CLHttpRest client object for HTTP REST calls to the server
        /// </summary>
        /// <param name="credential">Contains authentication information required for communication</param>
        /// <param name="syncBoxId">ID of sync box which can be manually synced</param>
        /// <param name="client">(output) Created CLHttpRest client</param>
        /// <param name="settings">(optional) Additional settings to override some defaulted parameters</param>
        /// <returns>Returns any error creating the CLHttpRest client, if any</returns>
        internal static CLError CreateAndInitialize(CLCredential credential, long syncBoxId, out CLHttpRest client, ICLSyncSettings settings = null)
        {
            try
            {
                client = new CLHttpRest(credential, syncBoxId, settings);
            }
            catch (Exception ex)
            {
                client = Helpers.DefaultForType<CLHttpRest>();
                return ex;
            }
            return null;
        }
        #endregion

        #region public API calls
        #region DownloadFile
        /// <summary>
        /// Asynchronously starts downloading a file from a provided file download change
        /// </summary>
        /// <param name="aCallback">Callback method to fire upon progress changes in download, make sure it processes quickly if the IAsyncResult IsCompleted is false</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="changeToDownload">File download change, requires Metadata.</param>
        /// <param name="moveFileUponCompletion">¡¡ Action required: move the completed download file from the temp directory to the final destination !! Callback fired when download completes</param>
        /// <param name="moveFileUponCompletionState">Userstate passed upon firing completed download callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="beforeDownload">(optional) Callback fired before a download starts</param>
        /// <param name="beforeDownloadState">Userstate passed upon firing before download callback</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <param name="customDownloadFolderFullPath">(optional) Full path to a folder where temporary downloads will be stored to override default</param>
        /// <returns>Returns the asynchronous result which is used to retrieve progress and/or the result</returns>
        public IAsyncResult BeginDownloadFile(AsyncCallback aCallback,
            object aState,
            FileChange changeToDownload,
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
            Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, Helpers.AfterDownloadToTempFile, object, int, Helpers.BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>> asyncParams =
                new Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, Helpers.AfterDownloadToTempFile, object, int, Helpers.BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>>(
                    toReturn,
                    aCallback,
                    changeToDownload,
                    moveFileUponCompletion,
                    moveFileUponCompletionState,
                    timeoutMilliseconds,
                    beforeDownload,
                    new Tuple<object, CancellationTokenSource, string>(
                        beforeDownloadState,
                        shutdownToken,
                        customDownloadFolderFullPath));

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, Helpers.AfterDownloadToTempFile, object, int, Helpers.BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>> castState = state as Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, Helpers.AfterDownloadToTempFile, object, int, Helpers.BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>>;
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

                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = DownloadFile(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            castState.Item6,
                            out status,
                            castState.Item7,
                            castState.Rest.Item1,
                            castState.Rest.Item2,
                            castState.Rest.Item3,
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
                                    processError, // any error that may have occurred during processing
                                    status), // the output status of communication
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
                    throw new NullReferenceException("aResult does not match expected internal type");
                }

                // try to cast the asynchronous result internal state as the holder for the progress
                GenericHolder<TransferProgress> iState = castAResult.InternalState as GenericHolder<TransferProgress>;

                // if trying to cast the internal state as the holder for progress failed, then throw an error (non-descriptive since it's our error)
                if (iState == null)
                {
                    throw new Exception("There was an internal error attempting to retrieve the progress, Error 1");
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="beforeDownload">(optional) Callback fired before a download starts</param>
        /// <param name="beforeDownloadState">Userstate passed upon firing before download callback</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <param name="customDownloadFolderFullPath">(optional) Full path to a folder where temporary downloads will be stored to override default</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DownloadFile(FileChange changeToDownload,
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            Helpers.BeforeDownloadToTempFile beforeDownload = null,
            object beforeDownloadState = null,
            CancellationTokenSource shutdownToken = null,
            string customDownloadFolderFullPath = null)
        {
            // pass through input parameters to the private call (which takes additional parameters we don't wish to expose)
            return DownloadFile(changeToDownload,
                moveFileUponCompletion,
                moveFileUponCompletionState,
                timeoutMilliseconds,
                out status,
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
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            Helpers.BeforeDownloadToTempFile beforeDownload,
            object beforeDownloadState,
            CancellationTokenSource shutdownToken,
            string customDownloadFolderFullPath,
            FileTransferStatusUpdateDelegate statusUpdate,
            Guid statusUpdateId)
        {
            return DownloadFile(changeToDownload,
                moveFileUponCompletion,
                moveFileUponCompletionState,
                timeoutMilliseconds,
                out status,
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
            Helpers.AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
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
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file download, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private download class upon Helpers.ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
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
                    currentDownloadFolder = Helpers.GetTempFileDownloadPath(_copiedSettings, _syncBoxId);
                }

                // check if the folder for temp downloads represents a bad path
                CLError badTempFolderError = Helpers.CheckForBadPath(currentDownloadFolder);

                // if the temp download folder is a bad path rethrow the error
                if (badTempFolderError != null)
                {
                    throw new AggregateException("The customDownloadFolderFullPath is bad", badTempFolderError.GrabExceptions());
                }

                // if the folder path for downloads is too long, then throw an exception
                if (currentDownloadFolder.Length > 222) // 222 calculated by 259 max path length minus 1 character for a folder slash seperator plus 36 characters for (Guid).ToString("N")
                {
                    throw new ArgumentException("Folder path for temp download files is too long by " + (currentDownloadFolder.Length - 222).ToString());
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathDownload + // download method path
                    Helpers.QueryStringBuilder(new[] // add SyncBoxId for file download
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // prepare the downloadParams before the Helpers.ProcessHttp because it does additional parameter checks first
                Helpers.downloadParams currentDownload = new Helpers.downloadParams( // this is a special communication method and requires passing download parameters
                    moveFileUponCompletion, // callback which should move the file to final location
                    moveFileUponCompletionState, // userstate for the move file callback
                    customDownloadFolderFullPath ?? // first try to use a provided custom folder full path
                        Helpers.GetTempFileDownloadPath(_copiedSettings, _syncBoxId), // if custom path not provided, null-coallesce to default
                    Helpers.HandleUploadDownloadStatus, // private event handler to relay status change events
                    changeToDownload, // the FileChange describing the download
                    shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                    _copiedSettings.SyncRoot, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    aCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                    aResult, // asynchronous result to pass when firing the asynchronous callback
                    progress, // holder for progress data which can be queried by user if called via async wrapper
                    statusUpdate, // callback to user to notify when a CLSyncEngine status has changed
                    statusUpdateId, // userstate to pass to the statusUpdate callback
                    beforeDownload, // optional callback fired before download starts
                    beforeDownloadState); // userstate passed when firing download start callback

                // run the actual communication
                Helpers.ProcessHttp(
                    new Download() // JSON contract to serialize
                    {
                        StorageKey = changeToDownload.Metadata.StorageKey // storage key parameter
                    },
                    CLDefinitions.CLUploadDownloadServerURL, // server for download
                    serverMethodPath, // dynamic method path to incorporate query string parameters
                    Helpers.requestMethod.post, // download is a post
                    timeoutMilliseconds, // time before communication timeout (does not restrict time
                    currentDownload, // download-specific parameters holder constructed directly above
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
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
        public IAsyncResult BeginUploadFile(AsyncCallback aCallback,
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
                Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, Stream, FileChange, int, CancellationTokenSource> castState = state as Tuple<GenericAsyncResult<UploadFileResult>, AsyncCallback, Stream, FileChange, int, CancellationTokenSource>;
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

                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the output message for upload
                        string message;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UploadFile(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            out status,
                            out message,
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
                                status,
                                message),
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
        public CLError GetProgressUploadFile(IAsyncResult aResult, out TransferProgress progress)
        {
            // try/catch to retrieve the latest progress, on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type of file uploads
                GenericAsyncResult<UploadFileResult> castAResult = aResult as GenericAsyncResult<UploadFileResult>;

                // if try casting the asynchronous result failed, throw an error
                if (castAResult == null)
                {
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        public CLError EndUploadFile(IAsyncResult aResult, out UploadFileResult result)
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="message">(output) upload response message</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UploadFile(Stream uploadStream,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out string message,
            CancellationTokenSource shutdownToken = null)
        {
            return UploadFile(
                uploadStream,
                changeToUpload,
                timeoutMilliseconds,
                out status,
                out message,
                shutdownToken,
                null,
                null,
                null,
                null,
                null);
        }

        // internal version with added action for status update
        internal CLError UploadFile(Stream uploadStream,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out string message,
            CancellationTokenSource shutdownToken,
            FileTransferStatusUpdateDelegate statusUpdate,
            Guid statusUpdateId)
        {
            return UploadFile(
                uploadStream,
                changeToUpload,
                timeoutMilliseconds,
                out status,
                out message,
                shutdownToken,
                null,
                null,
                null,
                statusUpdate,
                statusUpdateId);
        }

        // private helper for UploadFile which takes additional parameters we don't wish to expose; does the actual processing
        private CLError UploadFile(Stream uploadStream,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out string message,
            CancellationTokenSource shutdownToken,
            AsyncCallback aCallback,
            IAsyncResult aResult,
            GenericHolder<TransferProgress> progress,
            FileTransferStatusUpdateDelegate statusUpdate,
            Nullable<Guid> statusUpdateId)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file upload, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private upload class upon Helpers.ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathUpload + // path to upload
                    Helpers.QueryStringBuilder(new[] // add SyncBoxId and DeviceId for file upload
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString()),

                        (string.IsNullOrEmpty(_copiedSettings.DeviceId)
                            ? new KeyValuePair<string, string>()
                            :
                                // query string parameter for the device id, needs to be escaped since it's client-defined
                                new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)))
                    });

                // run the HTTP communication
                message = Helpers.ProcessHttp<string>(null, // the stream inside the upload parameter object is the request content, so no JSON contract object
                    CLDefinitions.CLUploadDownloadServerURL,  // Server URL
                    serverMethodPath, // dynamic upload path to add device id
                    Helpers.requestMethod.put, // upload is a put
                    timeoutMilliseconds, // time before communication timeout (does not restrict time for the actual file upload)
                    new Cloud.Static.Helpers.uploadParams( // this is a special communication method and requires passing upload parameters
                        uploadStream, // stream for file to upload
                        Helpers.HandleUploadDownloadStatus, // private event handler to relay status change events
                        changeToUpload, // the FileChange describing the upload
                        shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                        _copiedSettings.SyncRoot, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                        aCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                        aResult, // asynchronous result to pass when firing the asynchronous callback
                        progress, // holder for progress data which can be queried by user if called via async wrapper
                        statusUpdate, // callback to user to notify when a CLSyncEngine status has changed
                        statusUpdateId), // userstate to pass to the statusUpdate callback
                    Helpers.HttpStatusesOkCreatedNotModifiedNoContent, // use the hashset for ok/created/not modified as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                message = Helpers.DefaultForType<string>();
                return ex;
            }
            return null;
        }
        #endregion

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
        public IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
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
        public IAsyncResult BeginGetMetadata(AsyncCallback aCallback,
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
                        JsonContracts.Metadata result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetMetadata(
                            castState.Item2,
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetMetadataResult(
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
        /// Finishes a metadata query if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the metadata query</param>
        /// <param name="result">(output) The result from the metadata query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetMetadata(IAsyncResult aResult, out GetMetadataResult result)
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetMetadata(bool isFolder, string serverId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            return GetMetadata(/*fullPath*/ null, serverId, isFolder, timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Private helper to combine two overloaded public versions: Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetMetadata(FilePath fullPath, bool isFolder, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            return GetMetadata(fullPath, /*serverId*/ null, isFolder, timeoutMilliseconds, out status, out response);
        }

        /// <summary>
        /// Private helper to combine two overloaded public versions: Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="serverId">Unique id of the item on the server</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        private CLError GetMetadata(FilePath fullPath, string serverId, bool isFolder, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (fullPath == null
                    && string.IsNullOrEmpty(serverId))
                {
                    throw new NullReferenceException("Both fullPath and serverId cannot be null, at least one is required");
                }
                if (fullPath != null)
                {
                    CLError pathError = Helpers.CheckForBadPath(fullPath);
                    if (pathError != null)
                    {
                        throw new AggregateException("fullPath is not in the proper format", pathError.GrabExceptions());
                    }

                    if (string.IsNullOrEmpty(_copiedSettings.SyncRoot))
                    {
                        throw new NullReferenceException("settings SyncRoot cannot be null");
                    }

                    if (!fullPath.Contains(_copiedSettings.SyncRoot))
                    {
                        throw new ArgumentException("fullPath does not contain settings SyncRoot");
                    }
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
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
                                new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(fullPath.GetRelativePath((_copiedSettings.SyncRoot ?? string.Empty), true) + (isFolder ? "/" : string.Empty)))

                            : // query string parameter for the unique id to the file or folder on the server, escaped since it is a server opaque field of undefined format
                                new KeyValuePair<string, string>(CLDefinitions.CLMetadataServerId, Uri.EscapeDataString(serverId))),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Metadata>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    Helpers.requestMethod.get, // query metadata is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Metadata>();
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
                        JsonContracts.PendingResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetAllPending(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetAllPendingResult(
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAllPending(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PendingResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the pending query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }

                // build the location of the pending retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPending + // get pending
                    Helpers.QueryStringBuilder(new[] // grab parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the id of the device, escaped as needed for the URI
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)),
                        
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.PendingResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to get pending
                    Helpers.requestMethod.get, // get pending is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
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
        public IAsyncResult BeginPostFileChange(AsyncCallback aCallback,
            object aState,
            FileChange toCommunicate,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<PostFileChangeResult> toReturn = new GenericAsyncResult<PostFileChangeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<PostFileChangeResult>, FileChange, int> asyncParams =
                new Tuple<GenericAsyncResult<PostFileChangeResult>, FileChange, int>(
                    toReturn,
                    toCommunicate,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<PostFileChangeResult>, FileChange, int> castState = state as Tuple<GenericAsyncResult<PostFileChangeResult>, FileChange, int>;
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
                        JsonContracts.Event result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = PostFileChange(
                            castState.Item2,
                            castState.Item3,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new PostFileChangeResult(
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
        /// Finishes posting a FileChange if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the FileChange post</param>
        /// <param name="result">(output) The result from the FileChange post</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndPostFileChange(IAsyncResult aResult, out PostFileChangeResult result)
        {
            // declare the specific type of asynchronous result for FileChange post
            GenericAsyncResult<PostFileChangeResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for FileChange post and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for FileChange post
                castAResult = aResult as GenericAsyncResult<PostFileChangeResult>;

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
                result = Helpers.DefaultForType<PostFileChangeResult>();
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError PostFileChange(FileChange toCommunicate, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file change post, on catch return the error
            try
            {
                // check input parameters

                if (toCommunicate == null)
                {
                    throw new NullReferenceException("toCommunicate cannot be null");
                }
                if (toCommunicate.Direction == SyncDirection.From)
                {
                    throw new ArgumentException("toCommunicate Direction is not To the server");
                }
                if (toCommunicate.Metadata == null)
                {
                    throw new NullReferenceException("toCommunicate Metadata cannot be null");
                }
                if (toCommunicate.Type == FileChangeType.Modified
                    && toCommunicate.Metadata.HashableProperties.IsFolder)
                {
                    throw new ArgumentException("toCommunicate cannot be both a folder and of type Modified");
                }
                if (_copiedSettings.DeviceId == null)
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (_copiedSettings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
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
                            throw new NullReferenceException("toCommunicate NewPath cannot be null");
                        }

                        // if change is a folder, set path and create request content for folder creation
                        if (toCommunicate.Metadata.HashableProperties.IsFolder)
                        {
                            serverMethodPath = CLDefinitions.MethodPathOneOffFolderCreate;

                            requestContent = new JsonContracts.FolderAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _copiedSettings.DeviceId,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_copiedSettings.SyncRoot, true) + "/",
                                SyncBoxId = _syncBoxId
                            };
                        }
                        // else if change is a file, set path and create request content for file creation
                        else
                        {
                            string addHashString;
                            CLError addHashStringError = toCommunicate.GetMD5LowercaseString(out addHashString);
                            if (addHashStringError != null)
                            {
                                throw new AggregateException("Error retrieving toCommunicate MD5 lowercase string", addHashStringError.GrabExceptions());
                            }

                            // check additional parameters for file creation

                            if (string.IsNullOrEmpty(addHashString))
                            {
                                throw new NullReferenceException("MD5 lowercase string retrieved from toCommunicate cannot be null, set via toCommunicate.SetMD5");
                            }
                            if (toCommunicate.Metadata.HashableProperties.Size == null)
                            {
                                throw new NullReferenceException("toCommunicate Metadata HashableProperties Size cannot be null");
                            }

                            serverMethodPath = CLDefinitions.MethodPathOneOffFileCreate;

                            requestContent = new JsonContracts.FileAdd()
                            {
                                CreatedDate = toCommunicate.Metadata.HashableProperties.CreationTime,
                                DeviceId = _copiedSettings.DeviceId,
                                Hash = addHashString,
                                MimeType = toCommunicate.Metadata.MimeType,
                                ModifiedDate = toCommunicate.Metadata.HashableProperties.LastTime,
                                RelativePath = toCommunicate.NewPath.GetRelativePath(_copiedSettings.SyncRoot, true),
                                Size = toCommunicate.Metadata.HashableProperties.Size,
                                SyncBoxId = _syncBoxId
                            };
                        }
                        break;

                    case FileChangeType.Deleted:

                        // check additional parameters for file or folder deletion

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerId))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }

                        // file deletion and folder deletion share a json contract object for deletion
                        requestContent = new JsonContracts.FileOrFolderDelete()
                        {
                            DeviceId = _copiedSettings.DeviceId,
                            RelativePath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_copiedSettings.SyncRoot, true) +
                                    (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerId = toCommunicate.Metadata.ServerId,
                            SyncBoxId = _syncBoxId
                        };

                        // server method path switched from whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderDelete
                            : CLDefinitions.MethodPathOneOffFileDelete);
                        break;

                    case FileChangeType.Modified:

                        // grab MD5 hash string and rethrow any error that occurs

                        string modifyHashString;
                        CLError modifyHashStringError = toCommunicate.GetMD5LowercaseString(out modifyHashString);
                        if (modifyHashStringError != null)
                        {
                            throw new AggregateException("Error retrieving toCommunicate MD5 lowercase string", modifyHashStringError.GrabExceptions());
                        }

                        // check additional parameters for file modification

                        if (string.IsNullOrEmpty(modifyHashString))
                        {
                            throw new NullReferenceException("MD5 lowercase string retrieved from toCommunicate cannot be null, set via toCommunicate.SetMD5");
                        }
                        if (toCommunicate.Metadata.HashableProperties.Size == null)
                        {
                            throw new NullReferenceException("toCommunicate Metadata HashableProperties Size cannot be null");
                        }
                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerId))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }
                        if (string.IsNullOrEmpty(toCommunicate.Metadata.Revision))
                        {
                            throw new NullReferenceException("toCommunicate Metadata Revision cannot be null");
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
                                : toCommunicate.NewPath.GetRelativePath(_copiedSettings.SyncRoot, true)),
                            Revision = toCommunicate.Metadata.Revision,
                            ServerId = toCommunicate.Metadata.ServerId,
                            Size = toCommunicate.Metadata.HashableProperties.Size,
                            SyncBoxId = _syncBoxId
                        };

                        serverMethodPath = CLDefinitions.MethodPathOneOffFileModify;
                        break;

                    case FileChangeType.Renamed:

                        // check additional parameters for file or folder move (rename)

                        if (toCommunicate.NewPath == null
                            && string.IsNullOrEmpty(toCommunicate.Metadata.ServerId))
                        {
                            throw new NullReferenceException("Either toCommunicate NewPath must not be null or toCommunicate Metadata ServerId must not be null or both must not be null");
                        }
                        if (toCommunicate.OldPath == null)
                        {
                            throw new NullReferenceException("toCommunicate OldPath cannot be null");
                        }

                        // file move (rename) and folder move (rename) share a json contract object for move (rename)
                        requestContent = new JsonContracts.FileOrFolderMove()
                        {
                            DeviceId = _copiedSettings.DeviceId,
                            RelativeFromPath = toCommunicate.OldPath.GetRelativePath(_copiedSettings.SyncRoot, true) +
                                (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty),
                            RelativeToPath = (toCommunicate.NewPath == null
                                ? null
                                : toCommunicate.NewPath.GetRelativePath(_copiedSettings.SyncRoot, true)
                                    + (toCommunicate.Metadata.HashableProperties.IsFolder ? "/" : string.Empty)),
                            ServerId = toCommunicate.Metadata.ServerId,
                            SyncBoxId = _syncBoxId
                        };

                        // server method path switched on whether change is a folder or not
                        serverMethodPath = (toCommunicate.Metadata.HashableProperties.IsFolder
                            ? CLDefinitions.MethodPathOneOffFolderMove
                            : CLDefinitions.MethodPathOneOffFileMove);
                        break;

                    default:
                        throw new ArgumentException("toCommunicate Type is an unknown FileChangeType: " + toCommunicate.Type.ToString());
                }

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Event>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    Helpers.requestMethod.post, // one-off methods are all posts
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Event>();
                return ex;
            }
            return null;
        }
        #endregion

        #region UndoDeletionFileChange
        /// <summary>
        /// Asynchronously starts posting a single FileChange to the server
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="deletionChange">Deletion change which needs to be undone</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUndoDeletionFileChange(AsyncCallback aCallback,
            object aState,
            FileChange deletionChange,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<UndoDeletionFileChangeResult> toReturn = new GenericAsyncResult<UndoDeletionFileChangeResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int> asyncParams =
                new Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int>(
                    toReturn,
                    deletionChange,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int> castState = state as Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, FileChange, int>;
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
                        JsonContracts.Event result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UndoDeletionFileChange(
                            castState.Item2,
                            castState.Item3,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new UndoDeletionFileChangeResult(
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UndoDeletionFileChange(FileChange deletionChange, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (deletionChange == null)
                {
                    throw new NullReferenceException("deletionChange cannot be null");
                }
                if (deletionChange.Direction == SyncDirection.From)
                {
                    throw new ArgumentException("deletionChange Direction is not To the server");
                }
                if (deletionChange.Metadata == null)
                {
                    throw new NullReferenceException("deletionChange Metadata cannot be null");
                }
                if (deletionChange.Type != FileChangeType.Deleted)
                {
                    throw new ArgumentException("deletionChange is not of Type Deletion");
                }
                if (_copiedSettings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (string.IsNullOrEmpty(deletionChange.Metadata.ServerId))
                {
                    throw new NullReferenceException("deletionChange Metadata ServerId must not be null");
                }
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Event>(new JsonContracts.FileOrFolderUndelete() // files and folders share a request content object for undelete
                    {
                        DeviceId = _copiedSettings.DeviceId, // device id
                        ServerId = deletionChange.Metadata.ServerId, // unique id on server
                        SyncBoxId = _syncBoxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    (deletionChange.Metadata.HashableProperties.IsFolder // folder/file switch
                        ? CLDefinitions.MethodPathFolderUndelete // path for folder undelete
                        : CLDefinitions.MethodPathFileUndelete), // path for file undelete
                    Helpers.requestMethod.post, // undelete file or folder is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Event>();
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
                        JsonContracts.FileVersion[] result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFileVersions(
                            castState.Item2,
                            castState.Item3,
                            castState.Item4,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetFileVersionsResult(
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions = false)
        {
            return GetFileVersions(fileServerId, timeoutMilliseconds, null, out status, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions = false)
        {
            return GetFileVersions(null, timeoutMilliseconds, pathToFile, out status, out response, includeDeletedVersions);
        }

        /// <summary>
        /// Queries the server for all versions of a given file
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Full path to the file where it would be placed locally within the sync root</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeDeletedVersions">(optional) whether to include file versions which are deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFileVersions(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, out CLHttpRestStatus status, out JsonContracts.FileVersion[] response, bool includeDeletedVersions = false)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new NullReferenceException("Either pathToFile must not be null or fileServerId must not be null or both must not be null");
                }
                if (_copiedSettings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
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
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(pathToFile.GetRelativePath(_copiedSettings.SyncRoot, true)))),

                        // query string parameter for whether to include delete versions in the check, but only set if it's not default (if it's false)
                        (includeDeletedVersions
                            ? new KeyValuePair<string, string>()
                            : new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeDeleted, "false")),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FileVersion[]>(null, // get file versions has no request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // use a dynamic method path because it needs query string parameters
                    Helpers.requestMethod.get, // get file versions is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileVersion[]>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetUsedBytes (deprecated)
        ///// <summary>
        ///// Asynchronously grabs the bytes used by the sync box and the bytes which are pending for upload
        ///// </summary>
        ///// <param name="aCallback">Callback method to fire when operation completes</param>
        ///// <param name="aState">Userstate to pass when firing async callback</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //public IAsyncResult BeginGetUsedBytes(AsyncCallback aCallback,
        //    object aState,
        //    int timeoutMilliseconds)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<GetUsedBytesResult> toReturn = new GenericAsyncResult<GetUsedBytesResult>(
        //        aCallback,
        //        aState);

        //    // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //    Tuple<GenericAsyncResult<GetUsedBytesResult>, int> asyncParams =
        //        new Tuple<GenericAsyncResult<GetUsedBytesResult>, int>(
        //            toReturn,
        //            timeoutMilliseconds);

        //    // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //    (new Thread(new ParameterizedThreadStart(state =>
        //    {
        //        // try cast the state as the object with all the input parameters
        //        Tuple<GenericAsyncResult<GetUsedBytesResult>, int> castState = state as Tuple<GenericAsyncResult<GetUsedBytesResult>, int>;
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
        //                JsonContracts.UsedBytes result;
        //                // run the download of the file with the passed parameters, storing any error that occurs
        //                CLError processError = GetUsedBytes(
        //                    castState.Item2,
        //                    out status,
        //                    out result);

        //                // if there was an asynchronous result in the parameters, then complete it with a new result object
        //                if (castState.Item1 != null)
        //                {
        //                    castState.Item1.Complete(
        //                        new GetUsedBytesResult(
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

        ///// <summary>
        ///// Finishes grabing the bytes used by the sync box and the bytes which are pending for upload if it has not already finished via its asynchronous result and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="aResult">The asynchronous result provided upon starting grabbing the used bytes</param>
        ///// <param name="result">(output) The result from grabbing the used bytes</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //public CLError EndGetUsedBytes(IAsyncResult aResult, out GetUsedBytesResult result)
        //{
        //    // declare the specific type of asynchronous result for grabbing the used bytes
        //    GenericAsyncResult<GetUsedBytesResult> castAResult;

        //    // try/catch to try casting the asynchronous result as the type for grabbing the used bytes and pull the result (possibly incomplete), on catch default the output and return the error
        //    try
        //    {
        //        // try cast the asynchronous result as the type for grabbing the used bytes
        //        castAResult = aResult as GenericAsyncResult<GetUsedBytesResult>;

        //        // if trying to cast the asynchronous result failed, then throw an error
        //        if (castAResult == null)
        //        {
        //            throw new NullReferenceException("aResult does not match expected internal type");
        //        }

        //        // pull the result for output (may not yet be complete)
        //        result = castAResult.Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        result = Helpers.DefaultForType<GetUsedBytesResult>();
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
        ///// Grabs the bytes used by the sync box and the bytes which are pending for upload
        ///// </summary>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="status">(output) success/failure status of communication</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //public CLError GetUsedBytes(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.UsedBytes response)
        //{
        //    // start with bad request as default if an exception occurs but is not explicitly handled to change the status
        //    status = CLHttpRestStatus.BadRequest;
        //    // try/catch to process the undeletion, on catch return the error
        //    try
        //    {
        //        // check input parameters

        //        if (!(timeoutMilliseconds > 0))
        //        {
        //            throw new ArgumentException("timeoutMilliseconds must be greater than zero");
        //        }
        //        if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
        //        {
        //            throw new NullReferenceException("settings DeviceId cannot be null");
        //        }

        //        // run the HTTP communication and store the response object to the output parameter
        //        response = Helpers.ProcessHttp<JsonContracts.UsedBytes>(null, // getting used bytes requires no request content
        //            CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
        //            CLDefinitions.MethodPathGetUsedBytes + // path to get used bytes
        //                Helpers.QueryStringBuilder(new[]
        //                {
        //                    new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)), // device id, escaped since it's a user-input
        //                    new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString()) // sync box id, not escaped since it's from an integer
        //                }),
        //            Helpers.requestMethod.get, // getting used bytes is a get
        //            timeoutMilliseconds, // time before communication timeout
        //            null, // not an upload or download
        //            Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
        //            ref status, // reference to update the output success/failure status for the communication
        //            _copiedSettings, // pass the copied settings
        //            _credential, // pass the key/secret
        //            _syncBoxId); // pass the unique id of the sync box on the server
        //    }
        //    catch (Exception ex)
        //    {
        //        response = Helpers.DefaultForType<JsonContracts.UsedBytes>();
        //        return ex;
        //    }
        //    return null;
        //}
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
                        // declare the output status for communication
                        CLHttpRestStatus status;
                        // declare the specific type of result for this operation
                        JsonContracts.Event result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = CopyFile(
                            castState.Item2,
                            castState.Item3,
                            castState.Item4,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new CopyFileResult(
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath copyTargetPath, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            return CopyFile(fileServerId, timeoutMilliseconds, null, copyTargetPath, out status, out response);
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            return CopyFile(null, timeoutMilliseconds, pathToFile, copyTargetPath, out status, out response);
        }

        /// <summary>
        /// Copies a file on the server to another location
        /// </summary>
        /// <param name="fileServerId">Unique id to the file on the server</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="pathToFile">Location of existing file to copy from</param>
        /// <param name="copyTargetPath">Location where file shoule be copied to</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError CopyFile(string fileServerId, int timeoutMilliseconds, FilePath pathToFile, FilePath copyTargetPath, out CLHttpRestStatus status, out JsonContracts.Event response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the undeletion, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (_copiedSettings.SyncRoot == null)
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }
                if (copyTargetPath == null)
                {
                    throw new NullReferenceException("copyTargetPath cannot be null");
                }
                if (pathToFile == null
                    && string.IsNullOrEmpty(fileServerId))
                {
                    throw new NullReferenceException("Either pathToFile must not be null or fileServerId must not be null or both must not be null");
                }
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Event>(new JsonContracts.FileCopy() // object for file copy
                    {
                        DeviceId = _copiedSettings.DeviceId, // device id
                        ServerId = fileServerId, // unique id on server
                        RelativePath = (pathToFile == null
                            ? null
                            : pathToFile.GetRelativePath(_copiedSettings.SyncRoot, true)), // path of existing file to copy
                        RelativeToPath = copyTargetPath.GetRelativePath(_copiedSettings.SyncRoot, true), // location to copy file to
                        SyncBoxId = _syncBoxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathFileCopy, // path for file copy
                    Helpers.requestMethod.post, // file copy is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Event>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetPictures
        /// <summary>
        /// Asynchronously starts querying the server for pictures
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetPictures(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetPicturesResult> toReturn = new GenericAsyncResult<GetPicturesResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetPicturesResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetPicturesResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetPicturesResult>, int> castState = state as Tuple<GenericAsyncResult<GetPicturesResult>, int>;
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
                        JsonContracts.Pictures result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetPictures(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetPicturesResult(
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
        /// Finishes querying for pictures if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the pictures query</param>
        /// <param name="result">(output) The result from the pictures query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetPictures(IAsyncResult aResult, out GetPicturesResult result)
        {
            // declare the specific type of asynchronous result for pictures query
            GenericAsyncResult<GetPicturesResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for pictures query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for pictures query
                castAResult = aResult as GenericAsyncResult<GetPicturesResult>;

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
                result = Helpers.DefaultForType<GetPicturesResult>();
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
        /// Queries the server for pictures
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetPictures(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Pictures response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the pictures query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the pictures retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetPictures + // path for getting pictures
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Pictures>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query pictures (dynamic adding query string)
                    Helpers.requestMethod.get, // query pictures is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Pictures>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetVideos
        /// <summary>
        /// Asynchronously starts querying the server for videos
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetVideos(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetVideosResult> toReturn = new GenericAsyncResult<GetVideosResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetVideosResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetVideosResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetVideosResult>, int> castState = state as Tuple<GenericAsyncResult<GetVideosResult>, int>;
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
                        JsonContracts.Videos result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetVideos(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetVideosResult(
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
        /// Finishes querying for videos if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the videos query</param>
        /// <param name="result">(output) The result from the videos query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetVideos(IAsyncResult aResult, out GetVideosResult result)
        {
            // declare the specific type of asynchronous result for videos query
            GenericAsyncResult<GetVideosResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for videos query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for videos query
                castAResult = aResult as GenericAsyncResult<GetVideosResult>;

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
                result = Helpers.DefaultForType<GetVideosResult>();
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
        /// Queries the server for videos
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetVideos(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Videos response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the videos query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the videos retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetVideos + // path for getting videos
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Videos>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query videos (dynamic adding query string)
                    Helpers.requestMethod.get, // query videos is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Videos>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetAudios
        /// <summary>
        /// Asynchronously starts querying the server for audios
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetAudios(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetAudiosResult> toReturn = new GenericAsyncResult<GetAudiosResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetAudiosResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetAudiosResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetAudiosResult>, int> castState = state as Tuple<GenericAsyncResult<GetAudiosResult>, int>;
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
                        JsonContracts.Audios result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetAudios(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetAudiosResult(
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
        /// Finishes querying for audios if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the audios query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetAudios(IAsyncResult aResult, out GetAudiosResult result)
        {
            // declare the specific type of asynchronous result for audios query
            GenericAsyncResult<GetAudiosResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for audios query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for audios query
                castAResult = aResult as GenericAsyncResult<GetAudiosResult>;

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
                result = Helpers.DefaultForType<GetAudiosResult>();
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
        /// Queries the server for audios
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetAudios(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Audios response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the audios query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the audios retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetAudios + // path for getting audios
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Audios>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query audios (dynamic adding query string)
                    Helpers.requestMethod.get, // query audios is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Audios>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetArchives
        /// <summary>
        /// Asynchronously starts querying the server for archives
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetArchives(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetArchivesResult> toReturn = new GenericAsyncResult<GetArchivesResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetArchivesResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetArchivesResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetArchivesResult>, int> castState = state as Tuple<GenericAsyncResult<GetArchivesResult>, int>;
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
                        JsonContracts.Archives result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetArchives(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetArchivesResult(
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
        /// Finishes querying for archives if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the archives query</param>
        /// <param name="result">(output) The result from the archives query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetArchives(IAsyncResult aResult, out GetArchivesResult result)
        {
            // declare the specific type of asynchronous result for archives query
            GenericAsyncResult<GetArchivesResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for archives query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for archives query
                castAResult = aResult as GenericAsyncResult<GetArchivesResult>;

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
                result = Helpers.DefaultForType<GetArchivesResult>();
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
        /// Queries the server for archives
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetArchives(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Archives response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the archives query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the archives retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetArchives + // path for getting archives
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Archives>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query archives (dynamic adding query string)
                    Helpers.requestMethod.get, // query archives is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Archives>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetRecents
        /// <summary>
        /// Asynchronously starts querying the server for recents
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetRecents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetRecentsResult> toReturn = new GenericAsyncResult<GetRecentsResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetRecentsResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetRecentsResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetRecentsResult>, int> castState = state as Tuple<GenericAsyncResult<GetRecentsResult>, int>;
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
                        JsonContracts.Recents result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetRecents(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetRecentsResult(
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
        /// Finishes querying for recents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the recents query</param>
        /// <param name="result">(output) The result from the recents query</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetRecents(IAsyncResult aResult, out GetRecentsResult result)
        {
            // declare the specific type of asynchronous result for recents query
            GenericAsyncResult<GetRecentsResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for recents query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for recents query
                castAResult = aResult as GenericAsyncResult<GetRecentsResult>;

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
                result = Helpers.DefaultForType<GetRecentsResult>();
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
        /// Queries the server for recents
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetRecents(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Recents response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the recents query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the recents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetRecents + // path for getting recents
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString()),
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Recents>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query recents (dynamic adding query string)
                    Helpers.requestMethod.get, // query recents is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Recents>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetSyncBoxUsage
        /// <summary>
        /// Asynchronously starts getting sync box usage
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetSyncBoxUsage(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetSyncBoxUsageResult> toReturn = new GenericAsyncResult<GetSyncBoxUsageResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, int> castState = state as Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, int>;
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
                        JsonContracts.SyncBoxUsage result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetSyncBoxUsage(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetSyncBoxUsageResult(
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
        /// Finishes getting sync box usage if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting sync box usage</param>
        /// <param name="result">(output) The result from getting sync box usage</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSyncBoxUsage(IAsyncResult aResult, out GetSyncBoxUsageResult result)
        {
            // declare the specific type of asynchronous result for getting sync box usage
            GenericAsyncResult<GetSyncBoxUsageResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for getting sync box usage and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for getting sync box usage
                castAResult = aResult as GenericAsyncResult<GetSyncBoxUsageResult>;

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
                result = Helpers.DefaultForType<GetSyncBoxUsageResult>();
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetSyncBoxUsage(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxUsage response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the sync box usage query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the sync box usage retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathSyncBoxUsage + // path for getting sync box usage
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.SyncBoxUsage>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query synx box usage (dynamic adding query string)
                    Helpers.requestMethod.get, // query sync box usage is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncBoxUsage>();
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
                        JsonContracts.Folders result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFolderHierarchy(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetFolderHierarchyResult(
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="hierarchyRoot">(optional) root path of hierarchy query</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderHierarchy(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Folders response, FilePath hierarchyRoot = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the folder hierarchy query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (string.IsNullOrEmpty(_copiedSettings.SyncRoot))
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }

                // build the location of the folder hierarchy retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderHierarchy + // path for getting folder hierarchy
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString()),

                        (hierarchyRoot == null
                            ? new KeyValuePair<string, string>() // do not add extra query string parameter if path is not set
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(hierarchyRoot.GetRelativePath(_copiedSettings.SyncRoot, true) + "/"))) // query string parameter for optional path with escaped value
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.Folders>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder hierarchy (dynamic adding query string)
                    Helpers.requestMethod.get, // query folder hierarchy is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Folders>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetFolderContents
        /// <summary>
        /// Asynchronously starts querying folder contents with optional path and optional depth limit
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool includeCount = false,
            FilePath contentsRoot = null,
            Nullable<byte> depthLimit = null,
            bool includeDeleted = false)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetFolderContentsResult> toReturn = new GenericAsyncResult<GetFolderContentsResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetFolderContentsResult>, int, bool, FilePath, Nullable<byte>, bool> asyncParams =
                new Tuple<GenericAsyncResult<GetFolderContentsResult>, int, bool, FilePath, Nullable<byte>, bool>(
                    toReturn,
                    timeoutMilliseconds,
                    includeCount,
                    contentsRoot,
                    depthLimit,
                    includeDeleted);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetFolderContentsResult>, int, bool, FilePath, Nullable<byte>, bool> castState = state as Tuple<GenericAsyncResult<GetFolderContentsResult>, int, bool, FilePath, Nullable<byte>, bool>;
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
                        JsonContracts.FolderContents result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFolderContents(
                            castState.Item2,
                            out status,
                            out result,
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            castState.Item6);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetFolderContentsResult(
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
        /// Finishes getting folder contents if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting folder contents</param>
        /// <param name="result">(output) The result from folder contents</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetFolderContents(IAsyncResult aResult, out GetFolderContentsResult result)
        {
            // declare the specific type of asynchronous result for getting folder contents
            GenericAsyncResult<GetFolderContentsResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for getting folder contents and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for getting folder contents
                castAResult = aResult as GenericAsyncResult<GetFolderContentsResult>;

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
                result = Helpers.DefaultForType<GetFolderContentsResult>();
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
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="includeCount">(optional) whether to include counts of items inside each folder in the response object</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="depthLimit">(optional) how many levels deep to search from the root or provided path, use {null} to return everything</param>
        /// <param name="includeDeleted">(optional) whether to include changes which are marked deleted</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            bool includeCount = false,
            FilePath contentsRoot = null,
            Nullable<byte> depthLimit = null,
            bool includeDeleted = false)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the folder contents query, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (string.IsNullOrEmpty(_copiedSettings.SyncRoot))
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }

                // build the location of the folder contents retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathGetFolderContents + // path for getting folder contents
                    Helpers.QueryStringBuilder(new[]
                    {
                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString()),

                        (depthLimit == null
                            ? new KeyValuePair<string, string>() // do not add extra query string parameter if depth is not limited
                            : new KeyValuePair<string, string>(CLDefinitions.QueryStringDepth, ((byte)depthLimit).ToString())), // query string parameter for optional depth limit

                        (contentsRoot == null
                            ? new KeyValuePair<string, string>() // do not add extra query string parameter if path is not set
                            : new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(contentsRoot.GetRelativePath(_copiedSettings.SyncRoot, true) + "/"))), // query string parameter for optional path with escaped value

                        (includeDeleted
                            ? new KeyValuePair<string, string>() // do not add extra query string parameter if parameter is already the default
                            : new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeDeleted, "false")), // query string parameter for not including deleted objects

                        (includeCount
                            ? new KeyValuePair<string, string>(CLDefinitions.QueryStringIncludeCount, "true") // query string parameter for including counts within each folder
                            : new KeyValuePair<string, string>()) // do not add extra query string parameter if parameter is already the default
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.FolderContents>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder contents (dynamic adding query string)
                    Helpers.requestMethod.get, // query folder contents is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FolderContents>();
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
                        JsonContracts.PendingResponse result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = PurgePending(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new PurgePendingResult(
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
                    throw new NullReferenceException("aResult does not match expected internal type");
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError PurgePending(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PendingResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process purging pending, on catch return the error
            try
            {
                // check input parameters

                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.PendingResponse>(new JsonContracts.PurgePending() // json contract object for purge pending method
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        SyncBoxId = _syncBoxId
                    },
                    CLDefinitions.CLMetaDataServerURL,      // MDS server URL
                    CLDefinitions.MethodPathPurgePending, // purge pending address
                    Helpers.requestMethod.post, // purge pending is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // purge pending should give OK or Accepted
                    ref status, // reference to update output status
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PendingResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region UpdateSyncBoxExtendedMetadata
        /// <summary>
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncBoxExtendedMetadata<T>(AsyncCallback aCallback,
            object aState,
            IDictionary<string, T> metadata,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult> toReturn = new GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, IDictionary<string, T>, int> asyncParams =
                new Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, IDictionary<string, T>, int>(
                    toReturn,
                    metadata,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, IDictionary<string, T>, int> castState = state as Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, IDictionary<string, T>, int>;
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
                        JsonContracts.SyncBoxHolder result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = UpdateSyncBoxExtendedMetadata(
                            castState.Item2,
                            castState.Item3,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SyncBoxUpdateExtendedMetadataResult(
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
        /// Asynchronously updates the extended metadata on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncBoxExtendedMetadata(AsyncCallback aCallback,
            object aState,
            MetadataDictionary metadata,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult> toReturn = new GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, MetadataDictionary, int> asyncParams =
                new Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, MetadataDictionary, int>(
                    toReturn,
                    metadata,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, MetadataDictionary, int> castState = state as Tuple<GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>, MetadataDictionary, int>;
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
                        JsonContracts.SyncBoxHolder result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = UpdateSyncBoxExtendedMetadata(
                            castState.Item2,
                            castState.Item3,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new SyncBoxUpdateExtendedMetadataResult(
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
        /// Finishes updating the extended metadata on a sync box if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting updating extended metadata</param>
        /// <param name="result">(output) The result from updating extended metadata</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndUpdateSyncBoxExtendedMetadata(IAsyncResult aResult, out SyncBoxUpdateExtendedMetadataResult result)
        {
            // declare the specific type of asynchronous result for updating extended metadata
            GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating extended metadata and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for updating extended metadata
                castAResult = aResult as GenericAsyncResult<SyncBoxUpdateExtendedMetadataResult>;

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
                result = Helpers.DefaultForType<SyncBoxUpdateExtendedMetadataResult>();
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncBoxExtendedMetadata<T>(IDictionary<string, T> metadata, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response)
        {
            try
            {
                return UpdateSyncBoxExtendedMetadata((metadata == null
                        ? null
                        : new JsonContracts.MetadataDictionary(
                            ((metadata is IDictionary<string, object>)
                                ? (IDictionary<string, object>)metadata
                                : new JsonContracts.MetadataDictionary.DictionaryWrapper<T>(metadata)))),
                    timeoutMilliseconds, out status, out response);
            }
            catch (Exception ex)
            {
                status = CLHttpRestStatus.BadRequest;
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return ex;
            }
        }

        /// <summary>
        /// Updates the extended metadata on a sync box
        /// </summary>
        /// <param name="metadata">string keys to serializable object values to store as extra metadata to the sync box</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncBoxExtendedMetadata(MetadataDictionary metadata, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process setting extended metadata, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.SyncBoxHolder>(new JsonContracts.SyncBoxMetadata() // json contract object for extended sync box metadata
                    {
                        Id = SyncBoxId,
                        Metadata = metadata
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncBoxExtendedMetadata, // sync box extended metadata path
                    Helpers.requestMethod.post, // sync box extended metadata is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box extended metadata should give OK or Accepted
                    ref status, // reference to update output status
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return ex;
            }
            return null;
        }
        #endregion

        #region UpdateSyncBoxQuota (deprecated)
        ///// <summary>
        ///// Asynchronously updates the storage quota on a sync box
        ///// </summary>
        ///// <param name="aCallback">Callback method to fire when operation completes</param>
        ///// <param name="aState">Userstate to pass when firing async callback</param>
        ///// <param name="quotaSize">How many bytes big to make the storage quota</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //public IAsyncResult BeginUpdateSyncBoxQuota(AsyncCallback aCallback,
        //    object aState,
        //    long quotaSize,
        //    int timeoutMilliseconds)
        //{
        //    return BeginUpdateSyncBoxQuota(aCallback, aState, quotaSize, timeoutMilliseconds, reservedForActiveSync: false);
        //}
        
        ///// <summary>
        ///// Internal helper (extra bool to fail immediately): Asynchronously updates the storage quota on a sync box
        ///// </summary>
        ///// <param name="aCallback">Callback method to fire when operation completes</param>
        ///// <param name="aState">Userstate to pass when firing async callback</param>
        ///// <param name="quotaSize">How many bytes big to make the storage quota</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        //internal IAsyncResult BeginUpdateSyncBoxQuota(AsyncCallback aCallback,
        //    object aState,
        //    long quotaSize,
        //    int timeoutMilliseconds,
        //    bool reservedForActiveSync)
        //{
        //    // create the asynchronous result to return
        //    GenericAsyncResult<UpdateSyncBoxQuotaResult> toReturn = new GenericAsyncResult<SyncBoxUpdateQuotaResult>(
        //        aCallback,
        //        aState);

        //    if (reservedForActiveSync)
        //    {
        //        CLHttpRestStatus unusedStatus;
        //        JsonContracts.SyncBoxHolder unusedResult;
        //        toReturn.Complete(
        //            new UpdateSyncBoxQuotaResult(
        //                UpdateSyncBoxQuota(
        //                    quotaSize,
        //                    timeoutMilliseconds,
        //                    out unusedStatus,
        //                    out unusedResult,
        //                    reservedForActiveSync),
        //                unusedStatus,
        //                unusedResult),
        //            sCompleted: true);
        //    }
        //    else
        //    {
        //        // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
        //        Tuple<GenericAsyncResult<UpdateSyncBoxQuotaResult>, long, int> asyncParams =
        //            new Tuple<GenericAsyncResult<UpdateSyncBoxQuotaResult>, long, int>(
        //                toReturn,
        //                quotaSize,
        //                timeoutMilliseconds);

        //        // create the thread from a void (object) parameterized start which wraps the synchronous method call
        //        (new Thread(new ParameterizedThreadStart(state =>
        //        {
        //            // try cast the state as the object with all the input parameters
        //            Tuple<GenericAsyncResult<UpdateSyncBoxQuotaResult>, long, int> castState = state as Tuple<GenericAsyncResult<UpdateSyncBoxQuotaResult>, long, int>;
        //            // if the try cast failed, then show a message box for this unrecoverable error
        //            if (castState == null)
        //            {
        //                MessageEvents.FireNewEventMessage(
        //                    "Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState),
        //                    EventMessageLevel.Important,
        //                    new HaltAllOfCloudSDKErrorInfo());
        //            }
        //            // else if the try cast did not fail, then start processing with the input parameters
        //            else
        //            {
        //                // try/catch to process with the input parameters, on catch set the exception in the asyncronous result
        //                try
        //                {
        //                    // declare the output status for communication
        //                    CLHttpRestStatus status;
        //                    // declare the specific type of result for this operation
        //                    JsonContracts.SyncBoxHolder result;
        //                    // purge pending files with the passed parameters, storing any error that occurs
        //                    CLError processError = UpdateSyncBoxQuota(
        //                        castState.Item2,
        //                        castState.Item3,
        //                        out status,
        //                        out result);

        //                    // if there was an asynchronous result in the parameters, then complete it with a new result object
        //                    if (castState.Item1 != null)
        //                    {
        //                        castState.Item1.Complete(
        //                            new UpdateSyncBoxQuotaResult(
        //                                processError, // any error that may have occurred during processing
        //                                status, // the output status of communication
        //                                result), // the specific type of result for this operation
        //                                sCompleted: false); // processing did not complete synchronously
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    // if there was an asynchronous result in the parameters, then pass through the exception to it
        //                    if (castState.Item1 != null)
        //                    {
        //                        castState.Item1.HandleException(
        //                            ex, // the exception which was not handled correctly by the CLError wrapping
        //                            sCompleted: false); // processing did not complete synchronously
        //                    }
        //                }
        //            }
        //        }))).Start(asyncParams); // start the asynchronous processing thread with the input parameters object
        //    }

        //    // return the asynchronous result
        //    return toReturn;
        //}

        ///// <summary>
        ///// Finishes updating the storage quota on a sync box if it has not already finished via its asynchronous result and outputs the result,
        ///// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        ///// </summary>
        ///// <param name="aResult">The asynchronous result provided upon starting updating storage quota</param>
        ///// <param name="result">(output) The result from updating storage quota</param>
        ///// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        //public CLError EndUpdateSyncBoxQuota(IAsyncResult aResult, out UpdateSyncBoxQuotaResult result)
        //{
        //    // declare the specific type of asynchronous result for updating storage quota
        //    GenericAsyncResult<UpdateSyncBoxQuotaResult> castAResult;

        //    // try/catch to try casting the asynchronous result as the type for updating storage quota and pull the result (possibly incomplete), on catch default the output and return the error
        //    try
        //    {
        //        // try cast the asynchronous result as the type for updating storage quota
        //        castAResult = aResult as GenericAsyncResult<UpdateSyncBoxQuotaResult>;

        //        // if trying to cast the asynchronous result failed, then throw an error
        //        if (castAResult == null)
        //        {
        //            throw new NullReferenceException("aResult does not match expected internal type");
        //        }

        //        // pull the result for output (may not yet be complete)
        //        result = castAResult.Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        result = Helpers.DefaultForType<UpdateSyncBoxQuotaResult>();
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
        ///// Updates the storage quota on a sync box
        ///// </summary>
        ///// <param name="quotaSize">How many bytes big to make the storage quota</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="status">(output) success/failure status of communication</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //public CLError UpdateSyncBoxQuota(long quotaSize, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response)
        //{
        //    return UpdateSyncBoxQuota(quotaSize, timeoutMilliseconds, out status, out response, reservedForActiveSync: false);
        //}

        ///// <summary>
        ///// Internal helper (extra bool to fail immediately): Updates the storage quota on a sync box
        ///// </summary>
        ///// <param name="quotaSize">How many bytes big to make the storage quota</param>
        ///// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        ///// <param name="status">(output) success/failure status of communication</param>
        ///// <param name="response">(output) response object from communication</param>
        ///// <returns>Returns any error that occurred during communication, if any</returns>
        //internal CLError UpdateSyncBoxQuota(long quotaSize, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response, bool reservedForActiveSync)
        //{
        //    if (reservedForActiveSync)
        //    {
        //        status = CLHttpRestStatus.ReservedForActiveSync;
        //        response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
        //        return new Exception("Current SyncBox cannot be modified while in use in active syncing");
        //    }

        //    // start with bad request as default if an exception occurs but is not explicitly handled to change the status
        //    status = CLHttpRestStatus.BadRequest;

        //    IncrementModifyingSyncBoxViaPublicAPICalls();

        //    // try/catch to process updating quota, on catch return the error
        //    try
        //    {
        //        // check input parameters

        //        if (!(timeoutMilliseconds > 0))
        //        {
        //            throw new ArgumentException("timeoutMilliseconds must be greater than zero");
        //        }

        //        if (!(quotaSize > 0))
        //        {
        //            throw new ArgumentException("quotaSize must be greater than zero");
        //        }

        //        response = Helpers.ProcessHttp<JsonContracts.SyncBoxHolder>(new JsonContracts.SyncBoxQuota() // json contract object for sync box storage quota
        //        {
        //            Id = SyncBoxId,
        //            StorageQuota = quotaSize
        //        },
        //            CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
        //            CLDefinitions.MethodPathAuthSyncBoxQuota, // sync box storage quota path
        //            Helpers.requestMethod.post, // sync box storage quota is a post operation
        //            timeoutMilliseconds, // set the timeout for the operation
        //            null, // not an upload or download
        //            Helpers.HttpStatusesOkAccepted, // sync box storage quota should give OK or Accepted
        //            ref status, // reference to update output status
        //            _copiedSettings, // pass the copied settings
        //            _credential, // pass the key/secret
        //            _syncBoxId); // pass the unique id of the sync box on the server
        //    }
        //    catch (Exception ex)
        //    {
        //        response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
        //        return ex;
        //    }
        //    finally
        //    {
        //        DecrementModifyingSyncBoxViaPublicAPICalls();
        //    }
        //    return null;
        //}
        #endregion

        #region UpdateSyncBoxPlan
        /// <summary>
        /// Asynchronously updates the plan used by a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncBoxPlan(AsyncCallback aCallback,
            object aState,
            long planId,
            int timeoutMilliseconds)
        {
            return BeginUpdateSyncBoxPlan(aCallback, aState, planId, timeoutMilliseconds, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): Asynchronously updates the plan on a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateSyncBoxPlan(AsyncCallback aCallback,
            object aState,
            long planId,
            int timeoutMilliseconds,
            bool reservedForActiveSync)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncBoxUpdatePlanResult> toReturn = new GenericAsyncResult<SyncBoxUpdatePlanResult>(
                aCallback,
                aState);

            if (reservedForActiveSync)
            {
                CLHttpRestStatus unusedStatus;
                JsonContracts.SyncBoxUpdatePlanResponse unusedResult;
                toReturn.Complete(
                    new SyncBoxUpdatePlanResult(
                        UpdateSyncBoxPlan(
                            planId,
                            timeoutMilliseconds,
                            out unusedStatus,
                            out unusedResult,
                            reservedForActiveSync),
                        unusedStatus,
                        unusedResult),
                    sCompleted: true);
            }
            else
            {
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                Tuple<GenericAsyncResult<SyncBoxUpdatePlanResult>, long, int> asyncParams =
                    new Tuple<GenericAsyncResult<SyncBoxUpdatePlanResult>, long, int>(
                        toReturn,
                        planId,
                        timeoutMilliseconds);

                // create the thread from a void (object) parameterized start which wraps the synchronous method call
                (new Thread(new ParameterizedThreadStart(state =>
                {
                    // try cast the state as the object with all the input parameters
                    Tuple<GenericAsyncResult<SyncBoxUpdatePlanResult>, long, int> castState = state as Tuple<GenericAsyncResult<SyncBoxUpdatePlanResult>, long, int>;
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
                            JsonContracts.SyncBoxUpdatePlanResponse result;
                            // purge pending files with the passed parameters, storing any error that occurs
                            CLError processError = UpdateSyncBoxPlan(
                                castState.Item2,
                                castState.Item3,
                                out status,
                                out result);

                            // if there was an asynchronous result in the parameters, then complete it with a new result object
                            if (castState.Item1 != null)
                            {
                                castState.Item1.Complete(
                                    new SyncBoxUpdatePlanResult(
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
        public CLError EndUpdateSyncBoxPlan(IAsyncResult aResult, out SyncBoxUpdatePlanResult result)
        {
            // declare the specific type of asynchronous result for updating the plan
            GenericAsyncResult<SyncBoxUpdatePlanResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating the plan and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for updating the plan
                castAResult = aResult as GenericAsyncResult<SyncBoxUpdatePlanResult>;

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
                result = Helpers.DefaultForType<SyncBoxUpdatePlanResult>();
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
        /// Updates the the plan on a sync box
        /// </summary>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncBoxPlan(long planId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxUpdatePlanResponse response)
        {
            return UpdateSyncBoxPlan(planId, timeoutMilliseconds, out status, out response, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): Updates the plan on a sync box
        /// </summary>
        /// <param name="planId">The ID of the plan to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateSyncBoxPlan(long planId, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxUpdatePlanResponse response, bool reservedForActiveSync)
        {
            if (reservedForActiveSync)
            {
                status = CLHttpRestStatus.ReservedForActiveSync;
                response = Helpers.DefaultForType<JsonContracts.SyncBoxUpdatePlanResponse>();
                return new Exception("Current SyncBox cannot be modified while in use in active syncing");
            }

            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            IncrementModifyingSyncBoxViaPublicAPICalls();

            // try/catch to process updating plan, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                if (planId == 0)
                {
                    throw new ArgumentException("planId must not be zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.SyncBoxUpdatePlanResponse>(new JsonContracts.SyncBoxUpdatePlanRequest() // json contract object for sync box update plan request
                {
                    SyncBoxId = SyncBoxId,
                    PlanId = planId
                },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncBoxUpdatePlan, // sync box update plan path
                    Helpers.requestMethod.post, // sync box update plan is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box update plan should give OK or Accepted
                    ref status, // reference to update output status
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncBoxUpdatePlanResponse>();
                return ex;
            }
            finally
            {
                DecrementModifyingSyncBoxViaPublicAPICalls();
            }
            return null;
        }
        #endregion

        #region UpdateSyncBox
        /// <summary>
        /// Asynchronously updates the properties of a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginUpdateSyncBox(AsyncCallback aCallback,
            object aState,
            string friendlyName,
            int timeoutMilliseconds)
        {
            return BeginUpdateSyncBox(aCallback, aState, friendlyName, timeoutMilliseconds, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): Asynchronously updates the properties of a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginUpdateSyncBox(AsyncCallback aCallback,
            object aState,
            string friendlyName,
            int timeoutMilliseconds,
            bool reservedForActiveSync)
        {
            // create the asynchronous result to return
            GenericAsyncResult<SyncBoxUpdateResult> toReturn = new GenericAsyncResult<SyncBoxUpdateResult>(
                aCallback,
                aState);

            if (reservedForActiveSync)
            {
                CLHttpRestStatus unusedStatus;
                JsonContracts.SyncBoxHolder unusedResult;
                toReturn.Complete(
                    new SyncBoxUpdateResult(
                        UpdateSyncBox(
                            friendlyName,
                            timeoutMilliseconds,
                            out unusedStatus,
                            out unusedResult,
                            reservedForActiveSync),
                        unusedStatus,
                        unusedResult),
                    sCompleted: true);
            }
            else
            {
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                Tuple<GenericAsyncResult<SyncBoxUpdateResult>, string, int> asyncParams =
                    new Tuple<GenericAsyncResult<SyncBoxUpdateResult>, string, int>(
                        toReturn,
                        friendlyName,
                        timeoutMilliseconds);

                // create the thread from a void (object) parameterized start which wraps the synchronous method call
                (new Thread(new ParameterizedThreadStart(state =>
                {
                    // try cast the state as the object with all the input parameters
                    Tuple<GenericAsyncResult<SyncBoxUpdateResult>, string, int> castState = state as Tuple<GenericAsyncResult<SyncBoxUpdateResult>, string, int>;
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
                            JsonContracts.SyncBoxHolder result;
                            // purge pending files with the passed parameters, storing any error that occurs
                            CLError processError = UpdateSyncBox(
                                castState.Item2,
                                castState.Item3,
                                out status,
                                out result);

                            // if there was an asynchronous result in the parameters, then complete it with a new result object
                            if (castState.Item1 != null)
                            {
                                castState.Item1.Complete(
                                    new SyncBoxUpdateResult(
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
        public CLError EndUpdateSyncBox(IAsyncResult aResult, out SyncBoxUpdateResult result)
        {
            // declare the specific type of asynchronous result for updating the properties
            GenericAsyncResult<SyncBoxUpdateResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for updating the properties and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for setting the properties of the syncbox
                castAResult = aResult as GenericAsyncResult<SyncBoxUpdateResult>;

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
                result = Helpers.DefaultForType<SyncBoxUpdateResult>();
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UpdateSyncBox(string friendlyName, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response)
        {
            return UpdateSyncBox(friendlyName, timeoutMilliseconds, out status, out response, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): Updates the properties of a sync box
        /// </summary>
        /// <param name="friendlyName">The friendly name of the syncbox to set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError UpdateSyncBox(string friendlyName, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response, bool reservedForActiveSync)
        {
            if (reservedForActiveSync)
            {
                status = CLHttpRestStatus.ReservedForActiveSync;
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return new Exception("Current SyncBox cannot be modified while in use in active syncing");
            }

            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            IncrementModifyingSyncBoxViaPublicAPICalls();

            // try/catch to process updating the properties, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                if (String.IsNullOrWhiteSpace(friendlyName))
                {
                    throw new ArgumentException("friendlyName must be specified");
                }

                response = Helpers.ProcessHttp<JsonContracts.SyncBoxHolder>(new JsonContracts.SyncBoxUpdateRequest() // json contract object for sync box update request
                {
                    SyncBoxId = SyncBoxId,
                    SyncBox = new JsonContracts.SyncBoxForUpdateRequest()
                    {
                        FriendlyName = friendlyName
                    }
                },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncBoxUpdate, // sync box update
                    Helpers.requestMethod.post, // sync box update is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box update should give OK or Accepted
                    ref status, // reference to update output status
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return ex;
            }
            finally
            {
                DecrementModifyingSyncBoxViaPublicAPICalls();
            }
            return null;
        }
        #endregion

        #region DeleteSyncBox
        /// <summary>
        /// ¡¡ Do not use lightly !! Asynchronously deletes a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDeleteSyncBox(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            return BeginDeleteSyncBox(aCallback, aState, timeoutMilliseconds, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): ¡¡ Do not use lightly !! Asynchronously deletes a sync box
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        internal IAsyncResult BeginDeleteSyncBox(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            bool reservedForActiveSync)
        {
            // create the asynchronous result to return
            GenericAsyncResult<DeleteSyncBoxResult> toReturn = new GenericAsyncResult<DeleteSyncBoxResult>(
                aCallback,
                aState);

            if (reservedForActiveSync)
            {
                CLHttpRestStatus unusedStatus;
                JsonContracts.SyncBoxHolder unusedResult;
                toReturn.Complete(
                    new DeleteSyncBoxResult(
                        DeleteSyncBox(
                            timeoutMilliseconds,
                            out unusedStatus,
                            out unusedResult,
                            reservedForActiveSync),
                        unusedStatus,
                        unusedResult),
                    sCompleted: true);
            }
            else
            {
                // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
                Tuple<GenericAsyncResult<DeleteSyncBoxResult>, int> asyncParams =
                    new Tuple<GenericAsyncResult<DeleteSyncBoxResult>, int>(
                        toReturn,
                        timeoutMilliseconds);

                // create the thread from a void (object) parameterized start which wraps the synchronous method call
                (new Thread(new ParameterizedThreadStart(state =>
                {
                    // try cast the state as the object with all the input parameters
                    Tuple<GenericAsyncResult<DeleteSyncBoxResult>, int> castState = state as Tuple<GenericAsyncResult<DeleteSyncBoxResult>, int>;
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
                            JsonContracts.SyncBoxHolder result;
                            // purge pending files with the passed parameters, storing any error that occurs
                            CLError processError = DeleteSyncBox(
                                castState.Item2,
                                out status,
                                out result);

                            // if there was an asynchronous result in the parameters, then complete it with a new result object
                            if (castState.Item1 != null)
                            {
                                castState.Item1.Complete(
                                    new DeleteSyncBoxResult(
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
        public CLError EndDeleteSyncBox(IAsyncResult aResult, out DeleteSyncBoxResult result)
        {
            // declare the specific type of asynchronous result for sync box deletion
            GenericAsyncResult<DeleteSyncBoxResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for sync box deletion and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for sync box deletion
                castAResult = aResult as GenericAsyncResult<DeleteSyncBoxResult>;

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
                result = Helpers.DefaultForType<DeleteSyncBoxResult>();
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
        /// ¡¡ Do not use lightly !! Deletes a sync box
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError DeleteSyncBox(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response)
        {
            return DeleteSyncBox(timeoutMilliseconds, out status, out response, reservedForActiveSync: false);
        }

        /// <summary>
        /// Internal helper (extra bool to fail immediately): ¡¡ Do not use lightly !! Deletes a sync box
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError DeleteSyncBox(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response, bool reservedForActiveSync)
        {
            if (reservedForActiveSync)
            {
                status = CLHttpRestStatus.ReservedForActiveSync;
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return new Exception("Current SyncBox cannot be modified while in use in active syncing");
            }

            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            IncrementModifyingSyncBoxViaPublicAPICalls();

            // try/catch to process deleting sync box, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.SyncBoxHolder>(new JsonContracts.SyncBoxIdOnly() // json contract object for deleting sync boxes
                    {
                        Id = SyncBoxId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthDeleteSyncBox, // delete sync box path
                    Helpers.requestMethod.post, // delete sync box is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // delete sync box should give OK or Accepted
                    ref status, // reference to update output status
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
                return ex;
            }
            finally
            {
                DecrementModifyingSyncBoxViaPublicAPICalls();
            }
            return null;
        }
        #endregion

        #region GetSyncBoxStatus
        /// <summary>
        /// Asynchronously gets the status of this SyncBox
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetSyncBoxStatus(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetSyncBoxStatusResult> toReturn = new GenericAsyncResult<GetSyncBoxStatusResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetSyncBoxStatusResult>, int> asyncParams =
                new Tuple<GenericAsyncResult<GetSyncBoxStatusResult>, int>(
                    toReturn,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetSyncBoxStatusResult>, int> castState = state as Tuple<GenericAsyncResult<GetSyncBoxStatusResult>, int>;
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
                        JsonContracts.SyncBoxHolder result;
                        // purge pending files with the passed parameters, storing any error that occurs
                        CLError processError = GetSyncBoxStatus(
                            castState.Item2,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetSyncBoxStatusResult(
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
        /// Finishes getting sync box status if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting getting sync box status</param>
        /// <param name="result">(output) The result from getting sync box status</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetSyncBoxStatus(IAsyncResult aResult, out GetSyncBoxStatusResult result)
        {
            // declare the specific type of asynchronous result for sync box status
            GenericAsyncResult<GetSyncBoxStatusResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for getting sync box status and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for getting sync box status
                castAResult = aResult as GenericAsyncResult<GetSyncBoxStatusResult>;

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
                result = Helpers.DefaultForType<GetSyncBoxStatusResult>();
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
        /// Gets the status of this SyncBox
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetSyncBoxStatus(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.SyncBoxHolder response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process purging pending, on catch return the error
            try
            {
                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                response = Helpers.ProcessHttp<JsonContracts.SyncBoxHolder>(new JsonContracts.SyncBoxIdOnly() // json contract object for purge pending method
                    {
                        Id = SyncBoxId
                    },
                    CLDefinitions.CLPlatformAuthServerURL, // Platform server URL
                    CLDefinitions.MethodPathAuthSyncBoxStatus, // sync box status address
                    Helpers.requestMethod.post, // sync box status is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // sync box status should give OK or Accepted
                    ref status, // reference to update output status
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.SyncBoxHolder>();
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        internal CLError SyncToCloud(To syncToRequest, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.To response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (syncToRequest == null)
                {
                    throw new ArgumentException("syncToRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.To>(
                    syncToRequest, // object for request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathSyncTo, // path to sync to
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
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
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, or null.</returns>
        internal CLError SyncFromCloud(Push pushRequest, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PushResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;

            // try/catch to process the sync_to request, on catch return the error
            try
            {
                // check input parameters
                if (pushRequest == null)
                {
                    throw new ArgumentException("pushRequest must not be null");
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = Helpers.ProcessHttp<JsonContracts.PushResponse>(
                    pushRequest, // object to write as request content to the server
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathSyncFrom, // path to sync from
                    Helpers.requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    Helpers.HttpStatusesOkAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status, // reference to update the output success/failure status for the communication
                    _copiedSettings, // pass the copied settings
                    _credential, // pass the key/secret
                    _syncBoxId); // pass the unique id of the sync box on the server
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PushResponse>();
                return ex;
            }

            return null;
        }
        #endregion
    }
}