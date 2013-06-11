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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Result from <see cref="Cloud.CLSyncbox.GetAllPending"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    public sealed class PendingResponse
    {
        [DataMember(Name = CLDefinitions.CLMetadataFiles, IsRequired = false)]
        internal SyncboxMetadataResponse[] Files { get; set; }
    }
}