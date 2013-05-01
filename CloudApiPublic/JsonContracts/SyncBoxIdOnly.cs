//
// SyncboxIdOnly.cs
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
    internal sealed class SyncboxIdOnly
    {
        [DataMember(Name = CLDefinitions.QueryStringInsideSyncSyncbox_SyncboxId, IsRequired = false)]
        public Nullable<long> Id { get; set; }
    }
}