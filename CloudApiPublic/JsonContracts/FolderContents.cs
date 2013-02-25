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
    /// Result from <see cref="CloudApiPublic.CLSyncBox.GetFolderContents"/>
    /// </summary>
    [DataContract]
    public sealed class FolderContents
    {
        [DataMember(Name = CLDefinitions.CLMetadataTotalCount, IsRequired = false)]
        public Nullable<long> TotalCount { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataObjects, IsRequired = false)]
        public Metadata[] Objects { get; set; }
    }
}