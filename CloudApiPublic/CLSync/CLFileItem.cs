//
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

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string Revision { get; private set; }
        public Nullable<long> Size { get; private set; }
        public string MimeType { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public Nullable<DateTime> ModifiedDate { get; private set; }
        public string Uid { get; private set; }
        public string ParentUid { get; private set; }
        public bool IsFolder { get; private set; }
        public bool IsDeleted { get; private set; }
        public Nullable<POSIXPermissions> Permissions { get; private set; }
        public CLSyncbox Syncbox { get; private set; }
        public int ChildrenCount { get; private set; }

        #endregion  // end Public Properties

        #region Constructors
        
        public CLFileItem(
            string name,
            string path,
            string revision,
            Nullable<long> size,
            string mimeType,
            DateTime createdDate,
            Nullable<DateTime> modifiedDate,
            string uid,
            string parentUid,
            bool isFolder,
            bool isDeleted,
            Nullable<POSIXPermissions> permissions,
            CLSyncbox syncbox)
        {
            if (createdDate == null)
            {
                throw new ArgumentNullException("createdDate must not be null");
            }
            if (syncbox == null)
            {
                throw new ArgumentNullException("syncbox must not be null");
            }
            if (syncbox.HttpRestClient == null)
            {
                throw new NullReferenceException("syncbox HTTP REST client must not be null");
            }
            if (syncbox.CopiedSettings == null)
            {
                throw new NullReferenceException("syncbox CopiedSettings must not be null");
            }

            this.Name = name;
            this.Path = path;
            this.Revision = revision;
            this.Size = size;
            this.MimeType = mimeType;
            this.CreatedDate = createdDate;
            this.ModifiedDate = modifiedDate;
            this.Uid = uid;
            this.ParentUid = parentUid;
            this.IsFolder = isFolder;
            this.IsDeleted = isDeleted;
            this.Permissions = permissions;
            this.Syncbox = syncbox;
            this.ChildrenCount = 0;

            this._httpRestClient = syncbox.HttpRestClient;
            this._copiedSettings = syncbox.CopiedSettings;
        }

        internal CLFileItem(JsonContracts.SyncboxMetadataResponse response, CLSyncbox syncbox)
        {
            if (response == null)
            {
                throw new NullReferenceException("response must not be null");
            }
            if (response.CreatedDate == null)
            {
                throw new NullReferenceException("response CreatedDate must not be null");
            }
            if (syncbox == null)
            {
                throw new ArgumentNullException("syncbox must not be null");
            }
            if (syncbox.HttpRestClient == null)
            {
                throw new NullReferenceException("syncbox HTTP REST client must not be null");
            }
            if (syncbox.CopiedSettings == null)
            {
                throw new NullReferenceException("syncbox CopiedSettings must not be null");
            }

            this.Name = response.Name;
            this.Path = response.RelativePath;
            this.Revision = response.Revision;
            this.Size = response.Size;
            this.MimeType = response.MimeType;
            this.CreatedDate = (DateTime)response.CreatedDate;
            this.ModifiedDate = response.ModifiedDate;
            this.Uid = response.ServerUid;
            this.ParentUid = response.ParentUid;
            this.IsFolder = response.IsFolder ?? false;
            this.IsDeleted = response.IsDeleted ?? false;
            this.Permissions = response.PermissionsEnum;
            this.Syncbox = syncbox;
            this.ChildrenCount = 0;

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
        /// <param name="callbackUserState">Userstate to pass when firing async callback</param>
        /// <returns>Returns the asynchronous result which is used to retrieve the result</returns>
        public IAsyncResult BeginDownloadFile(AsyncCallback callback, object callbackUserState)
        {
            CheckHalted();

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
            CheckHalted();
            return _httpRestClient.EndDownloadFile(aResult, out result);
        }

        /// <summary>
        /// Download the file represented by this CLFileItem object from the cloud.
        /// </summary>
        /// <returns>CLError: Any error, or null.</returns>
        public CLError DownloadFile()
        {
            FileChange fcToDownload = new FileChange()
            {
                Direction = SyncDirection.From,
                Metadata = new FileMetadata()
            };

            return _httpRestClient.DownloadFile(fcToDownload, this.Uid, this.Revision, OnAfterDownloadToTempFile, this, _copiedSettings.HttpTimeoutMilliseconds);
        }

        /// <summary>
        /// A file download has completed.  Move the file to its target location.
        /// </summary>
        /// <param name="tempFileFullPath">The full path of the downloaded file in the temporary download location.</param>
        /// <param name="downloadChange">The FileChange representing the download request.</param>
        /// <param name="responseBody">The HTTP response body returned from the server.</param>
        /// <param name="UserState">The user state.</param>
        /// <param name="tempId">The temp ID of this downloaded file (GUID).</param>
        private void OnAfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId)
        {
            try
            {
                CLFileItem castState = UserState as CLFileItem;
                if (castState == null)
                {
                    throw new NullReferenceException("castState must be a CLFileItem");
                }
                if (castState._copiedSettings == null)
                {
                    throw new NullReferenceException("castState _copiedSettings must not be null");
                }
                if (castState.Syncbox == null)
                {
                    throw new NullReferenceException("castState Syncbox must not be null");
                }
                if (tempFileFullPath == null)
                {
                    throw new NullReferenceException("tempFileFullPath must not be null");
                }
                if (downloadChange == null)
                {
                    throw new NullReferenceException("downloadChange must not be null");
                }

                // Move the downloaded file.
                string backupFileFullPath = Helpers.GetTempFileDownloadPath(castState._copiedSettings, castState.Syncbox.SyncboxId) + /* '\\' */ (char)0x005c + Guid.NewGuid().ToString();
                CLError errorFromMoveDownloadedFile = Helpers.MoveDownloadedFile(tempFileFullPath, Path, backupFileFullPath);
                if (errorFromMoveDownloadedFile != null)
                {
                    throw new AggregateException("Error moving the downloaded file", errorFromMoveDownloadedFile.Exceptions);
                }

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
        CLError Children(out CLFileItem [] children)
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