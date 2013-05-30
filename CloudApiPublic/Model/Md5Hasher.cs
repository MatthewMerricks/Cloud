//
// Md5Hasher.cs
// Cloud Windows
//
// Created By GeorgeS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;

using System;
using System.Security.Cryptography;
using System.Collections.Generic;


namespace Cloud.Model
{

    /// <summary>
    /// Helper class to accumulate hashes; 
    /// A hash over all the bytes in the input stream and optional intermediate hashes of blocks of max bytes are calculated simultaneously;
    /// The resulting hashes array can be retrieved after Finalize is called;
    /// Instances of this class are not designed to be thread safe;
    /// </summary>
    /// <example>
    ///     Md5Hasher hasher = new Md5Hasher();   // no intermediate hashes
    ///     
    ///     Md5Hasher hasher = new Md5Hasher(maxBytesToHash);   // intermediate hashes for each maxBytesToHash bytes in the input stream
    ///     hasher.OnIntermediateHash = (byte[] hash, int index) => {...};
    ///     
    ///     while ((bytesRead = stream.Read(buffer, 0, buffer.Length) != 0) 
    ///     {
    ///         hashes.Update(buffer, bytesRead);
    ///     }
    ///     byte[] hash = hasher.Hash;
    ///     byte[][] hashes = hasher.IntermediateHashes; // null if no intermediate hashes
    /// </example>
    public class Md5Hasher : IDisposable
    {
        private bool _disposed = false;

        private MD5 _totalMd5;

        private int _maxBytesToHash;
        private MD5 _md5;
        private int _bytesHashed;
        private List<byte[]> _hashes;

        private byte[] _hash;
        private byte[][] _intermediateHashes;

        public delegate void IntermediateHashDelegate(byte[] hash, int index);

        /// <summary>
        ///  notification on each intermediate hash calculated
        /// </summary>
        public event IntermediateHashDelegate OnIntermediateHash;

        public int MaxBytesToHash
        {
            get { return _maxBytesToHash; }
        }

        /// <summary>
        ///  total hash of the stream
        /// </summary>
        public byte[] Hash
        {
            get { return _hash; }
        }
        /// <summary>
        ///  intermediate hashes of the stream
        /// </summary>
        public byte[][] IntermediateHashes
        {
            get { return _intermediateHashes ?? new byte[0][]; }
        }

        /// <summary>
        /// constructs a hasher for the whole stream only;
        /// </summary>
        public Md5Hasher()
        {
            Init_(0);
        }

        ~Md5Hasher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_totalMd5 != null)
                    {
                        try
                        {
                            _totalMd5.Dispose();
                        }
                        catch
                        {
                        }
                        _totalMd5 = null;
                    }

                    if (_md5 != null)
                    {
                        try
                        {
                            _md5.Dispose();
                        }
                        catch
                        {
                        }
                        _md5 = null;
                    }

                    if (_hashes != null)
                    {
                        _hashes.Clear();
                        _hashes = null;
                    }

                    _hash = null;
                    _intermediateHashes = null;
                }

                _bytesHashed = 0;

                _disposed = true;
            }
        }

        /// <summary>
        /// constructs a hasher for the whole stream and intermediate hashes for every maxBytesToHash in the input stream
        /// </summary>
        public Md5Hasher(int maxBytesToHash)
        {
            Init_(maxBytesToHash);
        }

        private void Init_(int maxBytesToHash)
        {
            _totalMd5 = MD5.Create();

            _maxBytesToHash = maxBytesToHash;
            _md5 = null;
            _bytesHashed = 0;
            _hashes = null;

            _hash = null;
            _intermediateHashes = null;
        }

        /// <summary>
        /// feeds data to hash;
        /// calling Update after Finalize is undefined;
        /// </summary>
        public void Update(byte[] buffer, int bytesRead)
        {
            Update(buffer, 0, bytesRead);
        }

        /// <summary>
        /// feeds data to hash;
        /// calling Update after Finalize is undefined;
        /// </summary>
        public void Update(byte[] buffer, int offset, int bytesRead)
        {
            if (_totalMd5 == null)
            {
                // assert: Update() should not be called after Finalize()/Dispose()
                const string assertMessage = "_totalMd5 cannot be null";
                MessageEvents.FireNewEventMessage(assertMessage,
                    EventMessageLevel.Important,
                    new EventMessages.ErrorInfo.HaltAllOfCloudSDKErrorInfo());
                throw new NullReferenceException(assertMessage);
            }

            _totalMd5.TransformBlock(buffer, offset, bytesRead, buffer, offset);
            UpdateIntermediateHashes_(buffer, offset, bytesRead);
        }

        private void UpdateIntermediateHashes_(byte[] buffer, int offset, int bytesRead)
        {
            if (_maxBytesToHash <= 0)
            {
                return; // no intermediate hashes
            }

            for (int totalBytesWritten = 0, bytesWritten = 0; totalBytesWritten < bytesRead; totalBytesWritten += bytesWritten)
            {
                if (_md5 == null)
                {
                    _md5 = MD5.Create();
                }
                bytesWritten = Math.Min(_maxBytesToHash - _bytesHashed, bytesRead - totalBytesWritten);
                _md5.TransformBlock(buffer, offset + totalBytesWritten, bytesWritten, buffer, offset + totalBytesWritten);
                _bytesHashed += bytesWritten;
                if (_bytesHashed == _maxBytesToHash)
                {
                    _md5.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);

                    byte[] hash = _md5.Hash;

                    if (_hashes == null)
                    {
                        _hashes = new List<byte[]>();
                    }
                    if (OnIntermediateHash != null)
                    {
                        OnIntermediateHash(hash, _hashes.Count);
                    }
                    _hashes.Add(hash);

                    _md5.Dispose();
                    _md5 = null;
                    _bytesHashed = 0;
                }
            }
        }

        /// <summary>
        /// finalizes hashing 
        /// </summary>
        public void FinalizeHashes()
        {
            _totalMd5.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);
            _hash = _totalMd5.Hash;

            _intermediateHashes = _hashes == null || _hashes.Count == 0 ? null : _hashes.ToArray();
        }

    }

}