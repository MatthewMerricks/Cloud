//
//  CLDefinitions.cs
//  Cloud SDK Windows 
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

// Back end definitions
// @@@@@@@@@ EXACTLY ONE OF THE FOLLOWING MUST BE DEFINED @@@@@@@@@@@@@@@@@
//#define PRODUCTION_BACKEND 
#define DEVELOPMENT_BACKEND     // cliff.cloudburrito.com
//#define STAGING_BACKEND       // cloudstaging.us

// URL definitions
// @@@@@@@@@ EXACTLY ONE OF THE FOLLOWING MUST BE DEFINED @@@@@@@@@@@@@@@@@
//#define URL_OLD
#define URL_API         // api.cloud.com

namespace CloudApiPublic.Static
{
    /// <summary>
    /// Contains constants used by Cloud
    /// </summary>
    public static class CLDefinitions
    {
        // Define the HTTP prefix to use.
#if NOSSL
        public const string HttpPrefix = "http://";
        public const string WsPrefix = "ws://";
#else
        public const string HttpPrefix = "https://";
        public const string WsPrefix = "wss://";
#endif
        // Define the subdomain
#if URL_API
        public const string SubDomainPrefix = "api.";
#else
        public const string SubDomainPrefix = "";
#endif  // !URL_API

        // Define the version
        public const string VersionPrefix = "/1";

        // Define the domain
#if PRODUCTION_BACKEND
        public const string Domain = "cloud.com";
#elif STAGING_BACKEND
        public const string Domain = "cloudstaging.us";
#else
        public const string Domain = "cliff.cloudburrito.com";
#endif

        // Server URLs built from the above definitions
#if URL_API
        // Platform Auth
        public const string CLPlatformAuthServerURL = HttpPrefix + SubDomainPrefix + Domain;

        // Meta Data
        public const string CLMetaDataServerURL = HttpPrefix + SubDomainPrefix + Domain;

        // Notifications
        public const string CLNotificationServerWsURL = WsPrefix + SubDomainPrefix + Domain;
        public const string CLNotificationServerSseURL = HttpPrefix + SubDomainPrefix + Domain;

        // Upload/Download Server
        public const string CLUploadDownloadServerURL = HttpPrefix + SubDomainPrefix + Domain;
#else
        // Platform Auth
        public const string CLPlatformAuthServerURL = HttpPrefix + @"platform-management." + Domain;

        // Meta Data
        public const string CLMetaDataServerURL = HttpPrefix + @"mds." + Domain;

        // Notifications
        public const string CLNotificationServerWsURL = WsPrefix + @"push." + Domain;
        public const string CLNotificationServerSseURL = HttpPrefix + @"push." + Domain;

        // Upload/Download Server
        public const string CLUploadDownloadServerURL = HttpPrefix + @"upd." + Domain;
#endif  // !URL_API

        // Miscellaneous constants
        public const string CLRegistrationAccessTokenKey = "access_token";
        public const string CLClientVersionHeaderName = "X-Cld-Client-Version";

        public const int ManualPollingIterationsBeforeConnectingPush = 10;
        public const int MaxManualPollingPeriodSeconds = 60;    // the manual polling period is a random number between these numbers.
        public const int MinManualPollingPeriodSeconds = 30;    // ....
        public const int PushNotificationFaultLimitBeforeFallback = 5;
        public const int MaxNumberOfConcurrentUploads = 6;
        public const int MaxNumberOfConcurrentDownloads = 6;
        public const int HttpTimeoutDefaultMilliseconds = 180000;

        public const string CLTwitterPageUrl = "http://twitter.com/clouddotcom";

