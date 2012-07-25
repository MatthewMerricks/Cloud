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
            _viewModel.ViewGridContainer = LayoutRoot;

        }


        #region Dependency Properties

        public CLPreferences Preferences
        {
            get { return (CLPreferences)GetValue(PreferencesProperty); }
            set { SetValue(PreferencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Preferences.  This enables animation, styling, binding, etc...
        // The big long member expression tree results in an automatically generated string "Preferences".
        private static readonly string PreferencesPropertyName = ((MemberExpression)((Expression<Func<PagePreferences, CLPreferences>>)(parent => parent.Preferences)).Body).Member.Name;
        public static readonly DependencyProperty PreferencesProperty =
            DependencyProperty.Register(PreferencesPropertyName, typeof(CLPreferences), typeof(FramePreferencesAccount), new PropertyMetadata(null, OnPreferencesChanged));
        private static void OnPreferencesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { } 

        #endregion

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void FramePreferencesAccount_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as FramePreferencesAccountViewModel;

            // Show the window.
            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            //tbEMail.Focus();
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
                // Show the window.
                CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

                this.SetBinding(PreferencesProperty,
                    new Binding()
                    {
                        Source = e.ExtraData,
                        Path = new PropertyPath(PreferencesPropertyName),
                        Mode = BindingMode.OneWay
                    });

                if (_isLoaded)
                {
                    chkStartCloudWhenSystemStarts.Focus();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #endregion

        #region "Message Handlers"

        private void OnCreateNewAccount_GetClearPasswordField(string notUsed)
        {
            //string clearPassword = tbPassword.Text;
            //if (_viewModel != null)
            //{
            //    _viewModel.Password2 = clearPassword;
            //}
        }

        private void OnCreateNewAccount_GetClearConfirmPasswordField(string notUsed)
        {
            //string clearConfirmPassword = tbConfirmPassword.Text;
            //if (_viewModel != null)
            //{
            //    _viewModel.ConfirmPassword2 = clearConfirmPassword;
            //}
        }

        private void OnCreateNewAccount_FocusToError_Message(string notUsed)
        {
            //if (Validation.GetHasError(tbEMail) == true )  {
            //    tbEMail.Focus();
            //    return;
            //}
            //if (Validation.GetHasError(tbFullName) == true )  {
            //    tbFullName.Focus();
            //    return;
            //}
            //if(Validation.GetHasError(this.tbPassword) == true)
            //    {
            //    tbPassword.Focus();
            //    return;
            //}
            //if(Validation.GetHasError(tbConfirmPassword) == true)
            //{
            //    tbConfirmPassword.Focus();
            //    return;
            //}
            //if(Validation.GetHasError(tbComputerName) == true)
            //{
            //    tbComputerName.Focus();
            //    return;
            //}
        }

        #endregion "ChangeScreenMessage"

    }
}
