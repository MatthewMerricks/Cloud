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
    public enum BadgeType : byte
    {
        Synced = 1,
        Syncing = 2,
        Failed = 3,
        Selective = 4
    }
}
