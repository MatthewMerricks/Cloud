//  PageFolderSelectionViewModel.cs
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
using Cloud;
using Cloud.Support;
using Cloud.Model;
using Cloud.Static;
using CloudApiPrivate.Static;
using win_client.ViewModelHelpers;
using win_client.Resources;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Messaging;
using CleanShutdown.Helpers;

namespace win_client.ViewModels
{
    /// <summary>
    /// Page to select the Cloud storage size desired by the user.
    /// </summary>
    public class PageFolderSelectionViewModel : ValidatingViewModelBase, ICleanup
    {
        protected delegate void OnCloudSetupNotifyFolderLocationConflictResolvedDelegate(Dictionary<string, object> parameters);

        #region Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked
        private bool _isResolvingCloudFolderConflict = false;  // true: The view was asked to have the user choose a new Cloud folder path for the purpose of resolving a folder merge conflict.

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageFolderSelectionViewModel class.
        /// </summary>
        public PageFolderSelectionViewModel(IDataService dataService)
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
        /// The <see cref="IsMergingFolder" /> property's name.
        /// Indicates whether the user has selected to merge an existing Cloud
        /// folder with the new Cloud folder.
        /// </summary>
        public const string IsMergingFolderPropertyName = "IsMergingFolder";
        private bool _isMergingFolder = false;
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
        /// The <see cref="PageFolderSelection_CloudFolder" /> property's name.
        /// </summary>
        public const string PageFolderSelection_CloudFolderPropertyName = "PageFolderSelection_CloudFolder";
        private string _pageFolderSelection_CloudFolder = String.Empty;
        public string PageFolderSelection_CloudFolder
        {
            get
            {
                return _pageFolderSelection_CloudFolder;
            }

            set
            {
                if (_pageFolderSelection_CloudFolder == value)
                {
                    return;
                }

                // Enable or disable the Reset button depending on the Cloud folder path being set (whether it is the default path or not).
                PageFolderSelection_ResetButtonEnabled = !value.Equals(Settings.Instance.GetDefaultCloudFolderPath(), StringComparison.InvariantCulture);

                _pageFolderSelection_CloudFolder = value;
                RaisePropertyChanged(PageFolderSelection_CloudFolderPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="PageFolderSelection_ResetButtonEnabled" /> property's name.
        /// </summary>
        public const string PageFolderSelection_ResetButtonEnabledPropertyName = "PageFolderSelection_ResetButtonEnabled";
        private bool _pageFolderSelection_ResetButtonEnabled = false;
        public bool PageFolderSelection_ResetButtonEnabled
        {
            get
            {
                return _pageFolderSelection_ResetButtonEnabled;
            }

            set
            {
                if (_pageFolderSelection_ResetButtonEnabled == value)
                {
                    return;
                }

                _pageFolderSelection_ResetButtonEnabled = value;
                RaisePropertyChanged(PageFolderSelection_ResetButtonEnabledPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CbAddCloudFolderShortcutToDesktop" /> property's name.
        /// </summary>
        public const string CbAddCloudFolderShortcutToDesktopPropertyName = "CbAddCloudFolderShortcutToDesktop";
        private bool _cbAddCloudFolderShortcutToDesktop = false;
        public bool CbAddCloudFolderShortcutToDesktop
        {
            get
            {
                return _cbAddCloudFolderShortcutToDesktop;
            }

            set
            {
                if (_cbAddCloudFolderShortcutToDesktop == value)
                {
                    return;
                }

                _cbAddCloudFolderShortcutToDesktop = value;
                Settings.Instance.ShouldAddShowCloudFolderOnDesktop = value;
                RaisePropertyChanged(CbAddCloudFolderShortcutToDesktopPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CbAddCloudFolderShortcutToTaskbar" /> property's name.
        /// </summary>
        public const string CbAddCloudFolderShortcutToTaskbarPropertyName = "CbAddCloudFolderShortcutToTaskbar";
        private bool _cbAddCloudFolderShortcutToTaskbar = false;
        public bool CbAddCloudFolderShortcutToTaskbar
        {
            get
            {
                return _cbAddCloudFolderShortcutToTaskbar;
            }

            set
            {
                if (_cbAddCloudFolderShortcutToTaskbar == value)
                {
                    return;
                }

                _cbAddCloudFolderShortcutToTaskbar = value;
                Settings.Instance.ShouldAddShowCloudFolderOnTaskbar = value;
                RaisePropertyChanged(CbAddCloudFolderShortcutToTaskbarPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CbAddCloudFolderShortcutToStartMenu" /> property's name.
        /// </summary>
        public const string CbAddCloudFolderShortcutToStartMenuPropertyName = "CbAddCloudFolderShortcutToStartMenu";
        private bool _cbAddCloudFolderShortcutToStartMenu = false;
        public bool CbAddCloudFolderShortcutToStartMenu
        {
            get
            {
                return _cbAddCloudFolderShortcutToStartMenu;
            }

            set
            {
                if (_cbAddCloudFolderShortcutToStartMenu == value)
                {
                    return;
                }

                _cbAddCloudFolderShortcutToStartMenu = value;
                Settings.Instance.ShouldAddShowCloudFolderInStartMenu = value;
                RaisePropertyChanged(CbAddCloudFolderShortcutToStartMenuPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CbAddCloudFolderShortcutToStartMenu" /> property's name.
        /// </summary>
        public const string CbAddCloudFolderShortcutToExplorerFavoritesPropertyName = "CbAddCloudFolderShortcutToExplorerFavorites";
        private bool _cbAddCloudFolderShortcutToExplorerFavorites = false;
        public bool CbAddCloudFolderShortcutToExplorerFavorites
        {
            get
            {
                return _cbAddCloudFolderShortcutToExplorerFavorites;
            }

            set
            {
                if (_cbAddCloudFolderShortcutToExplorerFavorites == value)
                {
                    return;
                }

                _cbAddCloudFolderShortcutToExplorerFavorites = value;
                Settings.Instance.ShouldAddShowCloudFolderInExplorerFavorites = value;
                RaisePropertyChanged(CbAddCloudFolderShortcutToExplorerFavoritesPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CbAddCloudFolderShortcutToInternetExplorerFavorites" /> property's name.
        /// </summary>
        public const string CbAddCloudFolderShortcutToInternetExplorerFavoritesPropertyName = "CbAddCloudFolderShortcutToInternetExplorerFavorites";
        private bool _cbAddCloudFolderShortcutToInternetExplorerFavorites = false;
        public bool CbAddCloudFolderShortcutToInternetExplorerFavorites
        {
            get
            {
                return _cbAddCloudFolderShortcutToInternetExplorerFavorites;
            }

            set
            {
                if (_cbAddCloudFolderShortcutToInternetExplorerFavorites == value)
                {
                    return;
                }

                _cbAddCloudFolderShortcutToInternetExplorerFavorites = value;
                Settings.Instance.ShouldAddShowCloudFolderInInternetExplorerFavorites = value;
                RaisePropertyChanged(CbAddCloudFolderShortcutToInternetExplorerFavoritesPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="IsBusy" /> property's name.
        /// </summary>
        public const string IsBusyPropertyName = "IsBusy";
        private bool _isBusy = false;
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }

            set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                RaisePropertyChanged(IsBusyPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="BusyContent" /> property's name.
        /// </summary>
        public const string BusyContentPropertyName = "BusyContent";
        private string _busyContent = "Setting up.  Please wait...";
        public string BusyContent
        {
            get
            {
                return _busyContent;
            }

            set
            {
                if (_busyContent == value)
                {
                    return;
                }

                _busyContent = value;
                RaisePropertyChanged(BusyContentPropertyName);
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
        private RelayCommand _pageFolderSelection_NavigatedToCommand;
        public RelayCommand PageFolderSelection_NavigatedToCommand
        {
            get
            {
                return _pageFolderSelection_NavigatedToCommand
                    ?? (_pageFolderSelection_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Load the current state from the persistent settings.
                                                PageFolderSelection_CloudFolder = Settings.Instance.CloudFolderPath;
                                                CbAddCloudFolderShortcutToDesktop = Settings.Instance.ShouldAddShowCloudFolderOnDesktop;
                                                CbAddCloudFolderShortcutToTaskbar = Settings.Instance.ShouldAddShowCloudFolderOnTaskbar;
                                                CbAddCloudFolderShortcutToStartMenu = Settings.Instance.ShouldAddShowCloudFolderInStartMenu;
                                                CbAddCloudFolderShortcutToExplorerFavorites = Settings.Instance.ShouldAddShowCloudFolderInExplorerFavorites;
                                                CbAddCloudFolderShortcutToInternetExplorerFavorites = Settings.Instance.ShouldAddShowCloudFolderInInternetExplorerFavorites;
                                            }));
            }
        }

        /// <summary>
        /// Gets the PageFolderSelection_ChangeCloudFolder.
        /// </summary>
        private ICommand _pageFolderSelection_ChangeCloudFolder;
        public ICommand PageFolderSelection_ChangeCloudFolder
        {
            get
            {
                return _pageFolderSelection_ChangeCloudFolder
                    ?? (_pageFolderSelection_ChangeCloudFolder = new RelayCommand(
                                            () =>
                                            {
                                                CLModalMessageBoxDialogs.Instance.DisplayModalMessageBox(
                                                    windowHeight: 250,
                                                    leftButtonWidth: 75,
                                                    rightButtonWidth: 75,
                                                    title: Resources.Resources.PageFolderSelection_ChangeCloudFolderTitle,
                                                    headerText: Resources.Resources.PageFolderSelection_ChangeCloudFolderHeaderText,
                                                    bodyText: Resources.Resources.PageFolderSelection_ChangeCloudFolderBodyText,
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
                                                            _trace.writeToLog(9, "PageFolderSelection: Move cloud folder: Entry.");
                                                            if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                                            {
                                                                // The user said yes.  Tell the view to put up the folder browser so the user can select the new location.
                                                                _trace.writeToLog(9, "PageFolderSelection: Move cloud folder: User said yes.");

                                                                // Display the Windows Forms folder selection
                                                                // dialog.  Tell the view to put up the dialog.  If the user clicks cancel, 
                                                                // the view will return and we will stay on this window.  If the user clicks
                                                                // OK, the view will send the PageFolderSelectionViewModel_CreateCloudFolderCommand
                                                                // back to us, and we will create the cloud folder in that command method.
                                                                // TODO: This is strange for WPF, since the FolderBrowser is a Windows Form thing.
                                                                // The processing is synchronous from the VM to the View, show the dialog, wait
                                                                // for the dialog, then return on cancel, or issue a RelayCommand back to us,
                                                                // process the RelayCommand, then back to the View, then back to here.
                                                                // Should we be more asynchronous?
                                                                var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                                                dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                                                                {
                                                                    _isResolvingCloudFolderConflict = false;
                                                                    CLAppMessages.Message_PageFolderSelection_ShouldChooseCloudFolder.Send("");
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
        /// Gets the PageFolderSelection_ResetCloudFolder.
        /// </summary>
        private ICommand _pageFolderSelection_ResetCloudFolder;
        public ICommand PageFolderSelection_ResetCloudFolder
        {
            get
            {
                return _pageFolderSelection_ResetCloudFolder
                    ?? (_pageFolderSelection_ResetCloudFolder = new RelayCommand(
                                            () =>
                                            {
                                                // If the Cloud folder is already at the default location, this shouldn't have happened.
                                                // Just exit, but disable the user's Reset button.
                                                if (Settings.Instance.CloudFolderPath.Equals(Settings.Instance.GetDefaultCloudFolderPath(), StringComparison.InvariantCulture))
                                                {
                                                    PageFolderSelection_ResetButtonEnabled = false;
                                                }
                                                else
                                                {
                                                    // Change the path to the default value.
                                                    string toPath = Settings.Instance.GetDefaultCloudFolderPath();
                                                    Settings.Instance.updateCloudFolderPath(toPath, DateTime.MinValue);

                                                    // Update visible path
                                                    PageFolderSelection_CloudFolder = toPath;
                                                }
                                            }));

            }
        }

        /// <summary>
        /// The user has selected a new cloud folder path.  Create the new cloud folder
        /// </summary>
        private GalaSoft.MvvmLight.Command.RelayCommand<string> _pageFolderSelectionViewModel_CreateCloudFolderCommand;
        public GalaSoft.MvvmLight.Command.RelayCommand<string> PageFolderSelectionViewModel_CreateCloudFolderCommand
        {
            get
            {
                return _pageFolderSelectionViewModel_CreateCloudFolderCommand
                    ?? (_pageFolderSelectionViewModel_CreateCloudFolderCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(
                                            (path) =>
                                            {
                                                if (_isResolvingCloudFolderConflict)
                                                {
                                                    // The user selected a new folder location.
                                                    var notifyParms = new Dictionary<string, object>();
                                                    notifyParms.Add(CLConstants.kFolderLocation, path);
                                                    notifyParms.Add(CLConstants.kMergeFolders, false);
                                                    OnCloudSetupNotifyFolderLocationConflictResolvedDelegate del = OnCloudSetupNotifyFolderLocationConflictResolved;
                                                    var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                                    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), del, notifyParms);
                                                }
                                                else
                                                {
                                                    // The user selected the new location to house the Cloud folder.  Save it.
                                                    string toPath = path + "\\Cloud";
                                                    Settings.Instance.updateCloudFolderPath(toPath, DateTime.MinValue);

                                                    // Update visible path
                                                    PageFolderSelection_CloudFolder = toPath;
                                                }
                                            }));
            }
        }


        /// <summary>
        /// Gets the PageFolderSelection_ChangeSelectiveSyncSettings.
        /// </summary>
        private ICommand _pageFolderSelection_ChangeSelectiveSyncSettings;
        public ICommand PageFolderSelection_ChangeSelectiveSyncSettings
        {
            get
            {
                return _pageFolderSelection_ChangeSelectiveSyncSettings
                    ?? (_pageFolderSelection_ChangeSelectiveSyncSettings = new RelayCommand(
                                          () =>
                                          {
                                              //TODO: Actually handle a request for more space.
                                              CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                                  errorMessage: "You can't get more space right now.  It's not implemented yet..",
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
        /// The user clicked the back button.
        /// </summary>
        private RelayCommand _pageFolderSelection_BackCommand;
        public RelayCommand PageFolderSelection_BackCommand
        {
            get
            {
                return _pageFolderSelection_BackCommand
                    ?? (_pageFolderSelection_BackCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Return to the storage size selector dialog
                                                Uri nextPage = new System.Uri(CLConstants.kPageSetupSelector, System.UriKind.Relative);
                                                CLAppMessages.PageFolderSelection_NavigationRequest.Send(nextPage);
                                            }));
            }
        }
        
        /// <summary>
        /// The user clicked has selected a choice and will continue.
        /// </summary>
        private RelayCommand _pageFolderSelection_ContinueCommand;
        public RelayCommand PageFolderSelection_ContinueCommand
        {
            get
            {
                return _pageFolderSelection_ContinueCommand
                    ?? (_pageFolderSelection_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Try to perform the installation
                                                goForward();
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

        #endregion

        #region "Installation"

        /// <summary>
        /// Try to install the Cloud folder and support.
        /// </summary>
        private void goForward()
        {
            // Merged 8/9/12
            //if ([self checkCloudFolderExistsAtPath:[[CLSettings sharedSettings] cloudFolderPath]]
            //    && self.mergeFolders == NO) {
        
            //    NSString *cloudFolderRoot = [[[CLSettings sharedSettings] cloudFolderPath] stringByDeletingLastPathComponent];
            //    NSString *updatedTextFieldWithPath = [NSString stringWithFormat:[self.setupSelectorViewController.folderExitTextFiled stringValue], cloudFolderRoot];
            //    [self.setupSelectorViewController.folderExitTextFiled setStringValue:updatedTextFieldWithPath];
            //    [NSApp beginSheet:self.setupSelectorViewController.folderExistPanel modalForWindow:[self.view window] modalDelegate:self didEndSelector:nil contextInfo:nil];
        
            //    return; // if folder exists we will present a selection option to the user.  This will ask "Merge with existing Cloud folder?, "Select new Location", "Merge".
            //}
    
            //// finish setup
            //CLAppDelegate *delegate = [NSApp delegate];
            //NSError *error = [delegate installCloudServices];

            //if (error) {

            //    NSAlert *alert = [NSAlert alertWithMessageText:[error localizedDescription] defaultButton:@"Try Again" alternateButton:@"Dismiss" otherButton:nil informativeTextWithFormat:@""];
            //    [alert setIcon:[NSImage imageNamed:NSImageNameInfo]];            
            //    [alert beginSheetModalForWindow:[self.view window] modalDelegate:self didEndSelector:@selector(alertDidEnd:returnCode:contextInfo:) contextInfo:nil];
        
            //} else {
        
            //    // setup successful. let's present go on to the tour.
            //    self.tourViewController = [[CLTourViewController alloc] initWithNibName:@"CLTourViewController" bundle:nil];
            //    [[[NSApp mainWindow] contentView] addSubview:self.tourViewController.view];
        
            //    PushAnimation *animation = [[PushAnimation alloc] initWithDuration:0.25f animationCurve:NSAnimationLinear];
            //    [animation setNewDirection:RightDirection];
            //    [animation setStartingView:self.view];
            //    [animation setDestinationView:self.tourViewController.view];
            //    [animation setAnimationBlockingMode:NSAnimationNonblocking];
            //    [animation startAnimation];
        
            //}
            //&&&&&

            // Get the creation time of the cloud folder if it exists
            DateTime cloudFolderCreationTime = DateTime.MinValue;
            bool cloudFolderExists = Directory.Exists(Settings.Instance.CloudFolderPath);
            if (cloudFolderExists)
            {
                DirectoryInfo info = new DirectoryInfo(Settings.Instance.CloudFolderPath);
                cloudFolderCreationTime = info.CreationTime.ToUniversalTime();
            }

            // Show the Locate/Merge dialog if we don't have an exact folder match at the current location, 
            // and the directory exists, and we are not merging
            if (!(cloudFolderExists && cloudFolderCreationTime == Settings.Instance.CloudFolderCreationTimeUtc)
                && (cloudFolderExists && !_isMergingFolder))
            {
                // Tell the user that there is already a Cloud folder at that location.  Allow him to choose 'Select new location' or 'Merge'.
                string cloudFolderRoot = Path.GetDirectoryName(Settings.Instance.CloudFolderPath);  // e.g., "c:/Users/<username>/Documents", if CloudFolderPath is "c:/Users/<username>/Documents/Cloud"
                string userMessageBody = Resources.Resources.folderExitTextFieldBody;
                userMessageBody = String.Format(userMessageBody, cloudFolderRoot);
                string userMessageTitle = Resources.Resources.folderExitTextFieldTitle;
                string userMessageHeader = Resources.Resources.folderExitTextFieldHeader;
                string userMessageButtonSelectNewLocation = Resources.Resources.folderExitTextFieldButtonSelectNewLocation;
                string userMessageButtonMerge = Resources.Resources.folderExitTextFieldButtonMerge;

                // Ask the user to 'Select new location' or 'Merge" the cloud folder.
                _trace.writeToLog(9, "PageFolderSelectionViewModel: goForward: Put up 'Select new location' or 'Merge' dialog.");
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
                                // The user will select a new Cloud folder location.
                                // Display the Windows Forms folder selection
                                // dialog.  Tell the view to put up the dialog.  If the user clicks cancel, 
                                // the view will return and we will stay on this window.  If the user clicks
                                // OK, the view will send the PageFolderSelectionViewModel_CreateCloudFolderCommand
                                // back to us, and we will create the cloud folder in that command method.
                                // TODO: This is strange for WPF, since the FolderBrowser is a Windows Form thing.
                                // The processing is synchronous from the VM to the View, show the dialog, wait
                                // for the dialog, then return on cancel, or issue a RelayCommand back to us,
                                // process the RelayCommand, then back to the View, then back to here.
                                // Should we be more asynchronous?
                                var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
                                {
                                    _isResolvingCloudFolderConflict = true;     // selecing a new location to resolve a conflict.
                                    CLAppMessages.Message_PageFolderSelection_ShouldChooseCloudFolder.Send("");
                                });
                            }
                        });

                return;
            }

            // Finish the setup.
            IsBusy = true;
            CLAppDelegate.Instance.InstallCloudServicesAsync(InstallCloudServicesAsyncCallback, timeoutInSeconds: 30);
        }

        /// <summary>
        /// Called when InstallCloudServicesAsync completes
        /// </summary>
        private void InstallCloudServicesAsyncCallback(CLError err)
        {
            IsBusy = false;
            if (err != null)
            {
                // An error occurred.  Show the user an Oh Snap! modal dialog.
                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                            errorMessage: err.errorDescription,
                            title: Resources.Resources.appDelegateErrorInstallingTitle,
                            headerText: Resources.Resources.appDelegateErrorInstallingHeader,
                            rightButtonContent: Resources.Resources.appDelegateErrorInstallingButtonTryAgain,
                            rightButtonIsDefault: true,
                            rightButtonIsCancel: true,
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
                Uri nextPage = new System.Uri(CLConstants.kPageTourAdvancedEnd, System.UriKind.Relative);
                CLAppMessages.PageFolderSelection_NavigationRequest.Send(nextPage);
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
                _trace.writeToLog(9, "PageFolderSelectionViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                {
                    // The user said yes.
                    _trace.writeToLog(9, "PageFolderSelectionViewModel: Prompt exit application: User said yes.");

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