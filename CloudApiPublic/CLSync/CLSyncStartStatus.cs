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

namespace CloudApiPublic
{
    /// <summary>
    /// State of starting sync
    /// </summary>
    public enum CLSyncStartStatus : byte
    {
        ErrorNullSyncBox,
        ErrorNullDeviceId,
        ErrorLongRootPath,
        ErrorBadRootPath,
        ErrorAlreadyStarted,
        ErrorIndexCreation,
        ErrorStartingNotification,
        ErrorCreatingRestClient,
        ErrorCreatingFileMonitor,
        ErrorStartingFileMonitor,
        ErrorStartingInitialIndexing,
        ErrorExceptionStartingFileMonitor,
        ErrorGeneralSyncStartException,
        Success
    }
}