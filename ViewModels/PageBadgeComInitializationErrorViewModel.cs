//
//  PageBadgeComInitializationErrorViewModel.cs
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
using CloudApiPublic.Support;
using System.Windows.Input;
using System.ComponentModel;
using CleanShutdown.Messaging;
using win_client.ViewModelHelpers;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageBadgeComInitializationErrorViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private RelayCommand _pageBadgeComInitializationErrorViewModel_OkCommand;
        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageBadgeComInitializationErrorViewModel(IDataService dataService)
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
        }

        #endregion

        #region Bindable Properties

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
        /// The user clicked the OK button.
        /// </summary>
        public RelayCommand PageBadgeComInitializationErrorViewModel_OkCommand
        {
            get
            {
                return _pageBadgeComInitializationErrorViewModel_OkCommand
                    ?? (_pageBadgeComInitializationErrorViewModel_OkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Exit the application
                                                Application.Current.Shutdown();
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

            // Always allow shutdown on this dialog.
            _isShuttingDown = true;
            return true;
        }

        #endregion
    }
}