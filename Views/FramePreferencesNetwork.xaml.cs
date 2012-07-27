//
//  FramePreferencesNetwork.xaml.cs
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

namespace win_client.Views
{
    public partial class FramePreferencesNetwork : Page, IOnNavigated
    {
        #region "Private Instance Variables"

        private bool _isLoaded = false;
        private FramePreferencesNetworkViewModel _viewModel = null;

        #endregion

        #region "Life Cycle"

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FramePreferencesNetwork()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(FramePreferencesNetwork_Loaded);
            Unloaded += new RoutedEventHandler(FramePreferencesNetwork_Unloaded);

            // Pass the view's grid to the view model for the dialogs to use.
            _viewModel = (FramePreferencesNetworkViewModel)DataContext;
        }

        #region Dependency Properties

        public CLPreferences Preferences
        {
            get { return (CLPreferences)GetValue(PreferencesProperty); }
            set { SetValue(PreferencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".  Better than hardcoding the string.
        private static readonly string PreferencesPropertyName = ((MemberExpression)((Expression<Func<PagePreferences, CLPreferences>>)(parent => parent.Preferences)).Body).Member.Name;
        public static readonly DependencyProperty PreferencesProperty =
            DependencyProperty.Register(PreferencesPropertyName, typeof(CLPreferences), typeof(FramePreferencesNetwork), new PropertyMetadata(null, OnPreferencesChanged));
        private static void OnPreferencesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        // The Page's grid object.  This view will pass this grid to this view's ViewModel for use by the modal dialogs so the entire Page window will be grayed and inaccessible.
        public Grid PageGrid
        {
            get { return (Grid)GetValue(PageGridProperty); }
            set { SetValue(PageGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".
        private static readonly string PageGridPropertyName = ((MemberExpression)((Expression<Func<PagePreferences, Grid>>)(parent => parent.PageGrid)).Body).Member.Name;
        public static readonly DependencyProperty PageGridProperty =
            DependencyProperty.Register(PageGridPropertyName, typeof(Grid), typeof(FramePreferencesNetwork), new PropertyMetadata(null, OnPageGridChanged));
        private static void OnPageGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        #endregion

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void FramePreferencesNetwork_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as FramePreferencesNetworkViewModel;
            _viewModel.ViewGridContainer = PageGrid;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void FramePreferencesNetwork_Unloaded(object sender, RoutedEventArgs e)
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

                this.SetBinding(PreferencesProperty,
                    new Binding()
                    {
                        Source = e.ExtraData,
                        Path = new PropertyPath(PreferencesPropertyName),
                        Mode = BindingMode.OneWay
                    });

                this.SetBinding(PageGridProperty,
                    new Binding()
                    {
                        Source = e.ExtraData,
                        Path = new PropertyPath(PageGridPropertyName),
                        Mode = BindingMode.OneWay
                    });

                if (_isLoaded)
                {
                    //TODO: Give some control focus?  Maybe not.  Only two buttons here.
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
