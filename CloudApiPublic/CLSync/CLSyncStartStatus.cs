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
    public enum CLSyncStartStatus : byte
    {
        ErrorUnknown,
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
        Successful
    }
}