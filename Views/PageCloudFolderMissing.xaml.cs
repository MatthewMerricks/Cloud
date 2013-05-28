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
using System.IO;
using System.Windows.Navigation;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using Ookii.Dialogs.WpfMinusTaskDialog;
using win_client.AppDelegate;
using win_client.Model;
using win_client.Resources;
using Cloud.Model;
using CleanShutdown.Messaging;
using Cloud.Support;

namespace win_client.Views
{
    public partial class PageCloudFolderMissing : Page, IOnNavigated
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageCloudFolderMissing()
        {
            // Register event handlers
            Loaded += new RoutedEventHandler(PageCloudFolderMissing_Loaded);
            Unloaded += new RoutedEventHandler(PageCloudFolderMissing_Unloaded);
        }

        /// <summary>
        /// Message handler: The user should choose a new location for the cloud folder.
        /// </summary>
        private void OnMessage_PageCloudFolderMissingShouldChooseCloudFolder(string obj)
        {
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            folderBrowser.Description = String.Format(win_client.Resources.Resources.pageCloudFolderMissingFolderBrowserDescription, 
                                    Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
            folderBrowser.RootFolder = Environment.SpecialFolder.MyDocuments;  // no way to get to the user's home directory.  RootFolder is a SpecialFolder.
            folderBrowser.ShowNewFolderButton = true;
            bool? wasOkButtonClicked = folderBrowser.ShowDialog(Window.GetWindow(this));
            if (wasOkButtonClicked == true)
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
            // Register messages
            CLAppMessages.PageCloudFolderMissing_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });
            CLAppMessages.Message_PageCloudFolderMissingShouldChooseCloudFolder.Register(this, OnMessage_PageCloudFolderMissingShouldChooseCloudFolder);

            // Set the view's grid into the view model.
            PageCloudFolderMissingViewModel vm = (PageCloudFolderMissingViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageCloudFolderMissing_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unregister for messages
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Navigated event handler.
        /// </summary>
        CLError IOnNavigated.HandleNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                // Register to receive the ConfirmShutdown message
                Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                    this,
                    message =>
                    {
                        OnConfirmShutdownMessage(message);
                    });
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                return ex;
            }
            return null;
        }
        /// <summary>
        /// NavigationWindow sends this to all pages prior to driving the HandleNavigated event above.
        /// Upon receipt, the page must unregister the WindowClosingMessage.
        /// </summary>
        private void OnMessage_PageMustUnregisterWindowClosingMessage(string obj)
        {
            Messenger.Default.Unregister<CleanShutdown.Messaging.NotificationMessageAction<bool>>(this, message => { });
        }

        /// <summary>
        /// The user clicked the 'X' on the NavigationWindow.  That sent a ConfirmShutdown message.
        /// If we will handle the shutdown ourselves, inform the ShutdownService that it should abort
        /// the automatic Window.Close (set true to message.Execute.
        /// </summary>
        private void OnConfirmShutdownMessage(CleanShutdown.Messaging.NotificationMessageAction<bool> message)
        {
            if (message.Notification == Notifications.ConfirmShutdown)
            {
                // Ask the ViewModel if we should allow the window to close.
                // This should not block.
                PageCloudFolderMissingViewModel vm = (PageCloudFolderMissingViewModel)DataContext;
                if (vm.WindowCloseRequested.CanExecute(null))
                {
                    vm.WindowCloseRequested.Execute(null);
                }

                // Get the answer and set the real event Cancel flag appropriately.
                message.Execute(!vm.WindowCloseOk);      // true == abort shutdown
            }
        }

    }
}
