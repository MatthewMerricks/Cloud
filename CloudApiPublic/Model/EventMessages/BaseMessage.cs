//
// BaseMessage.cs
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
    public abstract class BaseMessage
    {
        public abstract string Message { get; }

        public EventMessageType Type
        {
            get
            {
                return _type;
            }
        }
        private readonly EventMessageType _type;

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

        internal protected BaseMessage(EventMessageType Type, Nullable<long> SyncBoxId, string DeviceId)
        {
            this._type = Type;
            this._syncBoxId = SyncBoxId;
            this._deviceId = DeviceId;
        }
    }
}