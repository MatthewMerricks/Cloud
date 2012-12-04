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
using CleanShutdown.Messaging;
using CloudApiPublic.Support;

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

            // Set the view's grid into the view model.
            PageCloudAlreadyRunningViewModel vm = (PageCloudAlreadyRunningViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;

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
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
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
                PageCloudAlreadyRunningViewModel vm = (PageCloudAlreadyRunningViewModel)DataContext;
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
