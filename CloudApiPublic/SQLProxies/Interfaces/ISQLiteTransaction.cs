//
// ISQLiteTransaction.cs
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
    // \cond
    /// <summary>
    /// SQLite implementation of DbTransaction.
    /// </summary>
    public interface ISQLiteTransaction : IDisposable
    {
        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rolls back the active transaction.
        /// </summary>
        void Rollback();
    }
    // \endcond
}