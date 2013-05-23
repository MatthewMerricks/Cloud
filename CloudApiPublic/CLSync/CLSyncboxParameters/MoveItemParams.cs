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

namespace Cloud.CLSync.CLSyncboxParameters
{
    /// <summary>
    /// Contains the old path and new path for renaming a single item
    /// </summary>
    public sealed class MoveItemParams
    {
        /// <summary>
        /// Returns the item (file or folder) to rename in place.
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
        /// Returns the new name of the item.
        /// </summary>
        public string NewParentPath
        {
            get
            {
                return _newParentPath;
            }
        }
        private readonly string _newParentPath;

        /// <summary>
        /// Construct parameters for renaming an item in place.
        /// </summary>
        /// <param name="itemToMove">The item (file or folder) to move.</param>
        /// <param name="newParentPath">Full path of the new parent folder.</param>
        public MoveItemParams(CLFileItem itemToMove, string newParentPath)
        {
            if (itemToMove == null)
            {
                throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MoveItemParamsMissingProperties, Resources.ExceptionOnDemandItemToMoveMustNotBeNull);
            }
            if (String.IsNullOrWhiteSpace(newParentPath))
            {
                throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MoveItemParamsMissingProperties, Resources.ExceptionOnDemandNewParentFolderMustBeSpecified);
            }

            this._itemToMove = itemToMove;
            this._newParentPath = newParentPath;
        }
    }
}