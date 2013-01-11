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
    /// <summary>
    /// Base class for EventArgs which can be marked as handled when fired to EventHandlers
    /// </summary>
    public abstract class HandleableEventArgs : EventArgs
    {
        /// <summary>
        /// Whether this EventArgs has been marked as handled
        /// </summary>
        public bool Handled
        {
            get
            {
                return _handled;
            }
        }
        private bool _handled = false;
        /// <summary>
        /// Mark this EventArgs as handled
        /// </summary>
        public void MarkHandled()
        {
            _handled = true;
        }
    }
}