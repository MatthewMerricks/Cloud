//
// Header.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CloudApiPublic.Model;

namespace Sync.JsonContracts
{
    [DataContract]
    public class Header
    {
        [DataMember(Name = CLDefinitions.CLSyncEvent, IsRequired = false)]
        public string Action { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventStatus, IsRequired = false)]
        public string Status { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventID, IsRequired = false)]
        public string SyncEventId { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncID, IsRequired = false)]
        public string SyncId { get; set; }

        [DataMember(Name = CLDefinitions.CLClientEventId, IsRequired = false)]
        public string EventIdString
        {
            get
            {
                if (EventId == null)
                {
                    return null;
                }
                return ((long)EventId).ToString();
            }
            set
            {
                if (value == null)
                {
                    EventId = null;
                }
                EventId = long.Parse(value);
            }
        }
        public Nullable<long> EventId { get; set; }
    }
}
