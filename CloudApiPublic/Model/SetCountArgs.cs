//
// SetCountArgs.cs
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
    public sealed class SetCountArgs : HandleableEventArgs
    {
        public uint NewCount
        {
            get
            {
                return _newCount;
            }
        }
        private uint _newCount;

        public SetCountArgs(uint newCount)
        {
            this._newCount = newCount;
        }
    }
}