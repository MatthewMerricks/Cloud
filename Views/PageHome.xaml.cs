//
//  PageHome.xaml.cs
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
using win_client.ViewModels;
using win_client.Common;

namespace win_client.Views
{
    public partial class PageHome : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        public PageHome()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(PageHome_Loaded);
            Unloaded += new RoutedEventHandler(PageHome_Unloaded);

            Messenger.Default.Register<Uri>(this, "PageHome_NavigationRequest", 
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
            CLAppMessages.Home_FocusToError.Register(this, OnHome_FocusToError_Message);

        }

        #region "Message Handlers"

        void PageHome_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            tbEMail.Focus();
        }

        void PageHome_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(_isLoaded)
            {
                tbEMail.Focus();
            }

            var vm = DataContext as PageHomeViewModel;
            vm.PageHome_NavigatedToCommand.Execute(null);
        }

        private void OnHome_FocusToError_Message(string notUsed)
        {
            if(Validation.GetHasError(tbEMail) == true)
            {
                tbEMail.Focus();
                return;
            }
            if(Validation.GetHasError(pbPassword) == true)
            {
                pbPassword.Focus();
                return;
            }
        }

        #endregion
    }
}
