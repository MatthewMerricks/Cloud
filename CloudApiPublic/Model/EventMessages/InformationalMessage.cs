//
// InformationalMessage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model.EventMessages
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

        internal InformationalMessage(string Message, EventMessageLevel Importance, Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.Informational, SyncboxId, DeviceId)
        {
            this._message = Message;
            this._importance = Importance;
        }
    }
}