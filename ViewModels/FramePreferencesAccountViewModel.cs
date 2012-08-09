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
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs

        #endregion

        #region Life Cycle

        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
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
        private ICommand _framePreferencesAccount_UnlinkThisComputerCommand;
        public ICommand FramePreferencesAccount_UnlinkThisComputerCommand
        {
            get
            {
                return _framePreferencesAccount_UnlinkThisComputerCommand
                    ?? (_framePreferencesAccount_UnlinkThisComputerCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Ask the user if it is OK to Unlink, then do it or not.
                                                CLModalMessageBoxDialogs.Instance.DisplayModalMessageBox(
                                                    windowHeight: 250,
                                                    leftButtonWidth: 75,
                                                    rightButtonWidth: 75,
                                                    title: Resources.Resources.FramePreferencesAccount_RemoveThisDevice,
                                                    headerText: Resources.Resources.FramePreferencesAccount_UnlinkThisDevice,
                                                    bodyText: Resources.Resources.FramePreferencesAccount_UnlinkThisDeviceBody,
                                                    leftButtonContent: Resources.Resources.GeneralYesButtonContent,
                                                    leftButtonIsDefault: false,
                                                    leftButtonIsCancel: false,
                                                    rightButtonContent: Resources.Resources.GeneralNoButtonContent,
                                                    rightButtonIsDefault: true,
                                                    rightButtonIsCancel: false,
                                                    container: ViewGridContainer,
                                                    dialog: out _dialog,
                                                    actionResultHandler: 
                                                        returnedViewModelInstance =>
                                                        {
                                                            // Do nothing here when the user clicks the OK button.
                                                            _trace.writeToLog(9, "FramePreferencesAccount: Unlink device: Entry.");
                                                            if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                                            {
                                                                // The user said yes.  Unlink this device.
                                                                _trace.writeToLog(9, "FramePreferencesAccount: Unlink device: User said yes.");
                                                                CLError error = null;
                                                                CLAppDelegate.Instance.UnlinkFromCloudDotCom(out error);
                                                                //TODO: Handle any errors here.

                                                                // Restart ourselves now
                                                                System.Windows.Forms.Application.Restart();
                                                                System.Windows.Application.Current.Shutdown();
                                                            }
                                                            else
                                                            {
                                                                // The user said no.  Do nothing.
                                                            }
                                                        }
                                                );
                                          }));
            }
        }

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
                                                    errorMessage: "You can't get more space right now.  It's not implemented yet..",
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
