using CloudApiPublic.Static;
using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudSdkSyncSample.Support;

namespace CloudApiPublic.Support
{
    public sealed class MessageSender : IMessageSender
    {
        #region singleton pattern
        public static MessageSender Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance ?? (_instance = new MessageSender());
                }
            }
        }
        private static MessageSender _instance = null;
        private static readonly object InstanceLocker = new object();
        #endregion

        #region Private Constructor

        private MessageSender() { }

        #endregion

        #region Events

        public event EventHandler<NotificationEventArgs> NotifySyncStatusWindowShouldClose;

        #endregion

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
                    NotifySyncStatusWindowShouldClose(this, new NotificationEventArgs());
                    break;
            }
        }
        #endregion
    }
}