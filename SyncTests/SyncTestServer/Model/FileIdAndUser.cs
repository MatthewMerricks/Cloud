using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public struct FileIdAndUser
    {
        public Guid FileId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid FileIdAndUser");
                }
                return _fileId;
            }
        }
        private Guid _fileId;

        public int UserId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid FileIdAndUser");
                }
                return _userId;
            }
        }
        private int _userId;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public FileIdAndUser(Guid FileId, int UserId)
        {
            this._fileId = FileId;
            this._userId = UserId;
            this._isValid = true;
            this._toString = null;
            this.ToStringLocker = new object();
        }

        public static IEqualityComparer<FileIdAndUser> Comparer
        {
            get
            {
                return FileIdAndUserComparer.Instance;
            }
        }

        public override string ToString()
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot retrieve property values on an invalid FileIdAndUser");
            }

            lock (ToStringLocker)
            {
                return _toString
                    ?? (_toString = _fileId.ToString() + _userId.ToString());
            }
        }
        private string _toString;
        private readonly object ToStringLocker;

        private sealed class FileIdAndUserComparer : EqualityComparer<FileIdAndUser>
        {
            /// <summary>
            /// Overridden Equals for comparing FileIdAndUsers by deep compare
            /// </summary>
            /// <param name="x">First FileIdAndUser to compare</param>
            /// <param name="y">Second FileIdAndUser to compare</param>
            /// <returns>Returns true for equality, otherwise false</returns>
            public override bool Equals(FileIdAndUser x, FileIdAndUser y)
            {
                return x._fileId == y._fileId
                    && x._userId == y._userId;
            }
            /// <summary>
            /// Overridden GetHashCode that gets a hash from the underlying FileIdAndUser string,
            /// could be improved for efficiency
            /// </summary>
            /// <param name="obj">FileIdAndUser to hash</param>
            /// <returns>Returns hashcode of underlying FileIdAndUser string</returns>
            public override int GetHashCode(FileIdAndUser obj)
            {
                return obj.ToString().GetHashCode();
            }
            /// <summary>
            /// Public static instance to be used everywhere the FileIdAndUserComparer is needed
            /// </summary>
            public static FileIdAndUserComparer Instance
            {
                get
                {
                    lock (InstanceLocker)
                    {
                        if (_instance == null)
                        {
                            _instance = new FileIdAndUserComparer();
                        }
                        return _instance;
                    }
                }
            }
            private static FileIdAndUserComparer _instance = null;
            private static object InstanceLocker = new object();
            /// <summary>
            /// Private constructor to ensure other classes only use the public static Instance
            /// </summary>
            private FileIdAndUserComparer() { }
        }
    }
}