//
// SyncboxUpdateRequest.cs
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
    /// <summary>
    /// Request to update the properties of a sync box.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    internal sealed class SyncboxUpdateRequest
    {
        [DataMember(Name = CLDefinitions.QueryStringInsideSyncSyncbox_SyncboxId, IsRequired = false)]
        public long SyncboxId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSyncbox, IsRequired = false)]
        public SyncboxUpdateFriendlyNameRequest Syncbox { get; set; }
    }
}