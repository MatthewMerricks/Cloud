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
using System.Windows.Input;
using System.ComponentModel;
using win_client.AppDelegate;
using CloudApiPublic.Support;
using System.Resources;
using CleanShutdown.Messaging;
using win_client.ViewModelHelpers;

namespace win_client.ViewModels
{
    #region "Definitions"
    public enum StorageSizeSelections
    {
        Size5Gb = 5,
        Size50Gb = 50,
        Size500Gb = 500,
    }
    #endregion
         
    /// <summary>
    /// Page to select the Cloud storage size desired by the user.
    /// </summary>
    public class PageSelectStorageSizeViewModel : ValidatingViewModelBase
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private ResourceManager _rm;
        private CLTrace _trace = CLTrace.Instance;

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
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
            _rm = CLAppDelegate.Instance.ResourceManager;
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

        #region "Bindable Properties"
        /// <summary>
        /// The <see cref="PageSelectStorageSize_SizeSelected" /> property's name.
        /// </summary>
        public const string PageSelectStorageSize_SizeSelectedPropertyName = "PageSelectStorageSize_SizeSelected";

        private StorageSizeSelections _pageSelectStorageSize_SizeSelected = (StorageSizeSelections)Settings.Instance.Quota;

        /// <summary>
        /// Sets and gets the PageSelectStorageSize_SizeSelected property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public StorageSizeSelections PageSelectStorageSize_SizeSelected
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
        /// Put up a dialog.  We need credit card info here.  TODO:
        /// </summary>
        //TODO: Implement this.
        private void ToDoNeedCreditCardInfo() 
        {
            var message = new DialogMessage("This storage selection is not available for beta users.", DialogMessageCallback)
            {
                Button = MessageBoxButton.OK,
                Caption = "Not Available"
            };

            CLAppMessages.SelectStorageSize_PresentMessageDialog.Send(message);
        }

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

            // The Register/Login window is closing.  Warn the user and allow him to cancel the close.
            CLModalMessageBoxDialogs.Instance.DisplayModalShutdownPrompt(container: ViewGridContainer);

            return true;                // cancel the automatic Window close.
        }

        #endregion
    }
}