//
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
    public class PageSetupSelectorViewModel : ValidatingViewModelBase
    {

        #region Instance Variables
        private readonly IDataService _dataService;

        private RelayCommand _pageSetupSelector_NavigatedToCommand;        
        private RelayCommand _pageSetupSelector_BackCommand;
        private RelayCommand _pageSetupSelector_ContinueCommand;
        private RelayCommand _pageSetupSelector_DefaultAreaCommand;
        private RelayCommand _pageSetupSelector_AdvancedAreaCommand;
        #endregion

        #region "Bindable Properties"
        /// <summary>
        /// The <see cref="PageSetupSelector_OptionSelected" /> property's name.
        /// </summary>
        public const string PageSetupSelector_OptionSelectedPropertyName = "PageSetupSelector_OptionSelected";

        private SetupSelectorOptions _pageSetupSelector_OptionSelected = Settings.Instance.UseDefaultSetup ?
                                                    SetupSelectorOptions.OptionDefault : SetupSelectorOptions.OptionAdvanced;

        /// <summary>
        /// Sets and gets the PageSetupSelector_OptionSelected property.  This is the setup option
        /// chosen by the user. Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
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
                                                PageSetupSelector_OptionSelected = Settings.Instance.UseDefaultSetup ? 
                                                    SetupSelectorOptions.OptionDefault : SetupSelectorOptions.OptionAdvanced;
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

        /// <summary>
        /// The user clicked the area over the Default radio button.
        /// </summary>
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