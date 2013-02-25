//
// DownloadProgressMessage.cs
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
    public sealed class DownloadProgressMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Download progress on file " + _parameters.RelativePath + ": " + _parameters.ByteProgress.ToString() + "/" + _parameters.ByteSize.ToString() +
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

        public CLStatusFileTransferUpdateParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }
        private readonly CLStatusFileTransferUpdateParameters _parameters;

        internal DownloadProgressMessage(CLStatusFileTransferUpdateParameters Parameters, long EventId, Nullable<long> SyncBoxId, string DeviceId)
            : base(EventMessageType.DownloadProgress)
        {
            this._eventId = EventId;
            this._syncBoxId = SyncBoxId;
            this._deviceId = DeviceId;
            this._parameters = Parameters;
        }
    }
}