﻿//
//  PageTourAdvancedEndViewModel.cs
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
using Cloud.Support;
using System.Windows.Input;
using System.ComponentModel;
using CleanShutdown.Messaging;
using win_client.ViewModelHelpers;
using win_client.Resources;
using CleanShutdown.Helpers;
using System.Windows.Threading;
using System.Diagnostics;
using Cloud.Model;
using Cloud.Static;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageTourAdvancedEndViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageTourAdvancedEndViewModel class.
        /// </summary>
        public PageTourAdvancedEndViewModel(IDataService dataService)
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

                    PageTourAdvancedEnd_OpenCloudFolderCommand = true;
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
        /// The <see cref="PageTourAdvancedEnd_OpenCloudFolderCommand" /> property's name.
        /// </summary>
        public const string PageTourAdvancedEnd_OpenCloudFolderCommandPropertyName = "PageTourAdvancedEnd_OpenCloudFolderCommand";
        private bool _pageTourAdvancedEnd_OpenCloudFolderCommand = false;
        public bool PageTourAdvancedEnd_OpenCloudFolderCommand
        {
            get
            {
                return _pageTourAdvancedEnd_OpenCloudFolderCommand;
            }

            set
            {
                if (_pageTourAdvancedEnd_OpenCloudFolderCommand == value)
                {
                    return;
                }

                _pageTourAdvancedEnd_OpenCloudFolderCommand = value;
                RaisePropertyChanged(PageTourAdvancedEnd_OpenCloudFolderCommandPropertyName);
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
        /// The user clicked has selected a choice and will continue.
        /// </summary>
        private RelayCommand _PageTourAdvancedEnd_ContinueCommand;
        public RelayCommand PageTourAdvancedEnd_ContinueCommand
        {
            get
            {
                return _PageTourAdvancedEnd_ContinueCommand
                    ?? (_PageTourAdvancedEnd_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Show the Cloud folder with a delay
                                                if (_pageTourAdvancedEnd_OpenCloudFolderCommand)
                                                {
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
                                                CLAppMessages.PageTourAdvancedEnd_NavigationRequest.Send(nextPage);
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
                _trace.writeToLog(9, "PageTourAdvancedEndViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult == true)
                {
                    // The user said yes.
                    _trace.writeToLog(9, "PageTourAdvancedEndViewModel: Prompt exit application: User said yes.");

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