//
//  PageTour3.xaml.cs
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
    public partial class PageTour3 : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        public PageTour3()
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

            Loaded += new RoutedEventHandler(PageTour3_Loaded);
            Unloaded += new RoutedEventHandler(PageTour3_Unloaded);

#if SILVERLIGHT
            Messenger.Default.Register<Uri>(this, "PageTour_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
#else
            Messenger.Default.Register<Uri>(this, "PageTour_NavigationRequest",
                (uri) => 
                {
                    this.NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });
#endif

        }

        #region "Message Handlers"

        void PageTour3_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
#if !SILVERLIGHT
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);
#endif
            cmdContinue.Focus();
        }

        void PageTour3_Unloaded(object sender, RoutedEventArgs e)
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
                cmdContinue.Focus();
            }
        }

        #endregion

    }
}
