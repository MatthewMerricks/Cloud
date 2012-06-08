//
// FileChange.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FileMonitor
{
    /// <summary>
    /// Class for storing information about a file system change to be passed to the sync service for processing,
    /// implements DelayProcessable to allow timer-delayed action processing (one time only per instance)
    /// </summary>
    public class FileChange : DelayProcessable<FileChange>
    {
        private static int eventIdCounter = 0;

        /// <summary>
        /// Current path associated with the file system event
        /// </summary>
        public string NewPath { get; set; }
        /// <summary>
        /// For rename events only, this is the old path
        /// </summary>
        public string OldPath { get; set; }
        /// <summary>
        /// Contains data used to compare the file or folder and establish its identity
        /// </summary>
        public FileMetadata Metadata { get; set; }
        /// <summary>
        /// Type of file system event
        /// </summary>
        public FileChangeType Type { get; set; }
        /// <summary>
        /// Event ID
        /// </summary>
        public int EventId
        {
            get
            {
                return eventIdCounter++;
            }
        }
    }
}