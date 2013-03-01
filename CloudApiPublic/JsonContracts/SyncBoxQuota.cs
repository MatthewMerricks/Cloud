﻿//
// SyncBoxQuota.cs
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
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncBox.UpdateSyncBoxQuota"/>
    /// </summary>
    [DataContract]
    internal sealed class SyncBoxQuota
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxId, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxStorageQuotaUpdateOnly, IsRequired = false)]
        public Nullable<long> StorageQuota { get; set; }
    }
}