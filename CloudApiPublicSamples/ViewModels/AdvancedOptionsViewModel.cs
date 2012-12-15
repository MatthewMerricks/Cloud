using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
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
    public class AdvancedOptionsViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // RelayCommands
        RelayCommand _commandOk;
        RelayCommand _commandCancel;

        private Settings _settingsCurrent;
        private Settings _settingsCaller;

        #endregion

        #region Constructors

        public AdvancedOptionsViewModel(Settings settingsParam)
        {
            _settingsCaller = settingsParam;
            _settingsCurrent = new Settings(settingsParam);
        }

        #endregion

        #region Model Properties

        public string TempDownloadFolderFullPath
        {
            get { return _settingsCurrent.TempDownloadFolderFullPath; }
            set
            {
                if (value == _settingsCurrent.TempDownloadFolderFullPath)
                {
                    return;
                }

                _settingsCurrent.TempDownloadFolderFullPath = value;

                base.OnPropertyChanged("TempDownloadFolderFullPath");
            }
        }

        public string DatabaseFileFullPath
        {
            get { return _settingsCurrent.DatabaseFileFullPath; }
            set
            {
                if (value == _settingsCurrent.DatabaseFileFullPath)
                {
                    return;
                }

                _settingsCurrent.DatabaseFileFullPath = value;

                base.OnPropertyChanged("DatabaseFileFullPath");
            }
        }

        public bool LogErrors
        {
            get { return _settingsCurrent.LogErrors; }
            set
            {
                if (value == _settingsCurrent.LogErrors)
                {
                    return;
                }

                _settingsCurrent.LogErrors = value;

                base.OnPropertyChanged("LogErrors");
            }
        }

        public TraceType TraceType
        {
            get { return _settingsCurrent.TraceType; }
            set
            {
                if (value == _settingsCurrent.TraceType)
                {
                    return;
                }

                _settingsCurrent.TraceType = value;

                base.OnPropertyChanged("TraceType");
            }
        }

        public string TraceFilesFullPath
        {
            get { return _settingsCurrent.TraceFilesFullPath; }
            set
            {
                if (value == _settingsCurrent.TraceFilesFullPath)
                {
                    return;
                }

                _settingsCurrent.TraceFilesFullPath = value;

                base.OnPropertyChanged("TraceFilesFullPath");
            }
        }

        public bool TraceExcludeAuthorization
        {
            get { return _settingsCurrent.TraceExcludeAuthorization; }
            set
            {
                if (value == _settingsCurrent.TraceExcludeAuthorization)
                {
                    return;
                }

                _settingsCurrent.TraceExcludeAuthorization = value;

                base.OnPropertyChanged("TraceExcludeAuthorization");
            }
        }

        public int TraceLevel
        {
            get { return _settingsCurrent.TraceLevel; }
            set
            {
                if (value == _settingsCurrent.TraceLevel)
                {
                    return;
                }

                _settingsCurrent.TraceLevel = value;

                base.OnPropertyChanged("TraceLevel");
            }
        }

        #endregion

        #region Focus Properties

        public bool IsTempDownloadFolderFullPathFocused
        {
            get { return _isTempDownloadFolderFullPathFocused; }
            set
            {
                if (value == _isTempDownloadFolderFullPathFocused)
                {
                    _isTempDownloadFolderFullPathFocused = false;
                    base.OnPropertyChanged("IsTempDownloadFolderFullPathFocused");
                }

                _isTempDownloadFolderFullPathFocused = value;
                base.OnPropertyChanged("IsTempDownloadFolderFullPathFocused");
            }
        }
        private bool _isTempDownloadFolderFullPathFocused;

        public bool IsDatabaseFileFullPathFocused
        {
            get { return _isDatabaseFileFullPathFocused; }
            set
            {
                if (value == _isDatabaseFileFullPathFocused)
                {
                    _isDatabaseFileFullPathFocused = false;
                    base.OnPropertyChanged("IsDatabaseFileFullPathFocused");
                }

                _isDatabaseFileFullPathFocused = value;
                base.OnPropertyChanged("IsDatabaseFileFullPathFocused");
            }
        }
        private bool _isDatabaseFileFullPathFocused;

        public bool IsTraceTypeFocused
        {
            get { return _isTraceTypeFocused; }
            set
            {
                if (value == _isTraceTypeFocused)
                {
                    _isTraceTypeFocused = false;
                    base.OnPropertyChanged("IsTraceTypeFocused");
                }

                _isTraceTypeFocused = value;
                base.OnPropertyChanged("IsTraceTypeFocused");
            }
        }
        private bool _isTraceTypeFocused;

        public bool IsTraceFilesFullPathFocused
        {
            get { return _isTraceFilesFullPathFocused; }
            set
            {
                if (value == _isTraceFilesFullPathFocused)
                {
                    _isTraceFilesFullPathFocused = false;
                    base.OnPropertyChanged("IsTraceFilesFullPathFocused");
                }

                _isTraceFilesFullPathFocused = value;
                base.OnPropertyChanged("IsTraceFilesFullPathFocused");
            }
        }
        private bool _isTraceFilesFullPathFocused;

        public bool IsTraceLevelFocused
        {
            get { return _isTraceLevelFocused; }
            set
            {
                if (value == _isTraceLevelFocused)
                {
                    _isTraceLevelFocused = false;
                    base.OnPropertyChanged("IsTraceLevelFocused");
                }

                _isTraceLevelFocused = value;
                base.OnPropertyChanged("IsTraceLevelFocused");
            }
        }
        private bool _isTraceLevelFocused;

        #endregion

        #region Presentation Properties

        /// <summary>
        /// Returns a command that closes the dialog saving the changes.
        /// </summary>
        public ICommand CommandOk
        {
            get
            {
                if (_commandOk == null)
                {
                    _commandOk = new RelayCommand(
                        param => this.Ok(),
                        param => this.CanOk
                        );
                }
                return _commandOk;
            }
        }

        /// <summary>
        /// Returns a command that exits the dialog without saving changes.
        /// </summary>
        public ICommand CommandCancel
        {
            get
            {
                if (_commandCancel == null)
                {
                    _commandCancel = new RelayCommand(
                        param => this.Cancel(),
                        param => this.CanCancel
                        );
                }
                return _commandCancel;
            }
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        public void Ok()
        {
            // Validate the temporary download file full path.  OK to be empty.
            if (!String.IsNullOrEmpty(TempDownloadFolderFullPath) &&
                !Directory.Exists(TempDownloadFolderFullPath))
            {
                MessageBox.Show("The temporary download folder must be the full path of a valid directory.");
                this.IsTempDownloadFolderFullPathFocused = true;
                return;
            }

            // Validate the database file path.  OK to be empty.
            if (!String.IsNullOrEmpty(DatabaseFileFullPath) &&
                !File.Exists(DatabaseFileFullPath))
            {
                MessageBox.Show("The database file must exist at the location specified.");
                this.IsDatabaseFileFullPathFocused = true;
                return;
            }

            // Validate the trace files file path.  OK to be empty.
            if (!String.IsNullOrEmpty(TraceFilesFullPath) &&
                !Directory.Exists(TraceFilesFullPath))
            {
                MessageBox.Show("The trace folder must be the full path of a valid directory.");
                this.IsTraceFilesFullPathFocused = true;
                return;
            }

            // Validate the TraceType
            if (!String.IsNullOrEmpty(TraceType) &&
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

            // Validate the Device ID.
            if (String.IsNullOrEmpty(DeviceId) ||
                !ConvertStringToUlong(DeviceId, out value) ||
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
        }

        /// <summary>
        /// The user clicked the Cancel button.
        /// </summary>
        public void Cancel()
        {
            MessageBox.Show("Cancel button clicked");
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
        /// Returns true if the OK button can be clicked.
        /// </summary>
        private bool CanOk
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Cancel button should be active.
        /// </summary>
        private bool CanCancel
        {
            get
            {
                //TODO: Fill this in.
                return true;
            }
        }

        #endregion
    }
}
