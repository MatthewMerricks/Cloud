//
// SseResponse.cs
// Cloud Windows
//
// Created By BobS.
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
    internal sealed class SseResponse
    {
        //TODO: All of these are wrong.  Fix them for what is required to hold each of the possible SSE response commands.
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
