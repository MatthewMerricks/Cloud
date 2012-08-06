//
//  PagePreferences.xaml.cs
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
using System.Linq.Expressions;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using CleanShutdown.Messaging;

namespace win_client.Views
{
    public partial class PagePreferences : Page, IOnNavigated
    {
        #region "Instance Variables"

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PagePreferences()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PagePreferences_Loaded);
            Unloaded += new RoutedEventHandler(PagePreferences_Unloaded);
        }

        #region Dependency Properties


        public CLPreferences Preferences
        {
            get { return (CLPreferences)GetValue(PreferencesProperty); }
            set { SetValue(PreferencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreferencesProperty =
            DependencyProperty.Register(((MemberExpression)((Expression<Func<PagePreferences, CLPreferences>>)(parent => parent.Preferences)).Body).Member.Name, typeof(CLPreferences), typeof(PagePreferences), new PropertyMetadata(null, OnPreferencesChanged));

        private static void OnPreferencesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        // The Page's grid object.  Frame views will pass this grid to the Frame view's ViewModel for use by the modal dialogs so the entire Page window will be grayed and inaccessible.
        public Grid PageGrid
        {
            get { return (Grid)GetValue(PageGridProperty); }
            set { SetValue(PageGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".
        private static readonly string PageGridPropertyName = ((MemberExpression)((Expression<Func<PagePreferences, Grid>>)(parent => parent.PageGrid)).Body).Member.Name;
        public static readonly DependencyProperty PageGridProperty =
            DependencyProperty.Register(PageGridPropertyName, typeof(Grid), typeof(PagePreferences), new PropertyMetadata(null, OnPageGridChanged));
        private static void OnPageGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { } 

        #endregion

        #region "Message Handlers"

        /// <summary>
        /// Loaded event handler
        /// </summary>
        void PagePreferences_Loaded(object sender, RoutedEventArgs e)
        {
            // Register messages
            CLAppMessages.PagePreferences_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri);
                });
            CLAppMessages.PagePreferences_FrameNavigationRequest.Register(this,
                (uri) =>
                {
                    this.ContentFrame.NavigationService.Navigate(uri);
                });
            CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Register(this,
                (nextPage) =>
                {
                    this.PageGrid = this.LayoutRoot;
                    this.Preferences = nextPage.Value;
                    this.ContentFrame.NavigationService.Navigate(nextPage.Key, this);
                });

            this.ContentFrame.NavigationService.Navigated += MyNavigationWindow.NavigationService_Navigated;

            // Ignore F5 refresh for dialogs in this page's frame.
            this.ContentFrame.NavigationService.Navigating += MyNavigationWindow.NavigationService_Navigating;

            // Ignore F5 refresh for this page.
            this.NavigationService.Navigating += MyNavigationWindow.NavigationService_Navigating;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            // Set our content window to the ViewModel
            PagePreferencesViewModel vm = (PagePreferencesViewModel)DataContext;
            vm.ViewGridContainer = this.LayoutRoot;

            // Give focus to the General button.
            cmdGeneral.Focus();

            // And auto-click it.
            ButtonAutomationPeer peer = new ButtonAutomationPeer(cmdGeneral);
            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            if (invokeProv != null)
            {
                invokeProv.Invoke();
            }
        }

        /// <summary>
        /// Unloaded event handler
        /// </summary>
        void PagePreferences_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// Navigated event handler.
        /// </summary>
        CLError IOnNavigated.HandleNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                // Send this event to the ViewModel
                PagePreferencesViewModel vm = (PagePreferencesViewModel)DataContext;
                if (vm.OnNavigated.CanExecute(null))
                {
                    vm.OnNavigated.Execute(null);
                }

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
                PagePreferencesViewModel vm = (PagePreferencesViewModel)DataContext;
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

