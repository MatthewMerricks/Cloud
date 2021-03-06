﻿//
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
using Cloud.JsonContracts;
#if TRASH
using win_client.DragDropServer;
#endif // TRASH


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
            SetupSelector_PresentMessageDialog,
            Message_ReachabilityChangedNotification,
            Message_BalloonTooltipSystemTrayNotification,
            Message_GrowlSystemTrayNotification,
            Home_GetClearPasswordField,
            Message_DidReceivePushNotificationFromServer,
            Message_PageCloudFolderMissingShouldChooseCloudFolder,
            DialogPreferencesNetworkProxies_GetClearPasswordField,
            DialogPreferencesNetworkProxies_SetClearPasswordField,
            DialogPreferencesNetworkProxies_FocusToError_Message,
            DialogPreferencesNetworkBandwidth_FocusToError_Message,
            Message_FramePreferencesAdvanced_ShouldChooseCloudFolder,
            Message_PageMustUnregisterWindowClosingMessage,
            Message_DialogPreferencesNetworkBandwidthViewShouldClose,
            Message_DialogPreferencesNetworkProxiesViewShouldClose,
            Message_PageSelectStorageSizeViewSetFocusToContinueButton,
            Message_PageSetupSelectorViewSetFocusToContinueButton,
            Message_PageFolderSelection_ShouldChooseCloudFolder,
            Message_PageSetupSelector_ShouldChooseCloudFolder,
            Message_DialogCheckForUpdates_ShouldCheckForUpdates,
            Message_WindowSyncStatus_ShouldClose,
            Message_PageInvisible_ResetNotifyIcon,
#if TRASH
            Message_DragDropServer_ShouldShowSystrayDropWindow,
            Message_DragDropServer_ShouldHideSystrayDropWindow,
#endif // TRASH
                

            // Navigation requests
            PageCloudFolderMissing_NavigationRequest,
            PageCreateNewAccount_NavigationRequest,
            PageInvisible_NavigationRequest,
            PageInvisible_TriggerOutOfSystemTrayAnimation,
            PageBadgeComInitializationError_NavigationRequest,
            PageTour_NavigationRequest,
            PageSelectStorageSize_NavigationRequest,
            PageHome_NavigationRequest,
            PageCloudAlreadyRunning_NavigationRequest,
            PageSetupSelector_NavigationRequest,
            PagePreferences_NavigationRequest,
            PagePreferences_FrameNavigationRequest,
            PagePreferences_FrameNavigationRequest_WithPreferences,
            PageFolderSelection_NavigationRequest,
            PageTourAdvancedEnd_NavigationRequest,

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
            public static void Send(NotificationResponse msg)
            {
                Messenger.Default.Send(msg, MessageTypes.Message_DidReceivePushNotificationFromServer);
            }

            public static void Register(object recipient, Action<NotificationResponse> action)
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

        public static class DialogPreferencesNetworkProxies_SetClearPasswordField
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.DialogPreferencesNetworkProxies_SetClearPasswordField);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.DialogPreferencesNetworkProxies_SetClearPasswordField, action);
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

        public static class DialogPreferencesNetworkBandwidth_FocusToError_Message
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.DialogPreferencesNetworkBandwidth_FocusToError_Message);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.DialogPreferencesNetworkBandwidth_FocusToError_Message, action);
            }
        }

        public static class Message_FramePreferencesAdvanced_ShouldChooseCloudFolder
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_FramePreferencesAdvanced_ShouldChooseCloudFolder);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_FramePreferencesAdvanced_ShouldChooseCloudFolder, action);
            }
        }

        public static class Message_PageMustUnregisterWindowClosingMessage

        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageMustUnregisterWindowClosingMessage);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageMustUnregisterWindowClosingMessage, action);
            }
        }

        public static class Message_DialogPreferencesNetworkBandwidthViewShouldClose

        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_DialogPreferencesNetworkBandwidthViewShouldClose);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_DialogPreferencesNetworkBandwidthViewShouldClose, action);
            }
        }

        public static class Message_DialogPreferencesNetworkProxiesViewShouldClose
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_DialogPreferencesNetworkProxiesViewShouldClose);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_DialogPreferencesNetworkProxiesViewShouldClose, action);
            }
        }

        public static class Message_PageSelectStorageSizeViewSetFocusToContinueButton
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageSelectStorageSizeViewSetFocusToContinueButton);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageSelectStorageSizeViewSetFocusToContinueButton, action);
            }
        }

        public static class Message_PageSetupSelectorViewSetFocusToContinueButton
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageSetupSelectorViewSetFocusToContinueButton);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageSetupSelectorViewSetFocusToContinueButton, action);
            }
        }

        public static class Message_PageFolderSelection_ShouldChooseCloudFolder
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageFolderSelection_ShouldChooseCloudFolder);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageFolderSelection_ShouldChooseCloudFolder, action);
            }
        }

        public static class Message_PageSetupSelector_ShouldChooseCloudFolder
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageSetupSelector_ShouldChooseCloudFolder);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageSetupSelector_ShouldChooseCloudFolder, action);
            }
        }

        public static class Message_DialogCheckForUpdates_ShouldCheckForUpdates
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_DialogCheckForUpdates_ShouldCheckForUpdates);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_DialogCheckForUpdates_ShouldCheckForUpdates, action);
            }
        }

        public static class Message_WindowSyncStatus_ShouldClose
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_WindowSyncStatus_ShouldClose);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_WindowSyncStatus_ShouldClose, action);
            }

            public static string DefaultParameter = string.Empty;
        }

        public static class Message_PageInvisible_ResetNotifyIcon
        {
            public static void Send(string notUsed)
            {
                Messenger.Default.Send(notUsed, MessageTypes.Message_PageInvisible_ResetNotifyIcon);
            }

            public static void Register(object recipient, Action<string> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_PageInvisible_ResetNotifyIcon, action);
            }

            public static string DefaultParameter = string.Empty;
        }
        

