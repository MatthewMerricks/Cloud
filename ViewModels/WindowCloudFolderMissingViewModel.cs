//
//  WindowCloudFolderMissingViewModel.cs
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
using CloudApiPrivate.Model.Settings;
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
    public class WindowCloudFolderMissingViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private ResourceManager _rm;

        #endregion
        

        #region Bound Properties

        /// <summary>
        /// The <see cref="BodyMessagee" /> property's name.
        /// </summary>
        public const string BodyMessagePropertyName = "BodyMessage";
        private string _bodyMessage = "";

        /// <summary>
        /// Sets and gets the BodyMessage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string BodyMessage
        {
            get
            {
                return _bodyMessage;
            }

            set
            {
                if (_bodyMessage == value)
                {
                    return;
                }

                _bodyMessage = value;
                RaisePropertyChanged(BodyMessagePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="OkButtonContent" /> property's name.
        /// </summary>
        public const string OkButtonContentPropertyName = "OkButtonContent";
        private string _okButtonContent = "";

        /// <summary>
        /// Sets and gets the OkButtonContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string OkButtonContent
        {
            get
            {
                return _okButtonContent;
            }

            set
            {
                if (_okButtonContent == value)
                {
                    return;
                }

                _okButtonContent = value;
                RaisePropertyChanged(OkButtonContentPropertyName);
            }
        }

        
        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public WindowCloudFolderMissingViewModel(IDataService dataService)
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
            BodyMessage = _rm.GetString("windowCloudFolderBodyMesssage");
            OkButtonContent = CLAppDelegate.Instance.WindowCloudFolderMissingOkButtonContent;
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
        private RelayCommand _windowCloudFolderMissingViewModel_OkCommand;
        public RelayCommand WindowCloudFolderMissingViewModel_OkCommand
        {
            get
            {
                return _windowCloudFolderMissingViewModel_OkCommand
                    ?? (_windowCloudFolderMissingViewModel_OkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Process the OK button click.
                                                //&&&&
                                            }));                                              
            }
        }

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        private RelayCommand _windowCloudFolderMissingViewModel_UnlinkCommand;
        public RelayCommand WindowCloudFolderMissingViewModel_UnlinkCommand
        {
            get
            {
                return _windowCloudFolderMissingViewModel_UnlinkCommand
                    ?? (_windowCloudFolderMissingViewModel_UnlinkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // We will unlink this device.  Stop all core services and exit the application
                                                CLAppDelegate.Instance.UnlinkFromCloudDotCom();
                                                Application.Current.Shutdown();
                                            }));                                              
            }
        }

        #endregion
    }
}