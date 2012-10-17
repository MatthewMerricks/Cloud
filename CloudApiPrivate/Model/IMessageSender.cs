//
//  IMessageSender.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.Model
{
    public interface IMessageSender
    {
        void Send(MessageSenderType messageType, params object[] messageParams);
    }

    public interface IMessageSenderProvider
    {
        IMessageSender IMessageSender { get; }
    }

    public enum MessageSenderType
    {
        Message_WindowSyncStatus_ShouldClose
    }

    public sealed class NullMessageSender : IMessageSender
    {
        #region singleton pattern
        public static NullMessageSender Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new NullMessageSender());
                }
            }
        }
        private static NullMessageSender _instance = null;
        private static readonly object InstanceLocker = new object();
        #endregion

        public void Send(MessageSenderType messageType, params object[] messageParams)
        {
            Console.WriteLine("NullMessageSender Send type: " + messageType.ToString() +
                ((messageParams == null || messageParams.Length != 1)
                    ? string.Empty
                    : " parameter: " + (messageParams[0] == null ? "{null}" : messageParams[0].ToString())));
        }

        private NullMessageSender() { }
    }
}