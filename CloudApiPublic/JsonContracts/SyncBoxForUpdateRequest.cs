//
// SyncBoxForUpdateRequest.cs
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
    /// Inner request to change the properties of a sync box.
    /// </summary>
    [DataContract]
    public sealed class SyncBoxForUpdateRequest
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxFriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}