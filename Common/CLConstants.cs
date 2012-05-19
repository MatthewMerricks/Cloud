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
        public static string kFolderLocation = @"folder_location";
        public static string kMergeFolders = @"merge_folders";
        public static string kResourcesName = @"win_client.Resources.Resources";
        public static string kXamlSuffix = @".xaml";

        // Dialog boxes
        public static string kDialogBox_CloudMessageBoxView = @"dialog_box_CloudMessageBoxView";
        public static string kDialogBox_FolderSelectionSimpleView = @"dialog_box_FolderSelectionSimpleView";

        // Page URIs
        public static string kPageHome = @"/Views/PageHome.xaml";
        public static string kPageCreateNewAccount = @"/Views/PageCreateNewAccount.xaml";
        public static string kPageInvisible = @"/Views/PageInvisible.xaml";
        public static string kPageSelectStorageSize = @"/Views/PageSelectStorageSize.xaml";
        public static string kPageSetupSelector = @"/Views/PageSetupSelector.xaml";
        public static string kPageTour = @"/Views/PageTour";            // base for /Views/PageTour1, 2, 3, etc.

        // Validation template
        public static string kValidationErrorTemplate = @"/Skins/ErrorTemplates.xaml";
    }
}