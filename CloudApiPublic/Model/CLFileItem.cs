//
// CLFileItem.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;

namespace Cloud.Model
{
    /// <summary>
    /// Represents a Cloud file/folder item."/>
    /// </summary>
    public sealed class CLFileItem
    {
        #region Public Properties

        public long Id { get; set; }
        public string Name { get; set; }
        public long Tier { get; set; }
        public long ClientApplicationId { get; set; }
        public long BandwidthQuota { get; set; }
        public long StorageQuota { get; set; }
        public bool IsDefaultPlan { get; set; }
        public DateTime PlanCreatedAt { get; set; }
        public DateTime PlanUpdatedAt { get; set; }

        public bool IsValid
        {
            get
            {
                if (Id < 1)
                {
                    return false;
                }
                if (Name == null)
                {
                    return false;
                }
                if (Tier < 1)
                {
                    return false;
                }
                if (ClientApplicationId < 1)
                {
                    return false;
                }
                if (BandwidthQuota < 1)
                {
                    return false;
                }
                if (StorageQuota < 1)
                {
                    return false;
                }
                if (PlanCreatedAt == DateTime.MinValue)
                {
                    return false;
                }
                if (PlanUpdatedAt == DateTime.MinValue)
                {
                    return false;
                }

                return true;
            }
        }

        #endregion  // end Public Properties

        #region Constructors
        
        public CLFileItem(
            long id,
            string name,
            long tier,
            long clientApplicationId,
            long bandwidthQuota,
            long storageQuota,
            bool isDefaultPlan,
            DateTime planCreatedAt,
            DateTime planUpdatedAt)
        {
            Id = id;
            Name = name;
            Tier = tier;
            ClientApplicationId = clientApplicationId;
            BandwidthQuota = bandwidthQuota;
            StorageQuota = storageQuota;
            IsDefaultPlan = isDefaultPlan;
            PlanCreatedAt = planCreatedAt;
            PlanUpdatedAt = planUpdatedAt;
        }

        public CLFileItem(JsonContracts.CLFileItemResponse response)
        {
            Id = response.Id ?? -1;
            Name = response.Name;
            Tier = response.Tier ?? -1;
            ClientApplicationId = response.ClientApplicationId ?? -1;
            BandwidthQuota = response.BandwidthQuota ?? -1; ;
            StorageQuota = response.StorageQuota ?? -1;
            IsDefaultPlan = response.IsDefaultPlan ?? false;
            PlanCreatedAt = response.PlanCreatedAt ?? DateTime.MinValue;
            PlanUpdatedAt = response.PlanUpdatedAt ?? DateTime.MinValue;
        }

        #endregion
    }
}