//
// SyncboxCreateRequest.cs
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
    /// <summary>
    /// Contains properties for a syncbox create request.
    /// </summary>
    [DataContract]
    public sealed class SyncboxCreateRequest
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncboxPlanId, IsRequired = false)]
        public Nullable<long> PlanId { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxFriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}