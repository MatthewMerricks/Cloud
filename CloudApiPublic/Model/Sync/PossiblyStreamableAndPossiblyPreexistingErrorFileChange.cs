//
// PossiblyStreamableAndPossiblyPreexistingErrorFileChange.cs
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
    internal struct PossiblyStreamableAndPossiblyPreexistingErrorFileChange
    {
        public bool IsPreexisting
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyPreexistingErrorFileChange");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyPreexistingErrorFileChange");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyPreexistingErrorFileChange");
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
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableAndPossiblyPreexistingErrorFileChange");
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

        public PossiblyStreamableAndPossiblyPreexistingErrorFileChange(bool IsPreexisting, FileChange FileChange, StreamContext streamContext)
        {
            if (FileChange == null)
            {
                throw new NullReferenceException("FileChange cannot be null");
            }

            this._fileChange = FileChange;
            this._isPreexisting = IsPreexisting;
            this._streamContext = streamContext;
            this._isValid = true;
        }
    }
}
