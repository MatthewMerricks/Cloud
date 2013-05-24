//
// AddItemParams.cs
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
    public sealed class AddItemParams
    {
        /// <summary>
        /// Returns the item (file or folder) to rename in place.
        /// </summary>
        public CLFileItem Item
        {
            get
            {
                return _item;
            }
        }
        private readonly CLFileItem _item;

        /// <summary>
        /// Returns the new name of the item.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private readonly string _name;

        /// <summary>
        /// Construct parameters for renaming an item in place.
        /// </summary>
        /// <param name="itemToRename">The item (file or folder) to rename in place.</param>
        /// <param name="name">New name of the item</param>
        public AddItemParams(CLFileItem item, string name)
        {
            if (item == null)
            {
                throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandAddItemItemMustNotBeNull);
            }
            if (String.IsNullOrEmpty(name))
            {
                throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandAddItemNameMustBeSpecified);
            }

            this._item = item;
            this._name = name;
        }
    }
}