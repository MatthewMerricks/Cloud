//
//  CLDefinitions.cs
//  Cloud SDK Windows 
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

#define PRODUCTION_BACKEND 

// Merged 7/3/12
namespace CloudApiPublic.Model
{
    public static class CLDefinitions
    {

#if PRODUCTION_BACKEND

        // Registration
        public const string CLRegistrationCreateRequestURLString  = @"https://auth.cloud.com/user/create.json";
        public const string CLRegistrationCreateRequestBodyString = @"user[first_name]={0}&user[last_name]={1}&user[email]={2}&user[password]={3}&device[friendly_name]={4}&device[device_uuid]={5}&device[os_type]={6}&device[os_version]={7}&device[app_version]={8}";

        // Link/Unlink
        public const string CLRegistrationUnlinkRequestURLString  = @"https://auth.cloud.com/device/unlink.json";
        public const string CLRegistrationUnlinkRequestBodyString = @"access_token=[0]";
        public const string CLRegistrationLinkRequestURLString    = @"https://auth.cloud.com/device/link.json";
        public const string CLRegistrationLinkRequestBodyString   = @"email={0}&password={1}&device[friendly_name]={2}&device[device_uuid]={3}&device[os_type]={4}&device[os_version]={5}&device[app_version]={6}";

        // Meta Data
        public const string CLMetaDataServerURL =@"https://mds.cloud.com";

        // Notifications
        public const string CLNotificationServerURL = @"ws://push.cloud.com/events";

        // Upload/Download Server
        public const string CLUploadDownloadServerURL = @"https://upd.cloud.com";

#else

        // Registration
        public const string CLRegistrationCreateRequestURLString  = @"https://auth.cloudburrito.com/user/create.json";
        public const string CLRegistrationCreateRequestBodyString = @"user[first_name]={0}&user[last_name]={1}&user[email]={2}&user[password]={3}&device[friendly_name]={4}&device[device_uuid]={5}&device[os_type]={6}&device[os_version]={7}&device[app_version]={8}";

        // Link/Unlink
        public const string CLRegistrationUnlinkRequestURLString  = @"https://auth.cloudburrito.com/device/unlink.json";
        public const string CLRegistrationUnlinkRequestBodyString = @"access_token={0}";
        public const string CLRegistrationLinkRequestURLString    = @"https://auth.cloudburrito.com/device/link.json";
        public const string CLRegistrationLinkRequestBodyString   = @"email={0}&password={1}&device[friendly_name]={2}&device[device_uuid]={3}&device[os_type]={4}&device[os_version]={5}&device[app_version]={6}";

        // Meta Data
        public const string CLMetaDataServerURL = @"https://mds-edge.cloudburrito.com";

        // Notifications
        public const string CLNotificationServerURL = @"ws://push-edge.cloudburrito.com/events";

        // Upload/Download Server
        public const string CLUploadDownloadServerURL = @"https://upd-edge.cloudburrito.com";

#endif

        // Error Domain
        public const string CLCloudAppRestAPIErrorDomain = @"com.cloudapp.networking.error";

        // Sync Header
        public const string CLSyncEventMetadata = "metadata";
        public const string CLSyncEventHeader = "sync_header";
        public const string CLEventTypeAddFile = "add_file";
        public const string CLEventTypeDeleteFile = "delete_file";
        public const string CLEventTypeModifyFile = "modify_file";
        public const string CLEventTypeRenameFile = "rename_file";
        public const string CLEventTypeMoveFile = "move_file";
        public const string CLEventTypeAddLink = "add_link";
        public const string CLEventTypeDeleteLink = "delete_link";
        public const string CLEventTypeModifyLink = "modify_link";
        public const string CLEventTypeRenameLink = "rename_link";
        public const string CLEventTypeMoveLink = "move_link";
        public const string CLEventTypeAddFolder = "add_folder";
        public const string CLEventTypeDeleteFolder = "delete_folder";
        public const string CLEventTypeRenameFolder = "rename_folder";
        public const string CLEventTypeMoveFolder = "move_folder";
        public const string CLEventTypeModifyRange = "modify";
        public const string CLEventTypeDeleteRange = "delete";
        public const string CLEventTypeRenameRange = "rename";
        public const string CLEventTypeMoveRange = "move";
        public const string CLEventTypeAddRange = "add";
        public const string CLEventTypeFileRange = "file";
        public const string CLEventTypeLinkRange = "link";
        public const string CLEventTypeFolderRange = "folder";

