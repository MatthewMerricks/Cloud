//
//  PagePreferences.xaml.cs
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
    public partial class PagePreferences : Page, IOnNavigated
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PagePreferences()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PagePreferences_Loaded);
            Unloaded += new RoutedEventHandler(PagePreferences_Unloaded);

            // Register messages
            CLAppMessages.PagePreferences_NavigationRequest.Register(this,
                (uri) => 
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });
            CLAppMessages.PagePreferences_FrameNavigationRequest.Register(this,
                (uri) =>
                {
                    this.ContentFrame.NavigationService.Navigate(uri, UriKind.Relative);
                });
        }

        #region "Message Handlers"

        /// <summary>
        /// Loaded event handler
        /// </summary>
        void PagePreferences_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            //&&&&cmdContinue.Focus();
        }

        /// <summary>
        /// Unloaded event handler
        /// </summary>
        void PagePreferences_Unloaded(object sender, RoutedEventArgs e)
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
                    //&&&cmdContinue.Focus();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #endregion

    }
}
