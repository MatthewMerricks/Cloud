//
// BaseMessage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model.EventMessages
{
    public abstract class BaseMessage
    {
        public abstract string Message { get; }

        public EventMessageType Type
        {
            get
            {
                return _type;
            }
        }
        private readonly EventMessageType _type;

        internal protected BaseMessage(EventMessageType Type)
        {
            this._type = Type;
        }
    }
}