//
// EventMessageArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model.EventMessages;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Contains event message properties
    /// </summary>
    public sealed class EventMessageArgs : HandleableEventArgs
    {
        /// <summary>
        /// The actual message
        /// </summary>
        public BaseMessage Message
        {
            get
            {
                return _message;
            }
        }
        private readonly BaseMessage _message;

        internal EventMessageArgs(BaseMessage Message)
        {
            this._message = Message;
        }
    }
}