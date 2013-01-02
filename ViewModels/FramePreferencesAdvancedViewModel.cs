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
using CloudApiPublic.Static;
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
using System.IO;
using CloudApiPrivate.Common;
using System.Diagnostics;

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
        /// Initializes a new instance of the FramePreferencesAdvancedViewModel class.
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
                                                            _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: Move cloud folder: Entry.");
                                                            if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                                            {
                                                                // The user said yes.  Tell the view to put up the folder browser so the user can select the new location.
                                                                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: Move cloud folder: User said yes.");

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
        private GalaSoft.MvvmLight.Command.RelayCommand<string> _framePreferencesAdvancedViewModel_CreateCloudFolderCommand;
        public GalaSoft.MvvmLight.Command.RelayCommand<string> FramePreferencesAdvancedViewModel_CreateCloudFolderCommand
        {
            get
            {
                return _framePreferencesAdvancedViewModel_CreateCloudFolderCommand
                    ?? (_framePreferencesAdvancedViewModel_CreateCloudFolderCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(
                                            (path) =>
                                            {
                                                // The user selected the new location to house the Cloud folder.  Put up another
                                                // prompt to inform the user about what will happen.  We will create a folder named 'Cloud'
                                                // inside the selected folder and move the existing Cloud folder and all of the files inside
                                                // the existing Cloud folder into the new Cloud folder.
                                                var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                                dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                                                {
                                                    MoveCloudFolderWithUserInteraction(Settings.Instance.CloudFolderPath, path + "\\Cloud");
                                                });
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
            // The user selected the new location to house the Cloud folder.  Test this folder to make sure
            // it can be used.  It must be in this user's home directory, and it must not be in or at the
            // same location as the existing Cloud directory.
            if (!CLCreateCloudFolder.IsNewCloudFolderLocationValid(fromPath, toPath))
            {
                // This new cloud folder location is not valid.  Tell the user, and remain on the same dialog.
                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                    errorMessage: String.Format("The new location must be at or in your home directory ({0}), " +
                            "and you can't select the location containing the existing cloud folder location ({1}), " +
                            "or any location at or inside the existing cloud folder location ({2}).",
                            Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
                            Path.GetDirectoryName(fromPath), fromPath),
                    title: "Oh Snap!",
                    headerText: "Selected folder location not valid.",
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
            }
            else
            {
                // The selected location is valid.
                // Put up another prompt to inform the user about what will happen.  We will create a folder named 'Cloud'
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
                            _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: OK to move cloud folder?: Entry.");
                            if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                            {
                                // The user said yes.  Tell the view to put up the folder browser so the user can select the new location.
                                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: OK to move cloud folder: User said yes.");

                                ScheduleCloudFolderMove(fromPath, toPath);
                            }
                            else
                            {
                                // The user said no.  Do nothing.
                            }
                        }
                );
            }
        }

        /// <summary>
        /// The user has said to move the cloud folder, and the new folder location has been tested as valid.
        /// Set a new Settings flag to indicate that we are moving the cloud folder.  Also set a new Settings target
        /// cloud folder path.  Then spin off a VBScript passing the current and target cloud folder paths
        /// and immediately exit the application.  The Settings flag will prevent deletion of a virtually empty
        /// cloud folder during shutdown.  The VBScript will wait for Cloud to exit, and kill it
        /// if it takes too long.  Then the script will actually move the folder and restart Cloud.
        /// Cloud will see the Settings flag on startup. It will move the Settings target cloud folder
        /// path into the current path, and clear the Settings cloud folder move flag so that action is taken only once.
        /// </summary>
        /// <param name="fromPath"></param>
        /// <param name="toPath"></param>
        private void ScheduleCloudFolderMove(string fromPath, string toPath)
        {
            // Prepare to execute the script.  If any error occurs, tell the user and don't move the Cloud directory.
            // Write the self-destructing script to the user's temp directory and launch it.
            int errorNumber = 0;
            try
            {
                // Stream the CloudMoveCloudFolder.vbs file out to the user's temp directory
                // Locate the user's temp directory.
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Entry.");
                string userTempDirectory = System.IO.Path.GetTempPath();
                string vbsPath = userTempDirectory + "CloudMoveCloudFolder.vbs";

                // Get the assembly containing the .vbs resource.
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Get the assembly containing the .vbs resource.");
                System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(global::win_client.ViewModels.FramePreferencesAdvancedViewModel));
                if (storeAssembly == null)
                {
                    _trace.writeToLog(1, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: ERROR: storeAssembly null.");
                    errorNumber = 1;
                    throw new Exception("Error locating assembly");
                }

                // Stream the CloudMoveCloudFolder.vbs file out to the temp directory
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Call WriteResourceFileToFilesystemFile.");
                int rc = Helpers.WriteResourceFileToFilesystemFile(storeAssembly, "CloudMoveCloudFolder", vbsPath);
                if (rc != 0)
                {
                    _trace.writeToLog(1, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: ERROR: From WriteResourceFileToFilesystemFile. rc: {0}.", rc + 100);
                    errorNumber = 2;
                    throw new Exception("Error writing the script to the user temp directory");
                }

                // Now we will create a new process to run the VBScript file.
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Build the paths for launching the VBScript file.");
                string systemFolderPath = Helpers.Get32BitSystemFolderPath();
                string cscriptPath = systemFolderPath + "\\cscript.exe";
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Cscript executable path: <{0}>.", cscriptPath);

                string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @""" """ + fromPath + @""" """ + toPath + @"""";
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Launch the VBScript file.  Launch: <{0}>.", argumentsString);

                // Everything is prepared, and we are ready to launch the script process.  Change the settings in preparation for relaunch.
                lock (Settings.Instance.MovingCloudFolderTargetPath)
                {
                    Settings.Instance.IsMovingCloudFolder = true;
                    Settings.Instance.MovingCloudFolderTargetPath = toPath;
                }

                // Launch the process
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = cscriptPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = argumentsString;
                Process.Start(startInfo);

                // Exit the application.  If it hangs here, it will get killed!
                _trace.writeToLog(9, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: Exit the application.");
                CLAppDelegate.Instance.ExitApplication();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "FramePreferencesAdvancedViewModel: ScheduleCloudFolderMove: ERROR: Exception. Msg: {0}.", ex.Message);

                // Tell the user
                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                    errorMessage: String.Format(Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_BodyText, errorNumber),
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
            }


            //// Actually move the Cloud folder and all its files.
            //CLError error = null;
            //Settings.Instance.MoveCloudDirectoryFromPath_toDestination(fromPath, toPath, out error);
            //if (error == null)
            //{
            //    // Save the new cloud folder path.
            //    Settings.Instance.updateCloudFolderPath(toPath, Settings.Instance.CloudFolderCreationTimeUtc);

            //    // Update visible path
            //    FramePreferencesAdvanced_CloudFolder = toPath;
            //}
            //else
            //{
            //    // Display the error message.
            //    var dispatcher = CLAppDelegate.Instance.MainDispatcher;
            //    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            //    {
            //        CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
            //            errorMessage: String.Format(Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_BodyText, errorNumber),
            //            title: Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_Title,
            //            headerText: Resources.Resources.FramePreferencesAdvanced_ErrorMovingCloudFolder_HeaderText,
            //            rightButtonContent: Resources.Resources.generalOkButtonContent,
            //            rightButtonIsDefault: true,
            //            rightButtonIsCancel: true,
            //            container: ViewGridContainer,
            //            dialog: out _dialog,
            //            actionOkButtonHandler:
            //                returnedModalDialogViewModelInstance =>
            //                {
            //                    // Do nothing here when the user clicks the OK button.  Leave the user on this same FramePreferencesAdvanced dialog.
            //                });
            //    });
            //}
        }

        #endregion
    }
}
