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
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using win_client.AppDelegate;
using CloudApiPublic.Model;
using win_client.Model;

namespace win_client.Views
{
    public partial class PageCreateNewAccount : Page, IOnNavigated
    {
        #region "Instance Variables"

        private bool _isLoaded = false;
        private PageCreateNewAccountViewModel _viewModel = null;

        #endregion

        #region "Life Cycle"

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageCreateNewAccount()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PageCreateNewAccount_Loaded);
            Unloaded += new RoutedEventHandler(PageCreateNewAccount_Unloaded);

            // Register messages
            CLAppMessages.PageCreateNewAccount_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });

            CLAppMessages.CreateNewAccount_FocusToError.Register(this, OnCreateNewAccount_FocusToError_Message);
            CLAppMessages.CreateNewAccount_GetClearPasswordField.Register(this, OnCreateNewAccount_GetClearPasswordField);
            CLAppMessages.CreateNewAccount_GetClearConfirmPasswordField.Register(this, OnCreateNewAccount_GetClearConfirmPasswordField);

            // Pass the view's grid to the view model for the dialogs to use.
            _viewModel = (PageCreateNewAccountViewModel)DataContext;
            _viewModel.ViewGridContainer = LayoutRoot;

        }

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageCreateNewAccount_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as PageCreateNewAccountViewModel;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            tbEMail.Focus();
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageCreateNewAccount_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Navigated event handler.
        /// </summary>
        CLError IOnNavigated.HandleNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                // Show the window.
                CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

                if (_isLoaded)
                {
                    tbEMail.Focus();
                }

                _viewModel.PageCreateNewAccount_NavigatedToCommand.Execute(null);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #endregion

        #region "Message Handlers"

        private void OnCreateNewAccount_GetClearPasswordField(string notUsed)
        {
            string clearPassword = tbPassword.Text;
            if (_viewModel != null)
            {
                _viewModel.Password2 = clearPassword;
            }
        }

        private void OnCreateNewAccount_GetClearConfirmPasswordField(string notUsed)
        {
            string clearConfirmPassword = tbConfirmPassword.Text;
            if (_viewModel != null)
            {
                _viewModel.ConfirmPassword2 = clearConfirmPassword;
            }
        }

        private void OnCreateNewAccount_FocusToError_Message(string notUsed)
        {
            if (Validation.GetHasError(tbEMail) == true )  {
                tbEMail.Focus();
                return;
            }
            if (Validation.GetHasError(tbFullName) == true )  {
                tbFullName.Focus();
                return;
            }
            if(Validation.GetHasError(this.tbPassword) == true)
                {
                tbPassword.Focus();
                return;
            }
            if(Validation.GetHasError(tbConfirmPassword) == true)
            {
                tbConfirmPassword.Focus();
                return;
            }
            if(Validation.GetHasError(tbComputerName) == true)
            {
                tbComputerName.Focus();
                return;
            }
        }

        #endregion "ChangeScreenMessage"

    }
}
