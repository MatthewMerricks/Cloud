//
// DownloadCompleteMessage.cs
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
    public sealed class DownloadCompleteMessage : BaseMessage
    {
        public override string Message
        {
            get
            {
                lock (MessageHolder)
                {
                    if (MessageHolder.Value == null)
                    {
                        MessageHolder.Value = "Download complete for file " + _fileItem.RelativePath;
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
        private long _eventId;

        public CLFileItem FileItem
        {
            get
            {
                return _fileItem;
            }
        }
        private readonly CLFileItem _fileItem;

        internal DownloadCompleteMessage(long EventId, CLFileItem fileItem, Nullable<long> SyncboxId, string DeviceId)
            : base(EventMessageType.DownloadCompleteChanged, SyncboxId, DeviceId)
        {
            this._eventId = EventId;
            this._fileItem = fileItem;
        }
    }
}