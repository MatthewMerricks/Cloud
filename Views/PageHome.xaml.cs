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
using CleanShutdown.Messaging;
using CloudApiPublic.Support;
using CloudApiPrivate.Model.Settings;

namespace win_client.Views
{
    public partial class PageHome : Page, IOnNavigated
    {
        #region "Instance Variables"

        private PageHomeViewModel _viewModel = null;
        private bool _isLoaded = false;
        private CLTrace _trace = CLTrace.Instance;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageHome()
        {
            try
            {
                _trace.writeToLog(9, "PageHome: PageHome constructor: Call InitializeComponent.");
                InitializeComponent();
                _trace.writeToLog(9, "PageHome: PageHome constructor: Back from InitializeComponent.");

                // Register event handlers
                Loaded += new RoutedEventHandler(PageHome_Loaded);
                Unloaded += new RoutedEventHandler(PageHome_Unloaded);

                // Pass the view's grid to the view model for the dialogs to use.
                _viewModel = (PageHomeViewModel)DataContext;
                _viewModel.ViewGridContainer = LayoutRoot;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "PageHome: PageHome: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (PageHome).  Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode));
                global::System.Windows.Application.Current.Shutdown(0);
            }
            _trace.writeToLog(9, "PageHome: PageHome constructor: Exit.");
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
            CLAppMessages.Message_PageMustUnregisterWindowClosingMessage.Register(this, OnMessage_PageMustUnregisterWindowClosingMessage);

            CLAppDelegate.ShowMainWindow(Window.GetWindow(this));

            SetSignInButtonEnabledState();
            cmdCreateAccount.Focus();
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void PageHome_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            // Unregister for messages
            Messenger.Default.Unregister(this);
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
                PageHomeViewModel vm = (PageHomeViewModel)DataContext;
                if (vm.WindowCloseRequested.CanExecute(null))
                {
                    vm.WindowCloseRequested.Execute(null);
                }

                // Get the answer and set the real event Cancel flag appropriately.
                message.Execute(!vm.WindowCloseOk);      // true == abort shutdown
            }
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

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (tbPassword.Text.Length == 0)
            {
                tblkPasswordWatermark.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            tblkPasswordWatermark.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
        {

            SetSignInButtonEnabledState();
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {

            SetSignInButtonEnabledState();
        }

        private void SetSignInButtonEnabledState()
        {
            int tbEMailLength = 0;
            bool tbEMailValidationError = false;
            if (tbEMail != null)
            {
                tbEMailLength = tbEMail.Text.Length;
                tbEMailValidationError = Validation.GetHasError(tbEMail);
            }

            int tbPasswordLength = 0;
            if (tbPassword != null)
            {
                tbPasswordLength = tbPassword.Text.Length;
            }

            if (tbEMailLength == 0 || tbPasswordLength == 0 || tbEMailValidationError)
            {
                // The sign-in button should be disabled.
                if (cmdSignIn != null  && cmdCreateAccount != null)
                {
                    cmdSignIn.IsEnabled = false;

                    // The create-account button should be the default.
                    cmdSignIn.IsDefault = false;
                    cmdCreateAccount.IsDefault = true;
                }

            }
            else
            {
                // The sign-in button should be enabled.
                if (cmdSignIn != null && cmdCreateAccount != null)
                {
                    cmdSignIn.IsEnabled = true;

                    // The sign-in button should be the default.
                    cmdSignIn.IsDefault = true;
                    cmdCreateAccount.IsDefault = false;
                }
            }
        }
    }
}
