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
    [SqlAccess.Class("Events")]
    public class Event
    {
        [SqlAccess.Property]
        public long EventId { get; set; }

        [SqlAccess.Property]
        public int FileChangeTypeCategoryId { get; set; }

        [SqlAccess.Property]
        public int FileChangeTypeEnumId { get; set; }

        [SqlAccess.Property]
        public string PreviousPath { get; set; }

        [SqlAccess.Property]
        public bool SyncFrom { get; set; }

        [SqlAccess.Property]
        public Nullable<Guid> GroupId { get; set; }

        [SqlAccess.Property]
        public Nullable<int> GroupOrder { get; set; }

        [SqlAccess.Property(true)]
        public EnumCategory EnumCategory { get; set; }

        [SqlAccess.Property(Constants.SqlEnumName, true)]
        public SqlEnum SqlEnum { get; set; }

        [SqlAccess.Property(true)]
        public FileSystemObject FileSystemObject { get; set; }
    }
}