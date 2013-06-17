﻿//
// SyncboxLiveSyncFailedWithErrorMessageArgs.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.CLSync;
using Cloud.Interfaces;
using Cloud.Model.EventMessages;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal sealed class SyncboxLiveSyncFailedWithErrorMessageArgs : ISyncboxLiveSyncFailedWithErrorMessage
    {
        public EventMessageArgs MessageArgs
        {
            get
            {
                return _messageArgs;
            }
        }
        private readonly EventMessageArgs _messageArgs;

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

        #region ISyncboxLiveSyncFailedWithErrorMessage members
        CLSyncbox ISyncboxLiveSyncFailedWithErrorMessage.Syncbox
        {
            get
            {
                return ((SyncboxLiveSyncFailedWithErrorMessage)_messageArgs.Message).Syncbox;
            }
        }

        CLError ISyncboxLiveSyncFailedWithErrorMessage.Error
        {
            get
            {
                return ((SyncboxLiveSyncFailedWithErrorMessage)_messageArgs.Message).Error;
            }
        }
        #endregion

        internal SyncboxLiveSyncFailedWithErrorMessageArgs(EventMessageArgs MessageArgs)
        {
            if (MessageArgs == null)
            {
                throw new NullReferenceException("MessageArgs cannot be null");
            }
            if (MessageArgs.Message == null)
            {
                throw new NullReferenceException("MessageArgs Message cannot be null");
            }
            if (MessageArgs.Message.Type != EventMessageType.SyncboxLiveSyncFailedWithErrorChanged)
            {
                throw new ArgumentException("MessageArgs Message Type must be SyncboxLiveSyncFailedWithErrorChanged, instead it is " + MessageArgs.Message.Type.ToString());
            }

            this._messageArgs = MessageArgs;
        }
    }
}