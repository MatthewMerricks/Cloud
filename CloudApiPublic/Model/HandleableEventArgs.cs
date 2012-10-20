//
// HandleableEventArgs.cs
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
    public abstract class HandleableEventArgs : EventArgs
    {
        public bool Handled
        {
            get
            {
                return _handled;
            }
        }
        private bool _handled = false;
        public void MarkHandled()
        {
            _handled = true;
        }
    }
}