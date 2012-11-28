//
// InitialStorageData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public struct InitialStorageData
    {
        public long FileSize
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialStorageData");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialStorageData");
                }
                return _md5;
            }
        }
        private byte[] _md5;

        public int UserId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialStorageData");
                }
                return _userId;
            }
        }
        private int _userId;

        public FilePath RelativePath
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialStorageData");
                }
                return _relativePath;
            }
        }
        private FilePath _relativePath;

        public string StorageKey
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialStorageData");
                }
                return _storageKey;
            }
        }
        private string _storageKey;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public InitialStorageData(int UserId, string StorageKey, byte[] MD5, long FileSize, FilePath RelativePath)
        {
            if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be a 16-length byte array");
            }
            if (FileSize < 0)
            {
                throw new ArgumentException("FileSize cannot be negative");
            }
            if (RelativePath == null)
            {
                throw new NullReferenceException("RelativePath cannot be null");
            }

            this._relativePath = RelativePath;
            this._storageKey = StorageKey;
            this._md5 = MD5;
            this._fileSize = FileSize;
            this._userId = UserId;
            this._isValid = true;
        }
    }
}