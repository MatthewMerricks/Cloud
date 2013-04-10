//
// UploadProgressMessage.cs
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
    public sealed class UploadProgressMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Upload progress on file " + _parameters.RelativePath + ": " + _parameters.ByteProgress.ToString() + "/" + _parameters.ByteSize.ToString() +
                            " bytes, started at " + _parameters.TransferStartTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
                    }
                    return MessageHolder.Value;
                }
            }
        }
        private readonly GenericHolder<string> MessageHolder = new GenericHolder<string>(null);

        private readonly string _message;

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

        internal UploadProgressMessage(CLStatusFileTransferUpdateParameters Parameters, long EventId, Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.UploadProgress, SyncboxId, DeviceId)
        {
            this._eventId = EventId;
            this._parameters = Parameters;
        }
    }
}