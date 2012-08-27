//
// PurgePending.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CloudApiPublic.Model;

namespace Sync.JsonContracts
{
    [DataContract]
    public class PurgePending
    {
        [DataMember(Name = CLDefinitions.QueryStringUserId, IsRequired = false)]
        public string UserId { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringDeviceUUId, IsRequired = false)]
        public string DeviceId { get; set; }
    }
}