//
// CLFileItem.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

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
        public long Size { get; set; }
        public string MimeType { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string Uid { get; set; }
        public string ParentUid { get; set; }
        public bool IsFolder { get; set; }

        #endregion  // end Public Properties

        #region Constructors
        
        public CLFileItem(
            string name,
            string path,
            string revision,
            long size,
            string mimeType,
            DateTime createdTime,
            DateTime modifiedTime,
            string uid,
            string parentUid,
            bool isFolder)
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
        }

        public CLFileItem(JsonContracts.Metadata response)
        {
            if (response == null)
            {
                throw new NullReferenceException("response must not be null");
            }
            if (response.Size == null)
            {
                throw new NullReferenceException("size must not be null");
            }

            this.Name = response.Name;
            this.Path = response.RelativePath;
            this.Revision = response.Revision;
            this.Size = (long)response.Size;
            this.MimeType = response.MimeType;
            this.CreatedTime = response.CreatedDate ?? DateTime.MinValue;
            this.ModifiedTime = response.ModifiedDate ?? DateTime.MinValue;
            this.Uid = response.ServerUid;
            this.ParentUid = response.ParentUid;
            this.IsFolder = response.IsFolder ?? false;
        }

        #endregion
    }
}