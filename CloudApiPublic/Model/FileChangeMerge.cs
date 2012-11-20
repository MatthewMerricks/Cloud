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
    /// <summary>
    /// Do not use the parameterless constructor because the resulting struct will be marked invalid and throw exceptions when retrieving public property values
    /// </summary>
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

        /// <summary>
        /// Creates the struct for a FileChange merge which can be passed to the event source to update the database (including updating existing rows, adding a new row, or deleting a row);
        /// Always use this constructor instead of the parameterless constructor
        /// </summary>
        /// <param name="MergeTo">The values from this FileChange will update an existing row in the database, or create a new row if neither this nor the MergeFrom change has an EventId;
        /// Can be null if this FileChange merge is meant for row deletion</param>
        /// <param name="MergeFrom">This FileChange will be deleted or replaced with the values from MergeTo</param>
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