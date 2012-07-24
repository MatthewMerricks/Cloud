//
//  PageTour5.xaml.cs
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
    public partial class PageTour5 : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        public PageTour5()
        {

            // Remove the navigation bar
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                var navWindow = Window.GetWindow(this) as NavigationWindow;
                if (navWindow != null)
                {
                    navWindow.ShowsNavigationUI = false;
                }
            }));

            Loaded += new RoutedEventHandler(PageTour5_Loaded);
            Unloaded += new RoutedEventHandler(PageTour5_Unloaded);

            CLAppMessages.PageTour_NavigationRequest.Register(this,
                (uri) => 
                {
                    this.NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });

        }

        #region "Message Handlers"

        void PageTour5_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);
            cmdContinue.Focus();
        }

        void PageTour5_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            if (NavigationService != null)
            {
                NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo); ;
            }
            Messenger.Default.Unregister(this);
        }

        protected void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            if (_isLoaded)
            {
                cmdContinue.Focus();
            }
        }

        #endregion

    }
}
