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
        public int startCloudAppWithSystem {get; set;}
        public int animateMenuBarForUpdates {get; set;}
        public int showDesktopNotificationForUpdates {get; set;}
        public int cloudAppLanguage {get; set;}
        public DateTime dateWeLastCheckedForSoftwareUpdate {get; set;}

        // Bandwidth
        public int useLANForFileSync {get; set;}
        public int limitDownloadSpeeds {get; set;}
        public int downloadSpeedLimit {get; set;}
        public int limitUploadSpeeds {get; set;}
        public int uploadSpeedLimit {get; set;}
        public int useProxySetting {get; set;}
        public int useProxyType {get; set;}
        public string proxyServerAddress {get; set;}
        public int proxyServerPort {get; set;}
        public int proxyServerRequiresPassword {get; set;}
        public string proxyServerUsername {get; set;}
        public string proxyServerPassword {get; set;}

        // Account
        public string userName {get; set;}
        public string deviceName {get; set;}
        public string uuid {get; set;}
        public string akey {get; set;}
        public int quota {get; set;}
        public Boolean udidRegistered {get; set;}
        public Boolean completedSetup {get; set;}

        // Advanced
        public string cloudFolderPath {get; set;}
        public FileStream cloudFolderDescriptor {get; set;}
        // todo: property to track selective folders for sync in cloudFolderPath.

        // Others
        public int eid {get; set;}
        public string sid {get; set;}
        public bool addCloudFolderToDock {get; set;}
        public bool addCloudFolderToDesktop {get; set;}
        public List<string> recentFileItems {get; set;}

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
            recentFileItems = new List<string>();
        }

        /// <summary>
        /// Load the settings
        /// </summary>
        public void loadSettings()
        {    
            // Load defaults

            // General
            startCloudAppWithSystem = (int)buttonState.stateON;
            animateMenuBarForUpdates = (int)buttonState.stateON;
            showDesktopNotificationForUpdates = (int)buttonState.stateON;
            cloudAppLanguage = (int)cloudAppLanguageType.cloudAppLanguageEN;
            dateWeLastCheckedForSoftwareUpdate = DateTime.MinValue;
    
            // Network
            useLANForFileSync = (int)buttonState.stateON;
            limitDownloadSpeeds = (int)buttonState.stateOFF;
            downloadSpeedLimit = 50;
            limitUploadSpeeds = (int)uploadSpeedLimitType.uploadSpeedLimitAutoLimit;
            uploadSpeedLimit = 10;
            useProxySetting = (int)useProxySettingType.useProxySettingAutoDetect;
            useProxyType = (int)useProxyTypes.useProxyHTTP;
            proxyServerAddress = @"";
            proxyServerPort = 8080;
            proxyServerRequiresPassword = (int)buttonState.stateOFF;
            proxyServerUsername = @"";
            proxyServerPassword = @"";
    
            // Account
            akey = @""; // only available when registered.
            uuid = @""; // only available when registered.
            userName = @"";
            deviceName = @"";
            quota = 0;
            completedSetup = false;
            udidRegistered = false;
    
            // Advanced
            //cloudFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"/Cloud";
            cloudFolderPath = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));  // get the user's home directory.  e.g., C:\Users\<UserName>\
            cloudFolderPath = cloudFolderPath + @"Cloud";

            cloudFolderDescriptor = null;
    
            // Index Services
            eid = -1;
    
            // Others
            addCloudFolderToDock = true;
            addCloudFolderToDesktop = false;
            sid = @"0";
            recentFileItems.Clear(); 
    
            // Override default options with user preferences

            // General
            int temp;
            Boolean isPresent = SettingsBase.ReadIfPresent<int>(@"start_cloud_app_with_system", out temp);
            if (isPresent)
            {
                startCloudAppWithSystem = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"animate_menu_bar_for_updates", out temp);
            if (isPresent)
            {
                animateMenuBarForUpdates = temp;
            }


            isPresent = SettingsBase.ReadIfPresent<int>(@"show_desktop_notification_for_updates", out temp);
            if (isPresent)
            {
                showDesktopNotificationForUpdates = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"cloud_app_language", out temp);
            if (isPresent)
            {
                cloudAppLanguage = temp;
            }

            DateTime tempDate;
            isPresent = SettingsBase.ReadIfPresent<DateTime>(@"date_we_last_checked_for_updates", out tempDate);
            if (isPresent)
            {
                dateWeLastCheckedForSoftwareUpdate = tempDate;
            } 
 
            // Network
            isPresent = SettingsBase.ReadIfPresent<int>(@"use_lan_for_file_sync", out temp);
            if (isPresent)
            {
                useLANForFileSync = temp;
            }

    
            // Bandwidth
            isPresent = SettingsBase.ReadIfPresent<int>(@"limit_download_speeds", out temp);
            if (isPresent)
            {
                limitDownloadSpeeds = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"download_speed_limit", out temp);
            if (isPresent)
            {
                downloadSpeedLimit = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"limit_upload_speeds", out temp);
            if (isPresent)
            {
                limitUploadSpeeds = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"upload_speed_limit", out temp);
            if (isPresent)
            {
                uploadSpeedLimit = temp;
            }
    
            // Proxy
            isPresent = SettingsBase.ReadIfPresent<int>(@"use_proxy_settings", out temp);
            if (isPresent)
            {
                useProxySetting = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"use_proxy_type", out temp);
            if (isPresent)
            {
                useProxyType = temp;
            }

            string tempString;
            isPresent = SettingsBase.ReadIfPresent<string>(@"proxy_server_address", out  tempString);
            if (isPresent)
            {
                proxyServerAddress = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"proxy_server_port", out temp);
            if (isPresent)
            {
                proxyServerPort = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"proxy_server_requires_password", out temp);
            if (isPresent)
            {
                proxyServerRequiresPassword = temp;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"proxy_server_username", out tempString);
            if (isPresent)
            {
                proxyServerUsername = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"proxy_server_password", out tempString);
            if (isPresent)
            {
                proxyServerPassword = tempString;
            }
    
            // Account
            isPresent = SettingsBase.ReadIfPresent<string>(@"akey", out tempString);
            if (isPresent)
            {
                akey = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"uuid", out tempString);
            if (isPresent)
            {
                uuid = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"user_name", out tempString);
            if (isPresent)
            {
                userName = tempString;
            }

            isPresent = SettingsBase.ReadIfPresent<string>(@"device_name", out tempString);
            if (isPresent)
            {
                deviceName = tempString;
            }

            Boolean tempBoolean;
            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"cs", out tempBoolean);  // 'cs' stands for "completed_setup", but we don't want to make it obvious.
            if (isPresent)
            {
                completedSetup = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"r_udid", out tempBoolean);
            if (isPresent)
            {
                udidRegistered = tempBoolean;
            }

            isPresent = SettingsBase.ReadIfPresent<int>(@"q", out temp);      // q is not the most clear value, but we don't want to make it obvious.
            if (isPresent)
            {
                quota = temp;
            }

            // Advanced
            isPresent = SettingsBase.ReadIfPresent<string>(@"cloud_folder_path", out tempString);
            if (isPresent)
            {
                cloudFolderPath = tempString;
            }

            FileStream tempStream;
            isPresent = SettingsBase.ReadIfPresent<FileStream>(@"cloud_folder_descriptor", out tempStream);
            if (isPresent)
            {
                cloudFolderDescriptor = tempStream;
            }
    
            // Index Services
            isPresent = SettingsBase.ReadIfPresent<int>(@"eid", out temp);
            if (isPresent)
            {
                eid = temp;
            }
    
            // Others
            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"add_dock_folder", out tempBoolean);
            if (isPresent)
            {
                addCloudFolderToDock = tempBoolean;
            }
            isPresent = SettingsBase.ReadIfPresent<Boolean>(@"desktop_shortcut", out tempBoolean);
            if (isPresent)
            {
                addCloudFolderToDesktop = tempBoolean;
            }
            isPresent = SettingsBase.ReadIfPresent<string>(@"sid", out tempString);
            if (isPresent)
            {
                sid = tempString;
            }

            List<string> tempList;
            isPresent = SettingsBase.ReadIfPresent<List<string>>(@"recent_items", out tempList);
            if (isPresent)
            {
                recentFileItems = tempList;
            }
        }
        /// <summary>
        /// Record timestamp
        /// </summary>
        public void recordEventId(int eventId)
        {  
            eid = eventId;
            SettingsBase.Write<int>(@"eid", eventId);
        }

        /// <summary>
        /// Record SID
        /// </summary>
        public void recordSID(string sidParm)
        {  
            sid = sidParm;
            SettingsBase.Write<string>(@"sid", sidParm);
        }

        /// <summary>
        /// Record account settings
        /// </summary>
        public void saveAccountSettings(Dictionary<string, object> accountInfo)
        {  
            userName = (string)accountInfo[@"user_name"];
            SettingsBase.Write<string>(@"user_name", userName);

            deviceName = (string)accountInfo[@"device_name"];
            SettingsBase.Write<string>(@"device_name", deviceName);

            udidRegistered = (Boolean)accountInfo[@"r_udid"];
            SettingsBase.Write<Boolean>(@"r_udid", udidRegistered);

            akey = (string)accountInfo[@"akey"];
            SettingsBase.Write<string>(@"akey", akey);
        
            uuid = (string)accountInfo[@"uuid"];
            SettingsBase.Write<string>(@"uuid", uuid);
        }

        /// <summary>
        /// Record quota
        /// </summary>
        public void setCloudQuota(int quotaParm)
        {  
            quota = quotaParm;
            SettingsBase.Write<int>(@"q", quotaParm);
        }

        /// <summary>
        /// Record setup completed
        /// </summary>
        public void setCloudAppSetupCompleted(Boolean completedSetupParm)
        {  
            completedSetup = completedSetupParm;
            SettingsBase.Write<Boolean>(@"cs", completedSetupParm);
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
            cloudFolderPath = path; 
            cloudFolderDescriptor = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            SettingsBase.Write<String>(@"cloud_folder_path", path);
            SettingsBase.Write<FileStream>(@"cloud_folder_descriptor", cloudFolderDescriptor);
        }

        /// <summary>
        /// Record the recently accessed item list.
        /// </summary>
        public void recordRecentItems(List<string> items)
        {  
            List<string> tempRecents = new List<string>();
            tempRecents.AddRange(recentFileItems);
            tempRecents.AddRange(items);

            // Remove duplicates and removed files
            List<String> copy = ExtensionMethods.DeepCopy(tempRecents);
            for (int i = copy.Count - 1; i >= 0; i--) 
            { 
                string fullPath = cloudFolderPath + copy[i];
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
            recentFileItems = ExtensionMethods.DeepCopy(recents);
            SettingsBase.Write<List<string>>(@"recent_items", recentFileItems);
        }
    }

    #endregion

} 

