//
// InformationalMessage.cs
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
    public sealed class InformationalMessage : BaseMessage
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

        internal InformationalMessage(string Message, EventMessageLevel Importance, Nullable<long> SyncBoxId, string DeviceId)
            : base(EventMessageType.Informational, SyncBoxId, DeviceId)
        {
            this._message = Message;
            this._importance = Importance;
        }
    }
}