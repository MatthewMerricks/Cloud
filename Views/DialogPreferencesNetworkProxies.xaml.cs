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

namespace win_client.Views
{
    public partial class DialogPreferencesNetworkProxies : ChildWindow, IModalWindow
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DialogPreferencesNetworkProxies()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(DialogPreferencesNetworkProxies_Loaded);
            Unloaded += new RoutedEventHandler(DialogPreferencesNetworkProxies_Unloaded);

            // Register messages
            CLAppMessages.Message_PageCloudFolderMissingShouldChooseCloudFolder.Register(this, OnDialogPreferencesNetworkProxies_GetClearPasswordField);
            CLAppMessages.DialogPreferencesNetworkProxies_FocusToError_Message.Register(this, OnDialogPreferencesNetworkProxies_FocusToError_Message);
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

            this.DialogResult = true;
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
            FocusedElement = this.btnOK;

            DialogPreferencesNetworkProxiesViewModel vm = (DialogPreferencesNetworkProxiesViewModel)DataContext;
            vm.ViewLayoutRoot = this.LayoutRoot;
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

    }
}
