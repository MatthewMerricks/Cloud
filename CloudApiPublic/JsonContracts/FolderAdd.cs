//
// FolderAdd.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    [DataContract]
    internal sealed class FolderAdd
    {
        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        // left out folder modified date since it is not a real field in Windows

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

        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativePath { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringSyncBoxId, IsRequired = false)]
        public Nullable<long> SyncBoxId { get; set; }
    }
}