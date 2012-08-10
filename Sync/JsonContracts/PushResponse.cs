//
// PushResponse.cs
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

namespace Sync.JsonContracts
{
    [DataContract]
    public class PushResponse
    {
        [DataMember(Name = CLDefinitions.CLSyncEvents, IsRequired = false)]
        public Event[] Events { get; set; }

        [DataMember(Name = CLDefinitions.ResponsePendingCount, IsRequired = false)]
        public Nullable<int> PendingCount { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncID, IsRequired = false)]
        public string SyncId { get; set; }

        [DataMember(Name = CLDefinitions.ResponsePartial, IsRequired = false)]
        public Nullable<bool> PartialResponse {get;set;}
    }
}
