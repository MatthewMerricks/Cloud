//
// SessionDeleteRequest.cs
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
    /// Result from <see cref="Cloud.CLCredential.DeleteSession"/>
    /// </summary>
    [DataContract]
    internal sealed class SessionDeleteRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestSession_Key, IsRequired = false)]
        public string Key { get; set; }
    }
}