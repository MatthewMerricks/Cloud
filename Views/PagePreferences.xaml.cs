﻿//
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

namespace win_client.Views
{
    public partial class PagePreferences : Page, IOnNavigated
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

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
            _isLoaded = true;

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

            //&&&&cmdContinue.Focus();
        }

        /// <summary>
        /// Unloaded event handler
        /// </summary>
        void PagePreferences_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            Messenger.Default.Unregister(this);
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

