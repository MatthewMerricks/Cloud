//
// PushResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    [Obfuscation(Exclude = true)]
    [DataContract]
    internal sealed class PushResponse
    {
        [DataMember(Name = CLDefinitions.CLSyncEvents, IsRequired = false)]
        public FileChangeResponse[] Events { get; set; }

        [DataMember(Name = CLDefinitions.ResponsePendingCount, IsRequired = false)]
        public Nullable<int> PendingCount { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncID, IsRequired = false)]
        public string SyncId { get; set; }

        [DataMember(Name = CLDefinitions.ResponsePartial, IsRequired = false)]
        public Nullable<bool> PartialResponse {get;set;}

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxQuota, IsRequired = false)]
        public SyncboxUsageResponse Quota { get; set; }
    }
}
