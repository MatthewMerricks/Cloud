//
// SyncBoxUpdatePlanRequest.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Result from <see cref="CloudApiPublic.CLSyncBox.SyncBoxUpdatePlan"/>
    /// </summary>
    [DataContract]
    internal sealed class SyncBoxUpdatePlanRequest
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxId, IsRequired = false)]
        public long SyncBoxId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSyncBoxPlanId, IsRequired = false)]
        public long PlanId { get; set; }
    }
}