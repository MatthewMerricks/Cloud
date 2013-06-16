//
// StorageQuotaExceededMessage.cs
// Cloud Windows
//
// Created By Bobs.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.CLSync;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model.EventMessages
{
    public sealed class StorageQuotaExceededMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Storage quota has been exceeded.";
                    }
                    return MessageHolder.Value;
                }
            }
        }
        private readonly GenericHolder<string> MessageHolder = new GenericHolder<string>(null);
        private readonly string _message;

        internal StorageQuotaExceededMessage(Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.StorageQuotaExceededChanged, SyncboxId, DeviceId)
        {
        }
    }
}