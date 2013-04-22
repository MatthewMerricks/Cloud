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
using CleanShutdown.Helpers;
using Cloud.Support;
using CloudApiPrivate.Common;
using System.Diagnostics;
using Cloud.Model;
using Cloud.Static;

namespace win_client.Views
{
    public partial class DialogCheckForUpdates : Window, IModalWindow
    {
        private DispatcherTimer _timer;
        private bool _isVisible = false;
        private bool _isShuttingDown = false;
        private static CLTrace _trace = CLTrace.Instance;
        private DialogCheckForUpdatesViewModel _vm = null;

        public DialogCheckForUpdates()
        {
            try
            {
                _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates constructor: Entry. Call InitializeComponent.");
                InitializeComponent();
                _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates constructor: Back from InitializeComponent.");

                Loaded += DialogCheckForUpdates_Loaded;
                Unloaded += DialogCheckForUpdates_Unloaded;
                Closing += DialogCheckForUpdates_Closing;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.PrimaryException.Message, error.PrimaryException.Code);
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (DialogCheckForUpdates).  Msg: <{0}>. Code: {1}.", error.PrimaryException.Message, error.PrimaryException.Code));
                global::System.Windows.Application.Current.Shutdown(0);
            }

            _vm = (DialogCheckForUpdatesViewModel)this.DataContext;
            _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates constructor: Exit.");
        }

        void DialogCheckForUpdates_Loaded(object sender, RoutedEventArgs e)
        {
            // Register for messages
            _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates_Loaded: Entry.");
            Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                this,
                message =>
                {
                    OnConfirmShutdownMessage(message);
                });
            CLAppMessages.Message_DialogCheckForUpdates_ShouldCheckForUpdates.Register(this, OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates);

            // Give focus to the right button.
            //TODO: The caller's should establish the focus position in a parameter.
            this.cmdOk.Focus();

            // Check for updates now.
            this.ctlAutoUpdate.BeforeChecking += ctlAutoUpdate_BeforeChecking;
            this.ctlAutoUpdate.BeforeInstalling += ctlAutoUpdate_BeforeInstalling;
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
            this.ctlAutoUpdate.CloseAppNow += ctlAutoUpdate_CloseAppNow;
            this.ctlAutoUpdate.ReadyToBeInstalled += ctlAutoUpdate_ReadyToBeInstalled;

            this.ctlAutoUpdate.Visibility = System.Windows.Visibility.Hidden;
            this.ctlAutoUpdate.KeepHidden = true;
            this.ctlAutoUpdate.UpdateType = wyDay.Controls.UpdateType.CheckAndDownload;
            this.ctlAutoUpdate.wyUpdateLocation = CLConstants.CLUpdaterRelativePath;

            // Check first.
            this.cmdCheckNow.Visibility = System.Windows.Visibility.Visible;
            this.cmdInstallNow.Visibility = System.Windows.Visibility.Collapsed;

