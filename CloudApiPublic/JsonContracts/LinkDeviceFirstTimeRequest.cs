//
// LinkDeviceFirstTimeRequest.cs
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
    /// Contains actual HTTP request fields, representing a request to link a device and a user (first-time).
    /// </summary>
    
    [Obfuscation(Exclude=true)]
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class LinkDeviceFirstTimeRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestLinkDeviceFirstTime_User, IsRequired = false)]
        public UserRegistrationRequest User { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDeviceFirstTime_Key, IsRequired = false)]
        public string Key { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDeviceFirstTime_Secret, IsRequired = false)]
        public string Secret { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDeviceFirstTime_Device, IsRequired = false)]
        public DeviceRequest Device { get; set; }
    }
}