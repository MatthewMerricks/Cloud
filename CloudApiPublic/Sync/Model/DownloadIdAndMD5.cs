//
// DownloadIdAndMD5.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Sync.Model
{
    internal struct DownloadIdAndMD5
    {
        public Guid Id
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid DownloadIdAndMD5");
                }
                return _id;
            }
        }
        private Guid _id;

        public byte[] MD5
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid DownloadIdAndMD5");
                }
                return _mD5;
            }
        }
        private byte[] _mD5;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public DownloadIdAndMD5(Guid Id, byte[] MD5)
        {
            if (MD5 == null)
            {
                throw new NullReferenceException("MD5 cannot be null");
            }
            else if (MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be 16 bytes");
            }

            this._id = Id;
            this._mD5 = MD5;
            this._isValid = true;
        }
    }
}