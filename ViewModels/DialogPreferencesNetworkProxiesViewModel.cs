//
//  DialogPreferencesNetworkProxiesViewModel.cs
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
using Cloud.Support;
using System.Resources;
using win_client.AppDelegate;
using System.ComponentModel;
using System;
using Dialog.Abstractions.Wpf.Intefaces;
using win_client.ViewModelHelpers;

namespace win_client.ViewModels
{
    public class DialogPreferencesNetworkProxiesViewModel : ValidatingViewModelBase
    {
        #region ProxiesPreferencesSubset Class

        [Serializable]
        private class ProxiesPreferencesSubset
        {
            public useProxySettingType ProxySettingType { get; set; }
            public useProxyTypes ProxyType { get; set; }
            public string ProxyServerAddress { get; set; }
            public int ProxyServerPort { get; set; }
            public bool ProxyServerRequiresPassword { get; set; }
            public string ProxyServerUserName { get; set; }
            public string ProxyServerPassword { get; set; }
        }

        #endregion

        #region Private Instance Variables

        private const double _kNoProxyOpacity = 0.60;
        private const double _kProxyActiveOpacity = 1.00;

        private CLTrace _trace = CLTrace.Instance;

        private ProxiesPreferencesSubset _proxiesPreferencesSubset;             // the current values
        private ProxiesPreferencesSubset _originalProxiesPreferencesSubset;     // the original values

        private bool _windowClosingImmediately = false;                             // used to allow the window to close immediately

        IModalWindow _dialog = null;

        #endregion

        #region Constructors

        public DialogPreferencesNetworkProxiesViewModel()
        {
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
        /// The <see cref="DialogPreferencesNetworkProxies_Preferences" /> property's name.
        /// This is set by FramePreferencsNetworkViewModel at instantiation.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_PreferencesPropertyName = "DialogPreferencesNetworkProxies_Preferences";
        private CLPreferences _dialogPreferencesNetworkProxies_Preferences = null;
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_Preferences property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public CLPreferences DialogPreferencesNetworkProxies_Preferences
        {
            get
            {
                return _dialogPreferencesNetworkProxies_Preferences;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_Preferences == value)
                {
                    return;
                }

                // Set the passed in preferences to a property.
                _dialogPreferencesNetworkProxies_Preferences = value;

            }
        }

        /// <summary>
        /// The <see cref="RbProxySettingsNoProxy" /> property's name.
        /// </summary>
        public const string RbProxySettingsNoProxyPropertyName = "RbProxySettingsNoProxy";
        private bool _rbProxySettingsNoProxy = false;
        public bool RbProxySettingsNoProxy
        {
            get
            {
                return _rbProxySettingsNoProxy;
            }

            set
            {
                if (_rbProxySettingsNoProxy != value)
                {
                    if (value)
                    {
                        RbProxySettingsAutoDetect = false;
                        RbProxySettingsManual = false;
                        CbServerRequiresAPassword = false;
                        ProxyNoProxyOpacity = _kNoProxyOpacity;
                        ProxyNoProxyControlsEnabled = false;
                    }
                    else
                    {
                        ProxyNoProxyOpacity = _kProxyActiveOpacity;
                        ProxyNoProxyControlsEnabled = true;
                    }
                    _rbProxySettingsNoProxy = value;
                    CheckValidation();
                    RaisePropertyChanged(RbProxySettingsNoProxyPropertyName);
                }
            }
        }

        /// <summary>
        /// The <see cref="RbProxySettingsAutoDetect" /> property's name.
        /// </summary>
        public const string RbProxySettingsAutoDetectPropertyName = "RbProxySettingsAutoDetect";
        private bool _rbProxySettingsAutoDetect = false;
        public bool RbProxySettingsAutoDetect
        {
            get
            {
                return _rbProxySettingsAutoDetect;
            }

            set
            {
                if (_rbProxySettingsAutoDetect != value)
                {
                    if (value)
                    {
                        RbProxySettingsNoProxy = false;
                        RbProxySettingsManual = false;
                    }
                    _rbProxySettingsAutoDetect = value;
                    CheckValidation();
                    RaisePropertyChanged(RbProxySettingsAutoDetectPropertyName);
                }
            }
        }

