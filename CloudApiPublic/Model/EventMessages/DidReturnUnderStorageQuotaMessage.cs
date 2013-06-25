//
// DidReturnUnderStorageQuotaMessage.cs
// Cloud Windows
//
// Created By Bobs.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model.EventMessages
{
    public sealed class DidReturnUnderStorageQuotaMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Storage usage is back under or at quota.";
                    }
                    return MessageHolder.Value;
                }
            }
        }
        private readonly GenericHolder<string> MessageHolder = new GenericHolder<string>(null);
        private readonly string _message;

        internal DidReturnUnderStorageQuotaMessage(Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.DidReturnUnderStorageQuotaChanged, SyncboxId, DeviceId)
        {
        }
    }
}