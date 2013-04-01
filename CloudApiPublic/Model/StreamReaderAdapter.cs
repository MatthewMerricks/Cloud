//
// StreamReaderAdapter.cs
// Cloud Windows
//
// Created By GeorgeS.
// Copyright (c) Cloud.com. All rights reserved.

using System.IO;

namespace Cloud.Model
{
    /// <summary>
    /// StreamReaderAdapter adapts a stream Read method to predefined constraints
    /// </summary>
    internal class StreamReaderAdapter
    {
        protected Stream _stream;

        public StreamReaderAdapter(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// the base method just delegates to Read with no constraints
        /// </summary>
        public virtual int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }
    }

}