        // Method Path
#if DEVELOPMENT_BACKEND || PRODUCTION_BACKEND || STAGING_BACKEND
#if URL_API
        public const string MethodPathSyncFrom = VersionPrefix + "/sync/from_cloud";                            // POST
        public const string MethodPathDownload = VersionPrefix + "/sync/file/download";                         // POST
        public const string MethodPathUpload = VersionPrefix + "/sync/file/upload";                             // POST
        public const string MethodPathSyncTo = VersionPrefix + "/sync/to_cloud";                                // POST
        public const string MethodPathPurgePending = VersionPrefix + "/sync/file/purge_pending";                // POST
        public const string MethodPathGetPending = VersionPrefix + "/sync/file/pending";                        // GET
        public const string MethodPathGetFileMetadata = VersionPrefix + "/sync/file/metadata";                  // GET
        public const string MethodPathGetFolderMetadata = VersionPrefix + "/sync/folder/metadata";              // GET
        public const string MethodPathSyncBoxList = VersionPrefix + "/sync/syncbox/list";                       // POST

        //public const string MethodPathGetUsedBytes = VersionPrefix + "/sync/file/used_bytes";                   // GET  @@@@@@@@@@@@  DEPRECATED  @@@@@@@@@@@@@@@@@@@@@@

        #region one-off
        #region files
        public const string MethodPathOneOffFileCreate = VersionPrefix + "/sync/file/add";                      // POST
        public const string MethodPathOneOffFileDelete = VersionPrefix + "/sync/file/delete";                   // POST
        public const string MethodPathOneOffFileModify = VersionPrefix + "/sync/file/modify";                   // POST
        public const string MethodPathOneOffFileMove = VersionPrefix + "/sync/file/move";                        // POST
        /* duplicate functionality to file move:
        public const string MethodPathOneOffFileRename = VersionPrefix + "/sync/file/rename";                   // POST
 
        */
        #endregion

        #region folders
        public const string MethodPathOneOffFolderCreate = VersionPrefix + "/sync/folder/add";                  // POST
        public const string MethodPathOneOffFolderDelete = VersionPrefix + "/sync/folder/delete";               // POST
        public const string MethodPathOneOffFolderMove = VersionPrefix + "/sync/folder/move";                   // POST
        /* duplicate functionality to folder move:
        public const string MethodPathOneOffFolderRename = VersionPrefix + "/sync/folder/rename";               // POST
         */
        #endregion
        #endregion

        #region other file operations
        public const string MethodPathFileUndelete = VersionPrefix + "/sync/file/undelete";                     // POST
        public const string MethodPathFileGetVersions = VersionPrefix + "/sync/file/versions";                  // GET
        public const string MethodPathFileCopy = VersionPrefix + "/sync/file/copy";                             // POST
        public const string MethodPathGetPictures = VersionPrefix + "/sync/file/pictures";                      // GET
        public const string MethodPathGetVideos = VersionPrefix + "/sync/file/videos";                          // GET
        public const string MethodPathGetAudios = VersionPrefix + "/sync/file/audios";                          // GET
        public const string MethodPathGetArchives = VersionPrefix + "/sync/file/archives";                      // GET
        public const string MethodPathGetRecents = VersionPrefix + "/sync/file/recents";                        // GET
        #endregion

        #region other folder operations
        public const string MethodPathGetFolderContents = VersionPrefix + "/sync/folder/contents";              // GET
        public const string MethodPathGetFolderHierarchy = VersionPrefix + "/sync/folder/hierarchy";            // GET
        public const string MethodPathFolderUndelete = VersionPrefix + "/sync/folder/undelete";                 // POST
        #endregion

        #region SyncBox operations
        public const string MethodPathSyncBoxUsage = VersionPrefix + "/sync/syncbox/usage";                     // GET
    	#endregion

        #region Notification operations
        public const string MethodPathPushSubscribe = VersionPrefix + "/sync/notifications/subscribe";          // GET
    	#endregion

