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

namespace Cloud.SQLIndexer.SqlModel
{
    [SqlAccess.Class("EnumCategories")]
    internal class EnumCategory
    {
        [SqlAccess.Property]
        public long EnumCategoryId { get; set; }

        [SqlAccess.Property]
        public string Name { get; set; }

        [SqlAccess.Property(Constants.SqlEnumName, true)]
        public SqlEnum SqlEnum { get; set; }
    }
}