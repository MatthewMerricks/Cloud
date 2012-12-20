//
//  IMessageSender.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace win_client.Common
{
    public sealed class GenericAppMessageSender : IMessageSender
    {
        #region singleton pattern
        public static GenericAppMessageSender Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance ?? (_instance = new GenericAppMessageSender());
                }
            }
        }
        private static GenericAppMessageSender _instance = null;
        private static readonly object InstanceLocker = new object();
        #endregion

        private GenericAppMessageSender() { }

        #region IMessageSender member
        public void Send(MessageSenderType messageType, params object[] messageParams)
        {
            Func<object[], Type, bool> isCastable;
            if (messageParams != null
                && messageParams.Length == 1)
            {
                if (messageParams[0] == null)
                {
                    isCastable = (innerParams, castableType) =>
                        {
                            Type currentType = castableType;
                            do
                            {
                                if (currentType == typeof(ValueType))
                                {
                                    return Nullable.GetUnderlyingType(castableType) != null;
                                }
                                if (currentType == typeof(object))
                                {
                                    return true;
                                }

                                currentType = currentType.BaseType;
                            } while (currentType != null);

                            return false;
                        };
                }
                else
                {
                    isCastable = (innerParams, castableType) => (castableType == null ? false : innerParams[0].GetType().IsCastableTo(castableType));
                }
            }
            else
            {
                isCastable = (innerParams, castableType) => false;
            }

            switch (messageType)
            {
                case MessageSenderType.Message_WindowSyncStatus_ShouldClose:
                    CLAppMessages.Message_WindowSyncStatus_ShouldClose.Send(isCastable(messageParams, typeof(string))
                        ? (string)messageParams[0]
                        : CLAppMessages.Message_WindowSyncStatus_ShouldClose.DefaultParameter);
                    break;
            }
        }
        #endregion
    }
}