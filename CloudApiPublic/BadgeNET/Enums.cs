//
// Enums.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cloud.BadgeNET
{
    //!!!!!!!! Changes to the enumeration names below must be replicated to the appropriate portions of the named pipe names in BadgeIcon*.cpp files in project BadgeCOM, Source Files (i.e. pipeForCurrentBadgeType = L"\\\\.\\Pipe\\BadgeCOMcloudAppBadgeSyncSelective")
    /// <summary>
    /// Types of badges for icon overlays, view the associated comment before changing existing enumerated values
    /// </summary>
    [Obfuscation (Exclude=true)]
    internal enum cloudAppIconBadgeType : byte
    {
        cloudAppBadgeNone = 0, // clears a badge overlay, if any
        cloudAppBadgeSynced = 1, // sets a badge with a checkmark or similar metaphor.
        cloudAppBadgeSyncing = 2, // sets a badge indicating circular motion, active sync.
        cloudAppBadgeFailed = 3, // sets a badge with an x indicating failure to sync.
        cloudAppBadgeSyncSelective = 4, // sets a badge with an - indicating file/folder is selected not to sync.
    }
}
