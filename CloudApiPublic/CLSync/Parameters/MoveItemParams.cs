//
// MoveItemParams.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Parameters
{
    /// <summary>
    /// Contains the item and new full path for moving a single item
    /// </summary>
    public sealed class MoveItemParams
    {
        /// <summary>
        /// Returns the item (file or folder) to move.
        /// </summary>
        public CLFileItem ItemToMove
        {
            get
            {
                return _itemToMove;
            }
        }
        private readonly CLFileItem _itemToMove;

        /// <summary>
        /// Returns the folder item representing the new parent of the item.
        /// </summary>
        public CLFileItem NewParentFolderItem
        {
            get
            {
                return _newParentFolderItem;
            }
        }
        private readonly CLFileItem _newParentFolderItem;

        /// <summary>
        /// Construct parameters for moving an item.
        /// </summary>
        /// <param name="itemToMove">The item (file or folder) to move.</param>
        /// <param name="newParentFolderItem">The item representing the new parent folder.</param>
        public MoveItemParams(CLFileItem itemToMove, CLFileItem newParentFolderItem)
        {
            this._itemToMove = itemToMove;
            this._newParentFolderItem = newParentFolderItem;
        }
    }
}