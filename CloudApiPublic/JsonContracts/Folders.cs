//
// Folders.cs
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
    /// Result from <see cref="CloudApiPublic.REST.CLHttpRest.GetFolderHierarchy"/>
    /// </summary>
    [DataContract]
    public sealed class Folders
    {
        [DataMember(Name = CLDefinitions.CLMetadataCount, IsRequired = false)]
        public Nullable<long> Count { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFolders, IsRequired = false)]
        public JsonContracts.Metadata[] Metadata { get; set; }
    }
}