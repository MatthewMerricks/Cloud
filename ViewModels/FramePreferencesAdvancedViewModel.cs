//
//  FramePreferencesAdvancedViewModel.cs
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
using win_client.Resources;
using System.Windows.Threading;
using System.ComponentModel;
using CleanShutdown.Messaging;
using CleanShutdown.Helpers;

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
    public class FramePreferencesAdvancedViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public FramePreferencesAdvancedViewModel(IDataService dataService)
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

            // Set the current Cloud folder location.
            FramePreferencesAdvanced_CloudFolder = Settings.Instance.CloudFolderPath;
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

        /// <summary>
        /// The <see cref="FramePreferencesAdvanced_CloudFolder" /> property's name.
        /// </summary>
        public const string FramePreferencesAdvanced_CloudFolderPropertyName = "FramePreferencesAdvanced_CloudFolder";
        private string _framePreferencesAdvanced_CloudFolder = String.Empty;
        public string FramePreferencesAdvanced_CloudFolder
        {
            get
            {
                return _framePreferencesAdvanced_CloudFolder;
            }

            set
            {
                if (_framePreferencesAdvanced_CloudFolder == value)
                {
                    return;
                }

                // Enable or disable the Reset button depending on the Cloud folder path being set (whether it is the default path or not).
                FramePreferencesAdvanced_ResetButtonEnabled = !value.Equals(Settings.Instance.GetDefaultCloudFolderPath(), StringComparison.InvariantCulture);

                _framePreferencesAdvanced_CloudFolder = value;
                RaisePropertyChanged(FramePreferencesAdvanced_CloudFolderPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="FramePreferencesAdvanced_ResetButtonEnabled" /> property's name.
        /// </summary>
        public const string FramePreferencesAdvanced_ResetButtonEnabledPropertyName = "FramePreferencesAdvanced_ResetButtonEnabled";
        private bool _framePreferencesAdvanced_ResetButtonEnabled = false;
        public bool FramePreferencesAdvanced_ResetButtonEnabled
        {
            get
            {
                return _framePreferencesAdvanced_ResetButtonEnabled;
            }

            set
            {
                if (_framePreferencesAdvanced_ResetButtonEnabled == value)
                {
                    return;
                }

                _framePreferencesAdvanced_ResetButtonEnabled = value;
                RaisePropertyChanged(FramePreferencesAdvanced_ResetButtonEnabledPropertyName);
            }
        }

        #endregion
      
        #region Relay Commands

        /// <summary>
        /// Gets the FramePreferencesAdvanced_ChangeCloudFolder.
        /// </summary>
        private ICommand _framePreferencesAdvanced_ChangeCloudFolder;
        public ICommand FramePreferencesAdvanced_ChangeCloudFolder
        {
            get
            {
                return _framePreferencesAdvanced_ChangeCloudFolder
                    ?? (_framePreferencesAdvanced_ChangeCloudFolder = new RelayCommand(
                                            () =>
                                            {
                                                CLModalMessageBoxDialogs.Instance.DisplayModalMessageBox(
                                                    windowHeight: 250,
                                                    leftButtonWidth: 75,
                                                    rightButtonWidth: 75,
                                                    title: Resources.Resources.FramePreferencesAdvanced_ChangeCloudFolderTitle,
                                                    headerText: Resources.Resources.FramePreferencesAdvanced_ChangeCloudFolderHeaderText,
                                                    bodyText: Resources.Resources.FramePreferencesAdvanced_ChangeCloudFolderBodyText,
                                                    leftButtonContent: Resources.Resources.GeneralYesButtonContent,
                                                    leftButtonIsDefault: false,
                                                    leftButtonIsCancel: false,
                                                    rightButtonContent: Resources.Resources.GeneralNoButtonContent,
                                                    rightButtonIsDefault: true,
                                                    rightButtonIsCancel: false,
                                                    container: ViewGridContainer,
                                                    dialog: out _dialog,
                                                    actionResultHandler:
                                                        returnedViewModelInstance =>
                                                        {
                                                            // Do nothing here when the user clicks the OK button.
                                                            _trace.writeToLog(9, "FramePreferencesAdvanced: Move cloud folder: Entry.");
                                                            if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                                            {
                                                                // The user said yes.  Tell the view to put up the folder browser so the user can select the new location.
                                                                _trace.writeToLog(9, "FramePreferencesAdvanced: Move cloud folder: User said yes.");

                                                                // Display the Windows Forms folder selection
                                                                // dialog.  Tell the view to put up the dialog.  If the user clicks cancel, 
                                                                // the view will return and we will stay on this window.  If the user clicks
                                                                // OK, the view will send the FramePreferencesAdvancedViewModel_CreateCloudFolderCommand
                                                                // back to us, and we will create the cloud folder in that command method.
                                                                // TODO: This is strange for WPF, since the FolderBrowser is a Windows Form thing.
                                                                // The processing is synchronous from the VM to the View, show the dialog, wait
                                                                // for the dialog, then return on cancel, or issue a RelayCommand back to us,
                                                                // process the RelayCommand, then back to the View, then back to here.
                                                                // Should we be more asynchronous?
                                                                var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                                                dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                                                                {
                                                                    CLAppMessages.Message_FramePreferencesAdvanced_ShouldChooseCloudFolder.Send("");
                                                                });
                                                            }
                                                            else
                                                            {
                                                                // The user said no.  Do nothing.
                                                            }
                                                        }
                                                );
                                            }));
            }
        }

        /// <summary>
        /// Gets the FramePreferencesAdvanced_ResetCloudFolder.
        /// </summary>
        private ICommand _framePreferencesAdvanced_ResetCloudFolder;
        public ICommand FramePreferencesAdvanced_ResetCloudFolder
        {
            get
            {
                return _framePreferencesAdvanced_ResetCloudFolder
                    ?? (_framePreferencesAdvanced_ResetCloudFolder = new RelayCommand(
                                            () =>
                                            {
                                                // If the Cloud folder is already at the default location, this shouldn't have happened.
                                                // Just exit, but disable the user's Reset button.
                                                if (Settings.Instance.CloudFolderPath.Equals(Settings.Instance.GetDefaultCloudFolderPath(), StringComparison.InvariantCulture))
                                                {
                                                    FramePreferencesAdvanced_ResetButtonEnabled = false;
                                                }
                                                else
                                                {
                                                    // Tell the user we will be moving his Cloud folder back to the default location
                                                    var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                                    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                                                    {
                                                        MoveCloudFolderWithUserInteraction(Settings.Instance.CloudFolderPath, Settings.Instance.GetDefaultCloudFolderPath());
                                                    });
                                                }
                                            }));                                                

            }
        }

        /// <summary>
        /// Gets the FramePreferencesAdvanced_ChangeSelectiveSyncSettings.
        /// </summary>
        private ICommand _framePreferencesAdvanced_ChangeSelectiveSyncSettings;
        public ICommand FramePreferencesAdvanced_ChangeSelectiveSyncSettings
        {
            get
            {
                return _framePreferencesAdvanced_ChangeSelectiveSyncSettings
                    ?? (_framePreferencesAdvanced_ChangeSelectiveSyncSettings = new RelayCommand(
                                            () =>
                                            {
                                                //TODO: Actually handle a request to select folders to sync.
                                                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                                    errorMessage: "You can't select folders right now.  It's not implemented yet..",
                                                    title: "Information",
                                                    headerText: "Not implemented!",
                                                    rightButtonContent: Resources.Resources.generalOkButtonContent,
                                                    rightButtonIsDefault: true,
                                                    rightButtonIsCancel: true,
                                                    container: ViewGridContainer,
                                                    dialog: out _dialog,
                                                    actionOkButtonHandler:
                                                      returnedViewModelInstance =>
                                                      {
                                                          // Do nothing here when the user clicks the OK button.
                                                      }
                                                );
                                            }));
            }
        }

        /// <summary>
        /// The user has selected a new cloud folder path.  Create the new cloud folder
        /// </summary>
        private RelayCommand<string> _framePreferencesAdvancedViewModel_CreateCloudFolderCommand;
        public RelayCommand<string> FramePreferencesAdvancedViewModel_CreateCloudFolderCommand
        {
            get
            {
                return _framePreferencesAdvancedViewModel_CreateCloudFolderCommand
                    ?? (_framePreferencesAdvancedViewModel_CreateCloudFolderCommand = new RelayCommand<string>(
                                            (path) =>
                                            {
                                                // The user selected the new location to house the Cloud folder.  Put up another
                                                // prompt to inform the user about what will happen.  We will create a folder named 'Cloud'
                                                // inside the selected folder and move the existing Cloud folder and all of the files inside
                                                // the existing Cloud folder into the new Cloud folder.
                                                MoveCloudFolderWithUserInteraction(Settings.Instance.CloudFolderPath, path + "\\Cloud");
                                            }));
            }
        }

        #endregion

        #region Support Functions

        /// <summary>
        /// Move the cloud folder location with user interaction.
        /// </summary>
        private void MoveCloudFolderWithUserInteraction(string fromPath, string toPath)
        {
            // The user selected the new location to house the Cloud folder.  Put up another
            // prompt to inform the user about what will happen.  We will create a folder named 'Cloud'
            // inside the selected folder and move the existing Cloud folder and all of the files inside
            // the existing Cloud folder into the new Cloud folder.
            CLModalMessageBoxDialogs.Instance.DisplayModalMessageBox(
                windowHeight: 250,
                leftButtonWidth: 75,
                rightButtonWidth: 75,
                title: Resources.Resources.FramePreferencesAdvanced_NewCloudFolderSelectedAlert_Title,
                headerText: Resources.Resources.FramePreferencesAdvanced_NewCloudFolderSelectedAlert_HeaderText,

                // The body text is formatted like this:
                // "This will move your existing Cloud folder and all of the files inside it from the existing location:{0}{1}{2}{3}into the new folder:{4}{5}{6} "
                bodyText: String.Format(Resources.Resources.FramePreferencesAdvanced_NewCloudFolderSelectedAlert_BodyText, 
                            Environment.NewLine, 
                            "\t",
                            fromPath,
                            Environment.NewLine,
                            Environment.NewLine,
                            "\t",
                            toPath),
                leftButtonContent: Resources.Resources.GeneralYesButtonContent,
                leftButtonIsDefault: false,
                leftButtonIsCancel: false,
                rightButtonContent: Resources.Resources.GeneralNoButtonContent,
                rightButtonIsDefault: true,
                rightButtonIsCancel: false,
                container: ViewGridContainer,
                dialog: out _dialog,
                actionResultHandler:
                    returnedViewModelInstance =>
                    {
                        // Do nothing here when the user clicks the OK button.
                        _trace.writeToLog(9, "FramePreferencesAdvanced: OK to move cloud folder?: Entry.");
                        if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                        {
                            // The user said yes.  Tell the view to put up the folder browser so the user can select the new location.
                            _trace.writeToLog(9, "FramePreferencesAdvanced: OK to move cloud folder: User said yes.");

                            // Actually move the Cloud folder and all its files.
                            CLError error = null;
                            Settings.Instance.MoveCloudDirectoryFromPath_toDestination(fromPath, toPath, out error);
                            if (error == null)
                            {
                                // Save the new cloud folder path.
                                Settings.Instance.updateCloudFolderPath(toPath, Settings.Instance.CloudFolderCreationTimeUtc);

                                // Update visible path
                                FramePreferencesAdvanced_CloudFolder = toPath;
                            }
                            else
                            {
                                // Display the error message.
                                var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                                {
                                    CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                        errorMessage: Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_BodyText,
                                        title: Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_Title,
                                        headerText: Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_HeaderText,
                                        rightButtonContent: Resources.Resources.generalOkButtonContent,
                                        rightButtonIsDefault: true,
                                        rightButtonIsCancel: true,
                                        container: ViewGridContainer,
                                        dialog: out _dialog,
                                        actionOkButtonHandler:
                                            returnedModalDialogViewModelInstance =>
                                            {
                                                // Do nothing here when the user clicks the OK button.  Leave the user on this same FramePreferencesAdvanced dialog.
                                            });
                                });
                            }
                        }
                        else
                        {
                            // The user said no.  Do nothing.
                        }
                    }
            );
        }

        #endregion
    }
}
