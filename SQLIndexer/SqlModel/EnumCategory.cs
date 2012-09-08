//
// EnumCategory.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLIndexer.SqlModel
{
    [SqlAccess("EnumCategories")]
    public class EnumCategory
    {
        [SqlAccess]
        public int EnumCategoryId { get; set; }

        [SqlAccess]
        public string Name { get; set; }

        [SqlAccess(Constants.SqlEnumName, true)]
        public SqlEnum SqlEnum { get; set; }

        [SqlAccess(true)]
        public Event Event { get; set; }
    }
}