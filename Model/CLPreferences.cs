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
    }
}
