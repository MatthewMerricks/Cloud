//
// HashMismatchException.cs
// Cloud Windows
//
// Created By GeorgeS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.IO;


namespace Cloud.Model
{
    /// <summary>
    /// wraps a stream object in a upload stream context;
    /// </summary>

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class UploadStreamContext : StreamContext
    {
        private byte[][] _intermediateHashes;
        private byte[] _hash;
        private Nullable<long> _fileSize;

        public byte[][] IntermediateHashes
        {
            get { return _intermediateHashes; }
        }
        public byte[] Hash
        {
            get { return _hash; }
        }
        public Nullable<long> FileSize
        {
            get { return _fileSize; }
        }


        /// <summary>
        /// public factory that wraps the stream and its context in a UploadStreamContext object;
        /// Note that the object constructor is not public to constrain creating StreamContext objects that don't wrap a valid stream and context;
        /// </summary>
        public static UploadStreamContext Create(Stream stream, byte[][] intermediateHashes, byte[] hash, Nullable<long> fileSize)
        {
            if (stream == null)
            {
                // stream context is null if no stream
                return null; 
            }
            return new UploadStreamContext(stream, intermediateHashes, hash, fileSize);
        }

        private UploadStreamContext(Stream stream, byte[][] intermediateHashes, byte[] hash, Nullable<long> fileSize)
            : base(stream)
        {
            _intermediateHashes = intermediateHashes;
            _hash = hash;
            _fileSize = fileSize;
        }

    }

}