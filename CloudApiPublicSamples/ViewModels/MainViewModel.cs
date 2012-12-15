using CloudApiPublic.Interfaces;
using CloudApiPublicSamples.Models;
using CloudApiPublicSamples.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

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

        private Settings _settingsUi;
        //&&&&private ISyncSettingsAdvanced _syncSettingsAdvanced;

        #endregion

        #region Constructors

        public MainViewModel()
        {
            _settingsUi = new Settings();
        }

        #endregion

        #region Model Properties

        public string SyncRoot
        {
            get { return _settingsUi.SyncBoxFullPath; }
            set
            {
                if (value == _settingsUi.SyncBoxFullPath)
                {
                    return;
                }

                _settingsUi.SyncBoxFullPath = value;

                base.OnPropertyChanged("SyncRoot");
            }
        }

        public string AppKey
        {
            get { return _settingsUi.ApplicationKey; }
            set
            {
                if (value == _settingsUi.ApplicationKey)
                {
                    return;
                }

                _settingsUi.ApplicationKey = value;

                base.OnPropertyChanged("AppKey");
            }
        }

        public string AppSecret
        {
            get { return _settingsUi.ApplicationSecret; }
            set
            {
                if (value == _settingsUi.ApplicationSecret)
                {
                    return;
                }

                _settingsUi.ApplicationSecret = value;

                base.OnPropertyChanged("AppSecret");
            }
        }

        public string SyncBoxId
        {
            get { return _settingsUi.SyncBoxId; }
            set
            {
                if (value == _settingsUi.SyncBoxId)
                {
                    return;
                }

                _settingsUi.SyncBoxId = value;

                base.OnPropertyChanged("SyncBoxId");
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
                        param => this.CanBrowseSyncBoxFolder
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
                        param => this.CanShowAdvancedOptions
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
                        param => this.CanExit
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
            MessageBox.Show("BrowseSyncBoxFolder");
        }

        /// <summary>
        /// Show the advanced options dialog.
        /// </summary>
        public void ShowAdvancedOptions()
        {
            MessageBox.Show("ShowAdvancedOptions");
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
                !ConvertStringToUlong(SyncBoxId, out value) ||
                value == 0)
            {
                MessageBox.Show("The SyncBox ID must be a positive decimal number convertible to an unsigned integer <= 18446744073709551615.");
                this.IsSyncBoxIdFocused = true;
                return;
            }

            // Save the values to Settings
            Properties.Settings.Default.SyncBoxFullPath = SyncRoot;
            Properties.Settings.Default.ApplicationKey = AppKey;
            Properties.Settings.Default.ApplicationSecret = AppSecret;
            Properties.Settings.Default.SyncBoxId = SyncBoxId;
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
            MessageBox.Show("StartSyncing");
        }

        /// <summary>
        /// Stop syncing the SyncBox.
        /// </summary>
        public void StopSyncing()
        {
            MessageBox.Show("StopSyncing");
        }

        /// <summary>
        /// Exit the application.
        /// </summary>
        public void Exit()
        {
            MessageBox.Show("Exit");
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Returns true if the Browse button should be active to select a SyncBox folder.
        /// </summary>
        private bool CanBrowseSyncBoxFolder
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Advanced Options button should be active.
        /// </summary>
        private bool CanShowAdvancedOptions
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Settings can be saved.
        /// </summary>
        private bool CanSaveSettings
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Show Sync Status button should be active.
        /// </summary>
        private bool CanShowSyncStatus
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Start Syncing button should be active.
        /// </summary>
        private bool CanStartSyncing
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Stop Syncing button should be active.
        /// </summary>
        private bool CanStopSyncing
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Exit button should be active.
        /// </summary>
        private bool CanExit
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        #endregion

        #region Private Support Functions

        private bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        private bool ConvertStringToUlong(string inString, out ulong value)
        {
            bool toReturn = true;
            try
            {
                value = Convert.ToUInt64(inString);
            }
            catch
            {
                value = 0;
                toReturn = false;
            }

            return toReturn;
        }

        #endregion


    }
}
