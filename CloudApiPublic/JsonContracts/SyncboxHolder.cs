//
// SyncboxHolder.cs
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
    /// Result from <see cref="Cloud.CLCredentials.AddSyncboxOnServer"/>, <see cref="Cloud.CLSyncbox.SyncboxUpdateExtendedMetadata"/>,
    /// <see cref="Cloud.CLSyncbox.DeleteSyncbox"/>, and
    /// <see cref="Cloud.CLSyncbox.GetSyncboxStatus"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary] // within Syncbox Syncbox
    public sealed class SyncboxResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncbox, IsRequired = false)]
        public Syncbox Syncbox { get; set; }
    }
}