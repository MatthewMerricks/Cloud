//
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

        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;

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

            _rm = CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;
            _preferences = new CLPreferences();
            _preferences.GetPreferencesFromSettings();
        }

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
                                              // Save the preferences set by the user.
                                              _preferences.SetPreferencesToSettings();

                                              // Navigate to PageInvisible
                                              Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                              CLAppMessages.PagePreferences_NavigationRequest.Send(nextPage);
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
                                              // Reset the preferences from the last saved state
                                              _preferences.GetPreferencesFromSettings();

                                              // Navigate to PageInvisible
                                              Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                              CLAppMessages.PagePreferences_NavigationRequest.Send(nextPage);
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
                                              Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesGeneral, System.UriKind.Relative);
                                              KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);

                                              CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                              Title = _rm.GetString("PagePreferencesGeneralTitle");
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
                                              Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesAccount, System.UriKind.Relative);
                                              KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                              CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                              Title = _rm.GetString("PagePreferencesAccountTitle");
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
                                              Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesNetwork, System.UriKind.Relative);
                                              KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                              CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                              Title = _rm.GetString("PagePreferencesNetworkTitle");
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
                                              Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesAdvanced, System.UriKind.Relative);
                                              KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                              CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                              Title = _rm.GetString("PagePreferencesAdvancedTitle");
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
                                              Uri nextPageUri = new System.Uri(CLConstants.kFramePreferencesAbout, System.UriKind.Relative);
                                              KeyValuePair<Uri, CLPreferences> nextPage = new KeyValuePair<Uri, CLPreferences>(nextPageUri, Preferences);
                                              CLAppMessages.PagePreferences_FrameNavigationRequest_WithPreferences.Send(nextPage);
                                              Title = _rm.GetString("PagePreferencesAboutTitle");
                                          }));
            }
        }

        #region Private Support Functions

        #endregion
    }
}