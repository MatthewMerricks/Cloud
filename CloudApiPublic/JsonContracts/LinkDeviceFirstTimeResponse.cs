//
// LinkDeviceFirstTimeResponse.cs
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
    /// Contains actual HTTP response fields, representing a response to a request to link a device and a user (first-time).
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class LinkDeviceFirstTimeResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseLinkDeviceFirstTime_User, IsRequired = false)]
        public UserRegistrationResponse User { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseLinkDeviceFirstTime_SyncBox, IsRequired = false)]
        public SyncBoxAuthResponse SyncBox { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseLinkDeviceFirstTime_Session, IsRequired = false)]
        public Session Session { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseLinkDeviceFirstTime_Device, IsRequired = false)]
        public DeviceResponse Device { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseLinkDeviceFirstTime_AccessToken, IsRequired = false)]
        public string AccessToken { get; set; }
    }
}