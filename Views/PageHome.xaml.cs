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
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using win_client.ViewModels;
using win_client.Common;

namespace win_client.Views
{
    public partial class PageHome : Page
    {
        #region "Instance Variables"

        private PageHomeViewModel _viewModel = null;
        private bool _isLoaded = false;

        #endregion

        public PageHome()
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

            Loaded += new RoutedEventHandler(PageHome_Loaded);
            Unloaded += new RoutedEventHandler(PageHome_Unloaded);

#if SILVERLIGHT
            Messenger.Default.Register<Uri>(this, "PageHome_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
#else
            Messenger.Default.Register<Uri>(this, "PageHome_NavigationRequest",
                (uri) =>
                {
                    this.NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });
#endif

            CLAppMessages.Home_FocusToError.Register(this, OnHome_FocusToError_Message);
            CLAppMessages.Home_GetClearPasswordField.Register(this, OnHome_GetClearPasswordField);

            PageHomeViewModel vm = (PageHomeViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;

        }

        #region "Message Handlers"

        void PageHome_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as PageHomeViewModel;
#if !SILVERLIGHT
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo); ;
#endif
            tbEMail.Focus();
        }

        void PageHome_Unloaded(object sender, RoutedEventArgs e)
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
            if(Validation.GetHasError(tbPassword) == true)
            {
                tbPassword.Focus();
                return;
            }
        }

        private void OnHome_GetClearPasswordField(string notUsed)
        {
            string clearPassword = tbPassword.Text;
            if (_viewModel != null)
            {
                _viewModel.Password2 = clearPassword;
            }
        }

        #endregion
    }
}
