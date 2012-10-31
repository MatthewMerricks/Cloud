//
// ISyncSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Interfaces
{
    public interface ISyncSettings
    {
        /// <summary>
        /// Only required if LogErrors is set to true
        /// </summary>
        string ErrorLogLocation { get; }
        bool LogErrors { get; }
        TraceType TraceType { get; }
        /// <summary>
        /// Only required if TraceType has any flags set (TraceType.NotEnabled means no flags are set)
        /// </summary>
        string TraceLocation { get; }
        bool TraceExcludeAuthorization { get; }
        string Udid { get; }
        string Uuid { get; }
        string Akey { get; }
        /// <summary>
        /// If null, a precalculated value will be used based on the local, non-roaming user's application data in the Cloud subdirectory
        /// </summary>
        string TempDownloadFolderFullPath { get; }
        string ClientVersion { get; }
        string DeviceName { get; }
        string CloudRoot { get; }
    }
}