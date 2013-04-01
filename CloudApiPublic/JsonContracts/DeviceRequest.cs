//
// DeviceRequest.cs
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

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Contains actual HTTP request fields, representing a request to link a device.
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class DeviceRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestDevice_FriendlyName, IsRequired = false)]
        public string FriendlyName { get; set; }
    }
}