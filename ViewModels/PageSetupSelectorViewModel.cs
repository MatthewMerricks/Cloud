//  PageSetupSelectorViewModel.cs
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
using System.Windows.Threading;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Static;
using win_client.ViewModelHelpers;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Messaging;
using CleanShutdown.Helpers;

namespace win_client.ViewModels
{
    #region "Definitions"

    public enum SetupSelectorOptions
    {
        OptionDefault,
        OptionAdvanced,
    }
    
    #endregion
         
    /// <summary>
    /// Page to select the Cloud storage size desired by the user.
    /// </summary>
    public class PageSetupSelectorViewModel : ValidatingViewModelBase, ICleanup
    {
        protected delegate void OnCloudSetupNotifyFolderLocationConflictResolvedDelegate(Dictionary<string, object> parameters);

        #region Instance Variables

        private readonly IDataService _dataService;
        private ResourceManager _rm;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageSetupSelectorViewModel(IDataService dataService)
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

        #region "Bindable Properties"

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
        /// The <see cref="PageSetupSelector_OptionSelected" /> property's name.
        /// </summary>
        public const string PageSetupSelector_OptionSelectedPropertyName = "PageSetupSelector_OptionSelected";
        private SetupSelectorOptions _pageSetupSelector_OptionSelected = Settings.Instance.UseDefaultSetup ?
                                                    SetupSelectorOptions.OptionDefault : SetupSelectorOptions.OptionAdvanced;
        public SetupSelectorOptions PageSetupSelector_OptionSelected
        {
            get
            {
                return _pageSetupSelector_OptionSelected;
            }

            set
            {
                if(_pageSetupSelector_OptionSelected == value)
                {
                    return;
                }

                _pageSetupSelector_OptionSelected = value;
                Settings.Instance.UseDefaultSetup = (_pageSetupSelector_OptionSelected == SetupSelectorOptions.OptionDefault);
                RaisePropertyChanged(PageSetupSelector_OptionSelectedPropertyName);
            }
        }
        /// <summary>
        /// The <see cref="IsMergingFolder" /> property's name.
        /// Indicates whether the user has selected to merge an existing Cloud
        /// folder with the new Cloud folder.
        /// </summary>
        public const string IsMergingFolderPropertyName = "IsMergingFolder";
        private bool _isMergingFolder = false;
        /// <summary>
        /// Sets and gets the IsMergingFolder property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsMergingFolder
        {
            get
            {
                return _isMergingFolder;
            }

            set
            {
                if (_isMergingFolder == value)
                {
                    return;
                }

                _isMergingFolder = value;
                RaisePropertyChanged(IsMergingFolderPropertyName);
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
        /// The page was navigated to.
        /// </summary>
        private RelayCommand _pageSetupSelector_NavigatedToCommand;
        public RelayCommand PageSetupSelector_NavigatedToCommand
        {
            get
            {
                return _pageSetupSelector_NavigatedToCommand
                    ?? (_pageSetupSelector_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Load the current state from the persistent settings.
                                                PageSetupSelector_OptionSelected = Settings.Instance.UseDefaultSetup ? 
                                                    SetupSelectorOptions.OptionDefault : SetupSelectorOptions.OptionAdvanced;
                                            }));
            }
        }

