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

namespace CloudApiPublic.Model
{
    internal interface IMessageSender
    {
        void Send(MessageSenderType messageType, params object[] messageParams);
    }

    internal interface IMessageSenderProvider
    {
        IMessageSender IMessageSender { get; }
    }

    internal enum MessageSenderType
    {
        Message_WindowSyncStatus_ShouldClose
    }

    internal sealed class NullMessageSender : IMessageSender
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