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
    [SqlAccess.Class("SyncStates")]
    public class SyncState
    {
        [SqlAccess.Property]
        public long SyncStateId { get; set; }

        [SqlAccess.Property]
        public long SyncCounter { get; set; }

        [SqlAccess.Property]
        public long FileSystemObjectId { get; set; }

        [SqlAccess.Property]
        public Nullable<long> ServerLinkedFileSystemObjectId { get; set; }

        [SqlAccess.Property(true)]
        public Sync Sync { get; set; }

        [SqlAccess.Property(true)]
        public FileSystemObject FileSystemObject { get; set; }

        [SqlAccess.Property(true)]
        public FileSystemObject ServerLinkedFileSystemObject { get; set; }
    }
}