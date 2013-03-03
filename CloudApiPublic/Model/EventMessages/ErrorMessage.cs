//
// ErrorMessage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model.EventMessages
{
    public sealed class ErrorMessage : BaseMessage
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

        public BaseErrorInfo ErrorInfo
        {
            get
            {
                return _errorInfo;
            }
        }
        private readonly BaseErrorInfo _errorInfo;

        internal ErrorMessage(string Message, EventMessageLevel Importance, BaseErrorInfo ErrorInfo, Nullable<long> SyncBoxId, string DeviceId)
            : base(EventMessageType.Error, SyncBoxId, DeviceId)
        {
            this._message = Message;
            this._importance = Importance;
            this._errorInfo = ErrorInfo;
        }
    }
}