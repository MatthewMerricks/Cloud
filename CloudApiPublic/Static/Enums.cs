//
// Enums.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Static
{
    /// <summary>
    /// Type of domain for an error
    /// </summary>
    public enum CLExceptionDomain : uint // 32-bit unsigned integer
    {
        General = 0,
        Http = 1,
        Syncbox = 2,
        ShellExt = 3,
        Syncing = 4,
        OnDemand = 5,
        FileItem = 6,
        Credentials = 7,
    }

    /// <summary>
    /// Type of specific error code, grouped by domain
    /// </summary>
    public enum CLExceptionCode : ulong // 64-bit unsigned integer, 32 bits for the domain and 32 bits for the code itself
    {
        #region General

        // 0_0 removed, it was success, but we shouldn't be returning a CLError if there is nothing but successes

        /// <summary>
        /// General error
        /// </summary>
        General_Miscellaneous = (((ulong)CLExceptionDomain.General) << 32) | 1, // 0_1

        /// <summary>
        /// Invalid arguments
        /// </summary>
        General_Arguments = (((ulong)CLExceptionDomain.General) << 32) | 2, // 0_2

        /// <summary>
        /// Operation is not valid
        /// </summary>
        General_Invalid = (((ulong)CLExceptionDomain.General) << 32) | 3, // 0_3

        /// <summary>
        /// An exception occurred.  Inspect CLError.FirstException.
        /// </summary>
        General_SeeFirstException = (((ulong)CLExceptionDomain.General) << 32) | 4, // 0_4

        /// <summary>
        /// An object was not the expected type.
        /// </summary>
        General_ObjectNotExpectedType = (((ulong)CLExceptionDomain.General) << 32) | 5, // 0_5

        #endregion

        #region Http

        /// <summary>
        /// Method invoked a not found (404) response from the server
        /// </summary>
        Http_NotFound = (((ulong)CLExceptionDomain.Http) << 32) | 0, // 1_0

        /// <summary>
        /// Method invoked a server error (5xx) response from the server
        /// </summary>
        Http_ServerError = (((ulong)CLExceptionDomain.Http) << 32) | 1, // 1_1

        /// <summary>
        /// Method had some other problem with parameters processed locally or parameters sent up to the server
        /// </summary>
        Http_BadRequest = (((ulong)CLExceptionDomain.Http) << 32) | 2, // 1_2

        /// <summary>
        /// Method was cancelled via a provided cancellation token before completion
        /// </summary>
        Http_Cancelled = (((ulong)CLExceptionDomain.Http) << 32) | 3, // 1_3

        /// <summary>
        /// Method completed without error but has no response; it means that no data exists for given parameter(s)
        /// </summary>
        Http_NoContent = (((ulong)CLExceptionDomain.Http) << 32) | 4, // 1_4

        /// <summary>
        /// Method invoked an unauthorized (401) response from the server
        /// </summary>
        Http_NotAuthorized = (((ulong)CLExceptionDomain.Http) << 32) | 5, // 1_5

        /// <summary>
        /// Method invoked an unauthorized (401) response from the server, specifically due to expired session credentials
        /// </summary>
        Http_NotAuthorizedExpiredCredentials = (((ulong)CLExceptionDomain.Http) << 32) | 6, // 1_6

        /// <summary>
        /// Method invoked a storage quota exceeded (507) response from the server
        /// </summary>
        Http_QuotaExceeded = (((ulong)CLExceptionDomain.Http) << 32) | 7, // 1_7

        /// <summary>
        /// Unable to establish connection (possible local internet connection error or server is otherwise unreachable)
        /// </summary>
        Http_ConnectionFailed = (((ulong)CLExceptionDomain.Http) << 32) | 8, // 1_8

        /// <summary>
        /// The current Syncbox is actively syncing so cannot make modifications
        /// </summary>
        Http_ReservedForActiveSync = (((ulong)CLExceptionDomain.Http) << 32) | 9, // 1_9

        /// <summary>
        /// Error receiving response; communication attempted but did not complete
        /// </summary>
        Http_NoResponse = (((ulong)CLExceptionDomain.Http) << 32) | 10, // 1_10

        /// <summary>
        /// Internal completion routine failed
        /// </summary>
        Http_CompletionFailure = (((ulong)CLExceptionDomain.Http) << 32) | 11, // 1_11

        /// <summary>
        /// (Internal)Content-Length response header expected.
        /// </summary>
        Http_NoContentLengthResponseHeader = (((ulong)CLExceptionDomain.Http) << 32) | 12, // 1_12

        /// <summary>
        /// Credentials validated, but syncbox does not exist for the provided id
        /// </summary>
        Http_NotAuthorizedSyncboxNotFound = (((ulong)CLExceptionDomain.Http) << 32) | 13, // 1_13

        #endregion

        #region Syncbox

        /// <summary>
        /// Error creating Http REST client
        /// </summary>
        Syncbox_CreateRestClient = (((ulong)CLExceptionDomain.Syncbox) << 32) | 0, // 2_0

        /// <summary>
        /// Directory required since trace is enabled
        /// </summary>
        Syncbox_TraceEnabledWithoutDirectory = (((ulong)CLExceptionDomain.Syncbox) << 32) | 1, // 2_1

        /// <summary>
        /// Syncbox path cannot contain path to database
        /// </summary>
        Syncbox_DatabaseInsideSyncboxPath = (((ulong)CLExceptionDomain.Syncbox) << 32) | 2, // 2_2

        /// <summary>
        /// Syncbox path cannot contain path for trace
        /// </summary>
        Syncbox_TraceInsideSyncboxPath = (((ulong)CLExceptionDomain.Syncbox) << 32) | 3, // 2_3

        /// <summary>
        /// Syncbox path cannot contain temporary downloads
        /// </summary>
        Syncbox_TempDownloadsInsideSyncboxPath = (((ulong)CLExceptionDomain.Syncbox) << 32) | 4, // 2_4
        
        /// <summary>
        /// DeviceId cannot be null
        /// </summary>
        Syncbox_DeviceId = (((ulong)CLExceptionDomain.Syncbox) << 32) | 5, // 2_5

        /// <summary>
        /// Syncbox path too long; check path first with Cloud.Static.Helpers.CheckSyncboxPathLength
        /// </summary>
        Syncbox_LongPath = (((ulong)CLExceptionDomain.Syncbox) << 32) | 6, // 2_6

        /// <summary>
        /// Syncbox path not valid for syncing; check path first with Cloud.Static.Helpers.CheckForBadPath
        /// </summary>
        Syncbox_BadPath = (((ulong)CLExceptionDomain.Syncbox) << 32) | 7, // 2_7

        /// <summary>
        /// Error retrieving initial Syncbox status from server
        /// </summary>
        Syncbox_InitialStatus = (((ulong)CLExceptionDomain.Syncbox) << 32) | 8, // 2_8

        /// <summary>
        /// Syncbox cannot be null
        /// </summary>
        Syncbox_Null = (((ulong)CLExceptionDomain.Syncbox) << 32) | 9, // 2_9

        /// <summary>
        /// Syncbox already started syncing
        /// </summary>
        Syncbox_AlreadyStarted = (((ulong)CLExceptionDomain.Syncbox) << 32) | 10, // 2_10

        /// <summary>
        /// Syncbox not started syncing
        /// </summary>
        Syncbox_NotStarted = (((ulong)CLExceptionDomain.Syncbox) << 32) | 11, // 2_11

        /// <summary>
        /// Error creating backing index for syncing
        /// </summary>
        Syncbox_IndexCreation = (((ulong)CLExceptionDomain.Syncbox) << 32) | 12, // 2_12

        /// <summary>
        /// Error starting notification service
        /// </summary>
        Syncbox_StartingNotifications = (((ulong)CLExceptionDomain.Syncbox) << 32) | 13, // 2_13

        /// <summary>
        /// Error creating file monitor
        /// </summary>
        Syncbox_FileMonitorCreation = (((ulong)CLExceptionDomain.Syncbox) << 32) | 14, // 2_14

        /// <summary>
        /// Error starting file monitor
        /// </summary>
        Syncbox_StartingFileMonitor = (((ulong)CLExceptionDomain.Syncbox) << 32) | 15, // 2_15

        /// <summary>
        /// Starting initial indexing
        /// </summary>
        Syncbox_StartingInitialIndexing = (((ulong)CLExceptionDomain.Syncbox) << 32) | 16,  // 2_16

        /// <summary>
        /// Syncbox is current being modified
        /// </summary>
        Syncbox_InProcessOfModification = (((ulong)CLExceptionDomain.Syncbox) << 32) | 17,  // 2_17

        /// <summary>
        /// Folder not found at syncbox path
        /// </summary>
        Syncbox_PathNotFound = (((ulong)CLExceptionDomain.Syncbox) << 32) | 18,  // 2_18

        /// <summary>
        /// A general exception occurred starting syncing on a syncbox
        /// </summary>
        Syncbox_GeneralStart = (((ulong)CLExceptionDomain.Syncbox) << 32) | 19,  // 2_19

        /// <summary>
        /// The syncbox path has already been set.
        /// </summary>
        Syncbox_PathAlreadySet = (((ulong)CLExceptionDomain.Syncbox) << 32) | 20,  // 2_20

        /// <summary>
        /// Error initializing the syncbox.
        /// </summary>
        Syncbox_Initializing = (((ulong)CLExceptionDomain.Syncbox) << 32) | 21,  // 2_21

        /// <summary>
        /// Credentials expired. Refire the method with new credentials.
        /// </summary>
        Syncbox_ExpiredCredentials = (((ulong)CLExceptionDomain.Syncbox) << 32) | 22, // 2_22

        /// <summary>
        /// Credentials incorrect. Refire the method with appropriate credentials.
        /// </summary>
        Syncbox_BadCredentials = (((ulong)CLExceptionDomain.Syncbox) << 32) | 23, // 2_23

        /// <summary>
        /// Credentials validated, but syncbox does not exist for provided id.
        /// </summary>
        Syncbox_NotFoundForId = (((ulong)CLExceptionDomain.Syncbox) << 32) | 24, // 2_24

        /// <summary>
        /// Storage quota should have been queried from the server and set at least once, including upon initialization.
        /// </summary>
        Syncbox_StorageQuotaUnknown = (((ulong)CLExceptionDomain.Syncbox) << 32) | 25, // 2_25

        /// <summary>
        /// Error subscribing to live sync status messages.  Bad liveSyncStatusReceiver to CLSyncbox.AllocAndInit, or bad DeviceId in settings
        /// </summary>
        Syncbox_SubscribingToLiveSyncStatusReceiver = (((ulong)CLExceptionDomain.Syncbox) << 32) | 26, // 2_26

        #endregion

        #region ShellExt

        /// <summary>
        /// Error initializing the connection to BadgeCOM, likely not registered
        /// </summary>
        ShellExt_ExtensionInitialize = (((ulong)CLExceptionDomain.ShellExt) << 32) | 0, // 3_0

        /// <summary>
        /// Error creating badging dictionary
        /// </summary>
        ShellExt_CreateBadgingDictionary = (((ulong)CLExceptionDomain.ShellExt) << 32) | 1, // 3_1

        #endregion

        #region Syncing

        /// <summary>
        /// Database access error
        /// </summary>
        Syncing_Database = (((ulong)CLExceptionDomain.Syncing) << 32) | 0, // 4_0

        /// <summary>
        /// Live sync engine error
        /// </summary>
        Syncing_LiveSyncEngine = (((ulong)CLExceptionDomain.Syncing) << 32) | 1, // 4_1

        /// <summary>
        /// Live sync index error
        /// </summary>
        Syncing_LiveSyncIndex = (((ulong)CLExceptionDomain.Syncing) << 32) | 2, // 4_2

        /// <summary>
        /// Error in a syncing model class
        /// </summary>
        Syncing_Model = (((ulong)CLExceptionDomain.Syncing) << 32) | 3, // 4_3

        /// <summary>
        /// Error in file monitor
        /// </summary>
        Syncing_FileMonitor = (((ulong)CLExceptionDomain.Syncing) << 32) | 4, // 4_4

        #endregion

        #region OnDemand

        /// <summary>
        /// Cloud syncbox file rename error
        /// </summary>
        OnDemand_FileRename = (((ulong)CLExceptionDomain.OnDemand) << 32) | 0, // 5_0

        /// <summary>
        /// Cloud syncbox file rename invalid metadata error
        /// </summary>
        OnDemand_RenameInvalidMetadata = (((ulong)CLExceptionDomain.OnDemand) << 32) | 1, // 5_1

        /// <summary>
        /// Cloud syncbox file rename.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FileRenameNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 2, // 5_2

        /// <summary>
        /// Cloud syncbox file add error
        /// </summary>
        OnDemand_FileAdd = (((ulong)CLExceptionDomain.OnDemand) << 32) | 3, // 5_3

        /// <summary>
        /// Cloud syncbox file add invalid metadata error
        /// </summary>
        OnDemand_FileAddInvalidMetadata = (((ulong)CLExceptionDomain.OnDemand) << 32) | 4, // 5_4

        /// <summary>
        /// Cloud syncbox file add.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FileAddNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 5, // 5_5

        /// <summary>
        /// Cloud syncbox file delete error
        /// </summary>
        OnDemand_FileDelete = (((ulong)CLExceptionDomain.OnDemand) << 32) | 6, // 5_6

        /// <summary>
        /// Cloud syncbox file delete invalid metadata error
        /// </summary>
        OnDemand_FileDeleteInvalidMetadata = (((ulong)CLExceptionDomain.OnDemand) << 32) | 7, // 5_7

        /// <summary>
        /// Cloud syncbox file delete.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FileDeleteNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 8, // 5_8

        /// <summary>
        /// Cloud syncbox file move.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FileMoveNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 9, // 5_9

        /// <summary>
        /// Cloud syncbox folder rename error
        /// </summary>
        OnDemand_FolderRename = (((ulong)CLExceptionDomain.OnDemand) << 32) | 10, // 5_10

        /// <summary>
        /// Cloud syncbox folder rename invalid metadata error
        /// </summary>
        OnDemand_FolderRenameInvalidMetadata = (((ulong)CLExceptionDomain.OnDemand) << 32) | 11, // 5_11

        /// <summary>
        /// Cloud syncbox folder rename.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FolderRenameNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 12, // 5_12

        /// <summary>
        /// Cloud syncbox folder add error
        /// </summary>
        OnDemand_FolderAdd = (((ulong)CLExceptionDomain.OnDemand) << 32) | 13, // 5_13

        /// <summary>
        /// Cloud syncbox folder add invalid metadata error
        /// </summary>
        OnDemand_FolderAddInvalidMetadata = (((ulong)CLExceptionDomain.OnDemand) << 32) | 14, // 5_14

        /// <summary>
        /// Cloud syncbox folder add.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FolderAddNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 15, // 5_15

        /// <summary>
        /// Cloud syncbox folder delete error
        /// </summary>
        OnDemand_FolderDelete = (((ulong)CLExceptionDomain.OnDemand) << 32) | 16, // 5_16

        /// <summary>
        /// Cloud syncbox folder delete invalid metadata error
        /// </summary>
        OnDemand_FolderDeleteInvalidMetadata = (((ulong)CLExceptionDomain.OnDemand) << 32) | 17, // 5_17

        /// <summary>
        /// Cloud syncbox folder delete.  Server returned no status and no responses.
        /// </summary>
        OnDemand_FolderDeleteNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 18, // 5_18

        /// <summary>
        /// Cloud syncbox folder move.  Server returned no status and no responses.
        /// </summary>
        OnDemand_MoveNoServerResponsesOrErrors = (((ulong)CLExceptionDomain.OnDemand) << 32) | 19, // 5_19

        /// <summary>
        /// Cloud syncbox renames. Parameters are missing.
        /// </summary>
        OnDemand_RenameMissingParameters = (((ulong)CLExceptionDomain.OnDemand) << 32) | 20, // 5_20

        /// <summary>
        /// Cloud syncbox general. Settings Http milliseconds timeout cannot be too small nor negative.
        /// </summary>
        OnDemand_TimeoutMilliseconds = (((ulong)CLExceptionDomain.OnDemand) << 32) | 21, // 5_21

        /// <summary>
        /// Cloud syncbox renames. The existing path is invalid in the CLFileItem to be renamed.
        /// </summary>
        OnDemand_InvalidExistingPath = (((ulong)CLExceptionDomain.OnDemand) << 32) | 22, // 5_22

        /// <summary>
        /// Cloud syncbox renames. New name is required and it must be valid within the syncbox path.
        /// </summary>
        OnDemand_RenameNewName = (((ulong)CLExceptionDomain.OnDemand) << 32) | 23, // 5_23

        /// <summary>
        /// Cloud syncbox response item missing a field.
        /// </summary>
        OnDemand_MissingResponseField = (((ulong)CLExceptionDomain.OnDemand) << 32) | 24, // 5_24

        /// <summary>
        /// Item has already been deleted.
        /// </summary>
        OnDemand_AlreadyDeleted = (((ulong)CLExceptionDomain.OnDemand) << 32) | 25, // 5_25

        /// <summary>
        /// Item was not found.
        /// </summary>
        OnDemand_NotFound = (((ulong)CLExceptionDomain.OnDemand) << 32) | 26,  // 5_26

        /// <summary>
        /// Change conflicts with previous state.
        /// </summary>
        OnDemand_Conflict = (((ulong)CLExceptionDomain.OnDemand) << 32) | 27,  // 5_27

        /// <summary>
        /// An error occurred for the current item, check the InnerException Message.
        /// </summary>
        OnDemand_ItemError = (((ulong)CLExceptionDomain.OnDemand) << 32) | 28, // 5_28

        /// <summary>
        /// An unknown status was returned for an item, check the InnerException Message.
        /// </summary>
        OnDemand_UnknownItemStatus = (((ulong)CLExceptionDomain.OnDemand) << 32) | 29, // 5_29

        /// <summary>
        /// Cloud syncbox file delete bad path
        /// </summary>
        OnDemand_FileDeleteBadPath = (((ulong)CLExceptionDomain.OnDemand) << 32) | 30, // 5_30

        /// <summary>
        /// Cloud syncbox folder delete bad path
        /// </summary>
        OnDemand_FolderDeleteBadPath = (((ulong)CLExceptionDomain.OnDemand) << 32) | 31, // 5_31

        /// <summary>
        /// Cloud syncbox file add bad path
        /// </summary>
        OnDemand_FileAddBadPath = (((ulong)CLExceptionDomain.OnDemand) << 32) | 32, // 5_32

        /// <summary>
        /// Cloud syncbox folder add bad path
        /// </summary>
        OnDemand_FolderAddBadPath = (((ulong)CLExceptionDomain.OnDemand) << 32) | 33, // 5_33

        /// <summary>
        /// Cloud syncbox move parameters missing properties
        /// </summary>
        OnDemand_MoveItemParamsMissingProperties = (((ulong)CLExceptionDomain.OnDemand) << 32) | 34, // 5_34

        /// <summary>
        /// Cloud syncbox moves. The new full path of the renamed item must be valid within the syncbox path.
        /// </summary>
        OnDemand_MovedItemBadPath = (((ulong)CLExceptionDomain.OnDemand) << 32) | 23, // 5_23

        /// <summary>
        /// Cloud On Demand API.  Missing parameters.
        /// </summary>
        OnDemand_MissingParameters = (((ulong)CLExceptionDomain.OnDemand) << 32) | 24, // 5_24

        /// <summary>
        /// Cloud On Demand API.  The item already exists.
        /// </summary>
        OnDemand_AlreadyExists = (((ulong)CLExceptionDomain.OnDemand) << 32) | 25, // 5_25

        /// <summary>
        /// Cloud On Demand API.  Error in AddFolders.
        /// </summary>
        OnDemand_AddFolders = (((ulong)CLExceptionDomain.OnDemand) << 32) | 26, // 5_26

        /// <summary>
        /// Cloud On Demand API. Invalid parameter or parameters.
        /// </summary>
        OnDemand_InvalidParameters = (((ulong)CLExceptionDomain.OnDemand) << 32) | 27, // 5_27

        /// <summary>
        /// Cloud On Demand API. Delete syncbox.
        /// </summary>
        OnDemand_DeleteSyncbox = (((ulong)CLExceptionDomain.OnDemand) << 32) | 28, // 5_28

        /// <summary>
        /// File not found on disk in order to upload.
        /// </summary>
        OnDemand_FileAddNotFound = (((ulong)CLExceptionDomain.OnDemand) << 32) | 29, // 5_29

        /// <summary>
        /// A newer version of the file already exists and is available for download.
        /// </summary>
        OnDemand_NewerVersionAvailableForDownload = (((ulong)CLExceptionDomain.OnDemand) << 32) | 30, // 5_30

        /// <summary>
        /// The parent folder was not found
        /// </summary>
        OnDemand_ParentNotFound = (((ulong)CLExceptionDomain.OnDemand) << 32) | 31, // 5_31

        /// <summary>
        /// An error occurred uploading a file.
        /// </summary>
        OnDemand_Upload = (((ulong)CLExceptionDomain.OnDemand) << 32) | 32, // 5_32

        /// <summary>
        /// The server returned an invalid item among multiple items.
        /// </summary>
        OnDemand_ServerReturnedInvalidItem = (((ulong)CLExceptionDomain.OnDemand) << 32) | 33, // 5_33

        /// <summary>
        /// The server response did not contain a session.
        /// </summary>
        OnDemand_ServerResponseNoSession = (((ulong)CLExceptionDomain.OnDemand) << 32) | 34, // 5_34

        /// <summary>
        /// No server response.
        /// </summary>
        OnDemand_NoServerResponse = (((ulong)CLExceptionDomain.OnDemand) << 32) | 35, // 5_35

        /// <summary>
        /// A request is required.
        /// </summary>
        OnDemand_RequestRequired = (((ulong)CLExceptionDomain.OnDemand) << 32) | 36, // 5_36

        /// <summary>
        /// An error occurred downloading a file.
        /// </summary>
        OnDemand_Download = (((ulong)CLExceptionDomain.OnDemand) << 32) | 37, // 5_37

        /// <summary>
        /// An error occurred downloading a file.
        /// </summary>
        OnDemand_DownloadTempDownloadFileNotFoundAfterSuccessfulDownload = (((ulong)CLExceptionDomain.OnDemand) << 32) | 38, // 5_38

        /// <summary>
        /// Live sync is active for this syncbox, and this method would modify the syncbox.
        /// </summary>
        OnDemand_LiveSyncIsActive = (((ulong)CLExceptionDomain.OnDemand) << 32) | 39, // 5_39

        /// <summary>
        /// CLFileItem was not created in this synbox.
        /// </summary>
        OnDemand_NotCreatedInThisSyncbox = (((ulong)CLExceptionDomain.OnDemand) << 32) | 40, // 5_40

        /// <summary>
        /// CLFileItem was a folder when a file type was expected.
        /// </summary>
        OnDemand_FolderItemWhenFileItemExpected = (((ulong)CLExceptionDomain.OnDemand) << 32) | 41, // 5_41

        /// <summary>
        /// CLFileItem was a file when a folder type was expected.
        /// </summary>
        OnDemand_FileItemWhenFolderItemExpected = (((ulong)CLExceptionDomain.OnDemand) << 32) | 42, // 5_42

        #endregion

        #region FileItem

        /// <summary>
        /// CLFileItem requires a CLSyncbox
        /// </summary>
        FileItem_NullSyncbox = (((ulong)CLExceptionDomain.FileItem) << 32) | 0, // 6_0

        /// <summary>
        /// (internal constructor) CLFileItem requires a JsonContracts.SyncboxMetadataResponse
        /// </summary>
        FileItem_NullResponse = (((ulong)CLExceptionDomain.FileItem) << 32) | 1, // 6_1

        /// <summary>
        /// (internal constructor) CLFileItem requires one of the following to not be null: IsFolder, headerAction, or action
        /// </summary>
        FileItem_NullIsFolder = (((ulong)CLExceptionDomain.FileItem) << 32) | 2, // 6_2

        /// <summary>
        /// (internal constructor) Unable to determine whether item is a file or a folder from action
        /// </summary>
        FileItem_UnknownAction = (((ulong)CLExceptionDomain.FileItem) << 32) | 3, // 6_3

        /// <summary>
        /// (internal constructor) CLFileItem requires a FileChange.
        /// </summary>
        FileItem_RequiresFileChange = (((ulong)CLExceptionDomain.FileItem) << 32) | 4, // 6_4

        #endregion

        #region Credentials

        /// <summary>
        /// Credentials: Key is null.
        /// </summary>
        Credentials_NullKey = (((ulong)CLExceptionDomain.Credentials) << 32) | 1, // 7_0

        /// <summary>
        /// Credentials: Secret is null.
        /// </summary>
        Credentials_NullSecret = (((ulong)CLExceptionDomain.Credentials) << 32) | 1, // 7_1

        /// <summary>
        /// Credentials: Not session credentials.
        /// </summary>
        Credentials_NotSessionCredentials = (((ulong)CLExceptionDomain.Credentials) << 32) | 2, // 7_2

        /// <summary>
        /// Credentials: Not session credentials.
        /// </summary>
        Credentials_ExpirationDateMustNotBeNull = (((ulong)CLExceptionDomain.Credentials) << 32) | 3, // 7_3

        #endregion

        // add values in the format (even keep the comma for the last value and add the comment to the end as well, evaluate everything inside of [] brackets):
        // [domain name]_[name of type of error] = (((ulong)CLExceptionDomain.[domain name]) << 32) | [number value of type of error], // [number value of type of domain]_[number value of type of error]
    }

    internal enum AuthenticationErrorType : ulong
    {
        SessionExpired = 30002,
        SyncboxNotFound = 30007
    }

    /// <summary>
    /// POSIX-style permissions as flagged enumeration
    /// </summary>
    [Flags]
    public enum POSIXPermissions : int
    {
        /// <summary>
        /// --- --- ---
        /// </summary>
        NoPermission = 0,
        /// <summary>
        /// --- --- --x
        /// </summary>
        OtherUsersExecute = 1,
        /// <summary>
        /// --- --- -w-
        /// </summary>
        OtherUsersWrite = 1 << 1,
        /// <summary>
        /// --- --- r--
        /// </summary>
        OtherUsersRead = 1 << 2,
        /// <summary>
        /// --- --x ---
        /// </summary>
        GroupExecute = 1 << 3,
        /// <summary>
        /// --- -w- ---
        /// </summary>
        GroupWrite = 1 << 4,
        /// <summary>
        /// --- r-- ---
        /// </summary>
        GroupRead = 1 << 5,
        /// <summary>
        /// --x --- ---
        /// </summary>
        OwnerExecute = 1 << 6,
        /// <summary>
        /// -w- --- ---
        /// </summary>
        OwnerWrite = 1 << 7,
        /// <summary>
        /// r-- --- ---
        /// </summary>
        OwnerRead = 1 << 8,

        /// <summary>
        /// rwx rwx rwx
        /// </summary>
        AllPermissions =
            OtherUsersExecute | OtherUsersWrite | OtherUsersRead
                | GroupExecute | GroupWrite | GroupRead
                | OwnerExecute | OwnerWrite | OwnerRead,

        /// <summary>
        /// r-x r-x r-x
        /// </summary>
        ReadOnlyPermissions =
            OtherUsersExecute | OtherUsersRead
                | GroupExecute | GroupRead
                | OwnerExecute | OwnerRead
    }

    [Flags]
    /// <summary>
    /// Flagged enumeration used to determine running status of FileMonitor;
    /// File watcher may be running, folder watcher may be running, or both/neither
    /// </summary>
    internal enum MonitorRunning : byte
    {
        NotRunning = 0,
        FolderOnlyRunning = 1,
        FileOnlyRunning = 2,
        BothRunning = 3
    }

    /// <summary>
    /// Enumeration to provide information on the returns from starting or stopping the FileMonitor
    /// </summary>
    internal enum MonitorStatus : byte
    {
        Started,
        AlreadyStarted,
        Stopped,
        AlreadyStopped
    }

    /// <summary>
    /// The type of the event message.
    /// </summary>
    public enum EventMessageType : byte
    {
        Informational,
        Error,
        UploadProgress,
        DownloadProgress,
        SuccessfulUploadsIncremented,
        SuccessfulDownloadsIncremented,
        UploadingCountChanged,
        DownloadingCountChanged,
        InternetConnectivityChanged,
        DownloadCompleteChanged,
        StorageQuotaExceededChanged,
    }

    /// <summary>
    /// The type of the error message.
    /// </summary>
    public enum ErrorMessageType : byte
    {
        General = 1,
        HaltSyncboxOnConnectionFailure = 2,
        HaltSyncboxOnAuthenticationFailure = 3,
        HaltAllOfCloudSDK = 4
    }

    [Flags]
    /// <summary>
    /// Flagged enumeration for CLSync's current running state
    /// </summary>
    public enum CLSyncCurrentState : byte
    {
        Idle = 0,
        CommunicatingChanges = 1,
        UploadingFiles = 2,
        DownloadingFiles = 4,
        HaltedOnConnectionFailure = 8,
        HaltedOnExpiredCredentials = 16,
        InternetDisconnected = 32
    }

    /// <summary>
    /// Status of querying index by EventId
    /// </summary>
    public enum FileChangeQueryStatus
    {
        Success,
        ErrorMultipleResults,
        ErrorNotFound,
        ErrorNoIndexer,
        ErrorUnknown,
        ErrorDisposed
    }

    /// <summary>
    /// Enumeration for direction of sync
    /// </summary>
    public enum SyncDirection : byte
    {
        To,
        From
        //¡¡Do not add a third enumeration since this enumeration is set based on a bit value SyncFrom in table Events in the database (which only has two values)!!
    }

    /// <summary>
    /// Enumeration to associate the type of event occurred for a FileChange (mutually exclusive)
    /// </summary>
    public enum FileChangeType : byte
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

    /// <summary>
    /// readonly fields holding constants related to files
    /// </summary>
    internal static class FileConstants
    {
        public const long InvalidUtcTimeTicks = 504911232000000000; //number determined by practice
        public static readonly byte[] EmptyBuffer = new byte[0]; // empty buffer is used to complete an MD5 hash
        public const int BufferSize = 4096; //posts online seem to suggest between 1kb and 12kb is optimal for a FileStream buffer, 4kb seems commonly used
        public const long MaxUploadFileSize = 40L * 1024L * 1024L * 1024L; // -1 if no restrictions
        public const int MaxUploadIntermediateHashBytesSize = 10 * 1024 * 1024; // 10MB; size of intermediate file blocks to hash for verifying the file contents in an optimistic share lock startegy on uploads
    }

    /// <summary>
    /// Importance of event message from 1 to 9 with enumerated defaults (i.e. 1:Minor to 9:Important)
    /// </summary>
    public enum EventMessageLevel : byte
    {
        /// <summary>
        /// Below the lowest importance level, use this as filter to display everything
        /// </summary>
        All = 0,
        /// <summary>
        /// Importance of 1 out of 9
        /// </summary>
        Minor = 1,
        /// <summary>
        /// Importance of 5 out of 9
        /// </summary>
        Regular = 5,
        /// <summary>
        /// Importance of 9 out of 9
        /// </summary>
        Important = 9
    }

    /// <summary>
    /// Describes whether any event handlers were fired for an event and if so, whether any marked that they handled the event in their event args
    /// </summary>
    public enum EventHandledLevel : short
    {
        NothingFired = -1,
        FiredButNotHandled = 0,
        IsHandled = 1
    }

    /// <summary>
    /// Describes how a path should display for badging
    /// </summary>
    internal enum PathState : byte
    {
        None,
        Synced,
        Syncing,
        Failed,
        Selective
    }

    /// <summary>
    /// Types of images to display next to a item in a growl message
    /// </summary>
    public enum EventMessageImage
    {
        /// <summary>
        /// Use nothing or something transparent as the image
        /// </summary>
        NoImage,

        /// <summary>
        /// Use something like an 'i' icon
        /// </summary>
        Informational,

        /// <summary>
        /// Use something like the failed badge icon
        /// </summary>
        Error,

        /// <summary>
        /// Use something like the syncing badge icon
        /// </summary>
        Busy,

        /// <summary>
        /// Use something like the synced badge icon
        /// </summary>
        Completion,

        /// <summary>
        /// Use something like the selective badge icon
        /// </summary>
        Inaction
    }

    /// <summary>
    /// Used to determine what an individual thread will do when handling token expired errors in Helpers.processHttp.
    /// </summary>
    internal enum EnumRequestNewCredentialsStates : byte
    {
        RequestNewCredentials_NotSet = 0,
        RequestNewCredentials_BubbleResult,
        RequestNewCredentials_Retry,
    }

    /// <summary>
    /// Used as a parameter to CLSyncbox.StartLiveSync.  These values describe how the syncbox will be synced:
    ///   - CLSyncModeLive: All files, folders and metadata will be synced all the time (live).
    ///   - CLSyncModeLiveWithBadgingEnabled: As above, with shell extension (badging).
    /// </summary>
    public enum CLSyncMode : int
    {
        CLSyncModeLive,
        CLSyncModeLiveWithBadgingEnabled
    }

    /// <summary>
    /// Event Status provided by the syncbox:
    ///   - CLSyncboxStatusSyncingBegan: The syncbox was started.
    ///   - CLSyncboxStatusSyncingPaused: The syncbox was paused.
    ///   - CLSyncboxStatusSyncing: The syncbox is syncing.
    ///   - CLSyncboxStatusSyncingEnded: The syncbox was stopped.
    ///   
    /// </summary>
    public enum CLSyncboxStatus : int
    {
        CLSyncboxStatusSyncingBegan,
        CLSyncboxStatusSyncingPaused,
        CLSyncboxStatusSyncing,
        CLSyncboxStatusSyncingEnded
    }

}
