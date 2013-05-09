//
// EnumCategory.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.SQLIndexer.SqlModel
{
    [SqlAccess.Class(CLDefinitions.EnumCategory_EnumCategories)]
    internal class EnumCategory
    {
        [SqlAccess.Property]
        public long EnumCategoryId { get; set; }

        [SqlAccess.Property]
        public string Name { get; set; }

        [SqlAccess.Property(Constants.SqlEnumName, SqlAccess.FieldType.JoinedTable)]
        public SqlEnum SqlEnum { get; set; }
    }
}