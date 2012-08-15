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
        public const string kFramePreferencesAccount = "/Views/FramePreferencesAccount.xaml";
        public const string kFramePreferencesNetwork = "/Views/FramePreferencesNetwork.xaml";
        public const string kFramePreferencesAdvanced = "/Views/FramePreferencesAdvanced.xaml";
        public const string kFramePreferencesAbout = "/Views/FramePreferencesAbout.xaml";

        // Validation template
        public const string kValidationErrorTemplate = "/Skins/ErrorTemplates.xaml";

        // Delimiters for parsing names into tokens
        public static char[] kDelimiterChars = { ' ', ',', '.', ':', '\t' };

        // Values for the setBadgeType type parameter
        //TODO: These will be defined in BadgeNET.  Remove this when they are available.
        public enum CloudAppIconBadgeType
        {
            cloudAppBadgeNone                   = 0, // clears a badge overlay, if any.
            cloudAppBadgeSynced                 = 1, // sets a badge with a checkmark or similar metaphor.
            cloudAppBadgeSyncing                = 2, // sets a badge indicating circular motion, active sync.
            cloudAppBadgeFailed                 = 3, // sets a badge with an x indicating failure to sync.
            cloudAppBadgeSyncSelective          = 4, // sets a badge with an x indicating failure to sync.
            cloudAppBadgeMaxIndexPlusOne             // Maximum index plus one.  Add new values above this line.
        };
    }
}