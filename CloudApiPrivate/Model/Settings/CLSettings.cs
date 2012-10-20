﻿//
//  CLSettings.cs
//  Cloud Windows
//
//  Created by BobS on 4/30/12.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System.IO.IsolatedStorage;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using CloudApiPrivate;
using CloudApiPrivate.Static;
using CloudApiPublic.Static;
using CloudApiPrivate.Static.FriendlyEnumValues;
using CloudApiPrivate.Resources;
using CloudApiPublic.Model;
using System.Windows;
using System.Reflection;
using CloudApiPrivate.Common;
using CloudApiPublic.Support;

namespace CloudApiPrivate.Model.Settings
{
    #region "Enums"

    public enum StorageSizeSelections
    {
        Size5Gb = 5,
        Size50Gb = 50,
        Size500Gb = 500,
    }

    public enum cloudAppLanguageType
    {
        cloudAppLanguageEN = 0,
        cloudAppLanguageES = 1,
        cloudAppLanguagePT = 2,
        cloudAppLanguageFR = 3,
        cloudAppLanguageIT = 4,
        cloudAppLanguageGE = 5,
        cloudAppLanguageJP = 6,
        cloudAppLanguageCN = 7,
    };

    public enum uploadSpeedLimitType
    {
        uploadSpeedLimitDontLimit = 0,
        uploadSpeedLimitAutoLimit = 1,
        uploadSpeedLimitLimitTo = 2,
    };

    public enum useProxySettingType
    {
        useProxySettingNoProxy = 0,
        useProxySettingAutoDetect = 1,
        useProxySettingManual = 2,
    };

    public enum useProxyTypes
    {
        [LocalizableDescription("useProxyHTTP", typeof(Resources.Resources))]
        useProxyHTTP = 0,
        [LocalizableDescription("useProxySOCK4", typeof(Resources.Resources))]
        useProxySOCK4 = 1,
        [LocalizableDescription("useProxySOCK5", typeof(Resources.Resources))]
        useProxySOCK5 = 2,
    };

    public enum buttonState
    {
        stateOFF = 0,
        stateON = 1,
        stateMIXED = 2,
    };

    #endregion

    #region "SettingsBase class"

    /// <summary>
    /// Static class for accessing the application settings.
    /// Use these functions as follows:
    ///     // Save the most recent filename setting
    ///     string writeFileName = "MyDocument.xml";
    ///     SettingsBase.Write<string>("MostRecentFileName", writeFileName);
    ///     // Restore the most recent filename setting
    ///     string readFileName = SettingsBase.Read<string>("MostRecentFileName", "Untitled.xml");    
    /// </summary>
    public static class SettingsBase    
    {        
        public static Boolean ReadIfPresent<TT>(string name, out TT value)
        {
            Boolean rc = false;
            value = default(TT);

            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
            if (settings != null && settings.Contains(name))
            {
                value = (TT)settings[name];
                rc = true;
            }
            return rc;
        }

        public static TT Read<TT>(string name)       
        {            
            return Read<TT>(name, default(TT));        
        }   
      
        public static TT Read<TT>(string name, TT defaultValue)        
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
            TT value;
            if(settings == null || !settings.TryGetValue<TT>(name, out value)) 
            {
                return defaultValue;            
            }
            return value;        
        }    
     
