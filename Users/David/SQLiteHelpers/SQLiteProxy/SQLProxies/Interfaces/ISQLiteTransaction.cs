using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite implementation of DbTransaction.
    /// </summary>
    internal interface ISQLiteTransaction : IDisposable
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
}