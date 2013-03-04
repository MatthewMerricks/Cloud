//
//  BadgePathRenamedArgs.cs
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
    internal sealed class BadgePathRenamedArgs : HandleableEventArgs
    {
        public RenameBadgePath RenameBadgePath
        {
            get
            {
                return _renameBadgePath;
            }
        }
        private RenameBadgePath _renameBadgePath;

        public BadgePathRenamedArgs(RenameBadgePath renameBadgePath)
        {
            this._renameBadgePath = renameBadgePath;
        }
    }
}