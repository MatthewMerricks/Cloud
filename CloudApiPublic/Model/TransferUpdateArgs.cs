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
    /// <summary>
    /// Properties for a file transfer status update
    /// </summary>
    public sealed class TransferUpdateArgs : HandleableEventArgs
    {
        public long EventId
        {
            get
            {
                return _eventId;
            }
        }
        private readonly long _eventId;

        public CLStatusFileTransferUpdateParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }
        private readonly CLStatusFileTransferUpdateParameters _parameters;

        internal TransferUpdateArgs(long eventId, CLStatusFileTransferUpdateParameters parameters)
        {
            this._eventId = eventId;
            this._parameters = parameters;
        }
    }
}