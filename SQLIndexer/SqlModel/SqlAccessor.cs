//
// SqlAccessor.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using ErikEJ.SqlCe;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SQLIndexer.SqlModel
{
    public static class SqlAccessor<T> where T : class
    {
        public static IEnumerable<T> SelectResults(SqlCeConnection connection, string select, IEnumerable<string> includes = null, bool caseSensitive = true)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            SqlCeCommand selectCommand = connection.CreateCommand();
            try
            {
                selectCommand.CommandText = select;
                SqlCeDataReader selectResult = selectCommand.ExecuteReader(CommandBehavior.SingleResult);
                try
                {
                    Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>> currentParser = GetResultParser(string.Empty,
                            includes == null ? new string[0] : ((includes as string[]) ?? includes.ToArray())).Value;

                    while (selectResult.Read())
                    {
                        KeyValuePair<T, bool> currentRow = currentParser(string.Empty, selectResult, new GenericHolder<short>(0));

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

        private static Expression GetParserExpression(string parentName, string[] includes)
        {
            return GetResultParser(parentName, includes).Key;
        }

        private static KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> GetResultParser(string parentName, params string[] includes)
        {
            lock (ResultParsers)
            {
                KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> currentParser;

                HashSet<string> includeSet = new HashSet<string>(includes ?? Enumerable.Empty<string>());

                string currentParserKey = string.Join(" ",
                    includeSet
                    .OrderBy(currentInclude => currentInclude)
                    .ToArray());

                if (!ResultParsers.TryGetValue(currentParserKey, out currentParser))
                {
                    Type recordType = typeof(SqlCeDataReader);
                    Type typeType = typeof(Type);
                    Type stringType = typeof(string);
                    Type counterType = typeof(GenericHolder<short>);
                    Type boolType = typeof(bool);
                    Type keyValueType = typeof(KeyValuePair<,>);

                    IEnumerable<KeyValuePair<ParameterExpression, KeyValuePair<PropertyInfo, SqlAccess>>> sqlProps = CurrentType
                        .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                        .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<SqlAccess>>(currentProp,
                            currentProp
                                .GetCustomAttributes(typeof(SqlAccess), true)
                                .Cast<SqlAccess>()))
                        .Where(currentProp => currentProp.Value.Any())
                        .Select(currentProp => new KeyValuePair<ParameterExpression, KeyValuePair<PropertyInfo, SqlAccess>>(
                            Expression.Parameter(
                                keyValueType.MakeGenericType(currentProp.Key.PropertyType, boolType)),
                            new KeyValuePair<PropertyInfo, SqlAccess>(currentProp.Key,
                                currentProp.Value.Single())));

                    ParameterExpression selectedRow = Expression.Parameter(recordType);
                    ParameterExpression parentExpression = Expression.Parameter(stringType);
                    ParameterExpression nullCounterExpression = Expression.Parameter(counterType);

                    NewExpression newResult = Expression.New(CurrentType);

                    PropertyInfo indexProp = recordType
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Select(currentProp => new KeyValuePair<PropertyInfo, ParameterInfo[]>(currentProp, currentProp.GetIndexParameters()))
                        .Single(currentProp => currentProp.Value != null
                            && currentProp.Value.Length == 1
                            && currentProp.Value[0].ParameterType == stringType)
                        .Key;

                    short propCounter = 0;

                    IEnumerable<MemberBinding> initMembers = sqlProps
                        .Where(sqlProp => !sqlProp.Value.Value.IsChild
                            || includeSet.Contains(sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name))
                        .Select(sqlProp => Expression.Bind(sqlProp.Value.Key,
                            (sqlProp.Value.Value.IsChild
                                ? (Expression)Expression.Invoke(
                                    Expression.Lambda(
                                        Expression.Condition(
                                            Expression.Property(sqlProp.Key,
                                                keyValueType.MakeGenericType(sqlProp.Value.Key.PropertyType, boolType)
                                                    .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)),
                                            Expression.Constant(null, sqlProp.Value.Key.PropertyType),
                                            Expression.Property(sqlProp.Key,
                                                keyValueType.MakeGenericType(sqlProp.Value.Key.PropertyType, boolType)
                                                    .GetProperty("Key", BindingFlags.Instance | BindingFlags.Public))),
                                        sqlProp.Key),
                                    Expression.Invoke((Expression)typeof(SqlAccessor<>)
                                        .MakeGenericType(sqlProp.Value.Key.PropertyType)
                                        .GetMethod("GetParserExpression", BindingFlags.Static | BindingFlags.NonPublic)
                                        .Invoke(null,
                                            new object[]
                                            {
                                                parentName + sqlProp.Value.Value.SqlName + ".",
                                                includeSet.Where(currentInclude => currentInclude.StartsWith(sqlProp.Value.Value.SqlName + "."))
                                                    .Select(currentInclude => currentInclude.Substring(sqlProp.Value.Value.SqlName.Length + 1))
                                                    .ToArray()
                                            }),
                                        Expression.Call(ConcatStringsInfo,
                                                            Expression.NewArrayInit(stringType,
                                                                parentExpression,
                                                                Expression.Constant((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name) + ".",
                                                                    stringType))),
                                        selectedRow,
                                        Expression.New(typeof(GenericHolder<short>).GetConstructor(new Type[] { typeof(short) }),
                                            Expression.Constant((short)0))))
                                : ((propCounter = (short)(propCounter + (short)1)) > short.MinValue
                                    ? Expression.Call(Helpers.ConvertToInfo
                                            .MakeGenericMethod(sqlProp.Value.Key.PropertyType),
                                        Expression.Condition(
                                            Expression.Call(selectedRow,
                                                IsNullRecordInfo,
                                                Expression.Call(selectedRow,
                                                    GetOrdinalRecordInfo,
                                                    Expression.Call(ConcatStringsInfo,
                                                        Expression.NewArrayInit(stringType,
                                                            parentExpression,
                                                            Expression.Constant((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name),
                                                                stringType))))),
                                            Expression.Condition(
                                                Expression.GreaterThan(
                                                    Expression.AddAssign(
                                                        Expression.Property(nullCounterExpression, GenericHolder<short>.ValueInfo),
                                                        Expression.Constant((short)1)),
                                                    Expression.Constant(short.MinValue)),
                                                Expression.Convert(
                                                    Expression.Call(Helpers.DefaultForTypeInfo
                                                        .MakeGenericMethod(sqlProp.Value.Key.PropertyType)),
                                                    typeof(object)),
                                                Expression.Constant(null, typeof(object))),
                                            Expression.MakeIndex(selectedRow,
                                                indexProp,
                                                new Expression[]
                                                    {
                                                        Expression.Call(ConcatStringsInfo,
                                                            Expression.NewArrayInit(stringType,
                                                                parentExpression,
                                                                Expression.Constant((sqlProp.Value.Value.SqlName ?? sqlProp.Value.Key.Name),
                                                                    stringType)))
                                                    })))
                                    : null))));

                    MemberInitExpression initResult = Expression.MemberInit(newResult, initMembers);

                    Type[] keyValueTypes = new Type[]
                    {
                        initResult.Type,
                        boolType
                    };

                    NewExpression returnPairExpression = Expression.New(keyValueType
                            .MakeGenericType(keyValueTypes)
                            .GetConstructor(keyValueTypes),
                        initResult,
                        Expression.Equal(nullCounterExpression,
                            Expression.Constant(new GenericHolder<short>(propCounter))));

                    Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>> parserExpression = Expression.Lambda<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>(
                        returnPairExpression,
                        parentExpression,
                        selectedRow,
                        nullCounterExpression);

                    currentParser = new KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>(
                        parserExpression,
                        parserExpression.Compile());

                    ResultParsers.Add(currentParserKey, currentParser);
                }

                return currentParser;
            }
        }
        private static readonly Dictionary<string, KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>> ResultParsers =
            new Dictionary<string, KeyValuePair<Expression<Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>, Func<string, SqlCeDataReader, GenericHolder<short>, KeyValuePair<T, bool>>>>();

        private static void FindInsertColumns(SqlCeConnection connection, string TableName, out DataColumn[] columns, out PropertyInfo[] values, out string selectTopZero)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            lock (InsertLocker)
            {
                if (InsertColumns == null)
                {
                    HashSet<string> hashedIdentities = new HashSet<string>();

                    SqlCeCommand identityCommand = connection.CreateCommand();
                    try
                    {
                        identityCommand.CommandText = "SELECT [INFORMATION_SCHEMA].[COLUMNS].[COLUMN_NAME] " +
                            "FROM [INFORMATION_SCHEMA].[COLUMNS] " +
                            "WHERE [INFORMATION_SCHEMA].[COLUMNS].[TABLE_NAME] = '" + TableName.Replace("'", "''") + "' " +
                            "AND [INFORMATION_SCHEMA].[COLUMNS].[AUTOINC_SEED] IS NOT NULL";
                        SqlCeResultSet identitySet = identityCommand.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Insensitive);
                        try
                        {
                            foreach (SqlCeUpdatableRecord identityRecord in identitySet.OfType<SqlCeUpdatableRecord>())
                            {
                                hashedIdentities.Add(identityRecord.GetString(0));
                            }

                            IEnumerable<KeyValuePair<PropertyInfo, SqlAccess>> sqlProps = CurrentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<SqlAccess>>(currentProp,
                                    currentProp.GetCustomAttributes(typeof(SqlAccess), true)
                                        .Cast<SqlAccess>()))
                                .Where(currentProp => currentProp.Value.Any(currentAttrib => !currentAttrib.IsChild))
                                .Select(currentProp => new KeyValuePair<PropertyInfo, SqlAccess>(currentProp.Key, currentProp.Value.Single()))
                                .Where(currentProp => !hashedIdentities.Contains(currentProp.Value.SqlName ?? currentProp.Key.Name));

                            InsertColumns = sqlProps.Select(currentProp => new DataColumn((currentProp.Value.SqlName ?? currentProp.Key.Name),
                                    (Nullable.GetUnderlyingType(currentProp.Key.PropertyType) ?? currentProp.Key.PropertyType)))
                                .ToArray();

                            InsertValues = sqlProps.Select(currentProp => currentProp.Key).ToArray();

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

                columns = InsertColumns;
                values = InsertValues;
                selectTopZero = InsertSelectTopZero;
            }
        }

        public static void InsertRows(SqlCeConnection connection, IEnumerable<T> toInsert)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            if (toInsert != null)
            {
                DataColumn[] columns;
                PropertyInfo[] values;
                string insertTableName = TableName;
                string selectTopZero;

                FindInsertColumns(connection, insertTableName, out columns, out values, out selectTopZero);

                bool foundMultiple = false;
                T saveSingle;
                using (IEnumerator<T> singleEnumerator = toInsert.GetEnumerator())
                {
                    if (singleEnumerator.MoveNext())
                    {
                        saveSingle = singleEnumerator.Current;

                        if (singleEnumerator.MoveNext())
                        {
                            foundMultiple = true;
                        }
                    }
                    else
                    {
                        saveSingle = null;
                    }
                }

                if (foundMultiple)
                {
                    using (SqlCeBulkCopy bulkCopy = new SqlCeBulkCopy(connection, SqlCeBulkCopyOptions.KeepNulls))
                    {
                        DataTable insertTable;
                        if ((insertTable = (InsertColumns.Length == 0
                            ? null
                            : InsertColumns[0].Table)) == null)
                        {
                            insertTable = new DataTable();

                            insertTable.Columns.AddRange(InsertColumns);
                        }

                        foreach (T currentInsert in toInsert)
                        {
                            insertTable.Rows.Add(values.Select(currentValue => currentValue.GetValue(currentInsert, null) ?? DBNull.Value).ToArray());
                        }

                        bulkCopy.DestinationTableName = insertTableName;
                        bulkCopy.WriteToServer(insertTable);
                    }
                }
                else if (saveSingle != null)
                {
                    SqlCeCommand singleCommand = connection.CreateCommand();
                    try
                    {
                        singleCommand.CommandText = selectTopZero;
                        SqlCeResultSet singleResult = singleCommand.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable);
                        try
                        {

                            SqlCeUpdatableRecord singleUpdate = singleResult.CreateRecord();
                            for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                            {
                                singleUpdate.SetValue(singleResult.GetOrdinal(columns[columnIndex].ColumnName),
                                    values[columnIndex].GetValue(saveSingle, null) ?? DBNull.Value);
                            }

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
                }
            }
        }

        public static TKey InsertRow<TKey>(SqlCeConnection connection, T toInsert)
        {
            InsertRows(connection, new T[] { toInsert });

            SqlCeCommand identityCommand = connection.CreateCommand();
            try
            {
                identityCommand.CommandText = "SELECT @@IDENTITY";
                return Helpers.ConvertTo<TKey>(identityCommand.ExecuteScalar());
            }
            finally
            {
                identityCommand.Dispose();
            }
        }

        public static void InsertRow(SqlCeConnection connection, T toInsert)
        {
            InsertRows(connection, new T[] { toInsert });
        }

        private static DataColumn[] InsertColumns = null;
        private static PropertyInfo[] InsertValues = null;
        private static string InsertSelectTopZero = null;
        private static readonly object InsertLocker = new object();

        private static string TableName
        {
            get
            {
                if (_tableName == null)
                {
                    SqlAccess insertAccess = CurrentType.GetCustomAttributes(typeof(SqlAccess), true).Cast<SqlAccess>().SingleOrDefault();

                    if (insertAccess != null
                        && insertAccess.SqlName != null)
                    {
                        _tableName = insertAccess.SqlName;
                    }
                    else
                    {
                        _tableName = CurrentType.Name;
                    }
                }
                return _tableName;
            }
        }
        private static string _tableName = null;
        private static readonly object TableNameLocker = new object();

        private static readonly Type CurrentType = typeof(T);

        public static string GetSelectColumns(string child = null, string collectionName = null)
        {
            string selectTableName = TableName;

            KeyValuePair<string, string> selectKey = new KeyValuePair<string, string>((child ?? string.Empty), (collectionName ?? string.Empty));

            lock (SelectColumns)
            {
                string foundSelect;
                if (!SelectColumns.TryGetValue(selectKey, out foundSelect))
                {
                    foundSelect = string.Join(", ",
                        CurrentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<SqlAccess>>(currentProp, currentProp.GetCustomAttributes(typeof(SqlAccess), true).Cast<SqlAccess>()))
                            .Where(currentProp => currentProp.Value.Any(currentAttrib => !currentAttrib.IsChild))
                            .Select(currentProp => new KeyValuePair<PropertyInfo, SqlAccess>(currentProp.Key, currentProp.Value.Single()))
                            .Select(currentProp =>
                                (string.IsNullOrEmpty(collectionName)
                                    ? "[" + selectTableName + "]"
                                    : collectionName) +
                                    ".[" + (currentProp.Value.SqlName ?? currentProp.Key.Name) + "]" +
                                (string.IsNullOrEmpty(child)
                                ? string.Empty
                                : " AS [" + child + "." + (currentProp.Value.SqlName ?? currentProp.Key.Name) + "]"))
                            .ToArray());

                    SelectColumns.Add(selectKey, foundSelect);
                }

                return foundSelect;
            }
        }

        private static readonly Dictionary<KeyValuePair<string, string>, string> SelectColumns = new Dictionary<KeyValuePair<string, string>, string>();

        private static readonly MethodInfo ConcatStringsInfo = typeof(string)
            .GetMethod("Concat",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[]
                {
                    typeof(string[])
                },
                null);

        public static bool UpdateRow(SqlCeConnection connection, T toUpdate, bool searchCaseSensitive = true, bool updateCaseSensitive = true)
        {
            IEnumerable<int> unableToFindIndexes;
            UpdateRows(connection, new T[] { toUpdate }, out unableToFindIndexes, searchCaseSensitive, updateCaseSensitive);
            return !(unableToFindIndexes ?? Enumerable.Empty<int>()).Any();
        }

        public static void UpdateRows(SqlCeConnection connection, IEnumerable<T> toUpdate, out IEnumerable<int> unableToFindIndexes, bool searchCaseSensitive = true, bool updateCaseSensitive = true)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            if (toUpdate == null)
            {
                unableToFindIndexes = Enumerable.Empty<int>();
                return;
            }

            DataColumn[] columns;
            PropertyInfo[] values;
            string updateTableName = TableName;
            string selectTopZero;

            FindInsertColumns(connection, updateTableName, out columns, out values, out selectTopZero);

            Type[] valueTypes = values.Select(currentValue => currentValue.PropertyType).ToArray();

            string primaryKeyName;
            PropertyInfo[] keyValues = GetPrimaryKeyColumnValues(connection, updateTableName, selectTopZero, out primaryKeyName);

            List<int> unableToFindList = new List<int>();

            using (SqlCeCommand retrieveExisting = new SqlCeCommand())
            {
                retrieveExisting.CommandType = CommandType.TableDirect;
                retrieveExisting.CommandText = updateTableName;
                retrieveExisting.Connection = connection;
                retrieveExisting.IndexName = primaryKeyName;

                using (SqlCeResultSet retrievedExisting = retrieveExisting.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable
                    | (searchCaseSensitive
                        ? ResultSetOptions.Sensitive
                        : ResultSetOptions.Insensitive)))
                {
                    int updateIndex = 0;
                    foreach (T currentUpdate in toUpdate)
                    {
                        if (retrievedExisting.Seek(DbSeekOptions.FirstEqual,
                            keyValues.Select(currentKey => currentKey.GetValue(currentUpdate, null))
                                .ToArray()))
                        {
                            if (retrievedExisting.Read())
                            {
                                for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                                {
                                    int columnOrdinal = retrievedExisting.GetOrdinal(columns[columnIndex].ColumnName);
                                    object valueToUpdate = values[columnIndex].GetValue(currentUpdate, null);

                                    bool foundDifference = false;

                                    object originalValue = retrievedExisting.GetValue(columnOrdinal);
                                    if (originalValue == null
                                        || originalValue is DBNull)
                                    {
                                        if (valueToUpdate != null)
                                        {
                                            foundDifference = true;
                                        }
                                    }
                                    else if (valueToUpdate == null
                                        || valueToUpdate is DBNull)
                                    {
                                        foundDifference = true;
                                    }
                                    else if (originalValue.GetType().Equals(valueTypes[columnIndex]))
                                    {
                                        if (originalValue is string
                                            && !updateCaseSensitive)
                                        {
                                            if (!((string)originalValue).Equals((string)valueToUpdate, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                foundDifference = true;
                                            }
                                        }
                                        else if (!object.Equals(originalValue, valueToUpdate))
                                        {
                                            foundDifference = true;
                                        }
                                    }
                                    else
                                    {
                                        originalValue = Helpers.ConvertTo(originalValue, valueTypes[columnIndex]);
                                        if (originalValue is string
                                            && !updateCaseSensitive)
                                        {
                                            if (!((string)originalValue).Equals((string)valueToUpdate, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                foundDifference = true;
                                            }
                                        }
                                        else if (!object.Equals(originalValue, valueToUpdate))
                                        {
                                            foundDifference = true;
                                        }
                                    }

                                    if (foundDifference)
                                    {
                                        retrievedExisting.SetValue(columnOrdinal,
                                            valueToUpdate ?? DBNull.Value);
                                    }
                                }

                                retrievedExisting.Update();
                            }
                            else
                            {
                                unableToFindList.Add(updateIndex);
                            }
                        }
                        else
                        {
                            unableToFindList.Add(updateIndex);
                        }

                        updateIndex++;
                    }
                }
            }

            unableToFindIndexes = unableToFindList;
        }

        private static PropertyInfo[] GetPrimaryKeyColumnValues(SqlCeConnection connection, string tableName, string selectTopZero, out string primaryKeyName)
        {
            lock (PrimaryKeyOrdinalsLocker)
            {
                if (primaryKeyValues == null)
                {
                    SqlCeCommand ordinalCommand = connection.CreateCommand();
                    try
                    {
                        ordinalCommand.CommandText = "SELECT [INFORMATION_SCHEMA].[INDEXES].[ORDINAL_POSITION], " +
                            "[INFORMATION_SCHEMA].[INDEXES].[INDEX_NAME], " +
                            "[INFORMATION_SCHEMA].[INDEXES].[COLUMN_NAME] " +
                            "FROM [INFORMATION_SCHEMA].[INDEXES] " +
                            "WHERE [INFORMATION_SCHEMA].[INDEXES].[TABLE_NAME] = '" + tableName.Replace("'", "''") + "' " +
                            "AND [INFORMATION_SCHEMA].[INDEXES].[PRIMARY_KEY] = 1";
                        SqlCeResultSet ordinals = ordinalCommand.ExecuteResultSet(ResultSetOptions.Insensitive | ResultSetOptions.Scrollable);

                        try
                        {
                            string[] indexNames = ordinals.Cast<SqlCeUpdatableRecord>()
                                .ToArray()
                                .OrderBy(currentOrdinal => currentOrdinal.GetInt32(ordinals.GetOrdinal("ORDINAL_POSITION")))
                                .Select(currentOrdinal => new KeyValuePair<string, string>(currentOrdinal.GetString(ordinals.GetOrdinal("COLUMN_NAME")),
                                    (primaryKeyIndexName = currentOrdinal.GetString(ordinals.GetOrdinal("INDEX_NAME")))).Key)
                                .ToArray();

                            SqlCeCommand selectZero = connection.CreateCommand();
                            try
                            {
                                selectZero.CommandText = selectTopZero;
                                SqlCeResultSet zeroRows = selectZero.ExecuteResultSet(ResultSetOptions.Scrollable);

                                try
                                {
                                    Dictionary<string, int> primaryKeyColumnNames = new Dictionary<string, int>(indexNames.Length, StringComparer.InvariantCultureIgnoreCase);

                                    for (int nameIndex = 0; nameIndex < indexNames.Length; nameIndex++)
                                    {
                                        primaryKeyColumnNames.Add(indexNames[nameIndex], nameIndex);
                                    }

                                    primaryKeyValues = CurrentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                        .Select(currentProp => new KeyValuePair<PropertyInfo, IEnumerable<SqlAccess>>(currentProp,
                                            currentProp.GetCustomAttributes(typeof(SqlAccess), true)
                                                .Cast<SqlAccess>()))
                                        .Where(currentProp => currentProp.Value.Any())
                                        .Select(currentProp => new KeyValuePair<PropertyInfo, SqlAccess>(currentProp.Key, currentProp.Value.Single()))
                                        .Where(currentProp => primaryKeyColumnNames.ContainsKey(currentProp.Value.SqlName ?? currentProp.Key.Name))
                                        .OrderBy(currentProp => primaryKeyColumnNames[currentProp.Value.SqlName ?? currentProp.Key.Name])
                                        .Select(currentProp => currentProp.Key)
                                        .ToArray();
                                }
                                finally
                                {
                                    zeroRows.Dispose();
                                }
                            }
                            finally
                            {
                                selectZero.Dispose();
                            }
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
                primaryKeyName = primaryKeyIndexName;
                return primaryKeyValues;
            }
        }
        private static PropertyInfo[] primaryKeyValues = null;
        private static string primaryKeyIndexName = null;
        private static readonly object PrimaryKeyOrdinalsLocker = new object();

        public static bool DeleteRow(SqlCeConnection connection, T toDelete, bool caseSensitive = true)
        {
            IEnumerable<int> unableToFindIndexes;
            DeleteRows(connection, new T[] { toDelete }, out unableToFindIndexes, caseSensitive);
            return !(unableToFindIndexes ?? Enumerable.Empty<int>()).Any();
        }

        public static void DeleteRows(SqlCeConnection connection, IEnumerable<T> toDelete, out IEnumerable<int> unableToFindIndexes, bool caseSensitive = true)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            if (toDelete == null)
            {
                unableToFindIndexes = Enumerable.Empty<int>();
                return;
            }

            DataColumn[] columns;
            PropertyInfo[] values;
            string deleteTableName = TableName;
            string selectTopZero;

            FindInsertColumns(connection, deleteTableName, out columns, out values, out selectTopZero);

            string primaryKeyName;
            PropertyInfo[] keyValues = GetPrimaryKeyColumnValues(connection, deleteTableName, selectTopZero, out primaryKeyName);

            List<int> unableToFindList = new List<int>();

            using (SqlCeCommand retrieveExisting = new SqlCeCommand())
            {
                retrieveExisting.CommandType = CommandType.TableDirect;
                retrieveExisting.CommandText = deleteTableName;
                retrieveExisting.Connection = connection;
                retrieveExisting.IndexName = primaryKeyName;

                using (SqlCeResultSet retrievedExisting = retrieveExisting.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable
                    | (caseSensitive
                        ? ResultSetOptions.Sensitive
                        : ResultSetOptions.Insensitive)))
                {
                    int deleteIndex = 0;
                    foreach (T currentDelete in toDelete)
                    {
                        if (retrievedExisting.Seek(DbSeekOptions.FirstEqual,
                            keyValues.Select(currentKey => currentKey.GetValue(currentDelete, null))
                                .ToArray()))
                        {
                            if (retrievedExisting.Read())
                            {
                                retrievedExisting.Delete();
                            }
                            else
                            {
                                unableToFindList.Add(deleteIndex);
                            }
                        }
                        else
                        {
                            unableToFindList.Add(deleteIndex);
                        }

                        deleteIndex++;
                    }
                }
            }

            unableToFindIndexes = unableToFindList;
        }

        private static readonly MethodInfo IsNullRecordInfo = typeof(SqlCeDataReader)
            .GetMethod("IsDBNull",
                BindingFlags.Instance | BindingFlags.Public);

        private static readonly MethodInfo GetOrdinalRecordInfo = typeof(SqlCeDataReader)
            .GetMethod("GetOrdinal",
                BindingFlags.Instance | BindingFlags.Public);
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class SqlAccess : Attribute
    {
        public string SqlName
        {
            get
            {
                return _sqlName;
            }
        }
        private string _sqlName = null;

        public bool IsChild
        {
            get
            {
                return _isChild;
            }
        }
        private bool _isChild = false;

        public SqlAccess() { }
        public SqlAccess(string sqlName)
        {
            this._sqlName = sqlName;
        }
        public SqlAccess(bool isChild)
        {
            this._isChild = isChild;
        }
        public SqlAccess(string sqlName, bool isChild)
        {
            this._sqlName = sqlName;
            this._isChild = isChild;
        }
    }
}