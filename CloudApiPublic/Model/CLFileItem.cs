//
// CLFileItem.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;

namespace Cloud.Model
{
    /// <summary>
    /// Represents a Cloud file/folder item."/>
    /// </summary>
    public sealed class CLFileItem
    {
        #region Public Properties

        public string Name { get; set; }
        public string Path { get; set; }
        public string Revision { get; set; }
        public Nullable<long> Size { get; set; }
        public string MimeType { get; set; }
        public DateTime CreatedTime { get; set; }
        public Nullable<DateTime> ModifiedTime { get; set; }
        public string Uid { get; set; }
        public string ParentUid { get; set; }
        public bool IsFolder { get; set; }
        public bool IsDeleted { get; set; }
        public Nullable<POSIXPermissions> Permissions { get; set; }

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
            Nullable<POSIXPermissions> permissions)
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
        }

        internal CLFileItem(JsonContracts.SyncboxMetadataResponse response)
        {
            if (response == null)
            {
                throw new NullReferenceException("response must not be null");
            }
            if (response.CreatedDate == null)
            {
                throw new NullReferenceException("response CreatedDate must not be null");
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
        }

        #endregion
    }
}