using System;
using System.IO;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Navigation;

namespace win_client.Common
{
    /// <summary>
    /// class that defines all messages used in this application
    /// </summary>
    public static class AppMessages
    {
        enum MessageTypes
        {
            // CreateNewAccount View messages
            CreateNewAccount_FocusToError,
        }

        public static class CreateNewAccount_FocusToError
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.CreateNewAccount_FocusToError);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.CreateNewAccount_FocusToError, action);
            }
        }

    }
}
