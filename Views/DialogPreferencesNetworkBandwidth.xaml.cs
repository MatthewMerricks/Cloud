//
//  DialogPreferencesNetworkBandwidth.xaml.cs
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
    public partial class DialogPreferencesNetworkBandwidth : Window, IModalWindow
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DialogPreferencesNetworkBandwidth()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += DialogPreferencesNetworkBandwidth_Loaded;
            Unloaded += DialogPreferencesNetworkBandwidth_Unloaded;
            Closing += DialogPreferencesNetworkBandwidth_Closing;
        }

        /// <summary>
        /// The window is closing.  Should it close?
        /// </summary>
        void DialogPreferencesNetworkBandwidth_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogPreferencesNetworkBandwidthViewModel vm = (DialogPreferencesNetworkBandwidthViewModel)DataContext;
            if (!vm.WindowCloseOk)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// The user clicked the OK (update) button.
        /// Button clicks set the DialogResult.
        /// </summary>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogPreferencesNetworkBandwidthViewModel vm = (DialogPreferencesNetworkBandwidthViewModel)DataContext;
            if (vm.DialogPreferencesNetworkBandwidthViewModel_UpdateCommand.CanExecute(null))
            {
                vm.DialogPreferencesNetworkBandwidthViewModel_UpdateCommand.Execute(null);
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
            DialogPreferencesNetworkBandwidthViewModel vm = (DialogPreferencesNetworkBandwidthViewModel)DataContext;
            if (vm.DialogPreferencesNetworkBandwidthViewModel_CancelCommand.CanExecute(null))
            {
                vm.DialogPreferencesNetworkBandwidthViewModel_CancelCommand.Execute(null);
            }

            if (vm.WindowCloseOk)
            {
                this.DialogResult = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        //TODO: FocusedElement is a ChildWindow DependencyProperty, properly registered, but for some reason some of
        // the dependency properties are not firing.  FocusedElement is one of them.  Setting this property
        // via the code-behind works however, so we do it here.
        void DialogPreferencesNetworkBandwidth_Loaded(object sender, RoutedEventArgs e)
        {
            // Register messages
            CLAppMessages.DialogPreferencesNetworkBandwidth_FocusToError_Message.Register(this, OnDialogPreferencesNetworkBandwidth_FocusToError_Message);
            CLAppMessages.Message_DialogPreferencesNetworkBandwidthViewShouldClose.Register(this, OnMessage_DialogPreferencesNetworkBandwidthViewShouldClose);

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
            DialogPreferencesNetworkBandwidthViewModel vm = (DialogPreferencesNetworkBandwidthViewModel)DataContext;
            if (vm.DialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand.CanExecute(null))
            {
                vm.DialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand.Execute(null);
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
        void DialogPreferencesNetworkBandwidth_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Check for errors and put focus to one of the fields with an error
        /// </summary>
        private void OnDialogPreferencesNetworkBandwidth_FocusToError_Message(string notUsed)
        {
            if (Validation.GetHasError(tbDownloadBandwidthLimitKBPerSecond) == true)
            {
                tbDownloadBandwidthLimitKBPerSecond.Focus();
                return;
            }
            if (Validation.GetHasError(this.tbUploadBandwidthLimitKBPerSecond) == true)
            {
                tbUploadBandwidthLimitKBPerSecond.Focus();
                return;
            }
        }

        /// <summary>
        /// The ViewModel asked us to close.
        /// </summary>
        private void OnMessage_DialogPreferencesNetworkBandwidthViewShouldClose(string obj)
        {
            this.Close();
        }

        /// <summary>
        /// Event handler: The user clicked the download rate spinner.
        /// </summary>
        private void ButtonSpinner_DownloadRateSpin(object sender, SpinEventArgs e)
        {
            ButtonSpinner spinner = ( ButtonSpinner )sender;
            TextBox txtBox = ( TextBox )spinner.Content;

            try
            {
                int value = String.IsNullOrEmpty( txtBox.Text ) ? 0 : Convert.ToInt32( txtBox.Text );
                if (e.Direction == SpinDirection.Increase)
                {
                    value++;
                }
                else
                {
                    if (value > 1)
                    {
                        value--;
                    }
                }
                txtBox.Text = value.ToString();
            }
            catch
            {
                // txtBox.Text = "1";       // make the user do this
            }
        }

        /// <summary>
        /// Event handler: The user clicked the upload rate spinner.
        /// </summary>
        private void ButtonSpinner_UploadRateSpin(object sender, SpinEventArgs e)
        {
            ButtonSpinner spinner = (ButtonSpinner)sender;
            TextBox txtBox = (TextBox)spinner.Content;

            try
            {
                int value = String.IsNullOrEmpty(txtBox.Text) ? 0 : Convert.ToInt32(txtBox.Text);
                if (e.Direction == SpinDirection.Increase)
                {
                    value++;
                }
                else
                {
                    if (value > 1)
                    {
                        value--;
                    }
                }
                txtBox.Text = value.ToString();
            }
            catch
            {
                // txtBox.Text = "1";   // make the user do this.
            }
        }
    }
}
