//
// PossiblyStreamableAndPossiblyChangedFileChangeWithError.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sync.Model
{
    internal struct PossiblyStreamableAndPossiblyChangedFileChangeWithError
    {
        public FileChange FileChange
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChangeWithError");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChangeWithError");
                }
                return _stream;
            }
        }
        private Stream _stream;

        public bool Changed
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChangeWithError");
                }
                return _changed;
            }
        }
        private bool _changed;

        public Exception Error
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChangeWithError");
                }
                return _error;
            }
        }
        private Exception _error;
        
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyStreamableAndPossiblyChangedFileChangeWithError(bool Changed, FileChange FileChange, Stream Stream, Exception Error)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }
            if (Error == null)
            {
                throw new NullReferenceException("Error cannot be null");
            }

            this._fileChange = FileChange;
            this._changed = Changed;
            this._stream = Stream;
            this._error = Error;
            this._isValid = true;
        }
    }
}