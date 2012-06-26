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
        /// <summary>
        /// Current path associated with the file system event
        /// </summary>
        public FilePath NewPath { get; set; }
        /// <summary>
        /// For rename events only, this is the old path
        /// </summary>
        public FilePath OldPath { get; set; }
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
        public int EventId { get; set; }

        /// <summary>
        /// Boolean set when already indexed events are requeued in the FileMonitor,
        /// defaults to false
        /// </summary>
        public bool DoNotAddToSQLIndex
        {
            get
            {
                return _doNotAddToSQLIndex;
            }
            set
            {
                _doNotAddToSQLIndex = value;
            }
        }
        private bool _doNotAddToSQLIndex = false;

        /// <summary>
        /// Constructor with required fields of abstract base class,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="DelayCompletedLocker">Object to lock on to synchronize setting DelayCompleted boolean</param>
        public FileChange(object DelayCompletedLocker) : base(DelayCompletedLocker) { }
        /// <summary>
        /// Constructor for an object to store parameters,
        /// but not be delay-processable
        /// </summary>
        public FileChange() : base() { }
    }
}