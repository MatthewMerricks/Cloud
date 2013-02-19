//
// SessionDeleteRequest.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Result from <see cref="CloudApiPublic.CLCredential.DeleteSession"/>
    /// </summary>
    [DataContract]
    internal sealed class SessionDeleteRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestSession_Key, IsRequired = false)]
        public string Key { get; set; }
    }
}