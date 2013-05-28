//
//  PageSelectStorageSizeViewModel.cs
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
using System.Windows.Input;
using System.ComponentModel;
using win_client.AppDelegate;
using Cloud.Support;
using Cloud.Static;
using System.Resources;
using CleanShutdown.Messaging;
using win_client.ViewModelHelpers;
using win_client.Resources;
using Dialog.Abstractions.Wpf.Intefaces;
using CleanShutdown.Helpers;
using System.Windows.Threading;
using CloudApiPrivate.Common;

namespace win_client.ViewModels
{
    /// <summary>
    /// Page to select the Cloud storage size desired by the user.
    /// </summary>
    public class PageSelectStorageSizeViewModel : ValidatingViewModelBase
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private CLTrace _trace = CLTrace.Instance;
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageSelectStorageSizeViewModel class.
        /// </summary>
        public PageSelectStorageSizeViewModel(IDataService dataService)
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

        #region "Bindable Properties"
        /// <summary>
        /// The <see cref="PageSelectStorageSize_SizeSelected" /> property's name.
        /// </summary>
        public const string PageSelectStorageSize_SizeSelectedPropertyName = "PageSelectStorageSize_SizeSelected";
        private CloudApiPrivate.Model.Settings.StorageSizeSelections _pageSelectStorageSize_SizeSelected = (StorageSizeSelections)Settings.Instance.Quota;
        public CloudApiPrivate.Model.Settings.StorageSizeSelections PageSelectStorageSize_SizeSelected
        {
            get
            {
                return _pageSelectStorageSize_SizeSelected;
            }

            set
            {
                if (_pageSelectStorageSize_SizeSelected == (StorageSizeSelections)value)
                {
                    return;
                }

                _pageSelectStorageSize_SizeSelected = (StorageSizeSelections)value;
                Settings.Instance.Quota = (int)_pageSelectStorageSize_SizeSelected;
                RaisePropertyChanged(PageSelectStorageSize_SizeSelectedPropertyName);
            }
        }

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
        /// The user clicked the Continue button on the PageSelectStorageSize page.
        /// </summary>
        private RelayCommand _pageSelectStorageSize_ContinueCommand;
        public RelayCommand PageSelectStorageSize_ContinueCommand
        {
            get
            {
                return _pageSelectStorageSize_ContinueCommand
                    ?? (_pageSelectStorageSize_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                // The user has decided.  Process based on the storage size selection.
                                                switch (_pageSelectStorageSize_SizeSelected)
                                                {
                                                    case StorageSizeSelections.Size5Gb:
                                                        Settings.Instance.setCloudQuota(5);

                                                        Uri nextPage = new System.Uri(CLConstants.kPageSetupSelector, System.UriKind.Relative);
                                                        CLAppMessages.PageSelectStorageSize_NavigationRequest.Send(nextPage);
                                                        break;
                                                    case StorageSizeSelections.Size50Gb:
                                                    case StorageSizeSelections.Size500Gb:
                                                        // TODO: We need to collect credit card info.  
                                                        // Not implemented, put up a dialog.
                                                        ToDoNeedCreditCardInfo();
                                                        break;
                                                }
                                            }));
            }
        }

        /// <summary>
        /// The user clicked the "Pricing terms" hyperlink.
        /// </summary>
        private ICommand _pageSelectStorageSize_PricingTermsCommand;
        public ICommand PageSelectStorageSize_PricingTermsCommand
        {
            get
            {
                return _pageSelectStorageSize_PricingTermsCommand
                    ?? (_pageSelectStorageSize_PricingTermsCommand = new RelayCommand(
                                          () =>
                                          {
                                              CLShortcuts.StartBrowserToUrl(CLConstants.kUrlCloudCom);
                                          }));
            }
        }



        /// <summary>
        /// The user clicked the area over the 5Gb RadioButton on the PageSelectStorageSize page.
        /// </summary>
        private RelayCommand _pageSelectStorageSize_5GbAreaCommand;
        public RelayCommand PageSelectStorageSize_5GbAreaCommand
        {
            get
            {
                return _pageSelectStorageSize_5GbAreaCommand
                    ?? (_pageSelectStorageSize_5GbAreaCommand = new RelayCommand(
                                            () =>
                                            {
                                                PageSelectStorageSize_SizeSelected = StorageSizeSelections.Size5Gb;
                                                CLAppMessages.Message_PageSelectStorageSizeViewSetFocusToContinueButton.Send("");
                                            }));
            }
        }

        /// <summary>
        /// The user clicked the area over the 50Gb RadioButton on the PageSelectStorageSize page.
        /// </summary>
        private RelayCommand _pageSelectStorageSize_50GbAreaCommand;
        public RelayCommand PageSelectStorageSize_50GbAreaCommand
        {
            get
            {
                return _pageSelectStorageSize_50GbAreaCommand
                    ?? (_pageSelectStorageSize_50GbAreaCommand = new RelayCommand(
                                            () =>
                                            {
                                                PageSelectStorageSize_SizeSelected = StorageSizeSelections.Size50Gb;
                                                CLAppMessages.Message_PageSelectStorageSizeViewSetFocusToContinueButton.Send("");
                                            }));
            }
        }

        /// <summary>
        /// The user clicked the area over the 500Gb RadioButton on the PageSelectStorageSize page.
        /// </summary>
        private RelayCommand _pageSelectStorageSize_500GbAreaCommand;
        public RelayCommand PageSelectStorageSize_500GbAreaCommand
        {
            get
            {
                return _pageSelectStorageSize_500GbAreaCommand
                    ?? (_pageSelectStorageSize_500GbAreaCommand = new RelayCommand(
                                            () =>
                                            {
                                                PageSelectStorageSize_SizeSelected = StorageSizeSelections.Size500Gb;
                                                CLAppMessages.Message_PageSelectStorageSizeViewSetFocusToContinueButton.Send("");
                                            }));
            }
        }

        /// <summary>
        /// The page was navigated to.
        /// </summary>
        private RelayCommand _pageSelectStorageSize_NavigatedToCommand;
        public RelayCommand PageSelectStorageSize_NavigatedToCommand
        {
            get
            {
                return _pageSelectStorageSize_NavigatedToCommand
                    ?? (_pageSelectStorageSize_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                PageSelectStorageSize_SizeSelected = (StorageSizeSelections)Settings.Instance.Quota;
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

        #region "Callbacks"

        /// <summary>
        /// Callback from the View's dialog box.
        /// </summary>
        private void DialogMessageCallback(MessageBoxResult result)
        {
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

        #region Supporting Functions

        /// <summary>
        /// Put up a dialog.  We need credit card info here.  TODO:
        /// </summary>
        //TODO: Implement this.
        private void ToDoNeedCreditCardInfo() 
        {
            // Put up a dummy message
            IModalWindow dialog = null;
            CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                errorMessage: "This storage selection is not available for beta users.",
                title: "Information",
                headerText: "Not available.",
                rightButtonContent: Resources.Resources.generalOkButtonContent,
                rightButtonIsDefault: true,
                rightButtonIsCancel: true,
                container: ViewGridContainer,
                dialog: out dialog,
                actionOkButtonHandler:
                  returnedViewModelInstance =>
                  {
                      // Do nothing here when the user clicks the OK button.
                  }
            );
        }

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
                _trace.writeToLog(9, "PageSelectStorageSizeViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult == true)
                {
                    // The user said yes.
                    _trace.writeToLog(9, "PageSelectStorageSizeViewModel: Prompt exit application: User said yes.");

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