//
// AddFileItemParams.cs
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
    /// Contains the name of of new folder, and the parent folder item that will contain the new folder.
    /// </summary>
    public sealed class AddFileItemParams
    {
        /// <summary>
        /// Returns the parent folder item of the file being added.
        /// </summary>
        public CLFileItem Parent
        {
            get
            {
                return _parent;
            }
        }
        private readonly CLFileItem _parent;

        /// <summary>
        /// Returns the relative path in the syncbox of the file item being added.
        /// </summary>
        public string RelativePath
        {
            get
            {
                return _relativePath;
            }
        }
        private readonly string _relativePath;

        /// <summary>
        /// Construct parameters for adding a file item.
        /// </summary>
        /// <param name="parent">Parent folder of the item we wish to add.</param>
        /// <param name="relativePath">Relative path in the syncbox of the file being added.</param>
        public AddFileItemParams(CLFileItem parent, string relativePath)
        {
            if (parent == null)
            {
                throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandAddItemItemMustNotBeNull);
            }
            if (String.IsNullOrEmpty(relativePath))
            {
                throw new CLArgumentNullException(Static.CLExceptionCode.OnDemand_MissingParameters, Resources.ExceptionOnDemandAddItemNameMustBeSpecified);
            }

            this._parent = parent;
            this._relativePath = relativePath;
        }
    }
}