        #region Platform Management operations
        public const string MethodPathAuthCreateSyncBox = VersionPrefix + "/sync/syncbox/create";               // POST
        public const string MethodPathAuthListSyncBoxes = VersionPrefix + "/sync/syncbox/list";                 // POST
        #endregion
#else
        public const string MethodPathSyncFrom = "/1/sync/from_cloud";                       // POST
        public const string MethodPathDownload = "/1/get_file";                              // POST
        public const string MethodPathUpload = "/1/put_file";                                // POST
        public const string MethodPathSyncTo = "/1/sync/to_cloud";                           // POST
        public const string MethodPathPurgePending = "/1/file/purge/pending";                // POST
        public const string MethodPathGetPending = "/1/file/pending";                        // GET
        public const string MethodPathGetFileMetadata = "/1/file/metadata";                  // GET
        public const string MethodPathGetFolderMetadata = "/1/folder/metadata";              // GET
        public const string MethodPathSyncBoxList = "/1/sync_box/list";                      // POST

        //public const string MethodPathGetUsedBytes = "/1/file/used_bytes";                   // GET  @@@@@@@@@@@@  DEPRECATED  @@@@@@@@@@@@@@@@@@@@@@

        #region one-off
        #region files
        public const string MethodPathOneOffFileCreate = "/1/file/add";                      // POST
        public const string MethodPathOneOffFileDelete = "/1/file/delete";                   // POST
        public const string MethodPathOneOffFileModify = "/1/file/modify";                   // POST
        public const string MethodPathOneOffFileMove = "/1/file/move";                       // POST
        /* duplicate functionality to file move:
        public const string MethodPathOneOffFileRename = "/1/file/rename";                   // POST
         */
        #endregion

        #region folders
        public const string MethodPathOneOffFolderCreate = "/1/folder/add";                  // POST
        public const string MethodPathOneOffFolderDelete = "/1/folder/delete";               // POST
        public const string MethodPathOneOffFolderMove = "/1/folder/move";                   // POST
        /* duplicate functionality to folder move:
        public const string MethodPathOneOffFolderRename = "/1/folder/rename";               // POST
         */
        #endregion
        #endregion

        #region other file operations
        public const string MethodPathFileUndelete = "/1/file/undelete";                     // POST
        public const string MethodPathFileGetVersions = "/1/file/versions";                  // GET
        public const string MethodPathFileCopy = "/1/file/copy";                             // POST
        public const string MethodPathGetPictures = "/1/file/pictures";                      // GET
        public const string MethodPathGetVideos = "/1/file/videos";                          // GET
        public const string MethodPathGetAudios = "/1/file/audios";                          // GET
        public const string MethodPathGetArchives = "/1/file/archives";                      // GET
        public const string MethodPathGetRecents = "/1/file/recents";                        // GET
        #endregion

        #region other folder operations
        public const string MethodPathGetFolderContents = "/1/folder/contents";              // GET
        public const string MethodPathGetFolderHierarchy = "/1/folder/hierarchy";            // GET
        public const string MethodPathFolderUndelete = "/1/folder/undelete";                 // POST
        #endregion

        public const string MethodPathSyncBoxUsage = "/1/sync_box/usage";                    // GET

        public const string MethodPathPushSubscribe = "/1/sync/subscribe";                   // GET

        #region Platform Management operations
        public const string MethodPathAuthCreateSyncBox = "/1/sync/sync_box/create";         // POST
        public const string MethodPathAuthListSyncBoxes = "/1/sync/sync_box/list";           // POST
        #endregion
#endif  // !URL_API
#endif  // DEVELOPMENT_BACKEND || PRODUCTION_BACKEND || STAGING_BACKEND

        public const string AuthorizationFormatType = "CWS0";

        // Common Json field names
        public const string JsonResponseStatus = "status";
        public const string JsonResponseMessage = "message";
        public const string JsonAccountId = "account_id";
        public const string JsonApplicationsList = "client_applications";

        // Json objects
        public const string JsonAccount = "account";

        // Json application fields
        public const string JsonApplicationFieldId = "id";
        public const string JsonApplicationFieldAccountId = "account_id";
        public const string JsonApplicationFieldName = "name";
        public const string JsonApplicationFieldKey = "key";
        public const string JsonApplicationFieldSecret = "secret";

