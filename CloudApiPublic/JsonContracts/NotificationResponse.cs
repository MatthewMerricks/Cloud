//
// NotificationResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Notification from server contained in EventArgs: <see cref="CloudApiPublic.PushNotification.NotificationEventArgs"/> via events in <see cref="CloudApiPublic.PushNotification.CLNotification"/>
    /// </summary>
    [DataContract]
    public sealed class NotificationResponse
    {
        [DataMember(Name = CLDefinitions.NotificationMessageBody, IsRequired = false)]
        public string Body { get; set; }

        [DataMember(Name = CLDefinitions.NotificationMessageAuthor, IsRequired = false)]
        public string Author { get; set; }
    }
}
