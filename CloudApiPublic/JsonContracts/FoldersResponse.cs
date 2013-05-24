//
// Folders.cs
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
    /// Result from <see cref="Cloud.CLSyncbox.GetFolderHierarchy"/>
    /// </summary>
    [DataContract]
    public sealed class FoldersResponse
    {
        [DataMember(Name = CLDefinitions.CLMetadataFolders, IsRequired = false)]
        public JsonContracts.SyncboxMetadataResponse[] Metadata { get; set; }
    }
}