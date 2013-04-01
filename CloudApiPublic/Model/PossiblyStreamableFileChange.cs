//
// PossiblyStreamableFileChange.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class StreamContext : IDisposable
    {
        private bool _disposed = false;

        private Stream _stream;

        public Stream Stream 
        {
            get { return _stream; }
        }

        public StreamContext(Stream stream) 
        {
            if (stream == null)
            {
                throw new NullReferenceException("stream cannot be null");
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

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class UploadStreamContext : StreamContext {
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

        public UploadStreamContext(Stream stream, byte[][] intermediateHashes = null, byte[] hash = null, Nullable<long> fileSize = null) 
            : base(stream)
        {
            _intermediateHashes = intermediateHashes;
            _hash = hash;
            _fileSize = fileSize;
        }

    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct PossiblyStreamableFileChange
    {
        public FileChange FileChange
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChange");
                }
                return _fileChange;
            }
        }
        private FileChange _fileChange;

        public Stream Stream
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChange");
                }
                return _streamContext == null ? null : _streamContext.Stream;
            }
        }

        public StreamContext StreamContext
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChange");
                }
                return _streamContext;
            }
        }
        private StreamContext _streamContext;
        
        
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyStreamableFileChange(FileChange FileChange, StreamContext streamContext, bool ignoreStreamException = false)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }
            else if (!ignoreStreamException)
            {
                if (FileChange.Metadata != null
                    && !FileChange.Metadata.HashableProperties.IsFolder
                    && FileChange.Direction == Static.SyncDirection.To
                    && (FileChange.Type == Static.FileChangeType.Created
                        || FileChange.Type == Static.FileChangeType.Modified))
                {
                    if (!(FileChange is FileChangeWithDependencies)
                        && streamContext == null)
                    {
                        throw new NullReferenceException("Stream cannot be null when FileChange is meant to be uploaded to server (file creations and modifications)");
                    }
                }
                else if (streamContext != null)
                {
                    Exception disposalError = null;
                    try
                    {
                        streamContext.Dispose();
                    }
                    catch (Exception ex)
                    {
                        disposalError = ex;
                    }
                    throw new ArgumentException("Stream should not be set except when FileChange is meant to be uploaded to server (file creations and modifications). " +
                        (disposalError == null
                            ? "Stream has been disposed"
                            : "Error attempting to dispose Stream, see Inner Exception"),
                        disposalError);
                }
            }

            this._fileChange = FileChange;
            this._streamContext = streamContext;
            this._isValid = true;
        }
    }
}
