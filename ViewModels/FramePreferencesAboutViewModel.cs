//
//  FramePreferencesAboutViewModel.cs
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
using win_client.Views;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using CleanShutdown.Messaging;

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
    public class FramePreferencesAboutViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;
        private IModalWindow _dialog = null;        // for use with modal dialogs

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public FramePreferencesAboutViewModel(IDataService dataService)
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

            // Register to receive the ConfirmShutdown message
            Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                this,
                message =>
                {
                    OnConfirmShutdownMessage(message);
                });
        }

        #endregion

        #region Bindable Properties

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
        /// The <see cref="Preferences" /> property's name.
        /// </summary>
        public const string PreferencesPropertyName = "Preferences";
        private CLPreferences _preferences = null;
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


        #endregion
      
        #region Relay Commands

        /// <summary>
        /// Gets the FramePreferencesAbout_FollowUsOnTwitter.
        /// </summary>
        private ICommand _framePreferencesAbout_FollowUsOnTwitter;
        public ICommand FramePreferencesAbout_FollowUsOnTwitter
        {
            get
            {
                return _framePreferencesAbout_FollowUsOnTwitter
                    ?? (_framePreferencesAbout_FollowUsOnTwitter = new RelayCommand(
                                            () =>
                                            {
                                                // Launch the default browser and send them our twitter page.
                                                Process proc = new Process();
                                                proc.StartInfo.UseShellExecute = true;
                                                proc.StartInfo.FileName = CLDefinitions.CLTwitterPageUrl;
                                                proc.Start(); 
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
            return true;                // cancel the automatic Window close.
        }

        #endregion
    }
}
