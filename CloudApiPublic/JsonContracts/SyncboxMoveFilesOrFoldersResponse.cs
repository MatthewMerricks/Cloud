//
// SyncboxMoveFilesResponse.cs
// Cloud Windows
//
// Created By BobS.
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
    /// Result from <see cref="Cloud.CLSyncbox.RenameFiles"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]
    public sealed class SyncboxMoveFilesOrFoldersResponse
    {
        [DataMember(Name = CLDefinitions.RESTResponseMoveFilesOrFolders, IsRequired = false)]
        public JsonContracts.FileChangeResponse[] MoveResponses { get; set; }
    }
}