//
// Event.cs
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

namespace CloudApiPublic.JsonContracts
{
    [DataContract]
    public sealed class Event
    {
        [DataMember(Name = CLDefinitions.CLSyncEvent, IsRequired = false)]
        public string Action { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventMetadata, IsRequired = false)]
        public Metadata Metadata { get; set; }

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
                if (string.IsNullOrWhiteSpace(value))
                {
                    EventId = null;
                }
                else
                {
                    EventId = long.Parse(value);
                }
            }
        }
        public Nullable<long> EventId { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventHeader, IsRequired = false)]
        public Header Header { get; set; }
    }
}