        /// <summary>
        /// The user clicked the back button.
        /// </summary>
        private RelayCommand _pageSetupSelector_BackCommand;
        public RelayCommand PageSetupSelector_BackCommand
        {
            get
            {
                return _pageSetupSelector_BackCommand
                    ?? (_pageSetupSelector_BackCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Return to the storage size selector dialog
                                                Uri nextPage = new System.Uri(CLConstants.kPageSelectStorageSize, System.UriKind.Relative);
                                                CLAppMessages.PageSetupSelector_NavigationRequest.Send(nextPage);
                                            }));
            }
        }
        
        /// <summary>
        /// The user clicked has selected a choice and will continue.
        /// </summary>
        private RelayCommand _pageSetupSelector_ContinueCommand;
        public RelayCommand PageSetupSelector_ContinueCommand
        {
            get
            {
                return _pageSetupSelector_ContinueCommand
                    ?? (_pageSetupSelector_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Try to perform the installation
                                                goForward();
                                            }));                                              
            }
        }

        /// <summary>
        /// The user clicked the area over the Default radio button.
        /// </summary>
        private RelayCommand _pageSetupSelector_DefaultAreaCommand;
        public RelayCommand PageSetupSelector_DefaultAreaCommand
        {
            get
            {
                return _pageSetupSelector_DefaultAreaCommand
                    ?? (_pageSetupSelector_DefaultAreaCommand = new RelayCommand(
                                            () =>
                                            {
                                                PageSetupSelector_OptionSelected = SetupSelectorOptions.OptionDefault;
                                            }));
            }
        }


        /// <summary>
        /// The user clicked the area over the Advanced radio button.
        /// </summary>
        private RelayCommand _pageSetupSelector_AdvancedAreaCommand;
        public RelayCommand PageSetupSelector_AdvancedAreaCommand
        {
            get
            {
                return _pageSetupSelector_AdvancedAreaCommand
                    ?? (_pageSetupSelector_AdvancedAreaCommand = new RelayCommand(
                                            () =>
                                            {
                                                PageSetupSelector_OptionSelected = SetupSelectorOptions.OptionAdvanced;
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

        #region "Installation"

        /// <summary>
        /// Try to install the Cloud folder and support.
        /// </summary>
        private void goForward()
        {
            // Process by whether this is a default or advance installation
            if (_pageSetupSelector_OptionSelected == SetupSelectorOptions.OptionDefault)
            {
                if (Directory.Exists(Settings.Instance.CloudFolderPath) && !_isMergingFolder)
                {
                    // Tell the user that there is already a Cloud folder at that location.  Allow him to choose 'Select new location' or 'Merge'.
                    string cloudFolderRoot = Path.GetDirectoryName(Settings.Instance.CloudFolderPath);  // e.g., "c:/Users/<username>/Documents", if CloudFolderPath is "c:/Users/<username>/Documents/Cloud"
                    string userMessageBody = _rm.GetString("folderExitTextFieldBody");
                    userMessageBody = String.Format(userMessageBody, cloudFolderRoot);
                    string userMessageTitle = _rm.GetString("folderExitTextFieldTitle");
                    string userMessageHeader = _rm.GetString("folderExitTextFieldHeader");
                    string userMessageButtonSelectNewLocation = _rm.GetString("folderExitTextFieldButtonSelectNewLocation");
                    string userMessageButtonMerge = _rm.GetString("folderExitTextFieldButtonMerge");

                    // Ask the user to 'Select new location' or 'Merge" the cloud folder.
                    _trace.writeToLog(9, "goForward: Put up 'Select new location' or 'Merge' dialog.");
                    CLModalMessageBoxDialogs.Instance.DisplayModalMessageBox(
                        windowHeight: 250,
                        leftButtonWidth: 200,
                        rightButtonWidth: 100,
                        title: userMessageTitle,
                        headerText: userMessageHeader,
                        bodyText: userMessageBody,
                        leftButtonContent: userMessageButtonSelectNewLocation,
                        leftButtonIsDefault: false,
                        leftButtonIsCancel: false,
                        rightButtonContent: userMessageButtonMerge,
                        rightButtonIsDefault: true,
                        rightButtonIsCancel: false,
                        container: ViewGridContainer,
                        dialog: out _dialog,
                        actionResultHandler:
                            returnedViewModelInstance =>
                            {
                                _trace.writeToLog(9, "goForward: returnedViewModelInstance: Entry.");
                                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                {
                                    // The user selected Merge.  The standard Cloud folder will be used, with the user's existing files in it.
                                    _trace.writeToLog(9, "goForward: User selected Merge.");
                                    var notifyParms = new Dictionary<string, object>();
                                    notifyParms.Add(CLConstants.kFolderLocation, "");
                                    notifyParms.Add(CLConstants.kMergeFolders, true);
                                    OnCloudSetupNotifyFolderLocationConflictResolvedDelegate del = OnCloudSetupNotifyFolderLocationConflictResolved;
                                    var dispatcher = CLAppDelegate.Instance.MainDispatcher; 
                                    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), del, notifyParms);
                                }
                                else
                                {
                                    // The user will select a new Cloud folder location.  Put up a modal dialog for now.
                                    // This dialog will allow the user to simply type in the location of the Cloud folder.
                                    // It will have Back and Continue buttons.  The Continue button will cause validation
                                    // and will save the selected Cloud folder location in the settings.
                                    //
                                    // TODO: We need the FolderBrowserDialog from WPF.  Either switch to WPF for the Windows
                                    // desktop, or implement a WPF ActiveX DLL to perform that function.
                                    var dialogFolderSelection = SimpleIoc.Default.GetInstance<IModalWindow>(CLConstants.kDialogBox_FolderSelectionSimpleView);
                                    IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
                                    modalDialogService.ShowDialog(dialogFolderSelection, new DialogFolderSelectionSimpleViewModel
                                    {
                                        FolderSelectionSimpleViewModel_FolderLocationText = cloudFolderRoot,
                                        FolderSelectionSimpleViewModel_ButtonLeftText = _rm.GetString("folderSelectionSimpleButtonLeftText"),
                                        FolderSelectionSimpleViewModel_ButtonRightText = _rm.GetString("folderSelectionSimpleButtonRightText"),
                                    },
                                    ViewGridContainer,
                                    returnedFolderSelectionSimpleViewModelInstance =>
                                    {
                                        if (dialogFolderSelection.DialogResult.HasValue && dialogFolderSelection.DialogResult.Value)
                                        {
                                            // The user selected a new folder location.
                                            var notifyParms = new Dictionary<string, object>();
                                            notifyParms.Add(CLConstants.kFolderLocation, returnedFolderSelectionSimpleViewModelInstance.FolderSelectionSimpleViewModel_FolderLocationText);
                                            notifyParms.Add(CLConstants.kMergeFolders, false);
                                            OnCloudSetupNotifyFolderLocationConflictResolvedDelegate del = OnCloudSetupNotifyFolderLocationConflictResolved;
                                            var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), del, notifyParms);
                                        }
                                        else
                                        {
                                            // @@@@@ Do Nothing.  Just leave the user on the underlying SetupSelector dialog.
                                        }
                                    });
                                }
                            });

                    return;
                }

                // Finish the setup.
                CLError err = null;
                CLAppDelegate.Instance.installCloudServices(out err);
                if (err != null)
                {
                    // An error occurred.  Show the user an Oh Snap! modal dialog.
                    CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                errorMessage:  err.errorDescription,
                                title:  _rm.GetString("appDelegateErrorInstallingTitle"),
                                headerText: _rm.GetString("appDelegateErrorInstallingHeader"),
                                rightButtonContent: _rm.GetString("appDelegateErrorInstallingButtonTryAgain"),
                                rightButtonIsDefault: true,
                                container: ViewGridContainer,
                                dialog: out _dialog,
                                actionOkButtonHandler: 
                                    returnedViewModelInstance =>
                                    {
                                        if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                        {
                                            // The user selected Try Again.  Redrive this function on the main thread, but not recursively.
                                            var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () => { goForward(); });
                                        }
                                        else
                                        {
                                            // @@@@@@@@@ DO NOTHING @@@@@ The user selected Ignore.  We will just leave them on the SetupSelection page.
                                        }
                                    });
                }
                else
                {
                    // Start the tour
                    string nextPageName = string.Format("{0}{1}{2}", CLConstants.kPageTour, 1, CLConstants.kXamlSuffix);
                    Uri nextPage = new System.Uri(nextPageName, System.UriKind.Relative);
                    CLAppMessages.PageSetupSelector_NavigationRequest.Send(nextPage);
                }
            }
            else
            {
                // TODO: &&&&This is the advanced install
                //remove ourselves from the notification subscription
                //put up the advanced setup dialog in this window
                //; CLFolderSelectionViewController
#if TRASH           
                                                    // remove oursevles from observing an event. (Oh I miss, viewWillAppear, viewWillDispear from UIKit). Sigh...
                                                    [[NSNotificationCenter defaultCenter] removeObserver:self];

                                                    // Move to advanced setup
                                                    self.folderSelectionViewController = [[CLFolderSelectionViewController alloc] initWithNibName:(LFolderSelectionViewController" bundle:nil];
                                                    [self.folderSelectionViewController setSetupSelectorViewController:self];
            
                                                    [[[NSApp mainWindow] contentView] addSubview:self.folderSelectionViewController.view];

                                                    PushAnimation *animation = [[PushAnimation alloc] initWithDuration:0.25f animationCurve:NSAnimationLinear];
                                                    [animation setNewDirection:RightDirection];
                                                    [animation setStartingView:self.view];
                                                    [animation setDestinationView:self.folderSelectionViewController.view];
                                                    [animation setAnimationBlockingMode:NSAnimationNonblocking];
                                                    [animation startAnimation];
#endif  // end TRASH
            }
        }

        #endregion

        #region "Message Handlers"
        /// <summary>
        /// Event: The folder location conflict is resolved.
        /// </summary>
        protected void OnCloudSetupNotifyFolderLocationConflictResolved(Dictionary<string, object> parameters) 
        {
            IsMergingFolder = (bool)parameters[CLConstants.kMergeFolders];
            if (!_isMergingFolder)
            {
                Settings.Instance.CloudFolderPath = (string)parameters[CLConstants.kFolderLocation];        
            }

            goForward();
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
                _trace.writeToLog(9, "PageSetupSelectorViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                {
                    // The user said yes.  Unlink this device.
                    _trace.writeToLog(9, "PageSetupSelectorViewModel: Prompt exit application: User said yes.");

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