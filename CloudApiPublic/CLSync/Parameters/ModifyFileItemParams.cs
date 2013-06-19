//
// ModifyFileItemParams.cs
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
    /// Contains the file item that will be modified in the syncbox, and the full path of the modified file on the local disk.
    /// </summary>
    public sealed class ModifyFileItemParams
    {
        /// <summary>
        /// The file item to modify in the syncbox.
        /// </summary>
        public CLFileItem FileItem
        {
            get
            {
                return _fileItem;
            }
        }
        private readonly CLFileItem _fileItem;

        /// <summary>
        /// The full path on the local disk of the file item being added.
        /// </summary>
        public string FullPath
        {
            get
            {
                return _fullPath;
            }
        }
        private readonly string _fullPath;

        /// <summary>
        /// Construct parameters for adding a file item.
        /// </summary>
        /// <param name="fullPath">The full path on the local disk of the file being added.</param>
        /// <param name="parentFolder">Parent folder in the syncbox to which the file will be added.</param>
        /// <param name="fileName">The new filename of the file in the syncbox (just the filename and extension).</param>
        public ModifyFileItemParams(string fullPath, CLFileItem fileToModify)
        {
            this._fileItem = fileToModify;
            this._fullPath = fullPath;
        }
    }
}