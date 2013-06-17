//
// SyncboxDidStartLiveSyncMessage.cs
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
    public sealed class SyncboxDidStartLiveSyncMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Live sync started for syncbox ID " + _syncbox.SyncboxId.ToString() + ", " +  _syncbox.FriendlyName + ".";
                    }
                    return MessageHolder.Value;
                }
            }
        }
        private readonly GenericHolder<string> MessageHolder = new GenericHolder<string>(null);
        private readonly string _message;

        public CLSyncbox Syncbox
        {
            get
            {
                return _syncbox;
            }
        }
        private readonly CLSyncbox _syncbox;

        internal SyncboxDidStartLiveSyncMessage(CLSyncbox syncbox, Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.SyncboxDidStartLiveSyncChanged, SyncboxId, DeviceId)
        {
            this._syncbox = syncbox;
        }
    }
}