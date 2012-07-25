﻿//
//  FramePreferencesAccountViewModel.cs
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
using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Resources;
using win_client.AppDelegate;
using win_client.ViewModelHelpers;
using System.Windows.Input;

namespace win_client.ViewModels
{
         
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class FramePreferencesAccountViewModel : ValidatingViewModelBase
    {
        #region Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;


        #endregion

        #region Private Class Definitions

        public class SupportedLanguage
        {
            public string Name { get; set; }
            public cloudAppLanguageType Type { get; set; }
        }

        #endregion  

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public FramePreferencesAccountViewModel(IDataService dataService)
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
            _rm =  CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;

            //TODO: Move the list of languages to a more appropriate place.
            List<SupportedLanguage> supportedLanguages = new List<SupportedLanguage>
            {
                new SupportedLanguage() {Name = "English", Type = cloudAppLanguageType.cloudAppLanguageEN},
                new SupportedLanguage() {Name = "Georgia", Type = cloudAppLanguageType.cloudAppLanguageGE},
                new SupportedLanguage() {Name = "China", Type = cloudAppLanguageType.cloudAppLanguageCN},
                new SupportedLanguage() {Name = "Spain", Type = cloudAppLanguageType.cloudAppLanguageES},
                new SupportedLanguage() {Name = "France", Type = cloudAppLanguageType.cloudAppLanguageFR},
                new SupportedLanguage() {Name = "Italy", Type = cloudAppLanguageType.cloudAppLanguageIT},
                new SupportedLanguage() {Name = "Japan", Type = cloudAppLanguageType.cloudAppLanguageJP},
                new SupportedLanguage() {Name = "Portugal", Type = cloudAppLanguageType.cloudAppLanguagePT},
            };
            _cbSelectYourLanguage_ItemsSource = supportedLanguages;
        }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// The <see cref="CbSelectYourLanguage_ItemsSource" /> property's name.
        /// Source for the supported languages.
        /// </summary>
        public const string CbSelectYourLanguage_ItemsSourcePropertyName = "CbSelectYourLanguage_ItemsSource";
        private List<SupportedLanguage> _cbSelectYourLanguage_ItemsSource = null;
        /// <summary>
        /// Sets and gets the CbSelectYourLanguage_ItemsSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public List<SupportedLanguage> CbSelectYourLanguage_ItemsSource
        {
            get
            {
                return _cbSelectYourLanguage_ItemsSource;
            }

            set
            {
                if (_cbSelectYourLanguage_ItemsSource == value)
                {
                    return;
                }

                _cbSelectYourLanguage_ItemsSource = value;
                RaisePropertyChanged(CbSelectYourLanguage_ItemsSourcePropertyName);
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
         
        #endregion
      
        #region Commands
        private ICommand _framePreferencesAccount_CheckForUpdatesCommand;

        /// <summary>
        /// Gets the FramePreferencesAccount_CheckForUpdatesCommand.
        /// </summary>
        public ICommand FramePreferencesAccount_CheckForUpdatesCommand
        {
            get
            {
                return _framePreferencesAccount_CheckForUpdatesCommand
                    ?? (_framePreferencesAccount_CheckForUpdatesCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Record the time of the last update check
                                              Settings.Instance.DateWeLastCheckedForSoftwareUpdate = DateTime.Now;

                                              //TODO: Actually check to see if there are any updates.
                                              CLModalErrorDialog.Instance.DisplayModalErrorMessage("You are currently running the latest version of the Cloud application.", "Information",
                                                                                "Update check complete.", _rm.GetString("generalOkButtonContent"),
                                                                                ViewGridContainer, returnedViewModelInstance =>
                                                                                {
                                                                                    // Do nothing here when the user clicks the OK button.
                                                                                });
                                          }));
            }
        }
        #endregion

    }
}