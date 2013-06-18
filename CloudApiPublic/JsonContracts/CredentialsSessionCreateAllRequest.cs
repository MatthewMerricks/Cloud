//
// SessionCreateAllRequest.cs
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
    /// Request to create a session and associate all of the application's sync boxes.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    
    internal sealed class CredentialsSessionCreateAllRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestSession_Services, IsRequired = false)]
        public ServiceAllRequest Service { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSession_TokenDuration, IsRequired = false)]
        public Nullable<long> TokenDuration { get; set; }
    }
}