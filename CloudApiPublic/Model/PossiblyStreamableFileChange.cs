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

namespace CloudApiPublic.Model
{
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
                return _stream;
            }
        }
        private Stream _stream;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyStreamableFileChange(FileChange FileChange, Stream Stream, bool ignoreStreamException = false)
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
                        || FileChange.Type == Static.FileChangeType.Modified)
                    && Stream == null)
                {
                    if (Stream == null)
                    {
                        throw new NullReferenceException("Stream cannot be null when FileChange is meant to be uploaded to server (file creations and modifications)");
                    }
                }
                else if (Stream != null)
                {
                    Exception disposalError = null;
                    try
                    {
                        Stream.Dispose();
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
            this._stream = Stream;
            this._isValid = true;
        }
    }
}