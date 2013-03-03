//
//  FramePreferencesShortcuts.xaml.cs
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
using Cloud.Model;
using win_client.Model;
using System.Linq.Expressions;
using CleanShutdown.Messaging;
using Cloud.Support;

namespace win_client.Views
{
    public partial class FramePreferencesShortcuts : Page, IOnNavigated
    {
        #region "Private Instance Variables"

        private bool _isLoaded = false;
        private FramePreferencesShortcutsViewModel _viewModel = null;

        #endregion

        #region "Life Cycle"

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FramePreferencesShortcuts()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(FramePreferencesShortcuts_Loaded);
            Unloaded += new RoutedEventHandler(FramePreferencesShortcuts_Unloaded);

            // Pass the view's grid to the view model for the dialogs to use.
            _viewModel = (FramePreferencesShortcutsViewModel)DataContext;
        }


        #region Dependency Properties

        // The preferences data object.  This view will be bound to it.
        public CLPreferences Preferences
        {
            get { return (CLPreferences)GetValue(PreferencesProperty); }
            set { SetValue(PreferencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".
        private static readonly string PreferencesPropertyName = ((MemberExpression)((Expression<Func<FramePreferencesShortcuts, CLPreferences>>)(parent => parent.Preferences)).Body).Member.Name;
        public static readonly DependencyProperty PreferencesProperty =
            DependencyProperty.Register(PreferencesPropertyName, typeof(CLPreferences), typeof(FramePreferencesShortcuts), new PropertyMetadata(null, OnPreferencesChanged));
        private static void OnPreferencesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { } 

        // The Page's grid object.  This view will pass this grid to this view's ViewModel for use by the modal dialogs so the entire Page window will be grayed and inaccessible.
        public Grid PageGrid
        {
            get { return (Grid)GetValue(PageGridProperty); }
            set { SetValue(PageGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".
        private static readonly string PageGridPropertyName = ((MemberExpression)((Expression<Func<FramePreferencesShortcuts, Grid>>)(parent => parent.PageGrid)).Body).Member.Name;
        public static readonly DependencyProperty PageGridProperty =
            DependencyProperty.Register(PageGridPropertyName, typeof(Grid), typeof(FramePreferencesShortcuts), new PropertyMetadata(null, OnPageGridChanged));
        private static void OnPageGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { } 

        #endregion

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void FramePreferencesShortcuts_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as FramePreferencesShortcutsViewModel;
            _viewModel.ViewGridContainer = PageGrid;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            //tbEMail.Focus();
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void FramePreferencesShortcuts_Unloaded(object sender, RoutedEventArgs e)
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
                    chkShowCloudFolderOnDesktop.Focus();
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                return ex;
            }
            return null;
        }

        #endregion
    }
}
