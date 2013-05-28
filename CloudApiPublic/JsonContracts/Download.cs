//
// Download.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    [DataContract]
    internal sealed class Download
    {
        [DataMember(Name = CLDefinitions.CLMetadataStorageKey, IsRequired = false)]
        public string StorageKey { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileDownloadServerUid, IsRequired = false)]
        public string Uid { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataFileRevision, IsRequired = false)]
        public string Revision { get; set; }
    }
}
