//
// SessionCreateRequest.cs
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
    /// Request to create a session.
    /// </summary>
    [DataContract]
    internal sealed class SessionCreateRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestSession_TokenDuration, IsRequired = false)]
        public Nullable<long> TokenDuration { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSession_SyncBoxIds, IsRequired = false)]
        public long[] SessionIds { get; set; }
    }
}