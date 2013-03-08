//
// CLSyncStartStatus.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud
{
    /// <summary>
    /// State of starting sync
    /// </summary>
    public enum CLSyncStartStatus : byte
    {
        Success = 0,
        ErrorNullSyncBox = 1,
        ErrorTraceEnabledWithoutDirectory = 2,
        ErrorDatabaseFolderInsideSyncBoxFolder = 3,
        ErrorTraceFolderInsideSyncBoxFolder = 4,
        ErrorTempDownloadFolderInsideSyncBoxFolder = 5,
        ErrorNullDeviceId = 6,
        ErrorLongRootPath = 7,
        ErrorBadRootPath = 8,
        ErrorAlreadyStarted = 9,
        ErrorIndexCreation = 10,
        ErrorStartingNotification = 11,
        ErrorCreatingFileMonitor = 12,
        ErrorStartingFileMonitor = 13,
        ErrorStartingInitialIndexing = 14,
        ErrorExceptionStartingFileMonitor = 15,
        ErrorGeneralSyncStartException = 16,
        ErrorInProcessOfModification = 17,
    }
}