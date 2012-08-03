//
//  DialogPreferencesNetworkBandwidthViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Dialog.Implementors.Wpf.MVVM;
using win_client.ViewModels;
using win_client.Model;
using System.Windows;
using CloudApiPrivate.Model.Settings;
using win_client.Common;
using CloudApiPrivate.Static;
using System.Windows.Controls;
using CloudApiPrivate.Common;
using System.ComponentModel;
using CloudApiPublic.Support;
using System.Resources;
using win_client.AppDelegate;
using win_client.ViewModelHelpers;
using Dialog.Abstractions.Wpf.Intefaces;
using System;
using GalaSoft.MvvmLight.Messaging;

namespace win_client.ViewModels
{
    public class DialogPreferencesNetworkBandwidthViewModel : ValidatingViewModelBase
    {

        #region BandwidthPreferencesSubset Class

        [Serializable]
        private class BandwidthPreferencesSubset
        {
            public bool ShouldLimitDownloadSpeed { get; set; }
            public int DownloadSpeedLimitKBPerSecond { get; set; }
            public uploadSpeedLimitType UploadSpeeedLimitType { get; set; }
            public int UploadSpeedLimitKBPerSecond { get; set; }
        }

        #endregion

        #region Private Instance Variables

        private const double _kNotActiveOpacity = 0.60;
        private const double _kActiveOpacity = 1.00;

        private ResourceManager _rm;
        private CLTrace _trace = CLTrace.Instance;

        private BandwidthPreferencesSubset _bandwidthPreferencesSubset;             // the current values
        private BandwidthPreferencesSubset _originalBandwidthPreferencesSubset;     // the original values

        private bool _windowClosingImmediately = false;                             // used to allow the window to close immediately

        IModalWindow _dialog = null;

        #endregion

        #region Constructors

