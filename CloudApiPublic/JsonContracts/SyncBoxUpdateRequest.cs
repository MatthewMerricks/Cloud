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
    /// Result from <see cref="CloudApiPublic.CLSyncBox.SyncBoxUpdate"/>
    /// </summary>
    [DataContract]
    internal sealed class SyncBoxUpdateRequest
    {
        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxId, IsRequired = false)]
        public long SyncBoxId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSyncBoxFriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}