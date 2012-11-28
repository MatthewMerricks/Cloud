//
// InitialMetadata.cs
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
    public struct InitialMetadata
    {
        public FileMetadata Metadata
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialMetadata");
                }
                return _metadata;
            }
        }
        private FileMetadata _metadata;

        public byte[] MD5
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialMetadata");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid InitialMetadata");
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

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public InitialMetadata(FileMetadata Metadata, int UserId, byte[] MD5, FilePath RelativePath)
        {
            if (Metadata == null)
            {
                throw new NullReferenceException("Metadata cannot be null");
            }
            if (Metadata.HashableProperties.IsFolder)
            {
                if (MD5 != null)
                {
                    throw new ArgumentException("MD5 should be null if metadata HashashableProperties IsFolder is true");
                }
                if (Metadata.HashableProperties.Size != null)
                {
                    throw new ArgumentException("Metadata HashableProperties Size should be null if metadata HashashableProperties IsFolder is true");
                }
            }
            else if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be a 16-length byte array if metadata HashashableProperties IsFolder is false");
            }
            else if (Metadata.HashableProperties.Size == null)
            {
                throw new NullReferenceException("Metadata HashableProperties Size cannot be null if metadata HashableProperties IsFolder is false");
            }
            if (RelativePath == null)
            {
                throw new NullReferenceException("RelativePath cannot be null");
            }

            this._metadata = Metadata;
            this._relativePath = RelativePath;
            this._md5 = MD5;
            this._userId = UserId;
            this._isValid = true;
        }
    }
}