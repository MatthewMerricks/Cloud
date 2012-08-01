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
using win_client.AppDelegate;
using CloudApiPublic.Model;
using win_client.Model;

namespace win_client.Views
{
    public partial class PageTour3 : Page, IOnNavigated
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageTour3()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PageTour3_Loaded);
            Unloaded += new RoutedEventHandler(PageTour3_Unloaded);

        }

        #region "Message Handlers"

        /// <summary>
        /// Loaded event handler
        /// </summary>
        void PageTour3_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            // Register messages
            CLAppMessages.PageTour_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });

            // Set the view's grid into the view model.
            PageTourViewModel vm = (PageTourViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            cmdContinue.Focus();
        }

        /// <summary>
        /// Unloaded event handler
        /// </summary>
        void PageTour3_Unloaded(object sender, RoutedEventArgs e)
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
                    cmdContinue.Focus();
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
