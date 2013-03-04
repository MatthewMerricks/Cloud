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

namespace Cloud.SQLIndexer.SqlModel
{
    [SqlAccess.Class("FileSystemObjects")]
    internal class FileSystemObject
    {
        public static readonly string Name = (typeof(FileSystemObject)).Name;

        [SqlAccess.Property]
        public long FileSystemObjectId { get; set; }

        [SqlAccess.Property]
        public string Path { get; set; }

        [SqlAccess.Property]
        public Nullable<DateTime> LastTime { get; set; }

        [SqlAccess.Property]
        public Nullable<DateTime> CreationTime { get; set; }

        [SqlAccess.Property]
        public bool IsFolder { get; set; }

        [SqlAccess.Property]
        public Nullable<long> Size { get; set; }

        [SqlAccess.Property]
        public string TargetPath { get; set; }

        [SqlAccess.Property]
        public int PathChecksum { get; set; }

        [SqlAccess.Property]
        public string Revision { get; set; }

        [SqlAccess.Property]
        public string StorageKey { get; set; }

        [SqlAccess.Property]
        public Nullable<long> SyncCounter { get; set; }

        [SqlAccess.Property]
        public bool ServerLinked { get; set; }

        [SqlAccess.Property]
        public Nullable<long> EventId { get; set; }

        [SqlAccess.Property(true)]
        public Event Event { get; set; }

        [SqlAccess.Property(true)]
        public Sync Sync { get; set; }
    }
}