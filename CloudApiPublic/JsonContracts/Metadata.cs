//
// Metadata.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CloudApiPublic.Model;
using CloudApiPublic.Static;

namespace CloudApiPublic.JsonContracts
{
    // removed "<see cref="PendingResponse"/>, " from the summary below because it's internal
    /// <summary>
    /// Result from <see cref="CloudApiPublic.CLSyncBox.GetMetadata"/>; also an inner object containing metadata properties for <see cref="Event"/>, <see cref="FolderContents"/>, <see cref="Folders"/>, <see cref="Pictures"/>, <see cref="Videos"/>, <see cref="Audios"/>, <see cref="Archives"/>, and <see cref="Recents"/>
    /// </summary>
    [DataContract]
    public sealed class Metadata
    {
        [DataMember(Name = CLDefinitions.CLMetadataServerId, IsRequired = false)]
        public string ServerId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataIsDeleted, IsRequired = false)]
        public Nullable<bool> Deleted { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataMimeType, IsRequired = false)]
        public string MimeType { get; set; }
        
        [DataMember(Name = CLDefinitions.CLMetadataCreateDate, IsRequired = false)]
        public string CreatedDateString
        {
            get
            {
                if (CreatedDate == null)
                {
                    return null;
                }
                return ((DateTime)CreatedDate).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    CreatedDate = null;
                }
                else
                {
                    DateTime tempCreatedDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    CreatedDate = ((tempCreatedDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempCreatedDate = tempCreatedDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempCreatedDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> CreatedDate { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataModifiedDate, IsRequired = false)]
        public string ModifiedDateString
        {
            get
            {
                if (ModifiedDate == null)
                {
                    return null;
                }
                return ((DateTime)ModifiedDate).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    ModifiedDate = null;
                }
                else
                {
                    DateTime tempModifiedDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    ModifiedDate = ((tempModifiedDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempModifiedDate = tempModifiedDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempModifiedDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> ModifiedDate { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataIcon, IsRequired = false)]
        public string Icon { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataLastEventID, IsRequired = false)]
        public Nullable<long> LastEventId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataIsStored, IsRequired = false)]
        public Nullable<bool> IsPending { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativePath { get; set; }

        public string RelativePathWithoutEnclosingSlashes
        {
            get
            {
                if (RelativePath == null)
                {
                    return null;
                }

                if (RelativePath[RelativePath.Length - 1] == '/')
                {
                    if (RelativePath[0] == '/')
                    {
                        return RelativePath.Substring(1, RelativePath.Length - (RelativePath.Length == 1 ? 1 : 2));
                    }
                    else
                    {
                        return RelativePath.Substring(0, RelativePath.Length - 1);
                    }
                }
                else if (RelativePath[0] == '/')
                {
                    return RelativePath.Substring(1, RelativePath.Length - 1);
                }

                return RelativePath;
            }
        }

        [DataMember(Name = CLDefinitions.CLMetadataFromPath, IsRequired = false)]
        public string RelativeFromPath { get; set; }

        public string RelativeFromPathWithoutEnclosingSlashes
        {
            get
            {
                if (RelativeFromPath == null)
                {
                    return null;
                }

                if (RelativeFromPath[RelativeFromPath.Length - 1] == '/')
                {
                    if (RelativeFromPath[0] == '/')
                    {
                        return RelativeFromPath.Substring(1, RelativeFromPath.Length - 2);
                    }
                    else
                    {
                        return RelativeFromPath.Substring(0, RelativeFromPath.Length - 1);
                    }
                }
                else if (RelativeToPath[0] == '/')
                {
                    return RelativeFromPath.Substring(1, RelativeFromPath.Length - 1);
                }

                return RelativeFromPath;
            }
        }

        [DataMember(Name = CLDefinitions.CLMetadataToPath, IsRequired = false)]
        public string RelativeToPath { get; set; }

        public string RelativeToPathWithoutEnclosingSlashes
        {
            get
            {
                if (RelativeToPath == null)
                {
                    return null;
                }

                if (RelativeToPath[RelativeToPath.Length - 1] == '/')
                {
                    if (RelativeToPath[0] == '/')
                    {
                        return RelativeToPath.Substring(1, RelativeToPath.Length - 2);
                    }
                    else
                    {
                        return RelativeToPath.Substring(0, RelativeToPath.Length - 1);
                    }
                }
                else if (RelativeToPath[0] == '/')
                {
                    return RelativeToPath.Substring(1, RelativeToPath.Length - 1);
                }

                return RelativeToPath;
            }
        }

        [DataMember(Name = CLDefinitions.CLMetadataIsDirectory, IsRequired = false)]
        public Nullable<bool> IsFolder { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileRevision, IsRequired = false)]
        public string Revision { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileHash, IsRequired = false)]
        public string Hash { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataStorageKey, IsRequired = false)]
        public string StorageKey { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileTarget, IsRequired = false)]
        public string TargetPath { get; set; }

        public string TargetPathWithoutEnclosingSlashes
        {
            get
            {
                if (TargetPath == null)
                {
                    return null;
                }

                if (TargetPath[TargetPath.Length - 1] == '/')
                {
                    if (TargetPath[0] == '/')
                    {
                        return TargetPath.Substring(1, TargetPath.Length - 2);
                    }
                    else
                    {
                        return TargetPath.Substring(0, TargetPath.Length - 1);
                    }
                }
                else if (TargetPath[0] == '/')
                {
                    return TargetPath.Substring(1, TargetPath.Length - 1);
                }

                return TargetPath;
            }
        }

        [DataMember(Name = CLDefinitions.CLMetadataVersion, IsRequired = false)]
        public string Version { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileSize, IsRequired = false)]
        public Nullable<long> Size { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataItemCount, IsRequired = false)]
        public ItemCount ItemCount { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataDeletedItemCount, IsRequired = false)]
        public ItemCount DeletedItemCount { get; set; }
    }
}