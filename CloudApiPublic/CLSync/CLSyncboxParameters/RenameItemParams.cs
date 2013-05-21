//
// RenameItemParams.cs
// Cloud Windows
//
// Created By BobS.
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
    internal sealed class RenameItemParams
    {
        /// <summary>
        /// Returns the item (file or folder) to rename in place.
        /// </summary>
        public CLFileItem ItemToRename
        {
            get
            {
                return _itemToRename;
            }
        }
        private readonly CLFileItem _itemToRename;

        /// <summary>
        /// Returns the new name of the item.
        /// </summary>
        public string NewName
        {
            get
            {
                return _newName;
            }
        }
        private readonly string _newName;

        /// <summary>
        /// Construct parameters for renaming an item in place.
        /// </summary>
        /// <param name="itemToRename">The item (file or folder) to rename in place.</param>
        /// <param name="newName">New name of the item</param>
        public RenameItemParams(CLFileItem itemToRename, string newName)
        {
            this._itemToRename = itemToRename;
            this._newName = newName;
        }
    }
}