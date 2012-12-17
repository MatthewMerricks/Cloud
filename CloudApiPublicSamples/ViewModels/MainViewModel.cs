﻿using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
using CloudApiPublicSamples.Models;
using CloudApiPublicSamples.Support;
using CloudApiPublicSamples.Views;
using CloudApiPublicSamples.Static;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CloudApiPublic;
using CloudApiPublic.Model;

namespace CloudApiPublicSamples.ViewModels
{
    public class MainViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // RelayCommands
        RelayCommand _commandBrowseSyncBoxFolder;
        RelayCommand _commandShowAdvancedOptions;
        RelayCommand _commandSaveSettings;
        RelayCommand _commandShowSyncStatus;
        RelayCommand _commandStartSyncing;
        RelayCommand _commandStopSyncing;
        RelayCommand _commandExit;

        // Private fields
        private Settings _settingsCurrent = null;
        private Settings _settingsInitial = null;
        private Window _mainWindow = null;
        private bool _syncStarted = false;
        private bool _windowClosed = false;
        private CLSync _syncBox = null;
        private readonly object _locker = new object();

        #endregion

        #region Events

        public event EventHandler<NotificationEventArgs> NotifyBrowseSyncBoxFolder;
        public event EventHandler<NotificationEventArgs<string, bool>> NotifySettingsChanged;
        public event EventHandler<NotificationEventArgs<CLError>> NotifyException;
		 
	    #endregion

        #region Constructors

        public MainViewModel(Window mainWindow)
        {
            if (mainWindow == null)
            {
                throw new Exception("mainWindow must not be null");
            }
            _mainWindow = mainWindow;

            // Read in the settings
            _settingsCurrent = new Settings();
            _settingsInitial = new Settings();
            _settingsCurrent.GetSavedSettings();
            _settingsInitial.GetSavedSettings();
        }

        #endregion

        #region Model Properties

        public string SyncRoot
        {
            get { return _settingsCurrent.SyncBoxFullPath; }
            set
            {
                if (value == _settingsCurrent.SyncBoxFullPath)
                {
                    return;
                }

                _settingsCurrent.SyncBoxFullPath = value;

                base.OnPropertyChanged("SyncRoot");
            }
        }

        public string AppKey
        {
            get { return _settingsCurrent.ApplicationKey; }
            set
            {
                if (value == _settingsCurrent.ApplicationKey)
                {
                    return;
                }

                _settingsCurrent.ApplicationKey = value;

                base.OnPropertyChanged("AppKey");
            }
        }

        public string AppSecret
        {
            get { return _settingsCurrent.ApplicationSecret; }
            set
            {
                if (value == _settingsCurrent.ApplicationSecret)
                {
                    return;
                }

                _settingsCurrent.ApplicationSecret = value;

                base.OnPropertyChanged("AppSecret");
            }
        }

        public string SyncBoxId
        {
            get { return _settingsCurrent.SyncBoxId; }
            set
            {
                if (value == _settingsCurrent.SyncBoxId)
                {
                    return;
                }

                _settingsCurrent.SyncBoxId = value;

                base.OnPropertyChanged("SyncBoxId");
            }
        }

        public string DeviceId
        {
            get { return _settingsCurrent.UniqueDeviceId; }
            set
            {
                if (value == _settingsCurrent.UniqueDeviceId)
                {
                    return;
                }

                _settingsCurrent.UniqueDeviceId = value;

                base.OnPropertyChanged("DeviceId");
            }
        }

        #endregion

        #region Focus Properties

        public bool IsSyncBoxPathFocused
        {
            get { return _isSyncBoxPathFocused; }
            set
            {
                if (value == _isSyncBoxPathFocused)
                {
                    _isSyncBoxPathFocused = false;
                    base.OnPropertyChanged("IsSyncBoxPathFocused");
                }

                _isSyncBoxPathFocused = value;
                base.OnPropertyChanged("IsSyncBoxPathFocused");
            }
        }
        private bool _isSyncBoxPathFocused;

