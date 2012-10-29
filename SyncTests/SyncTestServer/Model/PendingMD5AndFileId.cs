using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public struct PendingMD5AndFileId
    {
        public byte[] MD5
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PendingMD5AndFileId");
                }
                return _md5;
            }
        }
        private byte[] _md5;

        public Guid FileId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PendingMD5AndFileId");
                }
                return _fileId;
            }
        }
        private Guid _fileId;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public object SyncRoot
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PendingMD5AndFileId");
                }
                return userPaths;
            }
        }
        private readonly Dictionary<int, HashSet<FilePath>> userPaths;

        public bool Pending
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PendingMD5AndFileId");
                }
                lock (userPaths)
                {
                    return _pending;
                }
            }
            set
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
                }
                lock (userPaths)
                {
                    _pending = value;
                }
            }
        }
        private bool _pending;

        public int GetDownloadingCount(Nullable<KeyValuePair<Action<object>, object>> runActionOnDownloadingCountOfZero = null)
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            lock (userPaths)
            {
                if (runActionOnDownloadingCountOfZero != null)
                {
                    KeyValuePair<Action<object>, object> castAction = (KeyValuePair<Action<object>, object>)runActionOnDownloadingCountOfZero;

                    if (castAction.Key == null)
                    {
                        throw new NullReferenceException("Action<object> in non-null runActionOnDownloadingCoundOfZero cannot be null");
                    }

                    if (_downloadingCount == 0)
                    {
                        castAction.Key(castAction.Value);
                    }
                    else
                    {
                        ActionsToRunOnDownloadingCountOfZero.Enqueue(castAction);
                    }
                }

                return _downloadingCount;
            }
        }
        public void DecrementDownloadingCount()
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            lock (userPaths)
            {
                _downloadingCount--;

                if (_downloadingCount == 0)
                {
                    while (ActionsToRunOnDownloadingCountOfZero.Count > 0)
                    {
                        KeyValuePair<Action<object>, object> zeroCountAction = ActionsToRunOnDownloadingCountOfZero.Dequeue();
                        zeroCountAction.Key(zeroCountAction.Value);
                        if (!_isValid)
                        {
                            return;
                        }
                    }
                }
                else if (_downloadingCount < 0)
                {
                    _downloadingCount = 0;
                    throw new Exception("Cannot decrement downloading count lower than zero");
                }
            }
        }
        public void IncrementDownloadingCount()
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            lock (userPaths)
            {
                _downloadingCount++;
            }
        }
        private readonly Queue<KeyValuePair<Action<object>, object>> ActionsToRunOnDownloadingCountOfZero;
        private int _downloadingCount;

        /// <summary>
        /// Returns true if the combination userId and userRelativePath were unique and thus added,
        /// otherwise returns false
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userRelativePath"></param>
        /// <returns></returns>
        public bool AddUserUsage(int userId, FilePath userRelativePath)
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            if (userRelativePath == null)
            {
                throw new NullReferenceException("userRelativePath cannot be null");
            }

            lock (userPaths)
            {
                HashSet<FilePath> foundPaths;
                if (userPaths.TryGetValue(userId, out foundPaths))
                {
                    if (foundPaths.Contains(userRelativePath))
                    {
                        return false;
                    }
                    else
                    {
                        foundPaths.Add(userRelativePath);
                        return true;
                    }
                }
                else
                {
                    userPaths.Add(userId, new HashSet<FilePath>(new[] { userRelativePath },
                        FilePathComparer.Instance));
                    return true;
                }
            }
        }

        /// <summary>
        /// Returns true if the last user usage accross all users would be removed (indicates that this object should be removed),
        /// otherwise returns false; if true is returned this struct is marked invalid and can no longer be used
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userRelativePath"></param>
        /// <returns></returns>
        public bool RemoveUserUsage(int userId, FilePath userRelativePath)
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            if (userRelativePath == null)
            {
                throw new NullReferenceException("userRelativePath cannot be null");
            }

            lock (userPaths)
            {
                HashSet<FilePath> foundPaths;
                if (userPaths.TryGetValue(userId, out foundPaths)
                    && foundPaths.Contains(userRelativePath))
                {
                    if (foundPaths.Count <= 1)
                    {
                        if (userPaths.Count <= 1)
                        {
                            _isValid = false;
                            return true;
                        }
                        else
                        {
                            userPaths.Remove(userId);
                        }
                    }
                    else
                    {
                        foundPaths.Remove(userRelativePath);
                    }
                }
            }
            return false;
        }

        public IEnumerable<FilePath> GetUserUsagesByUser(int userId)
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            lock (userPaths)
            {
                HashSet<FilePath> usedPaths;
                if (userPaths.TryGetValue(userId, out usedPaths))
                {
                    return usedPaths.ToArray();
                }
                else
                {
                    return Enumerable.Empty<FilePath>();
                }
            }
        }

        public IEnumerable<IGrouping<int, FilePath>> GetUserUsages()
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            lock (userPaths)
            {
                return userPaths.SelectMany(currentUser => currentUser.Value.Select(currentUserPath => new KeyValuePair<int, FilePath>(currentUser.Key, currentUserPath)))
                    .GroupBy(currentUserPath => currentUserPath.Key, currentUserPath => currentUserPath.Value)
                    .ToArray();// copy to new enumerable so that the original collection can't be modified outside of its locker
            }
        }

        public bool MoveUserUsage(int userId, FilePath oldUserRelativePath, FilePath newUserRelativePath)
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot set property values on an invalid PendingMD5AndFileId");
            }

            lock (userPaths)
            {
                HashSet<FilePath> foundPaths;
                if (userPaths.TryGetValue(userId, out foundPaths)
                    && foundPaths.Contains(oldUserRelativePath)
                    && !foundPaths.Contains(newUserRelativePath))
                {
                    foundPaths.Remove(oldUserRelativePath);
                    foundPaths.Add(newUserRelativePath);
                    return true;
                }
                return false;
            }
        }

        public PendingMD5AndFileId(byte[] md5, Guid fileId, int userId, FilePath userRelativePath, bool initiallyPending = true)
        {
            if (md5 == null
                || md5.Length != 16)
            {
                throw new ArgumentException("md5 must be a 16-length byte array");
            }
            if (userRelativePath == null)
            {
                throw new NullReferenceException("userRelativePath cannot be null");
            }

            this._md5 = md5;
            this._fileId = fileId;
            this._pending = initiallyPending;
            this._isValid = true;
            this._downloadingCount = 0;
            this.ActionsToRunOnDownloadingCountOfZero = new Queue<KeyValuePair<Action<object>, object>>();
            this.userPaths = new Dictionary<int, HashSet<FilePath>>()
            {
                { userId, new HashSet<FilePath>(new[] { userRelativePath },
                    FilePathComparer.Instance) }
            };
        }
    }
}