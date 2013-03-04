//
// FileVersion.cs
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
    /// Result (in array format) from <see cref="Cloud.CLSyncBox.GetFileVersions"/>
    /// </summary>
    [DataContract]
    public sealed class FileVersion
    {
        [DataMember(Name = CLDefinitions.CLMetadataFileCAttributes, IsRequired = false)]
        public string CustomAttributes { get; set; }

        [DataMember(Name = CLDefinitions.CLEventKey, IsRequired = false)]
        public Nullable<long> ServerEventId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileHash, IsRequired = false)]
        public string FileHash { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileSize, IsRequired = false)]
        public Nullable<long> FileSize { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFullStorageKey, IsRequired = false)]
        public string StorageKey { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataIsDeleted, IsRequired = false)]
        public Nullable<bool> IsDeleted { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataVersion, IsRequired = false)]
        public Nullable<int> Version { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataIsStored, IsRequired = false)]
        public Nullable<bool> IsNotPending { get; set; }
    }
}