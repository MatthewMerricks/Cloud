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
    public sealed class CLModalMessageBoxDialogs
    {
        private static CLModalMessageBoxDialogs _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace;
        private static ResourceManager _rm;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLModalMessageBoxDialogs Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLModalMessageBoxDialogs();

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
        private CLModalMessageBoxDialogs()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Display a prompt to shutdown the system, and perform the shutdown if the user says OK.
        /// This is called by any of the Pages.
        /// </summary>
        /// <param name="container">The Grid to paint gray and to go "modal" over.</param>
        public void DisplayModalShutdownPrompt(Grid container, out IModalWindow dialog, System.Action<DialogCloudMessageBoxViewModel> actionResultHandler)
        {
            // A page has been notified to close.  Warn the user and allow him to cancel the close.
            DisplayModalMessageBox(
                windowHeight: 250,
                leftButtonWidth: 75,
                rightButtonWidth: 75,
                title: _rm.GetString("PromptExitApplication_Title"),
                headerText: _rm.GetString("PromptExitApplication_HeaderText"),
                bodyText: _rm.GetString("PromptExitApplication_BodyText"),
                leftButtonContent: _rm.GetString("GeneralYesButtonContent"),
                rightButtonContent: _rm.GetString("GeneralNoButtonContent"),
                container: container,
                dialog: out dialog,
                actionResultHandler: actionResultHandler
            );
        }

        /// <summary>
        /// Display a prompt to save changes.
        /// This is called by any of the Pages.
        /// </summary>
        /// <param name="container">The Grid to paint gray and to go "modal" over.</param>
        public void DisplayModalSaveChangesPrompt(Grid container, out IModalWindow dialog, System.Action<DialogCloudMessageBoxViewModel> actionResultHandler)
        {
            // A page has been notified to close.  Warn the user and allow him to cancel the close.
            DisplayModalMessageBox(
                windowHeight: 250,
                leftButtonWidth: 75,
                rightButtonWidth: 75,
                title: _rm.GetString("PromptSaveChanges_Title"),
                headerText: _rm.GetString("PromptSaveChanges_HeaderText"),
                bodyText: _rm.GetString("PromptSaveChanges_BodyText"),
                leftButtonContent: _rm.GetString("GeneralYesButtonContent"),
                rightButtonContent: _rm.GetString("GeneralNoButtonContent"),
                container: container,
                dialog: out dialog,
                actionResultHandler: actionResultHandler
            );
        }

        /// <summary>
        /// Display an error message inside a grid.
        /// </summary>
        public void DisplayModalErrorMessage(string errorMessage, string title, string headerText, 
                                string rightButtonContent, Grid container,
                                out IModalWindow dialog,
                                System.Action<DialogCloudMessageBoxViewModel> actionOkButtonHandler)
        {
            _trace.writeToLog(1, "CLModalErrorDialog: DisplayModalErrorMessage:  Error: {0}.", errorMessage);

            dialog = SimpleIoc.Default.GetInstance<IModalWindow>(CLConstants.kDialogBox_CloudMessageBoxView);
            IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
            modalDialogService.ShowDialog(
                        dialog, 
                        new DialogCloudMessageBoxViewModel
                        {
                            CloudMessageBoxView_Title = title,
                            CloudMessageBoxView_WindowWidth = 450,
                            CloudMessageBoxView_WindowHeight = 230,
                            CloudMessageBoxView_HeaderText = headerText,
                            CloudMessageBoxView_BodyText = errorMessage,
                            CloudMessageBoxView_LeftButtonVisibility = Visibility.Collapsed,
                            CloudMessageBoxView_RightButtonWidth = 100,
                            CloudMessageBoxView_RightButtonMargin = new Thickness(0, 0, 0, 0),
                            CloudMessageBoxView_RightButtonContent = rightButtonContent,
                            CloudMessageBoxView_RightButtonVisibility = Visibility.Visible,
                        },
                        container,
                        actionOkButtonHandler
            );
        }

        /// <summary>
        /// Display a message box with two buttons inside a grid.
        /// </summary>
        public void DisplayModalMessageBox(int windowHeight, int leftButtonWidth, int rightButtonWidth, string title, string headerText, string bodyText, 
                                string leftButtonContent, string rightButtonContent, Grid container,
                                out IModalWindow dialog,
                                System.Action<DialogCloudMessageBoxViewModel> actionResultHandler)
        {
            _trace.writeToLog(1, "CLModalErrorDialog: DisplayModalMessageBox:  Message: {0}.", bodyText);

            dialog = SimpleIoc.Default.GetInstance<IModalWindow>(CLConstants.kDialogBox_CloudMessageBoxView);
            IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
            modalDialogService.ShowDialog(
                        dialog,
                        new DialogCloudMessageBoxViewModel
                        {
                            CloudMessageBoxView_Title = title,
                            CloudMessageBoxView_WindowWidth = 450,
                            CloudMessageBoxView_WindowHeight = windowHeight,
                            CloudMessageBoxView_HeaderText = headerText,
                            CloudMessageBoxView_BodyText = bodyText,
                            CloudMessageBoxView_LeftButtonWidth = leftButtonWidth,
                            CloudMessageBoxView_LeftButtonMargin = new Thickness(0, 0, 0, 0),
                            CloudMessageBoxView_LeftButtonContent = leftButtonContent,
                            CloudMessageBoxView_LeftButtonVisibility = Visibility.Visible,
                            CloudMessageBoxView_RightButtonWidth = rightButtonWidth,
                            CloudMessageBoxView_RightButtonMargin = new Thickness(0, 0, 30, 0),
                            CloudMessageBoxView_RightButtonContent = rightButtonContent,
                            CloudMessageBoxView_RightButtonVisibility = Visibility.Visible,
                        },
                        container,
                        actionResultHandler
            );
        }
    }
}