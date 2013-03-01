//
// PossiblyPreexistingFileChangeInError.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct PossiblyPreexistingFileChangeInError
    {
        public bool IsPreexisting
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyPreexistingFileChangeInError");
                }
                return _isPreexisting;
            }
        }
        private bool _isPreexisting;

        public FileChange FileChange
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyPreexistingFileChangeInError");
                }
                return _fileChange;
            }
        }
        private FileChange _fileChange;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public PossiblyPreexistingFileChangeInError(bool IsPreexisting, FileChange FileChange)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }

            this._fileChange = FileChange;
            this._isPreexisting = IsPreexisting;
            this._isValid = true;
        }
    }
}