        public bool IsAppKeyFocused
        {
            get { return _isAppKeyFocused; }
            set
            {
                if (value == _isAppKeyFocused)
                {
                    _isAppKeyFocused = false;
                    base.OnPropertyChanged("IsAppKeyFocused");
                }

                _isAppKeyFocused = value;
                base.OnPropertyChanged("IsAppKeyFocused");
            }
        }
        private bool _isAppKeyFocused;

        public bool IsAppSecretFocused
        {
            get { return _isAppSecretFocused; }
            set
            {
                if (value == _isAppSecretFocused)
                {
                    _isAppSecretFocused = false;
                    base.OnPropertyChanged("IsAppSecretFocused");
                }

                _isAppSecretFocused = value;
                base.OnPropertyChanged("IsAppSecretFocused");
            }
        }
        private bool _isAppSecretFocused;

        public bool IsSyncBoxIdFocused
        {
            get { return _isSyncBoxIdFocused; }
            set
            {
                if (value == _isSyncBoxIdFocused)
                {
                    _isSyncBoxIdFocused = false;
                    base.OnPropertyChanged("IsSyncBoxIdFocused");
                }

                _isSyncBoxIdFocused = value;
                base.OnPropertyChanged("IsSyncBoxIdFocused");
            }
        }
        private bool _isSyncBoxIdFocused;

        public bool IsDeviceIdFocused
        {
            get { return _isDeviceIdFocused; }
            set
            {
                if (value == _isDeviceIdFocused)
                {
                    _isDeviceIdFocused = false;
                    base.OnPropertyChanged("IsDeviceIdFocused");
                }

                _isDeviceIdFocused = value;
                base.OnPropertyChanged("IsDeviceIdFocused");
            }
        }
        private bool _isDeviceIdFocused;

        #endregion

        #region Presentation Properties

        /// <summary>
        /// Returns a command that browses to select a SyncBox folder.
        /// </summary>
        public ICommand CommandBrowseSyncBoxFolder
        {
            get
            {
                if (_commandBrowseSyncBoxFolder == null)
                {
                    _commandBrowseSyncBoxFolder = new RelayCommand(
                        param => this.BrowseSyncBoxFolder(),
                        param => { return true; }
                        );
                }
                return _commandBrowseSyncBoxFolder;
            }
        }

        /// <summary>
        /// Returns a command that selects advanced options.
        /// </summary>
        public ICommand CommandShowAdvancedOptions
        {
            get
            {
                if (_commandShowAdvancedOptions == null)
                {
                    _commandShowAdvancedOptions = new RelayCommand(
                        param => this.ShowAdvancedOptions(),
                        param => { return true; }
                        );
                }
                return _commandShowAdvancedOptions;
            }
        }

        /// <summary>
        /// Returns a command that saves the user's settings.
        /// </summary>
        public ICommand CommandSaveSettings
        {
            get
            {
                if (_commandSaveSettings== null)
                {
                    _commandSaveSettings = new RelayCommand(
                        param => this.SaveSettings(),
                        param => this.CanSaveSettings
                        );
                }
                return _commandSaveSettings;
            }
        }

        /// <summary>
        /// Returns a command that shows the Sync Status window.
        /// </summary>
        public ICommand CommandShowSyncStatus
        {
            get
            {
                if (_commandShowSyncStatus == null)
                {
                    _commandShowSyncStatus = new RelayCommand(
                        param => this.ShowSyncStatus(),
                        param => this.CanShowSyncStatus
                        );
                }
                return _commandShowSyncStatus;
            }
        }

        /// <summary>
        /// Returns a command that starts syncing the SyncBox.
        /// </summary>
        public ICommand CommandStartSyncing
        {
            get
            {
                if (_commandStartSyncing== null)
                {
                    _commandStartSyncing = new RelayCommand(
                        param => this.StartSyncing(),
                        param => this.CanStartSyncing
                        );
                }
                return _commandStartSyncing;
            }
        }

        /// <summary>
        /// Returns a command that stops syncing the SyncBox.
        /// </summary>
        public ICommand CommandStopSyncing
        {
            get
            {
                if (_commandStopSyncing == null)
                {
                    _commandStopSyncing = new RelayCommand(
                        param => this.StopSyncing(),
                        param => this.CanStopSyncing
                        );
                }
                return _commandStopSyncing;
            }
        }

