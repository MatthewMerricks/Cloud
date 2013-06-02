﻿//
// CLFileItem.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model;
using Cloud.REST;
using Cloud.Static;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Cloud.CLSync
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
        #region Public Delegates

        public delegate void DownloadFileProgress(int downloadedBytes, int totalDownloadedBytes, int totalExpectedBytes);		 

    	#endregion

        #region Public Enums

        public enum CLFileItemImageSize
        {
            CLFileItemImageSizeSmall = 0,
            CLFileItemImageSizeMedium,
            CLFileItemImageSizeLarge
        }

        #endregion

        #region Public Readonly Properties

        public string Name
        {
            get
            {
                return _name;
            }
        }
        private readonly string _name;

        public string RelativePath
        {
            get
            {
                return _relativePath;
            }
        }
        private readonly string _relativePath;

        public string FullPath
        {
            get
            {
                return _fullPath;
            }
        }
        private readonly string _fullPath;

        public string Revision
        {
            get
            {
                return _revision;
            }
        }
        private readonly string _revision;

        public Nullable<long> Size
        {
            get
            {
                return _size;
            }
        }
        private readonly Nullable<long> _size;

        public string MimeType
        {
            get
            {
                return _mimeType;
            }
        }
        private readonly string _mimeType;

        public DateTime CreatedDate
        {
            get
            {
                return _createdDate;
            }
        }
        private readonly DateTime _createdDate;

        public DateTime ModifiedDate
        {
            get
            {
                return _modifiedDate;
            }
        }
        private readonly DateTime _modifiedDate;

        public string Uid
        {
            get
            {
                return _uid;
            }
        }
        private readonly string _uid;

        public string ParentUid
        {
            get
            {
                return _parentUid;
            }
        }
        private readonly string _parentUid;

        public bool IsFolder
        {
            get
            {
                return _isFolder;
            }
        }
        private readonly bool _isFolder;

        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
        }
        private readonly bool _isDeleted;

        public bool IsPending
        {
            get
            {
                return _isPending;
            }
        }
        private readonly bool _isPending;

        public Nullable<POSIXPermissions> Permissions
        {
            get
            {
                return _permissions;
            }
        }
        private readonly Nullable<POSIXPermissions> _permissions;

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

        #region Internal Readonly Properties

        internal string StorageKey
        {
            get
            {
                return _storageKey;
            }
        }
        private readonly string _storageKey;

        #endregion

        #region Constructors

        //// iOS is not allowing public construction of the CLFileItem, it can only be produced internally by create operations or by queries
        //
        //// we aren't using this constructor and it's internal, so it's commented for now
        //
        //internal /* public */ CLFileItem(
        //    string name,
        //    string relativePath,
        //    string revision,
        //    Nullable<long> size,
        //    string mimeType,
        //    DateTime createdDate,
        //    DateTime modifiedDate,
        //    string uid,
        //    string parentUid,
        //    bool isFolder,
        //    bool isDeleted,
        //    bool isPending,d
        //    Nullable<POSIXPermissions> permissions,
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
        //    this._size = size;
        //    this._mimeType = mimeType;
        //    this._createdDate = createdDate;
        //    this._modifiedDate = modifiedDate;
        //    this._uid = uid;
        //    this._parentUid = parentUid;
        //    this._isFolder = isFolder;
        //    this._isDeleted = isDeleted;
        //    this._isPending = isPending;
        //    this._permissions = permissions;
        //    this._syncbox = syncbox;
        //    //// in public SDK documents, but for now it's always zero, so don't expose it
        //    //this._childrenCount = 0;

        //    this._httpRestClient = syncbox.HttpRestClient;
        //    this._copiedSettings = syncbox.CopiedSettings;
        //}

        /// <summary>
        /// Use this if the response does not have a headerAction or action.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="syncbox"></param>
        internal CLFileItem(JsonContracts.SyncboxMetadataResponse response, CLSyncbox syncbox) : this(response, null, null, syncbox)
        {
        }

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
                : (syncbox.Path + "\\" + this._relativePath));

            this._revision = response.Revision;
            this._size = response.Size;
            this._mimeType = response.MimeType;
            this._createdDate = response.CreatedDate ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
            this._modifiedDate = response.ModifiedDate ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
            this._uid = response.ServerUid;
            this._parentUid = response.ParentUid;
            this._isDeleted = response.IsDeleted ?? false;
            this._isPending = !(response.IsNotPending ?? true);
            this._permissions = response.PermissionsEnum;
            this._syncbox = syncbox;
            this._storageKey = response.StorageKey;
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
        /// <param name="callback">Callback method to fire when operation completes</param>
        /// <param name="callbackUserState">User state to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDownloadFile(AsyncCallback callback, object callbackUserState)
        {
            FileChange fcToDownload = new FileChange()
            {
                 Direction = SyncDirection.From
            };
            return _httpRestClient.BeginDownloadFile(callback, callbackUserState, fcToDownload, this.Uid, this.Revision, OnAfterDownloadToTempFile, this, _copiedSettings.HttpTimeoutMilliseconds);
        }

        /// <summary>
        /// Finishes downloading a file, if it has not already finished via its asynchronous result, and outputs the result,
        /// returning any error that occurs in the process (which is different than any error which may have occurred in communication; check the result's Error)
        /// </summary>
        /// <param name="aResult">The asynchronous result provided upon starting the audios query</param>
        /// <param name="result">(output) The result from the request</param>
        /// <returns>Returns the error that occurred while finishing and/or outputing the result, if any</returns>
        public CLError EndDownloadFile(IAsyncResult aResult, out DownloadFileResult result)
        {
            return _httpRestClient.EndDownloadFile(aResult, out result);
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
            FileDownloadTransferStatusCallback transferStatusCallback, 
            object transferStatusCallbackUserState,
            CancellationTokenSource cancellationSource)
        {
            // Build the file change to represent the file to download.
            FileChange fcToDownload = new FileChange()
            {
                Direction = SyncDirection.From,
                Metadata = new FileMetadata(this),
                NewPath = RelativePath,
            };

            // Make a holder for the callers transfer status callback and user state.
            Tuple<FileDownloadTransferStatusCallback, object> userTransferStatusParamHolder = new Tuple<FileDownloadTransferStatusCallback, object>(transferStatusCallback, transferStatusCallbackUserState);

            // Build a holder to receive the full path of the downloaded temp file.
            GenericHolder<string> fullPathDownloadedTempFileHolder = new GenericHolder<string>(null);

            // Download the file.
            CLError errorFromDownloadFile = _httpRestClient.DownloadFile(
                fcToDownload,
                this.Uid,
                this.Revision,
                moveFileUponCompletion: OnAfterDownloadToTempFile,
                moveFileUponCompletionState: fullPathDownloadedTempFileHolder,
                timeoutMilliseconds: _copiedSettings.HttpTimeoutMilliseconds,
                beforeDownload: null,
                beforeDownloadState: null,
                shutdownToken: cancellationSource,
                customDownloadFolderFullPath: null,
                statusUpdate: TransferStatusCallback,
                statusUpdateUserState: userTransferStatusParamHolder);

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
        /// Transfer status has changed.  This is called by CLHttpRest during the download.  Forward the info to the app.
        /// </summary>
        private void TransferStatusCallback(object userState, long eventId, SyncDirection direction, string relativePath, long byteProgress, long totalByteSize, bool isError)
        {
            Tuple<FileDownloadTransferStatusCallback, object> castState = userState as Tuple<FileDownloadTransferStatusCallback, object>;
            if (castState == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.OnDemand_Download, Resources.ExceptionOnDemandDownloadFileIncorrectUserState);
            }

            FileDownloadTransferStatusCallback transferStatusCallback = castState.Item1;
            object transferStatusCallbackUserState = castState.Item2;

            if (transferStatusCallback != null)
            {
                transferStatusCallback(byteProgress, totalByteSize, transferStatusCallbackUserState);
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

        #region Image Files

        /// <summary>
        /// Load a thumbnail image from the cloud file represented by this CLFileItem object.
        /// </summary>
        /// <param name="thumbnail">(output) The thumbnail image loaded from the cloud file.</param>
        /// <param name="callback">(optional) The load progress delegate to call.</param>
        /// <param name="callbackUserState">(optional) The user state to pass to the load progress delegate above.</param>
        /// <returns>Any error, or null.</returns>
        public CLError LoadThumbnail(out Image thumbnail, DownloadFileProgress callback = null, object callbackUserState = null)
        {
            thumbnail = null;
            return null;
        }

        /// <summary>
        /// Load an image from a cloud file represented by this CLFileItem object, selecting the size of the image to load.
        /// </summary>
        /// <param name="image">(output) The image loaded from the cloud file.</param>
        /// <param name="imageSize">The size of the image to load.</param>
        /// <param name="callback">(optional) The load progress delegate to call.</param>
        /// <param name="callbackUserState">(optional) The user state to pass to the load progress delegate above.</param>
        /// <returns>Any error, or null.</returns>
        public CLError LoadImageWithSize(out Image image, CLFileItemImageSize imageSize, DownloadFileProgress callback = null, object callbackUserState = null)
        {
            image = null;
            return null;
        }

        

        #endregion  // end Image Files

        #region Folder Contents

        /// <summary>
        /// Gets the children of the Cloud folder represented by this CLFileItem object.  The ChildrenCount property will be updated with the number of children returned.
        /// </summary>
        /// <param name="children">(output) The array of CLFileItem objects representing the children of this folder object in the cloud, or null.</param>
        /// <returns></returns>
        /// <remarks>This method will return an error if called when IsFolder is false.</remarks>
        CLError Children(out CLFileItem[] children)
        {
            children = null;
            return null;
        }

        #endregion  // end Folder Contents
        #endregion  // end Public Methods

        #region Private Methods

        /// <summary>
        /// Throw an exception if halted.
        /// </summary>
        private void CheckHalted()
        {
            Helpers.CheckHalted();
        }

        #endregion
    }
}