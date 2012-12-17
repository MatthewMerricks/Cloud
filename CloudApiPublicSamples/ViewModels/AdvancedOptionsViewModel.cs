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
using System.Windows.Threading;

namespace CloudApiPublicSamples.ViewModels
{
    public class AdvancedOptionsViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // RelayCommands
        RelayCommand _commandOk;
        RelayCommand _commandCancel;
        RelayCommand _commandBrowseTempDownloadFolder;
        RelayCommand _commandBrowseDatabaseFolder;
        RelayCommand _commandBrowseTraceFolder;
        

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
                    _commandBrowseTempDownloadFolder = new RelayCommand(
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
                    _commandBrowseDatabaseFolder = new RelayCommand(
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
                    _commandBrowseTraceFolder = new RelayCommand(
                        param => this.BrowseTraceFolder(),
                        param => { return true; }
                        );
                }
                return _commandBrowseTraceFolder;
            }
        }

        

        #endregion

        #region Public Methods

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        private void Ok()
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
            if (!String.IsNullOrEmpty(DatabaseFolderFullPath) &&
                !Directory.Exists(DatabaseFolderFullPath))
            {
                MessageBox.Show("The database folder must be the full path of a valid directory.");
                this.IsDatabaseFolderFullPathFocused = true;
                return;
            }

            // Validate the trace files file path.  OK to be empty.
            if (!String.IsNullOrEmpty(TraceFolderFullPath) &&
                !Directory.Exists(TraceFolderFullPath))
            {
                MessageBox.Show("The trace folder must be the full path of a valid directory.");
                this.IsTraceFolderFullPathFocused = true;
                return;
            }

            // Validate the TraceType
            ulong value;
            if (String.IsNullOrEmpty(TraceType) ||
                !Utilities.ConvertStringToUlong(TraceType, out value))
            {
                MessageBox.Show("The TraceType is a bit mask.  It must be a non-negative decimal number convertible to an unsigned integer <= 18446744073709551615.");
                this.IsTraceTypeFocused = true;
                return;
            }

            // Validate the TraceLevel
            if (String.IsNullOrEmpty(TraceLevel) ||
                !Utilities.ConvertStringToUlong(TraceLevel, out value))
            {
                MessageBox.Show("The TraceLevel must be a non-negative decimal number convertible to an unsigned integer <= 18446744073709551615.");
                this.IsTraceLevelFocused = true;
                return;
            }

            // Save the values to caller's settings
            _settingsCaller.TempDownloadFolderFullPath = TempDownloadFolderFullPath;
            _settingsCaller.DatabaseFolderFullPath = DatabaseFolderFullPath;
            _settingsCaller.LogErrors = LogErrors;
            _settingsCaller.TraceType = (TraceType)Convert.ToInt32(TraceType);
            _settingsCaller.TraceFolderFullPath = TraceFolderFullPath;
            _settingsCaller.TraceExcludeAuthorization = TraceExcludeAuthorization;
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
                _windowClosed = true;
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

        #endregion

        public bool CanOk
        {
            get
            {
                return !_settingsCaller.Equals(_settingsCurrent);
            }
        }
    }
}
