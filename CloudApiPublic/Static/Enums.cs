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

namespace CloudApiPublic.Static
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
    /// Enumeration for direction of sync
    /// </summary>
    public enum SyncDirection : byte
    {
        To,
        From
        //¡¡Do not add a third enumeration since this enumeration is set based on a bit value SyncFrom in table Events in the database (which only has two values)!!
    }

    /// <summary>
    /// Enumeration to associate the type of event occurred for a FileChange (mutually exclusive)
    /// </summary>
    public enum FileChangeType : byte
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

    /// <summary>
    /// readonly fields holding constants related to files
    /// </summary>
    public static class FileConstants
    {
        public const long InvalidUtcTimeTicks = 504911232000000000; //number determined by practice
        public static readonly byte[] EmptyBuffer = new byte[0]; // empty buffer is used to complete an MD5 hash
        public const int BufferSize = 4096; //posts online seem to suggest between 1kb and 12kb is optimal for a FileStream buffer, 4kb seems commonly used
    }

    public enum EventMessageLevel
    {
        Minor = 0,
        Regular = 5,
        Important = 9
    }

    public enum EventHandledLevel
    {
        NothingFired = -1,
        FiredButNotHandled = 0,
        IsHandled = 1
    }

    public enum PathState
    {
        None,
        Synced,
        Syncing,
        Failed,
        Selective
    }

    /// <summary>
    /// Types of images to display next to a item in a growl message
    /// </summary>
    public enum EventMessageImage
    {
        /// <summary>
        /// Use nothing or something transparent as the image
        /// </summary>
        NoImage,

        /// <summary>
        /// Use something like an 'i' icon
        /// </summary>
        Informational,

        /// <summary>
        /// Use something like the failed badge icon
        /// </summary>
        Error,

        /// <summary>
        /// Use something like the syncing badge icon
        /// </summary>
        Busy,

        /// <summary>
        /// Use something like the synced badge icon
        /// </summary>
        Completion,

        /// <summary>
        /// Use something like the selective badge icon
        /// </summary>
        Inaction
    }
}