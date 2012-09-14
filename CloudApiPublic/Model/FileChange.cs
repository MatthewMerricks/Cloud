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
        // If properties are changed/added/removed, make sure to update FileChangeWithDependencies in the Sync project!!

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
        public long EventId { get; set; }
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
        // a global counter which is interlocked-incremented everytime a FileChange is created
        private static long InMemoryIdCounter = 0;
        // the current FileChange's incremented id
        public long InMemoryId { get; private set; }

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
                md5 = Helpers.DefaultForType<byte[]>();
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
                md5 = Helpers.DefaultForType<string>();
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
        public byte FailureCounter = 0;
        public byte NotFoundForStreamCounter = 0;

        // If properties are changed/added/removed, make sure to update FileChangeWithDependencies in the Sync project!!

        /// <summary>
        /// Constructor with required fields of abstract base class,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="DelayCompletedLocker">Object to lock on to synchronize setting DelayCompleted boolean</param>
        public FileChange(object DelayCompletedLocker) : base(DelayCompletedLocker)
        {
            SetIncrementedId();
        }
        /// <summary>
        /// Constructor for an object to store parameters,
        /// but not be delay-processable
        /// </summary>
        public FileChange() : base()
        {
            SetIncrementedId();
        }

        // method which interlock-increments a static counter and sets the current object's in-memory id accordingly;
        // must be called from the constructor
        private void SetIncrementedId()
        {
            this.InMemoryId = Interlocked.Increment(ref InMemoryIdCounter);
        }

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

        public static void RunUnDownEvent(UpDownEventArgs callback)
        {
            lock (UpDownEventLocker)
            {
                if (UpDownEvent != null)
                {
                    UpDownEvent(null, callback);
                }
            }
        }
        public static event EventHandler<UpDownEventArgs> UpDownEvent;
        public static readonly object UpDownEventLocker = new object();
        public class UpDownEventArgs : EventArgs
        {
            public Action<FileChange> SendBackChange { get; private set; }

            public UpDownEventArgs(Action<FileChange> SendBackChange)
            {
                this.SendBackChange = SendBackChange;
            }
        }
        public void FileChange_UpDown(object sender, UpDownEventArgs e)
        {
            e.SendBackChange(this);
        }
    }
}