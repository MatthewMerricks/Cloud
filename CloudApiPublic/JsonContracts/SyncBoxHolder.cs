//
// SyncBoxHolder.cs
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
    /// <summary>
    /// Result from <see cref="Cloud.CLCredential.AddSyncBoxOnServer"/>, <see cref="Cloud.CLSyncBox.SyncBoxUpdateExtendedMetadata"/>,
    /// <see cref="Cloud.CLSyncBox.DeleteSyncBox"/>, and
    /// <see cref="Cloud.CLSyncBox.GetSyncBoxStatus"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary] // within SyncBox SyncBox
    public sealed class SyncBoxHolder
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBox, IsRequired = false)]
        public SyncBox SyncBox { get; set; }
    }
}