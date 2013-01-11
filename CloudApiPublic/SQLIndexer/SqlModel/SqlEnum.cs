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

namespace CloudApiPublic.SQLIndexer.SqlModel
{
    [SqlAccess.Class("Enums")]
    internal class SqlEnum
    {
        [SqlAccess.Property]
        public int EnumId { get; set; }

        [SqlAccess.Property]
        public int EnumCategoryId { get; set; }

        [SqlAccess.Property]
        public string Name { get; set; }

        [SqlAccess.Property(true)]
        public EnumCategory EnumCategory { get; set; }

        [SqlAccess.Property(true)]
        public Event Event { get; set; }
    }
}