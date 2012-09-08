using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;
#if PocketPC
#else
using Salient.Data;
#endif
namespace ErikEJ.SqlCe
{
    /// <summary>
    /// Lets you efficiently bulk load a SQL Server Compact table with data from another source.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ce")]
    public class SqlCeBulkCopy : IDisposable
    {
        private static readonly Type DbNullType = typeof(System.DBNull);
        private SqlCeBulkCopyColumnMappingCollection mappings = new SqlCeBulkCopyColumnMappingCollection();
        private int notifyAfter;
        private readonly SqlCeConnection conn;
        private readonly SqlCeTransaction trans;
        private readonly bool ownsConnection;
        private bool ownsTransaction;
        private readonly bool keepNulls;
        private readonly bool keepIdentity;
        private string destination;
        private readonly SqlCeBulkCopyOptions options;

        /// <summary>
        /// Initializes a new instance of the SqlCeBulkCopy class using the specified open instance of SqlCeConnection. 
        /// </summary>
        /// <param name="connection"></param>
        public SqlCeBulkCopy(SqlCeConnection connection)
        {
            conn = connection;
        }
        /// <summary>
        /// Initializes a new instance of the SqlCeBulkCopy class using the specified open instance of SqlCeConnection and the specified active SqlCeTransaction.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        public SqlCeBulkCopy(SqlCeConnection connection, SqlCeTransaction transaction)
        {
            conn = connection;
            trans = transaction;
        }

        /// <summary>
        /// Creates a new instance of the SqlCeBulkCopy class, using the specified connection and options
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="copyOptions"></param>
        public SqlCeBulkCopy(SqlCeConnection connection, SqlCeBulkCopyOptions copyOptions)
        {
            conn = connection;
            options = copyOptions;
            keepNulls = IsCopyOption(SqlCeBulkCopyOptions.KeepNulls);
            keepIdentity = IsCopyOption(SqlCeBulkCopyOptions.KeepIdentity);
        }

        /// <summary>
        /// Initializes a new instance of the SqlCeBulkCopy class, using the specified connection, transaction and options.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="copyOptions"></param>
        public SqlCeBulkCopy(SqlCeConnection connection, SqlCeBulkCopyOptions copyOptions, SqlCeTransaction transaction)
        {
            conn = connection;
            trans = transaction;
            options = copyOptions;
            keepNulls = IsCopyOption(SqlCeBulkCopyOptions.KeepNulls);
            keepIdentity = IsCopyOption(SqlCeBulkCopyOptions.KeepIdentity);
        }

        /// <summary>
        /// Initializes a new instance of the SqlCeBulkCopy class using the specified connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        public SqlCeBulkCopy(string connectionString)
        {
            conn = new SqlCeConnection(connectionString);
            ownsConnection = true;
        }
        /// <summary>
        /// Initializes a new instance of the SqlCeBulkCopy class, using the specified connection string and options
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="copyOptions"></param>
        public SqlCeBulkCopy(string connectionString, SqlCeBulkCopyOptions copyOptions)
        {
            conn = new SqlCeConnection(connectionString);
            ownsConnection = true;
            options = copyOptions;
            keepNulls = IsCopyOption(SqlCeBulkCopyOptions.KeepNulls);
            keepIdentity = IsCopyOption(SqlCeBulkCopyOptions.KeepIdentity);
        }

        /// <summary>
        /// Name of the destination table in the SQL Server Compact database.
        /// </summary>
        public string DestinationTableName
        {
            get
            {
                return destination;
            }
            set
            {
                destination = value;
            }
        }
        /// <summary>
        /// Returns a collection of SqlCeBulkCopyColumnMapping items. Column mappings define the relationships between columns in the data source and columns in the destination.
        /// </summary>
        public SqlCeBulkCopyColumnMappingCollection ColumnMappings
        {
            get
            {
                return mappings;
            }
        }
        /// <summary>
        /// The integer value of the BatchSize property, or zero if no value has been set.
        /// In this implementation not used.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// The integer value of the BulkCopyTimeout property.
        /// In this implementation not used.
        /// </summary>
        public int BulkCopyTimeout { get; set; }

