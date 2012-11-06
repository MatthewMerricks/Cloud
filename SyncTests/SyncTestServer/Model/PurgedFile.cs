using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public struct PurgedFile
    {
        public bool FileRemoved
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PurgedFile");
                }
                return _fileRemoved;
            }
        }
        private bool _fileRemoved;

        public string StorageKey
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PurgedFile");
                }
                return _storageKey;
            }
        }
        private string _storageKey;

        public long FileSize
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PurgedFile");
                }
                return _fileSize;
            }
        }
        private long _fileSize;

        public byte[] MD5
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PurgedFile");
                }
                return _md5;
            }
        }
        private byte[] _md5;

        public FilePath UserRelativePath
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PurgedFile");
                }
                return _userRelativePath;
            }
        }
        private FilePath _userRelativePath;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public bool IncompletePurge
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PurgedFile");
                }
                return _incomplete;
            }
        }
        private bool _incomplete;

        public PurgedFile(bool FileRemoved, string StorageKey, long FileSize, byte[] MD5, FilePath UserRelativePath)
        {
            if (MD5 == null
                || MD5.Length != 16)
            {
                //throw new ArgumentException("MD5 must be a 16-length byte array");
                _incomplete = true;
            }
            else if (UserRelativePath == null)
            {
                //throw new NullReferenceException("UserRelativePath cannot be null");
                _incomplete = true;
            }
            else if (FileSize < 0)
            {
                //throw new ArgumentException("FileSize cannot be negative");
                _incomplete = true;
            }
            else
            {
                _incomplete = false;
            }

            this._fileRemoved = FileRemoved;
            this._storageKey = StorageKey;
            this._fileSize = FileSize;
            this._md5 = MD5;
            this._userRelativePath = UserRelativePath;
            this._isValid = true;
        }
    }
}