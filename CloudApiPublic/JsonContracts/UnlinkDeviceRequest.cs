//
// UnlinkDeviceRequest.cs
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
    /// Contains actual HTTP request fields, representing a request to unlink a device.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class UnlinkDeviceRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestUnlinkDevice_AccessToken, IsRequired = false)]
        public string AccessToken { get; set; }
    }
}