        /// <summary>
        /// Defines the number of rows to be processed before generating a notification event. 
        /// </summary>
        public int NotifyAfter
        {
            get
            {
                return notifyAfter;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Must be > 0");
                }
                notifyAfter = value;
            }
        }

        /// <summary>
        /// Occurs every time that the number of rows specified by the NotifyAfter property have been processed. 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ce")]
        public event EventHandler<SqlCeRowsCopiedEventArgs> RowsCopied;

        /// <summary>
        /// Closes the SqlCeBulkCopy instance. 
        /// </summary>
        public void Close()
        {
            if (ownsConnection && conn != null)
            {
                conn.Dispose();
            }
        }

        /// <summary>
        /// Copies all rows in the supplied DataTable to a destination table specified by the DestinationTableName property of the SqlCeBulkCopy object. 
        /// </summary>
        /// <param name="table"></param>
        public void WriteToServer(DataTable table)
        {
            WriteToServer(table, 0);
        }
        /// <summary>
        /// Copies only rows that match the supplied row state in the supplied DataTable to a destination table specified by the DestinationTableName property of the SqlCeBulkCopy object. 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="rowState"></param>
        public void WriteToServer(DataTable table, DataRowState rowState)
        {
            WriteToServer(new SqlCeBulkCopyDataTableAdapter(table, rowState));
        }

        /// <summary>
        /// Copies all rows in the supplied IDataReader to a destination table specified by the DestinationTableName property of the SqlBulkCopy object. 
        /// </summary>
        /// <param name="reader"></param>
        public void WriteToServer(IDataReader reader)
        {
            //force a dispose
            using (var tempReader = reader)
            {
                WriteToServer(new SqlCeBulkCopyDataReaderAdapter(reader));
				tempReader.Close();
            }
        }

#if PocketPC
#else
        /// <summary>
        /// Copies all rows in the supplied IEnumerable&lt;> to a destination table specified by the DestinationTableName property of the SqlBulkCopy object.
        /// </summary>
        /// <param name="collection">IEnumerable&lt;>. For IEnumerable use other constructor and specify type.</param>
        public void WriteToServer(IEnumerable collection)
        {
            using (var reader = new Salient.Data.EnumerableDataReader(collection))
            {
                WriteToServer(new SqlCeBulkCopyDataReaderAdapter(reader));
            }
        }

        /// <summary>
        /// Copies all rows in the supplied IEnumerable to a destination table specified by the DestinationTableName property of the SqlBulkCopy object.
        /// Use other constructor for IEnumerable&lt;>
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="elementType"></param>
        public void WriteToServer(IEnumerable collection, Type elementType)
        {
            using (var reader = new Salient.Data.EnumerableDataReader(collection, elementType))
            {
                WriteToServer(new SqlCeBulkCopyDataReaderAdapter(reader));
            }
        }

#endif

