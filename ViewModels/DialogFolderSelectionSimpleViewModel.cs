//
//  DialogFolderSelectionSimpleViewModel.cs
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
using CloudApiPrivate.Model.Settings;
using System.Windows;

namespace win_client.ViewModels
{
    public class DialogFolderSelectionSimpleViewModel : ValidatingViewModelBase
    {
        public DialogFolderSelectionSimpleViewModel()
        {

        }

        /// <summary>
        /// The <see cref="FolderSelectionSimpleViewModel_ButtonLeftText" /> property's name.
        /// </summary>
        public const string FolderSelectionSimpleViewModel_ButtonLeftTextPropertyName = "FolderSelectionSimpleViewModel_ButtonLeftText";

        private string _folderSelectionSimpleViewModel_ButtonLeftText = "";

        /// <summary>
        /// Sets and gets the FolderSelectionSimpleViewModel_ButtonLeftText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string FolderSelectionSimpleViewModel_ButtonLeftText
        {
            get
            {
                return _folderSelectionSimpleViewModel_ButtonLeftText;
            }

            set
            {
                if (_folderSelectionSimpleViewModel_ButtonLeftText == value)
                {
                    return;
                }

                _folderSelectionSimpleViewModel_ButtonLeftText = value;
                RaisePropertyChanged(FolderSelectionSimpleViewModel_ButtonLeftTextPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="FolderSelectionSimpleViewModel_ButtonRightText" /> property's name.
        /// </summary>
        public const string FolderSelectionSimpleViewModel_ButtonRightTextPropertyName = "FolderSelectionSimpleViewModel_ButtonRightText";

        private string _folderSelectionSimpleViewModel_ButtonRightText = "";

        /// <summary>
        /// Sets and gets the FolderSelectionSimpleViewModel_ButtonRightText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string FolderSelectionSimpleViewModel_ButtonRightText
        {
            get
            {
                return _folderSelectionSimpleViewModel_ButtonRightText;
            }

            set
            {
                if (_folderSelectionSimpleViewModel_ButtonRightText == value)
                {
                    return;
                }

                _folderSelectionSimpleViewModel_ButtonRightText = value;
                RaisePropertyChanged(FolderSelectionSimpleViewModel_ButtonRightTextPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="FolderSelectionSimpleViewModel_FolderLocationText" /> property's name.
        /// </summary>
        public const string FolderSelectionSimpleViewModel_FolderLocationTextPropertyName = "FolderSelectionSimpleViewModel_FolderLocationText";

        private string _folderSelectionSimpleViewModel_FolderLocationText = "";

        /// <summary>
        /// Sets and gets the FolderSelectionSimpleViewModel_FolderLocationText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string FolderSelectionSimpleViewModel_FolderLocationText
        {
            get
            {
                return _folderSelectionSimpleViewModel_FolderLocationText;
            }

            set
            {
                if (_folderSelectionSimpleViewModel_FolderLocationText == value)
                {
                    return;
                }

                _folderSelectionSimpleViewModel_FolderLocationText = value;
                RaisePropertyChanged(FolderSelectionSimpleViewModel_FolderLocationTextPropertyName);
            }
        }
     }
}