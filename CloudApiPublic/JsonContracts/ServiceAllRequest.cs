//
// ServiceAllRequest.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Contains actual HTTP response fields, representing a request for a service that supports all syncboxes.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    public sealed class ServiceAllRequest
    {
        [DataMember(Name = CLDefinitions.RESTResponseSession_SyncboxIds, IsRequired = false)]
        public string SyncboxIds { get; set; }

        [DataMember(Name = CLDefinitions.JsonServiceType, IsRequired = false)]
        public string ServiceType { get; set; }
    }
}