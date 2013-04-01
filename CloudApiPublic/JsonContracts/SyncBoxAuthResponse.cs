//
// SyncBoxAuthResponse.cs
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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class SyncBoxAuthResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxAuth_Id, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxAuth_FriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}