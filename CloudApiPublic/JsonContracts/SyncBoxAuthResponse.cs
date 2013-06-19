//
// SyncboxAuthResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class SyncboxAuthResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncboxAuth_Id, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxAuth_FriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}