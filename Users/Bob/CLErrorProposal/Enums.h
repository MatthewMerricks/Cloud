format for UInt64 values of CLExceptionCode: left 32 bits for domain and right 32 bits for specific code
[name of domain]_[name of value] = (((ulong)CLExceptionDomain.[name of domain]) << 32) | [specific code value]

//
//  Enums.h
//  Cloud.com SDK
//
//  Created by David Bruck on 4/19/2013
//  Copyright © Cloud.com. All rights reserved.
//

/// <summary>
/// Type of domain for an error
/// </summary>
enum CLExceptionDomain inherits UInt32
	Values
      - General = 0,
      - Http = 1,
      - Syncbox = 2,
      - ShellExt = 3,
      - Syncing = 4,
      - REST = 5

/// <summary>
/// Type of specific error code, grouped by domain
/// </summary>
enum CLExceptionCode inherits UInt64
	Values
        #region General

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
        /// SyncRoot cannot contain path to database
        /// </summary>
        Syncbox_DatabaseInsideSyncRoot = (((ulong)CLExceptionDomain.Syncbox) << 32) | 2, // 2_2

        /// <summary>
        /// SyncRoot cannot contain path for trace
        /// </summary>
        Syncbox_TraceInsideSyncRoot = (((ulong)CLExceptionDomain.Syncbox) << 32) | 3, // 2_3

        /// <summary>
        /// SyncRoot cannot contain temporary downloads
        /// </summary>
        Syncbox_TempDownloadsInsideSyncRoot = (((ulong)CLExceptionDomain.Syncbox) << 32) | 4, // 2_4
        
        /// <summary>
        /// DeviceId cannot be null
        /// </summary>
        Syncbox_DeviceId = (((ulong)CLExceptionDomain.Syncbox) << 32) | 5, // 2_5

        /// <summary>
        /// SyncRoot too long; check path first with Cloud.Static.Helpers.CheckSyncboxPathLength
        /// </summary>
        Syncbox_LongSyncRoot = (((ulong)CLExceptionDomain.Syncbox) << 32) | 6, // 2_6

        /// <summary>
        /// SyncRoot not valid for syncing; check path first with Cloud.Static.Helpers.CheckForBadPath
        /// </summary>
        Syncbox_BadSyncRoot = (((ulong)CLExceptionDomain.Syncbox) << 32) | 7, // 2_7

        /// <summary>
        /// Error retrieving initial Syncbox status from server
        /// </summary>
        Syncbox_InitialStatus = (((ulong)CLExceptionDomain.Syncing) << 32) | 8, // 2_8

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

        #endregion

