//
// InternetConnectivityMessage.cs
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
    internal sealed class InternetConnectivityMessage : IInternetConnectivityMessage
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
                return null;
            }
        }
        string IMinimalMessage.DeviceId
        {
            get
            {
                return null;
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

        #region IInternetConnectivityMessage members
        bool IInternetConnectivityMessage.InternetConnected
        {
            get
            {
                return ((InternetChangeMessage)_messageArgs.Message).InternetConnected;
            }
        }
        #endregion

        internal InternetConnectivityMessage(EventMessageArgs MessageArgs)
        {
            if (MessageArgs == null)
            {
                throw new NullReferenceException("MessageArgs cannot be null");
            }
            if (MessageArgs.Message == null)
            {
                throw new NullReferenceException("MessageArgs Message cannot be null");
            }
            if (MessageArgs.Message.Type != EventMessageType.InternetConnectivityChanged)
            {
                throw new ArgumentException("MessageArgs Message Type must be InternetConnectivityChanged, instead it is " + MessageArgs.Message.Type.ToString());
            }

            this._messageArgs = MessageArgs;
        }
    }
}