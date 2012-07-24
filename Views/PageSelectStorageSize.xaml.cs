//
//  PageSelectStorageSize.xaml.cs
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
using System.Globalization;
using win_client.ViewModels;
using win_client.AppDelegate;

namespace win_client.Views
{
    public partial class PageSelectStorageSize : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageSelectStorageSize()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PageSelectStorageSize_Loaded);
            Unloaded += new RoutedEventHandler(PageSelectStorageSize_Unloaded);

            // Register messages
            CLAppMessages.PageSelectStorageSize_NavigationRequest.Register(this,
               (uri) => 
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative); 
                });
            CLAppMessages.SelectStorageSize_PresentMessageDialog.Register(this, SelectStorageSize_PresentMessageDialog);
            
        }

         #region "Event Handlers"

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageSelectStorageSize_Loaded(object sender, RoutedEventArgs e)
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
        void PageSelectStorageSize_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            if (NavigationService != null)
            {
                NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo); ;
            }
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// REMOVE THIS.  Need credit card UI.
        /// </summary>
        //TODO: Remove this.  Implement the credit card UI.
        private void SelectStorageSize_PresentMessageDialog(DialogMessage msg)
        {
            var result = MessageBox.Show(
                msg.Content,
                msg.Caption,
                msg.Button);

            msg.ProcessCallback(result);     // Send callback
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

            var vm = DataContext as PageSelectStorageSizeViewModel;
            vm.PageSelectStorageSize_NavigatedToCommand.Execute(null);
        }
 
        #endregion


    }

}
