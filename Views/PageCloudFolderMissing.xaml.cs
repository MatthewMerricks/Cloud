//
//  PageCloudFolderMissing.xaml.cs
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
using Ookii.Dialogs.Wpf;
using win_client.AppDelegate;

namespace win_client.Views
{
    public partial class PageCloudFolderMissing : Page
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageCloudFolderMissing()
        {
            // Register event handlers
            Loaded += new RoutedEventHandler(PageCloudFolderMissing_Loaded);
            Unloaded += new RoutedEventHandler(PageCloudFolderMissing_Unloaded);


            // Register messages
            CLAppMessages.PageCloudFolderMissing_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });
            CLAppMessages.Message_PageCloudFolderMissingShouldChooseCloudFolder.Register(this, OnMessage_PageCloudFolderMissingShouldChooseCloudFolder);

        }

        /// <summary>
        /// Message handler: The user should choose a new location for the cloud folder.
        /// </summary>
        private void OnMessage_PageCloudFolderMissingShouldChooseCloudFolder(string obj)
        {
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            folderBrowser.Description = CLAppDelegate.Instance.ResourceManager.GetString("pageCloudFolderMissingFolderBrowserDescription");
            folderBrowser.RootFolder = Environment.SpecialFolder.MyDocuments;  // no way to get to the user's home directory.  RootFolder is a SpecialFolder.
            folderBrowser.ShowNewFolderButton = true;
            bool? wasOkButtonClicked = folderBrowser.ShowDialog(Window.GetWindow(this));
            if (wasOkButtonClicked.HasValue && wasOkButtonClicked.Value)
            {
                // The user selected a folder.  Deliver the path to the ViewModel to process.
                PageCloudFolderMissingViewModel vm = (PageCloudFolderMissingViewModel)DataContext;
                if (vm.PageCloudFolderMissingViewModel_CreateCloudFolderCommand.CanExecute(folderBrowser.SelectedPath))
                {
                    vm.PageCloudFolderMissingViewModel_CreateCloudFolderCommand.Execute(folderBrowser.SelectedPath);
                }
            }

        }

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageCloudFolderMissing_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the view's grid into the view model.
            PageCloudFolderMissingViewModel vm = (PageCloudFolderMissingViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;

            // Register the navigated event.
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageCloudFolderMissing_Unloaded(object sender, RoutedEventArgs e)
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
