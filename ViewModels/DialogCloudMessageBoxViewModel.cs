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

namespace win_client.ViewModels
{
    public class DialogCloudMessageBoxViewModel : ValidatingViewModelBase
    {
        public DialogCloudMessageBoxViewModel()
        {

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
        private GridLength _cloudMessageBoxView_LeftButtonWidth = new GridLength(100);
        public GridLength CloudMessageBoxView_LeftButtonWidth
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
        private Thickness _cloudMessageBoxView_LeftButtonMargin = new Thickness(30, 0, 0, 0);
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
        private GridLength _cloudMessageBoxView_RightButtonWidth = new GridLength(100);
        public GridLength CloudMessageBoxView_RightButtonWidth
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
        private Thickness _cloudMessageBoxView_RightButtonMargin = new Thickness(30, 0, 0, 0);
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
    }
}