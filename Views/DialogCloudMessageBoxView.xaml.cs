//
//  PageCreateNewAccount.xaml.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using Dialog.Abstractions.Wpf.Intefaces;
using Xceed.Wpf.Toolkit;
using CleanShutdown.Messaging;

namespace win_client.Views
{
    public partial class DialogCloudMessageBoxView : ChildWindow, IModalWindow
    {
        private bool savedRightButtonIsDefault = false;
        private bool savedRightButtonIsCancel = false;
        private bool savedLeftButtonIsDefault = false;
        private bool savedLeftButtonIsCancel = false;

        public DialogCloudMessageBoxView()
        {
            InitializeComponent();

            Loaded +=DialogCloudMessageBoxView_Loaded;
            Unloaded += DialogCloudMessageBoxView_Unloaded;
        }

        // Button clicks set the DialogResult.
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        //TODO: FocusedElement is a ChildWindow DependencyProperty, properly registered, but for some reason some of
        // the dependency properties are not firing.  FocusedElement is one of them.  Setting this property
        // via the code-behind works however, so we do it here.
        void DialogCloudMessageBoxView_Loaded(object sender, RoutedEventArgs e)
        {
            // Register for messages
            Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                this,
                message =>
                {
                    OnConfirmShutdownMessage(message);
                });
            CLAppMessages.Message_DialogCloudMessageBoxViewShouldClose.Register(this, OnMessage_DialogCloudMessageBoxViewShouldClose);
            CLAppMessages.Message_SaveAndDisableIsDefaultAndIsCancelProperties.Register(this, OnMessage_SaveAndDisableIsDefaultAndIsCancelProperties);
            CLAppMessages.Message_RestoreIsDefaultAndIsCancelProperties.Register(this, Message_RestoreIsDefaultAndIsCancelProperties);

            // Tell all other listeners to save and disable the IsDefault and IsCancel button properties.  This should be the only active modal dialog.
            CLAppMessages.Message_SaveAndDisableIsDefaultAndIsCancelProperties.Send(this);

            // Give focus to the left button.
            //TODO: The caller's should establish the focus position in a parameter.
            btnLeft.Focus();
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void DialogCloudMessageBoxView_Unloaded(object sender, RoutedEventArgs e)
        {
            base.Close();

            // Tell all other listeners to save and disable the IsDefault and IsCancel button properties.  This should be the only active modal dialog.
            CLAppMessages.Message_RestoreIsDefaultAndIsCancelProperties.Send(this);

            // Unregister for messages
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// This view is being requested to close.
        /// </summary>
        private void OnMessage_DialogCloudMessageBoxViewShouldClose(string obj)
        {
            this.Close();
        }

        /// <summary>
        /// Save and disable any IsDefault or IsCancel properties.
        /// </summary>
        private void OnMessage_SaveAndDisableIsDefaultAndIsCancelProperties(object sender)
        {
            DialogCloudMessageBoxView castSender = sender as DialogCloudMessageBoxView;
            if (castSender != this)
            {
                // Save the state of the IsDefault and IsCancel button properties.
                savedRightButtonIsDefault = this.btnRight.IsDefault;
                savedRightButtonIsCancel = this.btnRight.IsCancel;
                savedLeftButtonIsDefault = this.btnLeft.IsDefault;
                savedLeftButtonIsCancel = this.btnLeft.IsCancel;

                // Clear the button properties.
                this.btnRight.IsDefault = false;
                this.btnRight.IsCancel = false;
                this.btnLeft.IsDefault = false;
                this.btnLeft.IsCancel = false;
            }
        }

        /// <summary>
        /// Restore any IsDefault or IsCancel properties.
        /// </summary>
        private void Message_RestoreIsDefaultAndIsCancelProperties(object sender)
        {
            DialogCloudMessageBoxView castSender = sender as DialogCloudMessageBoxView;
            if (castSender != this)
            {
                // Restore the state of the IsDefault and IsCancel button properties.
                this.btnRight.IsDefault = savedRightButtonIsDefault;
                this.btnRight.IsCancel = savedRightButtonIsCancel;
                this.btnLeft.IsDefault = savedLeftButtonIsDefault;
                this.btnLeft.IsCancel = savedLeftButtonIsCancel;
            }
        }

        /// <summary>
        /// The user clicked the 'X' on the NavigationWindow.  That sent a ConfirmShutdown message.
        /// This is a modal dialog.  Prevent the close.
        /// </summary>
        private void OnConfirmShutdownMessage(CleanShutdown.Messaging.NotificationMessageAction<bool> message)
        {
            if (message.Notification == Notifications.ConfirmShutdown)
            {
                message.Execute(true);      // true == abort shutdown
            }

            if (message.Notification == Notifications.QueryModalDialogsActive)
            {
                message.Execute(true);      // a modal dialog is active
            }
        }
    }
}
