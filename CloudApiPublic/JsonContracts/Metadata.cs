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
    [DataContract]
    public sealed class Metadata
    {
        [DataMember(Name = CLDefinitions.CLMetadataFileIsDeleted, IsRequired = false)]
        public Nullable<bool> Deleted { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataMimeType, IsRequired = false)]
        public string MimeType { get; set; }
        
        [DataMember(Name = CLDefinitions.CLMetadataFileCreateDate, IsRequired = false)]
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
                if (value == null)
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

        [DataMember(Name = CLDefinitions.CLMetadataFileModifiedDate, IsRequired = false)]
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
                if (value == null)
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

        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativePath { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFromPath, IsRequired = false)]
        public string RelativeFromPath { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataToPath, IsRequired = false)]
        public string RelativeToPath { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileIsDirectory, IsRequired = false)]
        public Nullable<bool> IsFolder { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileRevision, IsRequired = false)]
        public string Revision { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileHash, IsRequired = false)]
        public string Hash { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataStorageKey, IsRequired = false)]
        public string StorageKey { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileTarget, IsRequired = false)]
        public string TargetPath { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataVersion, IsRequired = false)]
        public string Version { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileSize, IsRequired = false)]
        public Nullable<long> Size { get; set; }
    }
}