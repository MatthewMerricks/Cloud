//
// SyncBoxUpdatePlanResponse.cs
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
    /// Result from <see cref="Cloud.CLSyncBox.UpdateSyncBoxPlan"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary] // within SyncBox SyncBox
    public sealed class SyncBoxUpdatePlanResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan, IsRequired = false)]
        public Plan Plan { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBox, IsRequired = false)]
        public SyncBox SyncBox { get; set; }
    }
}