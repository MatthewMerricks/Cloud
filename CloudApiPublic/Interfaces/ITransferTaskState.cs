//
// ITransferTaskState.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Interfaces
{
    // interface to allow HttpScheduler to access the SyncBox which started a Task for upload/download in order to fire filterable MessageEvents
    internal interface ITransferTaskState
    {
        CLSyncBox SyncBox { get; }
    }
}