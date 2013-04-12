//
// PossiblyStreamableAndPossiblyChangedFileChangeWithError.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cloud.Model
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
        private readonly FileChange _fileChange;

        public int ResultOrder
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyChangedFileChange");
                }
                return _resultOrder;
            }
        }
        private readonly int _resultOrder;

        public Stream Stream
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChangeWithError");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChangeWithError");
                }
                return _streamContext;
            }
        }
        private readonly StreamContext _streamContext;

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
        private readonly bool _changed;

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
        private readonly Exception _error;
        
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private readonly bool _isValid;

        public PossiblyStreamableAndPossiblyChangedFileChangeWithError(int ResultOrder, bool Changed, FileChange FileChange, StreamContext StreamContext, Exception Error)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }
            if (Error == null)
            {
                throw new NullReferenceException("Error cannot be null");
            }

            this._resultOrder = ResultOrder;
            this._fileChange = FileChange;
            this._changed = Changed;
            this._streamContext = StreamContext;
            this._error = Error;
            this._isValid = true;
        }
    }
}