//
// SyncBoxUsage.cs
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
    /// Result from <see cref="CloudApiPublic.CLSyncBox.GetSyncBoxUsage"/>
    /// </summary>
    [DataContract]
    public sealed class SyncBoxUsage
    {
        [DataMember(Name = CLDefinitions.CLMetadataLocal, IsRequired = false)]
        public Nullable<long> Local { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataShared, IsRequired = false)]
        public Nullable<long> Shared { get; set; }
    }
}