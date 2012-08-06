//
//  PageCloudFolderMissingViewModel.cs
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
using CloudApiPublic.Model;
using win_client.ViewModelHelpers;
using Ookii.Dialogs.WpfMinusTaskDialog;
using CloudApiPrivate.Model;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Helpers;
using System.Windows.Threading;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageCloudFolderMissingViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private ResourceManager _rm;
        private CLTrace _trace;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageCloudFolderMissingViewModel(IDataService dataService)
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
            _trace = CLAppDelegate.Instance.GetTrace();

            BodyMessage = _rm.GetString("pageCloudFolderMissingBodyMesssage");
            OkButtonContent = CLAppDelegate.Instance.PageCloudFolderMissingOkButtonContent;
        }

        /// <summary>
        /// Clean up all resources allocated, and save state as needed.
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            _rm = null;
        }

        #endregion
     

        #region Bound Properties

        /// <summary>
        /// The <see cref="BodyMessage" /> property's name.
        /// </summary>
        public const string BodyMessagePropertyName = "BodyMessage";
        private string _bodyMessage = "";

        /// <summary>
        /// Sets and gets the BodyMessage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string BodyMessage
        {
            get
            {
                return _bodyMessage;
            }

            set
            {
                if (_bodyMessage == value)
                {
                    return;
                }

                _bodyMessage = value;
                RaisePropertyChanged(BodyMessagePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="OkButtonContent" /> property's name.
        /// </summary>
        public const string OkButtonContentPropertyName = "OkButtonContent";
        private string _okButtonContent = "";

        /// <summary>
        /// Sets and gets the OkButtonContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string OkButtonContent
        {
            get
            {
                return _okButtonContent;
            }

            set
            {
                if (_okButtonContent == value)
                {
                    return;
                }

                _okButtonContent = value;
                RaisePropertyChanged(OkButtonContentPropertyName);
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
        /// The user clicked the OK button.
        /// The button will read "Restore" or "Locate...".  Restore occurs
        /// when the missing cloud folder is found in the recycle bin.  Locate...
        /// occurs when we don't find the renamed directory, and we don't find
        /// the missing folder in the recycle bin.  Locate... will put up the folder
        /// selection dialog to allow the user to select a folder in which to make
        /// a new Cloud folder.  The Cloud folder will be constructed in the folder
        /// selected by the user.  If the user cancels the folder selection dialog,
        /// he will be left on this PageCloudFolderMissing dialog in the same state.
        /// </summary>
        private RelayCommand _pageCloudFolderMissingViewModel_OkCommand;
        public RelayCommand PageCloudFolderMissingViewModel_OkCommand
        {
            get
            {
                return _pageCloudFolderMissingViewModel_OkCommand
                    ?? (_pageCloudFolderMissingViewModel_OkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Process the OK button click.
                                                if (this.OkButtonContent.Equals(_rm.GetString("pageCloudFolderMissingOkButtonLocate"), StringComparison.InvariantCulture))
                                                {
                                                    // This is the Locate... case.  Display the Windows Forms folder selection
                                                    // dialog.  Tell the view to put up the dialog.  If the user clicks cancel, 
                                                    // the view will return and we will stay on this window.  If the user clicks
                                                    // OK, the view will send the PageCloudFolderMissingViewModel_CreateCloudFolderCommand
                                                    // back to us, and we will create the cloud folder in that command method.
                                                    // TODO: This is strange for WPF, since the FolderBrowser is a Windows Form thing.
                                                    // The processing is synchronous from the VM to the View, show the dialog, wait
                                                    // for the dialog, then return on cancel, or issue a RelayCommand back to us,
                                                    // process the RelayCommand, then back to the View, then back to here.
                                                    // Should we be more asynchronous?
                                                    CLAppMessages.Message_PageCloudFolderMissingShouldChooseCloudFolder.Send("");
                                                }
                                                else if (this.OkButtonContent.Equals(_rm.GetString("pageCloudFolderMissingOkButtonRestore"), StringComparison.InvariantCulture))
                                                {
                                                    // This is the Restore case.  Restore the cloud folder from the recycle bin.
                                                    CLError error = null;
                                                    RestoreCloudFolderFromRecycleBin(out error);
                                                    if (error == null)
                                                    {
                                                        // Navigate to the PageInvisible page.  This will start the core services.
                                                        Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                                        CLAppMessages.PageCloudFolderMissing_NavigationRequest.Send(nextPage);
                                                    }
                                                    else
                                                    {
                                                        // Display the error message in a modal dialog
                                                        // Leave the user on this dialog when the user clicks OK on the error message modal dialog
                                                        CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                                                errorMessage: error.errorDescription, 
                                                                title: _rm.GetString("pageCloudFolderMissingErrorTitle"),
                                                                headerText: _rm.GetString("pageCloudFolderMissingErrorHeader"),
                                                                rightButtonContent: _rm.GetString("pageCloudFolderMissingErrorRightButtonContent"),
                                                                rightButtonIsDefault: true,
                                                                container: this.ViewGridContainer, 
                                                                dialog: out _dialog,
                                                                actionOkButtonHandler: 
                                                                    returnedViewModelInstance =>
                                                                    {
                                                                        // If the cloud folder actually exists at the new location, then we
                                                                        // were successful at moving it, even if an error was thrown.  In this
                                                                        // case, we will just use it and continue starting core services.
                                                                        // Otherwise, we will leave the user on this dialog, but change
                                                                        // the OK button to "Locate...".
                                                                        if (Directory.Exists(Settings.Instance.CloudFolderPath))
                                                                        {
                                                                            // Navigate to the PageInvisible page.  This will start the core services.
                                                                            Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                                                            CLAppMessages.PageCloudFolderMissing_NavigationRequest.Send(nextPage);
                                                                        }
                                                                        else
                                                                        {
                                                                            // Just leave the user on this same PageCloudFolderMissing window,
                                                                            // but change the OK button to Locate... since we had trouble
                                                                            // restoring the folder from the recycle bin.
                                                                            this.OkButtonContent = _rm.GetString("pageCloudFolderMissingOkButtonLocate");
                                                                        }
                                                                    }
                                                        );
                                                    }
                                                }
                                            }));                                              
            }
        }

        /// <summary>
        /// Move the cloud folder from the recycle bin back to its original location.
        /// </summary>
        private void RestoreCloudFolderFromRecycleBin(out CLError error)
        {
            try
            {
                // Move the directory and delete the recycle bin info file.
                Directory.Move(CLAppDelegate.Instance.FoundPathToDeletedCloudFolderRFile, Settings.Instance.CloudFolderPath);
                File.Delete(CLAppDelegate.Instance.FoundPathToDeletedCloudFolderIFile);
            }
            catch (Exception ex)
            {
                error = ex;
                return;
            }
            error = null;
        }

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        private RelayCommand _pageCloudFolderMissingViewModel_UnlinkCommand;
        public RelayCommand PageCloudFolderMissingViewModel_UnlinkCommand
        {
            get
            {
                return _pageCloudFolderMissingViewModel_UnlinkCommand
                    ?? (_pageCloudFolderMissingViewModel_UnlinkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Process the Remove button click.
                                                // We will unlink this device.  Stop all core services and exit the application
                                                CLError error = null;
                                                CLAppDelegate.Instance.UnlinkFromCloudDotCom(out error);
                                                if (error != null)
                                                {
                                                    CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                                        errorMessage: error.errorDescription,
                                                        title: _rm.GetString("pageCloudFolderMissingErrorTitle"),
                                                        headerText: _rm.GetString("pageCloudFolderMissingErrorHeader"),
                                                        rightButtonContent: _rm.GetString("pageCloudFolderMissingErrorRightButtonContent"),
                                                        rightButtonIsDefault: true,
                                                        container: this.ViewGridContainer,
                                                        dialog: out _dialog,
                                                        actionOkButtonHandler: 
                                                            returnedViewModelInstance =>
                                                            {
                                                                // Exit the app when the user clicks the OK button.
                                                                Application.Current.Shutdown();
                                                            }
                                                    );
                                                }
                                                else
                                                {
                                                    Application.Current.Shutdown();
                                                }
                                            }));                                              
            }
        }


        /// <summary>
        /// The user has selected a new cloud folder path.  Create the new cloud folder
        /// </summary>
        private RelayCommand<string> _pageCloudFolderMissingViewModel_CreateCloudFolderCommand;
        public RelayCommand<string> PageCloudFolderMissingViewModel_CreateCloudFolderCommand
        {
            get
            {
                return _pageCloudFolderMissingViewModel_CreateCloudFolderCommand
                    ?? (_pageCloudFolderMissingViewModel_CreateCloudFolderCommand = new RelayCommand<string>(
                                            (path) =>
                                            {
                                                // Create the new cloud folder.
                                                CLError error = null;
                                                DateTime creationTime;
                                                string cloudDirectoryName = path + "\\" + CLPrivateDefinitions.CloudDirectoryName;
                                                CLCreateCloudFolder.CreateCloudFolder(cloudDirectoryName, out creationTime, out error);
                                                if (error == null)
                                                {
                                                    // Cloud folder created
                                                    _trace.writeToLog(1, "PageCloudFolderMissingViewModel: Cloud folder created at <{0}>.", Settings.Instance.CloudFolderPath);

                                                    // Mark the creation time in Settings.
                                                    Settings.Instance.CloudFolderCreationTimeUtc = creationTime;
                                                    Settings.Instance.updateCloudFolderPath(cloudDirectoryName, creationTime);
                                                    Settings.Instance.setCloudAppSetupCompleted(true);

                                                    // Navigate to the PageInvisible page.  This will start the core services.
                                                    Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                                    CLAppMessages.PageCloudFolderMissing_NavigationRequest.Send(nextPage);
                                                }
                                                else
                                                {
                                                    // Error creating the cloud folder.  Display the error and stay on this dialog.
                                                    CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                                        errorMessage: error.errorDescription,
                                                        title: _rm.GetString("pageCloudFolderMissingErrorTitle"),
                                                        headerText: _rm.GetString("pageCloudFolderMissingErrorHeader"),
                                                        rightButtonContent: _rm.GetString("pageCloudFolderMissingErrorRightButtonContent"),
                                                        rightButtonIsDefault: true,
                                                        container: this.ViewGridContainer,
                                                        dialog: out _dialog,
                                                        actionOkButtonHandler:
                                                            returnedViewModelInstance =>
                                                            {
                                                                // Do nothing.  Stay on this dialog.
                                                            }
                                                    );
                                                }
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
                                              OnClosing();
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
                // Do nothing here when the user clicks the OK button.
                _trace.writeToLog(9, "PageCloudFolderMissingViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                {
                    // The user said yes.  Unlink this device.
                    _trace.writeToLog(9, "PageCloudFolderMissingViewModel: Prompt exit application: User said yes.");

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