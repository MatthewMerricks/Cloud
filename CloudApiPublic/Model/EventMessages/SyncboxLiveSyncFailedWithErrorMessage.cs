﻿//
// SyncboxLiveSyncFailedWithErrorMessage.cs
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
    public sealed class SyncboxLiveSyncFailedWithErrorMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Live sync failed for syncbox ID " + _syncbox.SyncboxId.ToString() + ", " +  _syncbox.FriendlyName + "." + Environment.NewLine +
                            "Error code: " + _error.PrimaryException.Code + ". Error message: " + Error.PrimaryException.Message + ".";
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

        public CLError Error
        {
            get
            {
                return _error;
            }
        }
        private readonly CLError _error;

        internal SyncboxLiveSyncFailedWithErrorMessage(CLSyncbox syncbox, CLError error, Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.SyncboxLiveSyncFailedWithErrorChanged, SyncboxId, DeviceId)
        {
            this._syncbox = syncbox;
            this._error = error;
        }
    }
}