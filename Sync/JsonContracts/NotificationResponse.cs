//
// NotificationResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sync.JsonContracts
{
    [DataContract]
    public class NotificationResponse
    {
        [DataMember(Name = CLDefinitions.NotificationMessageBody, IsRequired = false)]
        public string Body { get; set; }

        [DataMember(Name = CLDefinitions.NotificationMessageAuthor, IsRequired = false)]
        public string Author { get; set; }
    }
}
