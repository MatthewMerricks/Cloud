//
// TraceType.cs
// Cloud
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Static
{
    /// <summary>
    /// Types of Trace Log entries; must match TraceLog.xsd
    /// </summary>
    [Flags]
    public enum TraceType : int
    {
        Communication = 0x01
    }
}