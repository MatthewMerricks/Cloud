//
//  DialogCheckForUpdates.xaml.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using Dialog.Abstractions.Wpf.Intefaces;
using Xceed.Wpf.Toolkit;
using CleanShutdown.Messaging;
using System.Windows.Threading;
using CloudApiPrivate.Model.Settings;

namespace win_client.Views
{
    public partial class DialogCheckForUpdates : Window, IModalWindow
    {
        private DispatcherTimer _timer;
        private bool _isVisible = false;

        public DialogCheckForUpdates()
        {
            InitializeComponent();

            Loaded +=DialogCheckForUpdates_Loaded;
            Unloaded += DialogCheckForUpdates_Unloaded;
            Closing += DialogCheckForUpdates_Closing;

        }

        void DialogCheckForUpdates_Loaded(object sender, RoutedEventArgs e)
        {
            // Register for messages
            Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                this,
                message =>
                {
                    OnConfirmShutdownMessage(message);
                });
            CLAppMessages.Message_DialogCheckForUpdates_ShouldCheckForUpdates.Register(this, OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates);

            // Give focus to the right button.
            //TODO: The caller's should establish the focus position in a parameter.
            this.btnOk.Focus();

            // Check for updates now.
            this.ctlAutoUpdate.Visibility = System.Windows.Visibility.Hidden;
            this.ctlAutoUpdate.BeforeChecking += ctlAutoUpdate_BeforeChecking;
            this.ctlAutoUpdate.BeforeDownloading += ctlAutoUpdate_BeforeDownloading;
            this.ctlAutoUpdate.Cancelled += ctlAutoUpdate_Cancelled;
            this.ctlAutoUpdate.CheckingFailed += ctlAutoUpdate_CheckingFailed;
            this.ctlAutoUpdate.ClosingAborted += ctlAutoUpdate_ClosingAborted;
            this.ctlAutoUpdate.ContextMenuClosing += ctlAutoUpdate_ContextMenuClosing;
            this.ctlAutoUpdate.ContextMenuOpening += ctlAutoUpdate_ContextMenuOpening;
            this.ctlAutoUpdate.DataContextChanged += ctlAutoUpdate_DataContextChanged;
            this.ctlAutoUpdate.DownloadingOrExtractingFailed += ctlAutoUpdate_DownloadingOrExtractingFailed;
            this.ctlAutoUpdate.Loaded += ctlAutoUpdate_Loaded;
            this.ctlAutoUpdate.UpdateAvailable += ctlAutoUpdate_UpdateAvailable;
            this.ctlAutoUpdate.UpdateFailed += ctlAutoUpdate_UpdateFailed;
            this.ctlAutoUpdate.UpdateSuccessful += ctlAutoUpdate_UpdateSuccessful;
            this.ctlAutoUpdate.UpToDate += ctlAutoUpdate_UpToDate;
            this.ctlAutoUpdate.KeepHidden = true;
            this.ctlAutoUpdate.UpdateType = wyDay.Controls.UpdateType.Automatic;

            // Check first.
            this.btnCheckNow.Visibility = System.Windows.Visibility.Visible;
            this.btnInstallNow.Visibility = System.Windows.Visibility.Collapsed;

            // Start a timer to run every second
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1.0);
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        /// <summary>
        /// Timer tick callback handler.  This runs every second.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _timer_Tick(object sender, EventArgs e)
        {
            // Enable or disable the install button
            if (this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateReadyToInstall)
            {
                // Enable the install button
                this.btnCheckNow.Visibility = System.Windows.Visibility.Collapsed;
                this.btnInstallNow.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                // Check only.
                this.btnCheckNow.Visibility = System.Windows.Visibility.Visible;
                this.btnInstallNow.Visibility = System.Windows.Visibility.Collapsed;
            }

            // Display the current status
            switch (this.ctlAutoUpdate.UpdateStepOn)
            {
                case wyDay.Controls.UpdateStepOn.UpdateReadyToInstall:
                    this.tblkStatus.Text = String.Format("An update is ready to install.  The new update is version {0}.", this.ctlAutoUpdate.Version) +
                                            "\nThe changes are:" +
                                            String.Format("\n{0}", this.ctlAutoUpdate.Changes);
                    break;
                case wyDay.Controls.UpdateStepOn.UpdateDownloaded:
                    this.tblkStatus.Text = "The available update has been downloaded.";
                    break;
                case wyDay.Controls.UpdateStepOn.UpdateAvailable:
                    this.tblkStatus.Text = "An update is available.";
                    break;
                case wyDay.Controls.UpdateStepOn.Nothing:
                    //this.tblkStatus.Text = "No Status.";
                    break;
                case wyDay.Controls.UpdateStepOn.ExtractingUpdate:
                    this.tblkStatus.Text = "The available update has been downloaded and is being prepared.";
                    break;
                case wyDay.Controls.UpdateStepOn.DownloadingUpdate:
                    this.tblkStatus.Text = "The available update is being downloaded.";
                    break;
                case wyDay.Controls.UpdateStepOn.Checking:
                    break;
            }
        }

