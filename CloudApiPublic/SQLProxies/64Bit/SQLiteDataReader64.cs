//
// SQLiteDataReader64.cs
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
    extern alias SQL64;

    /// <summary>
    /// SQLite implementation of DbDataReader.
    /// </summary>
    internal sealed class SQLiteDataReader64 : DisposableProxyObject, ISQLiteDataReader
    {
        private readonly SQL64.System.Data.SQLite.SQLiteDataReader baseObject;

        protected override IDisposable BaseDisposable
        {
            get
            {
                return baseObject;
            }
        }

        internal SQLiteDataReader64(SQL64.System.Data.SQLite.SQLiteDataReader baseObject)
        {
            this.baseObject = baseObject;
        }

        #region ISQLiteDataReader members
        /// <summary>
        /// Indexer to retrieve data from a column given its name
        /// </summary>
        /// <param name="name">The name of the column to retrieve data for</param>
        /// <returns>The value contained in the column</returns>
        object ISQLiteDataReader.this[string name]
        {
            get
            {
                base.CheckDisposed();
                try
                {
                    return baseObject[name];
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
            }
        }

        /// <summary>
        /// Returns True if the specified column is null
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>True or False</returns>
        bool ISQLiteDataReader.IsDBNull(int i)
        {
            base.CheckDisposed();
            try
            {
                return baseObject.IsDBNull(i);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Retrieves the i of a column, given its name
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Retrieves the i of a column, given its name</returns>
        int ISQLiteDataReader.GetOrdinal(string name)
        {
            base.CheckDisposed();
            try
            {
                return baseObject.GetOrdinal(name);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Reads the next row from the resultset
        /// </summary>
        /// <returns>True if a new row was successfully loaded and is ready for processing</returns>
        bool ISQLiteDataReader.Read()
        {
            base.CheckDisposed();
            try
            {
                return baseObject.Read();
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }
        #endregion
    }
}