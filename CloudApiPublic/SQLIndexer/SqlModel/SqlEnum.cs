//
// SqlEnum.cs
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
    [SqlAccess.Class(CLDefinitions.SqlEnum_Events)]
    internal sealed class SqlEnum
    {
        [SqlAccess.Property]
        public long EnumId { get; set; }

        [SqlAccess.Property]
        public long EnumCategoryId { get; set; }

        [SqlAccess.Property]
        public string Name { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public EnumCategory EnumCategory { get; set; }

        [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
        public Event Event { get; set; }
    }
}