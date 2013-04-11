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
        [SqlAccess.Property]
        public long FileSystemObjectId { get; set; }

        [SqlAccess.Property]
        public string Name { get; set; }

        [SqlAccess.Property]
        public Nullable<long> ParentFolderId { get; set; }

        [SqlAccess.Property]
        public Nullable<long> LastTimeUTCTicks { get; set; }

        [SqlAccess.Property]
        public Nullable<long> CreationTimeUTCTicks { get; set; }

        [SqlAccess.Property]
        public bool IsFolder { get; set; }

        [SqlAccess.Property]
        public Nullable<long> Size { get; set; }

        [SqlAccess.Property]
        public string Revision { get; set; }

        [SqlAccess.Property]
        public string StorageKey { get; set; }

        [SqlAccess.Property]
        public string ServerName { get; set; }

        [SqlAccess.Property]
        public Nullable<long> EventId { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.ReadOnly)]
        public Nullable<long> EventOrder { get; set; }

        [SqlAccess.Property]
        public Nullable<bool> IsShare { get; set; }

        [SqlAccess.Property]
        public byte[] MD5 { get; set; }

        [SqlAccess.Property]
        public Nullable<int> Version { get; set; }

        [SqlAccess.Property]
        public string ServerUid { get; set; }
        
        [SqlAccess.Property]
        public bool Pending { get; set; }

        [SqlAccess.Property]
        public Nullable<long> SyncCounter { get; set; }

        [SqlAccess.Property]
        public string MimeType { get; set; }

        [SqlAccess.Property]
        public Nullable<int> Permissions { get; set; }

        [SqlAccess.Property]
        public long EventTimeUTCTicks { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.ReadOnly)]
        public string CalculatedFullPath { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public FileSystemObject Parent { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public FileSystemObject Child { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public Event Event { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public Event ReversePrevious { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public Sync Sync { get; set; }
    }
}