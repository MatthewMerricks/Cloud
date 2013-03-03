//
// SqlAccessor.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using ErikEJ.SqlCe;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Cloud.SQLIndexer.SqlModel
{
    internal static class SqlAccessor<T> where T : class
    {
        #region private static members
        // Store the Type for the current generic type
        private static readonly Type CurrentGenericType = typeof(T);

        // Dictionary to store the compiled and uncompiled parser expressions, keyed by the combination of includes as a space-seperated string
        private static readonly Dictionary<string, KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>> ResultParsers =
            new Dictionary<string, KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>>();

        #region columns
        // Array of insertable columns (used to build DataTable for bulk insert or to find column names), set via FindInsertColumns
        private static KeyValuePair<string, PropertyInfo>[] InsertColumns = null;
        // Array of identity columns (used in addition to InsertableColumns for identity insert), set via FindInsertColumns
        private static KeyValuePair<string, PropertyInfo>[] IdentityColumns = null;
        // String in the format "SELECT TOP 0 * FROM [TableName]", set via FindInsertColumns
        private static string InsertSelectTopZero = null;
        // Object to lock on for reading/writing to fields in this 'columns' group
        private static readonly object InsertLocker = new object();
        #endregion

        // Dictionary to store the portion of the select strings for accessing columns by inner table name (string.Empty for current level) and inner object name (string.Empty for default)
        private static readonly Dictionary<KeyValuePair<string, string>, string> SelectColumns = new Dictionary<KeyValuePair<string, string>, string>();

        #region table name
        // Returns the name of the table that the current generic type corresponds to
        private static string TableName
        {
            get
            {
                // If the current table name has not already been calculated,
                // then calculate it
                if (_tableName == null)
                {
                    // Pull the ISqlAccess attribute on the current generic type, if any
                    ISqlAccess insertAccess = CurrentGenericType.GetCustomAttributes(typeof(SqlAccess.ClassAttribute), true).Cast<ISqlAccess>().SingleOrDefault();

                    // If the ISqlAccess attribute exists and has a non-default name, then set the table name by the custom name
                    if (insertAccess != null
                        && insertAccess.SqlName != null)
                    {
                        _tableName = insertAccess.SqlName;
                    }
                    // Else if the ISqlAccess attribute does not exist or has a default name, then set the table name by the current generic type name
                    else
                    {
                        _tableName = CurrentGenericType.Name;
                    }
                }

                // Return the calculated table name
                return _tableName;
            }
        }
        // Stores the calculated table name
        private static string _tableName = null;
        // Locker for reading or writing the calculated table name
        private static readonly object TableNameLocker = new object();
        #endregion

        #region primary key
        // PropertyInfoes to access the generic typed object which correspond to the primary keys to lookup rows in the table, set via GetPrimaryKeyColumnValues
        private static PropertyInfo[] primaryKeyValues = null;
        // Name of the PrimaryKey index, for use with TableDirect access, set via GetPrimaryKeyColumnValues
        private static string primaryKeyIndexName = null;
        // Locker for reading or writing fields in this 'primary key' group
        private static readonly object PrimaryKeyOrdinalsLocker = new object();
        #endregion
        #endregion

        #region public static methods
        /// <summary>
        /// Gets a single result set by provided select statement and yield returns records converted to the current generic type
        /// </summary>
        /// <param name="connection">Database to query</param>
        /// <param name="select">Select statement</param>
        /// <param name="includes">List of joined children with dot syntax for multiple levels deep</param>
        /// <returns>Yield-returned converted database results as current generic type</returns>
        public static IEnumerable<T> SelectResultSet(SqlCeConnection connection, string select, IEnumerable<string> includes = null, SqlCeTransaction transaction = null)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            // grab the function that takes the values out of the current database row to produce the current generic type
            Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>> currentParser = GetResultParser(string.Empty,
                    includes == null ? new string[0] : ((includes as string[]) ?? includes.ToArray())).Value;

            // new command to run select
            SqlCeCommand selectCommand = connection.CreateCommand();
            try
            {
                // set command to run as select statement
                selectCommand.CommandText = select;

                if (transaction != null)
                {
                    selectCommand.Transaction = transaction;
                }

                // execute select command as a reader
                SqlCeDataReader selectResult = selectCommand.ExecuteReader(CommandBehavior.SingleResult);
                try
                {
                    // loop through database rows until there are no more left to read
                    while (selectResult.Read())
                    {
                        // get the current generic type by database row
                        KeyValuePair<T, bool> currentRow = currentParser(string.Empty, selectResult, new GenericHolder<short>(0));

                        // if not all of the columns in the current generic type are null,
                        // then return the converted row as the current generic type
                        if (!currentRow.Value)
                        {
                            yield return currentRow.Key;
                        }
                    }
                }
                finally
                {
                    selectResult.Dispose();
                }
            }
            finally
            {
                selectCommand.Dispose();
            }
        }

        /// <summary>
        /// Insert a list of generic typed objects into their corresponding Sql table;
        /// If more than one object is inserted then identities can not be queried by "SELECT @@IDENTITY"
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toInsert">Generic typed objects to insert</param>
        public static void InsertRows(SqlCeConnection connection, IEnumerable<T> toInsert, bool identityInsert = false, SqlCeTransaction transaction = null)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // If list of rows to insert exists,
            // then insert list of rows
            if (toInsert != null)
            {
                // Pull name of table
                string insertTableName = TableName;

                // Group of fields to pull from 'columns' group
                KeyValuePair<string, PropertyInfo>[] columns;
                KeyValuePair<string, PropertyInfo>[] identities;
                string selectTopZero;

                // Fill in the 'columns' group fields
                FindInsertColumns(connection, insertTableName, out columns, out identities, out selectTopZero, transaction);

                // Store whether more than one row is set for insert, defaulting with false
                bool foundMultiple = false;

                // Define a generic typed object that will be used to store the first object to be inserted
                T saveSingle;

                // Get enumerator for the objects to insert
                using (IEnumerator<T> singleEnumerator = toInsert.GetEnumerator())
                {
                    // If the enumerator has a first object,
                    // then set the first object found and check for multiple objects
                    if (singleEnumerator.MoveNext())
                    {
                        // set the first object found
                        saveSingle = singleEnumerator.Current;

                        // if there is a second object,
                        // then set that there was multiple objects
                        if (singleEnumerator.MoveNext())
                        {
                            foundMultiple = true;
                        }
                    }
                    // else if the enumerator does not have a first object,
                    // set the first object as null
                    else
                    {
                        saveSingle = null;
                    }
                }

                // if multiple (more than one) object was found,
                // then use the SQL Compact Bulk Insert Library (3rd party) to add the objects as rows
                if (foundMultiple)
                {
                    SqlCeCommand identityOnCommand = null;
                    SqlCeCommand identityOffCommand = null;

                    try
                    {
                        if (identityInsert)
                        {
                            identityOnCommand = connection.CreateCommand();

                            identityOnCommand.CommandText = "SET IDENTITY_INSERT [" + insertTableName + "] ON";

                            if (transaction != null)
                            {
                                identityOnCommand.Transaction = transaction;
                            }

                            identityOnCommand.ExecuteNonQuery();
                        }

                        // Create a new SQL Compact Bulk Inserter, preserving null values
                        using (SqlCeBulkCopy bulkCopy = (transaction == null
                            ? new SqlCeBulkCopy(connection, SqlCeBulkCopyOptions.KeepNulls)
                            : new SqlCeBulkCopy(connection, SqlCeBulkCopyOptions.KeepNulls, transaction)))
                        {
                            // Create a new DataTable to hold all the columns and rows to add
                            DataTable insertTable = new DataTable();

                            // Define the enumerable of types to retrieve
                            IEnumerable<PropertyInfo> values = columns.Select(currentColumn => currentColumn.Value);

                            // Add new DataColumns to match the list of columns
                            foreach (KeyValuePair<string, PropertyInfo> currentColumn in columns)
                            {
                                insertTable.Columns.Add(new DataColumn(currentColumn.Key, Nullable.GetUnderlyingType(currentColumn.Value.PropertyType) ?? currentColumn.Value.PropertyType));
                            }
                            if (identityInsert)
                            {
                                values = values.Concat(identities.Select(currentIdentity => currentIdentity.Value));

                                foreach (KeyValuePair<string, PropertyInfo> currentIdentity in identities)
                                {
                                    insertTable.Columns.Add(new DataColumn(currentIdentity.Key, Nullable.GetUnderlyingType(currentIdentity.Value.PropertyType) ?? currentIdentity.Value.PropertyType));
                                }
                            }

                            // Loop through all objects to insert
                            foreach (T currentInsert in toInsert)
                            {
                                // Add a new row to the DataTable with all insertable column values set
                                insertTable.Rows.Add(values.Select(currentValue => currentValue.GetValue(currentInsert, null) ?? DBNull.Value).ToArray());
                            }

                            // Set the table to insert into
                            bulkCopy.DestinationTableName = insertTableName;

                            // Write the DataTable with all the columns and objects to the database
                            bulkCopy.WriteToServer(insertTable);
                        }

                        if (identityInsert)
                        {
                            identityOffCommand = connection.CreateCommand();

                            identityOffCommand.CommandText = "SET IDENTITY_INSERT [" + insertTableName + "] OFF";

                            if (transaction != null)
                            {
                                identityOffCommand.Transaction = transaction;
                            }

                            identityOffCommand.ExecuteNonQuery();
                        }
                    }
                    finally
                    {
                        if (identityOnCommand != null)
                        {
                            identityOnCommand.Dispose();
                        }
                        if (identityOffCommand != null)
                        {
                            identityOffCommand.Dispose();
                        }
                    }
                }
                // else if only a sinle object was found,
                // then write the object via updatable SqlCeResultSet
                else if (saveSingle != null)
                {
                    SqlCeCommand identityOnCommand = null;
                    SqlCeCommand identityOffCommand = null;

                    try
                    {
                        if (identityInsert)
                        {
                            identityOnCommand = connection.CreateCommand();

                            identityOnCommand.CommandText = "SET IDENTITY_INSERT [" + insertTableName + "] ON";

                            if (transaction != null)
                            {
                                identityOnCommand.Transaction = transaction;
                            }

                            identityOnCommand.ExecuteNonQuery();
                        }

                        // create the command for querying the table to insert into
                        SqlCeCommand singleCommand = connection.CreateCommand();
                        try
                        {
                            // set the select statement with a zero row query on the current table
                            singleCommand.CommandText = selectTopZero;

                            if (transaction != null)
                            {
                                singleCommand.Transaction = transaction;
                            }

                            // execute a result set (empty) for the current table as updatable
                            SqlCeResultSet singleResult = singleCommand.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable);
                            try
                            {
                                // create the new database row
                                SqlCeUpdatableRecord singleUpdate = singleResult.CreateRecord();

                                // loop through the insertable columns
                                for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                                {
                                    // set the value in the new database row from the matching property in the current object to insert
                                    singleUpdate.SetValue(singleResult.GetOrdinal(columns[columnIndex].Key),
                                        columns[columnIndex].Value.GetValue(saveSingle, null) ?? DBNull.Value);
                                }

                                if (identityInsert)
                                {
                                    // loop through the identity columns
                                    for (int identityIndex = 0; identityIndex < identities.Length; identityIndex++)
                                    {
                                        // set the value in the new database row from the matching property in the current object to insert
                                        singleUpdate.SetValue(singleResult.GetOrdinal(identities[identityIndex].Key),
                                            identities[identityIndex].Value.GetValue(saveSingle, null) ?? DBNull.Value);
                                    }
                                }

                                // add the new database row to the database
                                singleResult.Insert(singleUpdate);
                            }
                            finally
                            {
                                singleResult.Dispose();
                            }
                        }
                        finally
                        {
                            singleCommand.Dispose();
                        }

                        if (identityInsert)
                        {
                            identityOffCommand = connection.CreateCommand();

                            identityOffCommand.CommandText = "SET IDENTITY_INSERT [" + insertTableName + "] OFF";

                            if (transaction != null)
                            {
                                identityOffCommand.Transaction = transaction;
                            }

                            identityOffCommand.ExecuteNonQuery();
                        }
                    }
                    finally
                    {
                        if (identityOnCommand != null)
                        {
                            identityOnCommand.Dispose();
                        }
                        if (identityOffCommand != null)
                        {
                            identityOffCommand.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calls InsertRows with a single row to insert and then selects the new identity and attempts to return it as the specified generic type
        /// </summary>
        /// <typeparam name="TKey">Type of identity to return</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="toInsert">Object to insert into database</param>
        /// <returns>Returns the identity of the inserted row</returns>
        public static TKey InsertRow<TKey>(SqlCeConnection connection, T toInsert, bool identityInsert = false, SqlCeTransaction transaction = null)
        {
            // Call to InsertRows which actually does the database insert
            InsertRows(connection, new T[] { toInsert }, identityInsert, transaction);

            // Create a command for selecting the identity
            SqlCeCommand identityCommand = connection.CreateCommand();
            try
            {
                // Set the command text to return the identity
                identityCommand.CommandText = "SELECT @@IDENTITY";

                if (transaction != null)
                {
                    identityCommand.Transaction = transaction;
                }

                // Run the identity selection command, convert the result to the generic type, and return it
                return Helpers.ConvertTo<TKey>(identityCommand.ExecuteScalar());
            }
            finally
            {
                identityCommand.Dispose();
            }
        }

        /// <summary>
        /// Calls InsertRows with a single row to insert but does not select the new identity, if any
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toInsert">Object to insert into database</param>
        public static void InsertRow(SqlCeConnection connection, T toInsert, bool identityInsert = false, SqlCeTransaction transaction = null)
        {
            // Call to InsertRows which actually does the database insert
            InsertRows(connection, new T[] { toInsert }, identityInsert, transaction);
        }

        /// <summary>
        /// Builds a portion of the select string with all the columns to be selected for the current generic type's columns (i.e. "[EnumCategories].[EnumCategoryId], [EnumCategories].[Name]")
        /// </summary>
        /// <param name="child">The SqlName (or reference name if not specified in ISqlAccess attribute) of the inner joined object</param>
        /// <param name="collectionName">The name of the joined set in the Select statement if not the table name (i.e. 'ServerLinkedFileSystemObjects' for LEFT OUTER JOIN [FileSystemObjects] ServerLinkedFileSystemObjects</param>
        /// <returns>Returns the build portion of the select string with the current generic type's columns</returns>
        public static string GetSelectColumns(string child = null, string collectionName = null)
        {
            // Store the current table name
            string selectTableName = TableName;

            // Create the dictionary key for getting/setting the select columns string
            KeyValuePair<string, string> selectKey = new KeyValuePair<string, string>((child ?? string.Empty), (collectionName ?? string.Empty));

            // Lock on the dictionary for getting/setting
            lock (SelectColumns)
            {
                // If the dictionary does not contain the current key,
                // then calculate the new select columns string and add it to the database
                string foundSelect;
                if (!SelectColumns.TryGetValue(selectKey, out foundSelect))
                {
                    // Calculate the new select columns string
                    foundSelect = string.Join(
                        
                        // Column names seperated by commas
                        ", ",

                        // Column names as a string array, based on current generic type
                        CurrentGenericType
                            // Get public instance properties on the current generic type that have one and only one ISqlAccess attribute
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<ISqlAccess>>(currentProp, currentProp.GetCustomAttributes(typeof(SqlAccess.PropertyAttribute), true).Cast<ISqlAccess>()))
                            .Where(currentProp => currentProp.Value.Any(currentAttrib => !currentAttrib.IsChild))
                            .Select(currentProp => new KeyValuePair<PropertyInfo, ISqlAccess>(currentProp.Key, currentProp.Value.Single()))

                            // For each of these ISqlAccess properties
                            .Select(currentProp =>

                                // If collection name is defaulted,
                                // then use format ['table name'] for the table name portion of the select column portion
                                (string.IsNullOrEmpty(collectionName)
                                    ? "[" + selectTableName + "]"

                                    // else if the collection name is custom,
                                    // then use the custom name
                                    : collectionName) +

                                    // add the current column name in format ['column name']
                                    ".[" + (currentProp.Value.SqlName ?? currentProp.Key.Name) + "]" +

                                        // if the child is defaulted,
                                        // then there is nothing left to add for the select column portion
                                        (string.IsNullOrEmpty(child)
                                            ? string.Empty

                                            // else if the child is specified,
                                            // then add a custom name for the column by format " AS ['child name'.'column name']"
                                            : " AS [" + child + "." + (currentProp.Value.SqlName ?? currentProp.Key.Name) + "]"))
                            // Output select column portions as array
                            .ToArray());
                    
                    // Add the current select columns string to the dictionary
                    SelectColumns.Add(selectKey, foundSelect);
                }

                // Return the current select columns string
                return foundSelect;
            }
        }

        /// <summary>
        /// Calls UpdateRows with a single row to merge into the database; row is modified where columns differ by searching the primary key values
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toUpdate">Object corresponding to row to update</param>
        /// <param name="searchCaseSensitive">Whether to search the row's primary key via a case-sensitive search</param>
        /// <param name="updateCaseSensitive">Whether to check for a column's difference to update by case-sensitive comparison</param>
        /// <returns>Returns whether the row was found to be updated</returns>
        public static bool UpdateRow(SqlCeConnection connection, T toUpdate, bool searchCaseSensitive = true, bool updateCaseSensitive = true, SqlCeTransaction transaction = null)
        {
            IEnumerable<int> unableToFindIndexes;
            UpdateRows(connection, new T[] { toUpdate }, out unableToFindIndexes, searchCaseSensitive, updateCaseSensitive, transaction);
            return !(unableToFindIndexes ?? Enumerable.Empty<int>()).Any();
        }

        /// <summary>
        /// Updates rows in the database corresponding to the provided list of objects; rows are modified where columns differ by searching the primary key values
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toUpdate">List of objects corresponding to rows to update</param>
        /// <param name="unableToFindIndexes">(output) List of indexes that were not found to update correlating to the index in the list of objects to update</param>
        /// <param name="searchCaseSensitive">Whether to search the row's primary key via a case-sensitive search</param>
        /// <param name="updateCaseSensitive">Whether to check for a column's difference to update by case-sensitive comparison</param>
        public static void UpdateRows(SqlCeConnection connection, IEnumerable<T> toUpdate, out IEnumerable<int> unableToFindIndexes, bool searchCaseSensitive = true, bool updateCaseSensitive = true, SqlCeTransaction transaction = null)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // if there are no rows to update,
            // then there were no indexes that weren't found, return
            if (toUpdate == null
                || !toUpdate.Any())
            {
                unableToFindIndexes = Enumerable.Empty<int>();
                return;
            }

            // get the current table name
            string updateTableName = TableName;

            // Group of fields to pull from 'columns' group
            KeyValuePair<string, PropertyInfo>[] columns;
            KeyValuePair<string, PropertyInfo>[] identities;
            string selectTopZero;

            // Fill in the 'columns' group fields
            FindInsertColumns(connection, updateTableName, out columns, out identities, out selectTopZero, transaction);

            // build an array of the types of properties which will be checked for update
            Type[] valueTypes = columns.Select(currentValue => currentValue.Value.PropertyType).ToArray();

            // get the primary key name and the properties which will retrieve values for the primary key search
            string primaryKeyName;
            PropertyInfo[] keyValues = GetPrimaryKeyColumnValues(connection, updateTableName, out primaryKeyName, transaction);

            // start a new list to store indexes which were not found to update
            List<int> unableToFindList = new List<int>();

            // create a new database command that will pull the existing rows as updatable
            using (SqlCeCommand retrieveExisting = connection.CreateCommand())
            {
                // query database table by TableDirect
                retrieveExisting.CommandType = CommandType.TableDirect;
                // set the table by the current name
                retrieveExisting.CommandText = updateTableName;
                // provide the primary key index name for searching
                retrieveExisting.IndexName = primaryKeyName;

                if (transaction != null)
                {
                    retrieveExisting.Transaction = transaction;
                }

                // retrieve a result set to allow primary key searching and updates
                using (SqlCeResultSet retrievedExisting = retrieveExisting.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable
                    | (searchCaseSensitive
                        ? ResultSetOptions.Sensitive
                        : ResultSetOptions.Insensitive)))
                {
                    // define a counter to keep track of which object is in the process of being updated
                    int updateIndex = 0;

                    // loop through all the objects to update
                    foreach (T currentUpdate in toUpdate)
                    {
                        // if the result set can seek to the primary key values in the current object to update,
                        // then continue updating the current object
                        if (retrievedExisting.Seek(DbSeekOptions.FirstEqual,
                            keyValues.Select(currentKey => currentKey.GetValue(currentUpdate, null))
                                .ToArray()))
                        {
                            // if the result set can read the row to update,
                            // then continue updating the current object
                            if (retrievedExisting.Read())
                            {
                                // store a boolean for whether a single difference or more required changing a value, defaulting to false
                                bool anyChangeFound = false;

                                // for each updateable column
                                for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                                {
                                    // get the column position of the current named updateble column
                                    int columnOrdinal = retrievedExisting.GetOrdinal(columns[columnIndex].Key);

                                    // get the value that will be checked against the current column
                                    object valueToUpdate = columns[columnIndex].Value.GetValue(currentUpdate, null);

                                    // store a boolean for whether a difference was found requiring the column to be updated, defaulting to false
                                    bool foundDifference = false;

                                    // get the value already in the database for comparison
                                    object originalValue = retrievedExisting.GetValue(columnOrdinal);

                                    // if the value already in the database is null,
                                    // then check if the new value is not null and thus different
                                    if (originalValue == null
                                        || originalValue is DBNull)
                                    {
                                        if (valueToUpdate != null)
                                        {
                                            foundDifference = true;
                                        }
                                    }
                                    // else if the value in the database is not null
                                    // and the new value is null,
                                    // then they are different
                                    else if (valueToUpdate == null
                                        || valueToUpdate is DBNull)
                                    {
                                        foundDifference = true;
                                    }
                                    // else if neither the value in the database is null nor the new value is null
                                    // and the types match,
                                    // then see if the values match to define a difference
                                    else if (originalValue.GetType().Equals(valueTypes[columnIndex]))
                                    {
                                        // if the current type to check is a string and a case-insensitive update was specified,
                                        // then a difference was only found if a case-insensitive string comparison gave a difference
                                        if (originalValue is string
                                            && !updateCaseSensitive)
                                        {
                                            if (!((string)originalValue).Equals((string)valueToUpdate, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                foundDifference = true;
                                            }
                                        }
                                        // else if the current type to check is not a string or a case-sensitive update was specificed,
                                        // then use the type's overriden Equals method comparer to see if there is a difference
                                        else if (!object.Equals(originalValue, valueToUpdate))
                                        {
                                            foundDifference = true;
                                        }
                                    }
                                    // else if neither the value in the datase is null nor the new value is null
                                    // and the types do not match,
                                    // then convert the values to match before checking if the values match to define a difference
                                    else
                                    {
                                        // convert the value in the database to the type of the new value
                                        originalValue = Helpers.ConvertTo(originalValue, valueTypes[columnIndex]);


                                        // if the current type to check is a string and a case-insensitive update was specified,
                                        // then a difference was only found if a case-insensitive string comparison gave a difference
                                        if (originalValue is string
                                            && !updateCaseSensitive)
                                        {
                                            if (!((string)originalValue).Equals((string)valueToUpdate, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                foundDifference = true;
                                            }
                                        }
                                        // else if the current type to check is not a string or a case-sensitive update was specificed,
                                        // then use the type's overriden Equals method comparer to see if there is a difference
                                        else if (!object.Equals(originalValue, valueToUpdate))
                                        {
                                            foundDifference = true;
                                        }
                                    }

                                    // if a difference was found for the current column in the current row,
                                    // then set the column in the row to the new value
                                    if (foundDifference)
                                    {
                                        // set the value in the column to the new value
                                        retrievedExisting.SetValue(columnOrdinal,
                                            valueToUpdate ?? DBNull.Value);

                                        // store that a change was to be made to at least one of the columns in the current row
                                        anyChangeFound = true;
                                    }
                                }

                                // if a change was made to any of the columns in the current row,
                                // then update the database with the changes
                                if (anyChangeFound)
                                {
                                    retrievedExisting.Update();
                                }
                            }
                            // else if the currently-seeked row could not be read,
                            // then add the current updateIndex to signify an object that couldn't be updated
                            else
                            {
                                unableToFindList.Add(updateIndex);
                            }
                        }
                        // else if a row could not be seeked for the provided primary key values,
                        // then add the current updateIndex to signify an object that couldn't be updated
                        else
                        {
                            unableToFindList.Add(updateIndex);
                        }
                        
                        // incremement the counter of objects that have been checked to update
                        updateIndex++;
                    }
                }
            }

            // set the output list of unable to find indexes
            unableToFindIndexes = unableToFindList;
        }

        /// <summary>
        /// Calls DeleteRows with a single object to delete from the database
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toDelete">Object to delete from database</param>
        /// <param name="caseSensitive">Whether the primary key will be searched as case-sensitive</param>
        /// <returns>Returns whether the row was deleted</returns>
        public static bool DeleteRow(SqlCeConnection connection, T toDelete, bool caseSensitive = true, SqlCeTransaction transaction = null)
        {
            IEnumerable<int> unableToFindIndexes;
            DeleteRows(connection, new T[] { toDelete }, out unableToFindIndexes, caseSensitive, transaction);
            return !(unableToFindIndexes ?? Enumerable.Empty<int>()).Any();
        }

        /// <summary>
        /// Deletes rows in the database that correspond to a given list of objects
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toDelete">List of objects to delete</param>
        /// <param name="unableToFindIndexes">(output) List of indexes that were not found to delete correlating to the index in the list of objects to delete</param>
        /// <param name="caseSensitive">Whether the primary key will be searched as case-sensitive</param>
        public static void DeleteRows(SqlCeConnection connection, IEnumerable<T> toDelete, out IEnumerable<int> unableToFindIndexes, bool caseSensitive = true, SqlCeTransaction transaction = null)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // If there are no objects to delete,
            // then set the output list of indexes that could not be found to delete as empty, return
            if (toDelete == null
                || !toDelete.Any())
            {
                unableToFindIndexes = Enumerable.Empty<int>();
                return;
            }

            // Get the table name
            string deleteTableName = TableName;

            // Group of fields to pull from 'columns' group
            KeyValuePair<string, PropertyInfo>[] columns;
            KeyValuePair<string, PropertyInfo>[] identities;
            string selectTopZero;

            // Fill in the 'columns' group fields
            FindInsertColumns(connection, deleteTableName, out columns, out identities, out selectTopZero, transaction);

            // Get the primary key name and PropertyInfoes for accesing the primary key values from current generic typed objects
            string primaryKeyName;
            PropertyInfo[] keyValues = GetPrimaryKeyColumnValues(connection, deleteTableName, out primaryKeyName, transaction);

            // Create the list for indexes that were unable to be found to delete
            List<int> unableToFindList = new List<int>();

            // Create the command for retrieving an updatable result set to search for rows to delete
            using (SqlCeCommand retrieveExisting = connection.CreateCommand())
            {
                // Set the comand type as TableDirect
                retrieveExisting.CommandType = CommandType.TableDirect;
                // Set the table as the current table
                retrieveExisting.CommandText = deleteTableName;
                // Set the index as the primary key index for searching rows
                retrieveExisting.IndexName = primaryKeyName;

                if (transaction != null)
                {
                    retrieveExisting.Transaction = transaction;
                }

                // Retrieve an updatable result set to search for rows to delete
                using (SqlCeResultSet retrievedExisting = retrieveExisting.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable
                    | (caseSensitive
                        ? ResultSetOptions.Sensitive
                        : ResultSetOptions.Insensitive)))
                {
                    // Start an index counter for which object is being deleted
                    int deleteIndex = 0;

                    // Loop through the objects to delete
                    foreach (T currentDelete in toDelete)
                    {
                        // If the retrieved result set can seek with the primary key values from the current object to delete,
                        // then continue to delete the current object
                        if (retrievedExisting.Seek(DbSeekOptions.FirstEqual,
                            keyValues.Select(currentKey => currentKey.GetValue(currentDelete, null))
                                .ToArray()))
                        {
                            // If the retrieved result can read the seeked object,
                            // then delete the row
                            if (retrievedExisting.Read())
                            {
                                retrievedExisting.Delete();
                            }
                            // Else if the retrieved result could not read the seeked object,
                            // then add the current object index to the list that couldn't be found to delete
                            else
                            {
                                unableToFindList.Add(deleteIndex);
                            }
                        }
                        // Else if the retrieved result could not be seeked for the current object,
                        // then add the current object index to the list that couldn't be found to delete
                        else
                        {
                            unableToFindList.Add(deleteIndex);
                        }

                        // Increment the counter for objects checked for delete
                        deleteIndex++;
                    }
                }
            }

            // Output the list of indexes not found to delete
            unableToFindIndexes = unableToFindList;
        }
        #endregion

        #region private static methods
        // Get and sets as necessary, the fields in the 'primary key' group for the primary key index name and property infoes to access primary key values in current generic typed objects
        private static PropertyInfo[] GetPrimaryKeyColumnValues(SqlCeConnection connection, string tableName, out string primaryKeyName, SqlCeTransaction transaction)
        {
            // Lock for getting/setting fields in the 'primary key' group
            lock (PrimaryKeyOrdinalsLocker)
            {
                // If the fields in the 'primary key' group have not been set,
                // then calculate them
                if (primaryKeyValues == null)
                {
                    // Create the command for finding the primary key name and columns for the current table
                    SqlCeCommand ordinalCommand = connection.CreateCommand();
                    try
                    {
                        // Set the command for finding the primary key name and columns for the current table
                        ordinalCommand.CommandText = "SELECT [INFORMATION_SCHEMA].[INDEXES].[ORDINAL_POSITION], " +
                            "[INFORMATION_SCHEMA].[INDEXES].[INDEX_NAME], " +
                            "[INFORMATION_SCHEMA].[INDEXES].[COLUMN_NAME] " +
                            "FROM [INFORMATION_SCHEMA].[INDEXES] " +
                            "WHERE [INFORMATION_SCHEMA].[INDEXES].[TABLE_NAME] = '" + tableName.Replace("'", "''") + "' " +
                            "AND [INFORMATION_SCHEMA].[INDEXES].[PRIMARY_KEY] = 1";

                        if (transaction != null)
                        {
                            ordinalCommand.Transaction = transaction;
                        }

                        // Execute the result set for finding the primary key name and columns for the current table
                        SqlCeResultSet ordinals = ordinalCommand.ExecuteResultSet(ResultSetOptions.Insensitive | ResultSetOptions.Scrollable);

                        try
                        {
                            // store the names of the columns in the primary key in the order of their ordinal positions while setting the primary key name
                            string[] indexNames = ordinals.Cast<SqlCeUpdatableRecord>()
                                .ToArray()
                                .OrderBy(currentOrdinal => currentOrdinal.GetInt32(ordinals.GetOrdinal("ORDINAL_POSITION")))
                                .Select(currentOrdinal => new KeyValuePair<string, string>(currentOrdinal.GetString(ordinals.GetOrdinal("COLUMN_NAME")),
                                    (primaryKeyIndexName = currentOrdinal.GetString(ordinals.GetOrdinal("INDEX_NAME")))).Key)
                                .ToArray();

                            // Create the dictionary to map primary key column names to their order in the list of columns using a case-insensitive key comparison
                            Dictionary<string, int> primaryKeyColumnNames = new Dictionary<string, int>(indexNames.Length, StringComparer.InvariantCultureIgnoreCase);

                            // Add each column name with its position the primary key column name dictionary
                            for (int nameIndex = 0; nameIndex < indexNames.Length; nameIndex++)
                            {
                                primaryKeyColumnNames.Add(indexNames[nameIndex], nameIndex);
                            }

                            // Set the PropertyInfoes in a current generic typed object used to access the primary key values
                            primaryKeyValues = CurrentGenericType
                                
                                // Filter the current generic type's properties by those with one and only one ISqlAccess attribute
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<ISqlAccess>>(currentProp,
                                    currentProp.GetCustomAttributes(typeof(SqlAccess.PropertyAttribute), true)
                                        .Cast<ISqlAccess>()))
                                .Where(currentProp => currentProp.Value.Any())
                                .Select(currentProp => new KeyValuePair<PropertyInfo, ISqlAccess>(currentProp.Key, currentProp.Value.Single()))

                                // Filter the ISqlAccess properties to those in the primary key (case-insensitive comparison)
                                .Where(currentProp => primaryKeyColumnNames.ContainsKey(currentProp.Value.SqlName ?? currentProp.Key.Name))

                                // Order according to the order of the primary key columns in the index
                                .OrderBy(currentProp => primaryKeyColumnNames[currentProp.Value.SqlName ?? currentProp.Key.Name])

                                // Select just the PropertyInfoes as an array
                                .Select(currentProp => currentProp.Key)
                                .ToArray();
                        }
                        finally
                        {
                            ordinals.Dispose();
                        }
                    }
                    finally
                    {
                        ordinalCommand.Dispose();
                    }
                }

                // Get the already queried index name for the primary key
                primaryKeyName = primaryKeyIndexName;
                // Return the PropertyInfoes for accessing the primary key values in current generic typed objects
                return primaryKeyValues;
            }
        }

        // Helper function to reduce expression complexity which returns the recursed parser expression of an inner child
        private static Expression GetParserExpression(string parentName, string[] includes)
        {
            return GetResultParser(parentName, includes).Key;
        }

        /// <summary>
        /// Builds new or returns existing compiled and noncompiled expressions which build a new generic typed object by a database row
        /// </summary>
        /// <param name="parentName">The part of the column names that precedes the current levels property names (i.e. "FileSystemObjects." before the "Path" property)</param>
        /// <param name="includes">The list of included inner objects</param>
        /// <returns>Returns the compiled and noncompiled expressions which build a new generic typed object by a database row</returns>
        private static KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> GetResultParser(string parentName, params string[] includes)
        {
            // lock to prevent simultaneously reading for existing parser functions and creating new ones when needed
            lock (ResultParsers)
            {
                // build HashSet for includes so they can be queried
                HashSet<string> includeSet = new HashSet<string>(includes ?? Enumerable.Empty<string>());

                // build database key to find/store parser function;
                // unique for every combination of includes, which are ordered so that including [A] and [B] is the same as including [B] and [A]
                string currentParserKey = string.Join(" ",
                    includeSet
                    .OrderBy(currentInclude => currentInclude)
                    .ToArray());

                // if the current parser function cannot be found in the dictionary,
                // then it needs to be created
                KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> currentParser;
                if (!ResultParsers.TryGetValue(currentParserKey, out currentParser))
                {
                    // Get all the properties in the current generic type which can be set from the database
                    IEnumerable<KeyValuePair<ParameterExpression, KeyValuePair<PropertyInfo, ISqlAccess>>> sqlProps =
                        
                        // Find all the public instance properties in the current generic type which are decorated with a ISqlAccess attribute
                        CurrentGenericType
                        .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                        .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<ISqlAccess>>(currentProp,
                            currentProp
                                .GetCustomAttributes(typeof(SqlAccess.PropertyAttribute), true)
                                .Cast<ISqlAccess>()))
                        .Where(currentProp => currentProp.Value.Any())

                        // For each, output a new parameter of KeyValuePair<[PropertyType], bool>,
                        // the PropertyInfo,
                        // and the ISqlAccess attribute
                        .Select(currentProp => new KeyValuePair<ParameterExpression, KeyValuePair<PropertyInfo, ISqlAccess>>(
                            Expression.Parameter(
                                SqlAccess.keyValueType.MakeGenericType(currentProp.Key.PropertyType, SqlAccess.boolType)),
                            new KeyValuePair<PropertyInfo, ISqlAccess>(currentProp.Key,
                                currentProp.Value.Single())));

                    // Create the parameters that will be passed into the expression
                    ParameterExpression selectedRow = Expression.Parameter(SqlAccess.recordType);
                    ParameterExpression parentExpression = Expression.Parameter(SqlAccess.stringType);
                    ParameterExpression nullCounterExpression = Expression.Parameter(SqlAccess.counterType);

                    // Create the construction expression for the current generic typed object
                    NewExpression newResult = Expression.New(CurrentGenericType);

                    // Get the indexer property expression that can pull column values by string name out of the database row type
                    PropertyInfo indexProp =
                        
                        // Pull public instance properties from the database row type
                        SqlAccess.recordType
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)

                        // For each, select the PropertyInfo and indexer parameters
                        .Select(currentProp => new KeyValuePair<PropertyInfo, ParameterInfo[]>(currentProp, currentProp.GetIndexParameters()))

                        // Output the PropertyInfo that has one and only one indexer parameter which is a string type
                        .Single(currentProp => currentProp.Value != null
                            && currentProp.Value.Length == 1
                            && currentProp.Value[0].ParameterType == SqlAccess.stringType)
                        .Key;

                    // Start a counter for the number of columns in the current generic type (not children objects)
                    short propCounter = 0;

                    // Select all the MemberAssignments that will be made in the constructor of the current generic typed object
                    IEnumerable<MemberBinding> initMembers =
                        
                        // Filter current generic typed object properties so they are either columns in the current table or children objects which are to be included
                        sqlProps
                        .Where(sqlProp => !sqlProp.Value.Value.IsChild
                            || includeSet.Contains(sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name))

                        // For each property, return the MemberAssignment
                        .Select(sqlProp =>
                            
                            // Create a MemberAssignment to set the current property from either the database column or by constructing an inner object
                            Expression.Bind(
                                // Current property to set
                                sqlProp.Value.Key,
                                
                                // If the current property is a child object,
                                // then invoke a recursed inner expression that will construct the inner object
                                (sqlProp.Value.Value.IsChild

                                    // invoke recursed inner expression that will construct the inner object
                                    ? (Expression)Expression.Invoke(

                                        // If constructed inner object has all null columns then assign the current property as null otherwise assign as the constructed inner object:
                                        // (Func<KeyValuePair<[property type], bool>, [property type]>)(constructedObject => constructedObject.Value ? null : constructedObject.Key)
                                        Expression.Lambda(

                                            // If the constructed inner object has all null columns,
                                            // then return null,
                                            // else return the constructed inner object
                                            Expression.Condition(
                                                
                                                // Test: constructed inner object has all null columns
                                                Expression.Property(
                                                
                                                    // Parameter of the KeyValuePair<[property type], bool> for the inner constructed object
                                                    sqlProp.Key,

                                                    // PropertyInfo for Value in KeyValuePair<[property type], bool>
                                                    SqlAccess.keyValueType
                                                        .MakeGenericType(sqlProp.Value.Key.PropertyType, SqlAccess.boolType)
                                                        .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)),

                                                // If true: return null
                                                Expression.Constant(null, sqlProp.Value.Key.PropertyType),

                                                // If false: return constructed object
                                                Expression.Property(sqlProp.Key,
                                                    SqlAccess.keyValueType.MakeGenericType(sqlProp.Value.Key.PropertyType, SqlAccess.boolType)
                                                        .GetProperty("Key", BindingFlags.Instance | BindingFlags.Public))),

                                            // Input parameter for null check test: the constructed inner object 
                                            sqlProp.Key),

                                        // Constructed inner object to pass into null check test, create by invoking recursed inner expression
                                        Expression.Invoke((Expression)

                                            // Invoke recursed inner expression to return expression to build inner object
                                            typeof(SqlAccessor<>)
                                                .MakeGenericType(sqlProp.Value.Key.PropertyType)
                                                .GetMethod("GetParserExpression", BindingFlags.Static | BindingFlags.NonPublic)
                                                .Invoke(
                                            
                                                // static method
                                                null,

                                                // invocation parameters
                                                new object[]
                                                {
                                                    // The inner [parent name] will be the current [parent name] + [column name] + "."
                                                    parentName + (sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name) + ".",

                                                    // Filter includes for constructing the inner expression by ones that start from the inner object name (dot notation hierarchy)
                                                    includeSet
                                                        .Where(currentInclude => currentInclude.StartsWith((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name) + "."))

                                                        // Remove the parent portion of the include name (i.e. [current level].SyncState.FileSystemObject -> [SyncState is not the current level].FileSystemObject)
                                                        .Select(currentInclude => currentInclude.Substring((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name).Length + 1))

                                                        // Includes parameter must be an array
                                                        .ToArray()
                                                }),

                                            // First parameter for recursed inner expression: [parent name];
                                            // Create by current [parent name] + [column name] + "." --> string.Concat([parent name], [column name] + ".")
                                            Expression.Call(SqlAccess.ConcatStringsInfo,

                                                // new string[] { [parent name], [column name] + "." }
                                                Expression.NewArrayInit(SqlAccess.stringType,

                                                    // [parent name]
                                                    parentExpression,

                                                    // [column name] + "."
                                                    Expression.Constant((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name) + ".",
                                                        SqlAccess.stringType))),

                                            // Second parameter for recursed inner expression: current database row
                                            selectedRow,

                                            // Third parameter for recursed inner expression: create a new GenericHolder<short> starting at 0 to count null database columns
                                            Expression.New(typeof(GenericHolder<short>).GetConstructor(new Type[] { typeof(short) }),
                                                Expression.Constant((short)0))))

                                    // Else if the current property is not a child object,
                                    // then the current property will be set by accessing the current database row column;
                                    // each column will increment the property counter
                                    : ((propCounter = (short)(propCounter + (short)1)) > short.MinValue

                                        // Convert the current database row column to the property type
                                        ? Expression.Call(Helpers.ConvertToInfo
                                                .MakeGenericMethod(sqlProp.Value.Key.PropertyType),

                                            // If the current column has a null value for the current database row column,
                                            // then increment the counter for null columns found and default the current property,
                                            // else set the current property by the accessing the current database row column
                                            Expression.Condition(
                                                
                                                // The test: current column has a null value for the current database row column;
                                                // [current database row].IsDBNull([current database row].GetOrdinal([parent name] + [column name]))
                                                Expression.Call(
                                                
                                                    // current database row
                                                    selectedRow,

                                                    // [current database row].IsDBNull
                                                    SqlAccess.IsNullRecordInfo,

                                                    // [current database row].GetOrdinal([parent name] + [column name])
                                                    Expression.Call(selectedRow,

                                                        // [current database row].GetOrdinal
                                                        SqlAccess.GetOrdinalRecordInfo,

                                                        // [parent name] + [column name] --> string.Concat([parent name], [column name])
                                                        Expression.Call(SqlAccess.ConcatStringsInfo,

                                                            // new string[] { [parent name], [column name] }
                                                            Expression.NewArrayInit(SqlAccess.stringType,
                                                                
                                                                // [parent name]
                                                                parentExpression,

                                                                // [column name]
                                                                Expression.Constant((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name),
                                                                    SqlAccess.stringType))))),

                                                // If true: increment the counter for null columns found and default the current property
                                                Expression.Block(
                                                
                                                    // block returns an object
                                                    SqlAccess.objectType,

                                                    // first line out of two: increment count of null database columns
                                                    Expression.AddAssign(
                                                        
                                                        // Value property in GenericHolder<short> to increment
                                                        Expression.Property(nullCounterExpression, GenericHolder<short>.ValueInfo),

                                                        //  Increment amount (one)
                                                        Expression.Constant((short)1)),

                                                    // second line out of two: return Helpers.DefaultForType([property type])
                                                    Expression.Call(Helpers.DefaultForTypeInfo,
                                                        Expression.Constant(sqlProp.Value.Key.PropertyType))),

                                                // If false: set the current property by the accessing the current database row column
                                                Expression.MakeIndex(
                                                
                                                    // current database row
                                                    selectedRow,
                                                    
                                                    // database row indexer property (that takes a string)
                                                    indexProp,

                                                    // parameters for indexer (only one)
                                                    new Expression[]
                                                        {
                                                            // parameter for indexer: [parent name] + [column name] <-- string.Concat([parent name], [column name])
                                                            Expression.Call(SqlAccess.ConcatStringsInfo,

                                                                // new string[] { [parent name], [column name] }
                                                                Expression.NewArrayInit(SqlAccess.stringType,

                                                                    // [parent name]
                                                                    parentExpression,

                                                                    // [column name]
                                                                    Expression.Constant((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name),
                                                                        SqlAccess.stringType)))
                                                        })))

                                        // not possible (an Int32 + 1 is always greater than Int32.MinValue):
                                        : null))));

                    // Create the expression to construct the current generic type while filling in its properties
                    MemberInitExpression initResult = Expression.MemberInit(newResult, initMembers);

                    // Types for KeyValuePair<,> to make KeyValuePair<[current generic type], bool>
                    Type[] keyValueTypes = new Type[]
                    {
                        initResult.Type,
                        SqlAccess.boolType
                    };

                    // Expression to construct the KeyValuePair of constructed object type and bool (for if all inner columns are null)
                    NewExpression returnPairExpression = Expression.New(

                        // KeyValuePair<[current generic type], bool>
                        SqlAccess.keyValueType
                            .MakeGenericType(keyValueTypes)

                            // Get KeyValuePair<[current generic type], bool> constructor that takes [current generic type] and bool parameters
                            // (new KeyValuePair<[current generic type], bool>([current generic type], [bool for whether inner columns are null)
                            .GetConstructor(keyValueTypes),

                        // First parameter out of two for constructing KeyValuePair: the constructed generic type
                        initResult,

                        // Second parameter out of two for constructing KeyValuePair: the result for equality of the null column counter to the count of columns
                        Expression.Equal(

                            // Access Value property of null column counter
                            Expression.Property(
                        
                                // Null column counter
                                nullCounterExpression,

                                // Value property
                                GenericHolder<short>.ValueInfo),

                            // Count of columns
                            Expression.Constant((short)propCounter)));

                    // Create lambda from construction of current generic typed object using input parameters
                    Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> parserExpression = Expression.Lambda<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>(

                        // Construction of current generic typed object
                        returnPairExpression,
                        
                        // Parameter one of three: [parent name]
                        parentExpression,

                        // Parameter two of three: current database row
                        selectedRow,

                        // Parameter three of three: null column counter
                        nullCounterExpression);

                    // Join the parser expression with its compiled method into a KeyValuePair, also sets the value to return on this function call
                    currentParser = new KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>(
                        parserExpression,
                        parserExpression.Compile());

                    // Add the compiled and uncompiled parser expression into the dictionary
                    ResultParsers.Add(currentParserKey, currentParser);
                }

                // Return the compiled and uncompiled parser expression
                return currentParser;
            }
        }

        // Gets and sets as necessary, lists of the updatable columns and corresponding PropertyInfoes to access the values in the current generic typed objects;
        // also creates a select statement that pulls zero rows with all the columns for the current table
        private static void FindInsertColumns(SqlCeConnection connection, string TableName, out KeyValuePair<string, PropertyInfo>[] columns, out KeyValuePair<string, PropertyInfo>[] identities, out string selectTopZero, SqlCeTransaction transaction)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Lock for getting/setting fields in the 'colummns' group
            lock (InsertLocker)
            {
                // If the fields have not yet been set in the 'columns' group,
                // then calculate all the fields
                if (InsertColumns == null)
                {
                    // Create HashSet to store column names for identity columns
                    HashSet<string> hashedIdentities = new HashSet<string>();

                    // Create command to search for identity columns in the current table
                    SqlCeCommand identityCommand = connection.CreateCommand();
                    try
                    {
                        // Set the command text to query identity columns based on a table with the current table name
                        identityCommand.CommandText = "SELECT [INFORMATION_SCHEMA].[COLUMNS].[COLUMN_NAME] " +
                            "FROM [INFORMATION_SCHEMA].[COLUMNS] " +
                            "WHERE [INFORMATION_SCHEMA].[COLUMNS].[TABLE_NAME] = '" + TableName.Replace("'", "''") + "' " +
                            "AND [INFORMATION_SCHEMA].[COLUMNS].[AUTOINC_SEED] IS NOT NULL";

                        if (transaction != null)
                        {
                            identityCommand.Transaction = transaction;
                        }
                        
                        // Create a result set of the selected column names of identity columns
                        SqlCeResultSet identitySet = identityCommand.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Insensitive);
                        try
                        {
                            // For each identity column name in the result set
                            foreach (SqlCeUpdatableRecord identityRecord in identitySet.OfType<SqlCeUpdatableRecord>())
                            {
                                // Add the current column name to the HashSet
                                hashedIdentities.Add(identityRecord.GetString(0));
                            }
                            
                            // Set the PropertyInfoes for the current generic typed object of Sql columns which are not identities
                            IEnumerable<KeyValuePair<PropertyInfo, ISqlAccess>> sqlProps = CurrentGenericType
                                
                                // Get properties which have one and only one ISqlAccess attribute
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<ISqlAccess>>(currentProp,
                                    currentProp.GetCustomAttributes(typeof(SqlAccess.PropertyAttribute), true)
                                        .Cast<ISqlAccess>()))
                                .Where(currentProp => currentProp.Value.Any(currentAttrib => !currentAttrib.IsChild))
                                .Select(currentProp => new KeyValuePair<PropertyInfo, ISqlAccess>(currentProp.Key, currentProp.Value.Single()));


                            // Set the list of insertable (not identity) columns as new DataColumns with the name of the column in Sql and the type of the property
                            InsertColumns = sqlProps
                                // Filter by properties not in the HashSet of identities
                                .Where(currentProp => !hashedIdentities.Contains(currentProp.Value.SqlName ?? currentProp.Key.Name))

                                .Select(currentProp => new KeyValuePair<string, PropertyInfo>((currentProp.Value.SqlName ?? currentProp.Key.Name),
                                    currentProp.Key))
                                .ToArray();

                            // Do the same for identity columns
                            IdentityColumns = sqlProps
                                // Filter by properties in the HashSet of identities
                                .Where(currentProp => hashedIdentities.Contains(currentProp.Value.SqlName ?? currentProp.Key.Name))

                                .Select(currentProp => new KeyValuePair<string, PropertyInfo>((currentProp.Value.SqlName ?? currentProp.Key.Name),
                                    currentProp.Key))
                                .ToArray();

                            // Build the select statement that pulls zero rows with all the columns for the current table
                            InsertSelectTopZero = "SELECT TOP 0 * FROM [" + TableName + "]";
                        }
                        finally
                        {
                            identitySet.Dispose();
                        }
                    }
                    finally
                    {
                        identityCommand.Dispose();
                    }
                }

                // Set the return fields from the 'columns' group
                columns = InsertColumns;
                identities = IdentityColumns;
                selectTopZero = InsertSelectTopZero;
            }
        }
        #endregion
    }

    internal interface ISqlAccess
    {
        /// <summary>
        /// Overrided name of table/property to match name in SQL
        /// </summary>
        string SqlName { get; }

        /// <summary>
        /// Whether a property represents a child object from another SQL table
        /// </summary>
        bool IsChild { get; }
    }

    /// <summary>
    /// Attribute parent namespace for decorating classes and properties to signify POCO objects/properties that match SQL CE tables/columns
    /// </summary>
    internal static class SqlAccess
    {
        [AttributeUsage(AttributeTargets.Property)]
        /// <summary>
        /// Attribute for decorating properties to signify POCO object properties that match SQL CE columns
        /// </summary>
        public sealed class PropertyAttribute : SqlAccessBase, ISqlAccess
        {
            public PropertyAttribute() { }
            public PropertyAttribute(string sqlName)
            {
                base._sqlName = sqlName;
            }
            public PropertyAttribute(bool isChild)
            {
                base._isChild = isChild;
            }
            public PropertyAttribute(string sqlName, bool isChild)
            {
                base._sqlName = sqlName;
                base._isChild = isChild;
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        /// <summary>
        /// Attribute for decorating classes to signify POCO objects that match SQL CE tables
        /// </summary>
        public sealed class ClassAttribute : SqlAccessBase, ISqlAccess
        {
            public ClassAttribute() { }
            public ClassAttribute(string sqlName)
            {
                base._sqlName = sqlName;
            }
        }

        public abstract class SqlAccessBase : Attribute
        {
            protected internal SqlAccessBase() { }

            /// <summary>
            /// Overrided name of table/property to match name in SQL
            /// </summary>
            public string SqlName
            {
                get
                {
                    return _sqlName;
                }
            }
            // Default to null
            protected string _sqlName = null;

            /// <summary>
            /// Whether a property represents a child object from another SQL table
            /// </summary>
            public bool IsChild
            {
                get
                {
                    return _isChild;
                }
            }
            // Default to false
            protected bool _isChild = false;
        }

        // find all the needed Types which are used more than once in GetParserExpression
        internal static readonly Type recordType = typeof(SqlCeDataReader);
        internal static readonly Type typeType = typeof(Type);
        internal static readonly Type stringType = typeof(string);
        internal static readonly Type counterType = typeof(GenericHolder<short>);
        internal static readonly Type boolType = typeof(bool);
        internal static readonly Type keyValueType = typeof(KeyValuePair<,>);
        internal static readonly Type objectType = typeof(object);

        #region reused method infoes for SqlAccessor
        internal static readonly MethodInfo IsNullRecordInfo = recordType
            .GetMethod("IsDBNull",
                BindingFlags.Instance | BindingFlags.Public);

        internal static readonly MethodInfo GetOrdinalRecordInfo = recordType
            .GetMethod("GetOrdinal",
                BindingFlags.Instance | BindingFlags.Public);

        internal static readonly MethodInfo ConcatStringsInfo = stringType
            .GetMethod("Concat",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[]
                {
                    typeof(string[])
                },
                null);
        #endregion
    }
}