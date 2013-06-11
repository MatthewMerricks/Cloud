//
// SyncboxQuota.cs
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
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncbox.UpdateSyncboxQuota"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    internal sealed class SyncboxQuota
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncboxId, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxStorageQuotaUpdateOnly, IsRequired = false)]
        public Nullable<long> StorageQuota { get; set; }
    }
}