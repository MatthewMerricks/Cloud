//
// ListSyncboxes.cs
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
    /// Result from <see cref="Cloud.CLCredential.ListSyncboxes"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary] // within Syncbox[] Syncboxes
    public sealed class ListSyncboxes
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncboxes, IsRequired = false)]
        public Syncbox[] Syncboxes { get; set; }
    }
}