        // Json client_application fields
        public const string JsonClientApplication = "client_application";
        public const string JsonClientApplicationFieldId = "id";
        public const string JsonClientApplicationFieldAccountId = "account_id";
        public const string JsonClientApplicationFieldName = "name";
        public const string JSonClientApplicationFieldApplicationNamespace = "application_namespace";
        public const string JsonClientApplicationFieldKey = "key";
        public const string JsonClientApplicationFieldSecret = "secret";

        // Json service type fields
        public const string JsonServiceTypeFieldName = "name";
        public const string JsonServiceTypeFieldCode = "code";

        // Json /private/services/list
        public const string JsonPrivateServicesListResponseFieldServiceTypes = "service_types";

        // Json 

        // Json account fields
        public const string JsonAccountFieldId = "id";
        public const string JsonAccountFieldName = "name";
        public const string JsonAccountFieldSyncBoxId = "sync_box_id";
        public const string JsonAccountFieldCreatedAt = "created_at";

        // client_param fields
        public const string JsonClientParamsFieldName = "name";
        public const string JsonClientParamsFieldApplicationNamespace = "application_namespace";

        // client_param values
        public const string JsonClientParamsFieldName_Value = "WindowsCloudClient";
        public const string JsonClientParamsFieldApplicationNamespace_Value = "com.cloud.product.clients.windows";

        // Query string keys
        public const string QueryStringDeviceId = "device_uuid";
        public const string QueryStringSyncBoxId = "sync_box_id";
        public const string QueryStringIncludeDeleted = "include_deleted";
        public const string QueryStringDepth = "depth";
        public const string QueryStringIncludeCount = "include_count";
        public const string QueryStringSender = "sender";

        // HttpWebRequest Header Key
        public const string HeaderKeyAuthorization = "Authorization";
        public const string HeaderKeyProxyAuthorization = "Proxy-Authorization";
        public const string HeaderKeyProxyAuthenticate = "Proxy-Authenticate";
        public const string HeaderKeyContentEncoding = "Content-Encoding";

        // Sync constants
        public const int SyncConstantsMaximumSyncToEvents = 1000;
        public const int SyncConstantsResponseBufferSize = 4096;

        // HttpWebRequest Header Append
        public const string HeaderAppendToken = ", token=";
        public const string HeaderAppendCWS0 = "CWS0 ";
        public const string HeaderAppendKey = "key=";
        public const string HeaderAppendSignature = "signature=";
        public const string HeaderAppendContentTypeJson = "application/json";
        public const string HeaderAppendContentTypeBinary = "application/octet-stream";
        public const string HeaderAppendContentEncoding = "UTF8";
        public const string HeaderAppendMethodPost = "POST";
        public const string HeaderAppendMethodGet = "GET";
        public const string HeaderAppendMethodPut = "PUT";
        public const string HeaderAppendCloudClient = "Windows SDK Client";
        public const string HeaderAppendStorageKey = "X-Ctx-Storage-Key";
        public const string HeaderAppendContentMD5 = "Content-MD5";

        public static string WrapInDoubleQuotes(string toWrap)
        {
            if (toWrap == null)
            {
                return null;
            }
            return DoubleQuote + toWrap + DoubleQuote;
        }
        private const char DoubleQuote = '\"';

        // Sync Body
        public const string SyncBodyRelativeRootPath = "/";

        // Sync Header
        public const string CLSyncEventMetadata = "metadata";
        public const string CLSyncEventHeader = "sync_header";
        public const string CLEventTypeAddFile = "add_file"; // returned upon undeleting a file
        public const string CLEventTypeCopyFile = "copy_file";
        public const string CLEventTypeDeleteFile = "delete_file";
        public const string CLEventTypeModifyFile = "modify_file";
        public const string CLEventTypeRenameFile = "rename_file";
        public const string CLEventTypeMoveFile = "move_file";
        public const string CLEventTypeAddLink = "add_link";
        public const string CLEventTypeCopyLink = "copy_link";
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
            CLEventTypeCopyFile,
            CLEventTypeAddLink,
            CLEventTypeCopyLink,
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
            CLEventTypeRenameRange,

