//
// FileOrFolderMoves.cs
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
    [DataContract]
    internal sealed class FileOrFolderMoves
    {
        [DataMember(Name = CLDefinitions.RESTRequestFileOrFolderMoves, IsRequired = false)]
        public FileOrFolderMove [] Moves { get; set; }
    }
}