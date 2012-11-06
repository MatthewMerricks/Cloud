using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    [DataContract]
    public sealed class Message
    {
        [DataMember(Name = "message", IsRequired = false)]
        public string Value { get; set; }
    }
}