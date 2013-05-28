//
// FileAdds.cs
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
    internal sealed class FileAdds
    {
        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringSyncboxId, IsRequired = false)]
        public Nullable<long> SyncboxId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestFileAdds, IsRequired = false)]
        public FileAdd [] Adds { get; set; }
    }
}