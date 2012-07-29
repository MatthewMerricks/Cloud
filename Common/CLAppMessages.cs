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
using win_client.Model;


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
            Home_GetClearPasswordField,
            Message_DidReceivePushNotificationFromServer,
            Message_PageCloudFolderMissingShouldChooseCloudFolder,
            DialogPreferencesNetworkProxies_GetClearPasswordField,
            DialogPreferencesNetworkProxies_FocusToError_Message,

            // Navigation requests
            PageCloudFolderMissing_NavigationRequest,
            PageCreateNewAccount_NavigationRequest,
            PageInvisible_NavigationRequest,
            PageBadgeComInitializationError_NavigationRequest,
            PageTour_NavigationRequest,
            PageSelectStorageSize_NavigationRequest,
            PageHome_NavigationRequest,
            PageCloudAlreadyRunning_NavigationRequest,
            PageSetupSelector_NavigationRequest,
            PagePreferences_NavigationRequest,
            PagePreferences_FrameNavigationRequest,
            PagePreferences_FrameNavigationRequest_WithPreferences,

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

        public static class Home_GetClearPasswordField
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Home_GetClearPasswordField);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Home_GetClearPasswordField, action);
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

        public static class Message_DidReceivePushNotificationFromServer
        {
            public static void Send(string msg)
            {
                Messenger.Default.Send(msg, MessageTypes.Message_DidReceivePushNotificationFromServer);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_DidReceivePushNotificationFromServer, action);
            }
        }

        public static class Message_PageCloudFolderMissingShouldChooseCloudFolder
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageCloudFolderMissingShouldChooseCloudFolder);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageCloudFolderMissingShouldChooseCloudFolder, action);
            }
        }

        public static class DialogPreferencesNetworkProxies_GetClearPasswordField
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.DialogPreferencesNetworkProxies_GetClearPasswordField);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.DialogPreferencesNetworkProxies_GetClearPasswordField, action);
            }
        }

        public static class DialogPreferencesNetworkProxies_FocusToError_Message
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.DialogPreferencesNetworkProxies_FocusToError_Message);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.DialogPreferencesNetworkProxies_FocusToError_Message, action);
            }
        }

        // Navigation requests
        public static class PageCloudFolderMissing_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageCloudFolderMissing_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageCloudFolderMissing_NavigationRequest, action);
            }
        }

        public static class PageCreateNewAccount_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageCreateNewAccount_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageCreateNewAccount_NavigationRequest, action);
            }
        }

        public static class PageHome_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageHome_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageHome_NavigationRequest, action);
            }
        }

        public static class PageSelectStorageSize_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageSelectStorageSize_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageSelectStorageSize_NavigationRequest, action);
            }
        }

        public static class PageTour_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageTour_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageTour_NavigationRequest, action);
            }
        }

        public static class PageBadgeComInitializationError_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageBadgeComInitializationError_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageBadgeComInitializationError_NavigationRequest, action);
            }
        }

        public static class PageInvisible_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageInvisible_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageInvisible_NavigationRequest, action);
            }
        }

        public static class PageCloudAlreadyRunning_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageCloudAlreadyRunning_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageCloudAlreadyRunning_NavigationRequest, action);
            }
        }

        public static class PageSetupSelector_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageSetupSelector_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageSetupSelector_NavigationRequest, action);
            }
        }

        public static class PagePreferences_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PagePreferences_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PagePreferences_NavigationRequest, action);
            }
        }

        public static class PagePreferences_FrameNavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PagePreferences_FrameNavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PagePreferences_FrameNavigationRequest, action);
            }
        }

        public static class PagePreferences_FrameNavigationRequest_WithPreferences
        {
            public static void Send(KeyValuePair<Uri, CLPreferences> targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PagePreferences_FrameNavigationRequest_WithPreferences);
            }

            public static void Register(object recipient, Action<KeyValuePair<Uri, CLPreferences>> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PagePreferences_FrameNavigationRequest_WithPreferences, action);
            }
        }
    }
}
