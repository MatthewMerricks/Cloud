//
// Header.cs
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
    /// An inner object containing header properties for an <see cref="Event"/>
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class Header
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
    }
}
