//
// PendingResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncbox.GetAllPending"/>
    /// </summary>
    [DataContract]
    public sealed class PendingResponse
    {
        [DataMember(Name = CLDefinitions.CLMetadataFiles, IsRequired = false)]
        public SyncboxMetadataResponse[] Files { get; set; }
    }
}