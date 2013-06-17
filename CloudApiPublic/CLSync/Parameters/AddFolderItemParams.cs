//
// AddFolderItemParams.cs
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
    /// Contains the name of of new folder, and the parent folder item that will contain the new folder.
    /// </summary>
    public sealed class AddFolderItemParams
    {
        /// <summary>
        /// Returns the parent folder item.
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
        /// Returns the name of the new folder item.
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
        /// Construct parameters for adding a folder.
        /// </summary>
        /// <param name="parent">Parent folder of the folder item we wish to create.</param>
        /// <param name="name">Name of the new folder item being added.</param>
        public AddFolderItemParams(CLFileItem parent, string name)
        {
            this._parent = parent;
            this._name = name;
        }
    }
}