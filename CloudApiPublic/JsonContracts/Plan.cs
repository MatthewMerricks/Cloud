//
// Plan.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Contains actual response properties for <see cref="ListPlansResponse"/>
    /// </summary>
    [DataContract]
    public sealed class Plan
    {
        [DataMember(Name = CLDefinitions.RESTResponsePlan_Id, IsRequired = false)]
        public Nullable<long> Id { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_PlanName, IsRequired = false)]
        public string FriendlyPlanName { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_ApplicationPlanTierId, IsRequired = false)]
        public Nullable<long> ApplicationPlanTierId { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_ClientApplicationId, IsRequired = false)]
        public Nullable<long> ClientApplicationId { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_MaxTransferBytes, IsRequired = false)]
        public Nullable<long> MaxTransferBytes { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_IsDefault, IsRequired = false)]
        public Nullable<bool> IsDefault { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_CreatedAt, IsRequired = false)]
        public string CreatedAtString
        {
            get
            {
                if (PlanCreatedAt == null)
                {
                    return null;
                }

                return ((DateTime)PlanCreatedAt).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PlanCreatedAt = null;
                }
                else
                {
                    DateTime tempCreatedDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    PlanCreatedAt = ((tempCreatedDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempCreatedDate = tempCreatedDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempCreatedDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> PlanCreatedAt { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponsePlan_UpdatedAt, IsRequired = false)]
        public string UpdatedAtString
        {
            get
            {
                if (PlanUpdatedAt == null)
                {
                    return null;
                }

                return ((DateTime)PlanUpdatedAt).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PlanUpdatedAt = null;
                }
                else
                {
                    DateTime tempUpdatedDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    PlanUpdatedAt = ((tempUpdatedDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempUpdatedDate = tempUpdatedDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempUpdatedDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> PlanUpdatedAt { get; set; }
    }
}