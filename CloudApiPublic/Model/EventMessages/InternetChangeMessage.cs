//
// InternetChangeMessage.cs
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
    public sealed class InternetChangeMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = (_internetConnected
                            ? "Detected internet is connected"
                            : "Detected internet is not connected");
                    }
                    return MessageHolder.Value;
                }
            }
        }
        private readonly GenericHolder<string> MessageHolder = new GenericHolder<string>(null);

        public bool InternetConnected
        {
            get
            {
                return _internetConnected;
            }
        }
        private readonly bool _internetConnected;

        internal InternetChangeMessage(bool internetConnected)
            : base(EventMessageType.InternetConnectivityChanged, SyncboxId: null, DeviceId: null)
        {
            this._internetConnected = internetConnected;
        }
    }
}