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

namespace Cloud.Model
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyChangedFileChange");
                }
                return _stream;
            }
        }
        private readonly Stream _stream;

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
        private readonly bool _changed;
        
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private readonly bool _isValid;

        public PossiblyStreamableAndPossiblyChangedFileChange(int ResultOrder, bool Changed, FileChange FileChange, Stream Stream)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }

            this._resultOrder = ResultOrder;
            this._fileChange = FileChange;
            this._changed = Changed;
            this._stream = Stream;
            this._isValid = true;
        }
    }
}