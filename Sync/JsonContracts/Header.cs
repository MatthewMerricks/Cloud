﻿//
// Header.cs
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
    public class Header
    {
        [DataMember(Name = CLDefinitions.CLSyncEvent, IsRequired = false)]
        public string Event { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncID, IsRequired = false)]
        public string SyncId { get; set; }
    }
}
