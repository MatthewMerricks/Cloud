//
//  DialogCloudMessageBoxViewModel.cs
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

namespace win_client.ViewModels
{
    public class DialogCloudMessageBoxViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;

        #endregion


        public DialogCloudMessageBoxViewModel()
        {
            _rm = CLAppDelegate.Instance.ResourceManager;
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_Title" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_TitlePropertyName = "CloudMessageBoxView_Title";
        private string _cloudMessageBoxView_Title = "Title";
        public string CloudMessageBoxView_Title
        {
            get
            {
                return _cloudMessageBoxView_Title;
            }

            set
            {
                if (_cloudMessageBoxView_Title == value)
                {
                    return;
                }

                _cloudMessageBoxView_Title = value;
                RaisePropertyChanged(CloudMessageBoxView_TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_WindowWidth" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_WindowWidthPropertyName = "CloudMessageBoxView_WindowWidth";
        private int _cloudMessageBoxView_WindowWidth = 325;
        public int CloudMessageBoxView_WindowWidth
        {
            get
            {
                return _cloudMessageBoxView_WindowWidth;
            }

            set
            {
                if (_cloudMessageBoxView_WindowWidth == value)
                {
                    return;
                }

                _cloudMessageBoxView_WindowWidth = value;
                RaisePropertyChanged(CloudMessageBoxView_WindowWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_WindowHeight" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_WindowHeightPropertyName = "CloudMessageBoxView_WindowHeight";
        private int _cloudMessageBoxView_WindowHeight = 210;
        public int CloudMessageBoxView_WindowHeight
        {
            get
            {
                return _cloudMessageBoxView_WindowHeight;
            }

            set
            {
                if (_cloudMessageBoxView_WindowHeight == value)
                {
                    return;
                }

                _cloudMessageBoxView_WindowHeight = value;
                RaisePropertyChanged(CloudMessageBoxView_WindowHeightPropertyName);
            }
        }
        /// <summary>
        /// The <see cref="CloudMessageBoxView_HeaderText" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_HeaderTextPropertyName = "CloudMessageBoxView_HeaderText";
        private string _cloudMessageBoxView_HeaderText = "";
        public string CloudMessageBoxView_HeaderText
        {
            get
            {
                return _cloudMessageBoxView_HeaderText;
            }

            set
            {
                if (_cloudMessageBoxView_HeaderText == value)
                {
                    return;
                }

                _cloudMessageBoxView_HeaderText = value;
                RaisePropertyChanged(CloudMessageBoxView_HeaderTextPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_BodyText" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_BodyTextPropertyName = "CloudMessageBoxView_BodyText";
        private string _cloudMessageBoxView_BodyText = "";
        public string CloudMessageBoxView_BodyText
        {
            get
            {
                return _cloudMessageBoxView_BodyText;
            }

            set
            {
                if (_cloudMessageBoxView_BodyText == value)
                {
                    return;
                }

                _cloudMessageBoxView_BodyText = value;
                RaisePropertyChanged(CloudMessageBoxView_BodyTextPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_LeftButtonWidth" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_LeftButtonWidthPropertyName = "CloudMessageBoxView_LeftButtonWidth";
        private double _cloudMessageBoxView_LeftButtonWidth = 100.0;
        public double CloudMessageBoxView_LeftButtonWidth
        {
            get
            {
                return _cloudMessageBoxView_LeftButtonWidth;
            }

            set
            {
                if (_cloudMessageBoxView_LeftButtonWidth == value)
                {
                    return;
                }

                _cloudMessageBoxView_LeftButtonWidth = value;
                RaisePropertyChanged(CloudMessageBoxView_LeftButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_LeftButtonMargin" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_LeftButtonMarginPropertyName = "CloudMessageBoxView_LeftButtonMargin";
        private Thickness _cloudMessageBoxView_LeftButtonMargin = new Thickness(0, 0, 0, 0);
        public Thickness CloudMessageBoxView_LeftButtonMargin
        {
            get
            {
                return _cloudMessageBoxView_LeftButtonMargin;
            }

            set
            {
                if (_cloudMessageBoxView_LeftButtonMargin == value)
                {
                    return;
                }

                _cloudMessageBoxView_LeftButtonMargin = value;
                RaisePropertyChanged(CloudMessageBoxView_LeftButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_LeftButtonContent" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_LeftButtonContentPropertyName = "CloudMessageBoxView_LeftButtonContent";
        private string _cloudMessageBoxView_LeftButtonContent = "";
        public string CloudMessageBoxView_LeftButtonContent
        {
            get
            {
                return _cloudMessageBoxView_LeftButtonContent;
            }

            set
            {
                if (_cloudMessageBoxView_LeftButtonContent == value)
                {
                    return;
                }

                _cloudMessageBoxView_LeftButtonContent = value;
                RaisePropertyChanged(CloudMessageBoxView_LeftButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_LeftButtonVisibility" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_LeftButtonVisibilityPropertyName = "CloudMessageBoxView_LeftButtonVisibility";
        private Visibility _cloudMessageBoxView_LeftButtonVisibility = Visibility.Visible;
        /// </summary>
        public Visibility CloudMessageBoxView_LeftButtonVisibility
        {
            get
            {
                return _cloudMessageBoxView_LeftButtonVisibility;
            }

            set
            {
                if (_cloudMessageBoxView_LeftButtonVisibility == value)
                {
                    return;
                }

                _cloudMessageBoxView_LeftButtonVisibility = value;
                RaisePropertyChanged(CloudMessageBoxView_LeftButtonVisibilityPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_RightButtonWidth" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_RightButtonWidthPropertyName = "CloudMessageBoxView_RightButtonWidth";
        private double _cloudMessageBoxView_RightButtonWidth = 100.0;
        public double CloudMessageBoxView_RightButtonWidth
        {
            get
            {
                return _cloudMessageBoxView_RightButtonWidth;
            }

            set
            {
                if (_cloudMessageBoxView_RightButtonWidth == value)
                {
                    return;
                }

                _cloudMessageBoxView_RightButtonWidth = value;
                RaisePropertyChanged(CloudMessageBoxView_RightButtonWidthPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_RightButtonMargin" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_RightButtonMarginPropertyName = "CloudMessageBoxView_RightButtonMargin";
        private Thickness _cloudMessageBoxView_RightButtonMargin = new Thickness(0, 0, 0, 0);
        public Thickness CloudMessageBoxView_RightButtonMargin
        {
            get
            {
                return _cloudMessageBoxView_RightButtonMargin;
            }

            set
            {
                if (_cloudMessageBoxView_RightButtonMargin == value)
                {
                    return;
                }

                _cloudMessageBoxView_RightButtonMargin = value;
                RaisePropertyChanged(CloudMessageBoxView_RightButtonMarginPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_RightButtonContent" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_RightButtonContentPropertyName = "CloudMessageBoxView_RightButtonContent";
        private string _cloudMessageBoxView_RightButtonContent = "";
        public string CloudMessageBoxView_RightButtonContent
        {
            get
            {
                return _cloudMessageBoxView_RightButtonContent;
            }

            set
            {
                if (_cloudMessageBoxView_RightButtonContent == value)
                {
                    return;
                }

                _cloudMessageBoxView_RightButtonContent = value;
                RaisePropertyChanged(CloudMessageBoxView_RightButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="CloudMessageBoxView_RightButtonVisibility" /> property's name.
        /// </summary>
        public const string CloudMessageBoxView_RightButtonVisibilityPropertyName = "CloudMessageBoxView_RightButtonVisibility";
        private Visibility _cloudMessageBoxView_RightButtonVisibility = Visibility.Visible;
        /// </summary>
        public Visibility CloudMessageBoxView_RightButtonVisibility
        {
            get
            {
                return _cloudMessageBoxView_RightButtonVisibility;
            }

            set
            {
                if (_cloudMessageBoxView_RightButtonVisibility == value)
                {
                    return;
                }

                _cloudMessageBoxView_RightButtonVisibility = value;
                RaisePropertyChanged(CloudMessageBoxView_RightButtonVisibilityPropertyName);
            }
        }

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
                                              args.Cancel = OnClosing();
                                          }));
            }
        }

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