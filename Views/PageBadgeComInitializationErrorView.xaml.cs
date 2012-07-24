//
//  PageBadgeComInitializationErrorView.xaml.cs
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

namespace win_client.Views
{
    public partial class PageBadgeComInitializationErrorView : Page
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageBadgeComInitializationErrorView()
        {
            // Register event handlers
            Loaded += new RoutedEventHandler(PageBadgeComInitializationError_Loaded);
            Unloaded += new RoutedEventHandler(PageBadgeComInitializationError_Unloaded);

            // Register messages
            CLAppMessages.PageBadgeComInitializationError_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });
        }

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageBadgeComInitializationError_Loaded(object sender, RoutedEventArgs e)
        {
            // Register the navigated event.
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageBadgeComInitializationError_Unloaded(object sender, RoutedEventArgs e)
        {

            if (NavigationService != null)
            {
                NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo); ;
            }
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Navigated event handler.  Show the window.
        /// </summary>
        protected void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));
        }
    }
}
