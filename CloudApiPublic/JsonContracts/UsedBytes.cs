//
// UsedBytes.cs
// Cloud Windows
//
// Created By DavidBruck.
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
    /// Result from <see cref="CloudApiPublic.REST.CLHttpRest.GetUsedBytes"/>
    /// </summary>
    [DataContract]
    internal /*public*/ sealed class UsedBytes
    {
        [DataMember(Name = CLDefinitions.CLSyncBoxStoredBytes, IsRequired = false)]
        public Nullable<long> StoredBytes { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncBoxPendingBytes, IsRequired = false)]
        public Nullable<long> PendingBytes { get; set; }
    }
}