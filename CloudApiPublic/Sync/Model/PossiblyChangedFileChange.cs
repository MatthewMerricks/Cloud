//
// PossiblyChangedFileChange.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Sync.Model
{
    internal struct PossiblyChangedFileChange
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
        private FileChange _fileChange;

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
        private bool _changed;
        
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyChangedFileChange(bool Changed, FileChange FileChange)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }

            this._fileChange = FileChange;
            this._changed = Changed;
            this._isValid = true;
        }
    }
}