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
    internal sealed class FileOrFolderDeletesRequest
    {
        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestFileOrFolderDeletes, IsRequired = false)]
        public FileOrFolderDeleteRequest [] Deletes { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringSyncboxId, IsRequired = false)]
        public Nullable<long> SyncboxId { get; set; }
    }
}