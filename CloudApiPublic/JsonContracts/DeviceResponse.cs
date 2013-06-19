//
// DeviceResponse.cs
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
    /// Contains actual HTTP response fields, representing the response to a request to link a device.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class DeviceResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseDevice_DeviceUuid, IsRequired = false)]
        public string DeviceUuid { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseDevice_FriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}