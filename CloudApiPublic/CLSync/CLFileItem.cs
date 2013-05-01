//
// CLFileItem.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;

namespace Cloud.CLSync
{
    /// <summary>
    /// Represents a Cloud file/folder item."/>
    /// </summary>
    public sealed class CLFileItem
    {
        #region Public Delegates

        public delegate void DownloadFileProgress(int downloadedBytes, int totalDownloadedBytes, int totalExpectedBytes);		 

    	#endregion

        #region Public Readonly Properties

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string Revision { get; private set; }
        public Nullable<long> Size { get; private set; }
        public string MimeType { get; private set; }
        public DateTime CreatedTime { get; private set; }
        public Nullable<DateTime> ModifiedTime { get; private set; }
        public string Uid { get; private set; }
        public string ParentUid { get; private set; }
        public bool IsFolder { get; private set; }
        public bool IsDeleted { get; private set; }
        public Nullable<POSIXPermissions> Permissions { get; private set; }
        public CLSyncbox Syncbox { get; private set; }

        #endregion  // end Public Properties

        #region Constructors
        
        public CLFileItem(
            string name,
            string path,
            string revision,
            Nullable<long> size,
            string mimeType,
            DateTime createdTime,
            Nullable<DateTime> modifiedTime,
            string uid,
            string parentUid,
            bool isFolder,
            bool isDeleted,
            Nullable<POSIXPermissions> permissions,
            CLSyncbox syncbox)
        {
            this.Name = name;
            this.Path = path;
            this.Revision = revision;
            this.Size = size;
            this.MimeType = mimeType;
            this.CreatedTime = createdTime;
            this.ModifiedTime = modifiedTime;
            this.Uid = uid;
            this.ParentUid = parentUid;
            this.IsFolder = isFolder;
            this.IsDeleted = isDeleted;
            this.Permissions = permissions;
            this.Syncbox = syncbox;
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

            this.Name = response.Name;
            this.Path = response.RelativePath;
            this.Revision = response.Revision;
            this.Size = response.Size;
            this.MimeType = response.MimeType;
            this.CreatedTime = (DateTime)response.CreatedDate;
            this.ModifiedTime = response.ModifiedDate;
            this.Uid = response.ServerUid;
            this.ParentUid = response.ParentUid;
            this.IsFolder = response.IsFolder ?? false;
            this.IsDeleted = response.IsDeleted ?? false;
            this.Permissions = response.PermissionsEnum;
            this.Syncbox = syncbox;
        }

        #endregion

        #region Public Methods

        #region File Sharing

        //TODO: Implement:
        // CLFileItemLink CreatePublicShareLink(int timeToLiveMinutes, int downloadTimesLimit);

        #endregion  // end File Sharing
        #region File Download

        /// <summary>
        /// Download the file represented by this CLFileItem object.
        /// </summary>
        /// <returns>CLError: Any error, or null.</returns>
        public CLError DownloadFile()
        {
            return null;
        }

        #endregion  // end File Download

        #region Image Files

        //public CLError LoadThumbnail(out thumbnail, 

        

        #endregion  // end Image Files

        #region Folder Contents

        #endregion  // end Folder Contents
        #endregion  // end Public Methods
    }
}