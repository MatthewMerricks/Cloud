//
// SetCountMessageArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.Model.EventMessages;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal sealed class SetCountMessageArgs : ISetCountMessage
    {
        public EventMessageArgs MessageArgs
        {
            get
            {
                return _messageArgs;
            }
        }
        private readonly EventMessageArgs _messageArgs;
        private readonly bool _upload;

        #region IHandleableArgs members
        bool IHandleableArgs.Handled
        {
            get
            {
                return _messageArgs.Handled;
            }
        }
        void IHandleableArgs.MarkHandled()
        {
            _messageArgs.MarkHandled();
        }
        #endregion

        #region IMinimalMessage members
        string IMinimalMessage.Message
        {
            get
            {
                return _messageArgs.Message.Message;
            }
        }
        Nullable<long> IMinimalMessage.SyncboxId
        {
            get
            {
                return _messageArgs.Message.SyncboxId;
            }
        }
        string IMinimalMessage.DeviceId
        {
            get
            {
                return _messageArgs.Message.DeviceId;
            }
        }
        BaseMessage IMinimalMessage.BaseMessage
        {
            get
            {
                return _messageArgs.Message;
            }
        }
        #endregion

        #region ISetCountMessage members
        uint ISetCountMessage.NewCount
        {
            get
            {
                return
                    (_upload
                        ? ((UploadingCountMessage)_messageArgs.Message).Count
                        : ((DownloadingCountMessage)_messageArgs.Message).Count);
            }
        }
        #endregion

        internal SetCountMessageArgs(EventMessageArgs MessageArgs)
        {
            if (MessageArgs == null)
            {
                throw new NullReferenceException("MessageArgs cannot be null");
            }
            if (MessageArgs.Message == null)
            {
                throw new NullReferenceException("MessageArgs Message cannot be null");
            }
            if (MessageArgs.Message.Type != EventMessageType.UploadingCountChanged
                && MessageArgs.Message.Type != EventMessageType.DownloadingCountChanged)
            {
                throw new ArgumentException("MessageArgs Message Type must be UploadingCountChanged or DownloadingCountChanged, instead it is " + MessageArgs.Message.Type.ToString());
            }

            this._messageArgs = MessageArgs;
            this._upload = MessageArgs.Message.Type == EventMessageType.UploadingCountChanged;
        }
    }
}