        public static readonly string[] SyncHeaderDeletions =
        {
            CLEventTypeDeleteFile,
            CLEventTypeDeleteLink,
            CLEventTypeDeleteFolder,
            CLEventTypeDeleteRange
        };

        public static readonly string[] SyncHeaderCreations =
        {
            CLEventTypeAddFile,
            CLEventTypeAddLink,
            CLEventTypeAddFolder,
            CLEventTypeAddRange
        };

        public static readonly string[] SyncHeaderModifications =
        {
            CLEventTypeModifyFile,
            CLEventTypeModifyLink,
            // Note: no modify folder, that is an invalid modification
            CLEventTypeModifyRange
        };

        public static readonly string[] SyncHeaderRenames =
        {
            CLEventTypeRenameFile,
            CLEventTypeRenameLink,
            CLEventTypeRenameFolder,
            CLEventTypeRenameRange
        };

        // Cloud Sync Status
        public const string CLEventTypeUpload = "upload";
        public const string CLEventTypeExists = "exists";
        public const string CLEventTypeDuplicate = "duplicate";
        public const string CLEventTypeUploading = "uploading";
        public const string CLEventTypeConflict = "conflict";
        public const string CLEventTypeNotFound = "not_found";

        // Cloud Metadata Protocol 
        public const string CLMetadataCloudPath = "path";
        public const string CLMetadataFileHash = "file_hash";
        public const string CLMetadataFileRevision = "revision";
        public const string CLMetadataFileCreateDate = "created_date";
        public const string CLMetadataFileModifiedDate = "modified_date";
        public const string CLMetadataFileIsDeleted = "isDeleted";
        public const string CLMetadataFileIsDirectory = "is_folder";
        public const string CLMetadataFileIsLink = "is_link";
        public const string CLMetadataFileSize = "file_size";
        public const string CLMetadataIsPending = "is_pending";
        public const string CLMetadataFromPath = "from_path";
        public const string CLMetadataToPath = "to_path";
        public const string CLMetadataItemStorageKey = "storage_key";
        public const string CLMetadataLastEventID = "last_event_id";
        public const string CLMetadataStorageKey = "storage_key";
        public const string CLMetadataFileTarget = "target_path";
        public const string CLMetadataParentPath = "parent_path";
        public const string CLMetadataFileCAttributes = "custom_attributes";

        // Cloud Events
        public const string CLSyncEvent = "event";
        public const string CLClientEventId = "client_reference";
        public const string CLSyncEventStatus = "status";
        public const string CLSyncEvents = "events";
        public const string CLSyncID = "sid";
        public const string CLSyncEventID = "eid";

        // Cloud Events Types
        public const string CLEventTypeAdd = "type_add";
        public const string CLEventTypeModify = "type_modify";
        public const string CLEventTypeRenameMove = "type_rename_move";
        public const string CLEventTypeDelete = "type_delete";

        // Cloud Notification Types
        public const string CLNotificationTypeNew = "new";
        public const string CLNotificationTypeUpgrade = "upgrade";
        public const string CLNotificationTypeShare = "sharw";

        // Sync dictionaries
        public const string CLEventKey = "event_id";
        public const string CLEventCount = "event_count";

        // Invalid SID or EID
        public const long CLDoNotSaveId = 1608198229012012;   // used with SyncTo and SyncFrom to represent sid and eid

    }
}