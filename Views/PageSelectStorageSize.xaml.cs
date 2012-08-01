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
using CloudApiPublic.Model;
using win_client.Model;

namespace win_client.Views
{
    public partial class PageSelectStorageSize : Page, IOnNavigated
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
        }

         #region "Event Handlers"

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageSelectStorageSize_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            // Register messages
            CLAppMessages.PageSelectStorageSize_NavigationRequest.Register(this,
               (uri) =>
               {
                   this.NavigationService.Navigate(uri, UriKind.Relative);
               });
            CLAppMessages.SelectStorageSize_PresentMessageDialog.Register(this, SelectStorageSize_PresentMessageDialog);

            // Pass the view's grid to the view model for the dialogs to use.
            PageSelectStorageSizeViewModel vm = (PageSelectStorageSizeViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;

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

                var vm = DataContext as PageSelectStorageSizeViewModel;
                vm.PageSelectStorageSize_NavigatedToCommand.Execute(null);
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
