//
//  CLPreferences.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiPrivate.Model.Settings;

namespace win_client.Model
{
    public sealed class CLPreferences
    {


        // General pane (FramePreferencesGeneral)
        public bool ShouldStartCloudWhenSystemStarts { get; set; }
        public bool ShouldAnimateMenuBarIcon { get; set; }
        public bool ShouldShowDesktopNotification { get; set; }
        public bool ShouldUseCloudAsFolderIcon { get; set; }
        public cloudAppLanguageType Language { get; set; }

        // Account pane (FramePreferencesAccount)
        public int Quota { get; set; }
        public string UserName { get; set; }
        public string DeviceName { get; set; }

        // Network pane (FramePreferencesNetwork)
        public bool ShouldEnableLanSync { get; set; }

        // Bandwidth modal dialog (DialogPreferencesNetworkBandwidth)
        public bool ShouldLimitDownloadSpeed { get; set; }
        public int DownloadSpeedLimitKBPerSecond { get; set; }
        public uploadSpeedLimitType UploadSpeeedLimitType { get; set; }
        public int UploadSpeedLimitKBPerSecond { get; set; }

        // Proxies modal dialog (DialogPreferencesNetworkProxies)
        public useProxySettingType ProxySettingType { get; set; }
        public useProxyTypes ProxyType { get; set; }
        public string ProxyServerAddress { get; set; }
        public int ProxyServerPort { get; set; }
        public bool ProxyServerRequiresPassword { get; set; }
        public string ProxyServerUserName { get; set; }
        public string ProxyServerPassword { get; set; }

        /// <summary>
        /// Get the current prefereces from the user's Settings.
        /// </summary>
        public void GetPreferencesFromSettings()
        {
            // General pane
            this.ShouldAnimateMenuBarIcon = Settings.Instance.AnimateMenuBarForUpdates;
            this.ShouldShowDesktopNotification = Settings.Instance.ShowDesktopNotificationForUpdates;
            this.ShouldStartCloudWhenSystemStarts = Settings.Instance.StartCloudAppWithSystem;
            this.ShouldUseCloudAsFolderIcon = Settings.Instance.UseColorIconForCloudFolder;
            this.Language = (cloudAppLanguageType)Settings.Instance.CloudAppLanguage;

            // Account pane 
            this.Quota = Settings.Instance.Quota;
            this.UserName = Settings.Instance.UserName;
            this.DeviceName = Settings.Instance.DeviceName;

            // Network pane
            this.ShouldEnableLanSync = Settings.Instance.UseLANForFileSync;

            // Bandwidth modal dialog (DialogPreferencesNetworkBandwidth)
            this.ShouldLimitDownloadSpeed = Settings.Instance.LimitDownloadSpeeds;
            this.DownloadSpeedLimitKBPerSecond = Settings.Instance.DownloadSpeedLimit;
            this.UploadSpeeedLimitType = Settings.Instance.LimitUploadSpeeds;
            this.UploadSpeedLimitKBPerSecond = Settings.Instance.UploadSpeedLimit;

            // Proxies modal dialog (DialogPreferencesNetworkProxies)
            this.ProxySettingType = Settings.Instance.UseProxySetting;
            this.ProxyType = Settings.Instance.UseProxyType;
            this.ProxyServerAddress = Settings.Instance.ProxyServerAddress;
            this.ProxyServerPort = Settings.Instance.ProxyServerPort;
            this.ProxyServerRequiresPassword = Settings.Instance.ProxyServerRequiresPassword;
            this.ProxyServerUserName = Settings.Instance.ProxyServerUsername;
            this.ProxyServerPassword = Settings.Instance.ProxyServerPassword;
        }

        /// <summary>
        /// Persist the memory settings to the user's Settings.
        /// </summary>
        public void SetPreferencesToSettings()
        {
            // General pane
            Settings.Instance.AnimateMenuBarForUpdates = this.ShouldAnimateMenuBarIcon;
            Settings.Instance.ShowDesktopNotificationForUpdates = this.ShouldShowDesktopNotification;
            Settings.Instance.StartCloudAppWithSystem = this.ShouldStartCloudWhenSystemStarts;
            Settings.Instance.UseColorIconForCloudFolder = this.ShouldUseCloudAsFolderIcon;
            Settings.Instance.CloudAppLanguage = (int)this.Language;

            // Account pane 
            Settings.Instance.Quota = this.Quota;
            Settings.Instance.UserName = this.UserName;
            Settings.Instance.DeviceName = this.DeviceName;

            // Network pane
            Settings.Instance.UseLANForFileSync = this.ShouldEnableLanSync;

            // Bandwidth modal dialog (DialogPreferencesNetworkBandwidth)
            Settings.Instance.LimitDownloadSpeeds = this.ShouldLimitDownloadSpeed;
            Settings.Instance.DownloadSpeedLimit = this.DownloadSpeedLimitKBPerSecond;
            Settings.Instance.LimitUploadSpeeds = this.UploadSpeeedLimitType;
            Settings.Instance.UploadSpeedLimit = this.UploadSpeedLimitKBPerSecond;

            // Proxies modal dialog (DialogPreferencesNetworkProxies)
            Settings.Instance.UseProxySetting = this.ProxySettingType;
            Settings.Instance.UseProxyType = this.ProxyType;
            Settings.Instance.ProxyServerAddress = this.ProxyServerAddress;
            Settings.Instance.ProxyServerPort = this.ProxyServerPort;
            Settings.Instance.ProxyServerRequiresPassword = this.ProxyServerRequiresPassword;
            Settings.Instance.ProxyServerUsername = this.ProxyServerUserName;
            Settings.Instance.ProxyServerPassword = this.ProxyServerPassword;
        }
    }
}
