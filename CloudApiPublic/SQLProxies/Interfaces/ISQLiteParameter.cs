//
// ISQLiteParameter.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite implementation of DbParameter.
    /// </summary>
    internal interface ISQLiteParameter
    {
        /// <summary>
        /// Gets and sets the parameter value. If no datatype was specified, the datatype
        /// will assume the type from the value given.
        /// </summary>
        object Value { get; set; }
    }
}