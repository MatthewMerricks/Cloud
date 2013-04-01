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
    /// wraps a stream object in a stream context;
    /// descendant classes would add additional context that accompanies the stream;
    /// </summary>

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class StreamContext : IDisposable
    {
        private bool _disposed = false;

        private Stream _stream;

        public Stream Stream
        {
            get { return _stream; }
        }

        /// <summary>
        /// public factory that wraps the stream in a StreamContext object;
        /// Note that the object constructor is not public to constrain creating StreamContext objects that don't wrap a valid stream 
        /// </summary>
        public static StreamContext Create(Stream stream)
        {
            if (stream == null)
            {
                // stream context is null if no stream
                return null;
            }

            return new StreamContext(stream);
        }

        protected StreamContext(Stream stream)
        {
            if (stream == null)
            {
                throw new NullReferenceException("stream must be not null in this context");   // ask: can we use ArgumentNullException()?
            }

            _stream = stream;
        }

        ~StreamContext()
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
                    if (_stream != null)
                    {
                        _stream.Dispose();
                        _stream = null;
                    }
                }
                _disposed = true;
            }
        }
    }

}