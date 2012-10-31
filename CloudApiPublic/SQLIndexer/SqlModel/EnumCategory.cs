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

namespace CloudApiPublic.SQLIndexer.SqlModel
{
    [SqlAccess.Class("EnumCategories")]
    public class EnumCategory
    {
        [SqlAccess.Property]
        public int EnumCategoryId { get; set; }

        [SqlAccess.Property]
        public string Name { get; set; }

        [SqlAccess.Property(Constants.SqlEnumName, true)]
        public SqlEnum SqlEnum { get; set; }

        [SqlAccess.Property(true)]
        public Event Event { get; set; }
    }
}