//
// PossiblyChangedFileChange.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct PossiblyChangedFileChange
    {
        public FileChange FileChange
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyChangedFileChange");
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

        public bool Changed
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyChangedFileChange");
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

        public PossiblyChangedFileChange(int ResultOrder, bool Changed, FileChange FileChange)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }

            this._resultOrder = ResultOrder;
            this._fileChange = FileChange;
            this._changed = Changed;
            this._isValid = true;
        }
    }
}