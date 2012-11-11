using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    [DataContract]
    public sealed class PutFileResponse
    {
        [DataMember(Name = "storage_key", IsRequired = false)]
        public string StorageKey { get; set; }
    }
}