        public static void Write<TT>(string name, TT value)        
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
            if (settings == null) 
            {
                return;           
            }
            if (settings.Contains(name))
            {
                settings[name] = value;            
            }
            else    
            {
                settings.Add(name, value);            

            }
            settings.Save();        
        }    

        public static void Clear()        
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
            settings.Clear();
            settings.Save();        
        }
    }
    #endregion

    #region "Settings class"
    /// <summary>
    /// Singleton class to represent settings properties.
    /// </summary>
    public sealed class Settings
    {
        // Constant strings
        public const string kStartCloudAppWithSystem = "start_cloud_app_with_system";
        public const string kAnimateMenuBarForUpdates = "animate_menu_bar_for_updates";
        public const string kShowDesktopNotificationForUpdates = "show_desktop_notification_for_updates";
        public const string kUseColorIconForCloudFolder = "colored_folder_icon";
        public const string kCloudAppLanguage = "cloud_app_language";
        public const string kDateWeLastCheckedForSoftwareUpdate = "date_we_last_checked_for_software_update";
        public const string kUseDefaultSetup = "use_default_setup";
        public const string kUseLanForFileSync = "use_lan_for_file_sync";
        public const string kLimitDownloadSpeeds = "limit_download_speeds";
        public const string kDownloadSpeedLimit = "download_speed_limit";
        public const string kLimitUploadSpeeds = "limit_upload_speeds";
        public const string kUploadSpeedLimit = "upload_speed_limit";
        public const string kUseProxySetting = "use_proxy_settings";
        public const string kUseProxyType = "use_proxy_type";
        public const string kProxyServerAddress = "proxy_server_address";
        public const string kProxyServerPort = "proxy_server_port";
        public const string kProxyServerRequiresPassword = "proxy_server_requires_password";
        public const string kProxyServerUserName = "proxy_server_user_name";
        public const string kProxyServerPassword = "proxy_server_password";
        public const string kUserName = "user_name";
        public const string kUserFullName = "user_full_name";
        public const string kDeviceName = "device_name";
        public const string kUuid = "uuid";
        public const string kAKey = "akey";
        public const string kQuota = "q";
        public const string kCompletedSetup = "cs";
        public const string kCloudFolderPath = "cloud_folder_path";
        public const string kEid = "eid";
        public const string kSid = "sid";
        public const string kRecentFileItems = "recent_file_items";
        public const string kUdid = "device_udid";
        public const string kLogErrors = "log_errors";
        public const string kLogErrorLocation = "log_error_location";
        public const string kCloudFolderCreationTimeUtc = "cloud_folder_path_creation_time";
        public const string kMainWindowPlacement = "main_window_placement";
        public const string kTraceType = "trace_type";
        public const string kTraceLocation = "trace_location";
        public const string kShouldAddShowCloudFolderOnDesktop = "should_add_show_cloud_folder_on_desktop";
        public const string kShouldAddShowCloudFolderInExplorerFavorites = "should_add_show_cloud_folder_in_explorer_favorites";
        public const string kShouldAddShowCloudFolderInInternetExplorerFavorites = "should_add_show_cloud_folder_in_internet_explorer_favorites";
        public const string kShouldAddShowCloudFolderOnTaskbar = "should_add_show_cloud_folder_on_taskbar";
        public const string kShouldAddShowCloudFolderInStartMenu = "should_add_show_cloud_folder_in_start_menu";
        public const string kShouldAnimateToSystemTray = "should_animate_to_system_tray";
        public const string kIsMovingCloudFolder = "is_moving_cloud_folder";
        public const string kMovingCloudFolderTargetPath = "moving_cloud_folder_target_path";
        public const string kHistoryUploadBandwidthBitPS = "historic_upload_bandwidth_bitsps";
        public const string kHistoryDownloadBandwidthBitPS = "historic_download_bandwidth_bitsps";

        /// <summary>
        /// The persistent settings properties.
        /// </summary>
        
        // General
        private bool _startCloudAppWithSystem;
        public bool StartCloudAppWithSystem {
        get {return _startCloudAppWithSystem; } 
        set
            {
                if (value != _startCloudAppWithSystem)
                {
                    CLShortcuts.UpdateShouldStartCloudAppWithSystem(value, Settings.Instance.CloudFolderPath);
                }
                _startCloudAppWithSystem = value;
                SettingsBase.Write<bool>(kStartCloudAppWithSystem, value);
            }
        }

        private bool _animateMenuBarForUpdates;
        public bool AnimateMenuBarForUpdates
        {
            get { return _animateMenuBarForUpdates; }
            set
            {
                _animateMenuBarForUpdates = value;
                SettingsBase.Write<bool>(kAnimateMenuBarForUpdates, value);
            }
        }

        private bool _showDesktopNotificationForUpdates;
        public bool ShowDesktopNotificationForUpdates
        {
            get { return _showDesktopNotificationForUpdates; }
            set
            {
                _showDesktopNotificationForUpdates = value;
                SettingsBase.Write<bool>(kCloudAppLanguage, value);
            }
        }

        private bool _useColorIconForCloudFolder;
        public bool UseColorIconForCloudFolder
        {
            get { return _useColorIconForCloudFolder; }
            set
            {
                if (value != _useColorIconForCloudFolder)
                {
                    CLShortcuts.UpdateShouldUseCloudIconForCloudFolder(value, Settings.Instance.CloudFolderPath);
                }
                _useColorIconForCloudFolder = value;
                SettingsBase.Write<bool>(kUseColorIconForCloudFolder, value);
            }
        }

        private bool _isMovingCloudFolder;
        public bool IsMovingCloudFolder
        {
            get { return _isMovingCloudFolder; }
            set
            {
                if (value != _isMovingCloudFolder)
                {
                    _isMovingCloudFolder = value;
                    SettingsBase.Write<bool>(kIsMovingCloudFolder, value);
                }
            }
        }

        private int _cloudAppLanguage;
        public int CloudAppLanguage
        {
            get { return _cloudAppLanguage; }
            set
            {
                _cloudAppLanguage = value;
                SettingsBase.Write<int>(kCloudAppLanguage, value);
            }
        }

        private DateTime _dateWeLastCheckedForSoftwareUpdate;
        public DateTime DateWeLastCheckedForSoftwareUpdate
        {
            get { return _dateWeLastCheckedForSoftwareUpdate; }
            set
            {
                _dateWeLastCheckedForSoftwareUpdate = value;
                SettingsBase.Write<DateTime>(kDateWeLastCheckedForSoftwareUpdate, value);
            }
        }

        // Setup
        private Boolean _useDefaultSetup;
        public Boolean UseDefaultSetup
        {
            get { return _useDefaultSetup; }
            set
            {
                _useDefaultSetup = value;
                SettingsBase.Write<Boolean>(kUseDefaultSetup, value);
            }
        }

        // Bandwidth
        private bool _useLANForFileSync;
        public bool UseLANForFileSync
        {
            get { return _useLANForFileSync; }
            set
            {
                _useLANForFileSync = value;
                SettingsBase.Write<bool>(kUseLanForFileSync, value);
            }
        }

        private bool _limitDownloadSpeeds;
        public bool LimitDownloadSpeeds
        {
            get { return _limitDownloadSpeeds; }
            set
            {
                _limitDownloadSpeeds = value;
                SettingsBase.Write<bool>(kLimitDownloadSpeeds, value);
            }
        }

        private int _downloadSpeedLimit;
        public int DownloadSpeedLimit
        {
            get { return _downloadSpeedLimit; }
            set
            {
                _downloadSpeedLimit = value;
                SettingsBase.Write<int>(kDownloadSpeedLimit, value);
            }
        }

        private uploadSpeedLimitType _limitUploadSpeeds;
        public uploadSpeedLimitType LimitUploadSpeeds
        {
            get { return _limitUploadSpeeds; }
            set
            {
                _limitUploadSpeeds = value;
                SettingsBase.Write<uploadSpeedLimitType>(kLimitUploadSpeeds, value);
            }
        }

        private int _uploadSpeedLimit;
        public int UploadSpeedLimit
        {
            get { return _uploadSpeedLimit; }
            set
            {
                _uploadSpeedLimit = value;
                SettingsBase.Write<int>(kUploadSpeedLimit, value);
            }
        }

        private useProxySettingType _useProxySetting;
        public useProxySettingType UseProxySetting
        {
            get { return _useProxySetting; }
            set
            {
                _useProxySetting = value;
                SettingsBase.Write<useProxySettingType>(kUseProxySetting, value);
            }
        }

        private useProxyTypes _useProxyType;
        public useProxyTypes UseProxyType
        {
            get { return _useProxyType; }
            set
            {
                _useProxyType = value;
                SettingsBase.Write<useProxyTypes>(kUseProxyType, value);
            }
        }

        private string _proxyServerAddress;
        public string ProxyServerAddress
        {
            get { return _proxyServerAddress; }
            set
            {
                _proxyServerAddress = value;
                SettingsBase.Write<string>(kProxyServerAddress, value);
            }
        }

        private int _proxyServerPort;
        public int ProxyServerPort
        {
            get { return _proxyServerPort; }
            set
            {
                _proxyServerPort = value;
                SettingsBase.Write<int>(kProxyServerPort, value);
            }
        }

        private bool _proxyServerRequiresPassword;
        public bool ProxyServerRequiresPassword
        {
            get { return _proxyServerRequiresPassword; }
            set
            {
                _proxyServerRequiresPassword = value;
                SettingsBase.Write<bool>(kProxyServerRequiresPassword, value);
            }
        }

        private string _proxyServerUsername;
        public string ProxyServerUsername
        {
            get { return _proxyServerUsername; }
            set
            {
                _proxyServerUsername = value;
                SettingsBase.Write<string>(kProxyServerUserName, value);
            }
        }

        private string _proxyServerPassword;
        public string ProxyServerPassword
        {
            get { return _proxyServerPassword; }
            set
            {
                _proxyServerPassword = value;
                SettingsBase.Write<string>(kProxyServerPassword, value);
            }
        }

        // Account user name.  This is the user's email address.
        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                SettingsBase.Write<string>(kUserName, value);
            }
        }

        // Account full name.  This is at least two space-separated words.
        private string _userFullName;
        public string UserFullName
        {
            get { return _userFullName; }
            set
            {
                _userFullName = value;
                SettingsBase.Write<string>(kUserFullName, value);
            }
        }

        private string _deviceName;
        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                _deviceName = value;
                SettingsBase.Write<string>(kDeviceName, value);
            }
        }

        private string _uuid;
        public string Uuid
        {
            get { return _uuid; }
            set
            {
                _uuid = value;
                SettingsBase.Write<string>(kUuid, value);
            }
        }

        private string _akey;
        public string Akey
        {
            get { return _akey; }
            set
            {
                _akey = value;
                SettingsBase.Write<string>(kAKey, value);
            }
        }

        private int _quota;
        public int Quota
        {
            get { return _quota; }
            set
            {
                _quota = value;
                SettingsBase.Write<int>(kQuota, value);
            }
        }

        private string _udid;
        public string Udid
        {
            get { return _udid; }
            set {
                _udid = value;
                _trace.writeToLog(9, "CLSettings: Instance: Set new Udid: {0}.", _udid);
                SettingsBase.Write<string>(kUdid, value);
            }
        }
        

        private Boolean _completedSetup;
        public Boolean CompletedSetup
        {
            get { return _completedSetup; }
            set
            {
                _completedSetup = value;
                SettingsBase.Write<Boolean>(kCompletedSetup, value);
            }
        }

        // Advanced
        private string _cloudFolderPath;
        public string CloudFolderPath
        {
            get { return _cloudFolderPath; }
            set
            {
                _cloudFolderPath = value;
                SettingsBase.Write<string>(kCloudFolderPath, value);
            }
        }

        private string _movingCloudFolderTargetPath;
        public string MovingCloudFolderTargetPath
        {
            get { return _movingCloudFolderTargetPath; }
            set
            {
                _movingCloudFolderTargetPath = value;
                SettingsBase.Write<string>(kMovingCloudFolderTargetPath, value);
            }
        }

        private DateTime _cloudFolderCreationTimeUtc;
        public DateTime CloudFolderCreationTimeUtc
        {
            get { return _cloudFolderCreationTimeUtc; }
            set
            {
                _cloudFolderCreationTimeUtc = value;
                SettingsBase.Write<DateTime>(kCloudFolderCreationTimeUtc, value);
            }
        }

        // todo: property to track selective folders for sync in cloudFolderPath.

        // Others
        private long _eid;
        public long Eid
        {
            get { return _eid; }
            set
            {
                _eid = value;
                SettingsBase.Write<long>(kEid, value);
            }
        }

        private string _sid;
        public string Sid
        {
            get { return _sid; }
            set
            {
                _sid = value;
                SettingsBase.Write<string>(kSid, value);
            }
        }

        private Boolean _shouldAddShowCloudFolderOnDesktop;
        public Boolean ShouldAddShowCloudFolderOnDesktop
        {
            get { return _shouldAddShowCloudFolderOnDesktop; }
            set
            {
                if (value != _shouldAddShowCloudFolderOnDesktop)
                {
                    CLShortcuts.UpdateShouldShowCloudFolderOnDesktop(value, Settings.Instance.CloudFolderPath);
                }
                _shouldAddShowCloudFolderOnDesktop = value;
                SettingsBase.Write<Boolean>(kShouldAddShowCloudFolderOnDesktop, value);
            }
        }

        private Boolean _shouldAddShowCloudFolderInExplorerFavorites;
        public Boolean ShouldAddShowCloudFolderInExplorerFavorites
        {
            get { return _shouldAddShowCloudFolderInExplorerFavorites; }
            set
            {
                if (value != _shouldAddShowCloudFolderInExplorerFavorites)
                {
                    CLShortcuts.UpdateShouldShowCloudFolderInExplorerFavorites(value, Settings.Instance.CloudFolderPath);
                }
                _shouldAddShowCloudFolderInExplorerFavorites = value;
                SettingsBase.Write<Boolean>(kShouldAddShowCloudFolderInExplorerFavorites, value);
            }
        }

        private Boolean _shouldAddShowCloudFolderInInternetExplorerFavorites;
        public Boolean ShouldAddShowCloudFolderInInternetExplorerFavorites
        {
            get { return _shouldAddShowCloudFolderInInternetExplorerFavorites; }
            set
            {
                if (value != _shouldAddShowCloudFolderInInternetExplorerFavorites)
                {
                    CLShortcuts.UpdateShouldShowCloudFolderInInternetExplorerFavorites(value, Settings.Instance.CloudFolderPath);
                }
                _shouldAddShowCloudFolderInInternetExplorerFavorites = value;
                SettingsBase.Write<Boolean>(kShouldAddShowCloudFolderInInternetExplorerFavorites, value);
            }
        }

        private Boolean _shouldAddShowCloudFolderOnTaskbar;
        public Boolean ShouldAddShowCloudFolderOnTaskbar
        {
            get { return _shouldAddShowCloudFolderOnTaskbar; }
            set
            {
                if (value != _shouldAddShowCloudFolderOnTaskbar)
                {
                    CLShortcuts.UpdateShouldShowCloudFolderOnTaskbar(value, Settings.Instance.CloudFolderPath, shouldWaitForCompletion: true);
                }
                _shouldAddShowCloudFolderOnTaskbar = value;
                SettingsBase.Write<Boolean>(kShouldAddShowCloudFolderOnTaskbar, value);
            }
        }

        private Boolean _shouldAddShowCloudFolderInStartMenu;
        public Boolean ShouldAddShowCloudFolderInStartMenu
        {
            get { return _shouldAddShowCloudFolderInStartMenu; }
            set
            {
                if (value != _shouldAddShowCloudFolderInStartMenu)
                {
                    CLShortcuts.UpdateShouldShowCloudFolderInStartMenu(value, Settings.Instance.CloudFolderPath, shouldWaitForCompletion: true);
                }
                _shouldAddShowCloudFolderInStartMenu = value;
                SettingsBase.Write<Boolean>(kShouldAddShowCloudFolderInStartMenu, value);
            }
        }

        private Boolean _shouldAnimateToSystemTray;
        public Boolean ShouldAnimateToSystemTray
        {
            get { return _shouldAnimateToSystemTray; }
            set
            {
                _shouldAnimateToSystemTray = value;
                SettingsBase.Write<Boolean>(kShouldAnimateToSystemTray, value);
            }
        }

        private List<string> _recentFileItems;
        public List<string> RecentFileItems
        {
            get { return _recentFileItems; }
            set
            {
                _recentFileItems = value;
                SettingsBase.Write<List<string>>(kRecentFileItems, value);
            }
        }

        // Setting to determine whether errors are logged to disk
        // Added by David
        private int _logErrors;
        public bool LogErrors
        {
            get { return _logErrors != 0; }
            set
            {
                _logErrors = (value ? 1 : 0);
                SettingsBase.Write<int>(kLogErrors, _logErrors);
            }
        }

        // Setting for error log location
        // Added by David
        private string _errorLogLocation;
        public string ErrorLogLocation
        {
            get { return _errorLogLocation; }
            set
            {
                _errorLogLocation = value;
                SettingsBase.Write<string>(kLogErrorLocation, value);
            }
        }

        // Setting to determine whether to log trace to disk
        // Added by David
        private int _traceType;
        public TraceType TraceType
        {
            get { return (TraceType)_traceType; }
            set
            {
                if ((value & TraceType.AddAuthorization) == TraceType.AddAuthorization
                    && (value & TraceType.CommunicationIncludeAuthorization) != CloudApiPublic.Static.TraceType.CommunicationIncludeAuthorization)
                {
                    throw new ArgumentException("A TraceType of AddAuthorization can only be used in conjunction with Communication");
                }
                _traceType = (int)value;
                SettingsBase.Write<int>(kTraceType, _traceType);
            }
        }

        // Setting for trace log location
        // Added by David
        private string _traceLocation;
        public string TraceLocation
        {
            get { return _traceLocation; }
            set
            {
                _traceLocation = value;
                SettingsBase.Write<string>(kTraceLocation, value);
            }
        }

        // Setting to determine whether the Authorization header is excluded from Trace
        // Added by David
        private int _traceExcludeAuthorization;
        public bool TraceExcludeAuthorization
        {
            get { return (TraceType & TraceType.AddAuthorization) != TraceType.AddAuthorization; }
            set
            {
                if (value)
                {
                    TraceType &= ~TraceType.AddAuthorization;
                }
                else
                {
                    TraceType |= TraceType.AddAuthorization;
                }
            }
        }

        // Setting to keep track of history upload bandwidth in bits per second
        // Added by David
        private double _historicUploadBandwidthBitsPS;
        public double HistoricUploadBandwidthBitsPS
        {
            get { return _historicUploadBandwidthBitsPS; }
            set
            {
                if (_historicUploadBandwidthBitsPS != value)
                {
                    _historicUploadBandwidthBitsPS = value;
                    SettingsBase.Write<double>(kHistoryUploadBandwidthBitPS, _historicUploadBandwidthBitsPS);
                }
            }
        }

        // Setting to keep track of history download bandwidth in bits per second
        // Added by David
        private double _historicDownloadBandwidthBitsPS;
        public double HistoricDownloadBandwidthBitsPS
        {
            get { return _historicDownloadBandwidthBitsPS; }
            set
            {
                if (_historicDownloadBandwidthBitsPS != value)
                {
                    _historicDownloadBandwidthBitsPS = value;
                    SettingsBase.Write<double>(kHistoryDownloadBandwidthBitPS, _historicDownloadBandwidthBitsPS);
                }
            }
        }

        // Main window placement info.
        private string _mainWindowPlacement;
        public String MainWindowPlacement
        {
            get 
            {
                _trace.writeToLog(9, "CLSettings: MainWindowPlacement.get: Return value: <{0}>.", _mainWindowPlacement);
                return _mainWindowPlacement; 
            }
            set
            {
                _trace.writeToLog(9, "CLSettings: MainWindowPlacement.set: Seet value: <{0}>.", value);
                _mainWindowPlacement = value;
                SettingsBase.Write<string>(kMainWindowPlacement, value);
            }
        }

        /// <summary>
        /// Allocate ourselves. We have a private constructor, so no one else can.
        /// </summary>
        private static Settings _instance = null;
        private static object InstanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;


        /// <summary>
        /// Access SiteStructure.Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static Settings Instance
        {
    	    get 
            {
                lock (InstanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new Settings();
                        _instance.loadSettings();
                        _trace.writeToLog(9, "CLSettings: Instance: Settings loaded.");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private Settings()
        {
    	    // Initialize members, etc. here.
            _recentFileItems = new List<string>();
        }

        /// <summary>
        /// Load the settings
        /// </summary>
        public void loadSettings()
        {    
            // Load defaults

            // Historic bandwidth
            _historicUploadBandwidthBitsPS = 1048576d; // 1 megabits per second
            _historicDownloadBandwidthBitsPS = 262144d; // 250 kilobits per second

            // Logging
            _logErrors = 0;
            _errorLogLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                "\\Cloud\\ErrorLog";
            _traceType = 0;

            //TODO: Bugbug Remove this debug only setting before release.  This forces trace to be on (causing a performance issue).
            _logErrors = 1;
            _traceType = (int)TraceType.Communication;

            // _traceType of zero implies the following:
            //_traceExcludeAuthorization = 1;
            _traceLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
                "\\Cloud\\Trace";

            // General
            _startCloudAppWithSystem = true;
            _animateMenuBarForUpdates = true;
            _showDesktopNotificationForUpdates = true;
            _useColorIconForCloudFolder = false;
            _cloudAppLanguage = (int)cloudAppLanguageType.cloudAppLanguageEN;
            _dateWeLastCheckedForSoftwareUpdate = (DateTime)Helpers.DefaultForType(typeof(DateTime));

            // Setup
            _useDefaultSetup = true;
    
            // Network
            _useLANForFileSync = true;
            _limitDownloadSpeeds = false;
            _downloadSpeedLimit = 50;
            _limitUploadSpeeds = uploadSpeedLimitType.uploadSpeedLimitAutoLimit;
            _uploadSpeedLimit = 10;
            _useProxySetting = useProxySettingType.useProxySettingAutoDetect;
            _useProxyType = useProxyTypes.useProxyHTTP;
            _proxyServerAddress = "";
            _proxyServerPort = 8080;
            _proxyServerRequiresPassword = false;
            _proxyServerUsername = "";
            _proxyServerPassword = "";
    
            // Account
            _akey = ""; // only available when registered.
            _uuid = ""; // only available when registered.
            _udid = "";
            _userName = "";
            _userFullName = "";
            _deviceName = "";
            _quota = (int)StorageSizeSelections.Size5Gb;
            _completedSetup = false;
    
            // Advanced
            _cloudFolderPath = GetDefaultCloudFolderPath();
            _movingCloudFolderTargetPath = String.Empty;
            _cloudFolderCreationTimeUtc = (DateTime)Helpers.DefaultForType(typeof(DateTime));

            // Index Services
            _eid = Helpers.DefaultForType<long>();
    
            // Others
            _shouldAddShowCloudFolderOnDesktop = true;
            _shouldAddShowCloudFolderInExplorerFavorites = true;
            _shouldAddShowCloudFolderInInternetExplorerFavorites = true;
            _shouldAddShowCloudFolderOnTaskbar = true;
            _shouldAddShowCloudFolderInStartMenu = true;
            _shouldAnimateToSystemTray = true;

            _sid = "0";
            _recentFileItems.Clear();
            _mainWindowPlacement = "";
    
            // Override default options with user preferences

            // Logging
            // --Added by David
            int temp;
            long uTemp;
            bool bTemp;
            bool isPresent = SettingsBase.ReadIfPresent<int>(kLogErrors, out temp);
            if (isPresent)
            {
                _logErrors = temp;
            }
            
            string tempString;
            isPresent = SettingsBase.ReadIfPresent<string>(kLogErrorLocation, out tempString);
            if (isPresent)
            {
                _errorLogLocation = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kTraceType, out temp);
            if (isPresent)
            {
                _traceType = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kTraceLocation, out tempString);
            if (isPresent)
            {
                _traceLocation = tempString;
            }

            // Historic bandwidth
            // --Added by David
            double dTemp;
            isPresent = SettingsBase.ReadIfPresent<double>(kHistoryUploadBandwidthBitPS, out dTemp);
            if (isPresent)
            {
                _historicUploadBandwidthBitsPS = dTemp;
            }

            isPresent = SettingsBase.ReadIfPresent<double>(kHistoryDownloadBandwidthBitPS, out dTemp);
            if (isPresent)
            {
                _historicDownloadBandwidthBitsPS = dTemp;
            }

            // General
            isPresent = SettingsBase.ReadIfPresent<bool>(kStartCloudAppWithSystem, out bTemp);
            if (isPresent)
            {
                _startCloudAppWithSystem = bTemp;
            }

            isPresent = SettingsBase.ReadIfPresent<bool>(kAnimateMenuBarForUpdates, out bTemp);
            if (isPresent)
            {
                _animateMenuBarForUpdates = bTemp;
            }


            isPresent = SettingsBase.ReadIfPresent<bool>(kShowDesktopNotificationForUpdates, out bTemp);
            if (isPresent)
            {
                _showDesktopNotificationForUpdates = bTemp;
            }

            isPresent = SettingsBase.ReadIfPresent<bool>(kUseColorIconForCloudFolder, out bTemp);
            if (isPresent)
            {
                _useColorIconForCloudFolder = bTemp;
            }

            isPresent = SettingsBase.ReadIfPresent<bool>(kIsMovingCloudFolder, out bTemp);
            if (isPresent)
            {
                _isMovingCloudFolder = bTemp;
            }
            
            isPresent = SettingsBase.ReadIfPresent<int>(kCloudAppLanguage, out temp);
            if (isPresent)
            {
                _cloudAppLanguage = temp;
            }

            DateTime tempDate;
            isPresent = SettingsBase.ReadIfPresent<DateTime>(kDateWeLastCheckedForSoftwareUpdate, out tempDate);
            if (isPresent)
            {
                _dateWeLastCheckedForSoftwareUpdate = tempDate;
            } 

            // Setup
            Boolean tempBoolean;
            isPresent = SettingsBase.ReadIfPresent<Boolean>(kUseDefaultSetup, out tempBoolean);
            if (isPresent)
            {
                _useDefaultSetup = tempBoolean;
            }

 
            // Network
            isPresent = SettingsBase.ReadIfPresent<bool>(kUseLanForFileSync, out bTemp);
            if (isPresent)
            {
                _useLANForFileSync = bTemp;
            }

    
            // Bandwidth
            isPresent = SettingsBase.ReadIfPresent<bool>(kLimitDownloadSpeeds, out bTemp);
            if (isPresent)
            {
                _limitDownloadSpeeds = bTemp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kDownloadSpeedLimit, out temp);
            if (isPresent)
            {
                _downloadSpeedLimit = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kLimitUploadSpeeds, out temp);
            if (isPresent)
            {
                _limitUploadSpeeds = (uploadSpeedLimitType)temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kUploadSpeedLimit, out temp);
            if (isPresent)
            {
                _uploadSpeedLimit = temp;
            }
    
            // Proxy
            isPresent = SettingsBase.ReadIfPresent<int>(kUseProxySetting, out temp);
            if (isPresent)
            {
                _useProxySetting = (useProxySettingType)temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kUseProxyType, out temp);
            if (isPresent)
            {
                _useProxyType = (useProxyTypes)temp;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kProxyServerAddress, out  tempString);
            if (isPresent)
            {
                _proxyServerAddress = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kProxyServerPort, out temp);
            if (isPresent)
            {
                _proxyServerPort = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<bool>(kProxyServerRequiresPassword, out bTemp);
            if (isPresent)
            {
                _proxyServerRequiresPassword = bTemp;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kProxyServerUserName, out tempString);
            if (isPresent)
            {
                _proxyServerUsername = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kProxyServerPassword, out tempString);
            if (isPresent)
            {
                _proxyServerPassword = tempString;
            }
    
            // Account
            isPresent = SettingsBase.ReadIfPresent<string>(kAKey, out tempString);
            if (isPresent)
            {
                _akey = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kUuid, out tempString);
            if (isPresent)
            {
                _uuid = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kUserName, out tempString);
            if (isPresent)
            {
                _userName = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kUserFullName, out tempString);
            if(isPresent)
            {
                _userFullName = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kDeviceName, out tempString);
            if (isPresent)
            {
                _deviceName = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(kCompletedSetup, out tempBoolean);  // 'cs' stands for "completed_setup", but we don't want to make it obvious.
            if (isPresent)
            {
                _completedSetup = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kQuota, out temp);      // q is not the most clear value, but we don't want to make it obvious.
            if (isPresent)
            {
                _quota = temp;
            }

            // Advanced
            isPresent = SettingsBase.ReadIfPresent<string>(kCloudFolderPath, out tempString);
            if (isPresent)
            {
                _cloudFolderPath = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kMovingCloudFolderTargetPath, out tempString);
            if (isPresent)
            {
                _movingCloudFolderTargetPath = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<DateTime>(kCloudFolderCreationTimeUtc, out tempDate);
            if (isPresent)
            {
                _cloudFolderCreationTimeUtc = tempDate;
            }

            // Index Services
            isPresent = SettingsBase.ReadIfPresent<long>(kEid, out uTemp);
            if (isPresent)
            {
                _eid = uTemp;
            }
    
            // Others
            isPresent = SettingsBase.ReadIfPresent<Boolean>(kShouldAddShowCloudFolderInExplorerFavorites, out tempBoolean);
            if (isPresent)
            {
                _shouldAddShowCloudFolderInExplorerFavorites = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(kShouldAddShowCloudFolderInInternetExplorerFavorites, out tempBoolean);
            if (isPresent)
            {
                _shouldAddShowCloudFolderInInternetExplorerFavorites = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(kShouldAddShowCloudFolderOnTaskbar, out tempBoolean);
            if (isPresent)
            {
                _shouldAddShowCloudFolderOnTaskbar = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(kShouldAddShowCloudFolderInStartMenu, out tempBoolean);
            if (isPresent)
            {
                _shouldAddShowCloudFolderInStartMenu = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(kShouldAnimateToSystemTray, out tempBoolean);
            if (isPresent)
            {
                _shouldAnimateToSystemTray = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kSid, out tempString);
            if (isPresent)
            {
                _sid = tempString;
            }

            List<string> tempList;
            isPresent = SettingsBase.ReadIfPresent<List<string>>(kRecentFileItems, out tempList);
            if (isPresent)
            {
                _recentFileItems = tempList;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kUdid, out tempString);
            if (isPresent)
            {
                _udid = tempString;
                _trace.writeToLog(9, "CLSettings: Instance: Initialize Udid: {0} from Settings file.", _udid);
            }

            isPresent = SettingsBase.ReadIfPresent<string>(kMainWindowPlacement, out tempString);
            if (isPresent)
            {
                _mainWindowPlacement = tempString;
            }
        }
        /// <summary>
        /// Record timestamp
        /// </summary>
        public void RecordEventId(long eventId)
        {  
            Eid = eventId;
        }

        /// <summary>
        /// Record SID
        /// </summary>
        public void recordSID(string sidParm)
        {  
            Sid = sidParm;
        }

        /// <summary>
        /// Record UDID
        /// </summary>
        //public void recordUDID(string udidParm)
        //{
        //    Udid = udidParm;
        //}

        /// <summary>
        /// Record account settings
        /// </summary>
        public void saveAccountSettings(Dictionary<string, object> accountInfo)
        {
            _trace.writeToLog(9, "CLSettings: Instance: saveAccountSettings. Udid: {0}.", accountInfo[kUdid] ?? "None");
            UserName = (string)accountInfo[kUserName];
            //UserFullName = (string)accountInfo[kUserFullName];
            DeviceName = (string)accountInfo[kDeviceName];
            Akey = (string)accountInfo[kAKey];
            Uuid = (string)accountInfo[kUuid];
            Udid = (string)accountInfo[kUdid];
        }

        /// <summary>
        /// Record quota
        /// </summary>
        public void setCloudQuota(int quotaParm)
        {  
            Quota = quotaParm;
        }

        /// <summary>
        /// Record setup completed
        /// </summary
        public void setCloudAppSetupCompleted(Boolean completedSetupParm)
        {  
            CompletedSetup = completedSetupParm;
        }

        /// <summary>
        /// Reset all of our settings.
        /// </summary>
        public void resetSettings()
        {
            _trace.writeToLog(9, "CLSettings: Instance: Reset settings.");
            // Clear the settings.
            //TODO: Check this.  Removed clearing the entire settings base so the user's settings would be remembered.
            //SettingsBase.Clear();
            Settings.Instance.CompletedSetup = false;       // tested at ExitApplication()
            Settings.Instance.Akey = String.Empty;          // lose the key
            Settings.Instance.Udid = String.Empty;          // lose the device ID
            Settings.Instance.ShouldAnimateToSystemTray = true;  // Animate to the system tray after setup just the first time
        }

        /// <summary>
        /// Record the new Cloud folder path.
        /// </summary>
        public void updateCloudFolderPath(string path, DateTime creationTime)
        {
            CloudFolderPath = path;
            CloudFolderCreationTimeUtc = creationTime;

            CLShortcuts.UpdateAllShortcuts(path);
        }

        /// <summary>
        /// Record the recently accessed item list.
        /// </summary>
        public void recordRecentItems(List<string> items)
        {  
            List<string> tempRecents = new List<string>();
            tempRecents.AddRange(_recentFileItems);
            tempRecents.AddRange(items);

            // Remove duplicates and removed files
            List<String> copy = CLExtensionMethods.DeepCopy(tempRecents);
            for (int i = copy.Count - 1; i >= 0; i--) 
            { 
                string fullPath = _cloudFolderPath + copy[i];
                if (!File.Exists(fullPath))
                {
                    tempRecents.RemoveAt(i);
                }
                else
                {
                    if (tempRecents.GetRange(0, i).Contains(tempRecents[i]))
                    {
                        tempRecents.RemoveAt(i);
                    }                
                }
            }

            // Now keep only the last 8 of those.
            List<string> recents = CLExtensionMethods.DeepCopy(tempRecents);
            if (recents.Count > 8)
            {
                for (int i = 0; i < tempRecents.Count; i++)
                {
                    if (recents.Count > 8) 
                    {
                        recents.RemoveAt(i);
                    }
                }        
            }

            // Now persist the result
            _recentFileItems = CLExtensionMethods.DeepCopy(recents);
            SettingsBase.Write<List<string>>("recent_items", _recentFileItems);
        }

        /// <summary>
        /// Get the default Cloud folder path.
        /// </summary>
        public string GetDefaultCloudFolderPath()
        {
            string folder = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));  // get the user's home directory.  e.g., C:\Users\<UserName>\
            folder = folder + "\\" + CLPrivateDefinitions.CloudDirectoryName;
            return folder;
        }

        ///// <summary>
        ///// Move the Cloud folder to a new location.
        ///// <param name="existingPath">The full path of the existing Cloud location.</param>
        ///// <param name="newPath">The full path of the new Cloud folder location</param>
        ///// <param name="error">A possible output error.</param>
        ///// </summary>
        //public void MoveCloudDirectoryFromPath_toDestination(string existingPath, string newPath, out CLError error)
        //{
        //    error = null;
        //    try
        //    {
        //        Directory.Move(existingPath, newPath);
        //    }
        //    catch (Exception ex)
        //    {
        //        error += ex;
        //    }
        //}
    }

    #endregion

}