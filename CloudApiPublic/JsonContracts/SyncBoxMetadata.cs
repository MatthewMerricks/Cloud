//
// SyncboxMetadata.cs
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
    /// Result from <see cref="Cloud.CLSyncbox.SyncboxUpdateExtendedMetadata"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    [ContainsMetadataDictionary]
    internal sealed class SyncboxMetadata
    {
        [DataMember(Name = CLDefinitions.QueryStringInsideSyncSyncbox_SyncboxId, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxMetadata, IsRequired = false)]
        public MetadataDictionary Metadata { get; set; }
    }
}