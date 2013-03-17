﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite error codes
    /// </summary>
    public enum WrappedSQLiteErrorCode : int
    {
        /// <summary>
        /// Success
        /// </summary>
        Ok = 0,
        /// <summary>
        /// SQL error or missing database
        /// </summary>
        Error = 1,
        /// <summary>
        /// Internal logic error in SQLite
        /// </summary>
        Internal = 2,
        /// <summary>
        /// Access permission denied
        /// </summary>
        Perm = 3,
        /// <summary>
        /// Callback routine requested an abort
        /// </summary>
        Abort = 4,
        /// <summary>
        /// The database file is locked
        /// </summary>
        Busy = 5,
        /// <summary>
        /// A table in the database is locked
        /// </summary>
        Locked = 6,
        /// <summary>
        /// malloc() failed
        /// </summary>
        NoMem = 7,
        /// <summary>
        /// Attempt to write a read-only database
        /// </summary>
        ReadOnly = 8,
        /// <summary>
        /// Operation terminated by sqlite3_interrupt()
        /// </summary>
        Interrupt = 9,
        /// <summary>
        /// Some kind of disk I/O error occurred
        /// </summary>
        IOErr = 10,
        /// <summary>
        /// The database disk image is malformed
        /// </summary>
        Corrupt = 11,
        /// <summary>
        /// Table or record not found
        /// </summary>
        NotFound = 12,
        /// <summary>
        /// Insertion failed because database is full
        /// </summary>
        Full = 13,
        /// <summary>
        /// Unable to open the database file
        /// </summary>
        CantOpen = 14,
        /// <summary>
        /// Database lock protocol error
        /// </summary>
        Protocol = 15,
        /// <summary>
        /// Database is empty
        /// </summary>
        Empty = 16,
        /// <summary>
        /// The database schema changed
        /// </summary>
        Schema = 17,
        /// <summary>
        /// Too much data for one row of a table
        /// </summary>
        TooBig = 18,
        /// <summary>
        /// Abort due to constraint violation
        /// </summary>
        Constraint = 19,
        /// <summary>
        /// Data type mismatch
        /// </summary>
        Mismatch = 20,
        /// <summary>
        /// Library used incorrectly
        /// </summary>
        Misuse = 21,
        /// <summary>
        /// Uses OS features not supported on host
        /// </summary>
        NOLFS = 22,
        /// <summary>
        /// Authorization denied
        /// </summary>
        Auth = 23,
        /// <summary>
        /// Auxiliary database format error
        /// </summary>
        Format = 24,
        /// <summary>
        /// 2nd parameter to sqlite3_bind out of range
        /// </summary>
        Range = 25,
        /// <summary>
        /// File opened that is not a database file
        /// </summary>
        NotADatabase = 26,
        /// <summary>
        /// sqlite3_step() has another row ready
        /// </summary>
        Row = 100,
        /// <summary>
        /// sqlite3_step() has finished executing
        /// </summary>
        Done = 101
    }
}