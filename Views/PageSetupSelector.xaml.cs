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

        public PageSetupSelector()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(PageSetupSelector_Loaded);
            Unloaded += new RoutedEventHandler(PageSetupSelector_Unloaded);

#if _SILVERLIGHT
            Messenger.Default.Register<Uri>(this, "PageSetupSelector_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
#else
            Messenger.Default.Register<Uri>(this, "PageSetupSelector_NavigationRequest",
                (uri) => { this.NavigationService.Navigate(uri); });
#endif

        }

        #region "Message Handlers"

        void PageSetupSelector_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
#if !_SILVERLIGHT
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);
#endif
            cmdContinue.Focus();
        }

        void PageSetupSelector_Unloaded(object sender, RoutedEventArgs e)
        {
#if !_SILVERLIGHT
            NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
#endif
            Messenger.Default.Unregister(this);
        }

#if _SILVERLIGHT
        protected override void OnNavigatedTo(NavigationEventArgs e)
#else
        protected void OnNavigatedTo(object sender, NavigationEventArgs e)
#endif
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
