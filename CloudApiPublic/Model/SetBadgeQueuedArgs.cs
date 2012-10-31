//
//  SetBadgeQueuedArgs.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public sealed class SetBadgeQueuedArgs : HandleableEventArgs
    {
        public SetBadge SetBadge
        {
            get
            {
                return _setBadge;
            }
        }
        private SetBadge _setBadge;

        public SetBadgeQueuedArgs(SetBadge SetBadge)
        {
            this._setBadge = SetBadge;
        }
    }
}