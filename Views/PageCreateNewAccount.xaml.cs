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

namespace win_client.Views
{
    public partial class PageCreateNewAccount : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;
        private PageCreateNewAccountViewModel _viewModel = null;

        #endregion

        #region "Life Cycle"

        public PageCreateNewAccount()
        {
            InitializeComponent();

            // Remove the navigation bar
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                var navWindow = Window.GetWindow(this) as NavigationWindow;
                if (navWindow != null)
                {
                    navWindow.ShowsNavigationUI = false;
                }
            }));

            Loaded += new RoutedEventHandler(PageCreateNewAccount_Loaded);
            Unloaded += new RoutedEventHandler(PageCreateNewAccount_Unloaded);

#if SILVERLIGHT
            Messenger.Default.Register<Uri>(this, "PageCreateNewAccount_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
#else
            Messenger.Default.Register<Uri>(this, "PageCreateNewAccount_NavigationRequest",
                (uri) =>
                {
                    this.NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });
#endif

            CLAppMessages.CreateNewAccount_FocusToError.Register(this, OnCreateNewAccount_FocusToError_Message);
            CLAppMessages.CreateNewAccount_GetClearPasswordField.Register(this, OnCreateNewAccount_GetClearPasswordField);
            CLAppMessages.CreateNewAccount_GetClearConfirmPasswordField.Register(this, OnCreateNewAccount_GetClearConfirmPasswordField);
        }

        void PageCreateNewAccount_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as PageCreateNewAccountViewModel;

#if !SILVERLIGHT
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);
#endif
            tbEMail.Focus();
        }

        void PageCreateNewAccount_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

#if !SILVERLIGHT
            if (NavigationService != null)
            {
                NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo); ;
            }
#endif
            Messenger.Default.Unregister(this);
        }

#if SILVERLIGHT
        protected override void OnNavigatedTo(NavigationEventArgs e)
#else
        protected void OnNavigatedTo(object sender, NavigationEventArgs e)
#endif
        {
            if (_isLoaded)
            {
                tbEMail.Focus();
            }

            var vm = DataContext as PageCreateNewAccountViewModel;
            vm.PageCreateNewAccount_NavigatedToCommand.Execute(null);
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
