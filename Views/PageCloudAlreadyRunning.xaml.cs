//
//  PageCloudAlreadyRunning.xaml.cs
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
using win_client.Model;
using CloudApiPublic.Model;

namespace win_client.Views
{
    public partial class PageCloudAlreadyRunning : Page, IOnNavigated
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageCloudAlreadyRunning()
        {
            // Register event handlers
            Loaded += new RoutedEventHandler(PageCloudAlreadyRunning_Loaded);
            Unloaded += new RoutedEventHandler(PageCloudAlreadyRunning_Unloaded);
        }

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageCloudAlreadyRunning_Loaded(object sender, RoutedEventArgs e)
        {
            // Register messages
            CLAppMessages.PageCloudAlreadyRunning_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageCloudAlreadyRunning_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Navigated event handler.  Show the window.
        /// </summary>
        CLError IOnNavigated.HandleNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                // Show the window.
                CLAppDelegate.ShowMainWindow(Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
    }
}
