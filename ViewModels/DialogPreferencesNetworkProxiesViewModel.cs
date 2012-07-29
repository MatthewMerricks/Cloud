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

namespace win_client.ViewModels
{
    public class DialogPreferencesNetworkProxiesViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

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

                // Set the bindable properties with the current preferences.  Do this without triggering
                // PropertyChanged events.
                _rbProxySettingsNoProxy = value.ProxySettingType == useProxySettingType.useProxySettingNoProxy ? true : false;
                _rbProxySettingsAutoDetect = value.ProxySettingType == useProxySettingType.useProxySettingAutoDetect ? true : false;
                _rbProxySettingsManual = value.ProxySettingType == useProxySettingType.useProxySettingManual ? true : false;
                _cbProxyType = value.ProxyType;
                _proxyServerAddress = value.ProxyServerAddress;
                _proxyServerPort = value.ProxyServerPort.ToString();
                _cbServerRequiresAPassword = value.ProxyServerRequiresPassword;
                _proxyServerUsername = value.ProxyServerUserName;
                _proxyServerPassword = value.ProxyServerPassword;

                // Set the property
                _dialogPreferencesNetworkProxies_Preferences = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_PreferencesPropertyName);
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
                return DialogPreferencesNetworkProxies_Preferences.ProxySettingType == useProxySettingType.useProxySettingNoProxy ? true : false;
            }

            set
            {
                if (_rbProxySettingsNoProxy != value)
                {
                    DialogPreferencesNetworkProxies_Preferences.ProxySettingType = useProxySettingType.useProxySettingNoProxy;
                    CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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
                return DialogPreferencesNetworkProxies_Preferences.ProxySettingType == useProxySettingType.useProxySettingAutoDetect ? true : false;
            }

            set
            {
                if (_rbProxySettingsAutoDetect != value)
                {
                    DialogPreferencesNetworkProxies_Preferences.ProxySettingType = useProxySettingType.useProxySettingAutoDetect;
                    CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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
                return DialogPreferencesNetworkProxies_Preferences.ProxySettingType == useProxySettingType.useProxySettingManual ? true : false;
            }

            set
            {
                if (_rbProxySettingsManual!= value)
                {
                    DialogPreferencesNetworkProxies_Preferences.ProxySettingType = useProxySettingType.useProxySettingManual;
                    CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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
                CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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

                _proxyServerAddress = value;
                CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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

                _proxyServerPort = value;
                CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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

                _cbServerRequiresAPassword = value;
                CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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
                if (_proxyServerUsername == value)
                {
                    return;
                }

                _proxyServerUsername = value;
                CLExtensionMethods.ForceValidation(_viewLayoutRoot);
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
                CLExtensionMethods.ForceValidation(_viewLayoutRoot);
                RaisePropertyChanged(ProxyServerPasswordPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_Title" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_TitlePropertyName = "DialogPreferencesNetworkProxies_Title";
        private string _DialogPreferencesNetworkProxies_Title = "";
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_Title property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string DialogPreferencesNetworkProxies_Title
        {
            get
            {
                return _DialogPreferencesNetworkProxies_Title;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_Title == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_Title = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_WindowWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_WindowWidthPropertyName = "DialogPreferencesNetworkProxies_WindowWidth";

        private int _DialogPreferencesNetworkProxies_WindowWidth = 325;

        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_WindowWidth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int DialogPreferencesNetworkProxies_WindowWidth
        {
            get
            {
                return _DialogPreferencesNetworkProxies_WindowWidth;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_WindowWidth == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_WindowWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_WindowWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_WindowHeight" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_WindowHeightPropertyName = "DialogPreferencesNetworkProxies_WindowHeight";

        private int _DialogPreferencesNetworkProxies_WindowHeight = 210;

        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_WindowHeight property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int DialogPreferencesNetworkProxies_WindowHeight
        {
            get
            {
                return _DialogPreferencesNetworkProxies_WindowHeight;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_WindowHeight == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_WindowHeight = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_WindowHeightPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonWidthPropertyName = "DialogPreferencesNetworkProxies_LeftButtonWidth";
        private GridLength _DialogPreferencesNetworkProxies_LeftButtonWidth = new GridLength(75);
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_LeftButtonWidth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// Set this to the width desired, plus the right margin.  So
        /// if you want a width of 75, and a distance between buttons of 50,  set
        /// LeftButtonMargin = new Thickness(0, 0, 50, 0), and
        /// LeftButtonWidth = new GridLength(125)
        /// </summary>
        public GridLength DialogPreferencesNetworkProxies_LeftButtonWidth
        {
            get
            {
                return _DialogPreferencesNetworkProxies_LeftButtonWidth;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_LeftButtonWidth == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_LeftButtonWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonMargin" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonMarginPropertyName = "DialogPreferencesNetworkProxies_LeftButtonMargin";
        private Thickness _DialogPreferencesNetworkProxies_LeftButtonMargin = new Thickness(30, 0, 0, 0);
        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_LeftButtonMargin property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Thickness DialogPreferencesNetworkProxies_LeftButtonMargin
        {
            get
            {
                return _DialogPreferencesNetworkProxies_LeftButtonMargin;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_LeftButtonMargin == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_LeftButtonMargin = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_LeftButtonContent" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_LeftButtonContentPropertyName = "DialogPreferencesNetworkProxies_LeftButtonContent";

        private string _DialogPreferencesNetworkProxies_LeftButtonContent = "";

        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_LeftButtonContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string DialogPreferencesNetworkProxies_LeftButtonContent
        {
            get
            {
                return _DialogPreferencesNetworkProxies_LeftButtonContent;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_LeftButtonContent == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_LeftButtonContent = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_LeftButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonWidth" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonWidthPropertyName = "DialogPreferencesNetworkProxies_RightButtonWidth";

        private GridLength _DialogPreferencesNetworkProxies_RightButtonWidth = new GridLength(75);

        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_RightButtonWidth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public GridLength DialogPreferencesNetworkProxies_RightButtonWidth
        {
            get
            {
                return _DialogPreferencesNetworkProxies_RightButtonWidth;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_RightButtonWidth == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_RightButtonWidth = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonMargin" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonMarginPropertyName = "DialogPreferencesNetworkProxies_RightButtonMargin";

        private Thickness _DialogPreferencesNetworkProxies_RightButtonMargin = new Thickness(30, 0, 0, 0);

        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_RightButtonMargin property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Thickness DialogPreferencesNetworkProxies_RightButtonMargin
        {
            get
            {
                return _DialogPreferencesNetworkProxies_RightButtonMargin;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_RightButtonMargin == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_RightButtonMargin = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_RightButtonContent" /> property's name.
        /// </summary>
        public const string DialogPreferencesNetworkProxies_RightButtonContentPropertyName = "DialogPreferencesNetworkProxies_RightButtonContent";

        private string _DialogPreferencesNetworkProxies_RightButtonContent = "";

        /// <summary>
        /// Sets and gets the DialogPreferencesNetworkProxies_RightButtonContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string DialogPreferencesNetworkProxies_RightButtonContent
        {
            get
            {
                return _DialogPreferencesNetworkProxies_RightButtonContent;
            }

            set
            {
                if (_DialogPreferencesNetworkProxies_RightButtonContent == value)
                {
                    return;
                }

                _DialogPreferencesNetworkProxies_RightButtonContent = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_RightButtonContentPropertyName);
            }
        }
        #endregion

        #region Relay Commands

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
                                              CLExtensionMethods.ForceValidation(_viewLayoutRoot);
                                              if (!HasErrors)
                                              {
                                                  // Handle the update command.  The user said to save.
                                                  // Use the bindable properties set by the user to set the current preferences.
                                                  if (_rbProxySettingsNoProxy)
                                                  {
                                                      _dialogPreferencesNetworkProxies_Preferences.ProxySettingType = useProxySettingType.useProxySettingNoProxy;
                                                  }
                                                  else if (_rbProxySettingsAutoDetect)
                                                  {
                                                      _dialogPreferencesNetworkProxies_Preferences.ProxySettingType = useProxySettingType.useProxySettingAutoDetect;
                                                  }
                                                  else
                                                  {
                                                      _dialogPreferencesNetworkProxies_Preferences.ProxySettingType = useProxySettingType.useProxySettingManual;
                                                  }
                                                  _dialogPreferencesNetworkProxies_Preferences.ProxyType = _cbProxyType;
                                                  _dialogPreferencesNetworkProxies_Preferences.ProxyServerAddress = _proxyServerAddress;
                                                  _dialogPreferencesNetworkProxies_Preferences.ProxyServerPort = int.Parse(_proxyServerPort);
                                                  _dialogPreferencesNetworkProxies_Preferences.ProxyServerRequiresPassword = _cbServerRequiresAPassword;
                                                  _dialogPreferencesNetworkProxies_Preferences.ProxyServerUserName = _proxyServerUsername;
                                                  _dialogPreferencesNetworkProxies_Preferences.ProxyServerPassword = _proxyServerPassword2;
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
                                              
                                          }));
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate the ProxyServerUsername property.
        /// </summary>
        private void ValidateProxyServerUsername(string username)
        {
            RemoveAllErrorsForPropertyName("ProxyServerUsername");
            if (_cbServerRequiresAPassword && username.Length == 0)
            {
                AddError("ProxyServerUsername", "The user name must be specified.");
            }
        }

        /// <summary>
        /// Validate the ProxyServerAddress property.
        /// </summary>
        private void ValidateProxyServerAddress(string proxyServerAddress)
        {
            RemoveAllErrorsForPropertyName("ProxyServerAddress");
            if (!_rbProxySettingsNoProxy && proxyServerAddress.Length == 0)
            {
                AddError("ProxyServerAddress", "The proxy server address must be specified.");
            }
        }

        /// <summary>
        /// Validate the ProxyServerPort property.
        /// </summary>
        private void ValidateProxyServerPort(string proxyServerPort)
        {
            RemoveAllErrorsForPropertyName("ProxyServerPort");
            if (_rbProxySettingsNoProxy && proxyServerPort.Length != 0)
            {
                AddError("ProxyServerPort", "The proxy server port must not be specified.");
            }
            else if (proxyServerPort.Length > 0)
            {
                int port;
                bool parseSuccessful = int.TryParse(proxyServerPort, out port);
                if (!parseSuccessful)
                {
                    AddError("ProxyServerPort", "Please enter a positive integer less than or equal to 65,535.");
                }
            }
        }

        /// <summary>
        /// Validate the ProxyServerPassword property.
        /// </summary>
        private void ValidateProxyServerPassword(string password)
        {
            //TODO: Should there be validation on the password.  Some installations may require a user name, but no password?
            //RemoveAllErrorsForPropertyName("ProxyServerPassword");
        }


        #endregion
    }
}