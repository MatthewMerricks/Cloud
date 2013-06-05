//
// Sync.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace Cloud.SQLIndexer.SqlModel
{
    // \cond
    [SqlAccess.Class(CLDefinitions.Sync_Syncs)]
    public sealed class Sync
    {
        [SqlAccess.Property]
        public long SyncCounter { get; set; }

        [SqlAccess.Property]
        public string SID { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public FileSystemObject FileSystemObject { get; set; }
    }
    // \endcond
}