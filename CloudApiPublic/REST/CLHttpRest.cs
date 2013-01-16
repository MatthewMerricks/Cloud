//
// CLHttpRest.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Sync;
using System.IO;
using System.Threading;
using System.Net;
using CloudApiPublic.Model;
using CloudApiPublic.JsonContracts;
using CloudApiPublic.Static;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using CloudApiPublic.Support;
using System.Linq.Expressions;
using System.Windows;

namespace CloudApiPublic.REST
{
    /// <summary>
    /// Client for manual HTTP communication calls to the Cloud
    /// </summary>
    internal sealed class CLHttpRest
    {
        #region private static readonly fields
        // hash set for http communication methods which are good when the status is ok, created, or not modified
        private static readonly HashSet<HttpStatusCode> okCreatedNotModified = new HashSet<HttpStatusCode>(new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Created,
                HttpStatusCode.NotModified,
            });

        // hash set for http communication methods which are good when the status is ok or accepted
        private static readonly HashSet<HttpStatusCode> okAccepted = new HashSet<HttpStatusCode>(new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Accepted
            });

        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableRequestTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Download), JsonContractHelpers.DownloadSerializer },
            { typeof(JsonContracts.PurgePending), JsonContractHelpers.PurgePendingSerializer },
            { typeof(JsonContracts.Push), JsonContractHelpers.PushSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer },
            
            #region one-offs
            { typeof(JsonContracts.FolderAdd), JsonContractHelpers.FolderAddSerializer },

            { typeof(JsonContracts.FileAdd), JsonContractHelpers.FileAddSerializer },
            { typeof(JsonContracts.FileModify), JsonContractHelpers.FileModifySerializer },

            { typeof(JsonContracts.FileOrFolderDelete), JsonContractHelpers.FileOrFolderDeleteSerializer },
            { typeof(JsonContracts.FileOrFolderMove), JsonContractHelpers.FileOrFolderMoveSerializer },
            { typeof(JsonContracts.FileOrFolderUndelete), JsonContractHelpers.FileOrFolderUndeleteSerializer },
            #endregion

            { typeof(JsonContracts.FileCopy), JsonContractHelpers.FileCopySerializer }
        };

        // dictionary to find which Json contract serializer to use given a provided input type
        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableResponseTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Metadata), JsonContractHelpers.GetMetadataResponseSerializer },
            { typeof(JsonContracts.NotificationResponse), JsonContractHelpers.NotificationResponseSerializer },
            { typeof(JsonContracts.PendingResponse), JsonContractHelpers.PendingResponseSerializer },
            { typeof(JsonContracts.PushResponse), JsonContractHelpers.PushResponseSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer },
            { typeof(JsonContracts.Event), JsonContractHelpers.EventSerializer },
            { typeof(JsonContracts.FileVersion[]), JsonContractHelpers.FileVersionsSerializer },
            { typeof(JsonContracts.UsedBytes), JsonContractHelpers.UsedBytesSerializer },
            { typeof(JsonContracts.Pictures), JsonContractHelpers.PicturesSerializer },
            { typeof(JsonContracts.SyncBoxUsage), JsonContractHelpers.SyncBoxUsageSerializer },
            { typeof(JsonContracts.Folders), JsonContractHelpers.FoldersSerializer },
            { typeof(JsonContracts.FolderContents), JsonContractHelpers.FolderContentsSerializer }
        };
        #endregion

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
        public CLCredentials Credentials
        {
            get
            {
                return _credentials;
            }
        }
        private readonly CLCredentials _credentials;

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
        private CLHttpRest(CLCredentials credentials, long syncBoxId, ICLSyncSettings settings)
        {
            if (credentials == null)
            {
                throw new NullReferenceException("credentials cannot be null");
            }

            this._credentials = credentials;
            this._syncBoxId = syncBoxId;
            if (settings == null)
            {
                this._copiedSettings = new AdvancedSyncSettings(
                    false,
                    TraceType.NotEnabled,
                    null,
                    true,
                    0,
                    Environment.MachineName + Guid.NewGuid().ToString("N"),
                    null,
                    "SimpleClient01",
                    Environment.MachineName,
                    null,
                    null);
            }
            else
            {
                this._copiedSettings = settings.CopySettings();
            }
        }

        /// <summary>
        /// Creates a CLHttpRest client object for HTTP REST calls to the server
        /// </summary>
        /// <param name="credentials">Contains authentication information required for communication</param>
        /// <param name="syncBoxId">ID of sync box which can be manually synced</param>
        /// <param name="client">(output) Created CLHttpRest client</param>
        /// <param name="settings">(optional) Additional settings to override some defaulted parameters</param>
        /// <returns>Returns any error creating the CLHttpRest client, if any</returns>
        public static CLError CreateAndInitialize(CLCredentials credentials, long syncBoxId, out CLHttpRest client, ICLSyncSettings settings = null)
        {
            try
            {
                client = new CLHttpRest(credentials, syncBoxId, settings);
            }
            catch (Exception ex)
            {
                client = Helpers.DefaultForType<CLHttpRest>();
                return ex;
            }
            return null;
        }
        #endregion

        #region base asynchronous result
        /// <summary>
        /// Exposes the result properties, must be inherited by a specific result implementation
        /// </summary>
        public abstract class BaseCLHttpRestResult
        {
            /// <summary>
            /// Any error which may have occurred during communication
            /// </summary>
            public CLError Error
            {
                get
                {
                    return _error;
                }
            }
            private readonly CLError _error;

            /// <summary>
            /// The status resulting from communication
            /// </summary>
            public CLHttpRestStatus Status
            {
                get
                {
                    return _status;
                }
            }
            private readonly CLHttpRestStatus _status;

            // construct with all readonly properties
            protected internal BaseCLHttpRestResult(CLError Error, CLHttpRestStatus Status)
            {
                this._error = Error;
                this._status = Status;
            }
        }

        /// <summary>
        /// Exposes the result properties, must be inherited by a specific result implementation
        /// </summary>
        public abstract class BaseCLHttpRestResult<T> : BaseCLHttpRestResult
        {
            /// <summary>
            /// The result returned from the server
            /// </summary>
            public T Result
            {
                get
                {
                    return _result;
                }
            }
            private readonly T _result;

            // construct with all readonly properties
            protected internal BaseCLHttpRestResult(CLError Error, CLHttpRestStatus Status, T Result)
                : base(Error, Status)
            {
                this._result = Result;
            }
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
            AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            BeforeDownloadToTempFile beforeDownload = null,
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
            Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, AfterDownloadToTempFile, object, int, BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>> asyncParams =
                new Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, AfterDownloadToTempFile, object, int, BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>>(
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
                Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, AfterDownloadToTempFile, object, int, BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>> castState = state as Tuple<GenericAsyncResult<DownloadFileResult>, AsyncCallback, FileChange, AfterDownloadToTempFile, object, int, BeforeDownloadToTempFile, Tuple<object, CancellationTokenSource, string>>;
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
        /// Holds result properties
        /// </summary>
        public sealed class DownloadFileResult : BaseCLHttpRestResult
        {
            // construct with all readonly properties
            internal DownloadFileResult(CLError Error, CLHttpRestStatus Status)
                : base(Error, Status) { }
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
            AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            BeforeDownloadToTempFile beforeDownload = null,
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
            AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            BeforeDownloadToTempFile beforeDownload,
            object beforeDownloadState,
            CancellationTokenSource shutdownToken,
            string customDownloadFolderFullPath,
            Action<Guid, long, SyncDirection, string, long, long, bool> statusUpdate,
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
            AfterDownloadToTempFile moveFileUponCompletion,
            object moveFileUponCompletionState,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            BeforeDownloadToTempFile beforeDownload,
            object beforeDownloadState,
            CancellationTokenSource shutdownToken,
            string customDownloadFolderFullPath,
            AsyncCallback aCallback,
            IAsyncResult aResult,
            GenericHolder<TransferProgress> progress,
            Action<Guid, long, SyncDirection, string, long, long, bool> statusUpdate,
            Nullable<Guid> statusUpdateId)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file download, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private download class upon ProcessHttp)

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
                else if (_copiedSettings.TempDownloadFolderFullPath != null)
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

                // prepare the downloadParams before the ProcessHttp because it does additional parameter checks first
                downloadParams currentDownload = new downloadParams( // this is a special communication method and requires passing download parameters
                    moveFileUponCompletion, // callback which should move the file to final location
                    moveFileUponCompletionState, // userstate for the move file callback
                    customDownloadFolderFullPath ?? // first try to use a provided custom folder full path
                        Helpers.GetTempFileDownloadPath(_copiedSettings, _syncBoxId),
                    HandleUploadDownloadStatus, // private event handler to relay status change events
                    changeToDownload, // the FileChange describing the download
                    shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                    _copiedSettings.SyncRoot, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                    aCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                    aResult, // asynchronous result to pass when firing the asynchronous callback
                    progress, // holder for progress data which can be queried by user if called via async wrapper
                    statusUpdate,
                    statusUpdateId,
                    beforeDownload, // optional callback fired before download starts
                    beforeDownloadState); // userstate passed when firing download start callback

                // run the actual communication
                ProcessHttp(
                    new Download() // JSON contract to serialize
                    {
                        StorageKey = changeToDownload.Metadata.StorageKey // storage key parameter
                    },
                    CLDefinitions.CLUploadDownloadServerURL, // server for download
                    CLDefinitions.MethodPathDownload, // download method path
                    requestMethod.post, // download is a post
                    timeoutMilliseconds, // time before communication timeout (does not restrict time
                    currentDownload, // download-specific parameters holder constructed directly above
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
        /// <param name="status">(output) success/failure status of communication</param>
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
                    MessageBox.Show("Cannot cast state as " + Helpers.GetTypeNameEvenForNulls(castState));
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
                        CLError processError = UploadFile(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            out status,
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
                                status),
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
        /// Holds result properties
        /// </summary>
        public sealed class UploadFileResult : BaseCLHttpRestResult
        {
            // construct with all readonly properties
            internal UploadFileResult(CLError Error, CLHttpRestStatus Status)
                : base(Error, Status) { }
        }

        /// <summary>
        /// Uploads a file from a provided stream and file upload change
        /// </summary>
        /// <param name="uploadStream">Stream to upload, if it is a FileStream then make sure the file is locked to prevent simultaneous writes</param>
        /// <param name="changeToUpload">File upload change, requires Metadata.HashableProperties.Size, NewPath, Metadata.StorageKey, and MD5 hash to be set</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception, does not restrict time for the actual file upload</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="shutdownToken">(optional) Token used to request cancellation of the upload</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError UploadFile(Stream uploadStream,
            FileChange changeToUpload,
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            CancellationTokenSource shutdownToken = null)
        {
            return UploadFile(
                uploadStream,
                changeToUpload,
                timeoutMilliseconds,
                out status,
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
            CancellationTokenSource shutdownToken,
            Action<Guid, long, SyncDirection, string, long, long, bool> statusUpdate,
            Guid statusUpdateId)
        {
            return UploadFile(
                uploadStream,
                changeToUpload,
                timeoutMilliseconds,
                out status,
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
            CancellationTokenSource shutdownToken,
            AsyncCallback aCallback,
            IAsyncResult aResult,
            GenericHolder<TransferProgress> progress,
            Action<Guid, long, SyncDirection, string, long, long, bool> statusUpdate,
            Nullable<Guid> statusUpdateId)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the file upload, on catch return the error
            try
            {
                // check input parameters (other checks are done on constructing the private upload class upon ProcessHttp)

                if (timeoutMilliseconds <= 0)
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    CLDefinitions.MethodPathUpload + // path to upload
                    Helpers.QueryStringBuilder(new[] // add DeviceId for file upload
                    {
                        (string.IsNullOrEmpty(_copiedSettings.DeviceId)
                            ? new KeyValuePair<string, string>()
                            :
                                // query string parameter for the device id, needs to be escaped since it's client-defined
                                new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)))
                    });

                // run the HTTP communication
                ProcessHttp(null, // the stream inside the upload parameter object is the request content, so no JSON contract object
                    CLDefinitions.CLUploadDownloadServerURL,  // Server URL
                    serverMethodPath, // dynamic upload path to add device id
                    requestMethod.put, // upload is a put
                    timeoutMilliseconds, // time before communication timeout (does not restrict time for the actual file upload)
                    new uploadParams( // this is a special communication method and requires passing upload parameters
                        uploadStream, // stream for file to upload
                        HandleUploadDownloadStatus, // private event handler to relay status change events
                        changeToUpload, // the FileChange describing the upload
                        shutdownToken, // a provided, possibly null CancellationTokenSource which can be cancelled to stop in the middle of communication
                        _copiedSettings.SyncRoot, // pass in the full path to the sync root folder which is used to calculate a relative path for firing the status change event
                        aCallback, // asynchronous callback to fire on progress changes if called via async wrapper
                        aResult, // asynchronous result to pass when firing the asynchronous callback
                        progress, // holder for progress data which can be queried by user if called via async wrapper
                        statusUpdate,
                        statusUpdateId),
                    okCreatedNotModified, // use the hashset for ok/created/not modified as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region GetMetadataAtPath
        /// <summary>
        /// Asynchronously starts querying the server at a given file or folder path (must be specified) for existing metadata at that path
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetMetadataAtPath(AsyncCallback aCallback,
            object aState,
            FilePath fullPath,
            bool isFolder,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetMetadataAtPathResult> toReturn = new GenericAsyncResult<GetMetadataAtPathResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetMetadataAtPathResult>, AsyncCallback, FilePath, bool, int> asyncParams =
                new Tuple<GenericAsyncResult<GetMetadataAtPathResult>, AsyncCallback, FilePath, bool, int>(
                    toReturn,
                    aCallback,
                    fullPath,
                    isFolder,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetMetadataAtPathResult>, AsyncCallback, FilePath, bool, int> castState = state as Tuple<GenericAsyncResult<GetMetadataAtPathResult>, AsyncCallback, FilePath, bool, int>;
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
                        JsonContracts.Metadata result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetMetadataAtPath(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetMetadataAtPathResult(
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
        public CLError EndGetMetadataAtPath(IAsyncResult aResult, out GetMetadataAtPathResult result)
        {
            // declare the specific type of asynchronous result for metadata query
            GenericAsyncResult<GetMetadataAtPathResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for metadata query and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for metadata query
                castAResult = aResult as GenericAsyncResult<GetMetadataAtPathResult>;

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
                result = Helpers.DefaultForType<GetMetadataAtPathResult>();
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
        /// Holds result properties
        /// </summary>
        public sealed class GetMetadataAtPathResult : BaseCLHttpRestResult<JsonContracts.Metadata>
        {
            // construct with all readonly properties
            internal GetMetadataAtPathResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Metadata Result)
                : base(Error, Status, Result) { }
        }

        /// <summary>
        /// Queries the server at a given file or folder path (must be specified) for existing metadata at that path; outputs CLHttpRestStatus.NoContent for status if not found on server
        /// </summary>
        /// <param name="fullPath">Full path to where file or folder would exist locally on disk</param>
        /// <param name="isFolder">Whether the query is for a folder (as opposed to a file/link)</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetMetadataAtPath(FilePath fullPath, bool isFolder, int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.Metadata response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // check input parameters

                if (fullPath == null)
                {
                    throw new NullReferenceException("fullPath cannot be null");
                }
                CLError pathError = Helpers.CheckForBadPath(fullPath);
                if (pathError != null)
                {
                    throw new AggregateException("fullPath is not in the proper format", pathError.GrabExceptions());
                }
                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }
                if (string.IsNullOrEmpty(_copiedSettings.SyncRoot))
                {
                    throw new NullReferenceException("settings SyncRoot cannot be null");
                }

                // build the location of the metadata retrieval method on the server dynamically
                string serverMethodPath =
                    (isFolder
                        ? CLDefinitions.MethodPathGetFolderMetadata // if the current metadata is for a folder, then retrieve it from the folder method
                        : CLDefinitions.MethodPathGetFileMetadata) + // else if the current metadata is for a file, then retrieve it from the file method
                    Helpers.QueryStringBuilder(new[] // both methods grab their parameters by query string (since this method is an HTTP GET)
                    {
                        // query string parameter for the path to query, built by turning the full path location into a relative path from the cloud root and then escaping the whole thing for a url
                        new KeyValuePair<string, string>(CLDefinitions.CLMetadataCloudPath, Uri.EscapeDataString(fullPath.GetRelativePath((_copiedSettings.SyncRoot ?? string.Empty), true) + "/")),

                        // query string parameter for the current sync box id, should not need escaping since it should be an integer in string format
                        new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString())
                    });

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.Metadata>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query metadata (dynamic based on file or folder)
                    requestMethod.get, // query metadata is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
            Tuple<GenericAsyncResult<GetAllPendingResult>, AsyncCallback, int> asyncParams =
                new Tuple<GenericAsyncResult<GetAllPendingResult>, AsyncCallback, int>(
                    toReturn,
                    aCallback,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetAllPendingResult>, AsyncCallback, int> castState = state as Tuple<GenericAsyncResult<GetAllPendingResult>, AsyncCallback, int>;
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
                        JsonContracts.PendingResponse result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetAllPending(
                            castState.Item3,
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
        /// Holds result properties
        /// </summary>
        public sealed class GetAllPendingResult : BaseCLHttpRestResult<JsonContracts.PendingResponse>
        {
            // construct with all readonly properties
            internal GetAllPendingResult(CLError Error, CLHttpRestStatus Status, JsonContracts.PendingResponse Result)
                : base(Error, Status, Result) { }
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
                response = ProcessHttp<JsonContracts.PendingResponse>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to get pending
                    requestMethod.get, // get pending is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
            Tuple<GenericAsyncResult<PostFileChangeResult>, AsyncCallback, FileChange, int> asyncParams =
                new Tuple<GenericAsyncResult<PostFileChangeResult>, AsyncCallback, FileChange, int>(
                    toReturn,
                    aCallback,
                    toCommunicate,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<PostFileChangeResult>, AsyncCallback, FileChange, int> castState = state as Tuple<GenericAsyncResult<PostFileChangeResult>, AsyncCallback, FileChange, int>;
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
                        JsonContracts.Event result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = PostFileChange(
                            castState.Item3,
                            castState.Item4,
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
        /// Holds result properties
        /// </summary>
        public sealed class PostFileChangeResult : BaseCLHttpRestResult<JsonContracts.Event>
        {
            // construct with all readonly properties
            internal PostFileChangeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Event Result)
                : base(Error, Status, Result) { }
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
                response = ProcessHttp<JsonContracts.Event>(requestContent, // dynamic type of request content based on method path
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // dynamic path to appropriate one-off method
                    requestMethod.post, // one-off methods are all posts
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
            Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, AsyncCallback, FileChange, int> asyncParams =
                new Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, AsyncCallback, FileChange, int>(
                    toReturn,
                    aCallback,
                    deletionChange,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, AsyncCallback, FileChange, int> castState = state as Tuple<GenericAsyncResult<UndoDeletionFileChangeResult>, AsyncCallback, FileChange, int>;
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
                        JsonContracts.Event result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = UndoDeletionFileChange(
                            castState.Item3,
                            castState.Item4,
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
        /// Holds result properties
        /// </summary>
        public sealed class UndoDeletionFileChangeResult : BaseCLHttpRestResult<JsonContracts.Event>
        {
            // construct with all readonly properties
            internal UndoDeletionFileChangeResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Event Result)
                : base(Error, Status, Result) { }
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
                response = ProcessHttp<JsonContracts.Event>(new JsonContracts.FileOrFolderUndelete() // files and folders share a request content object for undelete
                    {
                        DeviceId = _copiedSettings.DeviceId, // device id
                        ServerId = deletionChange.Metadata.ServerId, // unique id on server
                        SyncBoxId = _syncBoxId // id of sync box
                    },
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    (deletionChange.Metadata.HashableProperties.IsFolder // folder/file switch
                        ? CLDefinitions.MethodPathFolderUndelete // path for folder undelete
                        : CLDefinitions.MethodPathFileUndelete), // path for file undelete
                    requestMethod.post, // undelete file or folder is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
            Tuple<GenericAsyncResult<GetFileVersionsResult>, AsyncCallback, string, int, FilePath, bool> asyncParams =
                new Tuple<GenericAsyncResult<GetFileVersionsResult>, AsyncCallback, string, int, FilePath, bool>(
                    toReturn,
                    aCallback,
                    fileServerId,
                    timeoutMilliseconds,
                    pathToFile,
                    includeDeletedVersions);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetFileVersionsResult>, AsyncCallback, string, int, FilePath, bool> castState = state as Tuple<GenericAsyncResult<GetFileVersionsResult>, AsyncCallback, string, int, FilePath, bool>;
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
                        JsonContracts.FileVersion[] result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFileVersions(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
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
        /// Holds result properties
        /// </summary>
        public sealed class GetFileVersionsResult : BaseCLHttpRestResult<JsonContracts.FileVersion[]>
        {
            // construct with all readonly properties
            internal GetFileVersionsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FileVersion[] Result)
                : base(Error, Status, Result) { }
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
                response = ProcessHttp<JsonContracts.FileVersion[]>(null, // get file versions has no request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // use a dynamic method path because it needs query string parameters
                    requestMethod.get, // get file versions is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FileVersion[]>();
                return ex;
            }
            return null;
        }
        #endregion

        #region GetUsedBytes
        /// <summary>
        /// Asynchronously grabs the bytes used by the sync box and the bytes which are pending for upload
        /// </summary>
        /// <param name="aCallback">Callback method to fire when operation completes</param>
        /// <param name="aState">Userstate to pass when firing async callback</param>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetUsedBytes(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetUsedBytesResult> toReturn = new GenericAsyncResult<GetUsedBytesResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetUsedBytesResult>, AsyncCallback, int> asyncParams =
                new Tuple<GenericAsyncResult<GetUsedBytesResult>, AsyncCallback, int>(
                    toReturn,
                    aCallback,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetUsedBytesResult>, AsyncCallback, int> castState = state as Tuple<GenericAsyncResult<GetUsedBytesResult>, AsyncCallback, int>;
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
                        JsonContracts.UsedBytes result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetUsedBytes(
                            castState.Item3,
                            out status,
                            out result);

                        // if there was an asynchronous result in the parameters, then complete it with a new result object
                        if (castState.Item1 != null)
                        {
                            castState.Item1.Complete(
                                new GetUsedBytesResult(
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
        /// Finishes grabing the bytes used by the sync box and the bytes which are pending for upload if it has not already finished via its asynchronous result and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting grabbing the used bytes</param>
        /// <param name="result">(output) The result from grabbing the used bytes</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndGetUsedBytes(IAsyncResult aResult, out GetUsedBytesResult result)
        {
            // declare the specific type of asynchronous result for grabbing the used bytes
            GenericAsyncResult<GetUsedBytesResult> castAResult;

            // try/catch to try casting the asynchronous result as the type for grabbing the used bytes and pull the result (possibly incomplete), on catch default the output and return the error
            try
            {
                // try cast the asynchronous result as the type for grabbing the used bytes
                castAResult = aResult as GenericAsyncResult<GetUsedBytesResult>;

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
                result = Helpers.DefaultForType<GetUsedBytesResult>();
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
        /// Holds result properties
        /// </summary>
        public sealed class GetUsedBytesResult : BaseCLHttpRestResult<JsonContracts.UsedBytes>
        {
            // construct with all readonly properties
            internal GetUsedBytesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.UsedBytes Result)
                : base(Error, Status, Result) { }
        }

        /// <summary>
        /// Grabs the bytes used by the sync box and the bytes which are pending for upload
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetUsedBytes(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.UsedBytes response)
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
                if (string.IsNullOrEmpty(_copiedSettings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }

                // run the HTTP communication and store the response object to the output parameter
                response = ProcessHttp<JsonContracts.UsedBytes>(null, // getting used bytes requires no request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathGetUsedBytes + // path to get used bytes
                        Helpers.QueryStringBuilder(new[]
                        {
                            new KeyValuePair<string, string>(CLDefinitions.QueryStringDeviceId, Uri.EscapeDataString(_copiedSettings.DeviceId)), // device id, escaped since it's a user-input
                            new KeyValuePair<string, string>(CLDefinitions.QueryStringSyncBoxId, _syncBoxId.ToString()) // sync box id, not escaped since it's from an integer
                        }),
                    requestMethod.get, // getting used bytes is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.UsedBytes>();
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
            Tuple<GenericAsyncResult<CopyFileResult>, AsyncCallback, string, int, FilePath, FilePath> asyncParams =
                new Tuple<GenericAsyncResult<CopyFileResult>, AsyncCallback, string, int, FilePath, FilePath>(
                    toReturn,
                    aCallback,
                    fileServerId,
                    timeoutMilliseconds,
                    pathToFile,
                    copyTargetPath);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<CopyFileResult>, AsyncCallback, string, int, FilePath, FilePath> castState = state as Tuple<GenericAsyncResult<CopyFileResult>, AsyncCallback, string, int, FilePath, FilePath>;
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
                        JsonContracts.Event result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = CopyFile(
                            castState.Item3,
                            castState.Item4,
                            castState.Item5,
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
        /// Holds result properties
        /// </summary>
        public sealed class CopyFileResult : BaseCLHttpRestResult<JsonContracts.Event>
        {
            // construct with all readonly properties
            internal CopyFileResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Event Result)
                : base(Error, Status, Result) { }
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
                response = ProcessHttp<JsonContracts.Event>(new JsonContracts.FileCopy() // object for file copy
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
                    requestMethod.post, // file copy is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
            Tuple<GenericAsyncResult<GetPicturesResult>, AsyncCallback, int> asyncParams =
                new Tuple<GenericAsyncResult<GetPicturesResult>, AsyncCallback, int>(
                    toReturn,
                    aCallback,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetPicturesResult>, AsyncCallback, int> castState = state as Tuple<GenericAsyncResult<GetPicturesResult>, AsyncCallback, int>;
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
                        JsonContracts.Pictures result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetPictures(
                            castState.Item3,
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
        /// Holds result properties
        /// </summary>
        public sealed class GetPicturesResult : BaseCLHttpRestResult<JsonContracts.Pictures>
        {
            // construct with all readonly properties
            internal GetPicturesResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Pictures Result)
                : base(Error, Status, Result) { }
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
            // try/catch to process the metadata query, on catch return the error
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
                response = ProcessHttp<JsonContracts.Pictures>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query pictures (dynamic adding query string)
                    requestMethod.get, // query pictures is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.Pictures>();
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
            Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, AsyncCallback, int> asyncParams =
                new Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, AsyncCallback, int>(
                    toReturn,
                    aCallback,
                    timeoutMilliseconds);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, AsyncCallback, int> castState = state as Tuple<GenericAsyncResult<GetSyncBoxUsageResult>, AsyncCallback, int>;
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
                        JsonContracts.SyncBoxUsage result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetSyncBoxUsage(
                            castState.Item3,
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
        /// Holds result properties
        /// </summary>
        public sealed class GetSyncBoxUsageResult : BaseCLHttpRestResult<JsonContracts.SyncBoxUsage>
        {
            // construct with all readonly properties
            internal GetSyncBoxUsageResult(CLError Error, CLHttpRestStatus Status, JsonContracts.SyncBoxUsage Result)
                : base(Error, Status, Result) { }
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
            // try/catch to process the metadata query, on catch return the error
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
                response = ProcessHttp<JsonContracts.SyncBoxUsage>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query synx box usage (dynamic adding query string)
                    requestMethod.get, // query sync box usage is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
            Tuple<GenericAsyncResult<GetFolderHierarchyResult>, AsyncCallback, int, FilePath> asyncParams =
                new Tuple<GenericAsyncResult<GetFolderHierarchyResult>, AsyncCallback, int, FilePath>(
                    toReturn,
                    aCallback,
                    timeoutMilliseconds,
                    hierarchyRoot);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetFolderHierarchyResult>, AsyncCallback, int, FilePath> castState = state as Tuple<GenericAsyncResult<GetFolderHierarchyResult>, AsyncCallback, int, FilePath>;
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
                        JsonContracts.Folders result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFolderHierarchy(
                            castState.Item3,
                            out status,
                            out result,
                            castState.Item4);

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
        /// Holds result properties
        /// </summary>
        public sealed class GetFolderHierarchyResult : BaseCLHttpRestResult<JsonContracts.Folders>
        {
            // construct with all readonly properties
            internal GetFolderHierarchyResult(CLError Error, CLHttpRestStatus Status, JsonContracts.Folders Result)
                : base(Error, Status, Result) { }
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
            // try/catch to process the metadata query, on catch return the error
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
                response = ProcessHttp<JsonContracts.Folders>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder hierarchy (dynamic adding query string)
                    requestMethod.get, // query folder hierarchy is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
        /// <param name="contentsRoot">(optional) root path of contents query</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginGetFolderContents(AsyncCallback aCallback,
            object aState,
            int timeoutMilliseconds,
            FilePath contentsRoot = null,
            Nullable<byte> depthLimit = null,
            bool includeDeleted = false,
            bool includeCount = false)
        {
            // create the asynchronous result to return
            GenericAsyncResult<GetFolderContentsResult> toReturn = new GenericAsyncResult<GetFolderContentsResult>(
                aCallback,
                aState);

            // create a parameters object to store all the input parameters to be used on another thread with the void (object) parameterized start
            Tuple<GenericAsyncResult<GetFolderContentsResult>, AsyncCallback, int, FilePath, Nullable<byte>, bool, bool> asyncParams =
                new Tuple<GenericAsyncResult<GetFolderContentsResult>, AsyncCallback, int, FilePath, Nullable<byte>, bool, bool>(
                    toReturn,
                    aCallback,
                    timeoutMilliseconds,
                    contentsRoot,
                    depthLimit,
                    includeDeleted,
                    includeCount);

            // create the thread from a void (object) parameterized start which wraps the synchronous method call
            (new Thread(new ParameterizedThreadStart(state =>
            {
                // try cast the state as the object with all the input parameters
                Tuple<GenericAsyncResult<GetFolderContentsResult>, AsyncCallback, int, FilePath, Nullable<byte>, bool, bool> castState = state as Tuple<GenericAsyncResult<GetFolderContentsResult>, AsyncCallback, int, FilePath, Nullable<byte>, bool, bool>;
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
                        JsonContracts.FolderContents result;
                        // run the download of the file with the passed parameters, storing any error that occurs
                        CLError processError = GetFolderContents(
                            castState.Item3,
                            out status,
                            out result,
                            castState.Item4,
                            castState.Item5,
                            castState.Item6,
                            castState.Item7);

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
        /// Holds result properties
        /// </summary>
        public sealed class GetFolderContentsResult : BaseCLHttpRestResult<JsonContracts.FolderContents>
        {
            // construct with all readonly properties
            internal GetFolderContentsResult(CLError Error, CLHttpRestStatus Status, JsonContracts.FolderContents Result)
                : base(Error, Status, Result) { }
        }

        /// <summary>
        /// Queries server for folder contents with an optional path and an optional depth limit
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="contentsRoot">(optional) root path of hierarchy query</param>
        /// <param name="depthLimit">(optional) maximum levels deep to query under contents root, leave at default (null) to not limit depth</param>
        /// <param name="includeDeleted">(optional) whether to include deleted files or folders in the search contents</param>
        /// <param name="includeCount">(optional) whether to include counts of items and deleted items in each folder</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError GetFolderContents(
            int timeoutMilliseconds,
            out CLHttpRestStatus status,
            out JsonContracts.FolderContents response,
            FilePath contentsRoot = null,
            Nullable<byte> depthLimit = null,
            bool includeDeleted = false,
            bool includeCount = false)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
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
                response = ProcessHttp<JsonContracts.FolderContents>(
                    null, // HTTP Get method does not have content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    serverMethodPath, // path to query folder contents (dynamic adding query string)
                    requestMethod.get, // query folder contents is a get
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.FolderContents>();
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
                response = ProcessHttp<JsonContracts.To>(
                    syncToRequest, // object for request content
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathSyncTo, // path to sync to
                    requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
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
                response = ProcessHttp<JsonContracts.PushResponse>(
                    pushRequest, // object to write as request content to the server
                    CLDefinitions.CLMetaDataServerURL, // base domain is the MDS server
                    CLDefinitions.MethodPathSyncFrom, // path to sync from
                    requestMethod.post, // sync_to is a post
                    timeoutMilliseconds, // time before communication timeout
                    null, // not an upload or download
                    okAccepted, // use the hashset for ok/accepted as successful HttpStatusCodes
                    ref status); // reference to update the output success/failure status for the communication
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PushResponse>();
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Purges any pending changes for the provided user/device combination in the request object (pending file uploads) and outputs the files which were purged
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        internal CLError PurgePending(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.PendingResponse response)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
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

                response = ProcessHttp<JsonContracts.PendingResponse>(new JsonContracts.PurgePending() // json contract object for purge pending method
                    {
                        DeviceId = _copiedSettings.DeviceId,
                        SyncBoxId = _syncBoxId
                    },
                    CLDefinitions.CLMetaDataServerURL, CLDefinitions.MethodPathPurgePending, // purge pending address
                    requestMethod.post, // purge pending is a post operation
                    timeoutMilliseconds, // set the timeout for the operation
                    null, // not an upload or download
                    okAccepted, // purge pending should give OK or Accepted
                    ref status); // reference to update output status
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.PendingResponse>();
                return ex;
            }
            return null;
        }
        #endregion

        #region private helpers
        // event handler fired upon transfer buffer clears for uploads/downloads to relay to the global event
        private void HandleUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource)
        {
            // validate parameter which can throw an exception in this method

            if (eventSource == null)
            {
                throw new NullReferenceException("eventSource cannot be null");
            }

            // direction of communication determines which event to fire
            if (eventSource.Direction == SyncDirection.To)
            {
                MessageEvents.UpdateFileUpload(
                    sender: eventSource, // source of the event (the event itself)
                    eventId: eventSource.EventId, // the id for the event
                    parameters: status, // the event arguments describing the status change
                    SyncBoxId: this._syncBoxId,
                    DeviceId: this._copiedSettings.DeviceId);
            }
            else
            {
                MessageEvents.UpdateFileDownload(
                    sender: eventSource, // source of the event (the event itself)
                    eventId: eventSource.EventId, // the id for the event
                    parameters: status,  // the event arguments describing the status change
                    SyncBoxId: this._syncBoxId,
                    DeviceId: this._copiedSettings.DeviceId);
            }
        }

        // forwards to the main HTTP REST routine helper method which processes the actual communication, but only where the return type is object
        private object ProcessHttp(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status) // reference to the successful/failed state of communication
        {
            return ProcessHttp<object>(requestContent,
                serverUrl,
                serverMethodPath,
                method,
                timeoutMilliseconds,
                uploadDownload,
                validStatusCodes,
                ref status);
        }

        // main HTTP REST routine helper method which processes the actual communication
        // T should be the type of the JSON contract object which an be deserialized from the return response of the server if any, otherwise use string/object type which will be filled in as the entire string response
        private T ProcessHttp<T>(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status) // reference to the successful/failed state of communication
            where T : class // restrict T to an object type to allow default null return
        {
            // create the main request object for the provided uri location
            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(serverUrl + serverMethodPath);

            #region set request parameters
            // switch case to set the HTTP method (GET vs. POST vs. PUT); throw exception if not supported yet
            switch (method)
            {
                case requestMethod.get:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodGet;
                    break;
                case requestMethod.post:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                    break;
                case requestMethod.put:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodPut;
                    break;

                default:
                    throw new ArgumentException("Unknown method: " + method.ToString());
            }

            // set more request parameters

            httpRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient; // set client
            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            httpRequest.Headers[CLDefinitions.CLClientVersionHeaderName] = _copiedSettings.ClientVersion; // set client version
            httpRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendCWS0 +
                                CLDefinitions.HeaderAppendKey +
                                _credentials.ApplicationKey + ", " +
                                CLDefinitions.HeaderAppendSignature +
                                        Helpers.GenerateAuthorizationHeaderToken(
                                            _credentials.ApplicationSecret,
                                            httpMethod: httpRequest.Method,
                                            pathAndQueryStringAndFragment: serverMethodPath);   // set the authentication token
            httpRequest.SendChunked = false; // do not send chunked
            httpRequest.Timeout = timeoutMilliseconds; // set timeout by input parameter, timeout does not apply to the amount of time it takes to perform uploading or downloading of a file

            // declare the bytes for the serialized request body content
            byte[] requestContentBytes;

            // for any communication which is not a file upload, determine the bytes which will be sent up in the request
            if (uploadDownload == null ||
                !(uploadDownload is uploadParams))
            {
                // if there is no content for the request (such as for an HTTP Get method call), then set the bytes as null
                if (requestContent == null)
                {
                    requestContentBytes = null;
                }
                // else if there is content for the request, then serialize the requestContent object and store the bytes to send up
                else
                {
                    // declare a string for the request body content
                    string requestString;
                    // create a stream for serializing the request object
                    using (MemoryStream requestMemory = new MemoryStream())
                    {
                        // serialize the request object into the stream with the appropriate serializer based on the input type, and if the type is not supported then throw an exception

                        Type requestType = requestContent.GetType();
                        DataContractJsonSerializer getRequestSerializer;
                        if (!SerializableRequestTypes.TryGetValue(requestType, out getRequestSerializer))
                        {
                            throw new ArgumentException("Unknown requestContent Type: " + requestType.FullName);
                        }

                        getRequestSerializer.WriteObject(requestMemory, requestContent);

                        // grab the string from the serialized data
                        requestString = Encoding.Default.GetString(requestMemory.ToArray());
                    }

                    // grab the bytes for the serialized request body content
                    requestContentBytes = Encoding.UTF8.GetBytes(requestString);

                    // configure request parameters based on a json request body content

                    httpRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson; // the request body content is json-formatted
                    httpRequest.ContentLength = requestContentBytes.LongLength; // set the size of the request content
                    httpRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding; // the json content is utf8 encoded
                }
            }
            // else if communication is for a file upload, then set the appropriate request parameters
            else
            {
                httpRequest.ContentType = CLDefinitions.HeaderAppendContentTypeBinary; // content will be direct binary stream
                httpRequest.ContentLength = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // content length will be file size
                httpRequest.Headers[CLDefinitions.HeaderAppendStorageKey] = uploadDownload.ChangeToTransfer.Metadata.StorageKey; // add header for destination location of file
                httpRequest.Headers[CLDefinitions.HeaderAppendContentMD5] = ((uploadParams)uploadDownload).Hash; // set MD5 content hash for verification of upload stream
                httpRequest.KeepAlive = true; // do not close connection (is this needed?)
                requestContentBytes = null; // do not write content bytes since they will come from the Stream inside the upload object
            }
            #endregion

            #region trace request
            // if communication is supposed to be traced, then trace it
            if ((_copiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
            {
                // trace communication for the current request
                ComTrace.LogCommunication(_copiedSettings.TraceLocation, // location of trace file
                    _copiedSettings.DeviceId, // device id
                    _syncBoxId, // user id
                    CommunicationEntryDirection.Request, // direction is request
                    serverUrl + serverMethodPath, // location for the server method
                    true, // trace is enabled
                    httpRequest.Headers, // headers of request
                    ((uploadDownload != null && uploadDownload is uploadParams) // special condition for the request body content based on whether this is a file upload or not
                        ? "---File upload started---" // truncate the request body content to a predefined string so that the entire uploaded file is not written as content
                        : (requestContentBytes == null // condition on whether there were bytes to write in the request content body
                            ? null // if there were no bytes to write in the request content body, then log for none
                            : Encoding.UTF8.GetString(requestContentBytes))), // if there were no bytes to write in the request content body, then log them (in string form)
                    null, // no status code for requests
                    _copiedSettings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                    httpRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                    ((requestContentBytes != null || (uploadDownload != null && uploadDownload is uploadParams))
                        ? httpRequest.ContentLength.ToString() // if the communication had bytes to upload from an input object or a stream to upload for a file, then set the content length value which would be part of the headers (but cannot be pulled from headers directly)
                        : null), // else if the communication would not have any request content, then log no content length header
                    (httpRequest.Expect == null ? "100-continue" : httpRequest.Expect), // expect value which would be part of the headers (but cannot be pulled from headers directly)
                    (httpRequest.KeepAlive ? "Keep-Alive" : "Close")); // keep-alive value which would be part of the headers (but cannot be pulled from headers directly)
            }
            #endregion

            // status setup is for file uploads and downloads which fire event callbacks to fire global status events
            #region status setup
            // define size to be used for status update event callbacks
            long storeSizeForStatus;
            // declare the time when the transfer started (inaccurate for file downloads since the time is set before the request for the download and not before the download actually starts)
            DateTime transferStartTime;

            // if this communiction is not for a file upload or download, then the status parameters won't be used and can be set as nothing
            if (uploadDownload == null)
            {
                storeSizeForStatus = 0;
                transferStartTime = DateTime.MinValue;
            }
            // else if this communication is for a file upload or download, then set the status event parameters
            else
            {
                // check to make sure this is in fact an upload or download
                if (!(uploadDownload is uploadParams)
                    && !(uploadDownload is downloadParams))
                {
                    throw new ArgumentException("uploadDownload must be either upload or download");
                }

                // set the status event parameters

                storeSizeForStatus = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // pull size from the change to transfer
                transferStartTime = DateTime.Now; // use the current local time as transfer start time
            }
            #endregion

            #region write request
            // if this communication is for a file upload or download, then process its request accordingly
            if (uploadDownload != null)
            {
                // get the request stream
                Stream httpRequestStream = null;

                // try/finally process the upload request (which actually uploads the file) or download request, finally dispose the request stream if it was set
                try
                {
                    // if the current communication is file upload, then upload the file
                    if (uploadDownload is uploadParams)
                    {
                        if (uploadDownload.StatusUpdate != null
                            && uploadDownload.StatusUpdateId != null)
                        {
                            try
                            {
                                uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                    uploadDownload.ChangeToTransfer.EventId,
                                    uploadDownload.ChangeToTransfer.Direction,
                                    uploadDownload.RelativePathForStatus,
                                    0,
                                    (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                    false);
                            }
                            catch
                            {
                            }
                        }

                        // grab the upload request stream asynchronously since it can take longer than the provided timeout milliseconds
                        httpRequestStream = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, upload: true) as Stream;

                        // if there was no request stream retrieved, then the request was cancelled so return cancelled
                        if (httpRequestStream == null)
                        {
                            status = CLHttpRestStatus.Cancelled;
                            return null;
                        }

                        // define a transfer buffer between the file and the upload stream
                        byte[] uploadBuffer = new byte[FileConstants.BufferSize];

                        // declare a count of the bytes read in each buffer read from the file
                        int bytesRead;
                        // define a count for the total amount of bytes uploaded so far
                        long totalBytesUploaded = 0;

                        if (uploadDownload.ProgressHolder != null)
                        {
                            lock (uploadDownload.ProgressHolder)
                            {
                                uploadDownload.ProgressHolder.Value = new TransferProgress(
                                    0,
                                    storeSizeForStatus);
                            }
                        }

                        if (uploadDownload.ACallback != null)
                        {
                            uploadDownload.ACallback(uploadDownload.AResult);
                        }

                        // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the file and store the read byte count
                        while ((bytesRead = ((uploadParams)uploadDownload).Stream.Read(uploadBuffer, 0, uploadBuffer.Length)) != 0)
                        {
                            // write the buffer from the file to the upload stream
                            httpRequestStream.Write(uploadBuffer, 0, bytesRead);
                            // add the number of bytes read on the current buffer transfer to the total bytes uploaded
                            totalBytesUploaded += bytesRead;

                            // check for sync shutdown
                            if (uploadDownload.ShutdownToken != null)
                            {
                                Monitor.Enter(uploadDownload.ShutdownToken);
                                try
                                {
                                    if (uploadDownload.ShutdownToken.Token.IsCancellationRequested)
                                    {
                                        status = CLHttpRestStatus.Cancelled;
                                        return null;
                                    }
                                }
                                finally
                                {
                                    Monitor.Exit(uploadDownload.ShutdownToken);
                                }
                            }

                            if (uploadDownload.ProgressHolder != null)
                            {
                                lock (uploadDownload.ProgressHolder)
                                {
                                    uploadDownload.ProgressHolder.Value = new TransferProgress(
                                        totalBytesUploaded,
                                        storeSizeForStatus);
                                }
                            }

                            if (uploadDownload.ACallback != null)
                            {
                                uploadDownload.ACallback(uploadDownload.AResult);
                            }

                            // fire event callbacks for status change on uploading
                            uploadDownload.StatusCallback(new CLStatusFileTransferUpdateParameters(
                                    transferStartTime, // time of upload start
                                    storeSizeForStatus, // total size of file
                                    uploadDownload.RelativePathForStatus, // relative path of file
                                    totalBytesUploaded), // bytes uploaded so far
                                uploadDownload.ChangeToTransfer); // the source of the event (the event itself)

                            if (uploadDownload.StatusUpdate != null
                                && uploadDownload.StatusUpdateId != null)
                            {
                                try
                                {
                                    uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                        uploadDownload.ChangeToTransfer.EventId,
                                        uploadDownload.ChangeToTransfer.Direction,
                                        uploadDownload.RelativePathForStatus,
                                        totalBytesUploaded,
                                        (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                        false);
                                }
                                catch
                                {
                                }
                            }
                        }

                        // upload is finished so stream can be disposed
                        ((uploadParams)uploadDownload).DisposeStream();
                    }
                    // else if the communication is a file download, write the request stream content from the serialized download request object
                    else
                    {
                        // grab the request stream for writing
                        httpRequestStream = httpRequest.GetRequestStream();

                        // write the request for the download
                        httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                    }
                }
                finally
                {
                    // dispose the request stream if it was set
                    if (httpRequestStream != null)
                    {
                        try
                        {
                            httpRequestStream.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            // else if the communication is neither an upload nor download and there is a serialized request object to write, then get the request stream and write to it
            else if (requestContentBytes != null)
            {
                using (Stream httpRequestStream = httpRequest.GetRequestStream())
                {
                    httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                }
            }
            #endregion

            // define the web response outside the regions "get response" and "process response stream" so it can finally be closed (if it ever gets set); also for trace
            HttpWebResponse httpResponse = null; // communication response
            string responseBody = null; // string body content of response (for a string output is used instead of the response stream itself)
            Stream responseStream = null; // response stream (when the communication output is a deserialized object instead of a simple string representation)
            Stream serializationStream = null; // a possible copy of the response stream for when the stream has to be used both for trace and for deserializing a return object

            // try/catch/finally get the response and process its stream for output,
            // on error send a final status event if communication is for upload or download,
            // finally possibly trace if a string response was used and dispose any response/response streams
            try
            {
                #region get response
                // if the communication is a download, then grab the download response asynchronously so its time is not limited to the timeout milliseconds
                if (uploadDownload != null
                    && uploadDownload is downloadParams)
                {
                    // grab the download response asynchronously so its time is not limited to the timeout milliseconds
                    httpResponse = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, false) as HttpWebResponse;

                    // if there was no download response, then it was cancelled so return as such
                    if (httpRequest == null)
                    {
                        status = CLHttpRestStatus.Cancelled;
                        return null;
                    }
                }
                // else if the communication is not a download, then grab the response
                else
                {
                    // try/catch grab the communication response, on catch try to pull the response from the exception otherwise rethrow the exception
                    try
                    {
                        httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response == null)
                        {
                            throw new NullReferenceException(String.Format("httpResponse GetResponse at URL {0}, MethodPath {1}",
                                        (serverUrl ?? "{missing serverUrl}"),
                                        (serverMethodPath ?? "{missing serverMethodPath}"))
                                        + " threw a WebException without a WebResponse");
                        }

                        httpResponse = (HttpWebResponse)ex.Response;
                    }
                }

                // if the status code of the response is not in the provided HashSet of those which represent success,
                // then try to provide a more specific return status and try to pull the content from the response as a string and throw an exception for invalid status code
                if (!validStatusCodes.Contains(httpResponse.StatusCode))
                {
                    // if response status code is a not found, then set the output status accordingly
                    if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        status = CLHttpRestStatus.NotFound;
                    }
                    // else if response status was not a not found and is a no content, then set the output status accordingly
                    else if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                    {
                        status = CLHttpRestStatus.NoContent;
                    }
                    // else if the response status was neither a not found nor a no content and is an unauthorized, then set the output state accordingly
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        status = CLHttpRestStatus.NotAuthorized;
                    }
                    // else if response status was neither a not found nor a no content and is within the range of a server error (5XX), then set the output status accordingly
                    else if (((HttpStatusCode)(((int)httpResponse.StatusCode) - (((int)httpResponse.StatusCode) % 100))) == HttpStatusCode.InternalServerError)
                    {
                        status = CLHttpRestStatus.ServerError;
                    }

                    // try/catch to set the response body from the content of the response, on catch silence the error
                    try
                    {
                        // grab the response stream
                        using (Stream downloadResponseStream = httpResponse.GetResponseStream())
                        {
                            // read the response as UTF8 text
                            using (StreamReader downloadResponseStreamReader = new StreamReader(downloadResponseStream, Encoding.UTF8))
                            {
                                // set the response text
                                responseBody = downloadResponseStreamReader.ReadToEnd();
                            }
                        }
                    }
                    catch
                    {
                    }

                    // throw the exception for an invalid response
                    throw new Exception(String.Format("Invalid HTTP response status code at URL {0}, MethodPath {1}",
                                    (serverUrl ?? "{missing serverUrl"),
                                    (serverMethodPath ?? "{missing serverMethodPath")) +
                                    ": " + ((int)httpResponse.StatusCode).ToString() +
                                    (responseBody == null ? string.Empty
                                        : Environment.NewLine + "Response:" + Environment.NewLine +
                                            responseBody)); // either the default "incomplete" body or the body retrieved from the response content
                }
                #endregion

                #region process response stream
                // define an object for the communication return, defaulting to null
                T toReturn = null;

                // if the communication was an upload or a download, then process the response stream for a download (which is the download itself) or use a predefined return for an upload
                if (uploadDownload != null)
                {
                    // if communication is an upload, then use a predefined return
                    if (uploadDownload is uploadParams)
                    {
                        // set body as successful value
                        responseBody = "---File upload complete---";

                        // if we can use a string output for the return, then use it
                        if (typeof(T) == typeof(string)
                            || typeof(T) == typeof(object))
                        {
                            toReturn = (T)((object)responseBody);
                        }
                    }
                    // else if communication is a download, then process the actual download itself
                    else
                    {
                        // set the response body to a value that will be displayed if the actual response fails to process
                        responseBody = "---Incomplete file download---";

                        if (uploadDownload.StatusUpdate != null
                            && uploadDownload.StatusUpdateId != null)
                        {
                            try
                            {
                                uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                    uploadDownload.ChangeToTransfer.EventId,
                                    uploadDownload.ChangeToTransfer.Direction,
                                    uploadDownload.RelativePathForStatus,
                                    0,
                                    (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                    false);
                            }
                            catch
                            {
                            }
                        }

                        // create a new unique id for the download
                        Guid newTempFile = Guid.NewGuid();

                        // if a callback was provided to fire before a download starts, then fire it
                        if (((downloadParams)uploadDownload).BeforeDownloadCallback != null)
                        {
                            ((downloadParams)uploadDownload).BeforeDownloadCallback(newTempFile, ((downloadParams)uploadDownload).BeforeDownloadUserState);
                        }

                        // calculate location for downloading the file
                        string newTempFileString = ((downloadParams)uploadDownload).TempDownloadFolderPath + "\\" + ((Guid)newTempFile).ToString("N");

                        if (uploadDownload.ProgressHolder != null)
                        {
                            lock (uploadDownload.ProgressHolder)
                            {
                                uploadDownload.ProgressHolder.Value = new TransferProgress(
                                    0,
                                    storeSizeForStatus);
                            }
                        }

                        if (uploadDownload.ACallback != null)
                        {
                            uploadDownload.ACallback(uploadDownload.AResult);
                        }

                        // get the stream of the download
                        using (Stream downloadResponseStream = httpResponse.GetResponseStream())
                        {
                            // create a stream by creating a non-shared writable file at the file path
                            using (FileStream tempFileStream = new FileStream(newTempFileString, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                // define a count for the total bytes downloaded
                                long totalBytesDownloaded = 0;
                                // create the buffer for transferring bytes from the download stream to the file stream
                                byte[] data = new byte[CLDefinitions.SyncConstantsResponseBufferSize];
                                // declare an int for the amount of bytes read in each buffer transfer
                                int read;
                                // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the download stream and store the read byte count
                                while ((read = downloadResponseStream.Read(data, 0, data.Length)) > 0)
                                {
                                    // write the current buffer to the file
                                    tempFileStream.Write(data, 0, read);
                                    // append the count of the read bytes on this buffer transfer to the total downloaded
                                    totalBytesDownloaded += read;

                                    // check for sync shutdown
                                    if (uploadDownload.ShutdownToken != null)
                                    {
                                        Monitor.Enter(uploadDownload.ShutdownToken);
                                        try
                                        {
                                            if (uploadDownload.ShutdownToken.Token.IsCancellationRequested)
                                            {
                                                status = CLHttpRestStatus.Cancelled;
                                                return null;
                                            }
                                        }
                                        finally
                                        {
                                            Monitor.Exit(uploadDownload.ShutdownToken);
                                        }
                                    }

                                    if (uploadDownload.ProgressHolder != null)
                                    {
                                        lock (uploadDownload.ProgressHolder)
                                        {
                                            uploadDownload.ProgressHolder.Value = new TransferProgress(
                                                totalBytesDownloaded,
                                                storeSizeForStatus);
                                        }
                                    }

                                    if (uploadDownload.ACallback != null)
                                    {
                                        uploadDownload.ACallback(uploadDownload.AResult);
                                    }

                                    if (uploadDownload.StatusUpdate != null
                                        && uploadDownload.StatusUpdateId != null)
                                    {
                                        try
                                        {
                                            uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                                uploadDownload.ChangeToTransfer.EventId,
                                                uploadDownload.ChangeToTransfer.Direction,
                                                uploadDownload.RelativePathForStatus,
                                                totalBytesDownloaded,
                                                (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                                false);
                                        }
                                        catch
                                        {
                                        }
                                    }

                                    // fire event callbacks for status change on uploading
                                    uploadDownload.StatusCallback(
                                        new CLStatusFileTransferUpdateParameters(
                                                transferStartTime, // start time for download
                                                storeSizeForStatus, // total file size
                                                uploadDownload.RelativePathForStatus, // relative path of file
                                                totalBytesDownloaded), // current count of completed download bytes
                                        uploadDownload.ChangeToTransfer); // the source of the event, the event itself
                                }
                                // flush file stream to finish the file
                                tempFileStream.Flush();
                            }
                        }

                        // set the file attributes so when the file move triggers a change in the event source its metadata should match the current event;
                        // also, perform each attribute change with up to 4 retries since it seems to throw errors under normal conditions (if it still fails then it rethrows the exception);
                        // attributes to set: creation time, last modified time, and last access time

                        Helpers.RunActionWithRetries(() => System.IO.File.SetCreationTimeUtc(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.CreationTime), true);
                        Helpers.RunActionWithRetries(() => System.IO.File.SetLastAccessTimeUtc(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.LastTime), true);
                        Helpers.RunActionWithRetries(() => System.IO.File.SetLastWriteTimeUtc(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.LastTime), true);


                        // fire callback to perform the actual move of the temp file to the final destination
                        ((downloadParams)uploadDownload).AfterDownloadCallback(newTempFileString, // location of temp file
                            uploadDownload.ChangeToTransfer,
                            ref responseBody, // reference to response string (sets to "---Completed file download---" on success)
                            ((downloadParams)uploadDownload).AfterDownloadUserState, // timer for failure queue
                            newTempFile); // id for the downloaded file

                        // if the after downloading callback set the response to null, then replace it saying it was null
                        if (responseBody == null)
                        {
                            responseBody = "---responseBody set to null---";
                        }

                        // if a string can be output as the return type, then return the response (which is not the actual download, but a simple string status representation)
                        if (typeof(T) == typeof(string)
                            || typeof(T) == typeof(object))
                        {
                            toReturn = (T)((object)responseBody);
                        }
                    }
                }
                // else if the communication was neither an upload nor a download, then process the response stream for return
                else
                {
                    // declare the serializer which will be used to deserialize the response content for output
                    DataContractJsonSerializer outSerializer;
                    // try to get the serializer for the output by the type of output from dictionary and if successful, process response content as stream to deserialize
                    if (SerializableResponseTypes.TryGetValue(typeof(T), out outSerializer))
                    {
                        // grab the stream for response content
                        responseStream = httpResponse.GetResponseStream();

                        // set the stream for processing the response by a copy of the communication stream (if trace enabled) or the communication stream itself (if trace is not enabled)
                        serializationStream = (((_copiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                            ? Helpers.CopyHttpWebResponseStreamAndClose(responseStream) // if trace is enabled, then copy the communications stream to a memory stream
                            : responseStream); // if trace is not enabled, use the communication stream

                        // if tracing communication, then trace communication
                        if ((_copiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            // log communication for stream body
                            ComTrace.LogCommunication(_copiedSettings.TraceLocation, // trace file location
                                _copiedSettings.DeviceId, // device id
                                _syncBoxId, // user id
                                CommunicationEntryDirection.Response, // communication direction is response
                                serverUrl + serverMethodPath, // input parameter method path
                                true, // trace is enabled
                                httpResponse.Headers, // response headers
                                serializationStream, // copied response stream
                                (int)httpResponse.StatusCode, // status code of the response
                                _copiedSettings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                        }

                        // deserialize the response content into the appropriate json contract object
                        toReturn = (T)outSerializer.ReadObject(serializationStream);
                    }
                    // else if the output type is not in the dictionary of those serializable and if the output type is either object or string,
                    // then process the response content as a string to output directly
                    else if (typeof(T) == typeof(string)
                        || (typeof(T) == typeof(object)))
                    {
                        // grab the stream from the response content
                        responseStream = httpResponse.GetResponseStream();

                        // create a reader for the response content
                        using (TextReader purgeResponseStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            // set the error string from the response
                            toReturn = (T)((object)purgeResponseStreamReader.ReadToEnd());
                        }
                    }
                    // else if the output type is not in the dictionary of those serializable and if the output type is also neither object nor string,
                    // then throw an argument exception
                    else
                    {
                        throw new ArgumentException("T is not a serializable output type nor object/string");
                    }
                }

                // if the code has not thrown an exception by now then it was successful so mark it so in the output
                status = CLHttpRestStatus.Success;
                // return any object set to return for the response, if any
                return toReturn;
                #endregion
            }
            catch
            {
                // if there was an event for the upload or download, then fire the event callback for a final transfer status
                if (uploadDownload != null
                    && (uploadDownload is uploadParams
                        || uploadDownload is downloadParams))
                {
                    // try/catch fire the event callback for final transfer status, silencing errors
                    try
                    {
                        uploadDownload.StatusCallback(
                            new CLStatusFileTransferUpdateParameters(
                                transferStartTime, // retrieve the upload start time

                                // need to send a file size which matches the total uploaded bytes so they are equal to cancel the status
                                uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,

                                // try to build the same relative path that would be used in the normal status, falling back first to the full path then to an empty string
                                uploadDownload.RelativePathForStatus,

                                // need to send a total uploaded bytes which matches the file size so they are equal to cancel the status
                                uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0),
                            uploadDownload.ChangeToTransfer); // sender of event (the event itself)
                    }
                    catch
                    {
                    }

                    if (uploadDownload.StatusUpdate != null
                        && uploadDownload.StatusUpdateId != null)
                    {
                        try
                        {
                            uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                uploadDownload.ChangeToTransfer.EventId,
                                uploadDownload.ChangeToTransfer.Direction,
                                uploadDownload.RelativePathForStatus,
                                uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,
                                uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,
                                false);
                        }
                        catch
                        {
                        }
                    }
                }

                // rethrow
                throw;
            }
            finally
            {
                // for communication logging, log communication if it hasn't already been logged in stream deserialization or dispose the serialization stream
                if ((_copiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    // if there was no stream set for deserialization, then the response was handled as a string and needs to be logged here as such
                    if (serializationStream == null)
                    {
                        if (httpResponse != null)
                        {
                            // log communication for string body
                            ComTrace.LogCommunication(_copiedSettings.TraceLocation, // trace file location
                                _copiedSettings.DeviceId, // device id
                                _syncBoxId, // user id
                                CommunicationEntryDirection.Response, // communication direction is response
                                serverUrl + serverMethodPath, // input parameter method path
                                true, // trace is enabled
                                httpResponse.Headers, // response headers
                                responseBody, // response body (either an overridden string that says "complete" or "incomplete" or an error message from the actual response)
                                (int)httpResponse.StatusCode, // status code of the response
                                _copiedSettings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                        }
                    }
                    // else if there was a stream set for deserialization then the response was already logged, but it still needs to be disposed here
                    else if (serializationStream != null)
                    {
                        try
                        {
                            serializationStream.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }

                // if there was a response stream retrieved then try to dispose it
                if (responseStream != null)
                {
                    try
                    {
                        responseStream.Dispose();
                    }
                    catch
                    {
                    }
                }

                // if there was a response retrieved then try to close it
                if (httpResponse != null)
                {
                    try
                    {
                        httpResponse.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }

        // a dual-function wrapper for making asynchronous calls for either retrieving an upload request stream or retrieving a download response
        private static object AsyncGetUploadRequestStreamOrDownloadResponse(CancellationTokenSource shutdownToken, HttpWebRequest httpRequest, bool upload)
        {
            // declare the output object which would be either a Stream for upload request or an HttpWebResponse for a download response
            object toReturn;

            // create new async holder used to make async http calls synchronous
            AsyncRequestHolder requestOrResponseHolder = new AsyncRequestHolder(shutdownToken);

            // declare result from async http call
            IAsyncResult requestOrResponseAsyncResult;

            // lock on async holder for modification
            lock (requestOrResponseHolder)
            {
                // create a callback which handles the IAsyncResult style used in wrapping an asyncronous method to make it synchronous
                AsyncCallback requestOrResponseCallback = new AsyncCallback(MakeAsyncRequestSynchronous);

                // if this helper was called for an upload, then the action is for the request stream
                if (upload)
                {
                    // begin getting the upload request stream asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetRequestStream(requestOrResponseCallback, requestOrResponseHolder);
                }
                // else if this helper was called for a download, then the action is for the response
                else
                {
                    // begin getting the download response asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetResponse(requestOrResponseCallback, requestOrResponseHolder);
                }

                // if the request was not already completed synchronously, wait on it to complete
                if (!requestOrResponseHolder.CompletedSynchronously)
                {
                    // wait on the request to become synchronous again
                    Monitor.Wait(requestOrResponseHolder);
                }
            }

            // if there was an error that occurred on the async http call, then rethrow the error
            if (requestOrResponseHolder.Error != null)
            {
                throw requestOrResponseHolder.Error;
            }

            // if the http call was cancelled, then return immediately with default
            if (requestOrResponseHolder.IsCanceled)
            {
                return null;
            }

            // if this helper was called for an upload, then the action is for the request stream
            if (upload)
            {
                toReturn = httpRequest.EndGetRequestStream(requestOrResponseAsyncResult);
            }
            // else if this helper was called for a download, then the action is for the response
            else
            {
                // try/catch to retrieve the response and on catch try to pull the response from the exception otherwise rethrow the exception
                try
                {
                    toReturn = httpRequest.EndGetResponse(requestOrResponseAsyncResult);
                }
                catch (WebException ex)
                {
                    if (ex.Response == null)
                    {
                        throw new NullReferenceException("Download httpRequest EndGetResponse threw a WebException without a WebResponse", ex);
                    }

                    toReturn = ex.Response;
                }
            }

            // output the retrieved request stream or the retrieved response
            return toReturn;
        }

        /// <summary>
        /// Async HTTP operation holder used to help make async calls synchronous
        /// </summary>
        private sealed class AsyncRequestHolder
        {
            /// <summary>
            /// Whether IAsyncResult was found to be CompletedSynchronously: if so, do not Monitor.Wait
            /// </summary>
            public bool CompletedSynchronously
            {
                get
                {
                    return _completedSynchronously;
                }
            }
            /// <summary>
            /// Mark this when IAsyncResult was found to be CompletedSynchronously
            /// </summary>
            public void MarkCompletedSynchronously()
            {
                _completedSynchronously = true;
            }
            // storage for CompletedSynchronously, only marked when true so default to false
            private bool _completedSynchronously = false;

            /// <summary>
            /// cancelation token to check between async calls to cancel out of the operation
            /// </summary>
            public CancellationTokenSource FullShutdownToken
            {
                get
                {
                    return _fullShutdownToken;
                }
            }
            private readonly CancellationTokenSource _fullShutdownToken;

            /// <summary>
            /// Constructor for the async HTTP operation holder
            /// </summary>
            /// <param name="FullShutdownToken">Token to check for cancelation upon async calls</param>
            public AsyncRequestHolder(CancellationTokenSource FullShutdownToken)
            {
                // store the cancellation token
                this._fullShutdownToken = FullShutdownToken;
            }

            /// <summary>
            /// Whether the current async HTTP operation holder detected cancellation
            /// </summary>
            public bool IsCanceled
            {
                get
                {
                    return _isCanceled;
                }
            }
            // storage for cancellation
            private bool _isCanceled = false;

            /// <summary>
            /// Marks the current async HTTP operation holder as cancelled
            /// </summary>
            public void Cancel()
            {
                _isCanceled = true;
            }

            /// <summary>
            /// Any error that happened during current async HTTP operation
            /// </summary>
            public Exception Error
            {
                get
                {
                    return _error;
                }
            }
            // storage for any error that occurs
            private Exception _error = null;

            /// <summary>
            /// Marks the current async HTTP operation holder with any error that occurs
            /// </summary>
            /// <param name="toMark"></param>
            public void MarkException(Exception toMark)
            {
                // null coallesce the exception with a new exception that the exception was null
                _error = toMark ?? new NullReferenceException("toMark is null");
                // lock on this current async HTTP operation holder for pulsing waiters
                lock (this)
                {
                    Monitor.Pulse(this);
                }
            }
        }

        // Method to make async HTTP operations synchronous which can be ; requires passing an AsyncRequestHolder as the userstate
        private static void MakeAsyncRequestSynchronous(IAsyncResult makeSynchronous)
        {
            // try cast userstate as AsyncRequestHolder
            AsyncRequestHolder castHolder = makeSynchronous.AsyncState as AsyncRequestHolder;

            // ensure the cast userstate was successful
            if (castHolder == null)
            {
                throw new NullReferenceException("makeSynchronous AsyncState must be castable as AsyncRequestHolder");
            }

            // try/catch check for completion or cancellation to pulse the AsyncRequestHolder, on catch mark the exception in the AsyncRequestHolder (which will also pulse out)
            try
            {
                // if marked as completed synchronously pass through to the userstate which is used within the callstack to prevent blocking on Monitor.Wait
                if (makeSynchronous.CompletedSynchronously)
                {
                    lock (castHolder)
                    {
                        castHolder.MarkCompletedSynchronously();
                    }
                }

                // if asynchronous task completed, then pulse the AsyncRequestHolder
                if (makeSynchronous.IsCompleted)
                {
                    if (!makeSynchronous.CompletedSynchronously)
                    {
                        lock (castHolder)
                        {
                            Monitor.Pulse(castHolder);
                        }
                    }
                }
                // else if asychronous task is not completed, then check for cancellation
                else if (castHolder.FullShutdownToken != null)
                {
                    // check for cancellation
                    Monitor.Enter(castHolder.FullShutdownToken);
                    try
                    {
                        // if cancelled, then mark the AsyncRequestHolder as cancelled and pulse out
                        if (castHolder.FullShutdownToken.Token.IsCancellationRequested)
                        {
                            castHolder.Cancel();

                            if (!makeSynchronous.CompletedSynchronously)
                            {
                                lock (castHolder)
                                {
                                    Monitor.Pulse(castHolder);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(castHolder.FullShutdownToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // mark AsyncRequestHolder with error (which will also pulse out)
                castHolder.MarkException(ex);
            }
        }

        // simple enumeration of currently supported HTTP methods
        private enum requestMethod : byte
        {
            put,
            get,
            post
        }

        // class which is inherited by both the class for storing upload parameters and the class for storing download parameters, with the common properties between them
        private abstract class uploadDownloadParams
        {
            /// <summary>
            /// Path for the file where it would look on disk after truncating the location of the sync directory from the beginning
            /// </summary>
            public string RelativePathForStatus
            {
                get
                {
                    return _relativePathForStatus;
                }
            }
            private readonly string _relativePathForStatus;

            /// <summary>
            /// A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)
            /// </summary>
            public SendUploadDownloadStatus StatusCallback
            {
                get
                {
                    return _statusCallback;
                }
            }
            private readonly SendUploadDownloadStatus _statusCallback;

            /// <summary>
            /// UserState object which is required for calling the StatusCallback for sending status information events
            /// </summary>
            public FileChange ChangeToTransfer
            {
                get
                {
                    return _changeToTransfer;
                }
            }
            private readonly FileChange _changeToTransfer;

            /// <summary>
            /// A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it
            /// </summary>
            public CancellationTokenSource ShutdownToken
            {
                get
                {
                    return _shutdownToken;
                }
            }
            private readonly CancellationTokenSource _shutdownToken;

            /// <summary>
            /// Callback which may be provided by a user to fire for status updates
            /// </summary>
            public AsyncCallback ACallback
            {
                get
                {
                    return _aCallback;
                }
            }
            private readonly AsyncCallback _aCallback;

            /// <summary>
            /// Asynchronous result to be passed upon firing the asynchronous callback
            /// </summary>
            public IAsyncResult AResult
            {
                get
                {
                    return _aResult;
                }
            }
            private readonly IAsyncResult _aResult;

            /// <summary>
            /// Holder for the progress state which can be queried by the user
            /// </summary>
            public GenericHolder<TransferProgress> ProgressHolder
            {
                get
                {
                    return _progressHolder;
                }
            }
            private readonly GenericHolder<TransferProgress> _progressHolder;

            /// <summary>
            /// Callback to fire upon status updates, used internally for getting status from CLSync
            /// </summary>
            public Action<Guid, long, SyncDirection, string, long, long, bool> StatusUpdate
            {
                get
                {
                    return _statusUpdate;
                }
            }
            private readonly Action<Guid, long, SyncDirection, string, long, long, bool> _statusUpdate;

            public Nullable<Guid> StatusUpdateId
            {
                get
                {
                    return _statusUpdateId;
                }
            }
            private readonly Nullable<Guid> _statusUpdateId;

            /// <summary>
            /// The constructor for this abstract base object with all parameters corresponding to all properties
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="ACallback">User-provided callback to fire upon asynchronous operation</param>
            /// <param name="AResult">Asynchronous result for firing async callbacks</param>
            /// <param name="ProgressHolder">Holder for a progress state which can be queried by the user</param>
            public uploadDownloadParams(SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, AsyncCallback ACallback, IAsyncResult AResult, GenericHolder<TransferProgress> ProgressHolder, Action<Guid, long, SyncDirection, string, long, long, bool> StatusUpdate, Nullable<Guid> StatusUpdateId)
            {
                // check for required parameters and error out if not set

                if (ChangeToTransfer == null)
                {
                    throw new NullReferenceException("ChangeToTransfer cannot be null");
                }
                if (ChangeToTransfer.Metadata == null)
                {
                    throw new NullReferenceException("ChangeToTransfer Metadata cannot be null");
                }
                if (ChangeToTransfer.Metadata.HashableProperties.Size == null)
                {
                    throw new NullReferenceException("ChangeToTransfer Metadata HashableProperties Size cannot be null");
                }
                if (((long)ChangeToTransfer.Metadata.HashableProperties.Size) < 0)
                {
                    throw new ArgumentException("ChangeToTransfer Metadata HashableProperties Size must be greater than or equal to zero");
                }
                if (ChangeToTransfer.Metadata.StorageKey == null)
                {
                    throw new ArgumentException("ChangeToTransfer Metadata StorageKey cannot be null");
                }
                if (ChangeToTransfer.NewPath == null)
                {
                    throw new NullReferenceException("ChangeToTransfer NewPath cannot be null");
                }
                if (StatusCallback == null)
                {
                    throw new NullReferenceException("StatusCallback cannot be null");
                }

                // set the readonly properties for this instance from the construction parameters

                this._statusCallback = StatusCallback;
                this._changeToTransfer = ChangeToTransfer;
                this._relativePathForStatus = this.ChangeToTransfer.NewPath.GetRelativePath((SyncRootFullPath ?? string.Empty), false); // relative path is calculated from full path to file minus full path to sync directory
                this._shutdownToken = ShutdownToken;
                this._aCallback = ACallback;
                this._aResult = AResult;
                this._progressHolder = ProgressHolder;
                this._statusUpdate = StatusUpdate;
                this._statusUpdateId = StatusUpdateId;
            }
        }

        // class for storing download properties which inherits abstract base uploadDownloadParams which stores more necessary properties
        private sealed class downloadParams : uploadDownloadParams
        {
            /// <summary>
            /// A non-required (possibly null) event handler for before a download starts
            /// </summary>
            public BeforeDownloadToTempFile BeforeDownloadCallback
            {
                get
                {
                    return _beforeDownloadCallback;
                }
            }
            private readonly BeforeDownloadToTempFile _beforeDownloadCallback;

            /// <summary>
            /// UserState object passed through as-is when the BeforeDownloadCallback handler is fired
            /// </summary>
            public object BeforeDownloadUserState
            {
                get
                {
                    return _beforeDownloadUserState;
                }
            }
            private readonly object _beforeDownloadUserState;

            /// <summary>
            /// Event handler for after a download completes which needs to move the file from the temp location to its final location and set the response body to "---Completed file download---"
            /// </summary>
            public AfterDownloadToTempFile AfterDownloadCallback
            {
                get
                {
                    return _afterDownloadCallback;
                }
            }
            private readonly AfterDownloadToTempFile _afterDownloadCallback;

            /// <summary>
            /// UserState object passed through as-is when the AfterDownloadCallback handler is fired
            /// </summary>
            public object AfterDownloadUserState
            {
                get
                {
                    return _afterDownloadUserState;
                }
            }
            private readonly object _afterDownloadUserState;

            /// <summary>
            /// Full path location to the directory where temporary download files will be stored
            /// </summary>
            public string TempDownloadFolderPath
            {
                get
                {
                    return _tempDownloadFolderPath;
                }
            }
            private readonly string _tempDownloadFolderPath;

            /// <summary>
            /// The sole constructor for this class with all parameters corresponding to all properties in this class and within its base class uploadDownloadParams
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="AfterDownloadCallback">Event handler for after a download completes which needs to move the file from the temp location to its final location and set the response body to "---Completed file download---"</param>
            /// <param name="AfterDownloadUserState">UserState object passed through as-is when the AfterDownloadCallback handler is fired</param>
            /// <param name="TempDownloadFolderPath">Full path location to the directory where temporary download files will be stored</param>
            /// <param name="ACallback">User-provided callback to fire upon asynchronous operation</param>
            /// <param name="AResult">Asynchronous result for firing async callbacks</param>
            /// <param name="ProgressHolder">Holder for a progress state which can be queried by the user</param>
            /// <param name="BeforeDownloadCallback">A non-required (possibly null) event handler for before a download starts</param>
            /// <param name="BeforeDownloadUserState">UserState object passed through as-is when the BeforeDownloadCallback handler is fired</param>
            public downloadParams(AfterDownloadToTempFile AfterDownloadCallback, object AfterDownloadUserState, string TempDownloadFolderPath, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, AsyncCallback ACallback, IAsyncResult AResult, GenericHolder<TransferProgress> ProgressHolder, Action<Guid, long, SyncDirection, string, long, long, bool> StatusUpdate, Nullable<Guid> StatusUpdateId, BeforeDownloadToTempFile BeforeDownloadCallback = null, object BeforeDownloadUserState = null)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath, ACallback, AResult, ProgressHolder, StatusUpdate, StatusUpdateId)
            {
                // additional checks for parameters which were not already checked via the abstract base constructor

                if (base.ChangeToTransfer.Direction != SyncDirection.From)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for a download: " + base.ChangeToTransfer.Direction.ToString());
                }
                //// I changed my mind about this one. We can allow the before download callback to be null.
                //// But, the after download callback is still required since that needs to perform the actual file move operation from temp directory to final location.
                //if (BeforeDownloadCallback == null)
                //{
                //    throw new NullReferenceException("BeforeDownloadCallback cannot be null");
                //}
                if (AfterDownloadCallback == null)
                {
                    throw new NullReferenceException("AfterDownloadCallback cannot be null");
                }

                // set all the readonly fields for public properties by all the parameters which were not passed to the abstract base class

                this._beforeDownloadCallback = BeforeDownloadCallback;
                this._beforeDownloadUserState = BeforeDownloadUserState;
                this._afterDownloadCallback = AfterDownloadCallback;
                this._afterDownloadUserState = AfterDownloadUserState;
                this._tempDownloadFolderPath = TempDownloadFolderPath;
            }
        }

        // class for storing download properties which inherits abstract base uploadDownloadParams which stores more necessary properties
        private sealed class uploadParams : uploadDownloadParams
        {
            /// <summary>
            /// Stream which will be read from to buffer to write into the upload stream, or null if already disposed
            /// </summary>
            public Stream Stream
            {
                get
                {
                    return (_streamDisposed
                        ? null
                        : _stream);
                }
            }
            private readonly Stream _stream;

            /// <summary>
            /// Disposes Stream for the upload if it was not already disposed and marks that it was disposed; not thread-safe disposal checking
            /// </summary>
            public void DisposeStream()
            {
                if (!_streamDisposed)
                {
                    try
                    {
                        _stream.Dispose();
                    }
                    catch
                    {
                    }
                    _streamDisposed = true;
                }
            }
            private bool _streamDisposed = false;

            /// <summary>
            /// MD5 hash lowercase hexadecimal string for the entire upload content
            /// </summary>
            public string Hash
            {
                get
                {
                    return _hash;
                }
            }
            private readonly string _hash;

            /// <summary>
            /// The sole constructor for this class with all parameters corresponding to all properties in this class and within its base class uploadDownloadParams
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events; also used to retrieve the StorageKey and MD5 hash for upload</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="Stream">Stream which will be read from to buffer to write into the upload stream, or null if already disposed</param>
            /// <param name="ACallback">User-provided callback to fire upon asynchronous operation</param>
            /// <param name="AResult">Asynchronous result for firing async callbacks</param>
            /// <param name="ProgressHolder">Holder for a progress state which can be queried by the user</param>
            public uploadParams(Stream Stream, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, AsyncCallback ACallback, IAsyncResult AResult, GenericHolder<TransferProgress> ProgressHolder, Action<Guid, long, SyncDirection, string, long, long, bool> StatusUpdate, Nullable<Guid> StatusUpdateId)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath, ACallback, AResult, ProgressHolder, StatusUpdate, StatusUpdateId)
            {
                // additional checks for parameters which were not already checked via the abstract base constructor

                if (Stream == null)
                {
                    throw new Exception("Stream cannot be null");
                }
                if (base.ChangeToTransfer.Metadata.StorageKey == null)
                {
                    throw new Exception("ChangeToTransfer Metadata StorageKey cannot be null");
                }
                if (base.ChangeToTransfer.Direction != SyncDirection.To)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for an upload: " + base.ChangeToTransfer.Direction.ToString());
                }

                // hash is used in http header for MD5 validation of content stream
                CLError retrieveHashError = this.ChangeToTransfer.GetMD5LowercaseString(out this._hash);
                if (retrieveHashError != null)
                {
                    throw new AggregateException("Unable to retrieve MD5 from ChangeToTransfer", retrieveHashError.GrabExceptions());
                }
                if (this._hash == null)
                {
                    throw new NullReferenceException("ChangeToTransfer must have an MD5 hash");
                }

                // set the readonly field for the public property by all the parameters which were not passed to the abstract base class

                this._stream = Stream;
            }
        }
        #endregion
    }

    /// <summary>
    /// Handler called whenever progress has been made uploading or downloading a file or if the file was cancelled or completed
    /// </summary>
    /// <param name="status">The parameters which describe the progress itself</param>
    /// <param name="eventSource">The FileChange describing the change to upload or download</param>
    internal delegate void SendUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource);

    /// <summary>
    /// Handler called before a download starts with the temporary file id (used as filename for the download in the temp download folder) and passes through UserState
    /// </summary>
    /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
    /// <param name="UserState">Object passed through from the download method call specific to before download</param>
    public delegate void BeforeDownloadToTempFile(Guid tempId, object UserState);

    /// <summary>
    /// ¡¡ Action required: move the completed download file from the temp directory to the final destination !!
    /// Handler called after a file download completes with the id used as the file name in the originally provided temporary download folder,
    /// passes through UserState, passes the download change itself, gives a constructed full path where the downloaded file can be found in the temp folder,
    /// and references a string which should be set to something useful for communications trace to denote a completed file such as "---Completed file download---" (but only set after the file was succesfully moved)
    /// </summary>
    /// <param name="tempFileFullPath">Full path to where the downloaded file can be found in the temp folder (which needs to be moved)</param>
    /// <param name="downloadChange">The download change itself</param>
    /// <param name="responseBody">Reference to string used to trace communication, should be set to something useful to read in communications trace such as "---Completed file download---" (but only after the file was successfully moved)</param>
    /// <param name="UserState">Object passed through from the download method call specific to after download</param>
    /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
    public delegate void AfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId);

    /// <summary>
    /// Status from a call to one of the CLHttpRest communications methods
    /// </summary>
    public enum CLHttpRestStatus : byte
    {
        /// <summary>
        /// Method completed without error and has a normal response
        /// </summary>
        Success,
        /// <summary>
        /// Method invoked a not found (404) response from the server
        /// </summary>
        NotFound,
        /// <summary>
        /// Method invoked a server error (5xx) response from the server
        /// </summary>
        ServerError,
        /// <summary>
        /// Method had some other problem with parameters processed locally or parameters sent up to the server
        /// </summary>
        BadRequest,
        /// <summary>
        /// Method was cancelled via a provided cancellation token before completion
        /// </summary>
        Cancelled,
        /// <summary>
        /// Method completed without error but has no response; it means that no data exists for given parameter(s)
        /// </summary>
        NoContent,
        /// <summary>
        /// Method invoked an unauthorized (401) resposne from the server
        /// </summary>
        NotAuthorized
    }
}
