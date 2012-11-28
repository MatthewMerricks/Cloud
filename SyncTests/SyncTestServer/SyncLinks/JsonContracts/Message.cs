//
// Message.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

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