        private void WriteToServer(ISqlCeBulkCopyInsertAdapter adapter)
        {
            CheckDestination();

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            List<KeyValuePair<int, int>> map = null;

			SqlCeTransaction localTrans = trans ?? conn.BeginTransaction();

            if (ColumnMappings.Count > 0)
            {
                //mapping are set, and should be validated
                map = ColumnMappings.ValidateCollection(conn, localTrans, adapter, options, destination);
            }
            else
            {
                //create default column mappings
                map = SqlCeBulkCopyColumnMappingCollection.Create(conn, localTrans, adapter, options, destination);
            }

            using (SqlCeCommand cmd = new SqlCeCommand(destination, conn, localTrans))
            {
                cmd.CommandType = CommandType.TableDirect;
                using (SqlCeResultSet rs = cmd.ExecuteResultSet(ResultSetOptions.Updatable))
                {
                    int idOrdinal = SqlCeBulkCopyTableHelpers.IdentityOrdinal(conn, localTrans, options, destination);
                    SqlCeUpdatableRecord rec = rs.CreateRecord();

                    int rowCounter = 0;
                    int totalRows = 0;
                    IdInsertOn(localTrans);

                    //Converting to an array removed the perf issue of a list and foreach statement.
                    var cm = map.ToArray();

                    while (adapter.Read())
                    {
                        if (adapter.SkipRow())
                            continue;

                        for (int i = 0; i < cm.Length; i++)
                        {
                            //caching the values this way do not cause a perf issue.
                            var sourceIndex = cm[i].Key;
                            var destIndex = cm[i].Value;

                            // Let the destination assign identity values
                            if (!keepIdentity && destIndex == idOrdinal)
                                continue;

                            //determine if we should ever allow this in the map.
                            if (sourceIndex < 0)
                                continue;

                            var value = sourceIndex > -1 ? adapter.Get(sourceIndex) : null;

                            if (value != null && value.GetType() != DbNullType)
                            {
                                rec.SetValue(destIndex, value);
                            }
                            else
                            {
                                //we can't write to an auto number column so continue
                                if (keepNulls && destIndex == idOrdinal)
                                    continue;

                                if (keepNulls)
                                {
                                    rec.SetValue(destIndex, DBNull.Value);
                                }
                                else
                                {
                                    rec.SetDefault(destIndex);
                                }
                            }
                            // Fire event if needed
                            if (notifyAfter > 0 && rowCounter == notifyAfter)
                            {
                                FireRowsCopiedEvent(totalRows);
                                rowCounter = 0;
                            }
                        }
                        rowCounter++;
                        totalRows++;
                        rs.Insert(rec);
                    }
                    IdInsertOff(localTrans);
                }
            }

            //if we have our own transaction, we will commit it
            if (trans == null)
            {
                localTrans.Commit(CommitMode.Immediate);
				localTrans.Dispose();
            }
        }

        private void CheckDestination()
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("DestinationTable not specified");
            }
            ownsTransaction = (trans == null);
        }

        private void IdInsertOn(SqlCeTransaction localTrans)
        {
            if (keepIdentity)
            {
				using (var idCmd = AdoNetUtils.CreateCommand(conn, localTrans, string.Format(CultureInfo.InvariantCulture, "SET IDENTITY_INSERT [{0}] ON", DestinationTableName)))
                {
                    idCmd.ExecuteNonQuery();
                }

            }
        }

        private void IdInsertOff(SqlCeTransaction localTrans)
        {
            if (keepIdentity)
            {
				using (var idCmd = AdoNetUtils.CreateCommand(conn, localTrans, string.Format(CultureInfo.InvariantCulture, "SET IDENTITY_INSERT [{0}] OFF", DestinationTableName)))
                {
                    idCmd.ExecuteNonQuery();
                }
            }
        }

        private void OnRowsCopied(SqlCeRowsCopiedEventArgs e)
        {
            if (RowsCopied != null)
            {
                RowsCopied(this, e);
            }
        }

        private void FireRowsCopiedEvent(long rowsCopied)
        {
            SqlCeRowsCopiedEventArgs args = new SqlCeRowsCopiedEventArgs(rowsCopied);
            OnRowsCopied(args);
        }

        private bool IsCopyOption(SqlCeBulkCopyOptions copyOption)
        {
            return ((options & copyOption) == copyOption);
        }

        #region IDisposable Members

        /// <summary>
        /// Release resources owned by this instance
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ownsConnection && conn != null)
                {
                    conn.Dispose();
                }
                if (ownsTransaction && trans != null)
                {
                    trans.Dispose();
                }
            }
        }
        /// <summary>
        /// Release resources owned by this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Release resources owned by this instance
        /// </summary>
        ~SqlCeBulkCopy()
        {
            Dispose(false);
        }

        #endregion

    }
}
