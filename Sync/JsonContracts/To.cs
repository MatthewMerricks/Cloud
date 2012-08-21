//
// To.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sync.JsonContracts
{
    [DataContract]
    public class To
    {
        [DataMember(Name = CLDefinitions.CLSyncEvents)]
        public Event[] Events { get; set; }
        [DataMember(Name = CLDefinitions.CLSyncID)]
        public string SyncId { get; set; }
    }
}