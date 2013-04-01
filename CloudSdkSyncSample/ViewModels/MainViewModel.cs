using Cloud;
using Cloud.Model;
using SampleLiveSync.EventMessageReceiver;
using Cloud.Interfaces;
using Cloud.Static;
using Cloud.Support;
using SampleLiveSync.Models;
using SampleLiveSync.Support;
using SampleLiveSync.Views;
using SampleLiveSync.Static;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;
using System.Management;
using System.Reflection;  //&&&&& remove
using System.Text;   //&&&&& remove

namespace SampleLiveSync.ViewModels
{
    public class MainViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // RelayCommands
        RelayCommand<object> _commandBrowseSyncBoxFolder;
        RelayCommand<object> _commandShowAdvancedOptions;
        RelayCommand<object> _commandSaveSettings;
        RelayCommand<object> _commandResetSync;
        RelayCommand<object> _commandInstallBadging;
        RelayCommand<object> _commandUninstallBadging;
        RelayCommand<object> _commandShowSyncStatus;
        RelayCommand<object> _commandStartSyncing;
        RelayCommand<object> _commandStopSyncing;
        RelayCommand<object> _commandGenerateDeviceId;
        RelayCommand<object> _commandExit;

        // Private fields
        private Settings _settingsCurrent = null;
        private Settings _settingsInitial = null;
        private Window _mainWindow = null;
        private bool _syncStarted = false;
        private bool _windowClosed = false;
        private CLSyncEngine _syncEngine = null;
        private SyncStatusView _winSyncStatus = null;

        private static readonly object _locker = new object();
        private static readonly CLTrace _trace = CLTrace.Instance;

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
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;

            // Read in the settings
            _settingsCurrent = new Settings();
            _settingsInitial = new Settings();
            _settingsCurrent.GetSavedSettings();
            _settingsInitial.GetSavedSettings();

            // Initialize trace
            CLTrace.Initialize(_settingsInitial.TraceFolderFullPath, "SampleLiveSync", "log", _settingsInitial.TraceLevel, _settingsInitial.LogErrors);