            CLEventTypeMoveFile,
            CLEventTypeMoveLink,
            CLEventTypeMoveFolder,
            CLEventTypeMoveRange
        };

        public static readonly string[] SyncHeaderIsFolders =
        {
            CLEventTypeDeleteFolder,
            CLEventTypeAddFolder,
            CLEventTypeRenameFolder,
            CLEventTypeMoveFolder,
            // Note: no modify folder, that is an invalid modification
        };

        public static readonly string[] SyncHeaderIsFiles =
        {
            CLEventTypeDeleteFile,
            CLEventTypeAddFile,
            CLEventTypeCopyFile,
            CLEventTypeRenameFile,
            CLEventTypeMoveFile,
            CLEventTypeModifyFile,

            CLEventTypeDeleteLink,
            CLEventTypeAddLink,
            CLEventTypeCopyLink,
            CLEventTypeRenameLink,
            CLEventTypeMoveLink,
            CLEventTypeModifyLink
        };

        // SyncBox
        public const string CLSyncBoxClientAppId = "client_application_id";
        public const string CLSyncBoxCreatedAt = "created_at";
        public const string CLSyncBoxId = "id";
        public const string CLSyncBoxStorageQuota = "storage_quota";
        public const string CLSyncBoxUpdatedAt = "updated_at";
        public const string CLSyncBoxStoredBytes = "stored_bytes";
        public const string CLSyncBoxPendingBytes = "pending_bytes";

        // Cloud Sync Status
        public const string CLEventTypeAccepted = "ok";
        public const string CLEventTypeUpload = "upload";
        public const string CLEventTypeExists = "exists";
        public const string CLEventTypeDuplicate = "duplicate";
        public const string CLEventTypeUploading = "uploading";
        public const string CLEventTypeConflict = "conflict";
        public const string CLEventTypeNotFound = "not_found";
        public const string CLEventTypeDownload = "download";

        // Cloud Metadata Protocol
        public const string CLMetadataFileObject = "file_object";
        public const string CLMetadataFolderObject = "folder_object";
        public const string CLMetadataCloudPath = "path";
        public const string CLMetadataFileHash = "file_hash";
        public const string CLMetadataFileRevision = "revision";
        public const string CLMetadataCreateDate = "created_date";
        public const string CLMetadataModifiedDate = "modified_date";
        public const string CLMetadataIsDeleted = "is_deleted";
        public const string CLMetadataIsDirectory = "is_folder";
        public const string CLMetadataFileIsLink = "is_link";
        public const string CLMetadataFileSize = "file_size";
        public const string CLMetadataIsPending = "is_pending";
        public const string CLMetadataFromPath = "from_path";
        public const string CLMetadataToPath = "to_path";
        public const string CLMetadataLastEventID = "last_event_id";
        public const string CLMetadataStorageKey = "storage_key";
        public const string CLMetadataFullStorageKey = "full_storage_key";
        public const string CLMetadataFileTarget = "target_path";
        public const string CLMetadataParentPath = "parent_path";
        public const string CLMetadataFileCAttributes = "custom_attributes";
        public const string CLMetadataMimeType = "mime_type";
        public const string CLMetadataIcon = "icon";
        public const string CLMetadataVersion = "version";
        public const string CLMetadataFiles = "files";
        public const string CLMetadataFile = "file";
        public const string CLMetadataServerId = "uid";
        public const string CLMetadataCount = "count";
        public const string CLMetadataMoreItems = "more_items";
        public const string CLMetadataLocal = "local";
        public const string CLMetadataShared = "shared";
        public const string CLMetadataFolders = "folders";
        public const string CLMetadataLinks = "links";
        public const string CLMetadataItemCount = "item_count";
        public const string CLMetadataDeletedItemCount = "deleted_item_count";
        public const string CLMetadataObjects = "objects";

