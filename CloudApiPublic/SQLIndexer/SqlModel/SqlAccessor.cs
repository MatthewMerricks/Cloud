//
// SqlAccessor.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.SQLProxies;
using Cloud.Static;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
        private static readonly Dictionary<string, KeyValuePair<Expression<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>> ResultParsers =
            new Dictionary<string, KeyValuePair<Expression<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>>();

        #region columns
        // Array of insertable columns (used to build DataTable for bulk insert or to find column names), set via FindInsertColumns
        private static KeyValuePair<string, PropertyInfo>[] InsertColumns = null;
        // Array of identity columns (used in addition to InsertableColumns for identity insert), set via FindInsertColumns
        private static KeyValuePair<string, PropertyInfo>[] IdentityColumns = null;
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
        #endregion

        #region has trigger checks

        private static bool HasBeforeDeleteTrigger = (typeof(T)).IsCastableTo(typeof(IBeforeDeleteTrigger));

        #endregion

        #region public static methods
        public static bool TrySelectScalar<TKey>(ISQLiteConnection connection, string select, out TKey result, ISQLiteTransaction transaction = null, IEnumerable selectParameters = null)
        {
            using (ISQLiteCommand scalarCommand = connection.CreateCommand())
            {
                scalarCommand.CommandText = select;
                
                if (selectParameters != null)
                {
                    foreach (object selectParameter in selectParameters)
                    {
                        ISQLiteParameter currentParameter = scalarCommand.CreateParameter();
                        scalarCommand.Parameters.Add(currentParameter);

                        if (selectParameter is Guid)
                        {
                            currentParameter.Value = ((Guid)selectParameter).ToByteArray();
                        }
                        else
                        {
                            currentParameter.Value = selectParameter;
                        }
                    }
                }

                if (transaction != null)
                {
                    scalarCommand.Transaction = transaction;
                }

                object responseObject = scalarCommand.ExecuteScalar();

                if (responseObject == null
                    || responseObject is DBNull)
                {
                    result = Helpers.DefaultForType<TKey>();
                    return false;
                }

                result = Helpers.ConvertTo<TKey>(responseObject);
                return true;
            }
        }

        /// <summary>
        /// Gets a single result set by provided select statement and yield returns records converted to the current generic type
        /// </summary>
        /// <param name="connection">Database to query</param>
        /// <param name="select">Select statement</param>
        /// <param name="includes">List of joined children with dot syntax for multiple levels deep</param>
        /// <returns>Yield-returned converted database results as current generic type</returns>
        public static IEnumerable<T> SelectResultSet(ISQLiteConnection connection, string select, IEnumerable<string> includes = null, ISQLiteTransaction transaction = null, IEnumerable selectParameters = null)
        {
            // grab the function that takes the values out of the current database row to produce the current generic type
            Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>> currentParser = GetResultParser(string.Empty,
                    includes == null ? new string[0] : ((includes as string[]) ?? includes.ToArray())).Value;

            // new command to run select
            ISQLiteCommand selectCommand = connection.CreateCommand();
            try
            {
                // set command to run as select statement
                selectCommand.CommandText = select;

                if (selectParameters != null)
                {
                    foreach (object selectParameter in selectParameters)
                    {
                        ISQLiteParameter currentParameter = selectCommand.CreateParameter();
                        selectCommand.Parameters.Add(currentParameter);

                        if (selectParameter is Guid)
                        {
                            currentParameter.Value = ((Guid)selectParameter).ToByteArray();
                        }
                        else
                        {
                            currentParameter.Value = selectParameter;
                        }
                    }
                }

                if (transaction != null)
                {
                    selectCommand.Transaction = transaction;
                }

                // execute select command as a reader
                ISQLiteDataReader selectResult = selectCommand.ExecuteReader(CommandBehavior.SingleResult);
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
        public static void InsertRows(ISQLiteConnection connection, IEnumerable<T> toInsert, bool identityInsert = false, ISQLiteTransaction transaction = null)
        {
            InsertRows<object>(connection, toInsert, identityInsert, transaction, returnLastIdentity: false);
        }

        /// <summary>
        /// Helper insert method for the public InsertRow(s) methods
        /// </summary>
        private static TKey InsertRows<TKey>(ISQLiteConnection connection, IEnumerable<T> toInsert, bool identityInsert, ISQLiteTransaction transaction, bool returnLastIdentity)
        {
            TKey toReturn = Helpers.DefaultForType<TKey>();

            // If list of rows to insert exists,
            // then insert list of rows
            if (toInsert != null)
            {
                // Pull name of table
                string insertTableName = TableName;

                // Group of fields to pull from 'columns' group
                KeyValuePair<string, PropertyInfo>[] columns;
                KeyValuePair<string, PropertyInfo>[] identities;

                // Fill in the 'columns' group fields
                FindInsertColumns(connection, insertTableName, out columns, out identities);

                using (ISQLiteCommand insertCommand = connection.CreateCommand())
                {
                    StringBuilder columnNames = null;
                    StringBuilder valueMarks = null;

                    List<KeyValuePair<ISQLiteParameter, PropertyInfo>> paramPairs = new List<KeyValuePair<ISQLiteParameter, PropertyInfo>>();

                    if (identityInsert)
                    {
                        foreach (KeyValuePair<string, PropertyInfo> identity in identities)
                        {
                            if (columnNames == null)
                            {
                                columnNames = new StringBuilder(identity.Key);
                                valueMarks = new StringBuilder("?");
                            }
                            else
                            {
                                columnNames.Append(",");
                                columnNames.Append(identity.Key);
                                valueMarks.Append(",?");
                            }

                            ISQLiteParameter identityParam = insertCommand.CreateParameter();
                            insertCommand.Parameters.Add(identityParam);
                            paramPairs.Add(new KeyValuePair<ISQLiteParameter, PropertyInfo>(identityParam, identity.Value));
                        }
                    }

                    foreach (KeyValuePair<string, PropertyInfo> column in columns)
                    {
                        if (columnNames == null)
                        {
                            columnNames = new StringBuilder(column.Key);
                            valueMarks = new StringBuilder("?");
                        }
                        else
                        {
                            columnNames.Append(",");
                            columnNames.Append(column.Key);
                            valueMarks.Append(",?");
                        }

                        ISQLiteParameter columnParam = insertCommand.CreateParameter();
                        insertCommand.Parameters.Add(columnParam);
                        paramPairs.Add(new KeyValuePair<ISQLiteParameter, PropertyInfo>(columnParam, column.Value));
                    }

                    StringBuilder insertStringBuilder = new StringBuilder("INSERT INTO ");
                    insertStringBuilder.Append(insertTableName);
                    insertStringBuilder.Append("(");
                    insertStringBuilder.Append(columnNames.ToString());
                    insertStringBuilder.Append(") VALUES(");
                    insertStringBuilder.Append(valueMarks.ToString());
                    insertStringBuilder.Append(")");

                    insertCommand.CommandText = insertStringBuilder.ToString();

                    T storeLast = null;
                    using (IEnumerator<T> insertEnumerator = toInsert.GetEnumerator())
                    {
                        bool lastInsert;
                        while (!(lastInsert = !insertEnumerator.MoveNext()) || storeLast != null)
                        {
                            if (storeLast != null)
                            {
                                if (lastInsert && returnLastIdentity)
                                {
                                    insertCommand.CommandText += ";SELECT last_insert_rowid()";
                                }

                                // Add a new row to the DataTable with all insertable column values set
                                foreach (KeyValuePair<ISQLiteParameter, PropertyInfo> currentParam in paramPairs)
                                {
                                    object valueToSet = currentParam.Value.GetValue(storeLast, index: null);

                                    if (valueToSet is Guid)
                                    {
                                        currentParam.Key.Value = ((Guid)valueToSet).ToByteArray();
                                    }
                                    else
                                    {
                                        currentParam.Key.Value = valueToSet;
                                    }
                                }

                                if (lastInsert && returnLastIdentity)
                                {
                                    toReturn = Helpers.ConvertTo<TKey>(insertCommand.ExecuteScalar());
                                }
                                else
                                {
                                    insertCommand.ExecuteNonQuery();
                                }
                            }

                            storeLast = (lastInsert
                                ? null
                                : insertEnumerator.Current);
                        }
                    }
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Calls InsertRows with a single row to insert and then selects the new identity and attempts to return it as the specified generic type
        /// </summary>
        /// <typeparam name="TKey">Type of identity to return</typeparam>
        /// <param name="connection">Database connection</param>
        /// <param name="toInsert">Object to insert into database</param>
        /// <returns>Returns the identity of the inserted row</returns>
        public static TKey InsertRow<TKey>(ISQLiteConnection connection, T toInsert, bool identityInsert = false, ISQLiteTransaction transaction = null)
        {
            // Call to InsertRows which actually does the database insert
            return InsertRows<TKey>(connection, new T[] { toInsert }, identityInsert, transaction, returnLastIdentity: true);
        }

        /// <summary>
        /// Calls InsertRows with a single row to insert but does not select the new identity, if any
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toInsert">Object to insert into database</param>
        public static void InsertRow(ISQLiteConnection connection, T toInsert, bool identityInsert = false, ISQLiteTransaction transaction = null)
        {
            // Call to InsertRows which actually does the database insert
            InsertRows<object>(connection, new T[] { toInsert }, identityInsert, transaction, returnLastIdentity: false);
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
                            .Where(currentProp => currentProp.Value.Any(currentAttrib => currentAttrib.Type != SqlAccess.FieldType.JoinedTable))
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
        /// <returns>Returns whether the row was found to be updated</returns>
        public static bool UpdateRow(ISQLiteConnection connection, T toUpdate, ISQLiteTransaction transaction = null)
        {
            IEnumerable<int> unableToFindIndexes;
            UpdateRows(connection, new T[] { toUpdate }, out unableToFindIndexes, transaction);
            return !(unableToFindIndexes ?? Enumerable.Empty<int>()).Any();
        }

        /// <summary>
        /// Updates rows in the database corresponding to the provided list of objects; rows are modified where columns differ by searching the primary key values
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toUpdate">List of objects corresponding to rows to update</param>
        /// <param name="unableToFindIndexes">(output) List of indexes that were not found to update correlating to the index in the list of objects to update</param>
        public static void UpdateRows(ISQLiteConnection connection, IEnumerable<T> toUpdate, out IEnumerable<int> unableToFindIndexes, ISQLiteTransaction transaction = null)
        {
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

            // Fill in the 'columns' group fields
            FindInsertColumns(connection, updateTableName, out columns, out identities);

            // build an array of the types of properties which will be checked for update
            Type[] valueTypes = columns.Select(currentValue => currentValue.Value.PropertyType).ToArray();

            // start a new list to store indexes which were not found to update
            List<int> unableToFindList = new List<int>();

            // create a new database command that will pull the existing rows as updatable
            using (ISQLiteCommand updateCommand = connection.CreateCommand())
            {
                List<KeyValuePair<ISQLiteParameter, PropertyInfo>> paramPairs = new List<KeyValuePair<ISQLiteParameter, PropertyInfo>>();

                StringBuilder updateStringBuilder = new StringBuilder("UPDATE ");
                updateStringBuilder.Append(updateTableName);

                bool firstColumn = true;

                foreach (KeyValuePair<string, PropertyInfo> column in columns)
                {
                    if (firstColumn)
                    {
                        updateStringBuilder.Append(" SET ");

                        firstColumn = false;
                    }
                    else
                    {
                        updateStringBuilder.Append(", ");
                    }

                    updateStringBuilder.Append(column.Key);
                    updateStringBuilder.Append(" = ?");

                    ISQLiteParameter columnParam = updateCommand.CreateParameter();
                    updateCommand.Parameters.Add(columnParam);
                    paramPairs.Add(new KeyValuePair<ISQLiteParameter, PropertyInfo>(columnParam, column.Value));
                }

                firstColumn = true;

                foreach (KeyValuePair<string, PropertyInfo> identity in identities)
                {
                    if (firstColumn)
                    {
                        updateStringBuilder.Append(" WHERE ");

                        firstColumn = false;
                    }
                    else
                    {
                        updateStringBuilder.Append(" AND ");
                    }

                    updateStringBuilder.Append(identity.Key);
                    updateStringBuilder.Append(" = ?");

                    ISQLiteParameter identityParam = updateCommand.CreateParameter();
                    updateCommand.Parameters.Add(identityParam);
                    paramPairs.Add(new KeyValuePair<ISQLiteParameter, PropertyInfo>(identityParam, identity.Value));
                }

                updateStringBuilder.Append("; SELECT changes()");

                updateCommand.CommandText = updateStringBuilder.ToString();

                if (transaction != null)
                {
                    updateCommand.Transaction = transaction;
                }
                
                // define a counter to keep track of which object is in the process of being updated
                int updateIndex = 0;

                // loop through all the objects to update
                foreach (T currentUpdate in toUpdate)
                {
                    // Add a new row to the DataTable with all insertable column values set
                    foreach (KeyValuePair<ISQLiteParameter, PropertyInfo> currentParam in paramPairs)
                    {
                        object valueToSet = currentParam.Value.GetValue(currentUpdate, index: null);

                        if (valueToSet is Guid)
                        {
                            currentParam.Key.Value = ((Guid)valueToSet).ToByteArray();
                        }
                        else
                        {
                            currentParam.Key.Value = valueToSet;
                        }
                    }

                    switch (Convert.ToInt32(updateCommand.ExecuteScalar()))
                    {
                        case 0:
                            unableToFindList.Add(updateIndex);
                            break;

                        case 1:
                            // normal case, one set of primary keys with one row affected
                            break;

                        default:
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "UPDATE affected more than 1 row for table " + updateTableName);
                    }

                    // incremement the counter of objects that have been checked to update
                    updateIndex++;
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
        public static bool DeleteRow(ISQLiteConnection connection, T toDelete, ISQLiteTransaction transaction = null)
        {
            IEnumerable<int> unableToFindIndexes;
            DeleteRows(connection, new T[] { toDelete }, out unableToFindIndexes, transaction);
            return !(unableToFindIndexes ?? Enumerable.Empty<int>()).Any();
        }

        /// <summary>
        /// Deletes rows in the database that correspond to a given list of objects
        /// </summary>
        /// <param name="connection">Database connection</param>
        /// <param name="toDelete">List of objects to delete</param>
        /// <param name="unableToFindIndexes">(output) List of indexes that were not found to delete correlating to the index in the list of objects to delete</param>
        /// <param name="caseSensitive">Whether the primary key will be searched as case-sensitive</param>
        public static void DeleteRows(ISQLiteConnection connection, IEnumerable<T> toDelete, out IEnumerable<int> unableToFindIndexes, ISQLiteTransaction transaction = null)
        {
            // if there are no rows to delete,
            // then there were no indexes that weren't found, return
            if (toDelete == null
                || !toDelete.Any())
            {
                unableToFindIndexes = Enumerable.Empty<int>();
                return;
            }

            // get the current table name
            string deleteTableName = TableName;

            // Group of fields to pull from 'columns' group
            KeyValuePair<string, PropertyInfo>[] columns;
            KeyValuePair<string, PropertyInfo>[] identities;

            // Fill in the 'columns' group fields
            FindInsertColumns(connection, deleteTableName, out columns, out identities);

            // build an array of the types of properties which will be checked for delete
            Type[] valueTypes = columns.Select(currentValue => currentValue.Value.PropertyType).ToArray();

            // start a new list to store indexes which were not found to delete
            List<int> unableToFindList = new List<int>();

            // right before actually firing all the delete commands, check for and possibly run any before deletion triggers on all objects
            // TODO: figure out how to write all these triggers properly in the database instead to remove this functionality from C#
            if (HasBeforeDeleteTrigger)
            {
                foreach (T currentDelete in toDelete)
                {
                    ((IBeforeDeleteTrigger)currentDelete).BeforeDelete(connection, transaction);
                }
            }

            // create a new database command that will delete rows
            using (ISQLiteCommand deleteCommand = connection.CreateCommand())
            {
                List<KeyValuePair<ISQLiteParameter, PropertyInfo>> paramPairs = new List<KeyValuePair<ISQLiteParameter, PropertyInfo>>();

                StringBuilder deleteStringBuilder = new StringBuilder("DELETE FROM ");
                deleteStringBuilder.Append(deleteTableName);

                bool firstColumn = true;

                foreach (KeyValuePair<string, PropertyInfo> identity in identities)
                {
                    if (firstColumn)
                    {
                        deleteStringBuilder.Append(" WHERE ");

                        firstColumn = false;
                    }
                    else
                    {
                        deleteStringBuilder.Append(" AND ");
                    }

                    deleteStringBuilder.Append(identity.Key);
                    deleteStringBuilder.Append(" = ?");

                    ISQLiteParameter identityParam = deleteCommand.CreateParameter();
                    deleteCommand.Parameters.Add(identityParam);
                    paramPairs.Add(new KeyValuePair<ISQLiteParameter, PropertyInfo>(identityParam, identity.Value));
                }

                deleteStringBuilder.Append("; SELECT changes()");

                deleteCommand.CommandText = deleteStringBuilder.ToString();

                if (transaction != null)
                {
                    deleteCommand.Transaction = transaction;
                }

                // define a counter to keep track of which object is in the process of being deleted
                int deleteIndex = 0;

                // loop through all the objects to delete
                foreach (T currentUpdate in toDelete)
                {
                    // Add a new row to the DataTable with all insertable column values set
                    foreach (KeyValuePair<ISQLiteParameter, PropertyInfo> currentParam in paramPairs)
                    {
                        object valueToSet = currentParam.Value.GetValue(currentUpdate, index: null);

                        if (valueToSet is Guid)
                        {
                            currentParam.Key.Value = ((Guid)valueToSet).ToByteArray();
                        }
                        else
                        {
                            currentParam.Key.Value = valueToSet;
                        }
                    }

                    switch (Convert.ToInt32(deleteCommand.ExecuteScalar()))
                    {
                        case 0:
                            unableToFindList.Add(deleteIndex);
                            break;

                        case 1:
                            // normal case, one set of primary keys with one row affected
                            break;

                        default:
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "DELETE affected more than 1 row for table " + deleteTableName);
                    }

                    // incremement the counter of objects that have been checked to delete
                    deleteIndex++;
                }
            }

            // set the output list of unable to find indexes
            unableToFindIndexes = unableToFindList;
        }
        #endregion

        #region private static method
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
        private static KeyValuePair<Expression<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> GetResultParser(string parentName, params string[] includes)
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
                KeyValuePair<Expression<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> currentParser;
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
                        .Where(sqlProp => sqlProp.Value.Value.Type != SqlAccess.FieldType.JoinedTable
                            || includeSet.Contains(sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name))

                        // For each property, return the MemberAssignment
                        .Select(sqlProp =>
                            
                            // Create a MemberAssignment to set the current property from either the database column or by constructing an inner object
                            Expression.Bind(
                                // Current property to set
                                sqlProp.Value.Key,
                                
                                // If the current property is a child object,
                                // then invoke a recursed inner expression that will construct the inner object
                                (sqlProp.Value.Value.Type == SqlAccess.FieldType.JoinedTable

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
                    Expression<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> parserExpression = Expression.Lambda<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>(

                        // Construction of current generic typed object
                        returnPairExpression,
                        
                        // Parameter one of three: [parent name]
                        parentExpression,

                        // Parameter two of three: current database row
                        selectedRow,

                        // Parameter three of three: null column counter
                        nullCounterExpression);

                    // Join the parser expression with its compiled method into a KeyValuePair, also sets the value to return on this function call
                    currentParser = new KeyValuePair<Expression<Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, ISQLiteDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>(
                        parserExpression,
                        parserExpression.Compile());

                    // Add the compiled and uncompiled parser expression into the dictionary
                    ResultParsers.Add(currentParserKey, currentParser);
                }

                // Return the compiled and uncompiled parser expression
                return currentParser;
            }
        }

        // Gets and sets as necessary, lists of the updatable columns and corresponding PropertyInfoes to access the values in the current generic typed objects
        private static void FindInsertColumns(ISQLiteConnection connection, string TableName, out KeyValuePair<string, PropertyInfo>[] columns, out KeyValuePair<string, PropertyInfo>[] identities)
        {
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
                    ISQLiteCommand identityCommand = connection.CreateCommand();
                    try
                    {
                        // Set the command text to query identity columns based on a table with the current table name
                        identityCommand.CommandText =
                            "PRAGMA table_info(" + TableName + ")";

                        // Create a result set of the selected column names of identity columns
                        ISQLiteDataReader identityReader = identityCommand.ExecuteReader(CommandBehavior.SingleResult);
                        try
                        {
                            Dictionary<string, int> columnOrdinals = new Dictionary<string, int>();

                            // loop through columns
                            while (identityReader.Read())
                            {
                                // For each identity column name in the result set
                                if (Convert.ToBoolean(identityReader["pk"]))
                                {
                                    // Add the current column name to the HashSet
                                    hashedIdentities.Add(Convert.ToString(identityReader["name"]));
                                }
                            }

                            // Set the PropertyInfoes for the current generic typed object of Sql columns which are not identities
                            IEnumerable<KeyValuePair<PropertyInfo, ISqlAccess>> sqlProps = CurrentGenericType

                                // Get properties which have one and only one ISqlAccess attribute
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<ISqlAccess>>(currentProp,
                                    currentProp.GetCustomAttributes(typeof(SqlAccess.PropertyAttribute), true)
                                        .Cast<ISqlAccess>()))
                                .Where(currentProp => currentProp.Value.Any(currentAttrib => currentAttrib.Type == SqlAccess.FieldType.Normal))
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
                        }
                        finally
                        {
                            identityReader.Dispose();
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
            }
        }
        #endregion
    }

    internal interface IBeforeDeleteTrigger
    {
        void BeforeDelete(ISQLiteConnection sqlConn, ISQLiteTransaction sqlTran = null);
    }

    internal interface ISqlAccess
    {
        /// <summary>
        /// Overrided name of table/property to match name in SQL
        /// </summary>
        string SqlName { get; }

        /// <summary>
        /// Whether a property represents a child object from another SQL table or if it is readonly
        /// </summary>
        Cloud.SQLIndexer.SqlModel.SqlAccess.FieldType Type { get; }
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
            public PropertyAttribute(FieldType type)
            {
                base._type = type;
            }
            public PropertyAttribute(string sqlName, FieldType type)
            {
                base._sqlName = sqlName;
                base._type = type;
            }
        }

        /// <summary>
        /// Whether a property represents a child object from another SQL table or if it is readonly
        /// </summary>
        public enum FieldType : byte
        {
            /// <summary>
            /// Normal, read and writable fields
            /// </summary>
            Normal,
            /// <summary>
            /// Fields which should never be inserted nor updated, such as calculated fields
            /// </summary>
            ReadOnly,
            /// <summary>
            /// Objects which represent a relationship with another table, such as through foreign key constraints
            /// </summary>
            JoinedTable
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
            /// Whether a property represents a child object from another SQL table or if it is readonly
            /// </summary>
            public FieldType Type
            {
                get
                {
                    return _type;
                }
            }
            // Default to normal
            protected FieldType _type = FieldType.Normal;
        }

        // find all the needed Types which are used more than once in GetParserExpression
        internal static readonly Type recordType = typeof(ISQLiteDataReader);
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