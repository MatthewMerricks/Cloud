//
//  ModalDialog.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.using System;

using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using Dialog.Abstractions.Wpf.Intefaces;
using GalaSoft.MvvmLight.Ioc;
using win_client.AppDelegate;
using win_client.Common;
using win_client.ViewModels;

namespace win_client.ViewModelHelpers
{
    public sealed class CLModalErrorDialog
    {
        private static CLModalErrorDialog _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace;
        private static ResourceManager _rm;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLModalErrorDialog Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLModalErrorDialog();

                        // Perform initialization
                        _rm = CLAppDelegate.Instance.ResourceManager;
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLModalErrorDialog()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Display an error message inside a grid.
        /// </summary>
        public void DisplayModalErrorMessage(string errorMessage, string title, string headerText, 
                                string rightButtonContent, Grid container,
                                System.Action<DialogCloudMessageBoxViewModel> actionOkButtonHandler)
        {
            _trace.writeToLog(1, "CLModalErrorDialog: DisplayModalErrorMessage:  Error: {0}.", errorMessage);

            var dialog = SimpleIoc.Default.GetInstance<IModalWindow>(CLConstants.kDialogBox_CloudMessageBoxView);
            IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
            IMessageBoxService messageBoxService = SimpleIoc.Default.GetInstance<IMessageBoxService>();
            modalDialogService.ShowDialog(dialog, new DialogCloudMessageBoxViewModel
            {
                CloudMessageBoxView_Title = title,
                CloudMessageBoxView_WindowWidth = 450,
                CloudMessageBoxView_WindowHeight = 230,
                CloudMessageBoxView_HeaderText = headerText,
                CloudMessageBoxView_BodyText = errorMessage,
                CloudMessageBoxView_LeftButtonVisibility = Visibility.Collapsed,
                CloudMessageBoxView_RightButtonWidth = new GridLength(100),
                CloudMessageBoxView_RightButtonMargin = new Thickness(0, 0, 0, 0),
                CloudMessageBoxView_RightButtonContent = rightButtonContent,
            },
            container,
            actionOkButtonHandler
            //returnedViewModelInstance =>
            //{
            //    // User clicked OK.  Do nothing here.  Leave the user on the CreateNewAccount page.
            //}
            );
        }
    }
}