//
// To.cs
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
    internal sealed class To
    {
        [DataMember(Name = CLDefinitions.CLSyncEvents)]
        public FileChangeResponse[] Events { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncID)]
        public string SyncId { get; set; }

        [DataMember(Name = CLDefinitions.JsonAccountFieldSyncboxId, IsRequired = false)]
        public Nullable<long> SyncboxId { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxQuota, IsRequired = false)]
        public SyncboxUsageResponse Quota { get; set; }
    }
}