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

namespace win_client.Views
{
    public partial class DialogCheckForUpdates : Window, IModalWindow
    {
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

            // Disable install buttons
            this.btnInstallAtNextStart.Visibility = System.Windows.Visibility.Collapsed;
            this.btnInstallNow.Visibility = System.Windows.Visibility.Collapsed;

            // Check for an update.
            this.ctlAutoUpdate.ForceCheckForUpdate(recheck: true);
        }

        private void OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates(string obj)
        {
            // Disable install buttons
            this.btnInstallAtNextStart.Visibility = System.Windows.Visibility.Collapsed;
            this.btnInstallNow.Visibility = System.Windows.Visibility.Collapsed;

            // Check for an update.
            this.tblkEvent.Text = "";
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
            this.btnInstallAtNextStart.Visibility = System.Windows.Visibility.Visible;
            this.btnInstallNow.Visibility = System.Windows.Visibility.Visible;

            // Set the status
            this.tblkStatus.Text = "An update is available with the following changes:\n\r" + this.ctlAutoUpdate.Changes;
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
