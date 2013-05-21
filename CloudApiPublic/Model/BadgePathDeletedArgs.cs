//
//  BadgePathDeletedArgs.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal sealed class BadgePathDeletedArgs : HandleableEventArgs
    {
        public DeleteBadgePath DeleteBadgePath
        {
            get
            {
                return _deleteBadgePath;
            }
        }
        private readonly DeleteBadgePath _deleteBadgePath;

        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
        }
        private bool _isDeleted = false;
        public void MarkDeleted()
        {
            _isDeleted = true;
        }

        public BadgePathDeletedArgs(DeleteBadgePath deleteBadgePath)
        {
            this._deleteBadgePath = deleteBadgePath;
        }
    }
}