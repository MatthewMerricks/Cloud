//
// PossiblyStreamableAndPossiblyChangedFileChange.cs
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

namespace Cloud.Sync.Model
{
    internal struct PossiblyStreamableAndPossiblyChangedFileChange
    {
        public FileChange FileChange
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChange");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChange");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChange");
                }
                return _streamContext;
            }
        }
        private StreamContext _streamContext;

        public bool Changed
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChange");
                }
                return _changed;
            }
        }
        private bool _changed;
        
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyStreamableAndPossiblyChangedFileChange(bool Changed, FileChange FileChange, StreamContext StreamContext)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }

            this._fileChange = FileChange;
            this._changed = Changed;
            this._streamContext = StreamContext;
            this._isValid = true;
        }
    }
}
