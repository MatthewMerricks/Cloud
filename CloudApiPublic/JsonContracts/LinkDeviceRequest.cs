//
// LinkDeviceRequest.cs
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
    /// Contains actual HTTP request fields, representing a request to link a device and a user.
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class LinkDeviceRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestLinkDevice_EMail, IsRequired = false)]
        public string EMail { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDevice_Password, IsRequired = false)]
        public string Password { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDevice_Key, IsRequired = false)]
        public string Key { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDevice_Secret, IsRequired = false)]
        public string Secret { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestLinkDevice_Device, IsRequired = false)]
        public DeviceRequest Device { get; set; }
    }
}