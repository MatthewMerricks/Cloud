//
//  CloudAlreadyRunningViewModel.cs
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
using System.IO;
using System.Resources;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Collections.Generic;
using win_client.Views;
using win_client.AppDelegate;
using CloudApiPublic.Support;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class CloudAlreadyRunningViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private RelayCommand _cloudAlreadyRunningViewModel_OkCommand;
        private ResourceManager _rm;

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public CloudAlreadyRunningViewModel(IDataService dataService)
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
     
        #region Commands

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        public RelayCommand CloudAlreadyRunningViewModel_OkCommand
        {
            get
            {
                return _cloudAlreadyRunningViewModel_OkCommand
                    ?? (_cloudAlreadyRunningViewModel_OkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Exit the application
                                                Application.Current.Shutdown();
                                            }));                                              
            }
        }

        #endregion
    }
}