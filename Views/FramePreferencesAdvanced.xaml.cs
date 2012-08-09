//
//  FramePreferencesAdvanced.xaml.cs
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
using win_client.Resources;
using System.Linq.Expressions;
using Ookii.Dialogs.WpfMinusTaskDialog;
using CleanShutdown.Messaging;

namespace win_client.Views
{
    public partial class FramePreferencesAdvanced : Page, IOnNavigated
    {
        #region "Private Instance Variables"

        private bool _isLoaded = false;
        private FramePreferencesAdvancedViewModel _viewModel = null;

        #endregion

        #region "Life Cycle"

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FramePreferencesAdvanced()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(FramePreferencesAdvanced_Loaded);
            Unloaded += new RoutedEventHandler(FramePreferencesAdvanced_Unloaded);

            // Pass the view's grid to the view model for the dialogs to use.
            _viewModel = (FramePreferencesAdvancedViewModel)DataContext;
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
            DependencyProperty.Register(PreferencesPropertyName, typeof(CLPreferences), typeof(FramePreferencesAdvanced), new PropertyMetadata(null, OnPreferencesChanged));
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
            DependencyProperty.Register(PageGridPropertyName, typeof(Grid), typeof(FramePreferencesAdvanced), new PropertyMetadata(null, OnPageGridChanged));
        private static void OnPageGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        #endregion

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void FramePreferencesAdvanced_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as FramePreferencesAdvancedViewModel;
            _viewModel.ViewGridContainer = PageGrid;
            _viewModel.Preferences = Preferences;

            // Register messages
            CLAppMessages.Message_FramePreferencesAdvanced_ShouldChooseCloudFolder.Register(this, OnMessage_FramePreferencesAdvanced_ShouldChooseCloudFolder);

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

        }

        /// <summary>
        /// Let the user choose a new Cloud folder location.
        /// </summary>
        private void OnMessage_FramePreferencesAdvanced_ShouldChooseCloudFolder(string obj)
        {
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            folderBrowser.Description = win_client.Resources.Resources.FramePreferencesAdvanced_FolderBrowserDescription;
            folderBrowser.RootFolder = Environment.SpecialFolder.MyDocuments;  // no way to get to the user's home directory.  RootFolder is a SpecialFolder.
            folderBrowser.ShowNewFolderButton = true;
            bool? wasOkButtonClicked = folderBrowser.ShowDialog(Window.GetWindow(this));
            if (wasOkButtonClicked.HasValue && wasOkButtonClicked.Value)
            {
                // The user selected a folder.  Deliver the path to the ViewModel to process.
                FramePreferencesAdvancedViewModel vm = (FramePreferencesAdvancedViewModel)DataContext;
                if (vm.FramePreferencesAdvancedViewModel_CreateCloudFolderCommand.CanExecute(folderBrowser.SelectedPath))
                {
                    vm.FramePreferencesAdvancedViewModel_CreateCloudFolderCommand.Execute(folderBrowser.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void FramePreferencesAdvanced_Unloaded(object sender, RoutedEventArgs e)
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
