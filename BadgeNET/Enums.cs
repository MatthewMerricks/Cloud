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
    /// Types of badges for icon overlays
    /// </summary>
    public enum cloudAppIconBadgeType : byte
    {
        cloudAppBadgeNone = 0, // clears a badge overlay, if any
        cloudAppBadgeSynced = 1, // sets a badge with a checkmark or similar metaphor.
        cloudAppBadgeSyncing = 2, // sets a badge indicating circular motion, active sync.
        cloudAppBadgeFailed = 3, // sets a badge with an x indicating failure to sync.
        cloudAppBadgeSyncSelective = 4, // sets a badge with an - indicating file/folder is selected not to sync.
    }
}
