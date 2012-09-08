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

namespace SQLIndexer.SqlModel
{
    [SqlAccess("Syncs")]
    public class Sync
    {
        [SqlAccess]
        public string SyncId { get; set; }

        [SqlAccess]
        public long SyncCounter { get; set; }

        [SqlAccess]
        public string RootPath { get; set; }

        [SqlAccess(true)]
        public SyncState SyncState { get; set; }

        [SqlAccess(true)]
        public Event Event { get; set; }
    }
}