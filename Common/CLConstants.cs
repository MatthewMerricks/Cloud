//
//  CLConstants.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

namespace win_client.Common
{
    public class CLConstants
    {
        public const string kFolderLocation = "folder_location";
        public const string kMergeFolders = "merge_folders";
        public const string kResourcesName = "win_client.Resources.Resources";
        public const string kXamlSuffix = ".xaml";

        // Dialog boxes
        public const string kDialogBox_CloudMessageBoxView = "dialog_box_CloudMessageBoxView";
        public const string kDialogBox_FolderSelectionSimpleView = "dialog_box_FolderSelectionSimpleView";
        public const string kDialogBox_PreferencesNetworkProxies = "dialog_box_PreferencesNetworkProxies";
        public const string kDialogBox_PreferencesNetworkBandwidth = "dialog_box_PreferencesNetworkBandwidth";
        public const string kDialogBox_CheckForUpdates = "dialog_box_CheckForUpdates";

        // Page URIs
        public const string kPageHome = "/Views/PageHome.xaml";
        public const string kPageCreateNewAccount = "/Views/PageCreateNewAccount.xaml";
        public const string kPageInvisible = "/Views/PageInvisible.xaml";
        public const string kPageSelectStorageSize = "/Views/PageSelectStorageSize.xaml";
        public const string kPageSetupSelector = "/Views/PageSetupSelector.xaml";
        public const string kPageFolderSelection = "/Views/PageFolderSelection.xaml";
        public const string kPageTour = "/Views/PageTour";            // base for /Views/PageTour1, 2, 3, etc.
        public const string kPageTourAdvancedEnd = "/Views/PageTourAdvancedEnd.xaml";
        public const string kPagePreferences = "/Views/PagePreferences.xaml";
        public const string kFramePreferencesGeneral = "/Views/FramePreferencesGeneral.xaml";
        public const string kFramePreferencesShortcuts = "/Views/FramePreferencesShortcuts.xaml";
        public const string kFramePreferencesAccount = "/Views/FramePreferencesAccount.xaml";
        public const string kFramePreferencesNetwork = "/Views/FramePreferencesNetwork.xaml";
        public const string kFramePreferencesAdvanced = "/Views/FramePreferencesAdvanced.xaml";
        public const string kFramePreferencesAbout = "/Views/FramePreferencesAbout.xaml";

        // Artwork
        public const string kPagePreferencesBackgroundGeneral = "/Cloud;component/Artwork/WinClient_Preferences_bg_650x421.png";
        public const string kPagePreferencesBackgroundAbout = "/Cloud;component/Artwork/WinClient_Preferences_about_bg_650x421.png";

        // Public web site
        public const string kUrlCloudCom = "http://www.cloud.com";

        // Validation template
        public const string kValidationErrorTemplate = "/Skins/ErrorTemplates.xaml";

        // Delimiters for parsing names into tokens
        public static char[] kDelimiterChars = { ' ', ',', '.', ':', '\t' };

        // WyUpdate constants
        public const string CLUpdaterRelativePath = "CloudUpdater.exe";
    }
}