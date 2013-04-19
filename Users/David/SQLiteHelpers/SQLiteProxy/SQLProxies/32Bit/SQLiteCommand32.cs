using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Cloud.SQLProxies
{
    extern alias SQL32;

    /// <summary>
    /// SQLite implementation of DbCommand.
    /// </summary>
    internal sealed class SQLiteCommand32 : DisposableProxyObject, ISQLiteCommand
    {
        private readonly SQL32.System.Data.SQLite.SQLiteCommand baseObject;

        protected override IDisposable BaseDisposable
        {
            get
            {
                return baseObject;
            }
        }

        internal SQLiteCommand32(SQL32.System.Data.SQLite.SQLiteCommand baseObject)
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
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
                }
            }
            set
            {
                base.CheckDisposed();
                try
                {
                    baseObject.CommandText = value;
                }
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
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
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
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
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
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
            SQL32.System.Data.SQLite.SQLiteDataReader toWrap;
            try
            {
                toWrap =baseObject.ExecuteReader(behavior);
            }
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
            }
            if (toWrap == null)
            {
                return null;
            }
            return new SQLiteDataReader32(toWrap);
        }

        /// <summary>
        /// Create a new parameter
        /// </summary>
        ISQLiteParameter ISQLiteCommand.CreateParameter()
        {
            base.CheckDisposed();
            SQL32.System.Data.SQLite.SQLiteParameter toWrap;
            try
            {
                toWrap = baseObject.CreateParameter();
            }
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
            }
            if (toWrap == null)
            {
                return null;
            }
            return new SQLiteParameter32(toWrap);
        }

        /// <summary>
        /// Returns the SQLiteParameterCollection for the given command
        /// </summary>
        ISQLiteParameterCollection ISQLiteCommand.Parameters
        {
            get
            {
                base.CheckDisposed();
                SQL32.System.Data.SQLite.SQLiteParameterCollection toWrap;
                try
                {
                    toWrap = baseObject.Parameters;
                }
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
                }
                if (toWrap == null)
                {
                    return null;
                }
                return new SQLiteParameterCollection32(toWrap);
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
                SQL32.System.Data.SQLite.SQLiteTransaction toWrap;
                try
                {
                    toWrap = baseObject.Transaction;
                }
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
                }
                if (toWrap == null)
                {
                    return null;
                }
                return new SQLiteTransaction32(toWrap);
            }
            set
            {
                base.CheckDisposed();
                SQLiteTransaction32 castValue;
                if (value == null)
                {
                    castValue = null;
                }
                else
                {
                    castValue = value as SQLiteTransaction32;
                    if (castValue == null)
                    {
                        throw new ArgumentException("value is not castable as SQLiteTransaction32");
                    }
                }
                try
                {
                    baseObject.Transaction = (SQL32.System.Data.SQLite.SQLiteTransaction)castValue;
                }
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
                }
            }
        }
        #endregion
    }
}