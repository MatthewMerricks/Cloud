//
// ErrorMessage.cs
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
    public class ErrorMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                return _message;
            }
        }
        private readonly string _message;

        public EventMessageLevel Importance
        {
            get
            {
                return _importance;
            }
        }
        private readonly EventMessageLevel _importance;

        public ErrorMessageType ErrorType
        {
            get
            {
                return _errorType;
            }
        }
        private readonly ErrorMessageType _errorType;

        internal protected ErrorMessage(string Message, EventMessageLevel Importance, ErrorMessageType ErrorType)
            : base(EventMessageType.Error)
        {
            this._message = Message;
            this._importance = Importance;
            this._errorType = ErrorType;
        }
    }
}