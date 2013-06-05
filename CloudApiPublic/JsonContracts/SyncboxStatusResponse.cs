//
// SyncboxStatusResponse.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Reflection;

namespace Cloud.JsonContracts
{
    // \cond
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncbox.BeginGetCurrentStatus"/>
    /// </summary>
    [DataContract]
    [ContainsMetadataDictionary] // within Syncbox Syncbox
    [Obfuscation(Feature = "preserve-name-binding")]
    public sealed class SyncboxStatusResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSyncbox, IsRequired = false)]
        public Syncbox Syncbox { get; set; }
    }
    // \endcond
}