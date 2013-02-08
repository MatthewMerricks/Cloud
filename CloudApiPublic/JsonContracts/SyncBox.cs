﻿//
// SyncBox.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Contains actual response properties for <see cref="CreateSyncBox"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary]
    public sealed class SyncBox
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxId, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxStorageQuota, IsRequired = false)]
        public Nullable<long> StorageQuota { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxCreatedAt, IsRequired = false)]
        public string CreatedAtString
        {
            get
            {
                if (CreatedAt == null)
                {
                    return null;
                }

                return ((DateTime)CreatedAt).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    CreatedAt = null;
                }
                else
                {
                    DateTime tempCreatedDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    CreatedAt = ((tempCreatedDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempCreatedDate = tempCreatedDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempCreatedDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> CreatedAt { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxFriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxMetadata, IsRequired = false)]
        public MetadataDictionary Metadata { get; set; }
    }
}