//
// HashedStreamReaderAdapter.cs
// Cloud Windows
//
// Created By GeorgeS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;

using System.IO;

namespace Cloud.Model
{

    /// <summary>
    /// HashedStreamReaderAdapter adapts a stream Read method to verify the stream bytes against a pre-hashed stream
    /// </summary>
    internal sealed class HashedStreamReaderAdapter : StreamReaderAdapter
    {
        private byte[][] _verifierIntermediateHashes; /// original stream intermediate hashes
        private byte[] _verifierHash; /// original stream final hash
        private long _verifierFileSize; /// original stream file size

        private Md5Hasher _hasher; /// stream hasher
        private long _totalBytesRead;

        /// <summary>
        /// constucts hashed stream reader adapter
        /// <param name="stream">stream to read and verify</param>
        /// <param name="maxBytesToHash">size of block for bytes to hash in the intermediate hashes</param>
        /// <param name="verifierIntermediateHashes">original stream intermediate hashes</param>
        /// <param name="verifierHash">original stream final hash</param>
        /// <param name="verifierFileSize">original file size</param>
        /// </summary>
        public HashedStreamReaderAdapter(Stream stream, int maxBytesToHash, byte[][] verifierIntermediateHashes,
                                            byte[] verifierHash, long verifierFileSize)
            : base(stream)
        {
            _verifierIntermediateHashes = verifierIntermediateHashes;
            _verifierHash = verifierHash;
            _verifierFileSize = verifierFileSize;

            _hasher = new Md5Hasher(maxBytesToHash);
            _hasher.OnIntermediateHash += verifyIntermediateHash_;

            _totalBytesRead = 0;
        }

        private void verifyIntermediateHash_(byte[] hash, int index)
        {
            bool intermediateHashesMismatch = (_verifierIntermediateHashes.Length <= index);
            if (!intermediateHashesMismatch)
            {
                intermediateHashesMismatch = !Helpers.IsEqualHashes(hash, _verifierIntermediateHashes[index]);
            }
            if (intermediateHashesMismatch)
            {
                throw new HashMismatchException("intermediate hash does not match; file has been edited;");
            }
        }

        /// <summary>
        /// constrains reading up to hasher.fileSize bytes;
        /// throws a HashMismatchException exception if an intermediate or the total hash in the verifier hasher do not match what is being read
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead;

            if ((_totalBytesRead + count) <= _verifierFileSize)
            {
                bytesRead = _stream.Read(buffer, offset, count);
                _totalBytesRead += bytesRead;
            }
            else
            {
                // constrain the total bytes to read up to original stream file size
                count = (int)(_verifierFileSize - _totalBytesRead);
                if (count > 0)
                {
                    bytesRead = _stream.Read(buffer, offset, count);
                    _totalBytesRead += bytesRead;
                }
                else
                {
                    bytesRead = 0; // end of stream
                }
            }

            if (bytesRead > 0)
            {
                _hasher.Update(buffer, offset, bytesRead);
            }
            else if (bytesRead == 0)
            {
                // end of stream; check the final hash
                //

                _hasher.FinalizeHashes();
                if (!Helpers.IsEqualHashes(_hasher.Hash, _verifierHash))
                {
                    throw new HashMismatchException("final hash does not match; file has been edited;");
                }
            }
            else
            {
                // should not happen according to the documentation
            }

            return bytesRead;
        }
    }

} 