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

namespace SQLIndexer.SqlModel
{
    [SqlAccess("Events")]
    public class Event
    {
        [SqlAccess]
        public long EventId { get; set; }

        [SqlAccess]
        public Nullable<long> SyncCounter { get; set; }

        [SqlAccess]
        public int FileChangeTypeCategoryId { get; set; }

        [SqlAccess]
        public int FileChangeTypeEnumId { get; set; }

        [SqlAccess]
        public string PreviousPath { get; set; }

        [SqlAccess]
        public long FileSystemObjectId { get; set; }

        [SqlAccess]
        public bool SyncFrom { get; set; }

        [SqlAccess(true)]
        public Sync Sync { get; set; }

        [SqlAccess(true)]
        public EnumCategory EnumCategory { get; set; }

        [SqlAccess(Constants.SqlEnumName, true)]
        public SqlEnum SqlEnum { get; set; }

        [SqlAccess(true)]
        public FileSystemObject FileSystemObject { get; set; }
    }
}