﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Cloud.SQLProxies
{
    extern alias SQL64;

    /// <summary>
    /// SQLite implentation of DbConnection.
    /// </summary>
    /// <remarks>
    /// The System.Data.SQLite.SQLiteConnection.ConnectionString property of the
    /// SQLiteConnection class can contain the following parameter(s), delimited
    /// with a semi-colon: Parameter Values Required Default Data Source {filename}
    /// Y Version 3 N 3 UseUTF16Encoding TrueFalse N False DateTimeFormat Ticks -
    /// Use DateTime.TicksISO8601 - Use ISO8601 DateTime format N ISO8601 BinaryGUID
    /// True - Store GUID columns in binary formFalse - Store GUID columns as text
    /// N True Cache Size {size in bytes} N 2000 Synchronous Normal - Normal file
    /// flushing behaviorFull - Full flushing after all writesOff - Underlying OS
    /// flushes I/O's N Normal Page Size {size in bytes} N 1024 Password {password}
    /// N Enlist Y - Automatically enlist in distributed transactionsN - No automatic
    /// enlistment N Y Pooling True - Use connection poolingFalse - Do not use connection
    /// pooling N False FailIfMissing True - Don't create the database if it does
    /// not exist, throw an error insteadFalse - Automatically create the database
    /// if it does not exist N False Max Page Count {size in pages} - Limits the
    /// maximum number of pages (limits the size) of the database N 0 Legacy Format
    /// True - Use the more compatible legacy 3.x database formatFalse - Use the
    /// newer 3.3x database format which compresses numbers more effectively N False
    /// Default Timeout {time in seconds}The default command timeout N 30 Journal
    /// Mode Delete - Delete the journal file after a commitPersist - Zero out and
    /// leave the journal file on disk after a commitOff - Disable the rollback journal
    /// entirely N Delete Read Only True - Open the database for read only accessFalse
    /// - Open the database for normal read/write access N False Max Pool Size The
    /// maximum number of connections for the given connection string that can be
    /// in the connection pool N 100 Default IsolationLevel The default transaciton
    /// isolation level N Serializable
    /// </remarks>
    internal sealed class SQLiteConnection64 : DisposableProxyObject, ISQLiteConnection
    {
        private readonly SQL64.System.Data.SQLite.SQLiteConnection baseObject;

        protected override IDisposable BaseDisposable
        {
            get
            {
                return baseObject;
            }
        }

        /// <summary>
        /// Initializes the connection with the specified connection string
        /// </summary>
        /// <param name="connectionString">The connection string to use on the connection</param>
        public static ISQLiteConnection Construct(string connectionString)
        {
            try
            {
                return new SQLiteConnection64(new SQL64.System.Data.SQLite.SQLiteConnection(connectionString));
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        private SQLiteConnection64(SQL64.System.Data.SQLite.SQLiteConnection baseObject)
        {
            this.baseObject = baseObject;
        }

        #region ISQLiteConnection members
        /// <summary>
        /// Change the password (or assign a password) to an open database.
        /// </summary>
        /// <param name="newPassword">The new password to assign to the database</param>
        /// <remarks>
        /// No readers or writers may be active for this process. The database must already
        /// be open and if it already was password protected, the existing password must
        /// already have been supplied.
        /// </remarks>
        void ISQLiteConnection.ChangePassword(string newPassword)
        {
            base.CheckDisposed();
            try
            {
                baseObject.ChangePassword(newPassword);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Sets the password for a password-protected database. A password-protected
        /// database is unusable for any operation until the password has been set.
        /// </summary>
        /// <param name="databasePassword">The password for the database</param>
        void ISQLiteConnection.SetPassword(string databasePassword)
        {
            base.CheckDisposed();
            try
            {
                baseObject.SetPassword(databasePassword);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Opens the connection using the parameters found in the System.Data.SQLite.SQLiteConnection.ConnectionString
        /// </summary>
        void ISQLiteConnection.Open()
        {
            base.CheckDisposed();
            try
            {
                baseObject.Open();
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }

        /// <summary>
        /// Create a new SQLiteCommand and associate it with this connection.
        /// </summary>
        /// <returns>Returns an instantiated SQLiteCommand object already assigned to this connection.</returns>
        ISQLiteCommand ISQLiteConnection.CreateCommand()
        {
            base.CheckDisposed();
            SQL64.System.Data.SQLite.SQLiteCommand toWrap;
            try
            {
                toWrap = baseObject.CreateCommand();
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
            if (toWrap == null)
            {
                return null;
            }
            return new SQLiteCommand64(toWrap);
        }

        /// <summary>
        /// Creates a new SQLiteTransaction if one isn't already active on the connection.
        /// </summary>
        /// <remarks>Unspecified will use the default isolation level specified in the connection
        /// string. If no isolation level is specified in the connection string, Serializable
        /// is used. Serializable transactions are the default. In this mode, the engine
        /// gets an immediate lock on the database, and no other threads may begin a
        /// transaction. Other threads may read from the database, but not write. With
        /// a ReadCommitted isolation level, locks are deferred and elevated as needed.
        /// It is possible for multiple threads to start a transaction in ReadCommitted
        /// mode, but if a thread attempts to commit a transaction while another thread
        /// has a ReadCommitted lock, it may timeout or cause a deadlock on both threads
        /// until both threads' CommandTimeout's are reached.</remarks>
        /// <param name="isolationLevel">Supported isolation levels are Serializable, ReadCommitted and Unspecified.</param>
        /// <returns>Returns a SQLiteTransaction object.</returns>
        ISQLiteTransaction ISQLiteConnection.BeginTransaction(IsolationLevel isolationLevel)
        {
            base.CheckDisposed();
            SQL64.System.Data.SQLite.SQLiteTransaction toWrap;
            try
            {
                toWrap = baseObject.BeginTransaction(isolationLevel);
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
        #endregion
    }
}