//
//  AppMessages.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

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
            CreateNewAccount_FocusToError,
            Home_FocusToError,
            SelectStorageSize_PresentMessageDialog,
            SetupSelector_PresentMessageDialog,
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

        public static class Home_FocusToError
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Home_FocusToError);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Home_FocusToError, action);
            }
        }

        public static class SelectStorageSize_PresentMessageDialog
        {
            public static void Send(DialogMessage message)
            {
                Messenger.Default.Send(message, MessageTypes.SelectStorageSize_PresentMessageDialog);
            }

            public static void Register(object recipient, Action<DialogMessage> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.SelectStorageSize_PresentMessageDialog, action);
            }
        }

        public static class SetupSelector_PresentMessageDialog
        {
            public static void Send(DialogMessage message)
            {
                Messenger.Default.Send(message, MessageTypes.SetupSelector_PresentMessageDialog);
            }

            public static void Register(object recipient, Action<DialogMessage> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.SetupSelector_PresentMessageDialog, action);
            }
        }
    }
}
