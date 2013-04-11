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
using Cloud.Model;
using Cloud.Static;
using Cloud.Support;

namespace Cloud.Model
{
    /// <summary>
    /// Class for storing information about a file system change to be passed to the sync service for processing,
    /// implements DelayProcessable to allow timer-delayed action processing (one time only per instance)
    /// </summary>
    public class FileChange : DelayProcessable<FileChange>
    {
        #region special handling for a file download dependent on a rename
        internal readonly object fileDownloadMoveLocker;
        #endregion

        // If properties are changed/added/removed, make sure to update FileChangeWithDependencies in the Sync project!!

        /// <summary>
        /// Current path associated with the file system event
        /// </summary>
        public FilePath NewPath
        {
            get
            {
                return _newPath;
            }
            set
            {
                _newPath = value;
            }
        }
        private volatile FilePath _newPath = null;

        ///// <summary>
        ///// Server-mapped path of file system object for use in communication to and from the server
        ///// </summary>
        //public FilePath ServerNewPath { get; set; }
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
        internal bool PreviouslyModified { get; set; }
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
        internal long InMemoryId { get; private set; }

        /// <summary>
        /// Boolean set when already indexed events are requeued in the FileMonitor,
        /// defaults to false
        /// </summary>
        internal bool DoNotAddToSQLIndex
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
        /// Sets the MD5 from a byte array or clears it for null;
        /// returns an error if MD5 byte array is not 16 length
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
        /// <summary>
        /// Sets the MD5 from a 32 character hexadecimal string or clears it for a null/empty input;
        /// returns an error if the string is improperly formatted or if the resulting parsed byte array is not 16 length
        /// </summary>
        /// <param name="hashString">MD5 hexadecimal string to set</param>
        /// <returns>Returns error in setting MD5, if any</returns>
        public CLError SetMD5(string hashString)
        {
            try
            {
                return SetMD5(Helpers.ParseHexadecimalStringToByteArray(hashString));
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        private byte[] MD5 = null;
        internal byte FailureCounter = 0;
        internal byte NotFoundForStreamCounter = 0;

        // If properties are changed/added/removed, make sure to update FileChangeWithDependencies in the Sync project!!

        /// <summary>
        /// Constructor with required fields of abstract base class,
        /// DelayCompletedLocker to lock upon delay completion must be provided for syncing the DelayCompleted boolean
        /// </summary>
        /// <param name="DelayCompletedLocker">Object to lock on to synchronize setting DelayCompleted boolean</param>
        /// <param name="fileDownloadMoveLocker">A locker for file downloads, or null if not a file download (Sync From file create/modify)</param>
        internal FileChange(object DelayCompletedLocker, object fileDownloadMoveLocker = null)
            : base(DelayCompletedLocker)
        {
            this.fileDownloadMoveLocker = fileDownloadMoveLocker;
            SetIncrementedId();
        }
        /// <summary>
        /// Constructor for an object to store parameters,
        /// but not be delay-processable
        /// </summary>
        public FileChange() : base()
        {
            this.fileDownloadMoveLocker = null;
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

        /// <summary>
        /// event callback for the UpDownEvent in SyncEngine to allow this upload or download FileChange to add itself
        /// to an event argument list so the parent can grab all FileChanges in upload or download
        /// </summary>
        internal void FileChange_UpDown(object sender, UpDownEventArgs e)
        {
            // fires a callback stored in the event arguments with the current FileChange
            e.SendBackChange(this, sender);
        }

        /// <summary>
        /// event arguments for UpDownEvent in SyncEngine which stores a callback which will be fired back to the calling method firing the event with every FileChange subscribed to the event
        /// </summary>
        internal class UpDownEventArgs : EventArgs
        {
            // stores the callback which will be fired back upon event handling (see FileChange_UpDown above)
            public Action<FileChange, object> SendBackChange { get; private set; }

            /// <summary>
            /// Constructs new arguments to fire the UpDownEvent with a supplied callback which will be fired for every FileChange subscribed to the event
            /// </summary>
            /// <param name="SendBackChange">Required callback to receive each FileChange which had subscribed to the event</param>
            public UpDownEventArgs(Action<FileChange, object> SendBackChange)
            {
                // check that the input parameter was set; if it wasn't, throw an exception
                if (SendBackChange == null)
                {
                    throw new NullReferenceException("SendBackChange cannot be null");
                }

                // store the callback
                this.SendBackChange = SendBackChange;
            }
        }
    }
}