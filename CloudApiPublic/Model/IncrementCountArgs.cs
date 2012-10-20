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
    public sealed class IncrementCountArgs : HandleableEventArgs
    {
        public uint IncrementAmount
        {
            get
            {
                return _incrementAmount;
            }
        }
        private uint _incrementAmount;

        public IncrementCountArgs(uint incrementAmount = 1)
        {
            this._incrementAmount = incrementAmount;
        }
    }
}