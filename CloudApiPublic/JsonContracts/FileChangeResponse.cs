//
// FileChangeResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
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
    /// Result from <see cref="Cloud.CLSyncbox.SendFileChange"/>, <see cref="Cloud.REST.CLHttpRest.UndoDeletionFileChange"/>, and <see cref="Cloud.REST.CLHttpRest.CopyFile"/>
    /// </summary>
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class FileChangeResponse
    {
        [DataMember(Name = CLDefinitions.CLSyncEvent, IsRequired = false)]
        public string Action { get; set; }

        [DataMember(Name = CLDefinitions.CLSyncEventMetadata, IsRequired = false)]
        public SyncboxMetadataResponse Metadata { get; set; }

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