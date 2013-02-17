//
// SessionDeleteResponse.cs
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
    public sealed class SessionDeleteResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }
    }
}