        /// <summary>
        /// Returns a command that exits the application.
        /// </summary>
        public ICommand CommandExit
        {
            get
            {
                if (_commandExit == null)
                {
                    _commandExit = new RelayCommand(
                        param => this.Exit(),
                        param => { return true; }
                        );
                }
                return _commandExit;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Browse to locate a folder to be synced.
        /// </summary>
        public void BrowseSyncBoxFolder()
        {
            // Notify the view to put up the folder selector.
            NotifyBrowseSyncBoxFolder(this, new NotificationEventArgs());
        }

        /// <summary>
        /// Show the advanced options dialog.
        /// </summary>
        public void ShowAdvancedOptions()
        {
            // Show the advanced options as a modal dialog.
            AdvancedOptionsView viewWindow = new AdvancedOptionsView();
            viewWindow.Owner = _mainWindow;
            viewWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            viewWindow.ResizeMode = ResizeMode.NoResize;

            // Create the ViewModel to which the view binds.
            var viewModel = new AdvancedOptionsViewModel(_settingsCurrent);

            // When the ViewModel asks to be closed, close the window.
            EventHandler handler = null;
            handler = delegate
            {
                viewModel.RequestClose -= handler;
                viewWindow.Close();
            };
            viewModel.RequestClose += handler;

            // Allow all controls in the window to bind to the ViewModel by setting the 
            // DataContext, which propagates down the element tree.
            viewWindow.DataContext = viewModel;

            // Show the dialog.
            Dispatcher dispatcher = Application.Current.Dispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            {
                ((Window)viewWindow).ShowDialog();
            });
        }

        /// <summary>
        /// Save the settings entered so far.
        /// </summary>
        public void SaveSettings()
        {
            // Validate the SyncBox full path.
            if (String.IsNullOrEmpty(SyncRoot) ||
                !Directory.Exists(SyncRoot))
            {
                MessageBox.Show("The SyncBox Folder must be the full path of a valid directory.");
                this.IsSyncBoxPathFocused = true;
                return;
            }

            // Validate the App Key.
            if (String.IsNullOrEmpty(AppKey) ||
                !OnlyHexInString(AppKey) ||
                 AppKey.Length != 64)
            {
                MessageBox.Show("The Application Key must be a 64 character long string with only hexadecimal characters.");
                this.IsAppKeyFocused = true;
                return;
            }

            // Validate the App Secret.
            if (String.IsNullOrEmpty(AppSecret) ||
                !OnlyHexInString(AppSecret) ||
                 AppSecret.Length != 64)
            {
                MessageBox.Show("The Application Secret must be a 64 character long string with only hexadecimal characters.");
                this.IsAppSecretFocused = true;
                return;
            }

            // Validate the SyncBox ID.
            ulong value;
            if (String.IsNullOrEmpty(SyncBoxId) ||
                !Utilities.ConvertStringToUlong(SyncBoxId, out value) ||
                value == 0)
            {
                MessageBox.Show("The SyncBox ID must be a positive decimal number convertible to an unsigned integer <= 18446744073709551615.");
                this.IsSyncBoxIdFocused = true;
                return;
            }

            // Validate the Device ID.
            if (String.IsNullOrEmpty(DeviceId) ||
                !Utilities.ConvertStringToUlong(DeviceId, out value) ||
                value == 0)
            {
                MessageBox.Show("The Device ID must be a positive decimal number convertible to an unsigned integer <= 18446744073709551615.");
                this.IsDeviceIdFocused = true;
                return;
            }

            // Save the values to Settings
            Properties.Settings.Default.SyncBoxFullPath = SyncRoot;
            Properties.Settings.Default.ApplicationKey = AppKey;
            Properties.Settings.Default.ApplicationSecret = AppSecret;
            Properties.Settings.Default.SyncBoxId = SyncBoxId;
            Properties.Settings.Default.UniqueDeviceId = DeviceId;
            Properties.Settings.Default.TempDownloadFolderFullPath = _settingsCurrent.TempDownloadFolderFullPath;
            Properties.Settings.Default.DatabaseFolderFullPath = _settingsCurrent.DatabaseFolderFullPath;
            Properties.Settings.Default.LogErrors = _settingsCurrent.LogErrors;
            Properties.Settings.Default.TraceType = _settingsCurrent.TraceType;
            Properties.Settings.Default.TraceFolderFullPath = _settingsCurrent.TraceFolderFullPath;
            Properties.Settings.Default.TraceExcludeAuthorization = _settingsCurrent.TraceExcludeAuthorization;
            Properties.Settings.Default.TraceLevel = _settingsCurrent.TraceLevel;
            Properties.Settings.Default.Save();

            _settingsInitial = new Settings(_settingsCurrent);          // Saved.  Initial is now current.
        }

        /// <summary>
        /// Show the Sync Status window.
        /// </summary>
        public void ShowSyncStatus()
        {
            MessageBox.Show("ShowSyncStatus");
        }

        /// <summary>
        /// Start syncing the SyncBox.
        /// </summary>
        public void StartSyncing()
        {
            bool startSyncBox = false;
            lock (_locker)
            {
                if (_syncBox == null)
                {
                    _syncBox = new CLSync();
                    startSyncBox = true;
                }
            }

            if (startSyncBox)
            {
                CLSyncStartStatus startStatus;
                CLError errorFromSyncBoxStart = _syncBox.Start((ISyncSettings)_settingsInitial, out startStatus);
                if (errorFromSyncBoxStart != null)
                {
                    _syncBox = null;
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = errorFromSyncBoxStart, Message = String.Format("Error starting the SyncBox: {0}.", startStatus.ToString()) });
                }
                else
                {
                    _syncStarted = true;
                }
            }
        }

        /// <summary>
        /// Stop syncing the SyncBox.
        /// </summary>
        public void StopSyncing()
        {
            lock (_locker)
            {
                if (_syncBox != null)
                {
                    _syncStarted = false;
                    _syncBox.Stop();
                    _syncBox = null;
                }
            }
        }

        /// <summary>
        /// Exit the application.
        /// </summary>
        public void Exit()
        {
            if (!_settingsInitial.Equals(_settingsCurrent))
            {
                // Notify the view to put up a MessageBox saying that the settings have changed.  Does the user want to exit anyway?
                NotifySettingsChanged(this, new NotificationEventArgs<string, bool> { Completed = UserWantsToExit });
            }
            else
            {
                // Close the window
                _windowClosed = true;
                CloseCommand.Execute(null);
            }
        }

        /// <summary>
        /// The user clicked the "X" in the upper right corner of the view window.
        /// </summary>
        /// <returns>bool: Prevent the window close.</returns>
        public bool OnWindowClosing()
        {
            if (_windowClosed)
            {
                return false;           // allow the window close
            }

            Dispatcher dispatcher = Application.Current.Dispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            {
                Exit();
            });

            return true;                // cancel the window close
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// The user was asked (by the view) whether he wants to exit with changed advanced options.  This is the user's answer.
        /// </summary>
        /// <param name="willExit"></param>
        private void UserWantsToExit(bool willExit)
        {
            if (willExit)
            {
                // Close the window
                _windowClosed = true;
                CloseCommand.Execute(null);
            }
        }

        /// <summary>
        /// Returns true if the Settings can be saved.
        /// </summary>
        private bool CanSaveSettings
        {
            get
            {
                return !_settingsCurrent.Equals(_settingsInitial);
            }
        }

        /// <summary>
        /// Returns true if the Show Sync Status button should be active.
        /// </summary>
        private bool CanShowSyncStatus
        {
            get
            {
                return _syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the Start Syncing button should be active.
        /// </summary>
        private bool CanStartSyncing
        {
            get
            {
                return !_syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the Stop Syncing button should be active.
        /// </summary>
        private bool CanStopSyncing
        {
            get
            {
                return _syncStarted;
            }
        }

        #endregion

        #region Private Support Functions

        private bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        #endregion


    }
}
