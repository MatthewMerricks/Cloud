//
//  PageInvisibleViewModel.cs
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
using CloudApiPrivate.Static;
using System.IO;
using System.Resources;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Collections.Generic;
using win_client.Views;
using win_client.AppDelegate;
using CloudApiPublic.Support;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Helpers;
using win_client.ViewModelHelpers;
using System.Windows.Threading;
using System.Diagnostics;
using CloudApiPrivate.Common;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageInvisibleViewModel : ViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private CLTrace _trace = CLTrace.Instance;
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageInvisibleViewModel class.
        /// </summary>
        public PageInvisibleViewModel(IDataService dataService)
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
        }

        /// <summary>
        /// Clean up all resources allocated, and save state as needed.
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
        }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// The <see cref="TaskbarIconVisibility" /> property's name.
        /// </summary>
        public const string TaskbarIconVisibilityPropertyName = "TaskbarIconVisibility";
        private Visibility _taskbarIconVisibility = Visibility.Hidden;

        /// <summary>
        /// Sets and gets the TaskbarIconVisibility property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Visibility TaskbarIconVisibility
        {
            get
            {
                return _taskbarIconVisibility;
            }

            set
            {
                if (_taskbarIconVisibility == value)
                {
                    return;
                }

                _taskbarIconVisibility = value;
                RaisePropertyChanged(TaskbarIconVisibilityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TaskbarIconMenuActivation" /> property's name.
        /// </summary>
        public const string TaskbarIconMenuActivationPropertyName = "TaskbarIconMenuActivation";
        private Visibility _taskbarIconMenuActivation = Visibility.Visible;

        /// <summary>
        /// Sets and gets the TaskbarIconVisibility property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Visibility TaskbarIconMenuActivation
        {
            get
            {
                return _taskbarIconMenuActivation;
            }

            set
            {
                if (_taskbarIconMenuActivation == value)
                {
                    return;
                }

                _taskbarIconMenuActivation = value;
                RaisePropertyChanged(TaskbarIconMenuActivationPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TaskbarIconPopupActivation" /> property's name.
        /// </summary>
        public const string TaskbarIconPopupActivationPropertyName = "TaskbarIconPopupActivation";
        private Visibility _taskbarIconPopupActivation = Visibility.Visible;

        /// <summary>
        /// Sets and gets the TaskbarIconVisibility property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Visibility TaskbarIconMenuPopupActivation
        {
            get
            {
                return _taskbarIconPopupActivation;
            }

            set
            {
                if (_taskbarIconPopupActivation == value)
                {
                    return;
                }

                _taskbarIconPopupActivation = value;
                RaisePropertyChanged(TaskbarIconPopupActivationPropertyName);
            }
        }

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

        /// <summary>
        /// The <see cref="WindowCloseOk" /> property's name.
        /// </summary>
        public const string WindowCloseOkPropertyName = "WindowCloseOk";
        private bool _windowCloseOk = false;
        public bool WindowCloseOk
        {
            get
            {
                return _windowCloseOk;
            }

            set
            {
                if (_windowCloseOk == value)
                {
                    return;
                }

                _windowCloseOk = value;
                RaisePropertyChanged(WindowCloseOkPropertyName);
            }
        }

        #endregion

        #region Relay Commands


        /// <summary>
        /// The user clicked the system tray NotifyIcon context menu "Show Cloud Folder" item.
        /// </summary>
        private ICommand _showCloudFolderCommand;
        public ICommand ShowCloudFolderCommand
        {
            get
            {
                return _showCloudFolderCommand
                    ?? (_showCloudFolderCommand = new RelayCommand(
                                          () =>
                                          {
                                            // Launch the process
                                            CLShortcuts.LaunchExplorerToFolder(Settings.Instance.CloudFolderPath);
                                          }));
            }
        }

        /// <summary>
        /// The user clicked the system tray NotifyIcon context menu "Browse to Cloud.com..." item.
        /// </summary>
        private ICommand _browseToCloudComCommand;
        public ICommand BrowseToCloudComCommand
        {
            get
            {
                return _browseToCloudComCommand
                    ?? (_browseToCloudComCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Browse to www.cloud.com
                                              CLShortcuts.StartBrowserToUrl(CLConstants.kUrlCloudCom);
                                          }));
            }
        }

        /// <summary>
        /// The user clicked system tray NotifyIcon context menu preferences item.
        /// </summary>
        private RelayCommand _showPreferencesPageCommand;
        public RelayCommand ShowPreferencesPageCommand
        {
            get
            {
                return _showPreferencesPageCommand
                    ?? (_showPreferencesPageCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Show the preferences page
                                                Uri nextPage = new System.Uri(CLConstants.kPagePreferences, System.UriKind.Relative);
                                                CLAppMessages.PageInvisible_TriggerOutOfSystemTrayAnimation.Send(nextPage);
                                            }));
            }
        }

        /// <summary>
        /// The user clicked has selected a choice and will continue.
        /// </summary>
        private RelayCommand _checkForUpdatesCommand;
        public RelayCommand CheckForUpdatesCommand
        {
            get
            {
                return _checkForUpdatesCommand
                    ?? (_checkForUpdatesCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Check for updates
                                                Window win = CLAppDelegate.Instance.CheckForUpdatesWindow;
                                                win.MaxWidth = 640;
                                                win.MaxHeight = 480;
                                                win.MinWidth = 640;
                                                win.MinHeight = 480;
                                                win.ShowInTaskbar = true;
                                                win.ShowActivated = true;
                                                win.Visibility = Visibility.Visible;
                                                win.WindowStyle = WindowStyle.ThreeDBorderWindow;
                                                win.SetPlacement(Settings.Instance.MainWindowPlacement);
                                                _trace.writeToLog(9, "PageInvisibleViewModel: CheckFOrUpdatesCommand: Window coords: {0}, {1}, {2}, {3} (LTWH).", win.Left, win.Top, win.Width, win.Height);
                                                win.Show();
                                                win.Topmost = true;
                                                win.Topmost = false;
                                                win.Focus();
                                                CLAppMessages.Message_DialogCheckForUpdates_ShouldCheckForUpdates.Send("");
                                            }));                                              
            }
        }


        /// <summary>
        /// Gets the ExitApplicationCommand.
        /// </summary>
        private RelayCommand _exitApplicationCommand;
        public RelayCommand ExitApplicationCommand
        {
            get
            {
                return _exitApplicationCommand
                    ?? (_exitApplicationCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Exit the application
                                              CLAppDelegate.Instance.StopCloudAppServicesAndUI();
                                              CLAppDelegate.Instance.ExitApplication();
                                          }));
            }
        }

        /// <summary>
        /// The window wants to close.  The user clicked the 'X'.
        /// This will set the bindable property WindowCloseOk if we will not handle this event.
        /// </summary>
        private ICommand _windowCloseRequested;
        public ICommand WindowCloseRequested
        {
            get
            {
                return _windowCloseRequested
                    ?? (_windowCloseRequested = new RelayCommand(
                                          () =>
                                          {
                                              // Handle the request and set the property.
                                              WindowCloseOk = OnClosing();
                                          }));
            }
        }

        #endregion

        #region Support Functions

        /// <summary>
        /// Implement window closing logic.
        /// <remarks>Note: This function will be called twice when the user clicks the Cancel button, and only once when the user
        /// clicks the 'X'.  Be careful to check for the "already cleaned up" case.</remarks>
        /// <<returns>true to allow the automatic Window.Close action.</returns>
        /// </summary>
        private bool OnClosing()
        {
            // Clean-up logic here.

            // Just allow the shutdown if we have already decided to do it.
            if (_isShuttingDown)
            {
                return true;
            }

            // The Register/Login window is closing.  Warn the user and allow him to cancel the close.
            CLModalMessageBoxDialogs.Instance.DisplayModalShutdownPrompt(container: ViewGridContainer, dialog: out _dialog, actionResultHandler: returnedViewModelInstance =>
            {
                _trace.writeToLog(9, "PageInvisibleViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                {
                    // The user said yes.
                    _trace.writeToLog(9, "PageInvisibleViewModel: Prompt exit application: User said yes.");

                    // Shut down tha application
                    _isShuttingDown = true;         // allow the shutdown if asked

                    // It is tempting to call ShutdownService.RequestShutdown() here, but this dialog
                    // is still active and would prevent the shutdown.  Allow the dialog to fully close
                    // and then request the shutdown.
                    Dispatcher dispatcher = CLAppDelegate.Instance.MainDispatcher;
                    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                    {
                        ShutdownService.RequestShutdown();
                    });
                }
            });

            return false;                // cancel the automatic Window close.
        }

        #endregion
    }
}