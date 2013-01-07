using CloudApiPublic.Interfaces;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using CloudSdkSyncSample.Models;
using CloudSdkSyncSample.Support;
using CloudSdkSyncSample.Views;
using CloudSdkSyncSample.Static;
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
using CloudApiPublic.EventMessageReceiver;
using System.Threading;
using System.Management;

namespace CloudSdkSyncSample.ViewModels
{
    public class MainViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // RelayCommands
        RelayCommand<object> _commandBrowseSyncBoxFolder;
        RelayCommand<object> _commandShowAdvancedOptions;
        RelayCommand<object> _commandSaveSettings;
        RelayCommand<object> _commandInstallBadging;
        RelayCommand<object> _commandUninstallBadging;
        RelayCommand<object> _commandShowSyncStatus;
        RelayCommand<object> _commandStartSyncing;
        RelayCommand<object> _commandStopSyncing;
        RelayCommand<object> _commandExit;

        // Private fields
        private Settings _settingsCurrent = null;
        private Settings _settingsInitial = null;
        private Window _mainWindow = null;
        private bool _syncStarted = false;
        private bool _windowClosed = false;
        private CLSync _syncBox = null;
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
            CLTrace.Initialize(_settingsInitial.TraceFolderFullPath, "CloudSdkSyncSample", "log", _settingsInitial.TraceLevel, _settingsInitial.LogErrors);
        }

        public MainViewModel()
        {
            throw new Exception("Default constructor not supported.");
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

        public bool TbApplicationKeyEnabled
        {
            get { return _tbApplicationKeyEnabled; }
            set
            {
                if (value == _tbApplicationKeyEnabled)
                {
                    return;
                }

                _tbApplicationKeyEnabled = value;

                base.OnPropertyChanged("TbApplicationKeyEnabled");
            }
        }
        private bool _tbApplicationKeyEnabled = true;

        public bool TbApplicationSecretEnabled
        {
            get { return _tbApplicationSecretEnabled; }
            set
            {
                if (value == _tbApplicationSecretEnabled)
                {
                    return;
                }

                _tbApplicationSecretEnabled = value;

                base.OnPropertyChanged("TbApplicationSecretEnabled");
            }
        }
        private bool _tbApplicationSecretEnabled = true;

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
                        param => this.CanAdvancedOptions
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

        #region Public Methods

        /// <summary>
        /// Browse to locate a folder to be synced.
        /// </summary>
        public void BrowseSyncBoxFolder()
        {
            // Notify the view to put up the folder selector.
            if (NotifyBrowseSyncBoxFolder != null)
            {
                NotifyBrowseSyncBoxFolder(this, new NotificationEventArgs());
            }
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
            if (String.IsNullOrEmpty(SyncBoxId))
            {
                MessageBox.Show("The SyncBox ID must not be specified.");
                this.IsSyncBoxIdFocused = true;
                return;
            }

            // Validate the Device ID.
            if (String.IsNullOrEmpty(DeviceId))
            {
                MessageBox.Show("The Device ID must be specified.");
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

            // Reinitialize trace
            CLTrace.Initialize(_settingsInitial.TraceFolderFullPath, "CloudSdkSyncSample", "log", _settingsInitial.TraceLevel, 
                                _settingsInitial.LogErrors, willForceReset: true);
        }

        /// <summary>
        /// Determine whether we should request that the sync database be deleted when the SyncBox is started.
        /// </summary>
        private bool ShouldWeRequestSyncDatabaseDeletion()
        {
            if (!string.Equals(_settingsCurrent.SyncBoxFullPath, _settingsInitial.SyncBoxFullPath, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(_settingsCurrent.ApplicationKey, _settingsInitial.ApplicationKey, StringComparison.InvariantCultureIgnoreCase) ||
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
            try
            {
                // Stop Explorer
                StopExplorer();

                // Determine the path to the proper 64- or 32-bit .dll file to register.
                string commandArguments = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (IntPtr.Size == 4)
                {
                    // 32-bit 
                    commandArguments += "\\x86\\BadgeCom.dll";
                }
                else
                {
                    // 64-bit 
                    commandArguments += "\\amd64\\BadgeCom.dll";
                }
                commandArguments = "\"" + commandArguments + "\"";
                commandArguments = "/s " + commandArguments;

                // Build the command line: regsvr32 <path to the proper BadgeCom.dll>
                string commandProgram = "regsvr32";

                // Launch the process
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = true;
                startInfo.FileName = commandProgram;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = commandArguments;
                if (System.Environment.OSVersion.Version.Major >= 6)
                {
                    startInfo.Verb = "runas";
                }
                _trace.writeToLog(1, "MainViewModel: InstallBadging: Start process to run regsvr32. Program: {0}. Arguments: {1}.", commandProgram, commandArguments);
                regsvr32Process = Process.Start(startInfo);

                // Wait for the process to exit
                if (regsvr32Process.WaitForExit(5000))
                {
                    // Process has exited.  Get the return code.
                    int retCode = regsvr32Process.ExitCode;
                    if (retCode != 0)
                    {
                        // Error return code
                        CLError error = new Exception(String.Format("Error registering BadgeCom.dll.  Code: {0}.", retCode));
                        _trace.writeToLog(1, "MainViewModel: InstallBadging: Error registering BadgeCom.dll. Code: {0}.", retCode);
                        NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = "Error registering BadgeCom.dll." });
                    }
                }
                else
                {
                    // Timed out.
                    CLError error = new Exception("Error: Timeout registering BadgeCom.dll.");
                    _trace.writeToLog(1, "MainViewModel: InstallBadging: Error. Timeout registering BadgeCom.dll.");
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = "Error: Timeout registering BadgeCom.dll." });
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
            try
            {
                // Stop Explorer
                StopExplorer();

                // Determine the path to the proper 64- or 32-bit .dll file to register.
                string commandArguments = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (IntPtr.Size == 4)
                {
                    // 32-bit 
                    commandArguments += "\\x86\\BadgeCom.dll";
                }
                else
                {
                    // 64-bit 
                    commandArguments += "\\amd64\\BadgeCom.dll";
                }
                commandArguments = "\"" + commandArguments + "\"";
                commandArguments = "/u /s " + commandArguments;

                // Build the command line: regsvr32 <path to the proper BadgeCom.dll>
                string commandProgram = "regsvr32";

                // Launch the process
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = true;
                startInfo.FileName = commandProgram;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = commandArguments;
                if (System.Environment.OSVersion.Version.Major >= 6)
                {
                    startInfo.Verb = "runas";
                }
                regsvr32Process = Process.Start(startInfo);

                // Wait for the process to exit
                if (regsvr32Process.WaitForExit(5000))
                {
                    // Process has exited.  Get the return code.
                    int retCode = regsvr32Process.ExitCode;
                    if (retCode != 0)
                    {
                        // Error return code
                        CLError error = new Exception(String.Format("Error unregistering BadgeCom.dll.  Code: {0}.", retCode));
                        _trace.writeToLog(1, "MainViewModel: UninstallBadging: Error unregistering BadgeCom.dll. Code: {0}.", retCode);
                        NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = "Error unregistering BadgeCom.dll." });
                    }
                }
                else
                {
                    // Timed out.
                    CLError error = new Exception("Error: Timeout registering BadgeCom.dll.");
                    _trace.writeToLog(1, "MainViewModel: UninstallBadging: Error: Timeout unregistering BadgeCom.dll.");
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = "Error: Timeout unregistering BadgeCom.dll." });
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
        public void ShowSyncStatus()
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

        /// <summary>
        /// Start syncing the SyncBox.
        /// </summary>
        public void StartSyncing()
        {
            try
            {
                bool startSyncBox = false;
                lock (_locker)
                {
                    if (_syncBox == null)
                    {
                        _syncBox = new CLSync();
                        startSyncBox = true;

                        // Reset the sync database if we should
                        if (Properties.Settings.Default.ShouldResetSync)
                        {
                            Properties.Settings.Default.ShouldResetSync = false;
                            Properties.Settings.Default.Save();
                            CLError errorFromSyncReset = _syncBox.SyncReset(SettingsAvancedImpl.Instance);
                            if (errorFromSyncReset != null)
                            {
                                _syncBox = null;
                                startSyncBox = false;
                                if (NotifyException != null)
                                {
                                    _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From SyncBox.SyncReset: Msg: <{0}.", errorFromSyncReset.errorDescription);
                                    NotifyException(this, new NotificationEventArgs<CLError>()
                                    {
                                        Data = errorFromSyncReset,
                                        Message = "Error resetting the SyncBox: {0}."
                                    });
                                }
                            }
                        }
                    }
                }

                if (startSyncBox)
                {
                    CLSyncStartStatus startStatus;
                    CLError errorFromSyncBoxStart = _syncBox.Start(SettingsAvancedImpl.Instance, out startStatus);
                    if (errorFromSyncBoxStart != null)
                    {
                        _syncBox = null;
                        if (NotifyException != null)
                        {
                            _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: From SyncBox.Start: Msg: <{0}.", errorFromSyncBoxStart.errorDescription);
                            NotifyException(this, new NotificationEventArgs<CLError>() { Data = errorFromSyncBoxStart, Message = String.Format("Error starting the SyncBox: {0}.", startStatus.ToString()) });
                        }
                    }
                    else
                    {
                        lock (_locker)
                        {
                            // Sync has started
                            SetSyncBoxStartedState(isStartedStateToSet: true);

                            // Start an instance of the sync status window and start it hidden.
                            if (_winSyncStatus == null)
                            {
                                _trace.writeToLog(9, "MainViewModel: StartSyncing: Start the sync status window.");
                                _winSyncStatus = new SyncStatusView();
                                EventMessageReceiver vm = EventMessageReceiver.GetInstance(OnGetHistoricBandwidthSettings, OnSetHistoricBandwidthSettings);
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
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: StartSyncing: ERROR: Exception: Msg: <{0}.", ex.Message);
            }
        }

        /// <summary>
        /// Stop syncing the SyncBox.
        /// </summary>
        public void StopSyncing()
        {
            lock (_locker)
            {
                if (_winSyncStatus != null)
                {
                    _winSyncStatus.AllowClose = true;
                    _winSyncStatus.Close();
                    _winSyncStatus = null;
                }

                if (_syncBox != null)
                {
                    SetSyncBoxStartedState(isStartedStateToSet: false);
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
                if (NotifySettingsChanged != null)
                {
                    NotifySettingsChanged(this, new NotificationEventArgs<string, bool> { Completed = UserWantsToExit });
                }
            }
            else
            {
                // Stop syncing if it has been started.
                StopSyncing();
                CLSync.ShutdownSchedulers();

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
        private bool CanAdvancedOptions
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
                string explorerLocation = String.Empty;
                explorerLocation = "\"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") + "\"";
                _trace.writeToLog(9, "MainViewModel: StartExplorer: Entry. Explorer location: <{0}>.", explorerLocation);
                ProcessStartInfo taskStartInfo = new ProcessStartInfo();
                taskStartInfo.CreateNoWindow = true;
                taskStartInfo.UseShellExecute = false;
                taskStartInfo.FileName = explorerLocation;
                taskStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                taskStartInfo.Arguments = String.Empty;
                _trace.writeToLog(9, "MainViewModel: StartExplorer: Start explorer.");
                Process.Start(taskStartInfo);

            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "MainViewModel: StartExplorer: ERROR: Exception: Msg: <{0}.", ex.Message);
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
                _trace.writeToLog(1, "MainViewModel: StopExplorer: ERROR: Exception: Msg: <{0}.", ex.Message);
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

        private void SetSyncBoxStartedState(bool isStartedStateToSet)
        {
            _syncStarted = isStartedStateToSet;

            // Set the TextBox dependent properties.
            TbSyncBoxFolderEnabled = !isStartedStateToSet;
            TbApplicationKeyEnabled = !isStartedStateToSet;
            TbApplicationSecretEnabled= !isStartedStateToSet;
            TbSyncBoxIdEnabled = !isStartedStateToSet;
            TbUniqueDeviceIdEnabled = !isStartedStateToSet;
        }

        #endregion
    }
}
