//
// Session.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Contains actual HTTP response fields, representing a session.
    /// </summary>
    [DataContract]
    public sealed class Session
    {
        [DataMember(Name = CLDefinitions.RESTResponseSession_SyncBoxIds, IsRequired = false)]
        public HashSet<long> SyncBoxIds { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession_Key, IsRequired = false)]
        public string Key { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession_Secret, IsRequired = false)]
        public string Secret { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession_Token, IsRequired = false)]
        public string Token { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession_AllowAll, IsRequired = false)]
        public bool AllowAll { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseSession_ExpiresAt, IsRequired = false)]
        public string ExpiresAtString
        {
            get
            {
                if (ExpiresAt == null)
                {
                    return null;
                }

                return ((DateTime)ExpiresAt).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    ExpiresAt = null;
                }
                else
                {
                    DateTime tempExpiresAtDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    ExpiresAt = ((tempExpiresAtDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempExpiresAtDate = tempExpiresAtDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempExpiresAtDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> ExpiresAt { get; set; }
    }
}