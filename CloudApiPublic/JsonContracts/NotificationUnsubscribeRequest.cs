//
// NotificationUnsubscribeRequest.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Send a /sync/notifications/Unsubscribe request.
    /// </summary>
    [DataContract]
    internal sealed class NotificationUnsubscribeRequest
    {
        [DataMember(Name = CLDefinitions.QueryStringSyncBoxId, IsRequired = false)]
        public Nullable<long> SyncBoxId { get; set; }

        [DataMember(Name = CLDefinitions.QueryStringDeviceId, IsRequired = false)]
        public string DeviceId { get; set; }
    }
}