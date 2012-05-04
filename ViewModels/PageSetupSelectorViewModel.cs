using GalaSoft.MvvmLight;
using win_client.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using MVVMProductsDemo.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using win_client.Common;
using System.Reflection;
using System.Linq;
using win_client.DataModels.Settings;

namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to select the Cloud storage size desired by the user.
    /// </summary>
    public class PageSetupSelectorViewModel : ValidatingViewModelBase
    {
        #region Instance Variables
        private readonly IDataService _dataService;

        private RelayCommand _pageSetupSelector_NavigatedToCommand;        
        private RelayCommand _pageSetupSelector_BackCommand;
        private RelayCommand _pageSetupSelector_ContinueCommand;
        #endregion

        #region "Bindable Properties"

        /// <summary>
        /// The <see cref="IsDefaultSelected" /> property's name.
        /// </summary>
        public const string IsDefaultSelectedPropertyName = "IsDefaultSelected";
        public const string IsAdvancedSelectedPropertyName = "IsAdvancedSelected";

        private bool _isDefaultSelected = Settings.Instance.UseDefaultSetup;
        private bool _isAdvancedSelected = !Settings.Instance.UseDefaultSetup;

        /// <summary>
        /// Indicates the state of the Default radio button selected by the user.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDefaultSelected
        {
            get
            {
                return _isDefaultSelected;
            }

            set
            {
                _isDefaultSelected = value;
                _isAdvancedSelected = !_isDefaultSelected;
                Settings.Instance.UseDefaultSetup = _isDefaultSelected;
                RaisePropertyChanged(IsDefaultSelectedPropertyName);
                RaisePropertyChanged(IsAdvancedSelectedPropertyName);
            }
        }

        /// <summary>
        /// Indicates the state of the Advanced radio button selected by the user.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsAdvancedSelected
        {
            get
            {
                return _isAdvancedSelected;
            }

            set
            {
                _isAdvancedSelected = value;
                _isDefaultSelected = !_isAdvancedSelected;
                Settings.Instance.UseDefaultSetup = _isDefaultSelected;
                RaisePropertyChanged(IsDefaultSelectedPropertyName);
                RaisePropertyChanged(IsAdvancedSelectedPropertyName);
            }
        }
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
        }
        #endregion
      
        #region Commands

        /// <summary>
        /// The page was navigated to.
        /// </summary>
        public RelayCommand PageSetupSelector_NavigatedToCommand
        {
            get
            {
                return _pageSetupSelector_NavigatedToCommand
                    ?? (_pageSetupSelector_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Load the current state from the persistent settings.
                                                IsDefaultSelected = Settings.Instance.UseDefaultSetup;
                                            }));
            }
        }

        /// <summary>
        /// The user clicked the back button.
        /// </summary>
        public RelayCommand PageSetupSelector_BackCommand
        {
            get
            {
                return _pageSetupSelector_BackCommand
                    ?? (_pageSetupSelector_BackCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Return to the storage size selector dialog
                                                Uri nextPage = new System.Uri("/PageSelectStorageSize", System.UriKind.Relative);
                                                SendNavigationRequestMessage(nextPage);
                                            }));
            }
        }
        
        /// <summary>
        /// The user clicked has selected a choice and will continue.
        /// </summary>
        public RelayCommand PageSetupSelector_ContinueCommand
        {
            get
            {
                return _pageSetupSelector_ContinueCommand
                    ?? (_pageSetupSelector_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                
#if TRASH
                                                if ([[self.continueButton title] isEqualToString:@"Install"]) {
                
                                                    if ([self checkCloudFolderExistsAtPath:[[CLSettings sharedSettings] cloudFolderPath]]
                                                        && self.mergeFolders == NO) {
            
                                                        NSString *cloudFolderRoot = [[[CLSettings sharedSettings] cloudFolderPath] stringByDeletingLastPathComponent];
                                                        NSString *updatedTextFieldWithPath = [NSString stringWithFormat:[self.folderExitTextFiled stringValue], cloudFolderRoot];
                                                        [self.folderExitTextFiled setStringValue:updatedTextFieldWithPath];
                                                        [NSApp beginSheet:self.folderExistPanel modalForWindow:[self.view window] modalDelegate:self didEndSelector:nil contextInfo:nil];
            
                                                        return; // if folder exists we will present some options to the user.
                                                    }
        
                                                    // finish setup
                                                    CLAppDelegate *delegate = [NSApp delegate];
                                                    NSError *error = [delegate installCloudServices];
        
                                                    if (error) {
            
                                                        NSAlert *alert = [NSAlert alertWithMessageText:[error localizedDescription] defaultButton:@"Try Again" alternateButton:@"Dismiss" otherButton:nil informativeTextWithFormat:@""];
                                                        [alert setIcon:[NSImage imageNamed:NSImageNameInfo]];            
                                                        [alert beginSheetModalForWindow:[self.view window] modalDelegate:self didEndSelector:@selector(alertDidEnd:returnCode:contextInfo:) contextInfo:nil];
            
                                                    } else {
            
                                                        // setup successful. let's present go on to the tour.
                                                        self.tourViewController = [[CLTourViewController alloc] initWithNibName:@"CLTourViewController" bundle:nil];
                                                        [[[NSApp mainWindow] contentView] addSubview:self.tourViewController.view];
            
                                                        PushAnimation *animation = [[PushAnimation alloc] initWithDuration:0.25f animationCurve:NSAnimationLinear];
                                                        [animation setNewDirection:RightDirection];
                                                        [animation setStartingView:self.view];
                                                        [animation setDestinationView:self.tourViewController.view];
                                                        [animation setAnimationBlockingMode:NSAnimationNonblocking];
                                                        [animation startAnimation];
            
                                                    }
        
                                                } else {
        
                                                    // remove oursevles from observing an event. (Oh I miss, viewWillAppear, viewWillDispear from UIKit). Sigh...
                                                    [[NSNotificationCenter defaultCenter] removeObserver:self];

                                                    // Move to advanced setup
                                                    self.folderSelectionViewController = [[CLFolderSelectionViewController alloc] initWithNibName:@"CLFolderSelectionViewController" bundle:nil];
                                                    [self.folderSelectionViewController setSetupSelectorViewController:self];
            
                                                    [[[NSApp mainWindow] contentView] addSubview:self.folderSelectionViewController.view];

                                                    PushAnimation *animation = [[PushAnimation alloc] initWithDuration:0.25f animationCurve:NSAnimationLinear];
                                                    [animation setNewDirection:RightDirection];
                                                    [animation setStartingView:self.view];
                                                    [animation setDestinationView:self.folderSelectionViewController.view];
                                                    [animation setAnimationBlockingMode:NSAnimationNonblocking];
                                                    [animation startAnimation];
                                                }

#endif  // end TRASH
                                            }));
            }
        }

        #endregion

        #region "Callbacks"

        /// <summary>
        /// Callback from the View's dialog box.
        /// </summary>
        private void DialogMessageCallback(MessageBoxResult result)
        {
        }

        #endregion


        #region Supporting Functions

        /// <summary>
        /// Send a navigation request.
        /// </summary>
        protected void SendNavigationRequestMessage(Uri uri) 
        {
            Messenger.Default.Send<Uri>(uri, "PageSetupSelector_NavigationRequest");
        }

        #endregion
    }
}