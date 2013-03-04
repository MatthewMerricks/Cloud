//
// IFileResultParent.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLIndexer.Model
{
    internal interface IFileResultParent
    {
        string FullName { get; }
    }
}