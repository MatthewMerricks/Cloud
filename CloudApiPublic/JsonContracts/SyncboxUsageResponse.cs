﻿//
// SyncboxUsageResponse.cs
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
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncbox.Usage"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    public sealed class SyncboxUsageResponse
    {
        [DataMember(Name = CLDefinitions.CLMetadataLocal, IsRequired = false)]
        public Nullable<long> Local { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataShared, IsRequired = false)]
        public Nullable<long> Shared { get; set; }

        [DataMember(Name = CLDefinitions.CLMetadataLimit, IsRequired = false)]
        public Nullable<long> Limit { get; set; }
    }
}