        public DialogPreferencesNetworkBandwidthViewModel()
        {
            _rm = CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;
        }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// The <see cref="ViewLayoutRoot" /> property's name.
        /// </summary>
        public const string ViewLayoutRootPropertyName = "ViewLayoutRoot";
        private Grid _viewLayoutRoot = null;
        public Grid ViewLayoutRoot
        {
            get
            {
                return _viewLayoutRoot;
            }                               

            set
            {
                if (_viewLayoutRoot == value)
                {
                    return;
                }

                _viewLayoutRoot = value;
                RaisePropertyChanged(ViewLayoutRootPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_Preferences" /> property's name.
        /// This is set by FramePreferencsNetworkViewModel at instantiation.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_PreferencesPropertyName = "DialogPreferencesNetworkBandwidth_Preferences";
        private CLPreferences _DialogPreferencesNetworkBandwidth_Preferences = null;
        public CLPreferences DialogPreferencesNetworkBandwidth_Preferences
        {
            get
            {
                return _DialogPreferencesNetworkBandwidth_Preferences;
            }

            set
            {
                if (_DialogPreferencesNetworkBandwidth_Preferences == value)
                {
                    return;
                }

                // Set the passed in preferences to a property.
                _DialogPreferencesNetworkBandwidth_Preferences = value;

            }
        }

        /// <summary>
        /// The <see cref="RbDownloadBandwidthNoLimit" /> property's name.
        /// </summary>
        public const string RbDownloadBandwidthNoLimitPropertyName = "RbDownloadBandwidthNoLimit";
        private bool _rbDownloadBandwidthNoLimit = false;
        public bool RbDownloadBandwidthNoLimit
        {
            get
            {
                return _rbDownloadBandwidthNoLimit;
            }

            set
            {
                if (_rbDownloadBandwidthNoLimit != value)
                {
                    if (value)
                    {
                        RbDownloadBandwidthLimit = false;
                        DownloadRateNotLimitedOpacity = _kNotActiveOpacity;          // dim the controls that won't be used
                        TbDownloadBandwidthLimitEnabled = false;
                    }
                    else
                    {
                        DownloadRateNotLimitedOpacity = _kActiveOpacity;
                        TbDownloadBandwidthLimitEnabled = true;
                    }
                    _rbDownloadBandwidthNoLimit = value;
                    CheckValidation();
                    RaisePropertyChanged(RbDownloadBandwidthNoLimitPropertyName);
                }
            }
        }

        /// <summary>
        /// The <see cref="RbDownloadBandwidthLimit" /> property's name.
        /// </summary>
        public const string RbDownloadBandwidthLimitPropertyName = "RbDownloadBandwidthLimit";
        private bool _rbDownloadBandwidthLimit = false;
        public bool RbDownloadBandwidthLimit
        {
            get
            {
                return _rbDownloadBandwidthLimit;
            }

            set
            {
                if (_rbDownloadBandwidthLimit != value)
                {
                    if (value)
                    {
                        RbDownloadBandwidthNoLimit = false;
                    }
                    _rbDownloadBandwidthLimit = value;
                    CheckValidation();
                    RaisePropertyChanged(RbDownloadBandwidthLimitPropertyName);
                }
            }
        }

        /// <summary>
        /// The <see cref="RbUploadBandwidthNoLimit" /> property's name.
        /// </summary>
        public const string RbUploadBandwidthNoLimitPropertyName = "RbUploadBandwidthNoLimit";
        private bool _rbUploadBandwidthNoLimit = false;
        public bool RbUploadBandwidthNoLimit
        {
            get
            {
                return _rbUploadBandwidthNoLimit;
            }

            set
            {
                if (_rbUploadBandwidthNoLimit == value)
                {
                    return;
                }
                if (value)
                {
                    RbUploadBandwidthLimit = false;
                    RbUploadBandwidthLimitAutomatically = false;
                }

                _rbUploadBandwidthNoLimit = value;
                CheckValidation();
                RaisePropertyChanged(RbUploadBandwidthNoLimitPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="RbUploadBandwidthLimitAutomatically" /> property's name.
        /// </summary>
        public const string RbUploadBandwidthLimitAutomaticallyPropertyName = "RbUploadBandwidthLimitAutomatically";
        private bool _rbUploadBandwidthLimitAutomatically = false;
        public bool RbUploadBandwidthLimitAutomatically
        {
            get
            {
                return _rbUploadBandwidthLimitAutomatically;
            }

            set
            {
                if (_rbUploadBandwidthLimitAutomatically == value)
                {
                    return;
                }
                if (value)
                {
                    RbUploadBandwidthNoLimit = false;
                    RbUploadBandwidthLimit = false;
                }

                _rbUploadBandwidthLimitAutomatically = value;
                CheckValidation();
                RaisePropertyChanged(RbUploadBandwidthLimitAutomaticallyPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="RbUploadBandwidthLimit" /> property's name.
        /// </summary>
        public const string RbUploadBandwidthLimitPropertyName = "RbUploadBandwidthLimit";
        private bool _rbUploadBandwidthLimit = false;
        public bool RbUploadBandwidthLimit
        {
            get
            {
                return _rbUploadBandwidthLimit;
            }

            set
            {
                if (_rbUploadBandwidthLimit == value)
                {
                    return;
                }
                if (value)
                {
                    RbUploadBandwidthNoLimit = false;
                    RbUploadBandwidthLimitAutomatically = false;
                    UploadRateNotLimitedOpacity = _kActiveOpacity;
                    TbUploadBandwidthLimitEnabled = true;
                }
                else
                {
                    UploadRateNotLimitedOpacity = _kNotActiveOpacity;
                    TbUploadBandwidthLimitEnabled = false;
                }

                _rbUploadBandwidthLimit = value;
                CheckValidation();
                RaisePropertyChanged(RbUploadBandwidthLimitPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TbDownloadBandwidthLimitKBPerSecond" /> property's name.
        /// </summary>
        public const string TbDownloadBandwidthLimitKBPerSecondPropertyName = "TbDownloadBandwidthLimitKBPerSecond";
        private string _tbDownloadBandwidthLimitKBPerSecond = string.Empty;
        public string TbDownloadBandwidthLimitKBPerSecond
        {
            get
            {
                return _tbDownloadBandwidthLimitKBPerSecond;
            }

            set
            {
                if (_tbDownloadBandwidthLimitKBPerSecond == value)
                {
                    return;
                }

                _tbDownloadBandwidthLimitKBPerSecond = value;
                CheckValidation();
                RaisePropertyChanged(TbDownloadBandwidthLimitKBPerSecondPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TbUploadBandwidthLimitKBPerSecond" /> property's name.
        /// </summary>
        public const string TbUploadBandwidthLimitKBPerSecondPropertyName = "TbUploadBandwidthLimitKBPerSecond";
        private string _tbUploadBandwidthLimitKBPerSecond = string.Empty;
        public string TbUploadBandwidthLimitKBPerSecond
        {
            get
            {
                return _tbUploadBandwidthLimitKBPerSecond;
            }

            set
            {
                if (_tbUploadBandwidthLimitKBPerSecond == value)
                {
                    return;
                }

                _tbUploadBandwidthLimitKBPerSecond = value;
                CheckValidation();
                RaisePropertyChanged(TbUploadBandwidthLimitKBPerSecondPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TbDownloadBandwidthLimitEnabled" /> property's name.
        /// True when the download bandwidth textbox should be enabled.
        /// </summary>
        public const string TbDownloadBandwidthLimitEnabledPropertyName = "TbDownloadBandwidthLimitEnabled";
        private bool _tbDownloadBandwidthLimitEnabled = true;
        public bool TbDownloadBandwidthLimitEnabled
        {
            get
            {
                return _tbDownloadBandwidthLimitEnabled;
            }

            set
            {
                if (_tbDownloadBandwidthLimitEnabled == value)
                {
                    return;
                }

                _tbDownloadBandwidthLimitEnabled = value;
                RaisePropertyChanged(TbDownloadBandwidthLimitEnabledPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="TbUploadBandwidthLimitEnabled" /> property's name.
        /// True when the upload bandwidth textbox should be enabled.
        /// </summary>
        public const string TbUploadBandwidthLimitEnabledPropertyName = "TbUploadBandwidthLimitEnabled";
        private bool _tbUploadBandwidthLimitEnabled = true;
        public bool TbUploadBandwidthLimitEnabled
        {
            get
            {
                return _tbUploadBandwidthLimitEnabled;
            }

            set
            {
                if (_tbUploadBandwidthLimitEnabled == value)
                {
                    return;
                }

                _tbUploadBandwidthLimitEnabled = value;
                CheckValidation();
                RaisePropertyChanged(TbUploadBandwidthLimitEnabledPropertyName);
            }
        }
        
        /// <summary>
        /// The <see cref="DownloadRateNotLimitedOpacity" /> property's name.
        /// This controls the opacity of the KB/s textblock.
        /// </summary>
        public const string DownloadRateNotLimitedOpacityPropertyName = "DownloadRateNotLimitedOpacity";
        private double _downloadRateNotLimitedOpacity = _kActiveOpacity;
        public double DownloadRateNotLimitedOpacity
        {
            get
            {
                return _downloadRateNotLimitedOpacity;
            }

            set
            {
                if (_downloadRateNotLimitedOpacity == value)
                {
                    return;
                }

                _downloadRateNotLimitedOpacity = value;
                RaisePropertyChanged(DownloadRateNotLimitedOpacityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="UploadRateNotLimitedOpacity" /> property's name.
        /// </summary>
        public const string UploadRateNotLimitedOpacityPropertyName = "UploadRateNotLimitedOpacity";
        private double _uploadRateNotLimitedOpacity = _kActiveOpacity;
        public double UploadRateNotLimitedOpacity
        {
            get
            {
                return _uploadRateNotLimitedOpacity;
            }

            set
            {
                if (_uploadRateNotLimitedOpacity == value)
                {
                    return;
                }

                _uploadRateNotLimitedOpacity = value;
                RaisePropertyChanged(UploadRateNotLimitedOpacityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_Title" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_TitlePropertyName = "DialogPreferencesNetworkBandwidth_Title";
        private string _dialogPreferencesNetworkBandwidth_Title = "";
        public string DialogPreferencesNetworkBandwidth_Title
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_Title;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_Title == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_Title = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_WindowWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_WindowWidthPropertyName = "DialogPreferencesNetworkBandwidth_WindowWidth";
        private int _dialogPreferencesNetworkBandwidth_WindowWidth = 325;
        public int DialogPreferencesNetworkBandwidth_WindowWidth
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_WindowWidth;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_WindowWidth == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_WindowWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_WindowWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_WindowHeight" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_WindowHeightPropertyName = "DialogPreferencesNetworkBandwidth_WindowHeight";
        private int _dialogPreferencesNetworkBandwidth_WindowHeight = 210;
        public int DialogPreferencesNetworkBandwidth_WindowHeight
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_WindowHeight;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_WindowHeight == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_WindowHeight = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_WindowHeightPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_LeftButtonWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_LeftButtonWidthPropertyName = "DialogPreferencesNetworkBandwidth_LeftButtonWidth";
        private double _dialogPreferencesNetworkBandwidth_LeftButtonWidth = 75;
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkBandwidth_LeftButtonWidth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double DialogPreferencesNetworkBandwidth_LeftButtonWidth
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_LeftButtonWidth;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_LeftButtonWidth == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_LeftButtonWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_LeftButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_LeftButtonMargin" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_LeftButtonMarginPropertyName = "DialogPreferencesNetworkBandwidth_LeftButtonMargin";
        private Thickness _dialogPreferencesNetworkBandwidth_LeftButtonMargin = new Thickness(0, 0, 0, 0);
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkBandwidth_LeftButtonMargin property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Thickness DialogPreferencesNetworkBandwidth_LeftButtonMargin
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_LeftButtonMargin;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_LeftButtonMargin == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_LeftButtonMargin = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_LeftButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_LeftButtonContent" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_LeftButtonContentPropertyName = "DialogPreferencesNetworkBandwidth_LeftButtonContent";
        private string _dialogPreferencesNetworkBandwidth_LeftButtonContent = "";
        public string DialogPreferencesNetworkBandwidth_LeftButtonContent
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_LeftButtonContent;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_LeftButtonContent == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_LeftButtonContent = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_LeftButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_LeftButtonVisibility" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_LeftButtonVisibilityPropertyName = "DialogPreferencesNetworkBandwidth_LeftButtonVisibility";
        private Visibility _dialogPreferencesNetworkBandwidth_LeftButtonVisibility = Visibility.Visible;
        public Visibility DialogPreferencesNetworkBandwidth_LeftButtonVisibility
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_LeftButtonVisibility;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_LeftButtonVisibility == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_LeftButtonVisibility = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_LeftButtonVisibilityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_RightButtonWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_RightButtonWidthPropertyName = "DialogPreferencesNetworkBandwidth_RightButtonWidth";
        private double _dialogPreferencesNetworkBandwidth_RightButtonWidth = 75;
        public double DialogPreferencesNetworkBandwidth_RightButtonWidth
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_RightButtonWidth;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_RightButtonWidth == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_RightButtonWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_RightButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_RightButtonMargin" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_RightButtonMarginPropertyName = "DialogPreferencesNetworkBandwidth_RightButtonMargin";
        private Thickness _dialogPreferencesNetworkBandwidth_RightButtonMargin = new Thickness(0, 0, 0, 0);
        public Thickness DialogPreferencesNetworkBandwidth_RightButtonMargin
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_RightButtonMargin;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_RightButtonMargin == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_RightButtonMargin = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_RightButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_RightButtonContent" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_RightButtonContentPropertyName = "DialogPreferencesNetworkBandwidth_RightButtonContent";
        private string _dialogPreferencesNetworkBandwidth_RightButtonContent = "";
        public string DialogPreferencesNetworkBandwidth_RightButtonContent
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_RightButtonContent;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_RightButtonContent == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_RightButtonContent = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_RightButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkBandwidth_RightButtonVisibility" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkBandwidth_RightButtonVisibilityPropertyName = "DialogPreferencesNetworkBandwidth_RightButtonVisibility";
        private Visibility _dialogPreferencesNetworkBandwidth_RightButtonVisibility = Visibility.Visible;
        public Visibility DialogPreferencesNetworkBandwidth_RightButtonVisibility
        {
            get
            {
                return _dialogPreferencesNetworkBandwidth_RightButtonVisibility;
            }

            set
            {
                if (_dialogPreferencesNetworkBandwidth_RightButtonVisibility == value)
                {
                    return;
                }

                _dialogPreferencesNetworkBandwidth_RightButtonVisibility = value;
                RaisePropertyChanged(DialogPreferencesNetworkBandwidth_RightButtonVisibilityPropertyName);
            }
        }


        #endregion

        #region Relay Commands

        /// <summary>
        /// Gets the WindowClosingCommand.
        /// </summary>
        private ICommand _windowClosingCommand;
        public ICommand WindowClosingCommand
        {
            get
            {
                return _windowClosingCommand
                    ?? (_windowClosingCommand = new RelayCommand<CancelEventArgs>(
                                          (args) =>
                                          {
                                              args.Cancel = ProcessCancelCommand();    // true: cancel the window Close.
                                          }));
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkBandwidthViewModel_UpdateCommand.
        /// </summary>
        private ICommand _dialogPreferencesNetworkBandwidthViewModel_UpdateCommand;
        public ICommand DialogPreferencesNetworkBandwidthViewModel_UpdateCommand
        {
            get
            {
                return _dialogPreferencesNetworkBandwidthViewModel_UpdateCommand
                    ?? (_dialogPreferencesNetworkBandwidthViewModel_UpdateCommand = new RelayCommand(
                                          () =>
                                          {
                                              CheckValidation();
                                              CLExtensionMethods.ForceValidation(_viewLayoutRoot);
                                              if (!HasErrors)
                                              {
                                                  ProcessUpdateCommand();
                                              }
                                              else
                                              {
                                                  CLAppMessages.DialogPreferencesNetworkBandwidth_FocusToError_Message.Send("");
                                              }
                                          }));
            }
        }


        /// <summary>
        /// Gets the DialogPreferencesNetworkBandwidthViewModel_CancelCommand.
        /// </summary>
        private ICommand _dialogPreferencesNetworkBandwidthViewModel_CancelCommand;
        public ICommand DialogPreferencesNetworkBandwidthViewModel_CancelCommand
        {
            get
            {
                return _dialogPreferencesNetworkBandwidthViewModel_CancelCommand
                    ?? (_dialogPreferencesNetworkBandwidthViewModel_CancelCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Handle the cancel
                                              ProcessCancelCommand();
                                          }));
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand.
        /// </summary>
        private ICommand _dialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand;
        public ICommand DialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand
        {
            get
            {
                return _dialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand
                    ?? (_dialogPreferencesNetworkBandwidthViewModel_ViewLoadedCommand = new RelayCommand(
                                          () =>
                                          {
                                              // The view has loaded.  Get the subset of the global preferences that this dialog will use.
                                              _bandwidthPreferencesSubset = GetPreferencesSubsetFromGlobalPreferences(_DialogPreferencesNetworkBandwidth_Preferences);
                                              _originalBandwidthPreferencesSubset = _bandwidthPreferencesSubset.DeepCopy<BandwidthPreferencesSubset>();  // for change comparison

                                              SetPropertiesFromPreferencesSubset(_bandwidthPreferencesSubset);

                                              _windowClosingImmediately = false;                                    // reset this: used to allow the window to close immediately
                                          }));
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check validation of all validated controls.
        /// </summary>
        private void CheckValidation()
        {
            ValidateDownloadRate();
            ValidateUploadRate();
        }


        /// <summary>
        /// Validate the TbDownloadBandwidthLimitKBPerSecond property.
        /// </summary>
        private void ValidateDownloadRate()
        {
            RemoveAllErrorsForPropertyName(TbDownloadBandwidthLimitKBPerSecondPropertyName);
            if (_tbDownloadBandwidthLimitKBPerSecond.Length > 0)
            {
                int rate;
                bool parseSuccessful = int.TryParse(_tbDownloadBandwidthLimitKBPerSecond, out rate);
                if (!parseSuccessful)
                {
                    AddError(TbDownloadBandwidthLimitKBPerSecondPropertyName, "Please enter a non-negative integer less than or equal to 65,535.");
                }
                else
                {
                    if (rate < 1 || rate > 65535)
                    {
                        AddError(TbDownloadBandwidthLimitKBPerSecondPropertyName, "Please enter a non-negative integer less than or equal to 65,535.");
                    }
                }
            }
        }

        /// <summary>
        /// Validate the TbUploadBandwidthLimitKBPerSecond property.
        /// </summary>
        private void ValidateUploadRate()
        {
            RemoveAllErrorsForPropertyName(TbUploadBandwidthLimitKBPerSecondPropertyName);
            if (_tbUploadBandwidthLimitKBPerSecond.Length > 0)
            {
                int rate;
                bool parseSuccessful = int.TryParse(_tbUploadBandwidthLimitKBPerSecond, out rate);
                if (!parseSuccessful)
                {
                    AddError(TbUploadBandwidthLimitKBPerSecondPropertyName, "Please enter a non-negative integer less than or equal to 65,535.");
                }
                else
                {
                    if (rate < 1 || rate > 65535)
                    {
                        AddError(TbUploadBandwidthLimitKBPerSecondPropertyName, "Please enter a non-negative integer less than or equal to 65,535.");
                    }
                }
            }
        }

        #endregion

        #region Support Functions

        /// <summary>
        /// Get the preferences subset for this dialog from the current properties.
        /// <<returns>BandwidthPreferencesSubset: The output preferences subset.</returns>
        /// </summary>
        private void GetPreferencesSubsetFromProperties()
        {
            _bandwidthPreferencesSubset.ShouldLimitDownloadSpeed = !_rbDownloadBandwidthNoLimit;

            if (_rbUploadBandwidthNoLimit)
            {
                _bandwidthPreferencesSubset.UploadSpeeedLimitType = uploadSpeedLimitType.uploadSpeedLimitDontLimit;
            }
            else if (_rbUploadBandwidthLimit)
            {
                _bandwidthPreferencesSubset.UploadSpeeedLimitType = uploadSpeedLimitType.uploadSpeedLimitLimitTo;
            }
            else
            {
                _bandwidthPreferencesSubset.UploadSpeeedLimitType = uploadSpeedLimitType.uploadSpeedLimitAutoLimit;
            }

            _bandwidthPreferencesSubset.DownloadSpeedLimitKBPerSecond = string.IsNullOrWhiteSpace(_tbDownloadBandwidthLimitKBPerSecond) ? 0 : int.Parse(_tbDownloadBandwidthLimitKBPerSecond);
            _bandwidthPreferencesSubset.UploadSpeedLimitKBPerSecond = string.IsNullOrWhiteSpace(_tbUploadBandwidthLimitKBPerSecond) ? 0 : int.Parse(_tbUploadBandwidthLimitKBPerSecond);
        }

        /// <summary>
        /// Set this dialog's properties from the current preferences subset.
        /// <param name="subsetPreferences">The current preferences subset.</param>"/>
        /// <returns>void</returns>
        /// </summary>
        private void SetPropertiesFromPreferencesSubset(BandwidthPreferencesSubset subsetPreferences)
        {
            // Set all of the view-bound properties.
            RbDownloadBandwidthNoLimit = !subsetPreferences.ShouldLimitDownloadSpeed;
            RbDownloadBandwidthLimit = subsetPreferences.ShouldLimitDownloadSpeed;
            RbUploadBandwidthNoLimit = subsetPreferences.UploadSpeeedLimitType == uploadSpeedLimitType.uploadSpeedLimitDontLimit;
            RbUploadBandwidthLimitAutomatically = subsetPreferences.UploadSpeeedLimitType == uploadSpeedLimitType.uploadSpeedLimitAutoLimit;
            RbUploadBandwidthLimit = subsetPreferences.UploadSpeeedLimitType == uploadSpeedLimitType.uploadSpeedLimitLimitTo;
            TbDownloadBandwidthLimitKBPerSecond = subsetPreferences.DownloadSpeedLimitKBPerSecond.ToString();
            TbUploadBandwidthLimitKBPerSecond = subsetPreferences.UploadSpeedLimitKBPerSecond.ToString();

            // Make sure the proper download opacity is set
            if (_rbDownloadBandwidthNoLimit)
            {
                DownloadRateNotLimitedOpacity = _kNotActiveOpacity;
                TbDownloadBandwidthLimitEnabled = false;
            }
            else
            {
                DownloadRateNotLimitedOpacity = _kActiveOpacity;
                TbDownloadBandwidthLimitEnabled = true;
            }

            // Make sure the proper upload opacity is set
            if (_rbUploadBandwidthLimit)
            {
                UploadRateNotLimitedOpacity = _kActiveOpacity;
                TbUploadBandwidthLimitEnabled = true;
            }
            else
            {
                UploadRateNotLimitedOpacity = _kNotActiveOpacity;
                TbUploadBandwidthLimitEnabled = false;
            }
        }

        /// <summary>
        /// Get the preferences subset for this dialog from the global preferences.
        /// <param name="globalPreferences">The intput global preferences.</param>
        /// <<returns>BandwidthPreferencesSubset: The output preferences subset.</returns>
        /// </summary>
        private BandwidthPreferencesSubset GetPreferencesSubsetFromGlobalPreferences(CLPreferences globalPreferences)
        {
            BandwidthPreferencesSubset bandwidthPreferencesSubset = new BandwidthPreferencesSubset()
            {
                ShouldLimitDownloadSpeed = globalPreferences.ShouldLimitDownloadSpeed,
                DownloadSpeedLimitKBPerSecond = globalPreferences.DownloadSpeedLimitKBPerSecond,
                UploadSpeeedLimitType = globalPreferences.UploadSpeeedLimitType,
                UploadSpeedLimitKBPerSecond = globalPreferences.UploadSpeedLimitKBPerSecond,
            };
            return bandwidthPreferencesSubset;
        }


        /// <summary>
        /// Set the preferences subset for this dialog to the global preferences.
        /// <param name="globalPreferences">The output global preferences to read.</param>
        /// <param name="subsetPreferences">The input preferences subset.</param>
        /// <<returns>void.</returns>
        /// </summary>
        private void SetPreferencesSubsetToGlobalPreferences(CLPreferences globalPreferences, BandwidthPreferencesSubset subsetPreferences)
        {
            globalPreferences.ShouldLimitDownloadSpeed = subsetPreferences.ShouldLimitDownloadSpeed;
            globalPreferences.DownloadSpeedLimitKBPerSecond = subsetPreferences.DownloadSpeedLimitKBPerSecond;
            globalPreferences.UploadSpeeedLimitType = subsetPreferences.UploadSpeeedLimitType;
            globalPreferences.UploadSpeedLimitKBPerSecond = subsetPreferences.UploadSpeedLimitKBPerSecond;
        }

        /// <summary>
        /// Commit the preference subset changes to the global preferences.
        /// </summary>
        private void ProcessUpdateCommand()
        {
            // Pull the changes from the properties into the preferences subset
            GetPreferencesSubsetFromProperties();

            // Save the preferences set by the user.
            if (!CLDeepCompare.IsEqual(_bandwidthPreferencesSubset, _originalBandwidthPreferencesSubset))
            {
                SetPreferencesSubsetToGlobalPreferences(_DialogPreferencesNetworkBandwidth_Preferences, _bandwidthPreferencesSubset);
                _originalBandwidthPreferencesSubset = _bandwidthPreferencesSubset.DeepCopy<BandwidthPreferencesSubset>();   // also to the original copy because we saved the changes.
            }
        }

        /// <summary>
        /// The user is requesting to cancel.  He clicked the 'X', or he clicked the Cancel button.
        /// </summary>
        /// <<returns>bool: true: Cancel the Window Close operation.</returns>
        private bool ProcessCancelCommand()
        {
            // Ignore this if we are closing immediately
            if (_windowClosingImmediately)
            {
                return false;           // allow the window to close
            }

            // Pull the changes from the properties into the preferences subset
            GetPreferencesSubsetFromProperties();

            // Check for unsaved changes.
            if (!CLDeepCompare.IsEqual(_bandwidthPreferencesSubset, _originalBandwidthPreferencesSubset))
            {
                // The user is cancelling and there are unsaved changes.  Ask if he wants to save them.
                CLModalMessageBoxDialogs.Instance.DisplayModalSaveChangesPrompt(container: ViewLayoutRoot, dialog: out _dialog, actionResultHandler: returnedViewModelInstance =>
                {
                    _trace.writeToLog(9, "DialogPreferencesNetworkBandwidth: Prompt save changes: Entry.");
                    if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                    {
                        // The user said yes.
                        _trace.writeToLog(9, "DialogPreferencesNetworkBandwidth: Prompt save changes: User said yes.");
                        SetPreferencesSubsetToGlobalPreferences(_DialogPreferencesNetworkBandwidth_Preferences, _bandwidthPreferencesSubset);
                    }

                    // The parent dialog was told to stay open so this dialog could appear over it.  Now we have
                    // to cause the parent dialog to close without any processing because it has all been done.
                    // Send a message to the parent view to tell it to close.  However, that will cause a window closing
                    // event.  We'll set a flag to indicate that we are done, and use that flag to just ignore
                    // the window closing event, so we don't get ourselves in a never-ending UI loop.
                    _windowClosingImmediately = true;
                    CLAppMessages.Message_DialogPreferencesNetworkBandwidthViewShouldClose.Send("");
                });

                return true;    // keep the window open.  The child dialog completion will close this dialog.
            }
            else
            {
                // There are no changes.  Just allow the window to close.
                return false;
            }
        }


        #endregion
    }
}