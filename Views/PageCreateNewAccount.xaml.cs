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

namespace win_client.Views
{
    public partial class PageCreateNewAccount : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        #region "Life Cycle"

        public PageCreateNewAccount()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(PageCreateNewAccount_Loaded);
            Unloaded += new RoutedEventHandler(PageCreateNewAccount_Unloaded);

            Messenger.Default.Register<Uri>(this, "PageCreateNewAccount_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
            CLAppMessages.CreateNewAccount_FocusToError.Register(this, OnCreateNewAccount_FocusToError_Message);
        }

        void PageCreateNewAccount_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            tbEMail.Focus();
        }

        void PageCreateNewAccount_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
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
            if(Validation.GetHasError(tbPassword) == true)
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
