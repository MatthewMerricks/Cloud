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

namespace win_client.Views
{
    public partial class PageSetupSelector : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        #region "Life Cycle

        public PageSetupSelector()
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

            Loaded += new RoutedEventHandler(PageSetupSelector_Loaded);
            Unloaded += new RoutedEventHandler(PageSetupSelector_Unloaded);

            Messenger.Default.Register<Uri>(this, "PageSetupSelector_NavigationRequest",
                (uri) => 
                {
                    if (!_isLoaded)
                    {
                        int i = 0;
                        i++;
                    }
                    this.NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });
            PageSetupSelectorViewModel vm = (PageSetupSelectorViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;
        }

        #endregion

        #region "Public Methods"
        /// <summary>
        /// Get this view instance. <See cref="getViewInstance" />
        /// </summary>
        public PageSetupSelector getViewInstance()
        {
            return this;
        }

        #endregion


        #region "Message Handlers"

        void PageSetupSelector_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);
            cmdContinue.Focus();
        }

        void PageSetupSelector_Unloaded(object sender, RoutedEventArgs e)
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

            var vm = DataContext as PageSetupSelectorViewModel;
            vm.PageSetupSelector_NavigatedToCommand.Execute(null);
        }

        #endregion

    }
}
