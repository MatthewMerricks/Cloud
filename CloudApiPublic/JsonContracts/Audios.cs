//
// Audios.cs
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
    /// Result from <see cref="CloudApiPublic.CLSyncBox.GetAudios"/>
    /// </summary>
    [DataContract]
    public sealed class Audios
    {
        [DataMember(Name = CLDefinitions.CLMetadataCount, IsRequired = false)]
        public Nullable<long> Count { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventMetadata, IsRequired = false)]
        public JsonContracts.Metadata[] Metadata { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataMoreItems, IsRequired = false)]
        public Nullable<bool> MoreItems { get; set; }
    }
}