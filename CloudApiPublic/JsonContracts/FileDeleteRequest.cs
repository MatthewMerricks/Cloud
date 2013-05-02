using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    [DataContract]
    internal sealed class FileDeleteRequest
    {
        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }

        [DataMember(Name = CLDefinitions.RESTRequestFileOrFolderDeletes, IsRequired = false)]
        public string[] Deletes { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringSyncboxId, IsRequired = false)]
        public Nullable<long> SyncboxId { get; set; }
    }
}
