//
// SyncBoxUpdateRequest.cs
// Cloud Windows
//
// Created By BobS.
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
    /// Request to update the properties of a sync box.
    /// </summary>
    [DataContract]
    internal sealed class SyncBoxUpdateRequest
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxId, IsRequired = false)]
        public long SyncBoxId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSyncBox, IsRequired = false)]
        public SyncBoxForUpdateRequest SyncBox { get; set; }
    }
}