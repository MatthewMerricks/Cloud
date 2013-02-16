//
// SessionCreateRequest.cs
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
    /// Result from <see cref="CloudApiPublic.CLSyncBox.SyncBoxCreate"/>
    /// </summary>
    [DataContract]
    internal sealed class SessionCreateRequest
    {
        [DataMember(Name = CLDefinitions.RESTRequestSession_TokenDuration, IsRequired = false)]
        public Nullable<long> TokenDuration { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestSession_SyncBoxIds, IsRequired = false)]
        public HashSet<long> SessionIds { get; set; }
    }
}