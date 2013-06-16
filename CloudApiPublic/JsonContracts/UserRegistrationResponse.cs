//
// UserResponse.cs
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
    /// Contains actual HTTP response fields, representing the response to the definition of a new user to the server.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class UserRegistrationResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseUserRegistration_Id, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_FirstName, IsRequired = false)]
        public string FirstName { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_LastName, IsRequired = false)]
        public string LastName { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestUserRegistration_EMail, IsRequired = false)]
        public string EMail { get; set; }
    }
}