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
using win_client.Common;

namespace win_client.DataModels.Settings
{
    #region "Enums"

    enum cloudAppLanguageType
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

    enum uploadSpeedLimitType
    {
        uploadSpeedLimitDontLimit = 0,
        uploadSpeedLimitAutoLimit = 1,
        uploadSpeedLimitLimitTo = 2,
    };

    enum useProxySettingType
    {
        useProxySettingNoProxy = 0,
        useProxySettingAutoDetect = 1,
        useProxySettingNoManual = 2,
    };

    enum useProxyTypes
    {
        useProxyHTTP = 0,
        useProxySOCK4 = 1,
        useProxySOCK5 = 2,
    };

    enum buttonState
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

#if _SILVERLIGHT
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
#else
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
#endif
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
#if _SILVERLIGHT
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
#else
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
#endif
            TT value;
            if(settings == null || !settings.TryGetValue<TT>(name, out value)) 
            {
                return defaultValue;            
            }
            return value;        
        }    
     
        public static void Write<TT>(string name, TT value)        
        {
#if _SILVERLIGHT
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
#else
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
#endif
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
#if _SILVERLIGHT
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
#else
            IsolatedStorageSettings settings = IsolatedStorageSettings.Instance;
#endif
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
        private const string kStartCloudAppWithSystem = @"start_cloud_app_with_system";
        private const string kAnimateMenuBarForUpdates = @"animate_menu_bar_for_updates";
        private const string kShowDesktopNotificationForUpdates = @"show_desktop_notification_for_updates";
        private const string kCloudAppLanguage = @"cloud_app_language";
        private const string kDateWeLastCheckedForSoftwareUpdate = @"date_we_last_checked_for_software_update";
        private const string kUseDefaultSetup = @"use_default_setup";
        private const string kUseLanForFileSync = @"use_lan_for_file_sync";
        private const string kLimitDownloadSpeeds = @"limit_download_speeds";
        private const string kDownloadSpeedLimit = @"download_speed_limit";
        private const string kLimitUploadSpeeds = @"limit_upload_speeds";
        private const string kUploadSpeedLimit = @"upload_speed_limit";
        private const string kUseProxySetting = @"use_proxy_settings";
        private const string kUseProxyType = @"use_proxy_type";
        private const string kProxyServerAddress = @"proxy_server_address";
        private const string kProxyServerPort = @"proxy_server_port";
        private const string kProxyServerRequiresPassword = @"proxy_server_requires_password";
        private const string kProxyServerUserName = @"proxy_server_user_name";
        private const string kProxyServerPassword = @"proxy_server_password";
        private const string kUserName = @"user_name";
        private const string kUserFullName = @"user_full_name";
        private const string kDeviceName = @"device_name";
        private const string kUuid = @"uuid";
        private const string kAKey = @"akey";
        private const string kQuota = @"q";
        private const string kUdidRegistered = @"r_udid";
        private const string kCompletedSetup = @"cs";
        private const string kCloudFolderPath = @"cloud_folder_path";
        private const string kCloudFolderDescriptor = @"desktop_shortcut";
        private const string kEid = @"eid";
        private const string kSid = @"sid";
        private const string kAddCloudFolderToDock = @"add_dock_folder";
        private const string kAddCloudFolderToDesktop = @"add_cloud_folder_to_desktop";
        private const string kRecentFileItems = @"recent_file_items";


        /// <summary>
        /// The persistent settings properties.
        /// </summary>
        
        // General
        private int _startCloudAppWithSystem;
        public int StartCloudAppWithSystem {
        get {return _startCloudAppWithSystem; } 
        set
            {
                _startCloudAppWithSystem = value;
                SettingsBase.Write<int>(kStartCloudAppWithSystem, value);
            }
        }

        private int _animateMenuBarForUpdates;
        public int AnimateMenuBarForUpdates
        {
            get { return _animateMenuBarForUpdates; }
            set
            {
                _animateMenuBarForUpdates = value;
                SettingsBase.Write<int>(kAnimateMenuBarForUpdates, value);
            }
        }

        private int _showDesktopNotificationForUpdates;
        public int ShowDesktopNotificationForUpdates
        {
            get { return _showDesktopNotificationForUpdates; }
            set
            {
                _showDesktopNotificationForUpdates = value;
                SettingsBase.Write<int>(kCloudAppLanguage, value);
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
        private int _useLANForFileSync;
        public int UseLANForFileSync
        {
            get { return _useLANForFileSync; }
            set
            {
                _useLANForFileSync = value;
                SettingsBase.Write<int>(kUseLanForFileSync, value);
            }
        }

        private int _limitDownloadSpeeds;
        public int LimitDownloadSpeeds
        {
            get { return _limitDownloadSpeeds; }
            set
            {
                _limitDownloadSpeeds = value;
                SettingsBase.Write<int>(kLimitDownloadSpeeds, value);
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

        private int _limitUploadSpeeds;
        public int LimitUploadSpeeds
        {
            get { return _limitUploadSpeeds; }
            set
            {
                _limitUploadSpeeds = value;
                SettingsBase.Write<int>(kLimitUploadSpeeds, value);
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

        private int _useProxySetting;
        public int UseProxySetting
        {
            get { return _useProxySetting; }
            set
            {
                _useProxySetting = value;
                SettingsBase.Write<int>(kUseProxySetting, value);
            }
        }

        private int _useProxyType;
        public int UseProxyType
        {
            get { return _useProxyType; }
            set
            {
                _useProxyType = value;
                SettingsBase.Write<int>(kUseProxyType, value);
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

        private int _proxyServerRequiresPassword;
        public int ProxyServerRequiresPassword
        {
            get { return _proxyServerRequiresPassword; }
            set
            {
                _proxyServerRequiresPassword = value;
                SettingsBase.Write<int>(kProxyServerRequiresPassword, value);
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

        private Boolean _udidRegistered;
        public Boolean UdidRegistered
        {
            get { return _udidRegistered; }
            set
            {
                _udidRegistered = value;
                SettingsBase.Write<Boolean>(kUdidRegistered, value);
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

        private FileStream _cloudFolderDescriptor;
        public FileStream CloudFolderDescriptor
        {
            get { return _cloudFolderDescriptor; }
            set
            {
                _cloudFolderDescriptor = value;
                SettingsBase.Write<FileStream>(kCloudFolderDescriptor, value);
            }
        }

        // todo: property to track selective folders for sync in cloudFolderPath.

        // Others
        private int _eid;
        public int Eid
        {
            get { return _eid; }
            set
            {
                _eid = value;
                SettingsBase.Write<int>(kEid, value);
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

        private Boolean _addCloudFolderToDock;
        public Boolean AddCloudFolderToDock
        {
            get { return _addCloudFolderToDock; }
            set
            {
                _addCloudFolderToDock = value;
                SettingsBase.Write<Boolean>(kAddCloudFolderToDock, value);
            }
        }

        private Boolean _addCloudFolderToDesktop;
        public Boolean AddCloudFolderToDesktop
        {
            get { return _addCloudFolderToDesktop; }
            set
            {
                _addCloudFolderToDesktop = value;
                SettingsBase.Write<Boolean>(kAddCloudFolderToDesktop, value);
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

        /// <summary>
        /// Allocate ourselves. We have a private constructor, so no one else can.
        /// </summary>
        static readonly Settings _instance = new Settings();
        private static Boolean _isLoaded = false;

        /// <summary>
        /// Access SiteStructure.Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static Settings Instance
        {
    	    get 
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;
                    _instance.loadSettings();
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

            // General
            _startCloudAppWithSystem = (int)buttonState.stateON;
            _animateMenuBarForUpdates = (int)buttonState.stateON;
            _showDesktopNotificationForUpdates = (int)buttonState.stateON;
            _cloudAppLanguage = (int)cloudAppLanguageType.cloudAppLanguageEN;
            _dateWeLastCheckedForSoftwareUpdate = DateTime.MinValue;

            // Setup
            _useDefaultSetup = true;
    
            // Network
            _useLANForFileSync = (int)buttonState.stateON;
            _limitDownloadSpeeds = (int)buttonState.stateOFF;
            _downloadSpeedLimit = 50;
            _limitUploadSpeeds = (int)uploadSpeedLimitType.uploadSpeedLimitAutoLimit;
            _uploadSpeedLimit = 10;
            _useProxySetting = (int)useProxySettingType.useProxySettingAutoDetect;
            _useProxyType = (int)useProxyTypes.useProxyHTTP;
            _proxyServerAddress = @"";
            _proxyServerPort = 8080;
            _proxyServerRequiresPassword = (int)buttonState.stateOFF;
            _proxyServerUsername = @"";
            _proxyServerPassword = @"";
    
            // Account
            _akey = @""; // only available when registered.
            _uuid = @""; // only available when registered.
            _userName = @"";
            _userFullName = @"";
            _deviceName = @"";
            _quota = 0;
            _completedSetup = false;
            _udidRegistered = false;
    
            // Advanced
            //cloudFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"/Cloud";
            _cloudFolderPath = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));  // get the user's home directory.  e.g., C:\Users\<UserName>\
            _cloudFolderPath = _cloudFolderPath + @"\Cloud";

            _cloudFolderDescriptor = null;
    
            // Index Services
            _eid = -1;
    
            // Others
            _addCloudFolderToDock = true;
            _addCloudFolderToDesktop = false;
            _sid = @"0";
            _recentFileItems.Clear(); 
    
            // Override default options with user preferences

            // General
            int temp;
            Boolean isPresent = SettingsBase.ReadIfPresent<int>(kStartCloudAppWithSystem, out temp);
            if (isPresent)
            {
                _startCloudAppWithSystem = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kAnimateMenuBarForUpdates, out temp);
            if (isPresent)
            {
                _animateMenuBarForUpdates = temp;
            }


            isPresent = SettingsBase.ReadIfPresent<int>(kShowDesktopNotificationForUpdates, out temp);
            if (isPresent)
            {
                _showDesktopNotificationForUpdates = temp;
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
            isPresent = SettingsBase.ReadIfPresent<int>(kUseLanForFileSync, out temp);
            if (isPresent)
            {
                _useLANForFileSync = temp;
            }

    
            // Bandwidth
            isPresent = SettingsBase.ReadIfPresent<int>(kLimitDownloadSpeeds, out temp);
            if (isPresent)
            {
                _limitDownloadSpeeds = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kDownloadSpeedLimit, out temp);
            if (isPresent)
            {
                _downloadSpeedLimit = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kLimitUploadSpeeds, out temp);
            if (isPresent)
            {
                _limitUploadSpeeds = temp;
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
                _useProxySetting = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(kUseProxyType, out temp);
            if (isPresent)
            {
                _useProxyType = temp;
            }

            string tempString;
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

            isPresent = SettingsBase.ReadIfPresent<int>(kProxyServerRequiresPassword, out temp);
            if (isPresent)
            {
                _proxyServerRequiresPassword = temp;
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

            isPresent = SettingsBase.ReadIfPresent<Boolean>(kUdidRegistered, out tempBoolean);
            if (isPresent)
            {
                _udidRegistered = tempBoolean;
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

            FileStream tempStream;
            isPresent = SettingsBase.ReadIfPresent<FileStream>(kCloudFolderDescriptor, out tempStream);
            if (isPresent)
            {
                _cloudFolderDescriptor = tempStream;
            }
    
            // Index Services
            isPresent = SettingsBase.ReadIfPresent<int>(kEid, out temp);
            if (isPresent)
            {
                _eid = temp;
            }
    
            // Others
            isPresent = SettingsBase.ReadIfPresent<Boolean>(kAddCloudFolderToDock, out tempBoolean);
            if (isPresent)
            {
                _addCloudFolderToDock = tempBoolean;
            }
            isPresent = SettingsBase.ReadIfPresent<Boolean>(kCloudFolderDescriptor, out tempBoolean);
            if (isPresent)
            {
                _addCloudFolderToDesktop = tempBoolean;
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
        }
        /// <summary>
        /// Record timestamp
        /// </summary>
        public void recordEventId(int eventId)
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
        /// Record account settings
        /// </summary>
        public void saveAccountSettings(Dictionary<string, object> accountInfo)
        {  
            UserName = (string)accountInfo[kUserName];
            UserFullName = (string)accountInfo[kUserFullName];
            DeviceName = (string)accountInfo[kDeviceName];
            UdidRegistered = (Boolean)accountInfo[kUdidRegistered];
            Akey = (string)accountInfo[kAKey];
            Uuid = (string)accountInfo[kUuid];
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
        /// </summary>
        public void setCloudAppSetupCompleted(Boolean completedSetupParm)
        {  
            CompletedSetup = completedSetupParm;
        }

        /// <summary>
        /// Reset all of our settings.
        /// </summary>
        public void resetSettings()
        {  
            SettingsBase.Clear();
        }

        /// <summary>
        /// Record the new Cloud folder path.
        /// </summary>
        public void updateCloudFolderPath(string path)
        {  
            CloudFolderPath = path; 
            CloudFolderDescriptor = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
            SettingsBase.Write<List<string>>(@"recent_items", _recentFileItems);
        }
    }

    #endregion

} 

