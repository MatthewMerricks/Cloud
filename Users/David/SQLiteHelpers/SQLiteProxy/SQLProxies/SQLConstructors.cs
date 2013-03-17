using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// Constructors for all proxied-SQLite classes
    /// </summary>
    internal static class SQLConstructors
    {
        /// <summary>
        /// Initializes the connection with the specified connection string
        /// </summary>
        /// <param name="connectionString">The connection string to use on the connection</param>
        /// <returns>Constructed SQLiteConnection</returns>
        public static ISQLiteConnection SQLiteConnection(string connectionString)
        {
            return (Environment.Is64BitProcess
                ? SQLiteConnection64.Construct(connectionString)
                : SQLiteConnection32.Construct(connectionString));
        }
        
        /// <summary>
        /// Public constructor for generating a SQLite error given the base error code
        /// </summary>
        /// <param name="errorCode">The SQLite error code to report</param>
        /// <param name="message">Extra text to go along with the error message text</param>
        /// <returns>Constructed SQLiteException</returns>
        public static SQLiteExceptionBase SQLiteException(WrappedSQLiteErrorCode errorCode, string message)
        {
            return (Environment.Is64BitProcess
                ? SQLiteException64.Construct(errorCode, message)
                : SQLiteException32.Construct(errorCode, message));
        }
    }
}