            // Bind to MessageEvents for special message handling cases
            MessageEvents.NewEventMessage += MessageEvents_NewEventMessage;
        }

        public MainViewModel()
        {
            throw new NotSupportedException("Default constructor not supported.");
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

        public string Key
        {
            get { return _settingsCurrent.Key; }
            set
            {
                if (value == _settingsCurrent.Key)
                {
                    return;
                }

                _settingsCurrent.Key = value;

                base.OnPropertyChanged("Key");
            }
        }

        public string Secret
        {
            get { return _settingsCurrent.Secret; }
            set
            {
                if (value == _settingsCurrent.Secret)
                {
                    return;
                }

                _settingsCurrent.Secret = value;

                base.OnPropertyChanged("Secret");
            }
        }

        public string Token
        {
            get { return _settingsCurrent.Token; }
            set
            {
                if (value == _settingsCurrent.Token)
                {
                    return;
                }

                _settingsCurrent.Token = value;

                base.OnPropertyChanged("Token");
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

        public bool BadgingEnabled
        {
            get { return _settingsCurrent.BadgingEnabled; }
            set
            {
                if (value == _settingsCurrent.BadgingEnabled)
                {
                    return;
                }

                _settingsCurrent.BadgingEnabled= value;

                base.OnPropertyChanged("BadgingEnabled");
            }
        }

        #endregion

        #region IsEnabled Properties

        public bool TbSyncBoxFolderEnabled
        {
            get { return _tbSyncBoxFolderEnabled; }
            set
            {
                if (value == _tbSyncBoxFolderEnabled)
                {
                    return;
                }

                _tbSyncBoxFolderEnabled= value;

                base.OnPropertyChanged("TbSyncBoxFolderEnabled");
            }
        }
        private bool _tbSyncBoxFolderEnabled = true;

        public bool TbKeyEnabled
        {
            get { return _tbKeyEnabled; }
            set
            {
                if (value == _tbKeyEnabled)
                {
                    return;
                }

                _tbKeyEnabled = value;

                base.OnPropertyChanged("TbKeyEnabled");
            }
        }
        private bool _tbKeyEnabled = true;

        public bool TbSecretEnabled
        {
            get { return _tbSecretEnabled; }
            set
            {
                if (value == _tbSecretEnabled)
                {
                    return;
                }

                _tbSecretEnabled = value;

                base.OnPropertyChanged("TbSecretEnabled");
            }
        }
        private bool _tbSecretEnabled = true;

        public bool TbTokenEnabled
        {
            get { return _tbTokenEnabled; }
            set
            {
                if (value == _tbTokenEnabled)
                {
                    return;
                }

                _tbTokenEnabled = value;

                base.OnPropertyChanged("TbTokenEnabled");
            }
        }
        private bool _tbTokenEnabled = true;

        public bool TbSyncBoxIdEnabled
        {
            get { return _tbSyncBoxIdEnabled; }
            set
            {
                if (value == _tbSyncBoxIdEnabled)
                {
                    return;
                }

                _tbSyncBoxIdEnabled = value;

                base.OnPropertyChanged("TbSyncBoxIdEnabled");
            }
        }
        private bool _tbSyncBoxIdEnabled = true;

        public bool TbUniqueDeviceIdEnabled
        {
            get { return _tbUniqueDeviceIdEnabled; }
            set
            {
                if (value == _tbUniqueDeviceIdEnabled)
                {
                    return;
                }

                _tbUniqueDeviceIdEnabled = value;

                base.OnPropertyChanged("TbUniqueDeviceIdEnabled");
            }
        }
        private bool _tbUniqueDeviceIdEnabled = true;

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

        public bool IsKeyFocused
        {
            get { return _isKeyFocused; }
            set
            {
                if (value == _isKeyFocused)
                {
                    _isKeyFocused = false;
                    base.OnPropertyChanged("IsKeyFocused");
                }

                _isKeyFocused = value;
                base.OnPropertyChanged("IsKeyFocused");
            }
        }
        private bool _isKeyFocused;

        public bool IsSecretFocused
        {
            get { return _isSecretFocused; }
            set
            {
                if (value == _isSecretFocused)
                {
                    _isSecretFocused = false;
                    base.OnPropertyChanged("IsSecretFocused");
                }

                _isSecretFocused = value;
                base.OnPropertyChanged("IsSecretFocused");
            }
        }
        private bool _isSecretFocused;

        public bool IsTokenFocused
        {
            get { return _isTokenFocused; }
            set
            {
                if (value == _isTokenFocused)
                {
                    _isTokenFocused = false;
                    base.OnPropertyChanged("IsTokenFocused");
                }

                _isTokenFocused = value;
                base.OnPropertyChanged("IsTokenFocused");
            }
        }
        private bool _isTokenFocused;

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

        #region Commands

        /// <summary>
        /// Returns a command that browses to select a SyncBox folder.
        /// </summary>
        public ICommand CommandBrowseSyncBoxFolder
        {
            get
            {
                if (_commandBrowseSyncBoxFolder == null)
                {
                    _commandBrowseSyncBoxFolder = new RelayCommand<object>(
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
                    _commandShowAdvancedOptions = new RelayCommand<object>(
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
                    _commandSaveSettings = new RelayCommand<object>(
                        param => this.SaveSettings(),
                        param => this.CanSaveSettings
                        );
                }
                return _commandSaveSettings;
            }
        }

        /// <summary>
        /// Returns a command that resets the sync engine.
        /// </summary>
        public ICommand CommandResetSync
        {
            get
            {
                if (_commandResetSync == null)
                {
                    _commandResetSync = new RelayCommand<object>(
                        param => this.ResetSync(),
                        param => this.CanResetSync
                        );
                }
                return _commandResetSync;
            }
        }

        /// <summary>
        /// Returns a command that installs the BadgeCom badging COM object.
        /// </summary>
        public ICommand CommandInstallBadging
        {
            get
            {
                if (_commandInstallBadging == null)
                {
                    _commandInstallBadging = new RelayCommand<object>(
                        param => this.InstallBadging(),
                        param => this.CanInstallBadging
                        );
                }
                return _commandInstallBadging;
            }
        }

        /// <summary>
        /// Returns a command that uninstalls the BadgeCom badging COM object.
        /// </summary>
        public ICommand CommandUninstallBadging
        {
            get
            {
                if (_commandUninstallBadging == null)
                {
                    _commandUninstallBadging = new RelayCommand<object>(
                        param => this.UninstallBadging(),
                        param => this.CanUninstallBadging
                        );
                }
                return _commandUninstallBadging;
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
                    _commandShowSyncStatus = new RelayCommand<object>(
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
                    _commandStartSyncing = new RelayCommand<object>(
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
                    _commandStopSyncing = new RelayCommand<object>(
                        param => this.StopSyncing(),
                        param => this.CanStopSyncing
                        );
                }
                return _commandStopSyncing;
            }
        }

        /// <summary>
        /// Returns a command that generates a device ID.
        /// </summary>
        public ICommand CommandGenerateDeviceId
        {
            get
            {
                if (_commandGenerateDeviceId == null)
                {
                    _commandGenerateDeviceId = new RelayCommand<object>(
                        param => this.GenerateDeviceId(),
                        param => this.CanGenerateDeviceId
                        );
                }
                return _commandGenerateDeviceId;
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
                    _commandExit = new RelayCommand<object>(
                        param => this.Exit(),
                        param => this.CanExit
                        );
                }
                return _commandExit;
            }
        }

        #endregion

        #region Action Methods

        /// <summary>
        /// Browse to locate a folder to be synced.
        /// </summary>
        private void BrowseSyncBoxFolder()
        {
            try
            {
                // Notify the view to put up the folder selector.
                if (NotifyBrowseSyncBoxFolder != null)
                {
                    NotifyBrowseSyncBoxFolder(this, new NotificationEventArgs());
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: BrowseSyncBoxFolder: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Show the advanced options dialog.
        /// </summary>
        private void ShowAdvancedOptions()
        {
            try
            {
                // Show the advanced options as a modal dialog.
                AdvancedOptionsView viewWindow = new AdvancedOptionsView();
                viewWindow.Owner = _mainWindow;
                viewWindow.ShowInTaskbar = false;
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
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: ShowAdvancedOptions: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Save the settings entered so far.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // Validate the SyncBox full path.
                SyncRoot = SyncRoot.Trim();
                if (String.IsNullOrEmpty(SyncRoot) ||
                    !Directory.Exists(SyncRoot))
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The SyncBox Folder must be the full path of a valid directory.  Please create the directory first." });
                    this.IsSyncBoxPathFocused = true;
                    return;
                }

                // Validate that the SyncRoot is a good path.
                CLError badPathError = Cloud.Static.Helpers.CheckForBadPath(SyncRoot);
                if (badPathError != null)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The SyncBox Folder path is invalid: " + badPathError.errorDescription });
                    this.IsSyncBoxIdFocused = true;
                    return;
                }

                // Validate that the SyncRoot matches case perfectly with disk.
                bool syncPathMatches;
                CLError checkCaseError = Cloud.Static.Helpers.DirectoryMatchesCaseWithDisk(SyncRoot, out syncPathMatches);
                if (checkCaseError != null)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "There was an error checking whether the SyncBox Folder matches case with an existing directory on disk: " + checkCaseError.errorDescription });
                    this.IsSyncBoxIdFocused = true;
                    return;
                }
                if (!syncPathMatches)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The SyncBox Folder does not match case perfectly with an existing folder on disk. Please check the case of the directory string."});
                    this.IsSyncBoxIdFocused = true;
                    return;
                }

                // Validate the length of the SyncBox full path.
                int tooLongChars;
                CLError errorFromLengthCheck = Cloud.Static.Helpers.CheckSyncRootLength(SyncRoot, out tooLongChars);
                if (errorFromLengthCheck != null)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = String.Format("The SyncBox Folder is too long by {0} characters.  Please shorten the path.", tooLongChars) });
                    this.IsSyncBoxPathFocused = true;
                    return;
                }

                // Validate the Key.
                Key = Key.Trim();
                if (String.IsNullOrEmpty(Key) ||
                    !OnlyHexInString(Key) ||
                     Key.Length != 64)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The Key must be a 64 character long string with only hexadecimal characters." });
                    this.IsKeyFocused = true;
                    return;
                }

                // Validate the Secret.
                // NOTE: This private key should not be handled this way.  It should be retrieved dynamically from a remote server, or protected in some other way.
                Secret = Secret.Trim();
                if (String.IsNullOrEmpty(Secret) ||
                    !OnlyHexInString(Secret) ||
                     Secret.Length != 64)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The Secret must be a 64 character long string with only hexadecimal characters." });
                    this.IsSecretFocused = true;
                    return;
                }

                // Validate the Token.
                // NOTE: The token should not be handled this way.  It should be retrieved dynamically from a remote server, or protected in some other way.
                Token = Token.Trim();
                if (!String.IsNullOrEmpty(Token) &&
                    (!OnlyHexInString(Token) ||
                     Token.Length != 64))
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "If the token is specified, it must be a 64 character long string with only hexadecimal characters." });
                    this.IsTokenFocused = true;
                    return;
                }

                // Validate the SyncBox ID.
                SyncBoxId = SyncBoxId.Trim();
                if (String.IsNullOrEmpty(SyncBoxId))
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The SyncBox ID must be specified." });
                    this.IsSyncBoxIdFocused = true;
                    return;
                }

                // Validate the Device ID.
                DeviceId = DeviceId.Trim();
                if (String.IsNullOrWhiteSpace(DeviceId) || Path.GetInvalidPathChars().Any(x => DeviceId.Contains(x)))
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The Device ID must be specified, and it must be valid as a portion of a folder name." });
                    this.IsDeviceIdFocused = true;
                    return;
                }

                // The settings are valid.  Any of this information may have changed, and
                // we don't want to get the sync databases mixed up.  On changes, set a persistent
                // request to delete the sync database when the SyncBox is started.  The sync
                // database will be recreated with the current state of the SyncBox folder.
                if (ShouldWeRequestSyncDatabaseDeletion())
                {
                    Properties.Settings.Default.ShouldResetSync = true;
                }

                // Save the values to Settings
                Properties.Settings.Default.SyncBoxFullPath = SyncRoot;
                Properties.Settings.Default.Key = Key;
                Properties.Settings.Default.Secret = Secret;
                Properties.Settings.Default.Token = Token;
                Properties.Settings.Default.SyncBoxId = SyncBoxId;
                Properties.Settings.Default.UniqueDeviceId = DeviceId;
                Properties.Settings.Default.BadgingEnabled = BadgingEnabled;
                Properties.Settings.Default.TempDownloadFolderFullPath = _settingsCurrent.TempDownloadFolderFullPath;
                Properties.Settings.Default.DatabaseFolderFullPath = _settingsCurrent.DatabaseFolderFullPath;
                Properties.Settings.Default.BadgingEnabled = _settingsCurrent.BadgingEnabled;
                Properties.Settings.Default.LogErrors = _settingsCurrent.LogErrors;
                Properties.Settings.Default.TraceType = _settingsCurrent.TraceType;
                Properties.Settings.Default.TraceFolderFullPath = _settingsCurrent.TraceFolderFullPath;
                Properties.Settings.Default.TraceExcludeAuthorization = _settingsCurrent.TraceExcludeAuthorization;
                Properties.Settings.Default.TraceLevel = _settingsCurrent.TraceLevel;
                Properties.Settings.Default.Save();

                _settingsInitial = new Settings(_settingsCurrent);          // Saved.  Initial is now current.

                // Reinitialize trace
                CLTrace.Initialize(_settingsInitial.TraceFolderFullPath, "SampleLiveSync", "log", _settingsInitial.TraceLevel, 
                                    _settingsInitial.LogErrors, willForceReset: true);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: SaveSettings: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Determine whether we should request that the sync database be deleted when the SyncBox is started.
        /// </summary>
        private bool ShouldWeRequestSyncDatabaseDeletion()
        {
            if (!string.Equals(_settingsCurrent.SyncBoxFullPath, _settingsInitial.SyncBoxFullPath, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(_settingsCurrent.Key, _settingsInitial.Key, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(_settingsCurrent.SyncBoxId, _settingsInitial.SyncBoxId, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(_settingsCurrent.UniqueDeviceId, _settingsInitial.UniqueDeviceId, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(_settingsCurrent.DatabaseFolderFullPath, _settingsInitial.DatabaseFolderFullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Install the badging COM support.
        /// </summary>
        private void InstallBadging()
        {
            Process regsvr32Process = null;
            Process regsvr64Process = null;
            try
            {
                // Stop Explorer
                StopExplorer();

                // 32-bit platforms will install only the 32-bit version of the BadgeCom.dll.  RegSvr32.exe will be used from C:\Windows\System32.
                // 64-bit platforms will install both the 32-bit and the 64-bit versions of BadgeCom.dll.  For the 64-bit version, we will use regsvr32.exe from C:\Windows\System32.
                // For the 32-bit version, we will use regsvr32.exe from C:\Windows\SysWow64.
                // Set the directories and command arguments to use
                string commandRegSvr32ProgramPath32Bit = String.Empty;
                string commandRegSvr32ProgramPath64Bit = String.Empty;
                string commandArguments32Bit = "/s \"" + Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string commandArguments64Bit = commandArguments32Bit;
                commandArguments32Bit += "\\x86\\BadgeCom.dll\"";
                commandArguments64Bit += "\\amd64\\BadgeCom.dll\"";

                if (Is64BitOperatingSystem())
                {
                    commandRegSvr32ProgramPath32Bit = Get32BitSystemDirectory() + "\\regsvr32.exe";
                    commandRegSvr32ProgramPath64Bit = Environment.SystemDirectory + "\\regsvr32.exe";
                }
                else
                {
                    commandRegSvr32ProgramPath32Bit = Environment.SystemDirectory + "\\regsvr32.exe";
                }

                // We always register the 32-bit BadgeCom.dll.
                ProcessStartInfo startInfo32Bit = new ProcessStartInfo();
                startInfo32Bit.CreateNoWindow = true;
                startInfo32Bit.UseShellExecute = true;
                startInfo32Bit.FileName = commandRegSvr32ProgramPath32Bit;
                startInfo32Bit.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo32Bit.Arguments = commandArguments32Bit;
                if (!SampleLiveSync.Static.Helpers.IsAdministrator())
                {
                    _trace.writeToLog(1, "MainViewModel: InstallBadging: Run 32-bit regsvr32 as administrator.");
                    startInfo32Bit.Verb = "runas";
                }
                _trace.writeToLog(1, "MainViewModel: InstallBadging: Start process to run 32-bit regsvr32. Program: {0}. Arguments: {1}.", commandRegSvr32ProgramPath32Bit, commandArguments32Bit);
                regsvr32Process = Process.Start(startInfo32Bit);

                // Wait for the process to exit
                if (regsvr32Process.WaitForExit(20000))
                {
                    // Process has exited.  Get the return code.
                    int retCode = regsvr32Process.ExitCode;
                    if (retCode != 0)
                    {
                        // Error return code
                        string msg = "Error registering 32-bit BadgeCom.dll.";
                        CLError error = new Exception(String.Format("({0} Code: {1}.", msg, retCode));
                        _trace.writeToLog(1, "MainViewModel: InstallBadging: {0} Code: {1}.", msg, retCode);
                        NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                    }
                }
                else
                {
                    // Timed out.
                    string msg = "Error: Timeout registering 32-bit BadgeCom.dll.";
                    CLError error = new Exception(msg);
                    _trace.writeToLog(1, "MainViewModel: InstallBadging: " + msg);
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                }

                // Register the 64-bit BadgeCom.dll if we should.
                if (!String.IsNullOrEmpty(commandRegSvr32ProgramPath64Bit))
                {
                    ProcessStartInfo startInfo64Bit = new ProcessStartInfo();
                    startInfo64Bit.CreateNoWindow = true;
                    startInfo64Bit.UseShellExecute = true;
                    startInfo64Bit.FileName = commandRegSvr32ProgramPath64Bit;
                    startInfo64Bit.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo64Bit.Arguments = commandArguments64Bit;
                    if (!SampleLiveSync.Static.Helpers.IsAdministrator())
                    {
                        _trace.writeToLog(1, "MainViewModel: InstallBadging: Run 64-bit regsvr32 as administrator.");
                        startInfo64Bit.Verb = "runas";
                    }
                    _trace.writeToLog(1, "MainViewModel: InstallBadging: Start process to run 64-bit regsvr32. Program: {0}. Arguments: {1}.", commandRegSvr32ProgramPath64Bit, commandArguments64Bit);
                    regsvr64Process = Process.Start(startInfo64Bit);

                    // Wait for the process to exit
                    if (regsvr64Process.WaitForExit(20000))
                    {
                        // Process has exited.  Get the return code.
                        int retCode = regsvr64Process.ExitCode;
                        if (retCode != 0)
                        {
                            // Error return code
                            string msg = "Error registering 64-bit BadgeCom.dll.";
                            CLError error = new Exception(String.Format("({0} Code: {1}.", msg, retCode));
                            _trace.writeToLog(1, "MainViewModel: InstallBadging: {0} Code: {1}.", msg, retCode);
                            NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                        }
                    }
                    else
                    {
                        // Timed out.
                        string msg = "Error: Timeout registering 64-bit BadgeCom.dll.";
                        CLError error = new Exception(msg);
                        _trace.writeToLog(1, "MainViewModel: InstallBadging: " + msg);
                        NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "MainViewModel: InstallBadging: Error. Exception: Msg: {0}.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = "Error: Exception registering BadgeCom.dll." });
            }
            finally
            {
                try
                {
                    if (regsvr32Process != null)
                    {
                        regsvr32Process.Close();
                    }

                    if (regsvr64Process != null)
                    {
                        regsvr64Process.Close();
                    }

                }
                catch
                {
                }

                // Start Explorer again
                StartExplorer();
            }
        }

        /// <summary>
        /// Uninstall the badging COM support.
        /// </summary>
        private void UninstallBadging()
        {
            Process regsvr32Process = null;
            Process regsvr64Process = null;
            try
            {
                // Stop Explorer
                StopExplorer();

                // 32-bit platforms will install only the 32-bit version of the BadgeCom.dll.  RegSvr32.exe will be used from C:\Windows\System32.
                // 64-bit platforms will install both the 32-bit and the 64-bit versions of BadgeCom.dll.  For the 64-bit version, we will use regsvr32.exe from C:\Windows\System32.
                // For the 32-bit version, we will use regsvr32.exe from C:\Windows\SysWow64.
                // Set the directories and command arguments to use
                string commandRegSvr32ProgramPath32Bit = String.Empty;
                string commandRegSvr32ProgramPath64Bit = String.Empty;
                string commandArguments32Bit = "/u /s \"" + Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string commandArguments64Bit = commandArguments32Bit;
                commandArguments32Bit += "\\x86\\BadgeCom.dll\"";
                commandArguments64Bit += "\\amd64\\BadgeCom.dll\"";

                if (Is64BitOperatingSystem())
                {
                    commandRegSvr32ProgramPath32Bit = Get32BitSystemDirectory() + "\\regsvr32.exe";
                    commandRegSvr32ProgramPath64Bit = Environment.SystemDirectory + "\\regsvr32.exe";
                }
                else
                {
                    commandRegSvr32ProgramPath32Bit = Environment.SystemDirectory + "\\regsvr32.exe";
                }

                // We always register the 32-bit BadgeCom.dll.
                ProcessStartInfo startInfo32Bit = new ProcessStartInfo();
                startInfo32Bit.CreateNoWindow = true;
                startInfo32Bit.UseShellExecute = true;
                startInfo32Bit.FileName = commandRegSvr32ProgramPath32Bit;
                startInfo32Bit.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo32Bit.Arguments = commandArguments32Bit;
                if (!SampleLiveSync.Static.Helpers.IsAdministrator())
                {
                    _trace.writeToLog(1, "MainViewModel: UninstallBadging: Run 32-bit regsvr32 as administrator.");
                    startInfo32Bit.Verb = "runas";
                }
                _trace.writeToLog(1, "MainViewModel: UninstallBadging: Start process to run 32-bit regsvr32. Program: {0}. Arguments: {1}.", commandRegSvr32ProgramPath32Bit, commandArguments32Bit);
                regsvr32Process = Process.Start(startInfo32Bit);

                // Wait for the process to exit
                if (regsvr32Process.WaitForExit(20000))
                {
                    // Process has exited.  Get the return code.
                    int retCode = regsvr32Process.ExitCode;
                    if (retCode != 0)
                    {
                        // Error return code
                        string msg = "Error unregistering 32-bit BadgeCom.dll.";
                        CLError error = new Exception(String.Format("({0} Code: {1}.", msg, retCode));
                        _trace.writeToLog(1, "MainViewModel: UninstallBadging: {0} Code: {1}.", msg, retCode);
                        NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                    }
                }
                else
                {
                    // Timed out.
                    string msg = "Error: Timeout unregistering 32-bit BadgeCom.dll.";
                    CLError error = new Exception(msg);
                    _trace.writeToLog(1, "MainViewModel: UninstallBadging: " + msg);
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                }

                // Register the 64-bit BadgeCom.dll if we should.
                if (!String.IsNullOrEmpty(commandRegSvr32ProgramPath64Bit))
                {
                    ProcessStartInfo startInfo64Bit = new ProcessStartInfo();
                    startInfo64Bit.CreateNoWindow = true;
                    startInfo64Bit.UseShellExecute = true;
                    startInfo64Bit.FileName = commandRegSvr32ProgramPath64Bit;
                    startInfo64Bit.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo64Bit.Arguments = commandArguments64Bit;
                    if (!SampleLiveSync.Static.Helpers.IsAdministrator())
                    {
                        _trace.writeToLog(1, "MainViewModel: UninstallBadging: Run 64-bit regsvr32 as administrator.");
                        startInfo64Bit.Verb = "runas";
                    }
                    _trace.writeToLog(1, "MainViewModel: UninstallBadging: Start process to run 64-bit regsvr32. Program: {0}. Arguments: {1}.", commandRegSvr32ProgramPath64Bit, commandArguments64Bit);
                    regsvr64Process = Process.Start(startInfo64Bit);

                    // Wait for the process to exit
                    if (regsvr64Process.WaitForExit(20000))
                    {
                        // Process has exited.  Get the return code.
                        int retCode = regsvr64Process.ExitCode;
                        if (retCode != 0 && retCode != 5)
                        {
                            // Error return code
                            string msg = "Error unregistering 64-bit BadgeCom.dll.";
                            CLError error = new Exception(String.Format("({0} Code: {1}.", msg, retCode));
                            _trace.writeToLog(1, "MainViewModel: UninstallBadging: {0} Code: {1}.", msg, retCode);
                            NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                        }
                    }
                    else
                    {
                        // Timed out.
                        string msg = "Error: Timeout unregistering 64-bit BadgeCom.dll.";
                        CLError error = new Exception(msg);
                        _trace.writeToLog(1, "MainViewModel: UninstallBadging: " + msg);
                        NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = msg });
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "MainViewModel: UninstallBadging: Error. Exception: Msg: {0}.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = "Error: Exception unregistering BadgeCom.dll." });
            }
            finally
            {
                try
                {
                    if (regsvr32Process != null)
                    {
                        regsvr32Process.Close();
                    }

                    if (regsvr64Process != null)
                    {
                        regsvr64Process.Close();
                    }

                }
                catch
                {
                }

                // Start Explorer again
                StartExplorer();
            }
        }

        /// <summary>
        /// Show the Sync Status window.
        /// </summary>
        private void ShowSyncStatus()
        {
            // Open RateBar graph window for upload/download status and logs
            if (_winSyncStatus != null)
            {
                _winSyncStatus.ShowInTaskbar = false;
                _winSyncStatus.ShowActivated = true;
                _winSyncStatus.WindowStyle = WindowStyle.ThreeDBorderWindow;
                _winSyncStatus.MinWidth = 800;
                _winSyncStatus.MinHeight = 600;
                _winSyncStatus.MaxWidth = 800;
                _winSyncStatus.MaxHeight = 600;
                _winSyncStatus.Left = System.Windows.SystemParameters.PrimaryScreenWidth - 800 - 50;
                _winSyncStatus.Top = System.Windows.SystemParameters.PrimaryScreenHeight - 600 - 50;
                _winSyncStatus.Show();
                _winSyncStatus.Topmost = true;
                _winSyncStatus.Topmost = false;
                _winSyncStatus.Focus();
            }
        }

        /// <summary>
        /// Start syncing the SyncBox.
        /// </summary>
        private void StartSyncing()
        {
            try
            {
                bool startSyncBox = false;

                // Store SyncBox.  It will be set under the locker which checks the _syncEngine, but started afterwards if it was set
                CLSyncBox syncBox = null;
                lock (_locker)
                {
                    if (_syncEngine == null)
                    {
                        if (SettingsAdvancedImpl.Instance.SyncBoxId == null)
                        {
                            const string nullSyncBoxId = "SettingsAvancedImpl Instance SyncBoxId cannot be null";
                            if (NotifyException != null)
                            {
                                NotifyException(this, new NotificationEventArgs<CLError>()
                                {
                                    Data = new ArgumentException(nullSyncBoxId),
                                    Message = nullSyncBoxId
                                });
                            }
                            _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From StartSyncing: Msg: <{0}>.", nullSyncBoxId);
                        }
                        else
                        {
                            // create credential
                            CLCredential syncCredential;
                            CLCredentialCreationStatus syncCredentialStatus;
                            CLError errorCreateSyncCredential = CLCredential.CreateAndInitialize(
                                SettingsAdvancedImpl.Instance.Key,
                                SettingsAdvancedImpl.Instance.Secret,
                                out syncCredential,
                                out syncCredentialStatus,
                                SettingsAdvancedImpl.Instance.Token);

                            if (errorCreateSyncCredential != null)
                            {
                                _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From CLCredential.CreateAndInitialize: Msg: <{0}>.", errorCreateSyncCredential.errorDescription);
                            }
                            if (syncCredentialStatus != CLCredentialCreationStatus.Success)
                            {
                                if (NotifyException != null)
                                {
                                    NotifyException(this, new NotificationEventArgs<CLError>()
                                    {
                                        Data = errorCreateSyncCredential,
                                        Message = "syncCredentialStatus: " + syncCredentialStatus.ToString() + ":" + Environment.NewLine +
                                            errorCreateSyncCredential.errorDescription
                                    });
                                }
                            }
                            else
                            {
                                // create a SyncBox from an existing SyncBoxId
                                CLSyncBoxCreationStatus syncBoxStatus;
                                CLError errorCreateSyncBox = CLSyncBox.CreateAndInitialize(
                                    syncCredential,
                                    (long)SettingsAdvancedImpl.Instance.SyncBoxId,
                                    out syncBox,
                                    out syncBoxStatus,
                                    SettingsAdvancedImpl.Instance);

                                if (errorCreateSyncBox != null)
                                {
                                    _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From CLSyncBox.CreateAndInitialize: Msg: <{0}>.", errorCreateSyncBox.errorDescription);
                                }
                                if (syncBoxStatus != CLSyncBoxCreationStatus.Success)
                                {
                                    if (NotifyException != null)
                                    {
                                        NotifyException(this, new NotificationEventArgs<CLError>()
                                        {
                                            Data = errorCreateSyncBox,
                                            Message = "syncBoxStatus: " + syncBoxStatus.ToString() + ":" + Environment.NewLine +
                                                errorCreateSyncBox.errorDescription
                                        });
                                    }
                                }
                                else
                                {
                                    _syncEngine = new CLSyncEngine();
                                    startSyncBox = true;

                                    // Reset the sync database if we should
                                    if (Properties.Settings.Default.ShouldResetSync)
                                    {
                                        CLError errorFromSyncReset = _syncEngine.SyncReset(syncBox);
                                        if (errorFromSyncReset != null)
                                        {
                                            _syncEngine = null;
                                            startSyncBox = false;
                                            _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From SyncBox.SyncReset: Msg: <{0}.", errorFromSyncReset.errorDescription);
                                            if (NotifyException != null)
                                            {
                                                NotifyException(this, new NotificationEventArgs<CLError>()
                                                {
                                                    Data = errorFromSyncReset,
                                                    Message = String.Format("Error resetting the SyncBox: {0}.", errorFromSyncReset.errorDescription)
                                                });
                                            }
                                        }
                                        else
                                        {
                                            Properties.Settings.Default.ShouldResetSync = false;
                                            Properties.Settings.Default.Save();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (startSyncBox
                    && syncBox != null)
                {
                    // start syncing
                    CLSyncStartStatus startStatus;
                    CLError errorFromSyncBoxStart = _syncEngine.Start(
                        SyncBox: syncBox, // syncbox to sync (contains required settings)
                        Status: out startStatus, // The completion status of the Start() function
                        StatusUpdated: OnSyncStatusUpdated, // called when sync status is updated
                        StatusUpdatedUserState: _syncEngine); // the user state passed to the callback above

                    if (errorFromSyncBoxStart != null)
                    {
                        _syncEngine = null;
                        _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From SyncBox.Start: Msg: <{0}>.", errorFromSyncBoxStart.errorDescription);
                    }
                    if (startStatus != CLSyncStartStatus.Success)
                    {
                        if (NotifyException != null)
                        {
                            NotifyException(this, new NotificationEventArgs<CLError>() { Data = errorFromSyncBoxStart, Message = String.Format("Error starting the SyncBox: {0}.", startStatus.ToString()) });
                        }
                    }
                    else
                    {
                        lock (_locker)
                        {
                            // Sync has started
                            SetSyncBoxStartedState(isStartedStateToSet: true);

                            // Watch for push notification errors
                            _syncEngine.PushNotificationError += OnPushNotificationError;

                            // Start an instance of the sync status window and start it hidden.
                            if (_winSyncStatus == null)
                            {
                                _trace.writeToLog(9, "MainViewModel: StartSyncing: Start the sync status window.");

                                // Get a ViewModel to provide some of the status information to use on our status window.
                                EventMessageReceiver.EventMessageReceiver vm;
                                CLError errorCreateVM = EventMessageReceiver.EventMessageReceiver.CreateAndInitialize(
                                    syncBox.SyncBoxId, // filter by current sync box
                                    syncBox.CopiedSettings.DeviceId, // filter by current device
                                    out vm, // output the created view model
                                    OnGetHistoricBandwidthSettings, // optional to provide the historic upload and download bandwidth to the engine
                                    OnSetHistoricBandwidthSettings, // optional to persist the historic upload and download bandwidth to the engine
                                    EventMessageLevel.All, // optional to filter the non-error messages delivered to the EventMessageReceiver ListMessages
                                    EventMessageLevel.All, // optional to filter the error messages delivered to the EventMessageReceiver ListMessages
                                    500); // optional to restrict the number of messages in the EventMessageReceiver ListMessages

                                if (errorCreateVM != null)
                                {
                                    _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From EventMessageReceiver.CreateAndInitialize: Msg: <{0}>.", errorCreateVM.errorDescription);
                                }
                                else
                                {
                                    _winSyncStatus = new SyncStatusView();
                                    _winSyncStatus.DataContext = vm;
                                    _winSyncStatus.Width = 0;
                                    _winSyncStatus.Height = 0;
                                    _winSyncStatus.MinWidth = 0;
                                    _winSyncStatus.MinHeight = 0;
                                    _winSyncStatus.Left = Int32.MaxValue;
                                    _winSyncStatus.Top = Int32.MaxValue;
                                    _winSyncStatus.ShowInTaskbar = false;
                                    _winSyncStatus.ShowActivated = false;
                                    _winSyncStatus.Visibility = Visibility.Hidden;
                                    _winSyncStatus.WindowStyle = WindowStyle.None;
                                    _winSyncStatus.Owner = _mainWindow;
                                    _winSyncStatus.Show();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Reset the sync engine.
        /// </summary>
        private void ResetSync()
        {
            try
            {
                lock (_locker)
                {
                    // Don't do this if the sync engine has already been started.
                    if (_syncEngine != null)
                    {
                        return;
                    }

                    // Request that the sync engine be reset the next time it starts.
                    Properties.Settings.Default.ShouldResetSync = true;
                    Properties.Settings.Default.Save();
                }

            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: ResetSync: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// The sync status for this SyncBox has changed.  Pass this event to the sync status view.
        /// </summary>
        /// <param name="userState">This is the instance of CLSync.</param>
        private void OnSyncStatusUpdated(object userState)
        {
            if (_winSyncStatus != null)
            {
                _winSyncStatus.OnSyncStatusUpdated(userState);
            }
        }

        /// <summary>
        /// Push notification died.  Sync will no longer be notified when files or folders change on other devices.
        /// Changes made to the files or folders on this device will still be synced to the server, and when that
        /// occurs the server will return any changes made on other devices.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnPushNotificationError(object sender, Cloud.PushNotification.NotificationErrorEventArgs e)
        {
            string errorMsg = "Push notification stopped.  Changes on other devices will no longer be automatically synced to this device.";
            CLError error = new Exception(errorMsg);
            error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
            _trace.writeToLog(1, "MainViewModel: OnPushNotificationError: ERROR: Exception: Msg: <{0}>.", error.errorDescription);
            NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", error.errorDescription) });
        }

        /// <summary>
        /// Stop syncing the SyncBox.
        /// </summary>
        private void StopSyncing()
        {
            try
            {
                lock (_locker)
                {
                    if (_winSyncStatus != null)
                    {
                        EventMessageReceiver.EventMessageReceiver vm = _winSyncStatus.DataContext as EventMessageReceiver.EventMessageReceiver;
                        if (vm != null)
                        {
                            try
                            {
                                vm.Dispose();
                            }
                            catch
                            {
                            }
                            _winSyncStatus.DataContext = null;
                        }

                        _winSyncStatus.AllowClose = true;
                        _winSyncStatus.Close();
                        _winSyncStatus = null;
                    }

                    if (_syncEngine != null)
                    {
                        SetSyncBoxStartedState(isStartedStateToSet: false);
                        _syncStarted = false;
                        _syncEngine.Stop();
                        _syncEngine = null;
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: StopSyncing: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Generate a random device ID.
        /// </summary>
        private void GenerateDeviceId()
        {
            try
            {
                DeviceId = Environment.MachineName + Guid.NewGuid().ToString("N");
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: GenerateDeviceId: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Exit the application.
        /// </summary>
        private void Exit()
        {
            try
            {
                if (!_settingsInitial.Equals(_settingsCurrent))
                {
                    // Notify the view to put up a MessageBox saying that the settings have changed.  Does the user want to exit anyway?
                    if (NotifySettingsChanged != null)
                    {
                        NotifySettingsChanged(this, new NotificationEventArgs<string, bool> { Completed = UserWantsToExit });
                    }
                }
                else
                {
                    // Stop syncing if it has been started.
                    StopSyncing();
                    CLSyncEngine.Shutdown(); // kills constant scheduling threads which run forever and prevent application shutdown

                    // Close the window
                    _windowClosed = true;
                    CloseCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: Exit: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        #endregion

        #region Command Helpers

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
        /// Returns true if the sync engine can be reset.
        /// </summary>
        private bool CanResetSync
        {
            get
            {
                return !_syncStarted;
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
                return !_syncStarted && !CanSaveSettings;
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

        /// <summary>
        /// Returns true if the Install Badging button should be active.
        /// </summary>
        private bool CanInstallBadging
        {
            get
            {
                return !_syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the Uninstall Badging button should be active.
        /// </summary>
        private bool CanUninstallBadging
        {
            get
            {
                return !_syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the Generate button (generate device ID) should be active.
        /// </summary>
        private bool CanGenerateDeviceId
        {
            get
            {
                return !_syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the Exit button should be active.
        /// </summary>
        private bool CanExit
        {
            get
            {
                return !_syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the Advanced Options button should be active.
        /// </summary>
        private bool CanShowAdvancedOptions
        {
            get
            {
                return !_syncStarted;
            }
        }

        /// <summary>
        /// Returns true if the SyncBox Folder Browse button should be active.
        /// </summary>
        private bool CanBrowseSyncBoxFolder
        {
            get
            {
                return !_syncStarted;
            }
        }

        #endregion

        #region Event Callbacks

        private void MessageEvents_NewEventMessage(EventMessageArgs e)
        {
            //// you can pull out a formatted message regardless of message type
            //string messageText = e.Message.Message;

            //// all messages have properties to identify the unique combination of SyncBoxId and DeviceId,
            //// but some messages can be fired without a sync box and thus both will be null
            //Nullable<long> syncBoxId = e.Message.SyncBoxId;
            //string deviceId = e.Message.DeviceId;

            // switch on the message type for the special cases we wish to handle here
            switch (e.Message.Type)
            {
                case EventMessageType.Error: // error type
                    //// cast as error
                    Cloud.Model.EventMessages.ErrorMessage errMessage = (Cloud.Model.EventMessages.ErrorMessage)e.Message;

                    //// enumerated rating of the presumed weight of message importance
                    //errMessage.Importance
                    
                    // ErrorInfo has additional error information such as ErrorType and possibly even more information for certain types
                    // switch on ErrorInfo ErrorType for the types we wish to handle
                    switch (errMessage.ErrorInfo.ErrorType)
                    {
                        case ErrorMessageType.HaltAllOfCloudSDK: // entire SDK halted type (unrecoverable error requiring restarting the process running the Cloud SDK and possibly a call to [instance of CLSyncEngine].ResetSync)
                            // cast as halt all info
                            Cloud.Model.EventMessages.ErrorInfo.HaltAllOfCloudSDKErrorInfo haltAllInfo = (Cloud.Model.EventMessages.ErrorInfo.HaltAllOfCloudSDKErrorInfo)errMessage.ErrorInfo;

                            if (NotifyException != null)
                            {
                                NotifyException(this,
                                    new NotificationEventArgs<CLError>()
                                    {
                                        Data = new Exception(e.Message.Message),
                                        Message = String.Format("Cloud SDK had an unrecoverable error. The application must be restarted. If you see this message again, then click Reset Sync." +
                                            " Error message: {0}", e.Message.Message)
                                    });
                            }
                            break;

                        case ErrorMessageType.HaltSyncEngineOnAuthenticationFailure: // authentication failure type
                            // cast as authentication info
                            Cloud.Model.EventMessages.ErrorInfo.HaltSyncEngineOnAuthenticationFailureErrorInfo authInfo = (Cloud.Model.EventMessages.ErrorInfo.HaltSyncEngineOnAuthenticationFailureErrorInfo)errMessage.ErrorInfo;

                            // authentication failure has additional data in its info
                            // so switch on whether the authentication failure was due to an expired token
                            if (authInfo.TokenExpired)
                            {
                                // token is expired, but otherwise credentials were fine

                                if (NotifyException != null)
                                {
                                    NotifyException(this,
                                        new NotificationEventArgs<CLError>()
                                        {
                                            Data = new Exception(e.Message.Message),
                                            Message = String.Format("Temporary credentials expired for local SyncBox with id {0} and device id {1}. Restart sync with new credentials. Error message: {2}",
                                                (e.Message.SyncBoxId == null
                                                    ? "{null}"
                                                    : ((long)e.Message.SyncBoxId).ToString()),
                                                e.Message.DeviceId ?? "{null}",
                                                e.Message.Message)
                                        });
                                }
                            }
                            else if (NotifyException != null)
                            {
                                // general problem with credentials (probably a bad key\secret or sync box id)

                                NotifyException(this,
                                    new NotificationEventArgs<CLError>()
                                    {
                                        Data = new Exception(e.Message.Message),
                                        Message = String.Format("Incorrect credentials for local SyncBox with id {0} and device id {1}. Error message: {2}",
                                            (e.Message.SyncBoxId == null
                                                ? "{null}"
                                                : ((long)e.Message.SyncBoxId).ToString()),
                                            e.Message.DeviceId ?? "{null}",
                                            e.Message.Message)
                                    });
                            }
                            break;

                        case ErrorMessageType.HaltSyncEngineOnConnectionFailure: // unable to establish route to server type
                            // cast as connection failure info
                            Cloud.Model.EventMessages.ErrorInfo.HaltSyncEngineOnConnectionFailureErrorInfo connInfo = (Cloud.Model.EventMessages.ErrorInfo.HaltSyncEngineOnConnectionFailureErrorInfo)errMessage.ErrorInfo;

                            if (NotifyException != null)
                            {
                                NotifyException(this,
                                    new NotificationEventArgs<CLError>()
                                    {
                                        Data = new Exception(e.Message.Message),
                                        Message = String.Format("Unable to establish route to server. Error message: {0}", e.Message.Message)
                                    });
                            }
                            break;

                        case ErrorMessageType.General: // general error type
                            // cast as general info
                            Cloud.Model.EventMessages.ErrorInfo.GeneralErrorInfo genInfo = (Cloud.Model.EventMessages.ErrorInfo.GeneralErrorInfo)errMessage.ErrorInfo;

                            // general errors occur as normal processing, they are logged in the Sync Status view
                            // (no need to notify on exception)
                            break;

                        default: // unknown error type

                            // SDK has been updated with new error message types which need to be added as new cases above;
                            // for now, notify on exception

                            if (NotifyException != null)
                            {
                                NotifyException(this,
                                    new NotificationEventArgs<CLError>()
                                    {
                                        Data = new Exception(e.Message.Message),
                                        Message = String.Format("Unhandled type of error. Type: {0}. SyncBox id: {1}. Device id: {2}. Error message: {3}.",
                                            errMessage.ErrorInfo.ErrorType.ToString(),
                                            (e.Message.SyncBoxId == null
                                                ? "{null}"
                                                : ((long)e.Message.SyncBoxId).ToString()),
                                            e.Message.DeviceId ?? "{null}",
                                            e.Message.Message)
                                    });
                            }
                            break;
                    }

                    break;

                case EventMessageType.Informational: // information type
                    //// cast as information
                    //Cloud.Model.EventMessages.InformationalMessage infoMessage = (Cloud.Model.EventMessages.InformationalMessage)e.Message;

                    //// enumerated rating of the presumed weight of message importance
                    //infoMessage.Importance

                    // information messages occur as normal processing, they are logged in the Sync Status view
                    // (no need to handle here)

                    //break;

                case EventMessageType.UploadingCountChanged: // uploading count type
                    //// cast as uploading count
                    //Cloud.Model.EventMessages.UploadingCountMessage uploadingCountMessage = (Cloud.Model.EventMessages.UploadingCountMessage)e.Message;

                    //// the combined count of uploading files and files queued for upload
                    //uploadingCountMessage.Count
                    //break;

                case EventMessageType.DownloadingCountChanged: // downloading count type
                    //// cast as downloading count
                    //Cloud.Model.EventMessages.DownloadingCountMessage downloadingCountMessage = (Cloud.Model.EventMessages.DownloadingCountMessage)e.Message;

                    //// the combined count of downloading files and files queued for download
                    //downloadingCountMessage.Count
                    //break;

                case EventMessageType.UploadProgress: // upload progress type
                    //// cast as upload progress
                    //Cloud.Model.EventMessages.UploadProgressMessage uploadProgressMessage = (Cloud.Model.EventMessages.UploadProgressMessage)e.Message;

                    //// the unique id for the upload change on the client, can be used in method [CLSyncEngine instance].QueryFileChangeByEventId to lookup additional event details
                    //uploadProgressMessage.EventId

                    //// additional parameters to signify a file's transfer progress
                    //uploadProgressMessage.Parameters
                        //.TransferStartTime <-- UTC DateTime when transfer started
                        //.RelativePath <-- relative path to the file starting at the SyncRoot
                        //.ByteSize <-- total byte size of the file to transfer
                        //.ByteProgress <-- number of bytes transferred so far, if it is equal to the ByteSize then the transfer is complete
                    //break;

                case EventMessageType.DownloadProgress: // download progress type
                    //// cast as download progress
                    //Cloud.Model.EventMessages.DownloadProgressMessage downloadProgressMessage = (Cloud.Model.EventMessages.DownloadProgressMessage)e.Message;

                    //// the unique id for the upload change on the client, can be used in method [CLSyncEngine instance].QueryFileChangeByEventId to lookup additional event details
                    //downloadProgressMessage.EventId

                    //// additional parameters to signify a file's transfer progress
                    //downloadProgressMessage.Parameters
                        //.TransferStartTime <-- UTC DateTime when transfer started
                        //.RelativePath <-- relative path to the file starting at the SyncRoot
                        //.ByteSize <-- total byte size of the file to transfer
                        //.ByteProgress <-- number of bytes transferred so far, if it is equal to the ByteSize then the transfer is complete
                    //break;

                case EventMessageType.SuccessfulUploadsIncremented: // uploads incremented type
                    //// cast as uploads incremented
                    //Cloud.Model.EventMessages.SuccessfulUploadsIncrementedMessage uploadsIncrementMessage = (Cloud.Model.EventMessages.SuccessfulUploadsIncrementedMessage)e.Message;

                    //// the amount to increment (the number of finished transfers)
                    //uploadsIncrementMessage.Count
                    //break;

                case EventMessageType.SuccessfulDownloadsIncremented: // downloads incremented type
                    //// cast as downloads incremented
                    //Cloud.Model.EventMessages.SuccessfulDownloadsIncrementedMessage downloadIncrementMessage = (Cloud.Model.EventMessages.SuccessfulDownloadsIncrementedMessage)e.Message;

                    //// the amount to increment (the number of finished transfers)
                    //downloadIncrementMessage.Count
                    //break;

                default: // unknown type of message, added in an update to the SDK
                    break;
            }
            
            e.MarkHandled();
        }

        private void OnSetHistoricBandwidthSettings(double historicUploadBandwidthBitsPS, double historicDownloadBandwidthBitsPS)
        {
            Properties.Settings.Default.HistoricUploadBandwidthBitsPS = historicUploadBandwidthBitsPS;
            Properties.Settings.Default.HistoricDownloadBandwidthBitsPS = historicDownloadBandwidthBitsPS;
        }

        private void OnGetHistoricBandwidthSettings(out double historicUploadBandwidthBitsPS, out double historicDownloadBandwidthBitsPS)
        {
            historicUploadBandwidthBitsPS = Properties.Settings.Default.HistoricUploadBandwidthBitsPS;
            historicDownloadBandwidthBitsPS = Properties.Settings.Default.HistoricDownloadBandwidthBitsPS;
        }

        #endregion

        #region Public Methods called by view
        
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

            // Forward this request to our own Exit method.
            Dispatcher dispatcher = Application.Current.Dispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            {
                Exit();
            });

            return true;                // cancel the window close
        }

        #endregion

        #region Private Support Functions

        private bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        /// <summary>
        /// Start Explorer
        /// </summary>
        /// <returns>string: The path to the Explorer.exe file.</returns>
        private static void StartExplorer()
        {
            try
            {
                // Start Explorer
                string explorerLocation = "\" start " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") + "\"";
                string commandLocation = Environment.SystemDirectory + "\\cmd.exe";

                // Start explorer as a medium integrity process for Vista and above.
                // Note: For Windows 8, the Metro mode will be disabled if Explorer is started with Administrator privileges.  That could
                // happen if this app is started to "runas" Administrator.
                if (System.Environment.OSVersion.Version.Major >= 6)
                {
                    string commandLine = "\"" + commandLocation + "\" /c " + explorerLocation;
                    _trace.writeToLog(9, "MainViewModel: StartExplorer: Create medium integrity process. Command line: <{0}>.", commandLine);
                    SampleLiveSync.Static.Helpers.CreateMediumIntegrityProcess(commandLine, NativeMethods.CreateProcessFlags.CREATE_NEW_PROCESS_GROUP);
                }
                else
                {
                    _trace.writeToLog(9, "MainViewModel: StartExplorer: Create normal process. Explorer location: <{0}>. Cmd location: <{1}.", explorerLocation, commandLocation);
                    ProcessStartInfo taskStartInfo = new ProcessStartInfo();
                    taskStartInfo.CreateNoWindow = true;
                    taskStartInfo.UseShellExecute = true;
                    taskStartInfo.FileName = commandLocation;
                    taskStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    taskStartInfo.Arguments = explorerLocation;
                    _trace.writeToLog(9, "MainViewModel: StartExplorer: Start explorer.");
                    Process.Start(taskStartInfo);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: StartExplorer: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        /// <summary>
        /// Stop Explorer
        /// </summary>
        /// <returns>string: The path to the Explorer.exe file.</returns>
        private static string StopExplorer()
        {
            string explorerLocation = String.Empty;
            try
            {
                // Kill Explorer
                explorerLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                _trace.writeToLog(9, "MainViewModel: StopExplorer: Entry. Explorer location: <{0}>.", explorerLocation);
                ProcessStartInfo taskKillInfo = new ProcessStartInfo();
                taskKillInfo.CreateNoWindow = true;
                taskKillInfo.UseShellExecute = false;
                taskKillInfo.FileName = "cmd.exe";
                taskKillInfo.WindowStyle = ProcessWindowStyle.Hidden;
                taskKillInfo.Arguments = "/C taskkill /F /IM explorer.exe";
                _trace.writeToLog(9, "MainViewModel: StopExplorer: Start the command.");
                Process.Start(taskKillInfo);

                // Wait for all Explorer processes to stop.
                const int maxProcessWaits = 40; // corresponds to trying for 20 seconds (if each iteration waits 500 milliseconds)
                for (int waitCounter = 0; waitCounter < maxProcessWaits; waitCounter++)
                {
                    // For some reason this won't work unless we wait here for a bit.
                    Thread.Sleep(500);
                    if (!IsExplorerRunning(explorerLocation))
                    {
                        _trace.writeToLog(9, "MainViewModel: StopExplorer: Explorer is not running.  Break.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: StopExplorer: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
            _trace.writeToLog(9, "MainViewModel: StopExplorer: Return. explorerLocation: <{0}>.", explorerLocation);
            return explorerLocation;
        }

        /// <summary>
        /// Test whether Explorer is running.
        /// </summary>
        /// <param name="explorerLocation"></param>
        /// <returns></returns>
        private static bool IsExplorerRunning(string explorerLocation)
        {
            bool isExplorerRunning = false;         // assume not running

            try
            {
                _trace.writeToLog(9, "MainViewModel: IsExplorerRunning: Entry. explorerLocation: <{0}>.", explorerLocation);
                string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    if (searcher != null)
                    {
                        _trace.writeToLog(9, "MainViewModel: IsExplorerRunning: searcher not null. Get the results.");
                        using (ManagementObjectCollection results = searcher.Get())
                        {
                            _trace.writeToLog(9, "MainViewModel: IsExplorerRunning: Run the query.");
                            isExplorerRunning = Process.GetProcesses()
                                .Where(parent => parent.ProcessName.Equals("explorer", StringComparison.InvariantCultureIgnoreCase))
                                .Join(results.Cast<ManagementObject>(),
                                    parent => parent.Id,
                                    parent => (int)(uint)parent["ProcessId"],
                                    (outer, inner) => new ProcessWithPath(outer, (string)inner["ExecutablePath"]))
                                .Any(parent => string.Equals(parent.Path, explorerLocation, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }
                    else
                    {
                        // searcher is null.
                        _trace.writeToLog(1, "MainViewModel: IsExplorerRunning: ERROR: searcher is null.");
                        return isExplorerRunning;           // assume Explorer is not running.
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: IsExplorerRunning: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }

            return isExplorerRunning;
        }

        /// <summary>
        /// Set the SyncBox started state.
        /// </summary>
        /// <param name="isStartedStateToSet">The state to set.</param>
        private void SetSyncBoxStartedState(bool isStartedStateToSet)
        {
            _syncStarted = isStartedStateToSet;

            // Set the TextBox dependent properties.
            TbSyncBoxFolderEnabled = !isStartedStateToSet;
            TbKeyEnabled = !isStartedStateToSet;
            TbSecretEnabled= !isStartedStateToSet;
            TbTokenEnabled = !isStartedStateToSet;
            TbSyncBoxIdEnabled = !isStartedStateToSet;
            TbUniqueDeviceIdEnabled = !isStartedStateToSet;
        }

        /// <summary>
        /// The function determines whether the current operating system is a 
        /// 64-bit operating system.
        /// </summary>
        /// <returns>
        /// The function returns true if the operating system is 64-bit; 
        /// otherwise, it returns false.
        /// </returns>
        /// <remarks>From: http://1code.codeplex.com/SourceControl/changeset/view/39074#842775</remarks>
        private static bool Is64BitOperatingSystem()
        {
            if (IntPtr.Size == 8)  // 64-bit programs run only on Win64
            {
                return true;
            }
            else  // 32-bit programs run on both 32-bit and 64-bit Windows
            {
                // Detect whether the current process is a 32-bit process 
                // running on a 64-bit system.
                bool flag;
                return ((DoesWin32MethodExist("kernel32.dll", "IsWow64Process") &&
                    NativeMethods.IsWow64Process(NativeMethods.GetCurrentProcess(), out flag)) && flag);
            }
        }

        /// <summary>
        /// Get the 32-bit system directory:
        /// On x86: %windir%\System32.
        /// On x64: %windir%\SysWow64.
        /// </summary>
        /// <returns></returns>
        private static string Get32BitSystemDirectory()
        {
            StringBuilder path = new StringBuilder(260);
            NativeMethods.SHGetSpecialFolderPath(IntPtr.Zero, path, 0x0029, false);
            return path.ToString();
        }

        /// <summary>
        /// The function determins whether a method exists in the export 
        /// table of a certain module.
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <param name="methodName">The name of the method</param>
        /// <returns>
        /// The function returns true if the method specified by methodName 
        /// exists in the export table of the module specified by moduleName.
        /// </returns>
        private static bool DoesWin32MethodExist(string moduleName, string methodName)
        {
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
            {
                return false;
            }
            return (NativeMethods.GetProcAddress(moduleHandle, methodName) != IntPtr.Zero);
        }

        /// <summary>
        /// Used by IsExplorerRunning.
        /// </summary>
        private class ProcessWithPath
        {
            public Process Process { get; private set; }
            public string Path { get; private set; }

            public ProcessWithPath(Process process, string path)
            {
                this.Process = process;
                this.Path = path;
            }

            public override string ToString()
            {
                return (this.Process == null
                    ? "null"
                    : this.Process.ProcessName);
            }
        }

        #endregion
    }
}