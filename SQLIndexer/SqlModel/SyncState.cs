//
// SyncState.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLIndexer.SqlModel
{
    [SqlAccess("SyncStates")]
    public class SyncState
    {
        [SqlAccess]
        public long SyncStateId { get; set; }

        [SqlAccess]
        public long SyncCounter { get; set; }

        [SqlAccess]
        public long FileSystemObjectId { get; set; }

        [SqlAccess]
        public Nullable<long> ServerLinkedFileSystemObjectId { get; set; }

        [SqlAccess(true)]
        public Sync Sync { get; set; }

        [SqlAccess(true)]
        public FileSystemObject FileSystemObject { get; set; }

        [SqlAccess(true)]
        public FileSystemObject ServerLinkedFileSystemObject { get; set; }
    }
}