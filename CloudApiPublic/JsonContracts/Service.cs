//
// Service.cs
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
    /// Contains actual HTTP response fields, representing a service.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    public sealed class Service
    {
        [DataMember(Name = CLDefinitions.RESTResponseSession_SyncboxIds, IsRequired = false)]
        public long[] SyncboxIds { get; set; }

        [DataMember(Name = CLDefinitions.JsonServiceType, IsRequired = false)]
        public string ServiceType { get; set; }
    }
}