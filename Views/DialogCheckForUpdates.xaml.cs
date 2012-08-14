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

namespace win_client.Views
{
    public partial class DialogCheckForUpdates : Window, IModalWindow
    {
        private DispatcherTimer _timer;

        public DialogCheckForUpdates()
        {
            InitializeComponent();

            Loaded +=DialogCheckForUpdates_Loaded;
            Unloaded += DialogCheckForUpdates_Unloaded;

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
            this.ctlAutoUpdate.UpdateType = wyDay.Controls.UpdateType.OnlyCheck;

            // Disable install buttons
            this.btnInstallAtNextStart.Visibility = System.Windows.Visibility.Collapsed;
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
            // Enable or disable the install buttons
            if (this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateReadyToInstall ||
                this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateAvailable ||
                this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateDownloaded)
            {
                // Enable the install buttons
                this.btnInstallAtNextStart.Visibility = System.Windows.Visibility.Visible;
                this.btnInstallAtNextStart.IsEnabled = true;
                this.btnInstallNow.Visibility = System.Windows.Visibility.Visible;
                this.btnInstallNow.IsEnabled = true;
            }
            else
            {
                // Disable install buttons
                this.btnInstallAtNextStart.IsEnabled = false;
                this.btnInstallNow.IsEnabled = false;
                this.btnInstallAtNextStart.Visibility = System.Windows.Visibility.Collapsed;
                this.btnInstallNow.Visibility = System.Windows.Visibility.Collapsed;
            }

            // Display the current status
            switch (this.ctlAutoUpdate.UpdateStepOn)
            {
                case wyDay.Controls.UpdateStepOn.UpdateReadyToInstall:
                    this.tblkStatus.Text = String.Format("An update is ready to install.  The new update is version {0}.", this.ctlAutoUpdate.Version) +
                                            "\n\rThe changes are:" +
                                            String.Format("\n\r{0}", this.ctlAutoUpdate.Changes);
                    break;
                case wyDay.Controls.UpdateStepOn.UpdateDownloaded:
                    this.tblkStatus.Text = "The available update has been downloaded.";
                    break;
                case wyDay.Controls.UpdateStepOn.UpdateAvailable:
                    this.tblkStatus.Text = "An update is available.  It will be automatically downloaded.";
                    break;
                case wyDay.Controls.UpdateStepOn.Nothing:
                    this.tblkStatus.Text = "";
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
            // Check for an update.
            this.tblkEvent.Text = "";
            this.tblkStatus.Text = "";
            this.ctlAutoUpdate.ForceCheckForUpdate(recheck: true);
        }

        void ctlAutoUpdate_UpToDate(object sender, wyDay.Controls.SuccessArgs e)
        {
            this.tblkEvent.Text = "Event: Up-to-date";

            // Set the status
            this.tblkStatus.Text = "You are currently running the latest version.";
        }

        void ctlAutoUpdate_UpdateSuccessful(object sender, wyDay.Controls.SuccessArgs e)
        {
            this.tblkEvent.Text = "Event: Update successful";
        }

        void ctlAutoUpdate_UpdateFailed(object sender, wyDay.Controls.FailArgs e)
        {
            this.tblkEvent.Text = "Event: Update failed";

            // Set the status
            this.tblkStatus.Text = "The update failed:\n\r" + e.ErrorTitle + "\n\r" + e.ErrorMessage;
        }

        void ctlAutoUpdate_UpdateAvailable(object sender, EventArgs e)
        {
            this.tblkEvent.Text = "Event: Update available";
        }

        void ctlAutoUpdate_Loaded(object sender, RoutedEventArgs e)
        {
            this.tblkEvent.Text = "Event: Loaded";
        }

        void ctlAutoUpdate_DownloadingOrExtractingFailed(object sender, wyDay.Controls.FailArgs e)
        {
            this.tblkEvent.Text = "Event: DownloadingOrExtractingFailed";

            // Set the status
            this.tblkStatus.Text = "The download or extraction failed:\n\r" + e.ErrorTitle + "\n\r" + e.ErrorMessage;
        }

        void ctlAutoUpdate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.tblkEvent.Text = "Event: DataContextChanged";
        }

        void ctlAutoUpdate_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            this.tblkEvent.Text = "Event: ContextMenuOpening";
        }

        void ctlAutoUpdate_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            this.tblkEvent.Text = "Event: ContextMenuClosing";
        }

        void ctlAutoUpdate_ClosingAborted(object sender, EventArgs e)
        {
            this.tblkEvent.Text = "Event: ClosingAborted";
        }

        void ctlAutoUpdate_CheckingFailed(object sender, wyDay.Controls.FailArgs e)
        {
            this.tblkEvent.Text = "Event: CheckingFailed";

            // Set the status
            this.tblkStatus.Text = "The check for update failed:\n\r" + e.ErrorTitle + "\n\r" + e.ErrorMessage;
        }

        void ctlAutoUpdate_Cancelled(object sender, EventArgs e)
        {
            this.tblkEvent.Text = "Event: Cancelled";

            // Set the status
            this.tblkStatus.Text = "The check for update was cancelled.";
        }

        void ctlAutoUpdate_BeforeDownloading(object sender, wyDay.Controls.BeforeArgs e)
        {
            this.tblkEvent.Text = "Event: Downloading";
        }

        void ctlAutoUpdate_BeforeChecking(object sender, wyDay.Controls.BeforeArgs e)
        {
            this.tblkEvent.Text = "Event: BeforeChecking";
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
            if (message.Notification == Notifications.ConfirmShutdown)
            {
                message.Execute(true);      // true == abort shutdown
            }

            if (message.Notification == Notifications.QueryModalDialogsActive)
            {
                message.Execute(true);      // a modal dialog is active
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.tblkEvent.Text = "";
            this.tblkStatus.Text = "";
            this.Left = Int32.MaxValue;
            this.Top = Int32.MaxValue;
            this.ShowInTaskbar = false;
        }

        private void ButtonCheckNow_Click(object sender, RoutedEventArgs e)
        {
            this.tblkEvent.Text = "";
            this.tblkStatus.Text = "";
            this.ctlAutoUpdate.ForceCheckForUpdate(recheck: true);
        }

        private void ButtonInstallNow_Click(object sender, RoutedEventArgs e)
        {
            this.tblkEvent.Text = "";
            this.tblkStatus.Text = "";
            this.ctlAutoUpdate.InstallNow();
        }

        private void ButtonInstallAtNextStart_Click(object sender, RoutedEventArgs e)
        {
            this.tblkEvent.Text = "";
            this.tblkStatus.Text = "";
            this.Left = Int32.MaxValue;
            this.Top = Int32.MaxValue;
            this.ShowInTaskbar = false;
        }
    }
}
