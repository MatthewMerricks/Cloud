//
// UserRegistrationRequest.cs
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
    /// Contains actual HTTP request fields, representing the definition of a new user to the server.
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class UserRegistrationRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_FirstName, IsRequired = false)]
        public string FirstName { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_LastName, IsRequired = false)]
        public string LastName { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_EMail, IsRequired = false)]
        public string EMail { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_Password, IsRequired = false)]
        public string Password { get; set; }
    }
}