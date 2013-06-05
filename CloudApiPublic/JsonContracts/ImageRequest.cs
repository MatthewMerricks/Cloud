//
// ImageRequest.cs
// Cloud Windows
//
// Created By BobS.
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
    /// Contains actual HTTP request fields, representing a request for an image file.
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class ImageRequest
    {
        [DataMember(Name = CLDefinitions.CLMetadataFileDownloadServerUid, IsRequired = false)]
        public string ServerUid { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileRevision, IsRequired = false)]
        public string Revision { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringSyncboxId, IsRequired = false)]
        public Nullable<long> SyncboxId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataGenerate, IsRequired = false)]
        public string Generate { get; set; }
    }
}