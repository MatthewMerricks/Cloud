//
//  PageTour2.xaml.cs
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
using CleanShutdown.Messaging;

namespace win_client.Views
{
    public partial class PageTour2 : Page, IOnNavigated
    {
        #region "Instance Variables"

        private bool _isLoaded = false;
        private bool savedRightButtonIsDefault = false;
        private bool savedRightButtonIsCancel = false;
        private bool savedLeftButtonIsDefault = false;
        private bool savedLeftButtonIsCancel = false;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageTour2()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PageTour2_Loaded);
            Unloaded += new RoutedEventHandler(PageTour2_Unloaded);
        }

        #region "Message Handlers"

        /// <summary>
        /// Loaded event handler
        /// </summary>
        void PageTour2_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            // Register messages
            CLAppMessages.PageTour_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });
            CLAppMessages.Message_SaveAndDisableIsDefaultAndIsCancelProperties.Register(this, OnMessage_SaveAndDisableIsDefaultAndIsCancelProperties);
            CLAppMessages.Message_RestoreIsDefaultAndIsCancelProperties.Register(this, Message_RestoreIsDefaultAndIsCancelProperties);

            // Tell all other listeners to save and disable the IsDefault and IsCancel button properties.  This should be the only active modal dialog.
            CLAppMessages.Message_SaveAndDisableIsDefaultAndIsCancelProperties.Send(this);

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
        void PageTour2_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            // Tell all other listeners to save and disable the IsDefault and IsCancel button properties.  This should be the only active modal dialog.
            CLAppMessages.Message_RestoreIsDefaultAndIsCancelProperties.Send(this);

            // Unregister for messages
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Save and disable any IsDefault or IsCancel properties.
        /// </summary>
        private void OnMessage_SaveAndDisableIsDefaultAndIsCancelProperties(object sender)
        {
            PageTour2 castSender = sender as PageTour2;
            if (castSender != this)
            {
                // Save the state of the IsDefault and IsCancel button properties.
                savedRightButtonIsDefault = this.cmdContinue.IsDefault;
                savedRightButtonIsCancel = this.cmdContinue.IsCancel;
                savedLeftButtonIsDefault = this.cmdBack.IsDefault;
                savedLeftButtonIsCancel = this.cmdBack.IsCancel;

                // Clear the button properties.
                this.cmdContinue.IsDefault = false;
                this.cmdContinue.IsCancel = false;
                this.cmdBack.IsDefault = false;
                this.cmdBack.IsCancel = false;
            }
        }

        /// <summary>
        /// Restore any IsDefault or IsCancel properties.
        /// </summary>
        private void Message_RestoreIsDefaultAndIsCancelProperties(object sender)
        {
            PageTour2 castSender = sender as PageTour2;
            if (castSender != this)
            {
                // Restore the state of the IsDefault and IsCancel button properties.
                this.cmdContinue.IsDefault = savedRightButtonIsDefault;
                this.cmdContinue.IsCancel = savedRightButtonIsCancel;
                this.cmdBack.IsDefault = savedLeftButtonIsDefault;
                this.cmdBack.IsCancel = savedLeftButtonIsCancel;
            }
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
                PageTourViewModel vm = (PageTourViewModel)DataContext;
                if (vm.WindowCloseRequested.CanExecute(null))
                {
                    vm.WindowCloseRequested.Execute(null);
                }

                // Get the answer and set the real event Cancel flag appropriately.
                message.Execute(!vm.WindowCloseOk);      // true == abort shutdown
            }
        }

        #endregion

    }
}
