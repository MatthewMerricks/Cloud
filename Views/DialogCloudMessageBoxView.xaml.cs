//
//  DialogCloudMessageBoxView.xaml.cs
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
    public partial class DialogCloudMessageBoxView : Window, IModalWindow
    {
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

            // Unregister for messages
            Messenger.Default.Unregister(this);
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
