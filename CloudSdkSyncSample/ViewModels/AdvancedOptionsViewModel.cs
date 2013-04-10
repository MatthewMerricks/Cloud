using Cloud.Interfaces;
using Cloud.Static;
using Cloud.Support;
using Cloud.Model;
using SampleLiveSync.Models;
using SampleLiveSync.Support;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;

namespace SampleLiveSync.ViewModels
{
    public sealed class AdvancedOptionsViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // RelayCommands
        RelayCommand<object> _commandOk;
        RelayCommand<object> _commandCancel;
        RelayCommand<object> _commandBrowseTempDownloadFolder;
        RelayCommand<object> _commandBrowseDatabaseFolder;
        RelayCommand<object> _commandBrowseTraceFolder;

        private Settings _settingsCurrent;
        private Settings _settingsCaller;
        private bool _windowClosed = false;

        #endregion

        #region Events

        public event EventHandler<NotificationEventArgs> NotifyBrowseTempDownloadFolder;
        public event EventHandler<NotificationEventArgs> NotifyBrowseDatabaseFolder;
        public event EventHandler<NotificationEventArgs> NotifyBrowseTraceFolder;
        public event EventHandler<NotificationEventArgs<string, bool>> NotifyAdvancedSettingsChanged;

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

        public string DatabaseFolderFullPath
        {
            get { return _settingsCurrent.DatabaseFolderFullPath; }
            set
            {
                if (value == _settingsCurrent.DatabaseFolderFullPath)
                {
                    return;
                }

                _settingsCurrent.DatabaseFolderFullPath = value;

                base.OnPropertyChanged("DatabaseFolderFullPath");
            }
        }

