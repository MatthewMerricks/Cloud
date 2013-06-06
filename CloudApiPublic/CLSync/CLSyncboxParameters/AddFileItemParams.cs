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
        /// The parent folder item in the syncbox to which the file will be added.
        /// </summary>
        public CLFileItem ParentFolder
        {
            get
            {
                return _parentFolder;
            }
        }
        private readonly CLFileItem _parentFolder;

        /// <summary>
        /// The new name of the file in the syncbox (within the parent folder item).
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }
        }
        private readonly string _fileName;



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
        public AddFileItemParams(string fullPath, CLFileItem parentFolder, string fileName)
        {
            this._parentFolder = parentFolder;
            this._fullPath = fullPath;
            this._fileName = fileName;
        }
    }
}