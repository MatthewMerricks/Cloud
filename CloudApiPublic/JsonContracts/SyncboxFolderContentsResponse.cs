//
// FolderContents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Cloud.Model;
using Cloud.Static;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncbox.ItemsForPath"/>
    /// </summary>
    [DataContract]
    public sealed class SyncboxFolderContentsResponse
    {
        [DataMember(Name = CLDefinitions.CLMetadataTotalCount, IsRequired = false)]
        public Nullable<long> TotalCount { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataObjects, IsRequired = false)]
        public SyncboxMetadataResponse[] Objects { get; set; }
    }
}