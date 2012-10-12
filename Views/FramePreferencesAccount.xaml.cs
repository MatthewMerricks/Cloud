//
//  FramePreferencesAccount.xaml.cs
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
using CleanShutdown.Messaging;

namespace win_client.Views
{
    public partial class FramePreferencesAccount : Page, IOnNavigated
    {
        #region "Private Instance Variables"

        private bool _isLoaded = false;
        private FramePreferencesAccountViewModel _viewModel = null;

        #endregion

        #region "Life Cycle"

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FramePreferencesAccount()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(FramePreferencesAccount_Loaded);
            Unloaded += new RoutedEventHandler(FramePreferencesAccount_Unloaded);

            // Pass the view's grid to the view model for the dialogs to use.
            _viewModel = (FramePreferencesAccountViewModel)DataContext;
        }

        #region Dependency Properties

        public ICommand UnlinkCommand
        {
            get { return (ICommand)GetValue(UnlinkCommandProperty); }
            set { SetValue(UnlinkCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".
        private static readonly string UnlinkCommandPropertyName = ((MemberExpression)((Expression<Func<FramePreferencesAccount, ICommand>>)(parent => parent.UnlinkCommand)).Body).Member.Name;
        public static readonly DependencyProperty UnlinkCommandProperty =
            DependencyProperty.Register(UnlinkCommandPropertyName, typeof(ICommand), typeof(FramePreferencesAccount), new PropertyMetadata(null));

        public CLPreferences Preferences
        {
            get { return (CLPreferences)GetValue(PreferencesProperty); }
            set { SetValue(PreferencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".  Better than hardcoding the string.
        private static readonly string PreferencesPropertyName = ((MemberExpression)((Expression<Func<FramePreferencesAccount, CLPreferences>>)(parent => parent.Preferences)).Body).Member.Name;
        public static readonly DependencyProperty PreferencesProperty =
            DependencyProperty.Register(PreferencesPropertyName, typeof(CLPreferences), typeof(FramePreferencesAccount), new PropertyMetadata(null, OnPreferencesChanged));
        private static void OnPreferencesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        // The Page's grid object.  This view will pass this grid to this view's ViewModel for use by the modal dialogs so the entire Page window will be grayed and inaccessible.
        public Grid PageGrid
        {
            get { return (Grid)GetValue(PageGridProperty); }
            set { SetValue(PageGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "PageGrid".
        private static readonly string PageGridPropertyName = ((MemberExpression)((Expression<Func<FramePreferencesAccount, Grid>>)(parent => parent.PageGrid)).Body).Member.Name;
        public static readonly DependencyProperty PageGridProperty =
            DependencyProperty.Register(PageGridPropertyName, typeof(Grid), typeof(FramePreferencesAccount), new PropertyMetadata(null, OnPageGridChanged));
        private static void OnPageGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        #endregion

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void FramePreferencesAccount_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as FramePreferencesAccountViewModel;
            _viewModel.ViewGridContainer = PageGrid;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void FramePreferencesAccount_Unloaded(object sender, RoutedEventArgs e)
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
                this.SetBinding(UnlinkCommandProperty,
                    new Binding()
                    {
                        Source = e.ExtraData,
                        Path = new PropertyPath(UnlinkCommandPropertyName),
                        Mode = BindingMode.OneWay
                    });

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
