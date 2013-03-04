//
// FileCopy.cs
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
    [DataContract]
    internal sealed class FileCopy
    {
        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataServerId, IsRequired = false)]
        public string ServerId { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativePath { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataToPath, IsRequired = false)]
        public string RelativeToPath { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringSyncBoxId, IsRequired = false)]
        public Nullable<long> SyncBoxId { get; set; }
    }
}