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
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;

namespace CloudApiPublic.Model
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
        /// For files which are valid shortcuts, this is the target of the shortcut
        /// </summary>
        public FilePath LinkTargetPath { get; set; }
        /// <summary>
        /// Revision from server to identify file change version
        /// </summary>
        public string Revision { get; set; }
        /// <summary>
        /// Direction of sync (Sync To or Sync From),
        /// defaults to "To"
        /// </summary>
        public SyncDirection Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
            }
        }
        private SyncDirection _direction = SyncDirection.To;
        /// <summary>
        /// Storage key to identify server location for MDS events
        /// </summary>
        public string StorageKey { get; set; }

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
        /// Returns the MD5 as a 16 length byte array, or null
        /// </summary>
        /// <param name="md5">Output MD5 bytes</param>
        /// <returns>Returns error in retrieving MD5, if any</returns>
        public CLError GetMD5Bytes(out byte[] md5)
        {
            try
            {
                md5 = this.MD5;
            }
            catch (Exception ex)
            {
                md5 = (byte[])Helpers.DefaultForType(typeof(byte[]));
                return ex;
            }
            return null;
        }
        /// <summary>
        /// Returns the MD5 as a lowercase hexadecimal string 32 characters long with no seperators, or null
        /// </summary>
        /// <param name="md5">Output MD5 string</param>
        /// <returns>Returns error in retrieving MD5, if any</returns>
        public CLError GetMD5LowercaseString(out string md5)
        {
            try
            {
                md5 = (this.MD5 == null
                    ? null
                    : this.MD5
                        .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                        .Aggregate((previousBytes, newByte) => previousBytes + newByte));
            }
            catch (Exception ex)
            {
                md5 = (string)Helpers.DefaultForType(typeof(string));
                return ex;
            }
            return null;
        }
        /// <summary>
        /// Sets the MD5 from a byte array or clears it from null;
        /// throws exception if MD5 byte array is not 16 length
        /// </summary>
        /// <param name="md5">MD5 bytes to set</param>
        /// <returns>Returns error in setting MD5, if any</returns>
        public CLError SetMD5(byte[] md5)
        {
            try
            {
                if (md5 != null
                    && md5.Length != 16)
                {
                    throw new Exception("MD5 must be 128 bits (a byte array of length 16)");
                }
                this.MD5 = md5;
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        private byte[] MD5 = null;

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

        /// <summary>
        /// Overriding ToString so that QuickWatch will show KeyValue pairs of FilePaths, FileChanges as { [Path], [File or folder] [ChangeType] }
        /// </summary>
        /// <returns>Returns the string of the type of change</returns>
        public override string ToString()
        {
            return (Metadata == null
                    ? string.Empty
                    : (Metadata.HashableProperties.IsFolder
                        ? "Folder "
                        : "File ")) +
                Type.ToString();
        }
    }
}