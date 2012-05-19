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


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class PageTourViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private RelayCommand _PageTour_BackCommand;
        private RelayCommand _PageTour_ContinueCommand;
        private ResourceManager _rm;

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
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

        #region "Bindable Properties"

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
        #endregion 

      
        #region Commands

        /// <summary>
        /// The user clicked the back button.
        /// </summary>
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
                                                    nextPageName = CLConstants.kPageInvisible;
                                                }
                                                else
                                                {
                                                    nextPageName = string.Format(@"{0}{1}{2}", CLConstants.kPageTour, _tourPageNumber.ToString(), CLConstants.kXamlSuffix);
                                                }

                                                // Go to that page
                                                Uri nextPage = new System.Uri(nextPageName, System.UriKind.Relative);
                                                SendNavigationRequestMessage(nextPage);
                                            }));
            }
        }
        
        /// <summary>
        /// The user clicked has selected a choice and will continue.
        /// </summary>
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
                                                    nextPageName = CLConstants.kPageInvisible;
                                                }
                                                else
                                                {
                                                    nextPageName = string.Format(@"{0}{1}{2}", CLConstants.kPageTour, _tourPageNumber.ToString(), CLConstants.kXamlSuffix);
                                                }

                                                // Go to that page
                                                Uri nextPage = new System.Uri(nextPageName, System.UriKind.Relative);
                                                SendNavigationRequestMessage(nextPage);
                                            }));                                              
            }
        }

        #endregion

        #region Supporting Functions

        /// <summary>
        /// Send a navigation request.
        /// </summary>
        protected void SendNavigationRequestMessage(Uri uri) 
        {
            Messenger.Default.Send<Uri>(uri, "PageTour_NavigationRequest");
        }

        #endregion
    }
}