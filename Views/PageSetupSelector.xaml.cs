//
//  PageSetupSelector.xaml.cs
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
    public partial class PageSetupSelector : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        #region "Life Cycle

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageSetupSelector()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PageSetupSelector_Loaded);
            Unloaded += new RoutedEventHandler(PageSetupSelector_Unloaded);

            // Register messages
            CLAppMessages.PageSetupSelector_NavigationRequest.Register(this,
                (uri) => 
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });

            // Pass the view's grid to the viewmodel for use with the dialogs.
            PageSetupSelectorViewModel vm = (PageSetupSelectorViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;
        }

        #endregion

        #region "Message Handlers"

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageSetupSelector_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            cmdContinue.Focus();
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageSetupSelector_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            if (NavigationService != null)
            {
                NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo); ;
            }
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Navigated event handler.
        /// </summary>
        protected void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            if (_isLoaded)
            {
                cmdContinue.Focus();
            } 

            var vm = DataContext as PageSetupSelectorViewModel;
            vm.PageSetupSelector_NavigatedToCommand.Execute(null);
        }

        #endregion

    }
}
