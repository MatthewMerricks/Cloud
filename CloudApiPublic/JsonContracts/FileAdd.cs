//
// FileAdd.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    [Obfuscation(Exclude = true)]
    [DataContract]
    internal sealed class FileAdd
    {
        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativePath { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileHash, IsRequired = false)]
        public string Hash { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileSize, IsRequired = false)]
        public Nullable<long> Size { get; set; }

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

        [DataMember(Name = CLDefinitions.QueryStringSyncboxId, IsRequired = false)]
        public Nullable<long> SyncboxId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataName, IsRequired = false)]
        public string Name { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataParentUid, IsRequired = false)]
        public string ParentUid { get; set; }
    }
}