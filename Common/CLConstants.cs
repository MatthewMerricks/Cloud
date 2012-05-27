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

        // Page URIs
        public const string kPageHome = "/Views/PageHome.xaml";
        public const string kPageCreateNewAccount = "/Views/PageCreateNewAccount.xaml";
        public const string kPageInvisible = "/Views/PageInvisible.xaml";
        public const string kPageSelectStorageSize = "/Views/PageSelectStorageSize.xaml";
        public const string kPageSetupSelector = "/Views/PageSetupSelector.xaml";
        public const string kPageTour = "/Views/PageTour";            // base for /Views/PageTour1, 2, 3, etc.

        // Validation template
        public const string kValidationErrorTemplate = "/Skins/ErrorTemplates.xaml";

        // Delimiters for parsing names into tokens
        public static char[] kDelimiterChars = { ' ', ',', '.', ':', '\t' };

    }
}