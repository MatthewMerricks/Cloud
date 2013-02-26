//
// ListSyncBoxes.cs
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
    /// Result from <see cref="Cloud.CLCredential.ListSyncBoxes"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary] // within SyncBox[] SyncBoxes
    public sealed class ListSyncBoxes
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncBoxes, IsRequired = false)]
        public SyncBox[] SyncBoxes { get; set; }
    }
}