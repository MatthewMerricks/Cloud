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
        NotEnabled = 0x00,
        Communication = 0x01,
        /// <summary>
        /// AddAuthorization is only valid in conjunction with Communication (CommunicationIncludeAuthorization)
        /// </summary>
        AddAuthorization = 0x02,
        CommunicationIncludeAuthorization = 0x03,
        FileChangeFlow = 0x04
    }
}