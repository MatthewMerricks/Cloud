//
// DeleteBadgePath.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Static;

namespace CloudApiPublic.Model
{
    internal struct DeleteBadgePath
    {
        public FilePath PathToDelete
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid DeleteBadgePath");
                }
                return _pathToDelete;
            }
        }
        private FilePath _pathToDelete;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public DeleteBadgePath(FilePath pathToDelete)
        {
            // Ensure input variables have proper references set
            if (pathToDelete == null)
            {
                throw new NullReferenceException("PathToDelete cannot be null");
            }

            this._pathToDelete = pathToDelete;
            this._isValid = true;
        }
    }
}