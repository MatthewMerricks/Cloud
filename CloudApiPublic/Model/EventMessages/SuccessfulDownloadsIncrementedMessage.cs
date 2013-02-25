//
// SuccessfulDownloadsIncrementedMessage.cs
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

        public Nullable<long> SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private readonly Nullable<long> _syncBoxId;

        public string DeviceId
        {
            get
            {
                return _deviceId;
            }
        }
        private readonly string _deviceId;

        internal SuccessfulDownloadsIncrementedMessage(uint Count, Nullable<long> SyncBoxId, string DeviceId)
            : base(EventMessageType.SuccessfulDownloadsIncremented)
        {
            this._count = Count;
            this._syncBoxId = SyncBoxId;
            this._deviceId = DeviceId;
        }
    }
}