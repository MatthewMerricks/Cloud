//
//  DialogCheckForUpdatesViewModel.cs
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
using CloudApiPublic.Support;
using System.Resources;
using win_client.AppDelegate;
using System.ComponentModel;
using win_client.Common;

namespace win_client.ViewModels
{
    public class DialogCheckForUpdatesViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private CLTrace _trace = CLTrace.Instance;

        #endregion


        public DialogCheckForUpdatesViewModel()
        {

        }

        /// <summary>
        /// The <see cref="IsBusy" /> property's name.
        /// </summary>
        public const string IsBusyPropertyName = "IsBusy";
        private bool _isBusy = false;
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }

            set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                RaisePropertyChanged(IsBusyPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="BusyContent" /> property's name.
        /// </summary>
        public const string BusyContentPropertyName = "BusyContent";
        private string _busyContent = "Creating account...";
        public string BusyContent
        {
            get
            {
                return _busyContent;
            }

            set
            {
                if (_busyContent == value)
                {
                    return;
                }

                _busyContent = value;
                RaisePropertyChanged(BusyContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_Title" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_TitlePropertyName = "DialogCheckForUpdates_Title";
        private string _DialogCheckForUpdates_Title = "Title";
        public string DialogCheckForUpdates_Title
        {
            get
            {
                return _DialogCheckForUpdates_Title;
            }

            set
            {
                if (_DialogCheckForUpdates_Title == value)
                {
                    return;
                }

                _DialogCheckForUpdates_Title = value;
                RaisePropertyChanged(DialogCheckForUpdates_TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_WindowWidth" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_WindowWidthPropertyName = "DialogCheckForUpdates_WindowWidth";
        private int _DialogCheckForUpdates_WindowWidth = 325;
        public int DialogCheckForUpdates_WindowWidth
        {
            get
            {
                return _DialogCheckForUpdates_WindowWidth;
            }

            set
            {
                if (_DialogCheckForUpdates_WindowWidth == value)
                {
                    return;
                }

                _DialogCheckForUpdates_WindowWidth = value;
                RaisePropertyChanged(DialogCheckForUpdates_WindowWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_WindowHeight" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_WindowHeightPropertyName = "DialogCheckForUpdates_WindowHeight";
        private int _DialogCheckForUpdates_WindowHeight = 210;
        public int DialogCheckForUpdates_WindowHeight
        {
            get
            {
                return _DialogCheckForUpdates_WindowHeight;
            }

            set
            {
                if (_DialogCheckForUpdates_WindowHeight == value)
                {
                    return;
                }

                _DialogCheckForUpdates_WindowHeight = value;
                RaisePropertyChanged(DialogCheckForUpdates_WindowHeightPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_RightButtonWidth" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_RightButtonWidthPropertyName = "DialogCheckForUpdates_RightButtonWidth";
        private double _DialogCheckForUpdates_RightButtonWidth = 100.0;
        public double DialogCheckForUpdates_RightButtonWidth
        {
            get
            {
                return _DialogCheckForUpdates_RightButtonWidth;
            }

            set
            {
                if (_DialogCheckForUpdates_RightButtonWidth == value)
                {
                    return;
                }

                _DialogCheckForUpdates_RightButtonWidth = value;
                RaisePropertyChanged(DialogCheckForUpdates_RightButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_RightButtonMargin" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_RightButtonMarginPropertyName = "DialogCheckForUpdates_RightButtonMargin";
        private Thickness _DialogCheckForUpdates_RightButtonMargin = new Thickness(0, 0, 0, 0);
        public Thickness DialogCheckForUpdates_RightButtonMargin
        {
            get
            {
                return _DialogCheckForUpdates_RightButtonMargin;
            }

            set
            {
                if (_DialogCheckForUpdates_RightButtonMargin == value)
                {
                    return;
                }

                _DialogCheckForUpdates_RightButtonMargin = value;
                RaisePropertyChanged(DialogCheckForUpdates_RightButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_RightButtonContent" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_RightButtonContentPropertyName = "DialogCheckForUpdates_RightButtonContent";
        private string _DialogCheckForUpdates_RightButtonContent = "";
        public string DialogCheckForUpdates_RightButtonContent
        {
            get
            {
                return _DialogCheckForUpdates_RightButtonContent;
            }

            set
            {
                if (_DialogCheckForUpdates_RightButtonContent == value)
                {
                    return;
                }

                _DialogCheckForUpdates_RightButtonContent = value;
                RaisePropertyChanged(DialogCheckForUpdates_RightButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_RightButtonVisibility" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_RightButtonVisibilityPropertyName = "DialogCheckForUpdates_RightButtonVisibility";
        private Visibility _DialogCheckForUpdates_RightButtonVisibility = Visibility.Visible;
        /// </summary>
        public Visibility DialogCheckForUpdates_RightButtonVisibility
        {
            get
            {
                return _DialogCheckForUpdates_RightButtonVisibility;
            }

            set
            {
                if (_DialogCheckForUpdates_RightButtonVisibility == value)
                {
                    return;
                }

                _DialogCheckForUpdates_RightButtonVisibility = value;
                RaisePropertyChanged(DialogCheckForUpdates_RightButtonVisibilityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_RightButtonIsDefault" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_RightButtonIsDefaultPropertyName = "DialogCheckForUpdates_RightButtonIsDefault";
        private bool _DialogCheckForUpdates_RightButtonIsDefault = false;
        public bool DialogCheckForUpdates_RightButtonIsDefault
        {
            get
            {
                return _DialogCheckForUpdates_RightButtonIsDefault;
            }

            set
            {
                if (_DialogCheckForUpdates_RightButtonIsDefault == value)
                {
                    return;
                }

                _DialogCheckForUpdates_RightButtonIsDefault = value;
                RaisePropertyChanged(DialogCheckForUpdates_RightButtonIsDefaultPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="DialogCheckForUpdates_RightButtonIsCancel" /> property's name.
        /// </summary>
        public const string DialogCheckForUpdates_RightButtonIsCancelPropertyName = "DialogCheckForUpdates_RightButtonIsCancel";

        private bool _DialogCheckForUpdates_RightButtonIsCancel = false;

        /// <summary>
        /// Sets and gets the DialogCheckForUpdates_RightButtonIsCancel property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool DialogCheckForUpdates_RightButtonIsCancel
        {
            get
            {
                return _DialogCheckForUpdates_RightButtonIsCancel;
            }

            set
            {
                if (_DialogCheckForUpdates_RightButtonIsCancel == value)
                {
                    return;
                }

                _DialogCheckForUpdates_RightButtonIsCancel = value;
                RaisePropertyChanged(DialogCheckForUpdates_RightButtonIsCancelPropertyName);
            }
        }

        #region Relay Commands

        #endregion

        #region Support Functions

        /// <summary>
        /// Implement window closing logic.
        /// <remarks>Note: This function will be called twice when the user clicks the Cancel button, and only once when the user
        /// clicks the 'X'.  Be careful to check for the "already cleaned up" case.</remarks>
        /// <<returns>true to cancel the cancel.</returns>
        /// </summary>
        private bool OnClosing()
        {
            // Clean-up logic here.
            return false;                   // don't cancel the user's request to cancel
        }

        #endregion
    }
}