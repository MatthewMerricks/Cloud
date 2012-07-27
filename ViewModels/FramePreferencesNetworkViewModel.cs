//
//  FramePreferencesNetworkViewModel.cs
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
using System.Windows.Input;
using win_client.Views;

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
    public class FramePreferencesNetworkViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;
        private IModalWindow _dialog = null;        // for use with modal dialogs

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public FramePreferencesNetworkViewModel(IDataService dataService)
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
            _rm =  CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;
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

        /// <summary>
        /// The <see cref="Preferences" /> property's name.
        /// </summary>
        public const string PreferencesPropertyName = "Preferences";
        private CLPreferences _preferences = null;
        /// <summary>
        /// Sets and gets the Preferences property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public CLPreferences Preferences
        {
            get
            {
                return _preferences;
            }

            set
            {
                if (_preferences == value)
                {
                    return;
                }

                _preferences = value;
                RaisePropertyChanged(PreferencesPropertyName);
            }
        }

        #endregion
      
        #region Commands

        /// <summary>
        /// Gets the FramePreferencesNetwork_ChangeBandwidthSettings.
        /// </summary>
        private ICommand _framePreferencesNetwork_ChangeBandwidthSettings;
        public ICommand FramePreferencesNetwork_ChangeBandwidthSettings
        {
            get
            {
                return _framePreferencesNetwork_ChangeBandwidthSettings
                    ?? (_framePreferencesNetwork_ChangeBandwidthSettings = new RelayCommand(
                                            () =>
                                            {
                                                //TODO: Actually check to see if there are any updates.
                                                //CLModalMessageBoxDialogs.Instance.DisplayModalMessageBox(
                                                //    windowHeight: 250,
                                                //    leftButtonWidth: 75,
                                                //    rightButtonWidth: 75,
                                                //    title: "Remove this Device?",
                                                //    headerText: "Unlink this device from your account?",
                                                //    bodyText: "Do you want to remove this computer from your account?  Other devices in your account will continue to sync your files.",
                                                //    leftButtonContent: "No",
                                                //    rightButtonContent: "Yes",
                                                //    container: ViewGridContainer,
                                                //    dialog: out _dialog,
                                                //    actionResultHandler: 
                                                //        returnedViewModelInstance =>
                                                //        {
                                                //            // Do nothing here when the user clicks the OK button.
                                                //            _trace.writeToLog(9, "FramePreferencesNetwork: Unlink device: Entry.");
                                                //            if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                                //            {
                                                //                // The user said yes.  Unlink this device.
                                                //                _trace.writeToLog(9, "FramePreferencesNetwork: Unlink device: User said yes.");
                                                //                CLError error = null;
                                                //                CLAppDelegate.Instance.UnlinkFromCloudDotCom(out error);
                                                //                //TODO: Handle any errors here.

                                                //                // Restart ourselves now
                                                //                System.Windows.Forms.Application.Restart();
                                                //                System.Windows.Application.Current.Shutdown();
                                                //            }
                                                //            else
                                                //            {
                                                //                // The user said no.  Do nothing.
                                                //            }
                                                //        }
                                                //);
                                          }));
            }
        }

        /// <summary>
        /// Gets the FramePreferencesNetwork_ChangeProxySettings.
        /// </summary>
        private ICommand _framePreferencesNetwork_ChangeProxySettings;
        public ICommand FramePreferencesNetwork_ChangeProxySettings
        {
            get
            {
                return _framePreferencesNetwork_ChangeProxySettings
                    ?? (_framePreferencesNetwork_ChangeProxySettings = new RelayCommand(
                                            () =>
                                            {
                                                _dialog = SimpleIoc.Default.GetInstance<IModalWindow>(CLConstants.kDialogBox_PreferencesNetworkProxies);
                                                IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
                                                modalDialogService.ShowDialog(
                                                            this._dialog,
                                                            new DialogPreferencesNetworkProxiesViewModel
                                                            {
                                                                DialogPreferencesNetworkProxies_Preferences = this.Preferences,
                                                                DialogPreferencesNetworkProxies_Title = "Title",
                                                                DialogPreferencesNetworkProxies_WindowWidth = 450,
                                                                DialogPreferencesNetworkProxies_WindowHeight = 225,
                                                                DialogPreferencesNetworkProxies_LeftButtonWidth = new GridLength(100),
                                                                DialogPreferencesNetworkProxies_LeftButtonMargin = new Thickness(0, 0, 0, 0),
                                                                DialogPreferencesNetworkProxies_LeftButtonContent = "Cancel",
                                                                DialogPreferencesNetworkProxies_RightButtonWidth = new GridLength(100),
                                                                DialogPreferencesNetworkProxies_RightButtonMargin = new Thickness(0, 0, 0, 0),
                                                                DialogPreferencesNetworkProxies_RightButtonContent = "OK",
                                                            },
                                                            this.ViewGridContainer,
                                                            returnedViewModelInstance =>
                                                            {
                                                                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                                                                {
                                                                    // The user said yes.
                                                                }
                                                                else
                                                                {
                                                                    // The user said no.
                                                                }
                                                            }
                                                );
                                            }));
            }
        }

        #endregion
    }
}
