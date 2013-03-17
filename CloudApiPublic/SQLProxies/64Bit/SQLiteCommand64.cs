//
// SQLiteCommand64.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Cloud.SQLProxies
{
    extern alias SQL64;

    /// <summary>
    /// SQLite implementation of DbCommand.
    /// </summary>
    internal sealed class SQLiteCommand64 : DisposableProxyObject, ISQLiteCommand
    {
        private readonly SQL64.System.Data.SQLite.SQLiteCommand baseObject;

        protected override IDisposable BaseDisposable
        {
            get
            {
                return baseObject;
            }
        }

        internal SQLiteCommand64(SQL64.System.Data.SQLite.SQLiteCommand baseObject)
        {
            this.baseObject = baseObject;
        }

        #region ISQLiteCommand members
        /// <summary>
        /// The SQL command text associated with the command
        /// </summary>
        string ISQLiteCommand.CommandText
        {
            get
            {
                base.CheckDisposed();
                try
                {
                    return baseObject.CommandText;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
            }
            set
            {
                base.CheckDisposed();
                try
                {
                    baseObject.CommandText = value;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
            }
        }

        /// <summary>
        /// Execute the command and return the number of rows inserted/updated affected
        /// by it.
        /// </summary>
        int ISQLiteCommand.ExecuteNonQuery()
        {
            base.CheckDisposed();
            try
            {
                return baseObject.ExecuteNonQuery();
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Execute the command and return the first column of the first row of the resultset
        /// (if present), or null if no resultset was returned.
        /// </summary>
        /// <returns>The first column of the first row of the first resultset from the query</returns>
        object ISQLiteCommand.ExecuteScalar()
        {
            base.CheckDisposed();
            try
            {
                return baseObject.ExecuteScalar();
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Overrides the default behavior to return a SQLiteDataReader specialization
        /// class
        /// </summary>
        /// <param name="behavior">The flags to be associated with the reader</param>
        /// <returns>A SQLiteDataReader</returns>
        ISQLiteDataReader ISQLiteCommand.ExecuteReader(CommandBehavior behavior)
        {
            base.CheckDisposed();
            SQL64.System.Data.SQLite.SQLiteDataReader toWrap;
            try
            {
                toWrap = baseObject.ExecuteReader(behavior);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
            if (toWrap == null)
            {
                return null;
            }
            return new SQLiteDataReader64(toWrap);
        }

        /// <summary>
        /// Create a new parameter
        /// </summary>
        ISQLiteParameter ISQLiteCommand.CreateParameter()
        {
            base.CheckDisposed();
            SQL64.System.Data.SQLite.SQLiteParameter toWrap;
            try
            {
                toWrap = baseObject.CreateParameter();
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
            if (toWrap == null)
            {
                return null;
            }
            return new SQLiteParameter64(toWrap);
        }

        /// <summary>
        /// Returns the SQLiteParameterCollection for the given command
        /// </summary>
        ISQLiteParameterCollection ISQLiteCommand.Parameters
        {
            get
            {
                base.CheckDisposed();
                SQL64.System.Data.SQLite.SQLiteParameterCollection toWrap;
                try
                {
                    toWrap = baseObject.Parameters;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
                if (toWrap == null)
                {
                    return null;
                }
                return new SQLiteParameterCollection64(toWrap);
            }
        }

        /// <summary>
        /// The transaction associated with this command. SQLite only supports one transaction
        /// per connection, so this property forwards to the command's underlying connection.
        /// </summary>
        ISQLiteTransaction ISQLiteCommand.Transaction
        {
            get
            {
                base.CheckDisposed();
                SQL64.System.Data.SQLite.SQLiteTransaction toWrap;
                try
                {
                    toWrap = baseObject.Transaction;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
                if (toWrap == null)
                {
                    return null;
                }
                return new SQLiteTransaction64(toWrap);
            }
            set
            {
                base.CheckDisposed();
                SQLiteTransaction64 castValue;
                if (value == null)
                {
                    castValue = null;
                }
                else
                {
                    castValue = value as SQLiteTransaction64;
                    if (castValue == null)
                    {
                        throw new ArgumentException("value not castable as SQLiteTransaction64");
                    }
                }
                try
                {
                    baseObject.Transaction = (SQL64.System.Data.SQLite.SQLiteTransaction)castValue;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
            }
        }
        #endregion
    }
}