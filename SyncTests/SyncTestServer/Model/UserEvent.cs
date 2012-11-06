using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public struct UserEvent
    {
        public long SyncId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid UserEvent");
                }
                return _syncId;
            }
        }
        private long _syncId;

        public FileChange FileChange
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid UserEvent");
                }
                return _fileChange;
            }
        }
        private FileChange _fileChange;

        public FileMetadata PreviousMetadata
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid UserEvent");
                }
                return _previousMetadata;
            }
        }
        private FileMetadata _previousMetadata;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public UserEvent(long SyncId, FileChange FileChange, FileMetadata PreviousMetadata)
        {
            this._syncId = SyncId;
            this._fileChange = FileChange;
            this._previousMetadata = PreviousMetadata;
            this._isValid = true;
        }

        public static IComparer<UserEvent> SyncIdComparer
        {
            get
            {
                return EventSyncIdComparer.Instance;
            }
        }

        private class EventSyncIdComparer : IComparer<UserEvent>
        {
            #region singleton pattern
            public static EventSyncIdComparer Instance
            {
                get
                {
                    lock (InstanceLocker)
                    {
                        return _instance
                            ?? (_instance = new EventSyncIdComparer());
                    }
                }
            }
            private static EventSyncIdComparer _instance = null;
            private static readonly object InstanceLocker = new object();

            private EventSyncIdComparer() { }
            #endregion

            public int Compare(UserEvent x, UserEvent y)
            {
                return x.SyncId.CompareTo(y.SyncId);
            }
        }
    }
}