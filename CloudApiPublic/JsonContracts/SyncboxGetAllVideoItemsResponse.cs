//
// SyncboxGetAllVideoItemsResponse.cs
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
    /// Result from <see cref="Cloud.CLSyncbox.GetVideos"/>
    /// </summary>
    [DataContract]
    public sealed class SyncboxGetAllVideoItemsResponse
    {
        [DataMember(Name = CLDefinitions.CLMetadataTotalCount, IsRequired = false)]
        public Nullable<long> TotalCount { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventMetadata, IsRequired = false)]
        public JsonContracts.SyncboxMetadataResponse[] Metadata { get; set; }
    }
}