            // Start a timer to run every second
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1.0);
            _timer.Tick += _timer_Tick;
        }

        /// <summary>
        /// The automatic updater is trying to install.  Prevent it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ctlAutoUpdate_BeforeInstalling(object sender, wyDay.Controls.BeforeArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_BeforeInstalling: Entry.  Cancel the installation.");
            e.Cancel = true;
        }

        /// <summary>
        /// The update has been downloaded and extracted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ctlAutoUpdate_ReadyToBeInstalled(object sender, EventArgs e)
        {
            this.tblkStatus.Text = String.Format("An update is ready to install.  The new update is version {0}.", this.ctlAutoUpdate.Version) +
                                    "\nThe changes are:" +
                                    String.Format("\n{0}", this.ctlAutoUpdate.Changes);

            // Hide the busy indicator
            HideBusyIndicator();
        }

        /// <summary>
        /// The user clicked Install Now.  The AutomaticUpdater is asking us to close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ctlAutoUpdate_CloseAppNow(object sender, EventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_CloseAppNow: Entry.");
            //_isShuttingDown = true;
            //ShutdownService.OverrideShutdownProtection();       // go down immediately
            //ShutdownService.RequestShutdown();
        }

        /// <summary>
        /// Timer tick callback handler.  This runs every second.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _timer_Tick(object sender, EventArgs e)
        {
            lock (_timer)
            {
                UpdateUi();
            }
        }

        /// <summary>
        /// Display the current status
        /// </summary>
        private void UpdateUi()
        {
            // Enable or disable the install button
            _trace.writeToLog(9, "DialogCheckForUpdates: UpdateUi: Entry. UpdateStepOn: {0}.", this.ctlAutoUpdate.UpdateStepOn.ToString());
            if (this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateReadyToInstall
                || this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateDownloaded)
            {
                // Enable the install button
                //_trace.writeToLog(9, "DialogCheckForUpdates: _timer_Tick: Enable the Install Now button.");
                this.cmdCheckNow.Visibility = System.Windows.Visibility.Collapsed;
                this.cmdInstallNow.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                // Check only.
                //_trace.writeToLog(9, "DialogCheckForUpdates: _timer_Tick: Enable the Check for Updates button.");
                this.cmdCheckNow.Visibility = System.Windows.Visibility.Visible;
                this.cmdInstallNow.Visibility = System.Windows.Visibility.Collapsed;
            }

            // Display the current status
            //_trace.writeToLog(9, "DialogCheckForUpdates: _timer_Tick: UpdateStepOn: {0}.", this.ctlAutoUpdate.UpdateStepOn.ToString());
            switch (this.ctlAutoUpdate.UpdateStepOn)
            {
                case wyDay.Controls.UpdateStepOn.UpdateReadyToInstall:
                case wyDay.Controls.UpdateStepOn.UpdateDownloaded:
                    this.tblkStatus.Text = String.Format("An update is ready to install.  The new update is version {0}.", this.ctlAutoUpdate.Version) +
                                            "\nThe changes are:" +
                                            String.Format("\n{0}", this.ctlAutoUpdate.Changes);
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
                    this.tblkStatus.Text = "Checking for updates...";
                    break;
            }
        }

        void ctlAutoUpdate_UpToDate(object sender, wyDay.Controls.SuccessArgs e)
        {
            // Set the status
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_UpToDate: Entry.");
            this.tblkStatus.Text = "You are currently running the latest version of Cloud.";
            
            // Hide the busy indicator
            HideBusyIndicator();
        }

        void ctlAutoUpdate_UpdateSuccessful(object sender, wyDay.Controls.SuccessArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_UpdateSuccessful: Entry.");

            // Hide the busy indicator
            HideBusyIndicator();
        }

        void ctlAutoUpdate_UpdateFailed(object sender, wyDay.Controls.FailArgs e)
        {
            // Set the status
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_UpdateFailed: Entry.");
            this.tblkStatus.Text = "The update failed:\n" + e.ErrorTitle + "\n" + e.ErrorMessage;

            // Hide the busy indicator
            HideBusyIndicator();
        }

        void ctlAutoUpdate_UpdateAvailable(object sender, EventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_UpdateAvailable: Entry.");
        }

        void ctlAutoUpdate_Loaded(object sender, RoutedEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_Loaded: Entry.");

            //this.ctlAutoUpdate.ForceCheckForUpdate(recheck: true);
        }

        void ctlAutoUpdate_DownloadingOrExtractingFailed(object sender, wyDay.Controls.FailArgs e)
        {
            // Set the status
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_DownloadingOrExtractingFailed: Entry.");
            this.tblkStatus.Text = "The download or extraction failed:\n" + e.ErrorTitle + "\n" + e.ErrorMessage;

            // Hide the busy indicator
            HideBusyIndicator();
        }

        void ctlAutoUpdate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_DataContextChanged: Entry.");
        }

        void ctlAutoUpdate_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_ContextMenuOpening: Entry.");
        }

        void ctlAutoUpdate_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_ContextMenuClosing: Entry.");
        }

        void ctlAutoUpdate_ClosingAborted(object sender, EventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_ClosingAborted: Entry.");
        }

        void ctlAutoUpdate_CheckingFailed(object sender, wyDay.Controls.FailArgs e)
        {
            // Set the status
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_CheckingFailed: Entry.");
            this.tblkStatus.Text = "The check for update failed:\n" + e.ErrorTitle + "\n" + e.ErrorMessage;

            // Hide the busy indicator
            HideBusyIndicator();
        }

        void ctlAutoUpdate_Cancelled(object sender, EventArgs e)
        {
            // Set the status
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_Cancelled: Entry.");
            this.tblkStatus.Text = "The check for update was cancelled.";

            // Hide the busy indicator
            HideBusyIndicator();
        }

        void ctlAutoUpdate_BeforeDownloading(object sender, wyDay.Controls.BeforeArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_BeforeDownloading: Entry.");
        }

        void ctlAutoUpdate_BeforeChecking(object sender, wyDay.Controls.BeforeArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: ctlAutoUpdate_BeforeChecking: Entry.");
            this.tblkStatus.Text = "Checking for updates...";
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void DialogCheckForUpdates_Unloaded(object sender, RoutedEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates_Unloaded: Entry.");
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
            // Allow the shutdown if we requested it.
            _trace.writeToLog(9, "DialogCheckForUpdates: OnConfirmShutdownMessage: Entry.");
            if (_isShuttingDown)
            {
                _trace.writeToLog(9, "DialogCheckForUpdates: OnConfirmShutdownMessage: Allow the shutdown.");
                message.Execute(false);      // false == allow the shutdown
                return;
            }
            
            // Handle the preferences case.
            if (_isVisible)
            {
                if (message.Notification == Notifications.ConfirmShutdown)
                {
                    _trace.writeToLog(9, "DialogCheckForUpdates: OnConfirmShutdownMessage: Abort shutdown.");
                    message.Execute(true);      // true == abort shutdown
                }

                if (message.Notification == Notifications.QueryModalDialogsActive)
                {
                    _trace.writeToLog(9, "DialogCheckForUpdates: OnConfirmShutdownMessage: Abort shutdown(2).");
                    message.Execute(true);      // a modal dialog is active
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: OKButton_Click: Entry.");
            this.tblkStatus.Text = "";
            this.Left = Int32.MaxValue;
            this.Top = Int32.MaxValue;
            this.ShowInTaskbar = false;
            _isVisible = false;
        }

        /// <summary>
        /// The user clicked a "Check for Updates" button on the systray icon or on the FramePreferencesGeneral page.
        /// </summary>
        /// <param name="obj"></param>
        private void OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates(string obj)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates: Entry.");
            _isVisible = true;

            // Update the UI with the current AutoUpdater status.
            UpdateUi();

            // We may already have an update ready to install.  Set the proper button status.
            if (this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateReadyToInstall
                || this.ctlAutoUpdate.UpdateStepOn == wyDay.Controls.UpdateStepOn.UpdateDownloaded)
            {
                // We are ready to install an update.  Let the user take the action.  Do nothing here.
            }
            else
            {
                // Start a check automatically
                _trace.writeToLog(9, "DialogCheckForUpdates: OnMessage_DialogCheckForUpdates_ShouldCheckForUpdates: Automatically click the Check Now button.");
                ButtonCheckNow_Click(null, null);
            }
        }

        /// <summary>
        /// The user clicked the Check Now button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCheckNow_Click(object sender, RoutedEventArgs e)
        {
            // Set the status to something known at first.  We will force a check which should update the status pretty quickly.
            _trace.writeToLog(9, "DialogCheckForUpdates: ButtonCheckNow_Click: Entry.");
            this.tblkStatus.Text = "Checking for updates...";

            // Record the time of the last update check
            Settings.Instance.DateWeLastCheckedForSoftwareUpdate = DateTime.Now;

            // Show the busy indicator
            ShowBusyIndicator("Checking for updates...");

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
            _trace.writeToLog(9, "DialogCheckForUpdates: ButtonInstallNow_Click: Entry.");

            //TODO: Design changed to use a .vbs script (below).this.ctlAutoUpdate.InstallNow();

            // Asynchronously launch another process running a VBScript which will:
            //   o Wait for Cloud.exe to exit.  Just continue if it takes too long.
            //   o Kill Explorer if it is running, and wait for it to completely exit.  Just continue if it takes too long.
            //   o Launch another process with CloudUpdater.exe, which will perform the update (or not).
            //   o Restart Explorer and wait for its process to appear.
            //   o Wait for Explorer to be ready (how? Start with a time delay)
            //   o Re-launch Cloud.exe
            // Exit this instance Cloud.exe as quickly as possible, with no chance of the user stopping it.
            StartCloudUpdaterAndExitNow();
        }

        /// <summary>
        // Asynchronously launch another process running a VBScript which will:
        ///   o Wait for Cloud.exe to exit.  Just continue if it takes too long.
        ///   o Kill Explorer if it is running, and wait for it to completely exit.  Just continue if it takes too long.
        ///   o Launch another process with CloudUpdater.exe, which will perform the update (or not).
        ///   o Restart Explorer and wait for its process to appear.
        ///   o Wait for Explorer to be ready (how? Start with a time delay)
        ///   o Re-launch Cloud.exe
        /// Exit this instance Cloud.exe as quickly as possible, with no chance of the user stopping it.
        /// </summary>
        private void StartCloudUpdaterAndExitNow()
        {
            try
            {
                // Stream the CloudInstallUpdate.vbs file out to the user's temp directory
                // Locate the user's temp directory.
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Entry.");
                string userTempDirectory = System.IO.Path.GetTempPath(); 
                string vbsPath = userTempDirectory + "CloudInstallUpdate.vbs";

                // Get the assembly containing the .vbs resource.
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Get the assembly containing the .vbs resource.");
                System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(global::win_client.Views.DialogCheckForUpdates));
                if (storeAssembly == null)
                {
                    _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: ERROR: storeAssembly null.");
                    return;
                }

                // Stream the CloudInstallUpdate.vbs file out to the temp directory
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Call WriteResourceFileToFilesystemFile.");
                int rc = Helpers.WriteResourceFileToFilesystemFile(storeAssembly, "CloudInstallUpdate", vbsPath);
                if (rc != 0)
                {
                    _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: ERROR: From WriteResourceFileToFilesystemFile. rc: {0}.", rc + 100);
                    return;
                }

                // Now we will create a new process to run the VBScript file.
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Build the paths for launching the VBScript file.");
                string systemFolderPath = Helpers.Get32BitSystemFolderPath();
                string cscriptPath = systemFolderPath + "\\cscript.exe";
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Cscript executable path: <{0}>.", cscriptPath);

                string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @"""";
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Launch the VBScript file.  Launch: <{0}>.", argumentsString);

                // Launch the process
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = cscriptPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = argumentsString;
                Process.Start(startInfo);

                // Now exit this app quickly
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Shut down now quickly.");
                _isShuttingDown = true;
                ShutdownService.OverrideShutdownProtection();       // go down immediately
                ShutdownService.RequestShutdown();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: ERROR: Exception. Msg: {0}.", ex.Message);
            }

            _trace.writeToLog(1, "DialogCheckForUpdates: StartCloudUpdaterAndExitNow: Exit successfully.");
        }

        private void DialogCheckForUpdates_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trace.writeToLog(9, "DialogCheckForUpdates: DialogCheckForUpdates_Closing: Entry.");
            this.tblkStatus.Text = "";
            this.Left = -10000;
            this.Top = -10000;
            this.ShowInTaskbar = false;
            _isVisible = false;

            e.Cancel = true;
        }

        /// <summary>
        /// Show the busy indicator while we talk with the server
        /// </summary>
        /// <param name="message">This is the message to display in the busy indicator.</param>
        private void ShowBusyIndicator(string message)
        {
            if (_vm != null)
            {
                _vm.IsBusy = true;
                _vm.BusyContent = message;
            }

            // Start the timer
            lock (_timer)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Hide the busy indicator
        /// </summary>
        /// <param name="message">This is the message to display in the busy indicator.</param>
        private void HideBusyIndicator()
        {
            if (_vm != null)
            {
                _vm.IsBusy = false;
            }

            // Stop the timer
            lock (_timer)
            {
                _timer.Stop();
            }

            // Display the current status.
            UpdateUi();
        }
    }
}
