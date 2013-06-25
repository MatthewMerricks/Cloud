//
// SyncboxStatusRequest.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
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
    internal sealed class SyncboxStatusRequest
    {
        [DataMember(Name = CLDefinitions.QueryStringInsideSyncSyncbox_SyncboxId, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncboxIncludeUsage, IsRequired = false)]
        public bool IncludeUsage { get; set; }
    }
}