        /// <summary>
        /// The user clicked a "Check for Updates" button on the systray icon or on the FramePreferencesGeneral page.
        /// </summary>
        /// <param name="obj"></param>
        private void OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates(string obj)
        {
            _isVisible = true;

            // Set the status to something known at first.  We will force a check which should update the status pretty quickly.
            this.tblkStatus.Text = "Checking for updates...";

            // Record the time of the last update check
            Settings.Instance.DateWeLastCheckedForSoftwareUpdate = DateTime.Now;

            // Check for an update.
            this.ctlAutoUpdate.ForceCheckForUpdate(recheck: true);
        }

        void ctlAutoUpdate_UpToDate(object sender, wyDay.Controls.SuccessArgs e)
        {
            // Set the status
            this.tblkStatus.Text = "You are currently running the latest version of Cloud.";
        }

        void ctlAutoUpdate_UpdateSuccessful(object sender, wyDay.Controls.SuccessArgs e)
        {
        }

        void ctlAutoUpdate_UpdateFailed(object sender, wyDay.Controls.FailArgs e)
        {
            // Set the status
            this.tblkStatus.Text = "The update failed:\n" + e.ErrorTitle + "\n" + e.ErrorMessage;
        }

        void ctlAutoUpdate_UpdateAvailable(object sender, EventArgs e)
        {
        }

        void ctlAutoUpdate_Loaded(object sender, RoutedEventArgs e)
        {
        }

        void ctlAutoUpdate_DownloadingOrExtractingFailed(object sender, wyDay.Controls.FailArgs e)
        {
            // Set the status
            this.tblkStatus.Text = "The download or extraction failed:\n" + e.ErrorTitle + "\n" + e.ErrorMessage;
        }

        void ctlAutoUpdate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        void ctlAutoUpdate_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        }

        void ctlAutoUpdate_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
        }

        void ctlAutoUpdate_ClosingAborted(object sender, EventArgs e)
        {
        }

        void ctlAutoUpdate_CheckingFailed(object sender, wyDay.Controls.FailArgs e)
        {
            // Set the status
            this.tblkStatus.Text = "The check for update failed:\n" + e.ErrorTitle + "\n" + e.ErrorMessage;
        }

        void ctlAutoUpdate_Cancelled(object sender, EventArgs e)
        {
            // Set the status
            this.tblkStatus.Text = "The check for update was cancelled.";
        }

        void ctlAutoUpdate_BeforeDownloading(object sender, wyDay.Controls.BeforeArgs e)
        {
        }

        void ctlAutoUpdate_BeforeChecking(object sender, wyDay.Controls.BeforeArgs e)
        {
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void DialogCheckForUpdates_Unloaded(object sender, RoutedEventArgs e)
        {
            base.Close();

            // Unregister for messages
            Messenger.Default.Unregister(this);
        }

        /// <summary>
        /// The user clicked the 'X' on the NavigationWindow.  That sent a ConfirmShutdown message.
        /// This is a modal dialog.  Prevent the close.
        /// </summary>
        private void OnConfirmShutdownMessage(CleanShutdown.Messaging.NotificationMessageAction<bool> message)
        {
            if (_isVisible)
            {
                if (message.Notification == Notifications.ConfirmShutdown)
                {
                    message.Execute(true);      // true == abort shutdown
                }

                if (message.Notification == Notifications.QueryModalDialogsActive)
                {
                    message.Execute(true);      // a modal dialog is active
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.tblkStatus.Text = "";
            this.Left = Int32.MaxValue;
            this.Top = Int32.MaxValue;
            this.ShowInTaskbar = false;
            _isVisible = false;
        }

        private void ButtonCheckNow_Click(object sender, RoutedEventArgs e)
        {
            // Set the status to something known at first.  We will force a check which should update the status pretty quickly.
            this.tblkStatus.Text = "Checking for updates...";

            // Record the time of the last update check
            Settings.Instance.DateWeLastCheckedForSoftwareUpdate = DateTime.Now;

            this.ctlAutoUpdate.ForceCheckForUpdate(recheck: true);
        }

        //private void ButtonInstallAtNextStart_Click(object sender, RoutedEventArgs e)
        //{
        //    this.tblkStatus.Text = "";
        //    this.Left = Int32.MaxValue;
        //    this.Top = Int32.MaxValue;
        //    this.ShowInTaskbar = false;
        //}

        private void ButtonInstallNow_Click(object sender, RoutedEventArgs e)
        {
            this.ctlAutoUpdate.InstallNow();
        }

        private void DialogCheckForUpdates_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.tblkStatus.Text = "";
            this.Left = Int32.MaxValue;
            this.Top = Int32.MaxValue;
            this.ShowInTaskbar = false;
            _isVisible = false;

            e.Cancel = true;
        }

    }
}
