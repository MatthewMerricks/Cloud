//
// ListSessionsResponse.cs
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
    /// Result from <see cref="Cloud.CLCredential.ListSesions"/>
    /// </summary>
    [DataContract]
    public sealed class ListSessionsResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession_Sessions, IsRequired = false)]
        public Session[] Sessions { get; set; }
    }
}