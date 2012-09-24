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
using win_client.Resources;
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
    public class FramePreferencesNetworkViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs

        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the FramePreferencesNetworkViewModel class.
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
      
        #region Relay Commands

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
                                                _dialog = new DialogPreferencesNetworkBandwidth();
                                                IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
                                                modalDialogService.ShowDialog(
                                                            this._dialog,
                                                            new DialogPreferencesNetworkBandwidthViewModel
                                                            {
                                                                DialogPreferencesNetworkBandwidth_Preferences = this.Preferences,
                                                                DialogPreferencesNetworkBandwidth_Title = Resources.Resources.DialogPreferencesNetworkBandwidthTitle,
                                                                DialogPreferencesNetworkBandwidth_WindowWidth = 504,
                                                                DialogPreferencesNetworkBandwidth_WindowHeight = 325,
                                                                DialogPreferencesNetworkBandwidth_LeftButtonWidth = 75,
                                                                DialogPreferencesNetworkBandwidth_LeftButtonMargin = new Thickness(0, 0, 10, 0),
                                                                DialogPreferencesNetworkBandwidth_LeftButtonContent = Resources.Resources.generalOkButtonContent,
                                                                DialogPreferencesNetworkBandwidth_LeftButtonVisibility = Visibility.Visible,
                                                                DialogPreferencesNetworkBandwidth_LeftButtonIsDefault = true,
                                                                DialogPreferencesNetworkBandwidth_LeftButtonIsCancel = false,
                                                                DialogPreferencesNetworkBandwidth_RightButtonWidth = 75,
                                                                DialogPreferencesNetworkBandwidth_RightButtonMargin = new Thickness(0, 0, 0, 0),
                                                                DialogPreferencesNetworkBandwidth_RightButtonContent = Resources.Resources.generalCancelButtonContent,
                                                                DialogPreferencesNetworkBandwidth_RightButtonVisibility = Visibility.Visible,
                                                                DialogPreferencesNetworkBandwidth_RightButtonIsDefault = false,
                                                                DialogPreferencesNetworkBandwidth_RightButtonIsCancel = true,
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
                                                _dialog = new DialogPreferencesNetworkProxies();
                                                IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
                                                modalDialogService.ShowDialog(
                                                            this._dialog,
                                                            new DialogPreferencesNetworkProxiesViewModel
                                                            {
                                                                DialogPreferencesNetworkProxies_Preferences = this.Preferences,
                                                                DialogPreferencesNetworkProxies_Title = Resources.Resources.DialogPreferencesNetworkProxiesTitle,
                                                                DialogPreferencesNetworkProxies_WindowWidth = 504,
                                                                DialogPreferencesNetworkProxies_WindowHeight = 386,
                                                                DialogPreferencesNetworkProxies_LeftButtonWidth = 75,
                                                                DialogPreferencesNetworkProxies_LeftButtonMargin = new Thickness(0, 0, 10, 0),
                                                                DialogPreferencesNetworkProxies_LeftButtonContent = Resources.Resources.generalOkButtonContent,
                                                                DialogPreferencesNetworkProxies_LeftButtonVisibility = Visibility.Visible,
                                                                DialogPreferencesNetworkProxies_LeftButtonIsDefault = true,
                                                                DialogPreferencesNetworkProxies_LeftButtonIsCancel = false,
                                                                DialogPreferencesNetworkProxies_RightButtonWidth = 75,
                                                                DialogPreferencesNetworkProxies_RightButtonMargin = new Thickness(0, 0, 0, 0),
                                                                DialogPreferencesNetworkProxies_RightButtonContent = Resources.Resources.generalCancelButtonContent,
                                                                DialogPreferencesNetworkProxies_RightButtonVisibility = Visibility.Visible,
                                                                DialogPreferencesNetworkProxies_RightButtonIsDefault = false,
                                                                DialogPreferencesNetworkProxies_RightButtonIsCancel = true,
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

        #region Support Functions

        #endregion
    }
}
