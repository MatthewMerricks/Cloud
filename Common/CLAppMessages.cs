//
//  CLAppMessages.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.IO;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Navigation;
using System.Collections.Generic;


namespace win_client.Common
{
    /// <summary>
    /// class that defines all messages used in this application
    /// </summary>
    public static class CLAppMessages
    {
        enum MessageTypes
        {
            CreateNewAccount_FocusToError,
            CreateNewAccount_GetClearPasswordField,
            CreateNewAccount_GetClearConfirmPasswordField,
            Home_FocusToError,
            SelectStorageSize_PresentMessageDialog,
            SetupSelector_PresentMessageDialog,
            Message_ReachabilityChangedNotification,
            Message_BalloonTooltipSystemTrayNotification,
            Message_GrowlSystemTrayNotification,
        }

        public static class CreateNewAccount_GetClearPasswordField
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.CreateNewAccount_GetClearPasswordField);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.CreateNewAccount_GetClearPasswordField, action);
            }
        }

        public static class CreateNewAccount_GetClearConfirmPasswordField
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.CreateNewAccount_GetClearConfirmPasswordField);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.CreateNewAccount_GetClearConfirmPasswordField, action);
            }
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

        public static class Message_ReachabilityChangedNotification
        {
            public static void Send(DialogMessage message)
            {
                Messenger.Default.Send(message, MessageTypes.Message_ReachabilityChangedNotification);
            }

            public static void Register(object recipient, Action<DialogMessage> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_ReachabilityChangedNotification, action);
            }
        }

        public static class Message_BalloonTooltipSystemTrayNotification
        {
            public static void Send(CLBalloonTooltipNotification tooltipInfo)
            {
                Messenger.Default.Send(tooltipInfo, MessageTypes.Message_BalloonTooltipSystemTrayNotification);
            }

            public static void Register(object recipient, Action<CLBalloonTooltipNotification> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_BalloonTooltipSystemTrayNotification, action);
            }
        }

        public static class Message_GrowlSystemTrayNotification
        {
            public static void Send(CLGrowlNotification growlInfo)
            {
                Messenger.Default.Send(growlInfo, MessageTypes.Message_GrowlSystemTrayNotification);
            }

            public static void Register(object recipient, Action<CLGrowlNotification> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_GrowlSystemTrayNotification, action);
            }
        }
    }
}
