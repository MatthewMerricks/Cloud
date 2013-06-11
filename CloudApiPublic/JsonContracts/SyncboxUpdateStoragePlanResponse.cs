﻿//
// SyncboxUpdatePlanResponse.cs
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
    /// Result from <see cref="Cloud.CLSyncbox.UpdateStoragePlan"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    [ContainsMetadataDictionary] // within Syncbox Syncbox
    public sealed class SyncboxUpdateStoragePlanResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan, IsRequired = false)]
        public StoragePlanResponse Plan { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncbox, IsRequired = false)]
        public Syncbox Syncbox { get; set; }
    }
}