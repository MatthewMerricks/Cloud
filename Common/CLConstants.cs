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
        public const long CLDoNotSaveId = 1608198229012012;   // used with SyncTo and SyncFrom to represent sid and eid

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

        // Registration
        public const string CLRegistrationCreateRequestURLString  = "https://auth.cloudburrito.com/user/create.json";
        public const string CLRegistrationCreateRequestBodyString = "user[first_name]=%@&user[last_name]=%@&user[email]=%@&user[password]=%@&device[friendly_name]=%@&device[device_uuid]=%@&device[os_type]=%@&device[os_version]=%@&device[app_version]=%@";

        // Link/Unlink
        public const string CLRegistrationUnlinkRequestURLString  = "https://auth.cloudburrito.com/device/unlink.json";
        public const string CLRegistrationUnlinkRequestBodyString = "access_token=%@";
        public const string CLRegistrationLinkRequestURLString    = "https://auth.cloudburrito.com/device/link.json";
        public const string CLRegistrationLinkRequestBodyString   = "email=%@&password=%@&device[friendly_name]=%@&device[device_uuid]=%@&device[os_type]=%@&device[os_version]=%@&device[app_version]=%@";

        // Meta Data
        public const string CLMetaDataServerURL = "https://mds2.cloudburrito.com";

        // Notifications
        public const string CLNotificationServerURL = "ws://23.22.69.142:80";

        // Error Domain
        public const string CLCloudAppRestAPIErrorDomain = "com.cloudapp.networking.error";

        // Upload/Download Server
        public const string CLUploadDownloadServerURL = "https://upd.cloudburrito.com";

        // Sync Header
        public const string CLSyncEventMetadata      = "metadata";
        public const string CLSyncEventHeader        = "sync_header";
        public const string CLEventTypeAddFile       = "add_file";
        public const string CLEventTypeDeleteFile    = "delete_file";
        public const string CLEventTypeModifyFile    = "modify_file";
        public const string CLEventTypeRenameFile    = "rename_file";
        public const string CLEventTypeMoveFile      = "move_file";
        public const string CLEventTypeAddFolder     = "add_folder";
        public const string CLEventTypeDeleteFolder  = "delete_folder";
        public const string CLEventTypeRenameFolder  = "rename_folder";
        public const string CLEventTypeMoveFolder    = "move_folder";
        public const string CLEventTypeDeleteRange   = "delete";
        public const string CLEventTypeRenameRange   = "rename";
        public const string CLEventTypeMoveRange     = "move";
        public const string CLEventTypeAddRange      = "add";
        public const string CLEventTypeFileRange     = "file";
        public const string CLEventTypeFolderRange   = "folder";

        // Cloud Sync Status
        public const string LEventTypeUpload          = "upload";
        public const string CLEventTypeExists          = "exists";
        public const string CLEventTypeDuplicate       = "duplicate";
        public const string CLEventTypeUploading       = "uploading";
        public const string CLEventTypeConflict        = "conflict";

        // Cloud Metadata Protocol 
        public const string CLMetadataCloudPath        = "path";
        public const string CLMetadataFileHash         = "file_hash";
        public const string CLMetadataFileRevision     = "revision";
        public const string CLMetadataFileCreateDate   = "created_date";
        public const string CLMetadataFileModifiedDate = "modified_date";
        public const string CLMetadataFileIsDeleted    = "isDeleted";
        public const string CLMetadataFileIsDirectory  = "isDirectory";
        public const string CLMetadataFileSize         = "file_size";
        public const string CLMetadataIsPending        = "is_pending";
        public const string CLMetadataFromPath         = "from_path";
        public const string CLMetadataToPath           = "to_path";
        public const string CLMetadataItemStorageKey   = "storage_key";
        public const string CLMetadataLastEventID      = "last_event_id";
        public const string CLMetadataStorageKey       = "storage_key";

        // Cloud Events
        public const string CLSyncEvent                = "event";
        public const string CLSyncEventStatus          = "status";
        public const string CLSyncEvents               = "events";
        public const string CLSyncID                   = "sid";
        public const string CLSyncEventID              = "eid";

        // Cloud Events Types
        public const string CLEventTypeAdd           = "type_add";
        public const string CLEventTypeModify        = "type_modify";
        public const string CLEventTypeRenameMove    = "type_rename_move";
        public const string CLEventTypeDelete        = "type_delete";
    }
}