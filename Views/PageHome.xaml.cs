//
//  PageHome.xaml.cs
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
using win_client.ViewModels;
using win_client.Common;
using win_client.AppDelegate;
using CloudApiPublic.Model;
using win_client.Model;

namespace win_client.Views
{
    public partial class PageHome : Page, IOnNavigated
    {
        #region "Instance Variables"

        private PageHomeViewModel _viewModel = null;
        private bool _isLoaded = false;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageHome()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(PageHome_Loaded);
            Unloaded += new RoutedEventHandler(PageHome_Unloaded);

            // Pass the view's grid to the view model for the dialogs to use.
            _viewModel = (PageHomeViewModel)DataContext;
            _viewModel.ViewGridContainer = LayoutRoot;

        }

        #region "Event Handlers"

        /// <summary>
        /// Loaded event handler.
        /// </summary>
        void PageHome_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _viewModel = DataContext as PageHomeViewModel;

            // Register messages
            CLAppMessages.PageHome_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });

            CLAppMessages.Home_FocusToError.Register(this, OnHome_FocusToError_Message);
            CLAppMessages.Home_GetClearPasswordField.Register(this, OnHome_GetClearPasswordField);

            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            tbEMail.Focus();
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageHome_Unloaded(object sender, RoutedEventArgs e)
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
                CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

                if (_isLoaded)
                {
                    tbEMail.Focus();
                }

                _viewModel.PageHome_NavigatedToCommand.Execute(null);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }


        #endregion
        #region Message Handlers

        private void OnHome_FocusToError_Message(string notUsed)
        {
            if(Validation.GetHasError(tbEMail) == true)
            {
                tbEMail.Focus();
                return;
            }
            if(Validation.GetHasError(tbPassword) == true)
            {
                tbPassword.Focus();
                return;
            }
        }

        private void OnHome_GetClearPasswordField(string notUsed)
        {
            string clearPassword = tbPassword.Text;
            if (_viewModel != null)
            {
                _viewModel.Password2 = clearPassword;
            }
        }

        #endregion
    }
}
