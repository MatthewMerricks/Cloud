﻿//
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
    internal enum AuthenticationErrorType : ulong
    {
        SessionExpired = 30002
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

    public enum EventMessageType : byte
    {
        Informational,
        Error,
        UploadProgress,
        DownloadProgress,
        SuccessfulUploadsIncremented,
        SuccessfulDownloadsIncremented,
        UploadingCountChanged,
        DownloadingCountChanged
    }

    public enum ErrorMessageType : byte
    {
        General = 1,
        HaltSyncEngineOnConnectionFailure = 2,
        HaltSyncEngineOnAuthenticationFailure = 3,
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
        HaltedOnExpiredCredentials = 16
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
        ErrorUnknown
    }

    /// <summary>
    /// Status from a call to one of the CLHttpRest communications methods
    /// </summary>
    public enum CLHttpRestStatus : byte
    {
        /// <summary>
        /// Method completed without error and has a normal response
        /// </summary>
        Success,
        /// <summary>
        /// Method invoked a not found (404) response from the server
        /// </summary>
        NotFound,
        /// <summary>
        /// Method invoked a server error (5xx) response from the server
        /// </summary>
        ServerError,
        /// <summary>
        /// Method had some other problem with parameters processed locally or parameters sent up to the server
        /// </summary>
        BadRequest,
        /// <summary>
        /// Method was cancelled via a provided cancellation token before completion
        /// </summary>
        Cancelled,
        /// <summary>
        /// Method completed without error but has no response; it means that no data exists for given parameter(s)
        /// </summary>
        NoContent,
        /// <summary>
        /// Method invoked an unauthorized (401) response from the server
        /// </summary>
        NotAuthorized,
        /// <summary>
        /// Method invoked an unauthorized (401) response from the server, specifically due to expired session credentials
        /// </summary>
        NotAuthorizedExpiredCredentials,
        /// <summary>
        /// Method invoked a storage quota exceeded (507) response from the server
        /// </summary>
        QuotaExceeded,
        /// <summary>
        /// Unable to establish connection (possible local internet connection error or server is otherwise unreachable)
        /// </summary>
        ConnectionFailed,
        /// <summary>
        /// The current SyncBox is actively syncing so cannot make modifications
        /// </summary>
        ReservedForActiveSync
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
    internal enum EnumRequestNewCredentialStates : byte
    {
        RequestNewCredential_NotSet = 0,
        RequestNewCredential_BubbleResult,
        RequestNewCredential_Retry,
    }
}