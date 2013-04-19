//
// ISQLiteCommand.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite implementation of DbCommand.
    /// </summary>
    internal interface ISQLiteCommand : IDisposable
    {
        /// <summary>
        /// The SQL command text associated with the command
        /// </summary>
        string CommandText { get; set; }

        /// <summary>
        /// Execute the command and return the number of rows inserted/updated affected
        /// by it.
        /// </summary>
        int ExecuteNonQuery();
        
        /// <summary>
        /// Execute the command and return the first column of the first row of the resultset
        /// (if present), or null if no resultset was returned.
        /// </summary>
        /// <returns>The first column of the first row of the first resultset from the query</returns>
        object ExecuteScalar();
        
        /// <summary>
        /// Overrides the default behavior to return a SQLiteDataReader specialization
        /// class
        /// </summary>
        /// <param name="behavior">The flags to be associated with the reader</param>
        /// <returns>A SQLiteDataReader</returns>
        ISQLiteDataReader ExecuteReader(CommandBehavior behavior);
        
        /// <summary>
        /// Create a new parameter
        /// </summary>
        ISQLiteParameter CreateParameter();

        /// <summary>
        /// Returns the SQLiteParameterCollection for the given command
        /// </summary>
        ISQLiteParameterCollection Parameters { get; }

        /// <summary>
        /// The transaction associated with this command. SQLite only supports one transaction
        /// per connection, so this property forwards to the command's underlying connection.
        /// </summary>
        ISQLiteTransaction Transaction { get; set; }
    }
}