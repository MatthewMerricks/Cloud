﻿//
//  PagePreferencesViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using win_client.Model;
using System;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Controls;
using win_client.ViewModels;
using win_client.Common;
using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPublic.Model;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Collections.Generic;
using Dialog.Abstractions.Wpf.Intefaces;
using CloudApiPublic.Support;
using System.Resources;
using win_client.AppDelegate;
using win_client.ViewModelHelpers;
using win_client.Resources;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Messaging;
using CleanShutdown.Helpers;
using CloudApiPrivate.Common;

namespace win_client.ViewModels
{  

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class PagePreferencesViewModel : ValidatingViewModelBase
    {
        private readonly IDataService _dataService;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private CLTrace _trace = CLTrace.Instance;
        private CLPreferences _originalPreferences;  // to test for changes

        #region Life Cycle

        /// <summary>
        /// Initializes a new instance of the PagePreferencesViewModel class.
        /// </summary>
        public PagePreferencesViewModel(IDataService dataService)
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

        #endregion

        #region Bindable Properties

        /// <summary>
        /// The <see cref="Preferences" /> property's name.
        /// </summary>
        public const string PreferencesPropertyName = "Preferences";
        private CLPreferences _preferences = null;

        /// <summary>
        /// Sets and gets the ViewGridContainer property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public CLPreferences Preferences
        {
            get
            {
                return _preferences;
            }

            set
            {
                if (_preferences == value)
                {
                    return;
                }

                _preferences = value;
                RaisePropertyChanged(PreferencesPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ViewGridContainer" /> property's name.
        /// </summary>
        public const string ViewGridContainerPropertyName = "ViewGridContainer";
        private Grid _viewGridContainer = null;

        /// <summary>
        /// Sets and gets the ViewGridContainer property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
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
        /// The <see cref="Title" /> property's name.
        /// </summary>
        public const string TitlePropertyName = "Title";
        private string _title = "";
        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                if (_title == value)
                {
                    return;
                }

                _title = value;
                RaisePropertyChanged(TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="PagePreferences_BackgroundImageSource" /> property's name.
        /// </summary>
        public const string PagePreferences_BackgroundImageSourcePropertyName = "PagePreferences_BackgroundImageSource";
        private string _pagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundGeneral;
        public string PagePreferences_BackgroundImageSource
        {
            get
            {
                return _pagePreferences_BackgroundImageSource;
            }

            set
            {
                if (_pagePreferences_BackgroundImageSource == value)
                {
                    return;
                }

                _pagePreferences_BackgroundImageSource = value;
                RaisePropertyChanged(PagePreferences_BackgroundImageSourcePropertyName);
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
        /// Create new account from the PagePreferences page.
        /// </summary>
        private RelayCommand _pagePreferences_OkCommand;
        public RelayCommand PagePreferences_OkCommand
        {
            get
            {
                return _pagePreferences_OkCommand 
                    ?? (_pagePreferences_OkCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Save the changes
                                              CommitChangesAndMinimizeToSystemTray();
                                          }));
            }
        }

        /// <summary>
        /// Sign in to an existing account from the PagePreferences page.
        /// </summary>
        private RelayCommand _pagePreferences_CancelCommand;
        public RelayCommand PagePreferences_CancelCommand
        {
            get
            {
                return _pagePreferences_CancelCommand
                    ?? (_pagePreferences_CancelCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Reset the preferences from the last saved state and go back to the system tray.
                                              ProcessCancelRequest();
                                          }));
            }
        }

        /// <summary>
        /// Show the general preferences.
        /// </summary>
        private RelayCommand _pagePreferences_GeneralCommand;
        public RelayCommand PagePreferences_GeneralCommand
        {
            get
            {
                return _pagePreferences_GeneralCommand
                    ?? (_pagePreferences_GeneralCommand = new RelayCommand(
                                          () =>
                                          {
                                                // Set the general background
                                                PagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundGeneral;

                                                // Navigate to the next page
                                                Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesGeneral, System.UriKind.Relative);
                                                KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);

                                                CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                                Title = Resources.Resources.PagePreferencesGeneralTitle;
                                          }));
            }
        }

        private RelayCommand _pagePreferences_ShortcutsCommand;
        public RelayCommand PagePreferences_ShortcutsCommand
        {
            get
            {
                return _pagePreferences_ShortcutsCommand
                    ?? (_pagePreferences_ShortcutsCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Set the general background
                                              PagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundGeneral;

                                              // Navigate to the next page
                                              Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesShortcuts, System.UriKind.Relative);
                                              KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);

                                              CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                              Title = Resources.Resources.PagePreferencesShortcutsTitle;
                                          }));
            }
        }

        /// <summary>
        /// Show the account preferences.
        /// </summary>
        private RelayCommand _pagePreferences_AccountCommand;
        public RelayCommand PagePreferences_AccountCommand
        {
            get
            {
                return _pagePreferences_AccountCommand
                    ?? (_pagePreferences_AccountCommand = new RelayCommand(
                                          () =>
                                          {
                                                // Set the general background
                                                PagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundGeneral;

                                                // Navigate to the next page
                                                Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesAccount, System.UriKind.Relative);
                                                KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                                CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                                Title = Resources.Resources.PagePreferencesAccountTitle;
                                          }));
            }
        }

        /// <summary>
        /// Show the network preferences.
        /// </summary>
        private RelayCommand _pagePreferences_NetworkCommand;
        public RelayCommand PagePreferences_NetworkCommand
        {
            get
            {
                return _pagePreferences_NetworkCommand
                    ?? (_pagePreferences_NetworkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Set the general background
                                                PagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundGeneral;

                                                // Navigate to the next page
                                                Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesNetwork, System.UriKind.Relative);
                                                KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                                CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                                Title = Resources.Resources.PagePreferencesNetworkTitle;
                                            }));
            }
        }

        /// <summary>
        /// Show the advanced preferences.
        /// </summary>
        private RelayCommand _pagePreferences_AdvancedCommand;
        public RelayCommand PagePreferences_AdvancedCommand
        {
            get
            {
                return _pagePreferences_AdvancedCommand
                    ?? (_pagePreferences_AdvancedCommand = new RelayCommand(
                                          () =>
                                          {
                                                // Set the general background
                                                PagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundGeneral;

                                                // Navigate to the next page
                                                Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesAdvanced, System.UriKind.Relative);
                                                KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                                CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                                Title = Resources.Resources.PagePreferencesAdvancedTitle;
                                          }));
            }
        }

        /// <summary>
        /// Show the about preferences.
        /// </summary>
        private RelayCommand _pagePreferences_AboutCommand;
        public RelayCommand PagePreferences_AboutCommand
        {
            get
            {
                return _pagePreferences_AboutCommand
                    ?? (_pagePreferences_AboutCommand = new RelayCommand(
                                          () =>
                                          {
                                                // Set the "about" background
                                              PagePreferences_BackgroundImageSource = CLConstants.kPagePreferencesBackgroundAbout;

                                                // Navigate to the next page
                                                Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesAbout, System.UriKind.Relative);
                                                KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                                CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                                Title = Resources.Resources.PagePreferencesAboutTitle;
                                          }));
            }
        }


        /// <summary>
        /// Gets the OnNavigated.
        /// </summary>
        private ICommand _onNavigated;
        public ICommand OnNavigated
        {
            get
            {
                return _onNavigated
                    ?? (_onNavigated = new RelayCommand(
                                          () =>
                                          {
                                              // Get the current preferences from Settings.
                                              _preferences = new CLPreferences();
                                              _preferences.GetPreferencesFromSettings();
                                              _originalPreferences = _preferences.DeepCopy<CLPreferences>();          // make a copy to compare later
                                          }));
            }
        }

        /// <summary>
        /// The user pressed the ESC key.
        /// </summary>
        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand
                    ?? (_cancelCommand = new RelayCommand(
                                          () =>
                                          {
                                              // The user pressed the Esc key.
                                              ProcessCancelRequest();
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
                                              // The user clicked the 'X' on the NavigationWindow, or pressed Alt-F4.  They are trying to close the
                                              // window.  Normally, this would be handled as a click on the Cancel button, but there may be one or
                                              // more modal dialogs active.  For instance, This Page may have FramePreferencesNetwork active, and
                                              // that page may have the modal dialog DialogPreferencesNetworkProxies active, and that dialog may
                                              // have another (stacked) modal dialog DialogCloudMessageBox active.  If any modal dialog is active,
                                              // every involved page, frame or dialog should just ignore the request to close the outer window.
                                              // We will send a message query to see if any modal dialog is active.
                                              bool modalDialogIsActive = false;
                                              Messenger.Default.Send(new CleanShutdown.Messaging.NotificationMessageAction<bool>(
                                                            Notifications.QueryModalDialogsActive,
                                                                modalDialogActive => modalDialogIsActive |= modalDialogActive));
                                              if (modalDialogIsActive)
                                              {
                                                  WindowCloseOk = false;        // prevent the window close
                                                  return;
                                              }

                                              // Otherwise, handle the request as a cancel request.
                                              ProcessCancelRequest();
                                              WindowCloseOk = false;            // prevent the window close
                                          }));
            }
        }

        #endregion

        #region Support Functions

        /// <summary>
        /// Commit the changes to Settings and minimize to system tray.
        /// </summary>
        private void CommitChangesAndMinimizeToSystemTray()
        {
            // Save the preferences set by the user.
            if (!CLDeepCompare.IsEqual(_preferences, _originalPreferences))
            {
                _preferences.SetPreferencesToSettings();
            }

            // Navigate to PageInvisible
            Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
            CLAppMessages.PagePreferences_NavigationRequest.Send(nextPage);
        }

        /// <summary>
        /// The user is requesting to cancel.  He clicked the 'X', or he clicked the Cancel button.
        /// </summary>
        private void ProcessCancelRequest()
        {
            // Check for unsaved changes.
            if (!CLDeepCompare.IsEqual(_preferences, _originalPreferences))
            {
                // The user is cancelling and there are unsaved changes.  Ask if he wants to save them.
                CLModalMessageBoxDialogs.Instance.DisplayModalSaveChangesPrompt(container: ViewGridContainer, dialog: out _dialog, actionResultHandler: returnedViewModelInstance =>
                {
                    _trace.writeToLog(9, "PagePreferencesViewModel: Prompt save changes: Entry.");
                    if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                    {
                        // The user said yes.
                        _trace.writeToLog(9, "PagePreferencesViewModel: Prompt save changes: User said yes.");
                        _preferences.SetPreferencesToSettings();   // save the changes to Settings.
                    }

                    // In either case, minimize to the system tray.
                    Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                    CLAppMessages.PagePreferences_NavigationRequest.Send(nextPage);
                });
            }
            else
            {
                // There are no changes.  Minimize to the system tray.
                Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                CLAppMessages.PagePreferences_NavigationRequest.Send(nextPage);
            }
        }

        #endregion
    }
}