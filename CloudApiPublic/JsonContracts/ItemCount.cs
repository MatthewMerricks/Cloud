//
// ItemCount.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

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
    /// An inner object containing item count properties for <see cref="SyncboxMetadataResponse"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    public sealed class ItemCount
    {
        [DataMember(Name = CLDefinitions.CLMetadataFiles, IsRequired = false)]
        public Nullable<long> FileCount { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataLinks, IsRequired = false)]
        public Nullable<long> LinkCount { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFolders, IsRequired = false)]
        public Nullable<long> FolderCount { get; set; }
    }
}