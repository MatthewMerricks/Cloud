//
// PurgePending.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    [DataContract]
    internal sealed class PurgePending
    {
        [DataMember(Name = CLDefinitions.QueryStringSyncBoxId, IsRequired = false)]
        public Nullable<long> SyncBoxId { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }
    }
}