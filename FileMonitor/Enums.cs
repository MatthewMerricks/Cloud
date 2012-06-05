﻿//
// Enums.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileMonitor
{
    [Flags]
    /// <summary>
    /// Flagged enumeration used to determine running status of FileMonitor;
    /// File watcher may be running, folder watcher may be running, or both/neither
    /// </summary>
    public enum MonitorRunning : byte
    {
        NotRunning = 0,
        FolderOnlyRunning = 1,
        FileOnlyRunning = 2,
        BothRunning = 3
    }

    /// <summary>
    /// Enumeration to provide information on the returns from starting or stopping the FileMonitor
    /// </summary>
    public enum MonitorStatus : byte
    {
        Started,
        AlreadyStarted,
        Stopped,
        AlreadyStopped
    }

    /// <summary>
    /// Enumeration to associate the type of event occurred for a FileChange (mutually exclusive)
    /// </summary>
    public enum FileChangeType : byte
    {
        Created,
        Modified,
        Deleted,
        Renamed,
        /// <summary>
        /// Denotes a creation event, but an error occurred while reading the file for an MD5 checksum
        /// </summary>
        CreatedWithError,
        /// <summary>
        /// Denotes a modified event, but an error occurred while reading the file for an MD5 checksum
        /// </summary>
        ModifiedWithError,
        /// <summary>
        /// Denotes a renamed event, but an error occurred while reading the file for an MD5 checksum
        /// </summary>
        RenamedWithError
    }
}