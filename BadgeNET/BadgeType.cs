//
// BadgeType.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BadgeNET
{
    /// <summary>
    /// Type of badges for icon overlays, access via inner static object instances
    /// </summary>
    public struct BadgeType
    {
        public string BadgeTypeName
        {
            get
            {
                return _badgeTypeName;
            }
        }
        private string _badgeTypeName;

        public static readonly BadgeType Syncing = new BadgeType("Syncing");
        public static readonly BadgeType Synced = new BadgeType("Synced");
        public static readonly BadgeType Selective = new BadgeType("Selective");
        public static readonly BadgeType Failed = new BadgeType("Failed");

        private BadgeType(string badgeTypeName)
        {
            this._badgeTypeName = badgeTypeName;
        }

        public override string ToString()
        {
            return this.BadgeTypeName;
        }
    }
}
