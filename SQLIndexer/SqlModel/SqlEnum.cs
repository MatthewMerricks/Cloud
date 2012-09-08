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

namespace SQLIndexer.SqlModel
{
    [SqlAccess("Enums")]
    public class SqlEnum
    {
        [SqlAccess]
        public int EnumId { get; set; }

        [SqlAccess]
        public int EnumCategoryId { get; set; }

        [SqlAccess]
        public string Name { get; set; }

        [SqlAccess(true)]
        public EnumCategory EnumCategory { get; set; }

        [SqlAccess(true)]
        public Event Event { get; set; }
    }
}