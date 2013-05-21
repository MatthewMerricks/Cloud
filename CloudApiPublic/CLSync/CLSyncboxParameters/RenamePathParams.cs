//
// RenamePathParams.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.CLSync.CLSyncboxParameters
{
    /// <summary>
    /// Contains the old path and new path for renaming a single item
    /// </summary>
    internal sealed class RenamePathParams
    {
        /// <summary>
        /// Returns the new path for a rename
        /// </summary>
        public string NewPath
        {
            get
            {
                return _newPath;
            }
        }
        private readonly string _newPath;

        /// <summary>
        /// Returns the old path for a rename
        /// </summary>
        public string OldPath
        {
            get
            {
                return _oldPath;
            }
        }
        private readonly string _oldPath;

        /// <summary>
        /// Construct parameters for renaming an item with old path and new path
        /// </summary>
        /// <param name="newPath">New path after the rename</param>
        /// <param name="oldPath">Old path before the rename</param>
        public RenamePathParams(string newPath, string oldPath)
        {
            this._newPath = newPath;
            this._oldPath = oldPath;
        }
    }
}