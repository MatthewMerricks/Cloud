//
// SyncboxUpdateStoragePlanRequest.cs
// Cloud Windows
//
// Created By BobS.
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
    /// <summary>
    /// Request to update the plan used by a sync box.
    /// </summary>
    [DataContract]
    internal sealed class SyncboxUpdateStoragePlanRequest
    {
        [DataMember(Name = CLDefinitions.QueryStringInsideSyncSyncbox_SyncboxId, IsRequired = false)]
        public long SyncboxId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSyncboxPlanId, IsRequired = false)]
        public long PlanId { get; set; }
    }
}