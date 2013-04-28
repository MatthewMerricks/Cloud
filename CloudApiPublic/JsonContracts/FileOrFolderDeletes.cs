//
// FileOrFolderDeletes.cs
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
    internal sealed class FileOrFolderDeletes
    {
        [DataMember(Name = CLDefinitions.RESTRequestFileOrFolderDeletes, IsRequired = false)]
        public FileOrFolderDelete [] Deletes { get; set; }
    }
}