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

namespace Cloud.Static
{
    /// <summary>
    /// Types of Trace Log entries (flags); must match TraceLog.xsd
    /// </summary>
    [Flags]
    public enum TraceType : int
    {
        /// <summary>
        /// No general trace (errors are handled seperately)
        /// </summary>
        NotEnabled = 0x00,
        /// <summary>
        /// Communication tracing estimates the HTTP headers and body content during communication;
        /// when used without the AddAuthorization flag, authorization parameters will be exluded such as authentication tokens or user/pass
        /// </summary>
        Communication = 0x01,
        /// <summary>
        /// AddAuthorization is only valid in conjunction with Communication (CommunicationIncludeAuthorization);
        /// Adding this will cause authorization parameters to appear in communication trace such as authentication tokens or user/pass (in plain text!!!)
        /// </summary>
        AddAuthorization = 0x02,
        /// <summary>
        /// See flags Communication and AddAuthorization tags for this combination flag
        /// </summary>
        CommunicationIncludeAuthorization = 0x03,
        /// <summary>
        /// Logging the flow of FileChanges is extremely costly, use for development purposes only
        /// </summary>
        FileChangeFlow = 0x04
    }
}