#if TRASH
        public static class Message_DragDropServer_ShouldShowSystrayDropWindow
        {
            public static void Send(DragDropOperation operation)
            {
                Messenger.Default.Send(operation, MessageTypes.Message_DragDropServer_ShouldShowSystrayDropWindow);
            }

            public static void Register(object recipient, Action<DragDropOperation> operation)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_DragDropServer_ShouldShowSystrayDropWindow, operation);
            }
        }

        public static class Message_DragDropServer_ShouldHideSystrayDropWindow
        {
            public static void Send(DragDropOperation operation)
            {
                Messenger.Default.Send(operation, MessageTypes.Message_DragDropServer_ShouldHideSystrayDropWindow);
            }

            public static void Register(object recipient, Action<DragDropOperation> operation)
            {
                Messenger.Default.Register(recipient, MessageTypes.Message_DragDropServer_ShouldHideSystrayDropWindow, operation);
            }
        }
#endif // TRASH


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

        public static class PageFolderSelection_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageFolderSelection_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageFolderSelection_NavigationRequest, action);
            }
        }

        public static class PageTourAdvancedEnd_NavigationRequest
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageTourAdvancedEnd_NavigationRequest);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageTourAdvancedEnd_NavigationRequest, action);
            }
        }
        
        public static class PageInvisible_TriggerOutOfSystemTrayAnimation
        {
            public static void Send(Uri targetPage)
            {
                Messenger.Default.Send(targetPage, MessageTypes.PageInvisible_TriggerOutOfSystemTrayAnimation);
            }

            public static void Register(object recipient, Action<Uri> action)
            {
                Messenger.Default.Register(recipient, MessageTypes.PageInvisible_TriggerOutOfSystemTrayAnimation, action);
            }
        }
    }
}
