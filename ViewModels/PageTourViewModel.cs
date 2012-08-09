//
//  PageTourViewModel.cs
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
using System.Windows.Input;
using System.ComponentModel;
using CleanShutdown.Messaging;
using win_client.ViewModelHelpers;
using win_client.Resources;
using CleanShutdown.Helpers;
using System.Windows.Threading;
using System.Diagnostics;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageTourViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageTourViewModel class.
        /// </summary>
        public PageTourViewModel(IDataService dataService)
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
                    PageTour_OpenCloudFolderCommand = true;
                });

            _pageTour_GreetingText = String.Format(Resources.Resources.tourPage1Greeting, Settings.Instance.UserName.Split(CLConstants.kDelimiterChars)[0]);
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
        /// The <see cref="PageTour_OpenCloudFolderCommand" /> property's name.
        /// </summary>
        public const string PageTour_OpenCloudFolderCommandPropertyName = "PageTour_OpenCloudFolderCommand";
        private bool _pageTour_OpenCloudFolderCommand = false;
        public bool PageTour_OpenCloudFolderCommand
        {
            get
            {
                return _pageTour_OpenCloudFolderCommand;
            }

            set
            {
                if (_pageTour_OpenCloudFolderCommand == value)
                {
                    return;
                }

                _pageTour_OpenCloudFolderCommand = value;
                RaisePropertyChanged(PageTour_OpenCloudFolderCommandPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="PageTour_GreetingText" /> property's name.
        /// </summary>
        public const string PageTour_GreetingTextPropertyName = "PageTour_GreetingText";

        private string _pageTour_GreetingText = "";

        /// <summary>
        /// Sets and gets the PageTour_GreetingText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string PageTour_GreetingText
        {
            get
            {
                return _pageTour_GreetingText;
            }

            set
            {
                if (_pageTour_GreetingText == value)
                {
                    return;
                }

                _pageTour_GreetingText = value;
                RaisePropertyChanged(PageTour_GreetingTextPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TourPageNumber" /> property's name.
        /// </summary>
        public const string TourPageNumberPropertyName = "TourPageNumber";

        private int _tourPageNumber = 1;

        /// <summary>
        /// Sets and gets the TourPageNumber property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int TourPageNumber
        {
            get
            {
                return _tourPageNumber;
            }

            set
            {
                if (_tourPageNumber == value)
                {
                    return;
                }

                _tourPageNumber = value;
                RaisePropertyChanged(TourPageNumberPropertyName);
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
        /// The user clicked the back button.
        /// </summary>
        private RelayCommand _PageTour_BackCommand;
        public RelayCommand PageTour_BackCommand
        {
            get
            {
                return _PageTour_BackCommand
                    ?? (_PageTour_BackCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Choose the next page
                                                string nextPageName;
                                                _tourPageNumber--;
                                                RaisePropertyChanged(TourPageNumberPropertyName);
                                                if (_tourPageNumber <= 0)
                                                {
                                                    // Navigate to the PageInvisible page.  This will start the core services.
                                                    Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                                    CLAppMessages.PageTour_NavigationRequest.Send(nextPage);
                                                }
                                                else
                                                {
                                                    // Go to the next page
                                                    nextPageName = string.Format("{0}{1}{2}", CLConstants.kPageTour, _tourPageNumber.ToString(), CLConstants.kXamlSuffix);
                                                    Uri nextPage = new System.Uri(nextPageName, System.UriKind.Relative);
                                                    CLAppMessages.PageTour_NavigationRequest.Send(nextPage);
                                                }

                                            }));
            }
        }

        /// <summary>
        /// The user clicked has selected a choice and will continue.
        /// </summary>
        private RelayCommand _PageTour_ContinueCommand;
        public RelayCommand PageTour_ContinueCommand
        {
            get
            {
                return _PageTour_ContinueCommand
                    ?? (_PageTour_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Choose the next page
                                                string nextPageName;
                                                _tourPageNumber++;
                                                RaisePropertyChanged(TourPageNumberPropertyName);
                                                if (_tourPageNumber > 5)
                                                {
                                                    if (_pageTour_OpenCloudFolderCommand)
                                                    {
                                                        // Show the Cloud folder with a delay
                                                        var dispatcher = CLAppDelegate.Instance.MainDispatcher;
                                                        dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(1500), () =>
                                                        {
                                                            Process proc = new Process();
                                                            proc.StartInfo.UseShellExecute = true;
                                                            proc.StartInfo.FileName = Settings.Instance.CloudFolderPath;
                                                            proc.Start();
                                                        });
                                                    }

                                                    // Navigate to the PageInvisible page.  This will start the core services.
                                                    Uri nextPage = new System.Uri(CLConstants.kPageInvisible, System.UriKind.Relative);
                                                    CLAppMessages.PageTour_NavigationRequest.Send(nextPage);
                                                }
                                                else
                                                {
                                                    // Go to the next page
                                                    nextPageName = string.Format("{0}{1}{2}", CLConstants.kPageTour, _tourPageNumber.ToString(), CLConstants.kXamlSuffix);
                                                    Uri nextPage = new System.Uri(nextPageName, System.UriKind.Relative);
                                                    CLAppMessages.PageTour_NavigationRequest.Send(nextPage);
                                                }

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
                _trace.writeToLog(9, "PageTourViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                {
                    // The user said yes.  Unlink this device.
                    _trace.writeToLog(9, "PageTourViewModel: Prompt exit application: User said yes.");

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