//
// Download.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    [DataContract]
    internal sealed class Download
    {
        [DataMember(Name = CLDefinitions.CLMetadataStorageKey, IsRequired = false)]
        public string StorageKey { get; set; }
    }
}
