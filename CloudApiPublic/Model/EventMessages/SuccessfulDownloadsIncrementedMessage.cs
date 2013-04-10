//
// SuccessfulDownloadsIncrementedMessage.cs
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
    public sealed class SuccessfulDownloadsIncrementedMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = _count.ToString() + " file" + (_count == 1 ? string.Empty : "s") + " finished downloading";
                    }
                    return MessageHolder.Value;
                }
            }
        }
        private readonly GenericHolder<string> MessageHolder = new GenericHolder<string>(null);

        private readonly string _message;

        public uint Count
        {
            get
            {
                return _count;
            }
        }
        private readonly uint _count;

        internal SuccessfulDownloadsIncrementedMessage(uint Count, Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.SuccessfulDownloadsIncremented, SyncboxId, DeviceId)
        {
            this._count = Count;
        }
    }
}