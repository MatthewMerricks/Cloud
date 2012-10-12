//
//  FramePreferencesAccountViewModel.cs
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
using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Resources;
using win_client.AppDelegate;
using win_client.ViewModelHelpers;
using win_client.Resources;
using System.Windows.Input;
using System.ComponentModel;
using CleanShutdown.Messaging;
using CleanShutdown.Helpers;
using CloudApiPrivate.Common;
using System.Diagnostics;

namespace win_client.ViewModels
{
         
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class FramePreferencesAccountViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private readonly IDataService _dataService;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private CLTrace _trace = CLTrace.Instance;

        #endregion

        #region Life Cycle

        /// <summary>
        /// Initializes a new instance of the FramePreferencesAccountViewModel class.
        /// </summary>
        public FramePreferencesAccountViewModel(IDataService dataService)
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

        #region Bindable Properties

        /// <summary>
        /// The <see cref="ViewGridContainer" /> property's name.
        /// </summary>
        public const string ViewGridContainerPropertyName = "ViewGridContainer";
        private Grid _viewGridContainer = null;

        /// <summary>
        /// Sets and gets the ViewGridContainer property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
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
        /// Gets the FramePreferencesAccount_UnlinkThisComputerCommand.
        /// </summary>
        private ICommand _framePreferencesAccount_GetMoreSpaceCommand;
        public ICommand FramePreferencesAccount_GetMoreSpaceCommand
        {
            get
            {
                return _framePreferencesAccount_GetMoreSpaceCommand
                    ?? (_framePreferencesAccount_GetMoreSpaceCommand = new RelayCommand(
                                            () =>
                                            {
                                                //TODO: Actually handle a request for more space.
                                                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                                                    errorMessage: "You can't select your synced folders right now.  It's not implemented yet..",
                                                    title: "Information",
                                                    headerText: "Not implemented!",
                                                    rightButtonContent: Resources.Resources.generalOkButtonContent,
                                                    rightButtonIsDefault: true,
                                                    rightButtonIsCancel: true,
                                                    container: ViewGridContainer,
                                                    dialog: out _dialog,
                                                    actionOkButtonHandler:
                                                      returnedViewModelInstance =>
                                                      {
                                                          // Do nothing here when the user clicks the OK button.
                                                      }
                                                );
                                            }));
            }
        }

        #endregion

        #region Support Functions

        #endregion

    }
}
