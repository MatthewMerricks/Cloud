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
using System.Reflection;

namespace Cloud.SQLIndexer.SqlModel
{
    // \cond
    [Obfuscation(Exclude = true)]
    [SqlAccess.Class(CLDefinitions.SqlModel_Sync)]
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