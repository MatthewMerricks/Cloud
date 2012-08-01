//
//  PageCloudAlreadyRunningViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using GalaSoft.MvvmLight;
using win_client.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using win_client.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using win_client.Common;
using System.Reflection;
using System.Linq;
using CloudApiPrivate.Model.Settings;
using System.IO;
using System.Resources;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Collections.Generic;
using win_client.Views;
using win_client.AppDelegate;
using CloudApiPublic.Support;
using System.Windows.Input;
using System.ComponentModel;
using win_client.ViewModelHelpers;
using CleanShutdown.Messaging;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageCloudAlreadyRunningViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private RelayCommand _cloudAlreadyRunningViewModel_OkCommand;
        private ResourceManager _rm;
        private CLTrace _trace = CLTrace.Instance;

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageCloudAlreadyRunningViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    //&&&&               WelcomeTitle = item.Title;
                });
            _rm = CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;

            // Register to receive the ConfirmShutdown message
            Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                this,
                message =>
                {
                    OnConfirmShutdownMessage(message);
                });
        }

        #region Bindable Properties

        /// <summary>
        /// The <see cref="ViewGridContainer" /> property's name.
        /// </summary>
        public const string ViewGridContainerPropertyName = "ViewGridContainer";
        private Grid _viewGridContainer = null;
        public Grid ViewGridContainer
        {
            get
            {
                return _viewGridContainer;
            }

            set
            {
                if (_viewGridContainer == value)
                {
                    return;
                }

                _viewGridContainer = value;
                RaisePropertyChanged(ViewGridContainerPropertyName);
            }
        }

        #endregion

        /// <summary>
        /// Clean up all resources allocated, and save state as needed.
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            _rm = null;
        }

        #endregion
     
        #region Relay Commands

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        public RelayCommand CloudAlreadyRunningViewModel_OkCommand
        {
            get
            {
                return _cloudAlreadyRunningViewModel_OkCommand
                    ?? (_cloudAlreadyRunningViewModel_OkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Exit the application
                                                Application.Current.Shutdown();
                                            }));                                              
            }
        }

        #endregion

        #region Support Functions

        /// <summary>
        /// The user clicked the 'X' on the NavigationWindow.  That sent a ConfirmShutdown message.
        /// If we will handle the shutdown ourselves, inform the ShutdownService that it should abort
        /// the automatic Window.Close (set true to message.Execute.
        /// </summary>
        private void OnConfirmShutdownMessage(CleanShutdown.Messaging.NotificationMessageAction<bool> message)
        {
            if (message.Notification == Notifications.ConfirmShutdown)
            {
                // Cancel the shutdown.  We will do it here.
                message.Execute(OnClosing());       // true == abort shutdown.

                // NOTE: We may never reach this point if the user said to shut down.
            }
        }

        /// <summary>
        /// Implement window closing logic.
        /// <remarks>Note: This function will be called twice when the user clicks the Cancel button, and only once when the user
        /// clicks the 'X'.  Be careful to check for the "already cleaned up" case.</remarks>
        /// <<returns>true to cancel the automatic Window.Close action.</returns>
        /// </summary>
        private bool OnClosing()
        {
            // Clean-up logic here.

            // The Register/Login window is closing.  Warn the user and allow him to cancel the close.
            CLModalMessageBoxDialogs.Instance.DisplayModalShutdownPrompt(container: ViewGridContainer);

            return true;                // cancel the automatic Window close.
        }

        #endregion
    }
}