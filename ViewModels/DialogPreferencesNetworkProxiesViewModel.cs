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
        /// The <see cref="RbProxySettings" /> property's name.
        /// </summary>
        public const string RbProxySettingsPropertyName = "RbProxySettings";
        private useProxySettingType _rbProxySettings = useProxySettingType.useProxySettingNoProxy;
        public useProxySettingType RbProxySettings
        {
            get
            {
                return _rbProxySettings;
            }

            set
            {
                if (_rbProxySettings == value)
                {
                    return;
                }

                _rbProxySettings = value;
                RaisePropertyChanged(RbProxySettingsPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogPreferencesNetworkProxies_Preferences" /> property's name.
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

                _dialogPreferencesNetworkProxies_Preferences = value;
                RaisePropertyChanged(DialogPreferencesNetworkProxies_PreferencesPropertyName);
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

                                              // Handle the update command
                                              
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
    }
}