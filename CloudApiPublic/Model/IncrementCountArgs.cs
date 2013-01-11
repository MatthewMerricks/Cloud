//
// IncrementCountArgs.cs
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
    /// Properties for how much a value was incremented, meaning is according to which event passes these args
    /// </summary>
    public sealed class IncrementCountArgs : HandleableEventArgs
    {
        public uint IncrementAmount
        {
            get
            {
                return _incrementAmount;
            }
        }
        private readonly uint _incrementAmount;

        internal IncrementCountArgs(uint incrementAmount = 1)
        {
            this._incrementAmount = incrementAmount;
        }
    }
}