        public bool BadgingEnabled
        {
            get { return _settingsCurrent.BadgingEnabled; }
            set
            {
                if (value == _settingsCurrent.BadgingEnabled)
                {
                    return;
                }

                _settingsCurrent.BadgingEnabled = value;

                base.OnPropertyChanged("BadgingEnabled");
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

        public string TraceType
        {
            get { return ((int)_settingsCurrent.TraceType).ToString(); }
            set
            {
                try
                {
                    string currentStringValue = ((int)_settingsCurrent.TraceType).ToString();
                    if (value == currentStringValue)
                    {
                        return;
                    }

                    _settingsCurrent.TraceType = (TraceType)Convert.ToInt32(value);
                    base.OnPropertyChanged("TraceType");
                }
                catch
                {
                }
            }
        }

        public string TraceFolderFullPath
        {
            get { return _settingsCurrent.TraceFolderFullPath; }
            set
            {
                if (value == _settingsCurrent.TraceFolderFullPath)
                {
                    return;
                }

                _settingsCurrent.TraceFolderFullPath = value;

                base.OnPropertyChanged("TraceFolderFullPath");
            }
        }

        public string TraceLevel
        {
            get { return _settingsCurrent.TraceLevel.ToString(); }
            set
            {
                try
                {
                    string currentStringValue = ((int)_settingsCurrent.TraceLevel).ToString();
                    if (value == currentStringValue)
                    {
                        return;
                    }

                    _settingsCurrent.TraceLevel = Convert.ToInt32(value);
                    base.OnPropertyChanged("TraceLevel");
                }
                catch
                {
                }
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

        public bool IsDatabaseFolderFullPathFocused
        {
            get { return _isDatabaseFolderFullPathFocused; }
            set
            {
                if (value == _isDatabaseFolderFullPathFocused)
                {
                    _isDatabaseFolderFullPathFocused = false;
                    base.OnPropertyChanged("IsDatabaseFolderFullPathFocused");
                }

                _isDatabaseFolderFullPathFocused = value;
                base.OnPropertyChanged("IsDatabaseFolderFullPathFocused");
            }
        }
        private bool _isDatabaseFolderFullPathFocused;

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

        public bool IsTraceFolderFullPathFocused
        {
            get { return _isTraceFolderFullPathFocused; }
            set
            {
                if (value == _isTraceFolderFullPathFocused)
                {
                    _isTraceFolderFullPathFocused = false;
                    base.OnPropertyChanged("IsTraceFolderFullPathFocused");
                }

                _isTraceFolderFullPathFocused = value;
                base.OnPropertyChanged("IsTraceFolderFullPathFocused");
            }
        }
        private bool _isTraceFolderFullPathFocused;

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
                    _commandOk = new RelayCommand<object>(
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
                    _commandCancel = new RelayCommand<object>(
                        param => this.Cancel(),
                        param => { return true; }
                        );
                }
                return _commandCancel;
            }
        }

        /// <summary>
        /// Returns a command that allows the user to select a folder for the temporary download files.
        /// </summary>
        public ICommand CommandBrowseTempDownloadFolder
        {
            get
            {
                if (_commandBrowseTempDownloadFolder == null)
                {
                    _commandBrowseTempDownloadFolder = new RelayCommand<object>(
                        param => this.BrowseTempDownloadFolder(),
                        param => { return true; }
                        );
                }
                return _commandBrowseTempDownloadFolder;
            }
        }

        /// <summary>
        /// Returns a command that allows the user to select a folder for the database file.
        /// </summary>
        public ICommand CommandBrowseDatabaseFolder
        {
            get
            {
                if (_commandBrowseDatabaseFolder == null)
                {
                    _commandBrowseDatabaseFolder = new RelayCommand<object>(
                        param => this.BrowseDatabaseFolder(),
                        param => { return true; }
                        );
                }
                return _commandBrowseDatabaseFolder;
            }
        }

        /// <summary>
        /// Returns a command that allows the user to select a folder for the trace files.
        /// </summary>
        public ICommand CommandBrowseTraceFolder
        {
            get
            {
                if (_commandBrowseTraceFolder == null)
                {
                    _commandBrowseTraceFolder = new RelayCommand<object>(
                        param => this.BrowseTraceFolder(),
                        param => { return true; }
                        );
                }
                return _commandBrowseTraceFolder;
            }
        }

        #endregion

        #region Support Methods

        /// <summary>
        /// The user clicked the OK button to save the advanced settings.
        /// </summary>
        private void Ok()
        {
            // Validate the temporary download file full path.  OK to be empty.
            TempDownloadFolderFullPath = TempDownloadFolderFullPath.Trim();
            if (!String.IsNullOrEmpty(TempDownloadFolderFullPath) &&
                !Directory.Exists(TempDownloadFolderFullPath))
            {
                MessageBox.Show("The temporary download folder must be the full path of a valid directory.  Please create the directory first.");
                this.IsTempDownloadFolderFullPathFocused = true;
                return;
            }

            // Validate the length of the temp download file path.
            if (TempDownloadFolderFullPath.Length > 0 && TempDownloadFolderFullPath.Length > (259 - 33))             // 259 maximum path length on Windows, minus the length of the temp file names (GUIDs (32 characters)), minus one char for backslash.
            {
                MessageBox.Show(String.Format("The temporary download folder is too long by {0} characters.  Please shorten the path.", TempDownloadFolderFullPath.Length - (259 - 33)));
                this.IsTempDownloadFolderFullPathFocused = true;
                return;
            }

            // The temporary download file path cannot be in the Syncbox directory.
            FilePath fpTemp = TempDownloadFolderFullPath;
            FilePath fpSyncbox = _settingsCaller.SyncboxFullPath;
            if (fpTemp.Contains(fpSyncbox, insensitiveNameSearch: true))
            {
                MessageBox.Show("The temporary download folder cannot be inside the Syncbox directory.");
                this.IsTempDownloadFolderFullPathFocused = true;
                return;
            }

            // Validate the database file path.  OK to be empty.
            DatabaseFolderFullPath = DatabaseFolderFullPath.Trim();
            if (!String.IsNullOrEmpty(DatabaseFolderFullPath) &&
                !Directory.Exists(DatabaseFolderFullPath))
            {
                MessageBox.Show("The database folder must be the full path of a valid directory.  Please create the directory first.");
                this.IsDatabaseFolderFullPathFocused = true;
                return;
            }

            // The entire path must be 259 chars or less.  Validate the entire length of the database file path, including the database filename.ext.
            if (DatabaseFolderFullPath.Length > 0 && DatabaseFolderFullPath.Length > (259 - (CLDefinitions.kSyncDatabaseFileName.Length + 1)))             // 259 maximum path length on Windows, minus the length of the database filename.ext, minus one char for backslash.
            {
                MessageBox.Show(String.Format("The database folder path is too long by {0} characters.  Please shorten the path.", DatabaseFolderFullPath.Length - (259 - (CLDefinitions.kSyncDatabaseFileName.Length + 1))));
                this.IsDatabaseFolderFullPathFocused = true;
                return;
            }

            // The database file path cannot be in the Syncbox directory.
            fpTemp = DatabaseFolderFullPath;
            if (fpTemp.Contains(fpSyncbox, insensitiveNameSearch: true))
            {
                MessageBox.Show("The database folder cannot be inside the Syncbox directory.");
                this.IsDatabaseFolderFullPathFocused = true;
                return;
            }

            // Validate the trace files file path.  OK to be empty.
            TraceFolderFullPath = TraceFolderFullPath.Trim();
            if (!String.IsNullOrEmpty(TraceFolderFullPath) &&
                !Directory.Exists(TraceFolderFullPath))
            {
                MessageBox.Show("The trace folder must be the full path of a valid directory.");
                this.IsTraceFolderFullPathFocused = true;
                return;
            }

            // The entire path must be 259 chars or less.  Validate the entire length of the trace file path, including the max trace filename.ext.
            if (TraceFolderFullPath.Length > 0 && TraceFolderFullPath.Length > (259 - (CLDefinitions.kMaxTraceFilenameExtLength + 1)))             // 259 maximum path length on Windows, minus the max length of a trace filename.ext, minus one char for backslash.
            {
                MessageBox.Show(String.Format("The trace folder path is too long by {0} characters.  Please shorten the path.", TraceFolderFullPath.Length - (259 - (CLDefinitions.kMaxTraceFilenameExtLength + 1))));
                this.IsTraceFolderFullPathFocused = true;
                return;
            }

            // The trace file path cannot be in the Syncbox directory.
            fpTemp = TraceFolderFullPath;
            if (fpTemp.Contains(fpSyncbox, insensitiveNameSearch: true))
            {
                MessageBox.Show("The trace folder cannot be inside the Syncbox directory.");
                this.IsTraceFolderFullPathFocused = true;
                return;
            }

            // Validate the TraceType
            ulong valueTraceType;
            if (String.IsNullOrEmpty(TraceType) ||
                !Utilities.ConvertStringToUlong(TraceType, out valueTraceType) ||
                valueTraceType > 7 ||
                valueTraceType == 2 ||
                valueTraceType == 6)
            {
                MessageBox.Show("The TraceType is a bit mask.  It must be a non-negative decimal number with value 0, 1, 3, 4, 5 or 7.  If in doubt, use 0 (none) or 5 (full minus authorization information).");
                this.IsTraceTypeFocused = true;
                return;
            }

            // Validate the TraceLevel
            if (String.IsNullOrEmpty(TraceLevel) ||
                !Utilities.ConvertStringToUlong(TraceLevel, out valueTraceType))
            {
                MessageBox.Show("The TraceLevel must be a non-negative decimal number convertible to an unsigned integer <= 18446744073709551615.  If in doubt, use 0 (none) or 10 (full).");
                this.IsTraceLevelFocused = true;
                return;
            }

            // LogErrors must not be set if TraceLocation is not set.
            if (String.IsNullOrEmpty(TraceFolderFullPath) && LogErrors)
            {
                MessageBox.Show("The Trace Folder must be set if Log Errors is checked.  Please set the Trace Folder or uncheck Log Errors.");
                this.IsTraceFolderFullPathFocused = true;
                return;
            }

            // Save the values to caller's settings
            _settingsCaller.TempDownloadFolderFullPath = TempDownloadFolderFullPath;
            _settingsCaller.DatabaseFolderFullPath = DatabaseFolderFullPath;
            _settingsCaller.BadgingEnabled = BadgingEnabled;
            _settingsCaller.LogErrors = LogErrors;
            _settingsCaller.TraceType = (TraceType)Convert.ToInt32(TraceType);
            _settingsCaller.TraceFolderFullPath = TraceFolderFullPath;
            _settingsCaller.TraceExcludeAuthorization = (Convert.ToInt32(TraceType) & Convert.ToInt32(Cloud.Static.TraceType.AddAuthorization)) == 0 ? true : false;
            _settingsCaller.TraceLevel = Convert.ToInt32(TraceLevel);

            // Close the window
            _windowClosed = true;
            CloseCommand.Execute(null);
        }

        /// <summary>
        /// The user clicked the Cancel button.
        /// </summary>
        private void Cancel()
        {
            if (!_settingsCaller.Equals(_settingsCurrent))
            {
                // Notify the view to put up a MessageBox saying that the settings have changed.  Does the user want to exit anyway?
                NotifyAdvancedSettingsChanged(this, new NotificationEventArgs<string, bool> { Completed = UserWantsToExit });
            }
            else
            {
                // Close the window
                _windowClosed = true;           // allow the window to close
                CloseCommand.Execute(null);
            }
        }

        /// <summary>
        /// The user clicked the "X" in the upper right corner of the view window.
        /// </summary>
        /// <returns>bool: True: Cancel the window close.</returns>
        public bool OnWindowClosing()
        {
            if (_windowClosed)
            {
                return false;           // allow the window close
            }

            // Redirect to our own Cancel function as if the user clicked the Cancel button.
            Dispatcher dispatcher = Application.Current.Dispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            {
                Cancel();
            });

            return true;                // cancel the window close
        }

        /// <summary>
        /// The user was asked (by the view) whether he wants to exit with changed advanced options.  This is the user's answer.
        /// </summary>
        /// <param name="willExit"></param>
        private void UserWantsToExit(bool willExit)
        {
            if (willExit)
            {
                // Close the window
                _windowClosed = true;           // allow the window to close
                CloseCommand.Execute(null);
            }
        }
        
        /// <summary>
        /// The user wants to browse for the temporary download file folder.
        /// </summary>
        private void BrowseTempDownloadFolder()
        {
            // Notify the view to put up the folder selector.
            NotifyBrowseTempDownloadFolder(this, new NotificationEventArgs());
        }

        /// <summary>
        /// The user wants to browse for the database file folder.
        /// </summary>
        private void BrowseDatabaseFolder()
        {
            // Notify the view to put up the folder selector.
            NotifyBrowseDatabaseFolder(this, new NotificationEventArgs());
        }

        /// <summary>
        /// The user wants to browse for the trace file folder.
        /// </summary>
        private void BrowseTraceFolder()
        {
            // Notify the view to put up the folder selector.
            NotifyBrowseTraceFolder(this, new NotificationEventArgs());
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// The OK button will be enabled if any of the settings have been changed.
        /// </summary>
        public bool CanOk
        {
            get
            {
                return !_settingsCaller.Equals(_settingsCurrent);
            }
        }

        #endregion
    }
}
