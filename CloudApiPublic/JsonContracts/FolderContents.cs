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
using CloudApiPublic.Model;
using CloudApiPublic.Static;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Result from <see cref="CloudApiPublic.REST.CLHttpRest.GetFolderContents"/>
    /// </summary>
    [DataContract]
    internal /*public*/ sealed class FolderContents
    {
        [DataMember(Name = CLDefinitions.CLMetadataCount, IsRequired = false)]
        public Nullable<long> Count { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataObjects, IsRequired = false)]
        public Metadata[] Objects { get; set; }
    }
}