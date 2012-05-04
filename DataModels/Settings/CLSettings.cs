//
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

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
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
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
            TT value;            
            if (settings == null || !settings.TryGetValue<TT>(name, out value)) 
            {
                return defaultValue;            
            }
            return value;        
        }    
     
        public static void Write<TT>(string name, TT value)        
        {            
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;            
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
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;  
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
                SettingsBase.Write<int>(@"start_cloud_app_with_system", value);
            }
        }

        private int _animateMenuBarForUpdates;
        public int AnimateMenuBarForUpdates
        {
            get { return _animateMenuBarForUpdates; }
            set
            {
                _animateMenuBarForUpdates = value;
                SettingsBase.Write<int>(@"animate_menu_bar_for_updates", value);
            }
        }

        private int _showDesktopNotificationForUpdates;
        public int ShowDesktopNotificationForUpdates
        {
            get { return _showDesktopNotificationForUpdates; }
            set
            {
                _showDesktopNotificationForUpdates = value;
                SettingsBase.Write<int>(@"show_desktop_notification_for_updates", value);
            }
        }

        private int _cloudAppLanguage;
        public int CloudAppLanguage
        {
            get { return _cloudAppLanguage; }
            set
            {
                _cloudAppLanguage = value;
                SettingsBase.Write<int>(@"cloud_app_language", value);
            }
        }

        private DateTime _dateWeLastCheckedForSoftwareUpdate;
        public DateTime DateWeLastCheckedForSoftwareUpdate
        {
            get { return _dateWeLastCheckedForSoftwareUpdate; }
            set
            {
                _dateWeLastCheckedForSoftwareUpdate = value;
                SettingsBase.Write<DateTime>(@"date_we_last_checked_for_software_update", value);
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
                SettingsBase.Write<Boolean>(@"use_default_setup", value);
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
                SettingsBase.Write<int>(@"use_lan_for_file_sync", value);
            }
        }

        private int _limitDownloadSpeeds;
        public int LimitDownloadSpeeds
        {
            get { return _limitDownloadSpeeds; }
            set
            {
                _limitDownloadSpeeds = value;
                SettingsBase.Write<int>(@"limit_download_speeds", value);
            }
        }

        private int _downloadSpeedLimit;
        public int DownloadSpeedLimit
        {
            get { return _downloadSpeedLimit; }
            set
            {
                _downloadSpeedLimit = value;
                SettingsBase.Write<int>(@"download_speed_limit", value);
            }
        }

        private int _limitUploadSpeeds;
        public int LimitUploadSpeeds
        {
            get { return _limitUploadSpeeds; }
            set
            {
                _limitUploadSpeeds = value;
                SettingsBase.Write<int>(@"limit_upload_speeds", value);
            }
        }

        private int _uploadSpeedLimit;
        public int UploadSpeedLimit
        {
            get { return _uploadSpeedLimit; }
            set
            {
                _uploadSpeedLimit = value;
                SettingsBase.Write<int>(@"upload_speed_limit", value);
            }
        }

        private int _useProxySetting;
        public int UseProxySetting
        {
            get { return _useProxySetting; }
            set
            {
                _useProxySetting = value;
                SettingsBase.Write<int>(@"use_proxy_setting", value);
            }
        }

        private int _useProxyType;
        public int UseProxyType
        {
            get { return _useProxyType; }
            set
            {
                _useProxyType = value;
                SettingsBase.Write<int>(@"use_proxy_type", value);
            }
        }

        private string _proxyServerAddress;
        public string ProxyServerAddress
        {
            get { return _proxyServerAddress; }
            set
            {
                _proxyServerAddress = value;
                SettingsBase.Write<string>(@"proxy_server_address", value);
            }
        }

        private int _proxyServerPort;
        public int ProxyServerPort
        {
            get { return _proxyServerPort; }
            set
            {
                _proxyServerPort = value;
                SettingsBase.Write<int>(@"proxy_server_port", value);
            }
        }

        private int _proxyServerRequiresPassword;
        public int ProxyServerRequiresPassword
        {
            get { return _proxyServerRequiresPassword; }
            set
            {
                _proxyServerRequiresPassword = value;
                SettingsBase.Write<int>(@"proxy_server_requires_password", value);
            }
        }

        private string _proxyServerUsername;
        public string ProxyServerUsername
        {
            get { return _proxyServerUsername; }
            set
            {
                _proxyServerUsername = value;
                SettingsBase.Write<string>(@"proxy_server_user_name", value);
            }
        }

        private string _proxyServerPassword;
        public string ProxyServerPassword
        {
            get { return _proxyServerPassword; }
            set
            {
                _proxyServerPassword = value;
                SettingsBase.Write<string>(@"proxy_server_password", value);
            }
        }

        // Account
        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                SettingsBase.Write<string>(@"user_name", value);
            }
        }

        private string _deviceName;
        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                _deviceName = value;
                SettingsBase.Write<string>(@"device_name", value);
            }
        }

        private string _uuid;
        public string Uuid
        {
            get { return _uuid; }
            set
            {
                _uuid = value;
                SettingsBase.Write<string>(@"uuid", value);
            }
        }

        private string _akey;
        public string Akey
        {
            get { return _akey; }
            set
            {
                _akey = value;
                SettingsBase.Write<string>(@"akey", value);
            }
        }

        private int _quota;
        public int Quota
        {
            get { return _quota; }
            set
            {
                _quota = value;
                SettingsBase.Write<int>(@"quota", value);
            }
        }

        private Boolean _udidRegistered;
        public Boolean UdidRegistered
        {
            get { return _udidRegistered; }
            set
            {
                _udidRegistered = value;
                SettingsBase.Write<Boolean>(@"udid_registered", value);
            }
        }

        private Boolean _completedSetup;
        public Boolean CompletedSetup
        {
            get { return _completedSetup; }
            set
            {
                _completedSetup = value;
                SettingsBase.Write<Boolean>(@"cs", value);
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
                SettingsBase.Write<string>(@"cloud_folder_path", value);
            }
        }

        private FileStream _cloudFolderDescriptor;
        public FileStream CloudFolderDescriptor
        {
            get { return _cloudFolderDescriptor; }
            set
            {
                _cloudFolderDescriptor = value;
                SettingsBase.Write<FileStream>(@"cloud_folder_descriptor", value);
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
                SettingsBase.Write<int>(@"eid", value);
            }
        }

        private string _sid;
        public string Sid
        {
            get { return _sid; }
            set
            {
                _sid = value;
                SettingsBase.Write<string>(@"sid", value);
            }
        }

        private Boolean _addCloudFolderToDock;
        public Boolean AddCloudFolderToDock
        {
            get { return _addCloudFolderToDock; }
            set
            {
                _addCloudFolderToDock = value;
                SettingsBase.Write<Boolean>(@"add_cloud_folder_to_dock", value);
            }
        }

        private Boolean _addCloudFolderToDesktop;
        public Boolean AddCloudFolderToDesktop
        {
            get { return _addCloudFolderToDesktop; }
            set
            {
                _addCloudFolderToDesktop = value;
                SettingsBase.Write<Boolean>(@"add_cloud_folder_to_desktop", value);
            }
        }

        private List<string> _recentFileItems;
        public List<string> RecentFileItems
        {
            get { return _recentFileItems; }
            set
            {
                _recentFileItems = value;
                SettingsBase.Write<List<string>>(@"recent_file_items", value);
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
            _deviceName = @"";
            _quota = 0;
            _completedSetup = false;
            _udidRegistered = false;
    
            // Advanced
            //cloudFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"/Cloud";
            _cloudFolderPath = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));  // get the user's home directory.  e.g., C:\Users\<UserName>\
            _cloudFolderPath = _cloudFolderPath + @"Cloud";

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
            Boolean isPresent = SettingsBase.ReadIfPresent<int>(@"start_cloud_app_with_system", out temp);
            if (isPresent)
            {
                _startCloudAppWithSystem = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"animate_menu_bar_for_updates", out temp);
            if (isPresent)
            {
                _animateMenuBarForUpdates = temp;
            }


            isPresent = SettingsBase.ReadIfPresent<int>(@"show_desktop_notification_for_updates", out temp);
            if (isPresent)
            {
                _showDesktopNotificationForUpdates = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"cloud_app_language", out temp);
            if (isPresent)
            {
                _cloudAppLanguage = temp;
            }

            DateTime tempDate;
            isPresent = SettingsBase.ReadIfPresent<DateTime>(@"date_we_last_checked_for_updates", out tempDate);
            if (isPresent)
            {
                _dateWeLastCheckedForSoftwareUpdate = tempDate;
            } 

            // Setup
            Boolean tempBoolean;
            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"use_default_setup", out tempBoolean);
            if (isPresent)
            {
                _useDefaultSetup = tempBoolean;
            }

 
            // Network
            isPresent = SettingsBase.ReadIfPresent<int>(@"use_lan_for_file_sync", out temp);
            if (isPresent)
            {
                _useLANForFileSync = temp;
            }

    
            // Bandwidth
            isPresent = SettingsBase.ReadIfPresent<int>(@"limit_download_speeds", out temp);
            if (isPresent)
            {
                _limitDownloadSpeeds = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"download_speed_limit", out temp);
            if (isPresent)
            {
                _downloadSpeedLimit = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"limit_upload_speeds", out temp);
            if (isPresent)
            {
                _limitUploadSpeeds = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"upload_speed_limit", out temp);
            if (isPresent)
            {
                _uploadSpeedLimit = temp;
            }
    
            // Proxy
            isPresent = SettingsBase.ReadIfPresent<int>(@"use_proxy_settings", out temp);
            if (isPresent)
            {
                _useProxySetting = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"use_proxy_type", out temp);
            if (isPresent)
            {
                _useProxyType = temp;
            }

            string tempString;
            isPresent = SettingsBase.ReadIfPresent<string>(@"proxy_server_address", out  tempString);
            if (isPresent)
            {
                _proxyServerAddress = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"proxy_server_port", out temp);
            if (isPresent)
            {
                _proxyServerPort = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"proxy_server_requires_password", out temp);
            if (isPresent)
            {
                _proxyServerRequiresPassword = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"proxy_server_username", out tempString);
            if (isPresent)
            {
                _proxyServerUsername = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"proxy_server_password", out tempString);
            if (isPresent)
            {
                _proxyServerPassword = tempString;
            }
    
            // Account
            isPresent = SettingsBase.ReadIfPresent<string>(@"akey", out tempString);
            if (isPresent)
            {
                _akey = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"uuid", out tempString);
            if (isPresent)
            {
                _uuid = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"user_name", out tempString);
            if (isPresent)
            {
                _userName = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"device_name", out tempString);
            if (isPresent)
            {
                _deviceName = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"cs", out tempBoolean);  // 'cs' stands for "completed_setup", but we don't want to make it obvious.
            if (isPresent)
            {
                _completedSetup = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"r_udid", out tempBoolean);
            if (isPresent)
            {
                _udidRegistered = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"q", out temp);      // q is not the most clear value, but we don't want to make it obvious.
            if (isPresent)
            {
                _quota = temp;
            }

            // Advanced
            isPresent = SettingsBase.ReadIfPresent<string>(@"cloud_folder_path", out tempString);
            if (isPresent)
            {
                _cloudFolderPath = tempString;
            }

            FileStream tempStream;
            isPresent = SettingsBase.ReadIfPresent<FileStream>(@"cloud_folder_descriptor", out tempStream);
            if (isPresent)
            {
                _cloudFolderDescriptor = tempStream;
            }
    
            // Index Services
            isPresent = SettingsBase.ReadIfPresent<int>(@"eid", out temp);
            if (isPresent)
            {
                _eid = temp;
            }
    
            // Others
            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"add_dock_folder", out tempBoolean);
            if (isPresent)
            {
                _addCloudFolderToDock = tempBoolean;
            }
            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"desktop_shortcut", out tempBoolean);
            if (isPresent)
            {
                _addCloudFolderToDesktop = tempBoolean;
            }
            isPresent = SettingsBase.ReadIfPresent<string>(@"sid", out tempString);
            if (isPresent)
            {
                _sid = tempString;
            }

            List<string> tempList;
            isPresent = SettingsBase.ReadIfPresent<List<string>>(@"recent_items", out tempList);
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
            UserName = (string)accountInfo[@"user_name"];
            DeviceName = (string)accountInfo[@"device_name"];
            UdidRegistered = (Boolean)accountInfo[@"r_udid"];
            Akey = (string)accountInfo[@"akey"];
            Uuid = (string)accountInfo[@"uuid"];
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
            List<String> copy = ExtensionMethods.DeepCopy(tempRecents);
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
            List<string> recents = ExtensionMethods.DeepCopy(tempRecents);
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
            _recentFileItems = ExtensionMethods.DeepCopy(recents);
            SettingsBase.Write<List<string>>(@"recent_items", _recentFileItems);
        }
    }

    #endregion

} 

