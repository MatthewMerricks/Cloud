﻿//
// SyncboxAddFilesResponse.cs
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
    /// Result from <see cref="Cloud.CLSyncbox.AddFiles"/>
    /// </summary>
    [DataContract]
    public sealed class SyncboxAddFilesResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseAddFiles, IsRequired = false)]
        public JsonContracts.FileChangeResponse[] AddResponses { get; set; }
    }
}