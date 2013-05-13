//
// Event.cs
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
    [SqlAccess.Class("Events")]
    internal sealed class Event
    {
        [SqlAccess.Property]
        public long EventId { get; set; }

        [SqlAccess.Property]
        public long FileChangeTypeCategoryId { get; set; }

        [SqlAccess.Property]
        public long FileChangeTypeEnumId { get; set; }

        [SqlAccess.Property]
        public Nullable<long> PreviousId { get; set; }

        [SqlAccess.Property]
        public bool SyncFrom { get; set; }

        [SqlAccess.Property]
        public Nullable<Guid> GroupId { get; set; }

        [SqlAccess.Property]
        public Nullable<int> GroupOrder { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.ReadOnly)] // read-only since I don't want to pass this string around whenever creating FileChange objects, explicity updated in database
        public string FileDownloadPendingRevision { get; set; }

        [SqlAccess.Property(Constants.SqlEnumName, SqlAccess.FieldType.JoinedTable)]
        public SqlEnum SqlEnum { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public FileSystemObject Previous { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public FileSystemObject FileSystemObject { get; set; }
    }
}