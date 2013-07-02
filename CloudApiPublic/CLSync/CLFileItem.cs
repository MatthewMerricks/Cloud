//
// CLFileItem.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Callbacks;
using Cloud.Interfaces;
using Cloud.Model;
using Cloud.REST;
using Cloud.Static;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Cloud
{
    /// <summary>
    /// Represents a Cloud file/folder item."/>
    /// </summary>
    public sealed class CLFileItem
    {
        #region Private Fields

        private readonly CLHttpRest _httpRestClient;
        private readonly ICLSyncSettingsAdvanced _copiedSettings;

        #endregion
        #region Public Enums

        public enum CLFileItemImageSize
        {
            CLFileItemImageSizeSmall = 0,
            CLFileItemImageSizeMedium,
            CLFileItemImageSizeLarge,
            CLFileItemImageSizeFull,
            CLFileItemImageSizeThumbnail,
        }

        #endregion

        #region Public Readonly Properties

        /// <summary>
        /// The name of this item.  For a folder, it is the last token in the path.  For a file, it is the file name plus extension.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private readonly string _name;

        /// <summary>
        /// The relative path of this item in the syncbox.
        /// </summary>
        public string RelativePath
        {
            get
            {
                return _relativePath;
            }
        }
        private readonly string _relativePath;

        /// <summary>
        /// The absolute path represented by the syncbox path plus this item's relative path. Will be null if relative path or the syncbox path is null or empty.
        /// </summary>
        public string FullPath
        {
            get
            {
                return _fullPath;
            }
        }
        private readonly string _fullPath;

        /// <summary>
        /// The server-assigned revision of this file item.
        /// </summary>
        public string Revision
        {
            get
            {
                return _revision;
            }
        }
        private readonly string _revision;

        /// <summary>
        /// The MD5 hash of this file item.
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
        /// The size of this file item.
        /// </summary>
        public Nullable<long> Size
        {
            get
            {
                return _size;
            }
        }
        private readonly Nullable<long> _size;

        /// <summary>
        /// The Mime type associated with this file item.
        /// </summary>
        public string MimeType
        {
            get
            {
                return _mimeType;
            }
        }
        private readonly string _mimeType;

        /// <summary>
        /// The date and time that this item was created.
        /// </summary>
        public DateTime CreatedDate
        {
            get
            {
                return _createdDate;
            }
        }
        private readonly DateTime _createdDate;

        /// <summary>
        /// The date and time that this item was last modified.
        /// </summary>
        public DateTime ModifiedDate
        {
            get
            {
                return _modifiedDate;
            }
        }
        private readonly DateTime _modifiedDate;

        /// <summary>
        /// The server-assigned unique identifier for this item.
        /// </summary>
        public string ItemUid
        {
            get
            {
                return _uid;
            }
        }
        private readonly string _uid;

        /// <summary>
        /// The server-assigned unique identifier of the parent of this item.
        /// </summary>
        public string ParentUid
        {
            get
            {
                return _parentUid;
            }
        }
        private readonly string _parentUid;

        /// <summary>
        /// True: This item is a folder.  False: This item is a file.
        /// </summary>
        public bool IsFolder
        {
            get
            {
                return _isFolder;
            }
        }
        private readonly bool _isFolder;

        /// <summary>
        /// This item has been deleted in the syncbox.
        /// </summary>
        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
        }
        private readonly bool _isDeleted;

        /// <summary>
        /// True: The metadata for this item has been created in the syncbox, but no device has yet completed an upload of the file item.
        /// </summary>
        public bool IsPending
        {
            get
            {
                return _isPending;
            }
        }
        private readonly bool _isPending;

        /// <summary>
        /// The associated syncbox.
        /// </summary>
        public CLSyncbox Syncbox
        {
            get
            {
                return _syncbox;
            }
        }
        private readonly CLSyncbox _syncbox;

        //// in public SDK documents, but for now it's always zero, so don't expose it
        //public int ChildrenCount
        //{
        //    get
        //    {
        //        return _childrenCount;
        //    }
        //}
        //private readonly int _childrenCount;

        #endregion  // end Public Properties

        #region Constructors

        //// iOS is not allowing public construction of the CLFileItem, it can only be produced internally by create operations or by queries
        //
        //// we aren't using this constructor and it's internal, so it's commented for now
        //
        //internal /* public */ CLFileItem(
        //    string name,
        //    string relativePath,
        //    string revision,
        //    string hash,
        //    Nullable<long> size,
        //    string mimeType,
        //    DateTime createdDate,
        //    DateTime modifiedDate,
        //    string uid,
        //    string parentUid,
        //    bool isFolder,
        //    bool isDeleted,
        //    bool isPending,
        //    CLSyncbox syncbox)
        //{
        //    if (syncbox == null)
        //    {
        //        throw new CLArgumentNullException(CLExceptionCode.FileItem_NullSyncbox, Resources.SyncboxMustNotBeNull);
        //    }
            //if (string.IsNullOrEmpty(syncbox.Path))
            //{
            //    throw new CLArgumentNullException(CLExceptionCode.Syncbox_BadPath, Resources.ExceptionOnDemandCheckPathSyncboxPathNull);
            //}

        //    this._name = name;
        //    this._relativePath = relativePath;
        //    this._fullPath = [calculate full path here]
        //    this._revision = revision;
        //    this._hash = hash;
        //    this._size = size;
        //    this._mimeType = mimeType;
        //    this._createdDate = createdDate;
        //    this._modifiedDate = modifiedDate;
        //    this._uid = uid;
        //    this._parentUid = parentUid;
        //    this._isFolder = isFolder;
        //    this._isDeleted = isDeleted;
        //    this._isPending = isPending;
        //    this._syncbox = syncbox;
        //    //// in public SDK documents, but for now it's always zero, so don't expose it
        //    //this._childrenCount = 0;

        //    this._httpRestClient = syncbox.HttpRestClient;
        //    this._copiedSettings = syncbox.CopiedSettings;
        //}

        /// <summary>
        /// Constructor to create a CLFileItem from a FileChange.
        /// </summary>
        internal CLFileItem(FileChange fileChange, CLSyncbox syncbox, string revision, string uid, bool isDeleted, bool isPending)
        {
            if (fileChange == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.FileItem_RequiresFileChange, Resources.ExceptionFileItemNullFileChange);
            }
            if (syncbox == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.FileItem_NullSyncbox, Resources.SyncboxMustNotBeNull);
            }

            this._isFolder = fileChange.Metadata.HashableProperties.IsFolder;
            this._name = fileChange.NewPath.Name;
            this._relativePath = fileChange.NewPath.GetRelativePath(syncbox.Path, replaceWithForwardSlashes: false);  // relative Windows path
            this._fullPath = fileChange.NewPath.ToString();
            this._revision = revision;
            this._hash = fileChange.GetMD5LowercaseString();
            this._size = fileChange.Metadata.HashableProperties.Size;
            this._mimeType = fileChange.Metadata.MimeType;
            this._createdDate = fileChange.Metadata.HashableProperties.CreationTime;
            this._modifiedDate = fileChange.Metadata.HashableProperties.LastTime;
            this._uid = uid;
            this._parentUid = fileChange.Metadata.ParentFolderServerUid;
            this._isDeleted = isDeleted;
            this._isPending = isPending;
            this._syncbox = syncbox;

            ////// in public SDK documents, but for now it's always zero, so don't expose it
            ////this._childrenCount = 0;

            this._httpRestClient = syncbox.HttpRestClient;
            this._copiedSettings = syncbox.CopiedSettings;
        }

        /// <summary>
        /// Use this constructor if the response does not have a headerAction or action.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="syncbox"></param>
        internal CLFileItem(JsonContracts.SyncboxMetadataResponse response, CLSyncbox syncbox) : this(response, null, null, syncbox)
        {
        }

        /// <summary>
        /// Constructor from a JsonContracts.SyncboxMetadataResponse.
        /// </summary>
        /// <param name="headerAction">[FileChangeResponse].Header.Action, used as primary fallback for setting IsFolder</param>
        /// <param name="action">[FileChangeResponse].Action, used as secondary fallback for setting IsFolder</param>
        internal CLFileItem(JsonContracts.SyncboxMetadataResponse response, string headerAction, string action, CLSyncbox syncbox)
        {
            if (response == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.FileItem_NullResponse, Resources.ExceptionFileItemNullResponse);
            }
            if (syncbox == null)
            {
                throw new CLArgumentNullException(CLExceptionCode.FileItem_NullSyncbox, Resources.SyncboxMustNotBeNull);
            }

            if (response.IsFolder == null)
            {
                string pullAction = headerAction ?? action;

                if (string.IsNullOrEmpty(pullAction))
                {
                    throw new CLNullReferenceException(CLExceptionCode.FileItem_NullIsFolder, Resources.ExceptionFileItemNullIsFolderActionAndHeaderAction);
                }

                if (CLDefinitions.SyncHeaderIsFolders.Contains(pullAction))
                {
                    this._isFolder = true;
                }
                else if (CLDefinitions.SyncHeaderIsFiles.Contains(pullAction))
                {
                    this._isFolder = false;
                }
                else
                {
                    throw new CLNullReferenceException(CLExceptionCode.FileItem_UnknownAction, string.Format(Resources.ExceptionFileItemUnknownAction, pullAction));
                }
            }
            else
            {
                this._isFolder = (bool)response.IsFolder;
            }

            this._name = response.Name;
            string coallescedRelativePath = response.RelativeToPathWithoutEnclosingSlashes ?? response.RelativePathWithoutEnclosingSlashes;

            this._relativePath = (coallescedRelativePath == null
                ? null
                : (coallescedRelativePath.Replace('/', '\\')));

            this._fullPath = (coallescedRelativePath == null
                ? null
                : (string.IsNullOrEmpty(syncbox.Path)
                    ? null
                    : (syncbox.Path + ((char)0x5C /* '\\' */) + this._relativePath)));

            this._revision = response.Revision;
            this._hash = response.Hash;
            this._size = response.Size;
            this._mimeType = response.MimeType;
            this._createdDate = response.CreatedDate ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
            this._modifiedDate = response.ModifiedDate ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
            this._uid = response.ServerUid;
            this._parentUid = response.ParentUid;
            this._isDeleted = response.IsDeleted ?? false;
            this._isPending = !(response.IsNotPending ?? true);
            this._syncbox = syncbox;
            //// in public SDK documents, but for now it's always zero, so don't expose it
            //this._childrenCount = 0;

            this._httpRestClient = syncbox.HttpRestClient;
            this._copiedSettings = syncbox.CopiedSettings;
        }

        #endregion

        #region Public Methods

        #region File Sharing

        //TODO: Implement:
        // CLFileItemLink CreatePublicShareLink(int timeToLiveMinutes, int downloadTimesLimit);

        #endregion  // end File Sharing
        #region File Download

        /// <summary>
        /// Asynchronously starts downloading a file representing this CLFileItem object from the cloud.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.  May be null.</param>
        /// <param name="asyncCallbackUserState">User state to pass to the async callback when it is fired.  May be null.</param>
        /// <param name="transferStatusCallback">The callback to fire with tranfer status updates.  May be null.</param>
        /// <param name="transferStatusCallbackUserState">The user state to pass to the transfer status callback above.  May be null.</param>
        /// <param name="cancellationSource">A cancellation token source object that can be used to cancel the download operation.  May be null</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDownloadFile(
            AsyncCallback asyncCallback, 
            object asyncCallbackUserState,
            CLFileDownloadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource)
        {
            // Make a holder for the callers transfer status callback and user state.
            Tuple<CLFileDownloadTransferStatusCallback, object> userTransferStatusParamHolder = new Tuple<CLFileDownloadTransferStatusCallback, object>(transferStatusCallback, transferStatusCallbackUserState);

            // Build a holder to receive the full path of the downloaded temp file.
            GenericHolder<string> fullPathDownloadedTempFileHolder = new GenericHolder<string>(null);

            return _httpRestClient.BeginDownloadFile(
                asyncCallback,
                asyncCallbackUserState,
                ConstructFileChangeForDownload(),
                this.ItemUid,
                this.Revision,
                /* moveFileUponCompletion: */ OnAfterDownloadToTempFile,
                /* moveFileUponCompletionState: */ fullPathDownloadedTempFileHolder,
                /* timeoutMilliseconds: */ _copiedSettings.HttpTimeoutMilliseconds,
                /* statusUpdate: */ TransferStatusCallback,
                /* statusUpdateUserState: */ userTransferStatusParamHolder,
                /* beforeDownload: */ null,
                /* beforeDownloadState: */ null,
                /* shutdownToken: */ cancellationSource,
                needsScheduling: true);
        }

        /// <summary>
        /// Finishes downloading a file from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon starting creating the syncbox</param>
        /// <param name="result">(output) The result from the asynchronous operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDownloadFile(IAsyncResult asyncResult, out DownloadFileResult result)
        {
            return _httpRestClient.EndDownloadFile(asyncResult, out result);
        }

        /// <summary>
        /// Download the file represented by this CLFileItem object from the cloud.  The file will be downloaded into a unique
        /// file name in the temporary download directory.  Upon successful download, the full path of the temporary file will be output to the caller.
        /// The caller is responsible for moving the temp file to its permanent location, and for ensuring that the temporary file
        /// has been deleted.
        /// </summary>
        /// <param name="fullPathDownloadedTempFile">(output) The full path of the downloaded file in the temp download directory.</param>
        /// <param name="transferStatusCallback">The callback to fire with tranfer status updates.  May be null.</param>
        /// <param name="transferStatusCallbackUserState">The user state to pass to the transfer status callback above.  May be null.</param>
        /// <returns>CLError: Any error, or null.</returns>
        public CLError DownloadFile(
            out string fullPathDownloadedTempFile,
            CLFileDownloadTransferStatusCallback transferStatusCallback,
            object transferStatusCallbackUserState)
        {
            // Make a holder for the callers transfer status callback and user state.
            Tuple<CLFileDownloadTransferStatusCallback, object> userTransferStatusParamHolder = new Tuple<CLFileDownloadTransferStatusCallback, object>(transferStatusCallback, transferStatusCallbackUserState);

            // Build a holder to receive the full path of the downloaded temp file.
            GenericHolder<string> fullPathDownloadedTempFileHolder = new GenericHolder<string>(null);

            // Download the file.
            CLError errorFromDownloadFile = _httpRestClient.DownloadFile(
                ConstructFileChangeForDownload(),
                this.ItemUid,
                this.Revision,
                /* moveFileUponCompletion: */ OnAfterDownloadToTempFile,
                /* moveFileUponCompletionState: */ fullPathDownloadedTempFileHolder,
                /* timeoutMilliseconds: */ _copiedSettings.HttpTimeoutMilliseconds,
                /* statusUpdate: */ TransferStatusCallback,
                /* statusUpdateUserState: */ userTransferStatusParamHolder,
                needsScheduling: true);

            // Output the full path of the downloaded file on success
            if (errorFromDownloadFile == null)
            {
                fullPathDownloadedTempFile = fullPathDownloadedTempFileHolder.Value;
            }
            else
            {
                fullPathDownloadedTempFile = null;
            }

            return errorFromDownloadFile;
        }

        /// <summary>
        /// Build the file change to represent the file to download.
        /// </summary>
        private FileChange ConstructFileChangeForDownload()
        {
            return new FileChange()
            {
                Direction = SyncDirection.From,
                Metadata = new FileMetadata(this),
                NewPath = RelativePath,
            };
        }

        /// <summary>
        /// Transfer status has changed.  This is called by CLHttpRest during the download.  Forward the info to the app.
        /// </summary>
        private void TransferStatusCallback(object userState, long eventId, SyncDirection direction, string relativePath, long byteProgress, long totalByteSize, bool isError)
        {
            Tuple<CLFileDownloadTransferStatusCallback, object> castState = userState as Tuple<CLFileDownloadTransferStatusCallback, object>;
            if (castState == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.OnDemand_Download, Resources.ExceptionOnDemandDownloadFileIncorrectUserState);
            }

            CLFileDownloadTransferStatusCallback transferStatusCallback = castState.Item1;
            object transferStatusCallbackUserState = castState.Item2;

            if (!isError
                && transferStatusCallback != null)
            {
                try
                {
                    transferStatusCallback(byteProgress, totalByteSize, transferStatusCallbackUserState);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// A file download has completed.  Move the file to its target location.
        /// </summary>
        /// <param name="tempFileFullPath">The full path of the downloaded file in the temporary download location.</param>
        /// <param name="downloadChange">The FileChange representing the download request.</param>
        /// <param name="responseBody">The HTTP response body returned from the server.</param>
        /// <param name="UserState">The user state.</param>
        /// <param name="tempId">The temp ID of this downloaded file (Guid).</param>
        private void OnAfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId)
        {
            try
            {
                GenericHolder<string> castState = UserState as GenericHolder<string>;
                if (castState == null)
                {
                    throw new CLNullReferenceException(CLExceptionCode.OnDemand_Download, Resources.CLFileItemCastStateMustBeAGenericHolderOfString);
                }

                // Output the full path of the temp file for the user to move.
                castState.Value = tempFileFullPath;

                // Success
                responseBody = Resources.CompletedFileDownloadHttpBody;
            }
            catch (Exception ex)
            {
                responseBody = Resources.ErrorMovingDownloadedFileHttpBody;
                throw ex;
            }
        }

        #endregion  // end File Download

        #region DownloadImageOfSize (downloads an image for the syncbox file represented by this CLFileItem object, selecting the size of the image to load)

        /// <summary>
        /// Asynchronously starts downloading an image for the syncbox file represented by this CLFileItem object, selecting the size of the image to load.
        /// </summary>
        /// <param name="asyncCallback">Callback method to fire when the async operation completes.  May be null.</param>
        /// <param name="asyncCallbackUserState">User state to pass to the async callback when it is fired.  May be null.</param>
        /// <param name="imageSize">The size of the image to load.</param>
        /// <param name="transferStatusCallback">The callback to fire with tranfer status updates.  May be null.</param>
        /// <param name="transferStatusCallbackUserState">The user state to pass to the transfer status callback above.  May be null.</param>
        /// <param name="cancellationSource">A cancellation token source object that can be used to cancel the download operation.  May be null</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDownloadImageOfSize(
            AsyncCallback asyncCallback,
            object asyncCallbackUserState,
            CLFileItemImageSize imageSize,
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
                        Stream imageStream;
                        CLError processError = DownloadImageOfSize(
                            Data.imageSize,
                            out imageStream,
                            Data.transferStatusCallback,
                            Data.transferStatusCallbackUserState,
                            Data.cancellationSource);

                        Data.toReturn.Complete(
                            new FileItemDownloadImageResult(
                                processError,  // any error that may have occurred during processing
                                imageStream),  // the output stream
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
        /// Finishes downloading the image for this CLFileItem object from the syncbox, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="asyncResult">The asynchronous result provided upon completion of the async operation.</param>
        /// <param name="result">(output) The result from the asynchronous operation.</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDownloadImageOfSize(IAsyncResult asyncResult, out FileItemDownloadImageResult result)
        {
            return _httpRestClient.EndDownloadImageOfSize(asyncResult, out result);
        }

        /// <summary>
        /// Downloads an image for the syncbox file represented by this CLFileItem object, selecting the size of the image to load.
        /// </summary>
        /// <param name="imageSize">The size of the image to load.</param>
        /// <param name="imageStream">(output) An open Stream containing the data for the image loaded from the cloud file.</param>
        /// <param name="transferStatusCallback">The transfer progress delegate to call.  May be null.</param>
        /// <param name="transferStatusCallbackUserState">The user state to pass to the transfer progress delegate above.  May be null.</param>
        /// <param name="cancellationSource">A cancellation token source object that can be used to cancel the download operation.  May be null</param>
        /// <returns>Any error, or null.</returns>
        public CLError DownloadImageOfSize(
            CLFileItemImageSize imageSize,
            out Stream imageStream, 
            CLFileDownloadTransferStatusCallback transferStatusCallback, 
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource)
        {
            return _httpRestClient.DownloadImageOfSize(this, imageSize, out imageStream, transferStatusCallback, transferStatusCallbackUserState, cancellationSource);
        }

        #endregion  // end DownloadImageOfSize (downloads an image for the syncbox file represented by this CLFileItem object, selecting the size of the image to load)

        //// we don't keep track of children of CLFileItems internally
        //
        //#region Folder Contents

        ///// <summary>
        ///// Gets the children of the Cloud folder represented by this CLFileItem object.  The ChildrenCount property will be updated with the number of children returned.
        ///// </summary>
        ///// <param name="children">(output) The array of CLFileItem objects representing the children of this folder object in the cloud, or null.</param>
        ///// <returns></returns>
        ///// <remarks>This method will return an error if called when IsFolder is false.</remarks>
        //CLError Children(out CLFileItem[] children)
        //{
        //    children = null;
        //    return null;
        //}

        //#endregion  // end Folder Contents
        #endregion  // end Public Methods
    }
}