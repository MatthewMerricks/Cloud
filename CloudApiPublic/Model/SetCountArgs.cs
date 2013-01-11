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
    /// <summary>
    /// Properties for what a value was set to, meaning is according to which event passes these args
    /// </summary>
    public sealed class SetCountArgs : HandleableEventArgs
    {
        public uint NewCount
        {
            get
            {
                return _newCount;
            }
        }
        private readonly uint _newCount;

        internal SetCountArgs(uint newCount)
        {
            this._newCount = newCount;
        }
    }
}