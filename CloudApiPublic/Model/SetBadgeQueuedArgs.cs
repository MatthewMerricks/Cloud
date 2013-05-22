//
//  SetBadgeQueuedArgs.cs
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
    internal sealed class SetBadgeQueuedArgs : HandleableEventArgs
    {
        public SetBadge SetBadge
        {
            get
            {
                return _setBadge;
            }
        }
        private readonly SetBadge _setBadge;

        public SetBadgeQueuedArgs(SetBadge SetBadge)
        {
            this._setBadge = SetBadge;
        }
    }
}