        // Cloud Events
        public const string CLSyncEvent = "event";
        public const string CLClientEventId = "client_reference";
        public const string CLSyncEventStatus = "status";
        public const string CLSyncEvents = "events";
        public const string CLSyncID = "sid";
        public const string CLDefaultSyncID = "0";
        public const string CLSyncEventID = "eid";
        public const string ResponsePendingCount = "pending_count";
        public const string ResponsePartial = "partial_response";
        public const string CLSyncBoxes = "syncboxes";

        // Notification keys
        public const string NotificationMessageBody = "message_body";
        public const string NotificationMessageAuthor = "message_author";

        // Cloud Events Types
        public const string CLEventTypeAdd = "type_add";
        public const string CLEventTypeModify = "type_modify";
        public const string CLEventTypeRenameMove = "type_rename_move";
        public const string CLEventTypeDelete = "type_delete";

        // Cloud Notification Types
        public const string CLNotificationTypeNew = "new";
        public const string CLNotificationTypeUpgrade = "upgrade";
        public const string CLNotificationTypeShare = "share";

        // Sync dictionaries
        public const string CLEventKey = "event_id";
        public const string CLEventCount = "event_count";

        // Invalid SID or EID
        public const long CLDoNotSaveId = 1608198229012012;   // used with SyncTo and SyncFrom to represent sid and eid

        // Folder and file names
        public const string kTempDownloadFolderName = "DownloadTemp";   // the folder to hold temporary downloaded files before moving them to their permanent location.
        public const string kSyncDatabaseFileName = "IndexDB.sdf";
        public const int kMaxTraceFilenameExtLength = 60;               // maximum length of trace filenames (including the extension).

        // REST Response Status "status"
        public const string RESTResponseStatus = "status";
        public const string RESTResponseStatusSuccess = "success";
        public const string RESTResponseStatusFailed = "error";

        public const string RESTResponseMessage = "message";

        // REST Response SyncBox
        public const string RESTResponseSyncBox = "sync_box";
        public const string RESTResponseSyncBoxes = "sync_boxes";
        public const string RESTResponseSyncBoxId = "sync_box_id";
        public const string RESTResponseSyncBoxStorageQuota = "storage_quota";
        public const string RESTResponseSyncBoxCreatedAt = "created_at";
        public const string RESTResponseSyncBoxFriendlyName = "friendly_name";
        public const string RESTResponseSyncBoxMetadata = "metadata";

        //// Old definitions used by the full client.
        ////
        //// Registration
        //public const string CLRegistrationCreateRequestURLString = HttpPrefix + "auth.cliff.cloudburrito.com/user/create.json";
        //public const string CLRegistrationCreateRequestBodyString = "{{\"user\":{{\"first_name\":{0},\"last_name\":{1},\"email\":{2},\"password\":{3}}}," +
        //                                                            "\"device\":{{\"friendly_name\":{4},\"device_uuid\":{5},\"os_type\":{6},\"os_platform\":{7}," +
        //                                                            "\"os_version\":{8},\"app_version\":{9}}},\"client_id\":{10},\"client_secret\":{11}}}";

        //// Link/Unlink
        //public const string CLRegistrationUnlinkRequestURLString = HttpPrefix + "auth.cliff.cloudburrito.com/device/unlink.json";
        //public const string CLRegistrationUnlinkRequestBodyString = CLRegistrationAccessTokenKey + "={0}";

        //public const string CLRegistrationLinkRequestURLString = HttpPrefix + "auth.cliff.cloudburrito.com/device/link.json";
        //public const string CLRegistrationLinkRequestBodyString = "{{\"email\":{0},\"password\":{1},\"device\":{{\"friendly_name\":{2},\"device_uuid\":{3}," +
        //                                                             "\"os_type\":{4},\"os_platform\":{5},\"os_version\":{6},\"app_version\":{7}}}," +
        //                                                             "\"client_id\":{8},\"client_secret\":{9}}}";
    }
}