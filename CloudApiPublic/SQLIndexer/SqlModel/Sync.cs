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
    [Obfuscation(Exclude = true)]
    [SqlAccess.Class(CLDefinitions.SqlModel_Sync)]
    internal sealed class Sync
    {
        [SqlAccess.Property]
        public long SyncCounter { get; set; }

        [SqlAccess.Property]
        public string SID { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public Cloud.SQLIndexer.SqlModel.FileSystemObjectHolder.FileSystemObject FileSystemObject { get; set; }
    }
}