        /// <summary>
        /// The <see cref="RbProxySettingsManual" /> property's name.
        /// </summary>
        public const string RbProxySettingsManualPropertyName = "RbProxySettingsManual";
        private bool _rbProxySettingsManual = false;
        public bool RbProxySettingsManual
        {
            get
            {
                return _rbProxySettingsManual;
            }

            set
            {
                if (_rbProxySettingsManual!= value)
                {
                    if (value)
                    {
                        RbProxySettingsNoProxy = false;
                        RbProxySettingsAutoDetect = false;
                        ProxyManualOpacity = _kProxyActiveOpacity;
                        ProxyManualControlsEnabled = true;
                    }
                    else
                    {
                        ProxyManualOpacity = _kNoProxyOpacity;
                        ProxyManualControlsEnabled = false;
                    }
                    _rbProxySettingsManual = value;
                    CheckValidation();
                    RaisePropertyChanged(RbProxySettingsManualPropertyName);
                }
            }
        }

        /// <summary>
        /// The <see cref="CbProxyType" /> property's name.
        /// </summary>
        public const string CbProxyTypePropertyName = "CbProxyType";
        private useProxyTypes _cbProxyType = useProxyTypes.useProxyHTTP;
        public useProxyTypes CbProxyType
        {
            get
            {
                return _cbProxyType;
            }

            set
            {
                if (_cbProxyType == value)
                {
                    return;
                }

                _cbProxyType = value;
                CheckValidation();
                RaisePropertyChanged(CbProxyTypePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyServerAddress" /> property's name.
        /// </summary>
        public const string ProxyServerAddressPropertyName = "ProxyServerAddress";
        private string _proxyServerAddress = "";
        public string ProxyServerAddress
        {
            get
            {
                return _proxyServerAddress;
            }

            set
            {
                if (_proxyServerAddress == value)
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = string.Empty;
                }

                _proxyServerAddress = value;
                CheckValidation();
                RaisePropertyChanged(ProxyServerAddressPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyServerPort" /> property's name.
        /// </summary>
        public const string ProxyServerPortPropertyName = "ProxyServerPort";
        private string _proxyServerPort = "";
        public string ProxyServerPort
        {
            get
            {
                return _proxyServerPort;
            }

            set
            {
                if (_proxyServerPort == value)
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = string.Empty;
                }

                _proxyServerPort = value;
                CheckValidation();
                RaisePropertyChanged(ProxyServerPortPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CbServerRequiresAPassword" /> property's name.
        /// </summary>
        public const string CbServerRequiresAPasswordPropertyName = "CbServerRequiresAPassword";
        private bool _cbServerRequiresAPassword = false;
        public bool CbServerRequiresAPassword
        {
            get
            {
                return _cbServerRequiresAPassword;
            }

            set
            {
                if (_cbServerRequiresAPassword == value)
                {
                    return;
                }
                if (value)
                {
                    ProxyAuthenticationControlsOpacity = _kProxyActiveOpacity;
                    ProxyAuthenticationControlsEnabled = true;
                }
                else
                {
                    ProxyAuthenticationControlsOpacity = _kNoProxyOpacity;
                    ProxyAuthenticationControlsEnabled = false;
                }

                _cbServerRequiresAPassword = value;
                CheckValidation();
                RaisePropertyChanged(CbServerRequiresAPasswordPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyServerUsername" /> property's name.
        /// </summary>
        public const string ProxyServerUsernamePropertyName = "ProxyServerUsername";
        private string _proxyServerUsername = "";
        public string ProxyServerUsername
        {
            get
            {
                return _proxyServerUsername;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = string.Empty;
                }
                if (_proxyServerUsername == value)
                {
                    return;
                }

                _proxyServerUsername = value;
                CheckValidation();
                RaisePropertyChanged(ProxyServerUsernamePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="Password2" /> clear password.
        /// </summary>
        public const string ProxyServerPassword2PropertyName = "ProxyServerPassword2";
        private string _proxyServerPassword2 = "";
        /// <summary>
        /// Sets the ProxyServerPassword2 property.
        /// This is the clear password. 
        /// </summary>
        public string ProxyServerPassword2
        {
            get
            {
                return "";
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = string.Empty;
                }
                CLAppMessages.DialogPreferencesNetworkProxies_SetClearPasswordField.Send(value);
                _proxyServerPassword2 = value;
            }
        }

        /// <summary>
        /// The <see cref="ProxyServerPassword" /> property's name.
        /// </summary>
        public const string ProxyServerPasswordPropertyName = "ProxyServerPassword";
        private string _proxyServerPassword = "";
        public string ProxyServerPassword
        {

            get
            {
                return _proxyServerPassword;
            }

            set
            {
                // The password is scrambled at this point because we don't want it in the visual tree for
                // Snoop and other tools to see.  We need to get the password in the clear.  However, only the view knows how
                // to get the clear password.  Send the view a message to cause it to set the clear
                // password.  Upon receiving the message, the view will retrieve the password and invoke a
                // public write-only property on this ViewModel object.  The whole process is synchronous, so
                // we will have the clear password when the Send completes.
                CLAppMessages.DialogPreferencesNetworkProxies_GetClearPasswordField.Send("");

                if (_proxyServerPassword == value)
                {
                    return;
                }

                _proxyServerPassword = value;
                CheckValidation();
                RaisePropertyChanged(ProxyServerPasswordPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyManualOpacity" /> property's name.
        /// Set to dim the controls when "No Proxy" is selected.
        /// </summary>
        public const string ProxyManualOpacityPropertyName = "ProxyManualOpacity";
        private double _proxyManualOpacity = 0.0;
        public double ProxyManualOpacity
        {
            get
            {
                return _proxyManualOpacity;
            }

            set
            {
                if (_proxyManualOpacity == value)
                {
                    return;
                }

                _proxyManualOpacity = value;
                RaisePropertyChanged(ProxyManualOpacityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyManualControlsEnabled" /> property's name.
        /// Enables the manual proxy setting controls.
        /// </summary>
        public const string ProxyManualControlsEnabledPropertyName = "ProxyManualControlsEnabled";
        private bool _proxyManualControlsEnabled = false;
        public bool ProxyManualControlsEnabled
        {
            get
            {
                return _proxyManualControlsEnabled;
            }

            set
            {
                if (_proxyManualControlsEnabled == value)
                {
                    return;
                }

                _proxyManualControlsEnabled = value;
                RaisePropertyChanged(ProxyManualControlsEnabledPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyNoProxyOpacity" /> property's name.
        /// Controls the "Server requires password" checkbox.
        /// </summary>
        public const string ProxyNoProxyOpacityPropertyName = "ProxyNoProxyOpacity";
        private double _proxyNoProxyOpacity = _kProxyActiveOpacity;
        public double ProxyNoProxyOpacity
        {
            get
            {
                return _proxyNoProxyOpacity;
            }

            set
            {
                if (_proxyNoProxyOpacity == value)
                {
                    return;
                }

                _proxyNoProxyOpacity = value;
                RaisePropertyChanged(ProxyNoProxyOpacityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyNoProxyControlsEnabled" /> property's name.
        /// Controls the "Server requires password" checkbox.
        /// </summary>
        public const string ProxyNoProxyControlsEnabledPropertyName = "ProxyNoProxyControlsEnabled";
        private bool _proxyNoProxyControlsEnabled = false;
        public bool ProxyNoProxyControlsEnabled
        {
            get
            {
                return _proxyNoProxyControlsEnabled;
            }

            set
            {
                if (_proxyNoProxyControlsEnabled == value)
                {
                    return;
                }

                _proxyNoProxyControlsEnabled = value;
                RaisePropertyChanged(ProxyNoProxyControlsEnabledPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyAuthenticationControlsOpacity" /> property's name.
        /// Changes the proxy authentication control opacity to dim them when the server does not require a password.
        /// </summary>
        public const string ProxyAuthenticationControlsOpacityPropertyName = "ProxyAuthenticationControlsOpacity";
        private double _proxyAuthenticationControlsOpacity = _kProxyActiveOpacity;
        public double ProxyAuthenticationControlsOpacity
        {
            get
            {
                return _proxyAuthenticationControlsOpacity;
            }

            set
            {
                if (_proxyAuthenticationControlsOpacity == value)
                {
                    return;
                }

                _proxyAuthenticationControlsOpacity = value;
                RaisePropertyChanged(ProxyAuthenticationControlsOpacityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProxyAuthenticationControlsEnabled" /> property's name.
        /// Enables the proxy server authentication controls.
        /// </summary>
        public const string ProxyAuthenticationControlsEnabledPropertyName = "ProxyAuthenticationControlsEnabled";
        private bool _proxyAuthenticationControlsEnabled = false;
        public bool ProxyAuthenticationControlsEnabled
        {
            get
            {
                return _proxyAuthenticationControlsEnabled;
            }

            set
            {
                if (_proxyAuthenticationControlsEnabled == value)
                {
                    return;
                }

                _proxyAuthenticationControlsEnabled = value;
                RaisePropertyChanged(ProxyAuthenticationControlsEnabledPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_Title" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_TitlePropertyName = "DialogPreferencesNetworkProxies_Title";
        private string _dialogPreferencesNetworkProxies_Title = "";
        public string DialogPreferencesNetworkProxies_Title
        {
            get
            {
                return _dialogPreferencesNetworkProxies_Title;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_Title == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_Title = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_WindowWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_WindowWidthPropertyName = "DialogPreferencesNetworkProxies_WindowWidth";
        private int _dialogPreferencesNetworkProxies_WindowWidth = 325;
        public int DialogPreferencesNetworkProxies_WindowWidth
        {
            get
            {
                return _dialogPreferencesNetworkProxies_WindowWidth;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_WindowWidth == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_WindowWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_WindowWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_WindowHeight" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_WindowHeightPropertyName = "DialogPreferencesNetworkProxies_WindowHeight";
        private int _dialogPreferencesNetworkProxies_WindowHeight = 210;
        public int DialogPreferencesNetworkProxies_WindowHeight
        {
            get
            {
                return _dialogPreferencesNetworkProxies_WindowHeight;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_WindowHeight == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_WindowHeight = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_WindowHeightPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonWidthPropertyName = "DialogPreferencesNetworkProxies_LeftButtonWidth";
        private double _dialogPreferencesNetworkProxies_LeftButtonWidth = 75;
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_LeftButtonWidth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double DialogPreferencesNetworkProxies_LeftButtonWidth
        {
            get
            {
                return _dialogPreferencesNetworkProxies_LeftButtonWidth;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_LeftButtonWidth == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_LeftButtonWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonMargin" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonMarginPropertyName = "DialogPreferencesNetworkProxies_LeftButtonMargin";
        private Thickness _dialogPreferencesNetworkProxies_LeftButtonMargin = new Thickness(0, 0, 0, 0);
        public Thickness DialogPreferencesNetworkProxies_LeftButtonMargin
        {
            get
            {
                return _dialogPreferencesNetworkProxies_LeftButtonMargin;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_LeftButtonMargin == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_LeftButtonMargin = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonContent" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonContentPropertyName = "DialogPreferencesNetworkProxies_LeftButtonContent";
        private string _dialogPreferencesNetworkProxies_LeftButtonContent = "";
        public string DialogPreferencesNetworkProxies_LeftButtonContent
        {
            get
            {
                return _dialogPreferencesNetworkProxies_LeftButtonContent;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_LeftButtonContent == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_LeftButtonContent = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonVisibility" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonVisibilityPropertyName = "DialogPreferencesNetworkProxies_LeftButtonVisibility";
        private Visibility _dialogPreferencesNetworkProxies_LeftButtonVisibility = Visibility.Visible;
        public Visibility DialogPreferencesNetworkProxies_LeftButtonVisibility
        {
            get
            {
                return _dialogPreferencesNetworkProxies_LeftButtonVisibility;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_LeftButtonVisibility == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_LeftButtonVisibility = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonVisibilityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonIsDefault" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonIsDefaultPropertyName = "DialogPreferencesNetworkProxies_LeftButtonIsDefault";
        private bool _dialogPreferencesNetworkProxies_LeftButtonIsDefault = false;
        public bool DialogPreferencesNetworkProxies_LeftButtonIsDefault
        {
            get
            {
                return _dialogPreferencesNetworkProxies_LeftButtonIsDefault;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_LeftButtonIsDefault == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_LeftButtonIsDefault = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonIsDefaultPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="_dialogPreferencesNetworkProxies_LeftButtonIsCancel" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonIsCancelPropertyName = "DialogPreferencesNetworkProxies_LeftButtonIsCancel";
        private bool _dialogPreferencesNetworkProxies_LeftButtonIsCancel = false;
        public bool DialogPreferencesNetworkProxies_LeftButtonIsCancel
        {
            get
            {
                return _dialogPreferencesNetworkProxies_LeftButtonIsCancel;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_LeftButtonIsCancel == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_LeftButtonIsCancel = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonIsCancelPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonWidthPropertyName = "DialogPreferencesNetworkProxies_RightButtonWidth";
        private double _dialogPreferencesNetworkProxies_RightButtonWidth = 75;
        public double DialogPreferencesNetworkProxies_RightButtonWidth
        {
            get
            {
                return _dialogPreferencesNetworkProxies_RightButtonWidth;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_RightButtonWidth == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_RightButtonWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonMargin" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonMarginPropertyName = "DialogPreferencesNetworkProxies_RightButtonMargin";
        private Thickness _dialogPreferencesNetworkProxies_RightButtonMargin = new Thickness(0, 0, 0, 0);
        public Thickness DialogPreferencesNetworkProxies_RightButtonMargin
        {
            get
            {
                return _dialogPreferencesNetworkProxies_RightButtonMargin;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_RightButtonMargin == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_RightButtonMargin = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonContent" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonContentPropertyName = "DialogPreferencesNetworkProxies_RightButtonContent";
        private string _dialogPreferencesNetworkProxies_RightButtonContent = "";
        public string DialogPreferencesNetworkProxies_RightButtonContent
        {
            get
            {
                return _dialogPreferencesNetworkProxies_RightButtonContent;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_RightButtonContent == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_RightButtonContent = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonVisibility" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonVisibilityPropertyName = "DialogPreferencesNetworkProxies_RightButtonVisibility";
        private Visibility _dialogPreferencesNetworkProxies_RightButtonVisibility = Visibility.Visible;
        public Visibility DialogPreferencesNetworkProxies_RightButtonVisibility
        {
            get
            {
                return _dialogPreferencesNetworkProxies_RightButtonVisibility;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_RightButtonVisibility == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_RightButtonVisibility = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonVisibilityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonIsDefault" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonIsDefaultPropertyName = "DialogPreferencesNetworkProxies_RightButtonIsDefault";
        private bool _dialogPreferencesNetworkProxies_RightButtonIsDefault = false;
        public bool DialogPreferencesNetworkProxies_RightButtonIsDefault
        {
            get
            {
                return _dialogPreferencesNetworkProxies_RightButtonIsDefault;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_RightButtonIsDefault == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_RightButtonIsDefault = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonIsDefaultPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonIsCancel" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonIsCancelPropertyName = "DialogPreferencesNetworkProxies_RightButtonIsCancel";
        private bool _dialogPreferencesNetworkProxies_RightButtonIsCancel = false;
        public bool DialogPreferencesNetworkProxies_RightButtonIsCancel
        {
            get
            {
                return _dialogPreferencesNetworkProxies_RightButtonIsCancel;
            }

            set
            {
                if (_dialogPreferencesNetworkProxies_RightButtonIsCancel == value)
                {
                    return;
                }

                _dialogPreferencesNetworkProxies_RightButtonIsCancel = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonIsCancelPropertyName);
            }
        }

        #endregion

        #region Relay Commands

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

        /// <summary>
        /// Gets the DialogPreferencesNetworkProxiesViewModel_UpdateCommand.
        /// </summary>
        private ICommand _dialogPreferencesNetworkProxiesViewModel_UpdateCommand;
        public ICommand DialogPreferencesNetworkProxiesViewModel_UpdateCommand
        {
            get
            {
                return _dialogPreferencesNetworkProxiesViewModel_UpdateCommand
                    ?? (_dialogPreferencesNetworkProxiesViewModel_UpdateCommand = new RelayCommand(
                                          () =>
                                          {
                                              CheckValidation();
                                              CLExtensionMethods.ForceValidation(_viewLayoutRoot);
                                              if (!HasErrors)
                                              {

                                                  ProcessUpdateCommand();

                                                  // Allow the window to close.
                                                  WindowCloseOk = true;
                                                  _windowClosingImmediately = false;
                                              }
                                              else
                                              {
                                                  CLAppMessages.DialogPreferencesNetworkProxies_FocusToError_Message.Send("");
                                              }
                                          }));
            }
        }


        /// <summary>
        /// Gets the DialogPreferencesNetworkProxiesViewModel_CancelCommand.
        /// </summary>
        private ICommand _dialogPreferencesNetworkProxiesViewModel_CancelCommand;
        public ICommand DialogPreferencesNetworkProxiesViewModel_CancelCommand
        {
            get
            {
                return _dialogPreferencesNetworkProxiesViewModel_CancelCommand
                    ?? (_dialogPreferencesNetworkProxiesViewModel_CancelCommand = new RelayCommand(
                                          () =>
                                          {
                                              // Handle the cancel
                                              WindowCloseOk = !ProcessCancelCommand();
                                          }));
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand.
        /// </summary>
        private ICommand _dialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand;
        public ICommand DialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand
        {
            get
            {
                return _dialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand
                    ?? (_dialogPreferencesNetworkProxiesViewModel_ViewLoadedCommand = new RelayCommand(
                                          () =>
                                          {
                                              // The view has loaded.  Get the subset of the global preferences that this dialog will use.
                                              _proxiesPreferencesSubset = GetPreferencesSubsetFromGlobalPreferences(_dialogPreferencesNetworkProxies_Preferences);
                                              _originalProxiesPreferencesSubset = _proxiesPreferencesSubset.DeepCopy<ProxiesPreferencesSubset>();  // for change comparison

                                              SetPropertiesFromPreferencesSubset(_proxiesPreferencesSubset);

                                              _windowClosingImmediately = false;                                    // reset this: used to allow the window to close immediately;
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
            ValidateProxyServerUsername();
            ValidateProxyServerAddress();
            ValidateProxyServerPort();
            ValidateProxyServerPassword();
        }


        /// <summary>
        /// Validate the ProxyServerUsername property.
        /// </summary>
        private void ValidateProxyServerUsername()
        {
            RemoveAllErrorsForPropertyName(ProxyServerUsernamePropertyName);
            if (_cbServerRequiresAPassword && _proxyServerUsername.Length == 0)
            {
                AddError(ProxyServerUsernamePropertyName, "The user name must be specified.");
            }
        }

        /// <summary>
        /// Validate the ProxyServerAddress property.
        /// </summary>
        private void ValidateProxyServerAddress()
        {
            RemoveAllErrorsForPropertyName(ProxyServerAddressPropertyName);
            if (_rbProxySettingsManual && _proxyServerAddress.Length == 0)
            {
                AddError(ProxyServerAddressPropertyName, "The proxy server address must be specified.");
            }
        }

        /// <summary>
        /// Validate the ProxyServerPort property.
        /// </summary>
        private void ValidateProxyServerPort()
        {
            RemoveAllErrorsForPropertyName(ProxyServerPortPropertyName);
            if (_proxyServerPort.Length > 0)
            {
                int port;
                bool parseSuccessful = int.TryParse(_proxyServerPort, out port);
                if (!parseSuccessful)
                {
                    AddError(ProxyServerPortPropertyName, "Please enter a positive integer less than or equal to 65,535.");
                }
                else
                {
                    if (port < 0 || port > 65535)
                    {
                        AddError(ProxyServerPortPropertyName, "Please enter a positive integer less than or equal to 65,535.");
                    }
                }
            }
        }

        /// <summary>
        /// Validate the ProxyServerPassword property.
        /// </summary>
        private void ValidateProxyServerPassword()
        {
            //TODO: Should there be validation on the password.  Some installations may require a user name, but no password?
            //RemoveAllErrorsForPropertyName("ProxyServerPassword");
        }


        #endregion

        #region Support Functions

        /// <summary>
        /// Get the preferences subset for this dialog from the current properties.
        /// <<returns>ProxiesPreferencesSubset: The output preferences subset.</returns>
        /// </summary>
        private void GetPreferencesSubsetFromProperties()
        {
            if (_rbProxySettingsNoProxy)
            {
                _proxiesPreferencesSubset.ProxySettingType = useProxySettingType.useProxySettingNoProxy;
            }
            else if (_rbProxySettingsAutoDetect)
            {
                _proxiesPreferencesSubset.ProxySettingType = useProxySettingType.useProxySettingAutoDetect;
            }
            else
            {
                _proxiesPreferencesSubset.ProxySettingType = useProxySettingType.useProxySettingManual;
            }
            _proxiesPreferencesSubset.ProxyType = _cbProxyType;
            _proxiesPreferencesSubset.ProxyServerAddress = string.IsNullOrWhiteSpace(_proxyServerAddress) ? string.Empty : _proxyServerAddress;
            _proxiesPreferencesSubset.ProxyServerPort = string.IsNullOrWhiteSpace(_proxyServerPort) ? 0 : int.Parse(_proxyServerPort);
            _proxiesPreferencesSubset.ProxyServerRequiresPassword = _cbServerRequiresAPassword;
            _proxiesPreferencesSubset.ProxyServerUserName = string.IsNullOrWhiteSpace(_proxyServerUsername) ? string.Empty : _proxyServerUsername;

            // Get the clear password again.
            CLAppMessages.DialogPreferencesNetworkProxies_GetClearPasswordField.Send("");
            _proxiesPreferencesSubset.ProxyServerPassword = CLSecureString.EncryptString(CLSecureString.ToSecureString(_proxyServerPassword2));
        }

        /// <summary>
        /// Set this dialog's properties from the current preferences subset.
        /// <param name="subsetPreferences">The current preferences subset.</param>"/>
        /// <returns>void</returns>
        /// </summary>
        private void SetPropertiesFromPreferencesSubset(ProxiesPreferencesSubset subsetPreferences)
        {
            // Set all of the view-bound properties.
            RbProxySettingsNoProxy = subsetPreferences.ProxySettingType == useProxySettingType.useProxySettingNoProxy ? true : false;
            RbProxySettingsAutoDetect = subsetPreferences.ProxySettingType == useProxySettingType.useProxySettingAutoDetect ? true : false;
            RbProxySettingsManual = subsetPreferences.ProxySettingType == useProxySettingType.useProxySettingManual ? true : false;
            CbProxyType = subsetPreferences.ProxyType;
            ProxyServerAddress = subsetPreferences.ProxyServerAddress;
            if (string.IsNullOrWhiteSpace(ProxyServerAddress))
            {
                ProxyServerAddress = string.Empty;
            }
            ProxyServerPort = subsetPreferences.ProxyServerPort == 0 ? string.Empty :
                                          subsetPreferences.ProxyServerPort.ToString();
            CbServerRequiresAPassword = subsetPreferences.ProxyServerRequiresPassword;
            if (string.IsNullOrWhiteSpace(ProxyServerAddress))
            {
                ProxyServerAddress = string.Empty;
            }
            ProxyServerUsername = subsetPreferences.ProxyServerUserName;
            if (string.IsNullOrWhiteSpace(ProxyServerUsername))
            {
                ProxyServerUsername = string.Empty;
            }
            ProxyServerPassword2 = CLSecureString.ToInsecureString(CLSecureString.DecryptString(subsetPreferences.ProxyServerPassword == null ? String.Empty : subsetPreferences.ProxyServerPassword));
            if (string.IsNullOrWhiteSpace(ProxyServerPassword2))
            {
                _proxyServerPassword2 = string.Empty;
            }

            // Make sure the proper opacity is set based on the proxy manual setting type.
            if (_rbProxySettingsManual)
            {
                ProxyManualOpacity = _kProxyActiveOpacity;
                ProxyManualControlsEnabled = true;
            }
            else
            {
                ProxyManualOpacity = _kNoProxyOpacity;
                ProxyManualControlsEnabled = false;
            }

            // Make sure the proper opacity is set based on the proxy "No Proxy" setting type
            if (_rbProxySettingsNoProxy)
            {
                ProxyNoProxyOpacity = _kNoProxyOpacity;
                ProxyNoProxyControlsEnabled = false;
            }
            else
            {
                ProxyNoProxyOpacity = _kProxyActiveOpacity;
                ProxyNoProxyControlsEnabled = true;
            }

            // Make sure the proper opacity is set based on the "Server requires password" checkbox.
            if (_cbServerRequiresAPassword)
            {
                ProxyAuthenticationControlsOpacity = _kProxyActiveOpacity;
                ProxyAuthenticationControlsEnabled = true;
            }
            else
            {
                ProxyAuthenticationControlsOpacity = _kNoProxyOpacity;
                ProxyAuthenticationControlsEnabled = false;
            }
        }

        /// <summary>
        /// Get the preferences subset for this dialog from the global preferences.
        /// <param name="globalPreferences">The intput global preferences.</param>
        /// <<returns>ProxiesPreferencesSubset: The output preferences subset.</returns>
        /// </summary>
        private ProxiesPreferencesSubset GetPreferencesSubsetFromGlobalPreferences(CLPreferences globalPreferences)
        {
            ProxiesPreferencesSubset ProxiesPreferencesSubset = new ProxiesPreferencesSubset()
            {
                ProxySettingType = globalPreferences.ProxySettingType,
                ProxyType = globalPreferences.ProxyType,
                ProxyServerAddress = globalPreferences.ProxyServerAddress,
                ProxyServerPort = globalPreferences.ProxyServerPort,
                ProxyServerRequiresPassword = globalPreferences.ProxyServerRequiresPassword,
                ProxyServerUserName = globalPreferences.ProxyServerUserName,
                ProxyServerPassword = globalPreferences.ProxyServerPassword,
            };
            return ProxiesPreferencesSubset;
        }


        /// <summary>
        /// Set the preferences subset for this dialog to the global preferences.
        /// <param name="globalPreferences">The output global preferences to read.</param>
        /// <param name="subsetPreferences">The input preferences subset.</param>
        /// <<returns>void.</returns>
        /// </summary>
        private void SetPreferencesSubsetToGlobalPreferences(CLPreferences globalPreferences, ProxiesPreferencesSubset subsetPreferences)
        {
            globalPreferences.ProxySettingType = subsetPreferences.ProxySettingType;
            globalPreferences.ProxyType = subsetPreferences.ProxyType;
            globalPreferences.ProxyServerAddress = subsetPreferences.ProxyServerAddress;
            globalPreferences.ProxyServerPort = subsetPreferences.ProxyServerPort;
            globalPreferences.ProxyServerRequiresPassword = subsetPreferences.ProxyServerRequiresPassword;
            globalPreferences.ProxyServerUserName = subsetPreferences.ProxyServerUserName;
            globalPreferences.ProxyServerPassword = subsetPreferences.ProxyServerPassword;
        }

        /// <summary>
        /// Commit the preference subset changes to the global preferences.
        /// </summary>
        private void ProcessUpdateCommand()
        {
            // Pull the changes from the properties into the preferences subset
            GetPreferencesSubsetFromProperties();

            // Save the preferences set by the user.
            if (!PreferenceSubsetsAreEqual(_proxiesPreferencesSubset, _originalProxiesPreferencesSubset))
            {
                SetPreferencesSubsetToGlobalPreferences(_dialogPreferencesNetworkProxies_Preferences, _proxiesPreferencesSubset);
                _originalProxiesPreferencesSubset = _proxiesPreferencesSubset.DeepCopy<ProxiesPreferencesSubset>();   // also to the original copy because we saved the changes.
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
            if (!PreferenceSubsetsAreEqual(_proxiesPreferencesSubset, _originalProxiesPreferencesSubset))
            {
                // The user is cancelling and there are unsaved changes.  Ask if he wants to save them.
                CLModalMessageBoxDialogs.Instance.DisplayModalSaveChangesPrompt(container: ViewLayoutRoot, dialog: out _dialog, actionResultHandler: returnedViewModelInstance =>
                {
                    _trace.writeToLog(9, "DialogPreferencesNetworkProxies: Prompt save changes: Entry.");
                    if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                    {
                        // The user said yes.
                        _trace.writeToLog(9, "DialogPreferencesNetworkProxies: Prompt save changes: User said yes.");
                        SetPreferencesSubsetToGlobalPreferences(_dialogPreferencesNetworkProxies_Preferences, _proxiesPreferencesSubset);
                    }

                    // Don't handle the cancel again.
                    _windowClosingImmediately = true;
                    WindowCloseOk = true;

                    // Ask the view to close.
                    CLAppMessages.Message_DialogPreferencesNetworkProxiesViewShouldClose.Send("");
                });

                return true;    // keep the window open.  The child dialog completion will close this dialog.
            }
            else
            {
                // There are no changes.  Just allow the window to close.
                return false;
            }
        }

        /// <summary>
        /// Compare two proxy preference subsets.
        /// Unfortunately, the subset contains an encrypted password.  The encrypted strings are not always the
        /// same for a given clear text string.  We have to do some nonsense so we can use DeepCompare for the
        /// other fields.
        /// </summary>
        /// <<returns>bool: true: Cancel the Window Close operation.</returns>
        private bool PreferenceSubsetsAreEqual(ProxiesPreferencesSubset firstSubset, ProxiesPreferencesSubset secondSubset)
        {
            // Decrypt both of the passwords
            string firstClearPassword = CLSecureString.ToInsecureString(CLSecureString.DecryptString(firstSubset.ProxyServerPassword == null ? String.Empty : firstSubset.ProxyServerPassword));
            string secondClearPassword = CLSecureString.ToInsecureString(CLSecureString.DecryptString(secondSubset.ProxyServerPassword == null ? String.Empty : secondSubset.ProxyServerPassword));
            if (!firstClearPassword.Equals(secondClearPassword, StringComparison.InvariantCulture))
            {
                return false;
            }

            // The passwords are the same.  Compare the rest, but not the password.
            string firstEncryptedPassword = firstSubset.ProxyServerPassword;
            string secondEncryptedPassword = secondSubset.ProxyServerPassword;
            firstSubset.ProxyServerPassword = String.Empty;
            secondSubset.ProxyServerPassword = String.Empty;

            bool isEqual = CLDeepCompare.IsEqual(firstSubset, secondSubset);

            firstSubset.ProxyServerPassword = firstEncryptedPassword;
            secondSubset.ProxyServerPassword = secondEncryptedPassword;

            return isEqual;
        }

        #endregion
    }
}