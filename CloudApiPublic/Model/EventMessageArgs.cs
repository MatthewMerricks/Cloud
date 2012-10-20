//
// EventMessageArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public sealed class EventMessageArgs : HandleableEventArgs
    {
        public string Message
        {
            get
            {
                return _message;
            }
        }
        private string _message;

        public bool IsError
        {
            get
            {
                return _isError;
            }
        }
        private bool _isError;

        public EventMessageLevel Level
        {
            get
            {
                return _level;
            }
        }
        private EventMessageLevel _level;

        public EventMessageArgs(string Message, EventMessageLevel Level = EventMessageLevel.Minor, bool IsError = false)
        {
            this._message = Message;
            this._level = Level;
            this._isError = IsError;
        }
    }
}