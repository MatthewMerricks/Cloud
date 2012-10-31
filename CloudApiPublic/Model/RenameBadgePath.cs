//
// RenameBadgePath.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public struct RenameBadgePath
    {
        public string FromPath
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid RenameBadgePath");
                }
                return _fromPath;
            }
        }
        private string _fromPath;

        public string ToPath
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid RenameBadgePath");
                }
                return _toPath;
            }
        }
        private string _toPath;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public RenameBadgePath(string fromPath, string toPath)
        {
            // Ensure input variables have proper references set
            if (fromPath == null)
            {
                throw new NullReferenceException("fromPath cannot be null");
            }
            else if (toPath == null)
            {
                throw new NullReferenceException("toPath cannot be null");
            }

            this._fromPath = fromPath;
            this._toPath = toPath;
            this._isValid = true;
        }
    }
}