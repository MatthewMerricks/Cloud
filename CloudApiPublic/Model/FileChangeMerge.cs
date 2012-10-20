//
// FileChangeMerge.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public struct FileChangeMerge
    {
        public FileChange MergeFrom
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid FileChangeMerge");
                }
                return _mergeFrom;
            }
        }
        private FileChange _mergeFrom;

        public FileChange MergeTo
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid FileChangeMerge");
                }
                return _mergeTo;
            }
        }
        private FileChange _mergeTo;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public FileChangeMerge(FileChange MergeTo, FileChange MergeFrom = null)
        {
            // Ensure input variables have proper references set
            if (MergeTo == null)
            {
                // null merge events are only valid if there is an oldEvent to remove
                if (MergeFrom == null)
                {
                    throw new NullReferenceException("MergeFrom cannot be null if MergeTo is also null");
                }
            }
            else if (MergeTo.Metadata == null)
            {
                throw new NullReferenceException("MergeTo cannot have null Metadata");
            }

            this._mergeTo = MergeTo;
            this._mergeFrom = MergeFrom;
            this._isValid = true;
        }
    }
}