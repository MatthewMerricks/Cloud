//
//  DialogPreferencesNetworkProxies.xaml.cs
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
    public partial class DialogPreferencesNetworkProxies : Window, IModalWindow
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DialogPreferencesNetworkProxies()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += DialogPreferencesNetworkProxies_Loaded;
            Unloaded += DialogPreferencesNetworkProxies_Unloaded;
        }

        /// <summary>
        /// The user clicked the OK (update) button.
        /// Button clicks set the DialogResult.
        /// </summary>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogPreferencesNetworkProxiesViewModel vm = (DialogPreferencesNetworkProxiesViewModel)DataContext;
            if (vm.DialogPreferencesNetworkProxiesViewModel_UpdateCommand.CanExecute(null))
            {
                vm.DialogPreferencesNetworkProxiesViewModel_UpdateCommand.Execute(null);
            }

            // Don't let the user cancel the dialog if it has validation errors
            if (vm.HasErrors)
            {
                e.Handled = true;
            }
            else
            {
                this.DialogResult = true;
            }
        }

        /// <summary>
        /// The user clicked the Cancel button.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogPreferencesNetworkProxiesViewModel vm = (DialogPreferencesNetworkProxiesViewModel)DataContext;
            if (vm.DialogPreferencesNetworkProxiesViewModel_CancelCommand.CanExecute(null))
            {
                vm.DialogPreferencesNetworkProxiesViewModel_CancelCommand.Execute(null);
            }

            this.DialogResult = false;
        }

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        //TODO: FocusedElement is a ChildWindow DependencyProperty, properly registered, but for some reason some of
        // the dependency properties are not firing.  FocusedElement is one of them.  Setting this property
        // via the code-behind works however, so we do it here.
        void DialogPreferencesNetworkProxies_Loaded(object sender, RoutedEventArgs e)
        {
            // Register messages
            CLAppMessages.DialogPreferencesNetworkProxies_FocusToError_Message.Register(this, OnDialogPreferencesNetworkProxies_FocusToError_Message);
            CLAppMessages.DialogPreferencesNetworkProxies_GetClearPasswordField.Register(this, OnDialogPreferencesNetworkProxies_GetClearPasswordField);
            CLAppMessages.DialogPreferencesNetworkProxies_SetClearPasswordField.Register(this, OnDialogPreferencesNetworkProxies_SetClearPasswordField);
            CLAppMessages.Message_DialogPreferencesNetworkProxiesViewShouldClose.Register(this, OnMessage_DialogPreferencesNetworkProxiesViewShouldClose);

            // Register to receive the ConfirmShutdown message
            Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                this,
                message =>
                {
                    OnConfirmShutdownMessage(message);
                });

            this.btnOK.Focus();

            // Tell the ViewModel that the view has loaded.  This is necessary because setting the fields in the ViewModel
            // sometimes requires a message to be sent to the view, and if the fields are set in the ViewModel constructor,
            // the view has not yet registered to receive the messages.
            DialogPreferencesNetworkProxiesViewModel vm = (DialogPreferencesNetworkProxiesViewModel)DataContext;
            if (vm.DialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand.CanExecute(null))
            {
                vm.DialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand.Execute(null);
            }
            vm.ViewLayoutRoot = this.LayoutRoot;
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

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void DialogPreferencesNetworkProxies_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Check for errors and put focus to one of the fields with an error
        /// </summary>
        private void OnDialogPreferencesNetworkProxies_FocusToError_Message(string notUsed)
        {
            if (Validation.GetHasError(tbProxyServerAddress) == true)
            {
                tbProxyServerAddress.Focus();
                return;
            }
            if (Validation.GetHasError(this.tbProxyServerPort) == true)
            {
                tbProxyServerPort.Focus();
                return;
            }
            if (Validation.GetHasError(tbProxyServerUsername) == true)
            {
                tbProxyServerUsername.Focus();
                return;
            }
            if (Validation.GetHasError(tbProxyServerPassword) == true)
            {
                tbProxyServerPassword.Focus();
                return;
            }
        }

        /// <summary>
        /// Get the clear password and present it to the ViewModel.
        /// </summary>
        private void OnDialogPreferencesNetworkProxies_GetClearPasswordField(string obj)
        {
            DialogPreferencesNetworkProxiesViewModel vm = (DialogPreferencesNetworkProxiesViewModel)DataContext;
            string clearPassword = tbProxyServerPassword.Text;
            if (vm != null)
            {
                vm.ProxyServerPassword2 = clearPassword;
            }
        }

        /// <summary>
        /// Set the clear password.
        /// </summary>
        private void OnDialogPreferencesNetworkProxies_SetClearPasswordField(string password)
        {
            tbProxyServerPassword.Text = password;
        }

        /// <summary>
        /// The ViewModel asked us to close.
        /// </summary>
        private void OnMessage_DialogPreferencesNetworkProxiesViewShouldClose(string obj)
        {
            this.Close();
        }

    }
}
