//
// TransferUpdateArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public sealed class TransferUpdateArgs : HandleableEventArgs
    {
        public long EventId
        {
            get
            {
                return _eventId;
            }
        }
        private long _eventId;

        public CLStatusFileTransferUpdateParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }
        private CLStatusFileTransferUpdateParameters _parameters;

        public TransferUpdateArgs(long eventId, CLStatusFileTransferUpdateParameters parameters)
        {
            this._eventId = eventId;
            this._parameters = parameters;
        }
    }
}