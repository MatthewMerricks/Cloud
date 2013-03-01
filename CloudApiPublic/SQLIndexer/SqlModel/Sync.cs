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

namespace Cloud.SQLIndexer.SqlModel
{
    [SqlAccess.Class("Syncs")]
    internal class Sync
    {
        [SqlAccess.Property]
        public string SyncId { get; set; }

        [SqlAccess.Property]
        public long SyncCounter { get; set; }

        [SqlAccess.Property]
        public string RootPath { get; set; }

        [SqlAccess.Property(true)]
        public FileSystemObject FileSystemObject { get; set; }
    }
}