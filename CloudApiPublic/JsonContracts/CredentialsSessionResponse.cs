//
// CredentialsSessionResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
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
    /// Result from <see cref="Cloud.CLCredentials.ListAllActiveSessionCredentials"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    internal sealed class CredentialsSessionResponse : ICredentialsSessionsResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession, IsRequired = false)]
        public Session Session { get; set; }

        Session[] ICredentialsSessionsResponse.Sessions
        {
            get
            {
                Session storeSession = this.Session;
                return (storeSession == null
                    ? new Session[0]
                    : new[] { storeSession });
            }
        }
    }
}