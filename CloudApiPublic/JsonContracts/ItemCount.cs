//
// ItemCount.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CloudApiPublic.Model;

namespace CloudApiPublic.JsonContracts
{
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