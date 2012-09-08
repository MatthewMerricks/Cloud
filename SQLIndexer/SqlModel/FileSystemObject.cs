//
// FileSystemObject.cs
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
    [SqlAccess("FileSystemObjects")]
    public class FileSystemObject
    {
        public static readonly string Name = (typeof(FileSystemObject)).Name;

        [SqlAccess]
        public long FileSystemObjectId { get; set; }

        [SqlAccess]
        public string Path { get; set; }

        [SqlAccess]
        public Nullable<DateTime> LastTime { get; set; }

        [SqlAccess]
        public Nullable<DateTime> CreationTime { get; set; }

        [SqlAccess]
        public bool IsFolder { get; set; }

        [SqlAccess]
        public Nullable<long> Size { get; set; }

        [SqlAccess]
        public string TargetPath { get; set; }

        [SqlAccess]
        public int PathChecksum { get; set; }

        [SqlAccess]
        public string Revision { get; set; }

        [SqlAccess]
        public bool RevisionIsNull { get; set; }

        [SqlAccess]
        public string StorageKey { get; set; }

        [SqlAccess(true)]
        public Event Event { get; set; }

        [SqlAccess(true)]
        public SyncState SyncState { get; set; }

        [SqlAccess(true)]
        public SyncState ServerLinkedSyncState { get; set; }
    }
}