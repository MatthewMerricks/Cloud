//
// IndexingAgent.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Globalization;
using Cloud.Model;
using Cloud.Static;
using Cloud.SQLIndexer.SqlModel;
using Cloud.SQLIndexer.Migrations;
using Cloud.SQLIndexer.Model;
using SqlSync = Cloud.SQLIndexer.SqlModel.Sync;
using Cloud.Interfaces;
using Cloud.Support;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.SQLProxies;
using System.Data;

namespace Cloud.SQLIndexer
{
    internal sealed class IndexingAgent : IDisposable
    {
        #region private fields
        private static readonly CLTrace _trace = CLTrace.Instance;
        // store the path that represents the root of indexing
        private string indexedPath = null;
        private readonly CLSyncbox syncbox;
        private readonly bool copyDatabaseBetweenChanges;
        private long rootFileSystemObjectId = 0;
        private long rootFileSystemObjectServerUidId = 0;
        private bool disposed = false;

        private readonly Dictionary<long, long> migratedServerUidIds = new Dictionary<long, long>();

        #region SQLite
        private readonly string indexDBLocation;
        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases
        private const string indexScriptsResourceFolder = ".SQLIndexer.IndexDBScripts.";
        #endregion

        // store dictionaries to convert between the FileChangetype enumeration and its integer value in the database,
        // will be filled in during startup
        private static Dictionary<long, FileChangeType> changeEnums = null;
        private static Dictionary<FileChangeType, long> changeEnumsBackward = null;

        private readonly GenericHolder<int> dbCopyNumber = new GenericHolder<int>(0);

        // category in SQL that represents the Enumeration type FileChangeType
        private static long changeCategoryId = 0;
        // locker for reading/writing the change enumerations
        private static object changeEnumsLocker = new object();
        #endregion

        #region public properties
        /// <summary>
        /// Store the last Sync Id, starts null before indexing; lock on the IndexingAgent instance for all reads/writes
        /// </summary>
        public string LastSyncId { get; private set; }
        public readonly ReaderWriterLockSlim LastSyncLocker = new ReaderWriterLockSlim();
        #endregion

        /// <summary>
        /// Creates the SQL indexing service and outputs it,
        /// must be started afterwards with StartInitialIndexing
        /// </summary>
        /// <param name="newIndexer">Output indexing agent</param>
        /// <param name="syncbox">Syncbox to index</param>
        /// <returns>Returns the error that occurred during creation, if any</returns>
        public static CLError CreateNewAndInitialize(out IndexingAgent newIndexer, CLSyncbox syncbox, bool copyDatabaseBetweenChanges = false)
        {
            // Fill in output with constructor
            IndexingAgent newAgent;
            try
            {
                newIndexer = newAgent = new IndexingAgent(syncbox, copyDatabaseBetweenChanges); // this double instance setting is required for some reason to prevent a "does not exist in the current context" compiler error
            }
            catch (Exception ex)
            {
                newIndexer = Helpers.DefaultForType<IndexingAgent>();
                return ex;
            }

            try
            {
                newIndexer.InitializeDatabase(syncbox.Path);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #region public methods
        public CLError CreateNewServerUid(string serverUid, string revision, out long serverUidId, SQLTransactionalBase existingTransaction = null)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                _trace.writeToLog(9, "IndexingAgent: Entry: CreateNewServerUid: serverUid: {0}. revision: {1}. existingTransaction: {2}.", serverUid, revision, existingTransaction == null ? "null" : "notNull");

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
            }
            catch (Exception ex)
            {
                serverUidId = Helpers.DefaultForType<long>();

                return ex;
            }

            CLError toReturn = null;

            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                SqlServerUid newUid = new SqlServerUid()
                {
                    ServerUid = serverUid,
                    Revision = revision
                };

                serverUidId = SqlAccessor<SqlServerUid>.InsertRow<long>(
                    castTransaction.sqlConnection,
                    newUid,
                    transaction: castTransaction.sqlTransaction);

                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();
                }
                _trace.writeToLog(9, "IndexingAgent: CreateNewServerUid: Return serverUidId: {0}.", serverUidId);
                if ((syncbox.CopiedSettings.TraceType & TraceType.ServerUid) == TraceType.ServerUid)
                {
                    ComTrace.LogServerUid(syncbox.CopiedSettings.TraceLocation, syncbox.CopiedSettings.DeviceId, syncbox.SyncboxId, serverUidId, serverUid, revision);
                }
            }
            catch (Exception ex)
            {
                serverUidId = Helpers.DefaultForType<long>();

                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        public CLError UpdateServerUid(long serverUidId, string serverUid, string revision, out Nullable<long> existingServerUidIdRequiringMerging, SQLTransactionalBase existingTransaction = null)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                _trace.writeToLog(9, "IndexingAgent: Entry: UpdateServerUid: serverUidId: {0}. serverUid: {1}. revision: {2}. existingTransaction: {3}.", serverUidId, serverUid, revision, existingTransaction == null ? "null" : "notNull");

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
            }
            catch (Exception ex)
            {
                existingServerUidIdRequiringMerging = null;

                return ex;
            }

            CLError toReturn = null;
            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                const string selectServerUidByUid = "t3Ee1ulQLjs62aHw5E7nECvEOXnIBnZugOzaPhT39+GYAeWswAkglLpCoOQhZXKdFn8CHvWfA82vrWmGb0RJcXpc5THDH449IVGfc/8aS2qWhWFHtV75xEfaq3iY3/MSCY8UkeCo1WsrUTf4FwJvG3SwDFBf79QC3MhZyK0IgX4=";

                SqlServerUid existingUid = SqlAccessor<SqlServerUid>.SelectResultSet(
                        castTransaction.sqlConnection,

                        //// before
                        //
                        //"SELECT * " +
                        //"FROM ServerUids " +
                        //"WHERE ServerUids.ServerUid = ?" // <-- parameter 1
                        //
                        //// after (decrypted)
                        //
                        //SELECT *
                        //FROM ServerUids
                        //WHERE ServerUids.ServerUid = ?
                        Helpers.DecryptString(
                            selectServerUidByUid,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))),

                        transaction: castTransaction.sqlTransaction,
                        selectParameters: Helpers.EnumerateSingleItem(serverUid))
                    .FirstOrDefault();

                lock (migratedServerUidIds)
                {
                    long nextServerUidId;
                    while (migratedServerUidIds.TryGetValue(serverUidId, out nextServerUidId))
                    {
                        serverUidId = nextServerUidId;
                        _trace.writeToLog(9, "IndexingAgent: UpdateServerUid: Migrated forwards: serverUidId: {0}.", serverUidId);
                    }
                }

                if (existingUid != null
                    && existingUid.ServerUidId == serverUidId
                    && existingUid.Revision == revision)
                {
                    // no op, row already exists and matches current data

                    existingServerUidIdRequiringMerging = null;
                }
                else
                {
                    Nullable<long> previousMigrationTarget = null;
                    bool migratedExistingUid = false;

                    try
                    {
                        if (existingUid == null
                            || existingUid.ServerUidId == serverUidId)
                        {
                            // either serverUid does not already exist in the database and can be added,
                            // or row matches current row and revision needs to be updated

                            // in this condition, only the existing row will be updated (after else condition below)

                            existingServerUidIdRequiringMerging = null;
                        }
                        else
                        {
                            // another row already is using the same ServerUid,
                            // need to move all rows forward to use this ServerUidId and remove the other one and update the current ServerUidId to the latest values

                            existingServerUidIdRequiringMerging = existingUid.ServerUidId;

                            lock (migratedServerUidIds)
                            {
                                long grabExistingTarget;
                                if (migratedServerUidIds.TryGetValue(existingUid.ServerUidId, out grabExistingTarget))
                                {
                                    previousMigrationTarget = grabExistingTarget;
                                    migratedServerUidIds[existingUid.ServerUidId] = serverUidId;
                                }
                                else
                                {
                                    migratedServerUidIds.Add(existingUid.ServerUidId, serverUidId);
                                }
                            }
                            migratedExistingUid = true;

                            using (ISQLiteCommand moveServerUidIds = castTransaction.sqlConnection.CreateCommand())
                            {
                                moveServerUidIds.Transaction = castTransaction.sqlTransaction;

                                const string updateServerUidString = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFNFXhWHlzk5+A66zGcgZSlb9qptC/hOnRmD/WJipY2H4qdmQgGkev3cYWKTwAtGBzDXlP1mROBqutN5IMu6bEo8JUmgGu3YdVii7zT1cg7qpea6QlXG8x/Axq+akepJBk7wzku/vMkDmTqh3zkpavv45cOOuuLaGn38MNaCCCN+uG94JccTbV1WJgYMSkuhjA4YSqjgsXwzTLAT1KxWiPRJO7a49/ueOe6On7u3Hg1vfZCSKBcsTKZOy9Kb1xzdiI";

                                moveServerUidIds.CommandText =
                                    //// before
                                    //
                                    //"UPDATE FileSystemObjects " +
                                    //"SET ServerUidId = ? " + // <-- parameter 1
                                    //"WHERE ServerUidId = ?;" + // <-- parameter 2
                                    //"DELETE FROM ServerUids " +
                                    //"WHERE ServerUidId = ?;" // <-- paramter 3 (equivalent to parameter 2)
                                    //
                                    //// after (decrypted)
                                    //
                                    //UPDATE FileSystemObjects
                                    //SET ServerUidId = ?
                                    //WHERE ServerUidId = ?;
                                    //DELETE FROM ServerUids
                                    //WHERE ServerUidId = ?;
                                    Helpers.DecryptString(
                                        updateServerUidString,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword)));

                                ISQLiteParameter uidIdToKeep = moveServerUidIds.CreateParameter();
                                uidIdToKeep.Value = serverUidId;
                                moveServerUidIds.Parameters.Add(uidIdToKeep);

                                ISQLiteParameter uidIdToRemoveOne = moveServerUidIds.CreateParameter();
                                uidIdToRemoveOne.Value = existingUid.ServerUidId;
                                moveServerUidIds.Parameters.Add(uidIdToRemoveOne);

                                ISQLiteParameter uidIdToRemoveTwo = moveServerUidIds.CreateParameter();
                                uidIdToRemoveTwo.Value = existingUid.ServerUidId;
                                moveServerUidIds.Parameters.Add(uidIdToRemoveTwo);

                                moveServerUidIds.ExecuteNonQuery();
                            }
                        }

                        SqlServerUid updateUid = new SqlServerUid()
                        {
                            ServerUidId = serverUidId,
                            ServerUid = serverUid,
                            Revision = revision
                        };

                        if (!SqlAccessor<SqlServerUid>.UpdateRow(
                            castTransaction.sqlConnection,
                            updateUid,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, string.Format("Unable to update server \"uid\" and revision for id {0}", serverUidId));
                        }

                        if (!inputTransactionSet
                            && castTransaction != null)
                        {
                            castTransaction.Commit();
                        }
                    }
                    catch
                    {
                        if (migratedExistingUid)
                        {
                            lock (migratedServerUidIds)
                            {
                                if (previousMigrationTarget == null)
                                {
                                    migratedServerUidIds.Remove(existingUid.ServerUidId);
                                }
                                else
                                {
                                    migratedServerUidIds[existingUid.ServerUidId] = ((long)previousMigrationTarget);
                                }
                            }
                        }

                        throw;
                    }

                    if ((syncbox.CopiedSettings.TraceType & TraceType.ServerUid) == TraceType.ServerUid)
                    {
                        ComTrace.LogServerUid(syncbox.CopiedSettings.TraceLocation, syncbox.CopiedSettings.DeviceId, syncbox.SyncboxId, serverUidId, serverUid, revision);
                    }
                }
            }
            catch (Exception ex)
            {
                existingServerUidIdRequiringMerging = null;

                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        public CLError QueryServerUid(long serverUidId, out string serverUid, out string revision, SQLTransactionalBase existingTransaction = null)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                _trace.writeToLog(9, "IndexingAgent: Entry: QueryServerUid: serverUidId: {0}. existingTransaction: {1}.", serverUidId, existingTransaction == null ? "null" : "notNull");

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
            }
            catch (Exception ex)
            {
                serverUid = Helpers.DefaultForType<string>();
                revision = Helpers.DefaultForType<string>();

                return ex;
            }

            CLError toReturn = null;
            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                SqlServerUid retrievedUid;
                lock (migratedServerUidIds)
                {
                    long nextServerUidId;
                    while (migratedServerUidIds.TryGetValue(serverUidId, out nextServerUidId))
                    {
                        serverUidId = nextServerUidId;
                        _trace.writeToLog(9, "IndexingAgent: QueryServerUid: Migrated forwards: serverUidId: {0}.", serverUidId);
                    }

                    const string selectServerUidById = "t3Ee1ulQLjs62aHw5E7nECvEOXnIBnZugOzaPhT39+GYAeWswAkglLpCoOQhZXKdFn8CHvWfA82vrWmGb0RJcXpc5THDH449IVGfc/8aS2qWhWFHtV75xEfaq3iY3/MS2PMQ+BUzwkO+YyBhLPCQ2fSbA6e8s6RF+Kso44IF6D4=";

                    retrievedUid = SqlAccessor<SqlServerUid>.SelectResultSet(
                            castTransaction.sqlConnection,
                            //// before
                            //
                            //"SELECT * " +
                            //    "FROM ServerUids " +
                            //    "WHERE ServerUids.ServerUidId = ?"
                            //
                            //// after (decrypted)
                            //
                            //SELECT *
                            //FROM ServerUids
                            //WHERE ServerUids.ServerUidId = ?
                            Helpers.DecryptString(
                                selectServerUidById,
                                Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword))),
                            transaction: castTransaction.sqlTransaction,
                            selectParameters: Helpers.EnumerateSingleItem(serverUidId))
                        .FirstOrDefault();
                }

                if (retrievedUid == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, string.Format("Unable to retrieve ServerUid with id {0}", serverUidId));
                }

                serverUid = retrievedUid.ServerUid;
                revision = retrievedUid.Revision;

                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();
                }
                _trace.writeToLog(9, "IndexingAgent: QueryServerUid: Return: serverUid: {0}. revision: {1}.", serverUid, revision);
                if ((syncbox.CopiedSettings.TraceType & TraceType.ServerUid) == TraceType.ServerUid)
                {
                    ComTrace.LogServerUid(syncbox.CopiedSettings.TraceLocation, syncbox.CopiedSettings.DeviceId, syncbox.SyncboxId, serverUidId, serverUid, revision);
                }
            }
            catch (Exception ex)
            {
                serverUid = Helpers.DefaultForType<string>();
                revision = Helpers.DefaultForType<string>();

                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        public CLError QueryOrCreateServerUid(string serverUid, out long serverUidId, string revision, bool syncFromFileModify, SQLTransactionalBase existingTransaction = null)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                _trace.writeToLog(9, "IndexingAgent: Entry: QueryOrCreateServerUid: serverUid: {0}. revision: {1}. syncFromFileModify {2}. existingTransaction: {3}.", serverUid, revision, syncFromFileModify, existingTransaction == null ? "null" : "notNull");

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
            }
            catch (Exception ex)
            {
                serverUidId = Helpers.DefaultForType<long>();

                return ex;
            }

            CLError toReturn = null;
            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                const string selectServerUidByUid = "t3Ee1ulQLjs62aHw5E7nECvEOXnIBnZugOzaPhT39+GYAeWswAkglLpCoOQhZXKdFn8CHvWfA82vrWmGb0RJcXpc5THDH449IVGfc/8aS2qWhWFHtV75xEfaq3iY3/MSCY8UkeCo1WsrUTf4FwJvG1loRIFBh+xMms2EQCAl2nc=";

                SqlServerUid retrievedUid = SqlAccessor<SqlServerUid>.SelectResultSet(
                        castTransaction.sqlConnection,
                        //// before
                        //
                        //"SELECT * " +
                        //    "FROM ServerUids " +
                        //    "WHERE ServerUids.ServerUid = ?"
                        //
                        //// after (decrypted)
                        //
                        //SELECT *
                        //FROM ServerUids
                        //WHERE ServerUids.ServerUid = ?
                        Helpers.DecryptString(
                            selectServerUidByUid,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))),
                                
                        transaction: castTransaction.sqlTransaction,
                        selectParameters: Helpers.EnumerateSingleItem(serverUid))
                    .FirstOrDefault();

                if (retrievedUid == null)
                {
                    retrievedUid = new SqlServerUid()
                    {
                        ServerUid = serverUid,
                        Revision = revision
                    };

                    serverUidId = SqlAccessor<SqlServerUid>.InsertRow<long>(
                        castTransaction.sqlConnection,
                        retrievedUid,
                        transaction: castTransaction.sqlTransaction);
                }
                else
                {
                    serverUidId = retrievedUid.ServerUidId;

                    if (!syncFromFileModify
                        && revision != retrievedUid.Revision)
                    {
                        retrievedUid.Revision = revision;

                        if (!SqlAccessor<SqlServerUid>.UpdateRow(castTransaction.sqlConnection,
                            retrievedUid,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to update ServerUid with different revision");
                        }
                    }
                }

                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();
                }
                _trace.writeToLog(9, "IndexingAgent: QueryOrCreateServerUid: Return: serverUidid: {0}.", serverUidId);
                if ((syncbox.CopiedSettings.TraceType & TraceType.ServerUid) == TraceType.ServerUid)
                {
                    ComTrace.LogServerUid(syncbox.CopiedSettings.TraceLocation, syncbox.CopiedSettings.DeviceId, syncbox.SyncboxId, serverUidId, serverUid, revision);
                }
            }
            catch (Exception ex)
            {
                serverUidId = Helpers.DefaultForType<long>();

                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        public CLError SearchExistingServerUidIdForPendingSyncToCreate(string name, string parentFolderServerUid, out Nullable<long> existingServerUidId)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    existingServerUidId = Helpers.DefaultForType<Nullable<long>>();

                    return ex;
                }
            }

            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException("name cannot be null");
                }
                if (string.IsNullOrEmpty(parentFolderServerUid))
                {
                    throw new ArgumentNullException("parentFolderServerUid cannot be null");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    using (ISQLiteCommand searchServerUid = indexDB.CreateCommand())
                    {
                        const string selectFileSystemObjectByParentUidAndName = "Ir/JfpeZX5xlpoW+GgV7/Q5hgKLsGrAsu4Vfm4TmJFZhZok4RQz6RsaJvls5+3rOrAO0LRfqauEG/XIRSGL74GZXq1EqUW30Jx3G8Fjhpj2DqMFqXlKKGzfJ5nbCY4b7KfekaHm0+b4igA60UMIa8kBMyB/ldpY0R+AGrzqSIOSNZZ5FQEAL35rmQ5B0q5m66BzX3o4oxagl05ul8YxwtOkVUtbjvIlItZ+PiCrsA2FMaIErAJHR4kqamDv408InUADXvKic3baJ5IwsM10l6wcspapvcqMWqt/cV4RFUIPU5ScZs9O6JJC1ks+hhe4a9tSo5kpu5JxVt2DYF8jLHNrF3eFs/Tl3K8gJt3bg5D4/aJ7S118l2AOJike8EU5lf3xf2uoOhJP/M4QVYWcm4Nt0RO3tcYCNAxgpmfNPLdWPYNYCFhaGuJJlJETGwjvUxDOxYyQTOmmO4TKzZtiUF0lcuNuEtsFnMHIRVQiTJ6s+Bu4c1FymBrCD7Vm7Ue7yN3yY6wUwWO0yMoLqd9jlfppegFS7K3+0FboXcOfIReUmpSpZzaMCrz1BmLMhQzUZpfc3rm04zFhs8W7sPWuOy5iA6Ustf2dFzos4GXNsJhVpoDdERvZYY/5F5+14hABrFces9MHgZ+HXYuA4e6BgAKcKJYtdKfqn66HmLWzXox9rJd0dY3ak+hTIK1qN18VskY5Qr/3n/fG+62gjBKpytYae6zfFAS5kq5yHRWmPqdCSSEDmD00i8RT5F8xMzhfJgw6exeGXSoALio3dM7qHUMRQZMsN+MZ0tIIRhU0icptgT6Qh9ZTZeVC/zTt3DvQeOlVQVyM2MSxp+OOuGG3mToa6o57WLkbezOT5bn3+UHrK5e1vGroaAHeEeljLfKWiCCZgCUAUdFbt/ZOSlNH+oJAwH9KSaDrNIjD2WT8URJ4sm22NpoS8B6GNq9PJXGYeA+CGiAYNaRxI7A7Zr5+cwzNW7OAe0EnyAPqhY49ey6b53FgWNxXQ3dCB4DnV2cAavDz31UjTYurQSBAA4DFInbPTJFtO1jWUmCdaOmgLx1QHYY5CUS6LkU0vP78wrH8u0QWS3BYgrin8QOu5YYAqKUIj0TD7KiJTBv4g5nXaAPrm5vGuJ8SaHpzqLZeaCqZUde914uXozJU2UcHrfeuMZV1bxNeDpycKzfid80g6ZJH4d4zV4yrbLG1LxNo/VCH5dSoW3M7NKhXY1IJL22PnGKAjPPBDgXlHAoZiXL7z/VvbGACfmx3PtstloPjR9qAx/0KQl7TLKHs39tsdnMVIsn+Xd4lWMuG5Smrnm+jMHTnz7DscUaHt/weWGxQgeiXwiQEOiO3mIAmteoK+DLZfFA5CElD1UcBE+Ex/ElkyclHxK4F/ANPapJzgyg70t4K6WIwif0JfRL9HmaPSDsmbMs7sxMggtIqCygULTpQYan/+hW3jNnlN/NdxkveQi5WPPJ4npCixUMa/Dj9OesWn74Kkn+paxvArg8Zo+2AvK/h4aE3gOkOg+ftCyOxc6IQs";

                        searchServerUid.CommandText =
                            //// before
                            //
                            //"SELECT MainObjects.ServerUidId, MainObjects.Name " +
                            //"FROM FileSystemObjects MainObjects " +
                            //"INNER JOIN ServerUids MainUids ON MainObjects.ServerUidId = MainUids.ServerUidId " +
                            //"INNER JOIN Events ON MainObjects.EventId = Events.EventId " +
                            //"INNER JOIN FileSystemObjects Parents ON MainObjects.ParentFolderId = Parents.FileSystemObjectId " +
                            //"INNER JOIN ServerUids ParentUids ON Parents.ServerUidId = ParentUids.ServerUidId " +
                            //"WHERE MainObjects.Pending = 1 " +
                            //"AND MainUids.ServerUid IS NULL " +
                            //"AND Events.FileChangeTypeEnumId <> " + changeEnumsBackward[FileChangeType.Deleted].ToString() +
                            //" AND ParentUids.ServerUid = ?" + // <-- parameter 1
                            //" AND MainObjects.NameCIHash = ?" // <-- parameter 2
                            //
                            //// after (decrypted; {0}: changeEnumsBackward[FileChangeType.Deleted])
                            //
                            //SELECT MainObjects.ServerUidId, MainObjects.Name
                            //FROM FileSystemObjects MainObjects
                            //INNER JOIN ServerUids MainUids ON MainObjects.ServerUidId = MainUids.ServerUidId
                            //INNER JOIN Events ON MainObjects.EventId = Events.EventId
                            //INNER JOIN FileSystemObjects Parents ON MainObjects.ParentFolderId = Parents.FileSystemObjectId
                            //INNER JOIN ServerUids ParentUids ON Parents.ServerUidId = ParentUids.ServerUidId
                            //WHERE MainObjects.Pending = 1
                            //AND MainUids.ServerUid IS NULL
                            //AND Events.FileChangeTypeEnumId <> {0}
                            //AND ParentUids.ServerUid = ?
                            //AND MainObjects.NameCIHash = ?
                            string.Format(
                                Helpers.DecryptString(
                                    selectFileSystemObjectByParentUidAndName,
                                    Encoding.ASCII.GetString(
                                        Convert.FromBase64String(indexDBPassword))),
                                changeEnumsBackward[FileChangeType.Deleted]);

                        ISQLiteParameter parentUidParam = searchServerUid.CreateParameter();
                        parentUidParam.Value = parentFolderServerUid;
                        searchServerUid.Parameters.Add(parentUidParam);

                        ISQLiteParameter nameCIHashParam = searchServerUid.CreateParameter();
                        nameCIHashParam.Value = StringComparer.OrdinalIgnoreCase.GetHashCode(name);
                        searchServerUid.Parameters.Add(nameCIHashParam);

                        using (ISQLiteDataReader searchUidReader = searchServerUid.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            existingServerUidId = null;

                            while (searchUidReader.Read())
                            {
                                if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(searchUidReader[Resources.NotTranslatedSqlIndexerName]), name))
                                {
                                    existingServerUidId = Convert.ToInt64(searchUidReader[Resources.NotTranslatedSqlIndexerServerUidId]);

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                existingServerUidId = Helpers.DefaultForType<Nullable<long>>();

                return ex;
            }

            return null;
        }

        /// <summary>
        /// Queries database by eventId to return latest metadata and path as a FileChange and whether or not the event is still pending
        /// </summary>
        /// <param name="eventId">EventId key to lookup</param>
        /// <param name="queryResult">(output) Result FileChange from EventId lookup</param>
        /// <param name="isPending">(output) Result whether event is pending from EventId lookup</param>
        /// <param name="status">(output) Status of quering the database</param>
        /// <returns>Returns any error which occurred querying the database, if any</returns>
        public CLError QueryFileChangeByEventId(long eventId, out FileChange queryResult, out bool isPending, out FileChangeQueryStatus status)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    queryResult = Helpers.DefaultForType<FileChange>();
                    isPending = Helpers.DefaultForType<bool>();
                    status = FileChangeQueryStatus.ErrorDisposed;

                    return ex;
                }
            }

            try
            {
                if (eventId <= 0)
                {
                    throw new ArgumentException("eventId cannot be equal to or less than zero");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    bool resultFound = false;
                    queryResult = null;
                    isPending = false;

                    const string selectEventById = "XiF/n8DAmECRcpl1q3g5SOaFkrEO/c+iI1V66stCO9bB3hEK7nYGLuijwAsZ69MKKHe0UmlVXNE46Xe1g79swjJQypbvXXe7eZglXh6sLHo9InIxJpV0S12E0AmeiKBLbBC157Xkvz89qvl1+ADG9swUwr5jwIGdufPgd9mVZ+ToUXo7Ux5hQw4MHiu/1iJLNq4w1rRHAQYh5FAlCCyG9ik9fZkrsp2Jiz+0hX2QS0MA70TrgQZxH9GSEgF7VY1kOcaXqaubUd1rI3FdOgmigYYhyd2yyOIR0xuendQGLlYlKA51/9+9z4smgA2Dgvt6vAkpNDu60UOXqB7IQzCaNUSziZ6P0Yao8NthNo3YMY95tCWb4sZM8c7uR90xHLPe1/MAc8Z/UEofiWDGOVGw6bpg3nRjiHvVDOLO1N3sH1cp/KAv28o5VWA4q0v3az1OfhWWkUm79tMfdxSoRwea5heWno74jGeAfe0eqOno4ukjvABq5Dl0kbbDWY6sTkTZ0vjA3VH74n8uwJ1stD8hmwRBwicmbWcsoeA3VePskNld89eBGskRwicZPwxfakUO/DJ4t60lL087iZFGHyhnhtR6DXzItsKAJ3BJDUQIwC3IPdUoSebR++kDLkCYvXieXODYZylyCkX5vqyxM1dTs237KNsbQOViDCs73IPAFRbfDUtuXZPb+gMlSBIIRwXo+FxJeVn7yDA0aHKakXxlnNVbRt8FQWhfFs2O1XdxojNJCd1DEmDaHV98S2qG4HII9XLHaaoA2zUJzMOl/6Lu1wTSyI+j6e2lcdEG7WJ4qn26GsuMoiofIv4NS2KsPCd43ufJE9bo3CmQnccGx8r6AtbY7gJbdHMi2jUFfvBd0oAOl/xfiezGPVMf5DGLzzoBSKb6H+RrOE4WUHS/4vqMx8Sn75rjuehaChjcsUpBPmWhjHJ1Qn97L1y4lUDfWzg3";

                    foreach (Event existingEvent in SqlAccessor<Event>.SelectResultSet(
                            indexDB,
                            //// before
                            //
                            //"SELECT " +
                            //    SqlAccessor<Event>.GetSelectColumns() + ", " +
                            //    SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerFileSystemObject) + ", " +
                            //    SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerPrevious, Resources.NotTranslatedSqlIndexerPreviouses) +
                            //    " FROM Events" +
                            //    " INNER JOIN FileSystemObjects ON Events.EventId = FileSystemObjects.EventId" +
                            //    " LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = FileSystemObjects.FileSystemObjectId" +
                            //    " WHERE Events.EventId = ?" +
                            //    " ORDER BY" +
                            //    " CASE WHEN FileSystemObjects.EventOrder IS NULL" +
                            //    " THEN 0" +
                            //    " ELSE FileSystemObjects.EventOrder" +
                            //    " END DESC"
                            //
                            //// after (decrypted; {0}: SqlAccessor<Event>.GetSelectColumns()
                            //// {1}: SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerFileSystemObject)
                            //// {2}: SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerPrevious, Resources.NotTranslatedSqlIndexerPreviouses) )
                            //
                            //SELECT
                            //{0},
                            //{1},
                            //{2}
                            //FROM Events
                            //INNER JOIN FileSystemObjects ON Events.EventId = FileSystemObjects.EventId
                            //LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = FileSystemObjects.FileSystemObjectId
                            //WHERE Events.EventId = ?
                            //ORDER BY
                            //CASE WHEN FileSystemObjects.EventOrder IS NULL
                            //THEN 0
                            //ELSE FileSystemObjects.EventOrder
                            //END DESC
                            string.Format(
                                Helpers.DecryptString(
                                    selectEventById,
                                    Encoding.ASCII.GetString(
                                        Convert.FromBase64String(indexDBPassword))),
                                SqlAccessor<Event>.GetSelectColumns(),
                                SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerFileSystemObject),
                                SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerPrevious, Resources.NotTranslatedSqlIndexerPreviouses)),
                            new[]
                            {
                                Resources.NotTranslatedSqlIndexerFileSystemObject,
                                Resources.NotTranslatedSqlIndexerPrevious
                            },
                            selectParameters: Helpers.EnumerateSingleItem(eventId)))
                    {
                        if (resultFound)
                        {
                            status = FileChangeQueryStatus.ErrorMultipleResults;
                            return SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Multiple objects found for given eventId");
                        }

                        resultFound = true;

                        queryResult = new FileChange()
                        {
                            Direction = (existingEvent.SyncFrom ? SyncDirection.From : SyncDirection.To),
                            EventId = existingEvent.EventId,
                            Metadata = new FileMetadata(existingEvent.FileSystemObject.ServerUidId)
                            {
                                EventTime = new DateTime(existingEvent.FileSystemObject.EventTimeUTCTicks, DateTimeKind.Utc),
                                HashableProperties = new FileMetadataHashableProperties(
                                    existingEvent.FileSystemObject.IsFolder,
                                    (existingEvent.FileSystemObject.LastTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)existingEvent.FileSystemObject.LastTimeUTCTicks, DateTimeKind.Utc)),
                                    (existingEvent.FileSystemObject.CreationTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)existingEvent.FileSystemObject.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                    existingEvent.FileSystemObject.Size),
                                IsShare = existingEvent.FileSystemObject.IsShare,
                                MimeType = existingEvent.FileSystemObject.MimeType,
                                Permissions = (existingEvent.FileSystemObject.Permissions == null
                                    ? (Nullable<POSIXPermissions>)null
                                    : (POSIXPermissions)((int)existingEvent.FileSystemObject.Permissions)),
                                StorageKey = existingEvent.FileSystemObject.StorageKey,
                                Version = existingEvent.FileSystemObject.Version
                            },
                            NewPath = existingEvent.FileSystemObject.CalculatedFullPath,
                            OldPath = (existingEvent.Previous == null
                                ? null
                                : existingEvent.Previous.CalculatedFullPath),
                            Type = changeEnums[existingEvent.FileChangeTypeEnumId]
                        };
                        queryResult.SetMD5(existingEvent.FileSystemObject.MD5);
                        isPending = existingEvent.FileSystemObject.Pending;
                    }

                    if (!resultFound)
                    {
                        status = FileChangeQueryStatus.ErrorNotFound;
                    }
                    else
                    {
                        status = FileChangeQueryStatus.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                queryResult = Helpers.DefaultForType<FileChange>();
                isPending = Helpers.DefaultForType<bool>();
                status = FileChangeQueryStatus.ErrorUnknown;

                return ex;
            }
            return null;
        }

        /// <summary>
        /// Starts the indexing process on an indexing agent which will resolve the last events and changes to the file system since the last time
        /// the file monitor was running to produce the initial in-memory index and changes to process,
        /// spins off a user work thread for the actual processing and returns immediately
        /// </summary>
        /// <param name="indexCompletionCallback">FileMonitor method to call upon completion of the index (should trigger normal processing of file events)</param>
        /// <param name="getPath">FileMonitor method which returns the path to be indexed (so that the indexing and monitor are tied together)</param>
        /// <returns>Returns an error that occurred during startup, if any</returns>
        public CLError StartInitialIndexing(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback,
            Func<string> getPath)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            try
            {
                this.indexedPath = getPath();
                ThreadPool.QueueUserWorkItem(state => 
                    {
                        try 
	                    {
                            this.BuildIndex((Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>>)state);
	                    }
	                    catch (Exception ex)
	                    {
                            CLError error = new AggregateException("Error building the index", ex);
                            error.Log(_trace.TraceLocation, _trace.LogErrors);
                            _trace.writeToLog(1, "IndexingAgent: StartInitialIndexing: ERROR: Exception: Error building the initial index. Msg: <{0}>.", ex.Message);
	                    }
                    },
                    indexCompletionCallback);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public void SwapOrderBetweenTwoEventIds(long eventIdA, long eventIdB, SQLTransactionalBase requiredTransaction)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    return;
                }
            }

            if (requiredTransaction == null)
            {
                throw new NullReferenceException("requiredTransaction cannot be null");
            }

            SQLTransactionalImplementation castTransaction = requiredTransaction as SQLTransactionalImplementation;

            if (castTransaction == null)
            {
                throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
            }
            if (!(eventIdA > 0))
            {
                throw new ArgumentException("eventIdA was not the positive integer created from adding a new Event to the databse");
            }
            if (!(eventIdB > 0))
            {
                throw new ArgumentException("eventIdB was not the positive integer created from adding a new Event to the database");
            }
            if (eventIdA == eventIdB)
            {
                throw new ArgumentException("Cannot swap two events with the same ID");
            }

            FileSystemObject eventAObject = null;
            FileSystemObject eventBObject = null;

            const string selectFileSystemObjectByEventId = "t3Ee1ulQLjs62aHw5E7nEHC3Yt6pkKQiMjDOMA00p+Qo1PZHGpfRx91FJNSloGZ3xDH11QktFYyaPHTl7mAN/QkLD0PnpC8sDmmRC3eIdnNwEv6VbgcYJMh2e8FkOh6pkTk+wvxmCRAw6xSk4LnkkKwhOy+K1PbDsM0gmAsv7FH9owvFUl6Kqdc6lKyE4dRdNY/BR7ifuAagqpifyIdzUO2HMAWtilo2ChITR0yPWGwgsxA3WnGPkaBknVI0jG/iTXOmwbu2CK1/r6YN+x/pdw==";

            foreach (FileSystemObject matchedEventObject in SqlAccessor<FileSystemObject>.SelectResultSet(
                castTransaction.sqlConnection,
                //// before
                //
                //"SELECT *" +
                //    "FROM FileSystemObjects " +
                //    "WHERE FileSystemObjects.EventId = ? " + // <-- parameter 1
                //    "OR FileSystemObjects.EventId = ?" // <-- paremeter 2
                //
                //// after (decrypted)
                //
                //SELECT *
                //FROM FileSystemObjects
                //WHERE FileSystemObjects.EventId = ?
                //OR FileSystemObjects.EventId = ?
                Helpers.DecryptString(
                    selectFileSystemObjectByEventId,
                    Encoding.ASCII.GetString(
                        Convert.FromBase64String(indexDBPassword))),
                transaction: castTransaction.sqlTransaction,
                selectParameters: new[] { eventIdA, eventIdB }))
            {
                if (matchedEventObject.EventId == eventIdA)
                {
                    if (eventAObject != null)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB returned more than one Event for eventIdA");
                    }

                    eventAObject = matchedEventObject;
                }
                else if (matchedEventObject.EventId == eventIdB)
                {
                    if (eventBObject != null)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB returned more than one Event for eventIdB");
                    }

                    eventBObject = matchedEventObject;
                }
                else
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB returned an event which matches neither ID");
                }
            }

            if (eventAObject == null)
            {
                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB did not return any Event for eventIdA");
            }
            if (eventBObject == null)
            {
                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query for FileSystemObjects by eventIdA and eventIdB did not return any Event for eventIdB");
            }

            using (ISQLiteCommand swapEventOrders = castTransaction.sqlConnection.CreateCommand())
            {
                swapEventOrders.Transaction = castTransaction.sqlTransaction;

                const string updateFileSystemObjectsById = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFwfuX4RFIfW1SlGaWLm/6a4w1L0VGWdOp1mFenOQFWvidNs96A2LvtvVNuJSRRd3dbDJ2uff0T9GfBcwjwcCQ/alEPFY/maTWztGEQe8iD4qrkqGb+KC2sdfiLg0AmjhDoPLxmuK1XyspJM3jfArYcPT4ql2QAqpn8GNUUt+X07fQ6Wc3tTR6oFgbDuPhVz8TZJwXKyMFRRvqv479d3x/7pjYxIBgqU2TcbExdyhpfrrO0cKLrHF3JOddQUfZBaVIGCfXO3HIIjDa3PDqHclWj0xZfCdRZ/k8iXTc8EaQHb/kuz6VK2y60oDzVIAw4qmasgYsdq+QVl9qWGCom/MOhxjG9dR7QQ5SBk2Z6j5ckcQ=";

                swapEventOrders.CommandText =
                    //// before
                    //
                    //"UPDATE FileSystemObjects " +
                    //"SET EventOrder = ? " + // <-- parameter 1
                    //"WHERE FileSystemObjectId = ?; " + // <-- parameter 2
                    //"UPDATE FileSystemObjects " +
                    //"SET EventOrder = ? " + // <-- parameter 3
                    //"WHERE FileSystemObjectId = ?;" // <-- parameter 4
                    //
                    //// after (decrypted)
                    //
                    //UPDATE FileSystemObjects
                    //SET EventOrder = ?
                    //WHERE FileSystemObjectId = ?;
                    //UPDATE FileSystemObjects
                    //SET EventOrder = ?
                    //WHERE FileSystemObjectId = ?;
                    Helpers.DecryptString(
                        updateFileSystemObjectsById,
                        Encoding.ASCII.GetString(
                            Convert.FromBase64String(indexDBPassword)));

                ISQLiteParameter eventBOrderParam = swapEventOrders.CreateParameter();
                eventBOrderParam.Value = eventBObject.EventOrder;
                swapEventOrders.Parameters.Add(eventBOrderParam);

                ISQLiteParameter eventAIdParam = swapEventOrders.CreateParameter();
                eventAIdParam.Value = eventAObject.FileSystemObjectId;
                swapEventOrders.Parameters.Add(eventAIdParam);

                ISQLiteParameter eventAOrderParam = swapEventOrders.CreateParameter();
                eventAOrderParam.Value = eventAObject.EventOrder;
                swapEventOrders.Parameters.Add(eventAOrderParam);

                ISQLiteParameter eventBIdParam = swapEventOrders.CreateParameter();
                eventBIdParam.Value = eventBObject.FileSystemObjectId;
                swapEventOrders.Parameters.Add(eventBIdParam);

                swapEventOrders.ExecuteNonQuery();
            }
        }

        public CLError GetCalculatedFullPathByServerUid(string serverUid, out string calculatedFullPath, Nullable<long> excludedEventId = null)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    calculatedFullPath = Helpers.DefaultForType<string>();

                    return ex;
                }
            }

            try
            {
                if (serverUid == null)
                {
                    throw new NullReferenceException("serverUid cannot be null");
                }

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    const string selectFileSystemObjectByUid = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYMR07cBwbt10hS8kRRyr8PVUl1vGvN+5X72diozXNAmWM6H173dlUMwHqQRoQXrKkLgNYNNVsIwFDUUB0jMNPpRqQD3IZKfC1ivXoiLjexXqAmdSxKiAv+mjj40Ah4q+djiEVFFqEVMcjYV/3FvYPlirYr3Jh14G6rN1dZmldugBTtnRG3F98UrGOkitHyJtO2lQCOfYRVZL5OQk7+cSoVXy9198iXdkMMLAs0OvIujjdEEywPXQv7MiPu2PQTu9FS7my3l8vrj4LFFHS/Rm3iP68QVVtJ4XSc6buDfpERoHbC9/CQ+gTonzt+p+ahh7B+2D/BB3UYdNHsJvE33OcHr8fOzN9aRrEtR0W2vfTTX+h69qnfZYjxxCXvpEm/7aRlxIO3oJJNMfIcWjZpCkcwhipmlmswR7SZeJCPb36ZwPTLxERj847Ua1bQLeQxhxNYMOnIPzReyMSDazB7vV6EPOMu7NfNS8Tpgg8OiYVzrB1zidh6vWNk6p4WrML0WioR07kn8UGPDYMZBkqqFNs1q0tHQkk1ooXL3n+M/ZGO4fTtb6LbMGqc1USBk5qJ6Us/0oqSmtqME92Xk+wZ6qOEdZqXaNinmR7MdBj9c6NOWTd2VXRSCg67r3MLzdcqFKeMEn9gdWKugpE2HzBWRXMeiwjd/qzptSaQ69PUbqAYn67rRWoToiNJpN9ssK/MeZ8frxnRDjVrZE4FibotufDvMYFXsspMvNvwPsR9p1Lm6xA7WNCmC78lBR8FdVmD+UtsLCZ+55+H1npNYqA6yAuqJCOHOnB+WjCrwk2i/Majxp9fgg+osKMVZwrVsUFqM2QojpF4AK9STfawFCOLalgo7X9D7a9PwvUQAv33pIKoBWD8XAlO6zbKwLKTyqhl4RCqd3gEpmmsXKxOqKdRI6Qb7xdotN3ci25Ew6x/d6p4BtqZtFFlLxFSJzfpiGpdXC5vyeNH1PRKOKlPZwea/lPeTUYOANPz3DWtAvnz5CJSxYw99oB23rfKbWVbLhk8vszlTpzEmCNGmr6ud6we6OlUtGh7LDid/A+nkLarYpSmb7WY8wXucpMxp1ZUJjCVroC9bOiNt4zup0kaS0qfctZiXPOVvgSMF0rjVASSZUrPv0lPyh7jQPADRmDHhiK1qLYdiEHttD40whht5bjPeatFHB8wUtG3chA6vVBhuKVxoWIREGQuKk6MDYf8Bgp5oj9jpzVxPFGsymhB6NWkZUBAEjInu+ez93mNWNwzEbP/HbeA7y85i4wzqCsnb2wv4hefhvWQUm1uPK+U9UIAjWZdIU6tVSSujChfrgi8czhwJkZH8orMm3N6bjbjiduIte1h42bYgq45N+hTn2JxImOkceQvx+Ib7feUr5f8klnDmDFV9FSqOwmvGtaRCu3Mn1jfgn71y/NW1oJJ2fOf6UKSfSIWrvD3tyJX5PYDDBZmRWtTddoFajConcF8iese1NgWETm0Tr/QNBskxQZxhdjV7s0iZ9Acav64Tncv7W6gVrZrt/vd44EWZ2ixLlP2HEg1PF+o7tsOIo8PPjSQJCq2NfDU62f0lULryxys8BuNcq8Tog2UEGLyk/rou4TqS1Z47Q+hV6H60b2al4u8ZJOBc/TFoEJ6l+nN4LJg+v1YVooV2/GCDPnUEJb4crmJcYB4dktb+f+iUx53r5IPcg6xZ24UwlDMAPL47tdsD5LF34PMGND+FK5HmJDb7XXUC9GCMdBIyuYVMaADLaVh68VmmIUAizMYk9TBEghk08zWdPJORYn9SNn34ij/HjRmLL2fD0VQ8BCdLNG0tEqf9ImzWD5zZSonKe16Z8PhGfLFTWLNBO7GsSyzE7KVskgZK8lK3xjmqNPWCPR4LRzhwXhJWLdV0+pZzULJRfjvAKwa5gSS4TvDg5nCENMnouSXw98tXsY/Cp3bf0vFZuMHAJdGvxU3Kj7wWsIHMbPKa/dyX2GwZex08tfHpdYfZ8Y5vx4e9xoIrNb3TRxKxvbnoEXPyDO97PUQUfo9wPMJH/Ogyxo6W8h2ZFx2axQILIyyKs9tFkdoVkPUAXm1JDrBQqYRyPHoMxiYEPEUNhnmZUNBDfLUxMCzxhbgbJn8Szh27O4wP9nfUuEFtyGFx7nMmR0bVHqoGN+nkgs8gDpEAmZkExJrSkH/e4EawG1Lln/s5G/grYupyXkFoc6QkGCngDRCExCmjFH9K2qf0btAc3SGoHX68os65eAdZk1KKP5v8TfhXLnrQEONPxmN7qWPCJ2N+Rox1VYUKrPNNWmYlEqCDTTP5E0Mk0KsQpTAkqgzPHWuevrQiB2M6RLppkXtOPIZEyhHTF2gPgAizHm4CoJXfRnRACShUZHSkg5lewgUdTOE";

                    // prefers the latest rename which is pending,
                    // otherwise prefers non-pending,
                    // last take most recent event
                    if (!SqlAccessor<object>.TrySelectScalar<string>(
                        indexDB,
                        //// before
                        //
                        //"SELECT FileSystemObjects.CalculatedFullPath " +
                        //    "FROM FileSystemObjects " +
                        //    "LEFT OUTER JOIN " +
                        //    "(" +
                        //        "SELECT InnerObjects.EventOrder " +
                        //        "FROM FileSystemObjects InnerObjects " +
                        //        "WHERE InnerObjects.EventId IS NOT NULL " +
                        //        "AND InnerObjects.EventOrder IS NOT NULL " +
                        //        "AND InnerObjects.EventId = ? " + // <-- parameter 1
                        //        "LIMIT 1" +
                        //    ") ConstantJoin " +
                        //    "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                        //    "INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidId " +
                        //    "WHERE ServerUids.ServerUid = ? " + // <-- parameter 2
                        //    "AND (ConstantJoin.EventOrder IS NULL OR FileSystemObjects.EventId IS NULL OR ConstantJoin.EventOrder > FileSystemObjects.EventOrder) " +
                        //    "ORDER BY " +
                        //    "CASE WHEN FileSystemObjects.EventId IS NOT NULL " +
                        //    "AND Events.FileChangeTypeEnumId = " + changeEnumsBackward[FileChangeType.Renamed].ToString() +
                        //    " AND FileSystemObjects.Pending = 1 " +
                        //    "THEN 0 " +
                        //    "ELSE 1 " +
                        //    "END ASC, " +
                        //    "FileSystemObjects.Pending ASC, " +
                        //    "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                        //    "THEN 0 " +
                        //    "ELSE FileSystemObjects.EventOrder " +
                        //    "END DESC " +
                        //    "LIMIT 1"
                        //
                        //// after (decrypted; {0}: changeEnumsBackward[FileChangeType.Renamed])
                        //
                        //SELECT FileSystemObjects.CalculatedFullPath
                        //FROM FileSystemObjects
                        //LEFT OUTER JOIN
                        //(
                        //SELECT InnerObjects.EventOrder
                        //FROM FileSystemObjects InnerObjects
                        //WHERE InnerObjects.EventId IS NOT NULL
                        //AND InnerObjects.EventOrder IS NOT NULL
                        //AND InnerObjects.EventId = ?
                        //LIMIT 1
                        //) ConstantJoin
                        //LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId
                        //INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidId
                        //WHERE ServerUids.ServerUid = ?
                        //AND (ConstantJoin.EventOrder IS NULL OR FileSystemObjects.EventId IS NULL OR ConstantJoin.EventOrder > FileSystemObjects.EventOrder)
                        //ORDER BY
                        //CASE WHEN FileSystemObjects.EventId IS NOT NULL
                        //AND Events.FileChangeTypeEnumId = {0}
                        //AND FileSystemObjects.Pending = 1
                        //THEN 0
                        //ELSE 1
                        //END ASC,
                        //FileSystemObjects.Pending ASC,
                        //CASE WHEN FileSystemObjects.EventOrder IS NULL
                        //THEN 0
                        //ELSE FileSystemObjects.EventOrder
                        //END DESC
                        //LIMIT 1
                        string.Format(
                            Helpers.DecryptString(
                                selectFileSystemObjectByUid,
                                Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword))),
                            changeEnumsBackward[FileChangeType.Renamed]),
                        out calculatedFullPath,
                        selectParameters: new[] { excludedEventId, (object)serverUid }))
                    {
                        calculatedFullPath = null;
                    }
                }
            }
            catch (Exception ex)
            {
                calculatedFullPath = Helpers.DefaultForType<string>();

                return ex;
            }
            return null;
        }

        public CLError GetServerUidByNewPath(string newPath, out string serverUid)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    serverUid = Helpers.DefaultForType<string>();

                    return ex;
                }
            }

            try
            {
                if (newPath == null)
                {
                    throw new NullReferenceException("newPath cannot be null");
                }

                FilePath pathObject = newPath;

                List<string> namePortions = new List<string>();

                while (!FilePathComparer.Instance.Equals(pathObject, indexedPath))
                {
                    namePortions.Add(pathObject.Name);

                    pathObject = pathObject.Parent;
                }

                namePortions.Add(indexedPath);

                namePortions.Reverse();

                string pathCIHashes = string.Join(((char)0x5c /* \ */).ToString(),
                    namePortions.Select(currentPortion => StringComparer.OrdinalIgnoreCase.GetHashCode(currentPortion).ToString()));

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    using (ISQLiteCommand existingUidCommand = indexDB.CreateCommand())
                    {
                        const string selectServerUidByFullPathHash = "WoSiN1iPXeT1ZD9VT//d8Jz8cdTqLc7gxRqmo361fGSIF0WCOpTjrV0d7/XDXFSpPOz+6TNS+xaN1IyZwybY1c2IsyMg0YyTZMeykk9p65VAs0vSPs5lKAHTBb1/PR/hfFt5rMyc2HwYLEu9cvTmzlHvo4Mj2T8gXpsDAsxoev/8nBXmikmQAtoWhEAFQuxTRU855YB++ok9Y6Tk7zE9GvZt9SON1GCdeYHFG5stVhPMXbX1P1rG5lKkkjs1x5EQF/8hEwLPbXq8BM3f6iRs+I1EZFNmb5PyhgjhrElGhIuqbqSxOG2Xo4j8gb4VBwMD6LoIW3SJ+Ol4e0+CSvrjmK0BokT7V59lnat9HEFiZqSgZQjUbtSMjXjTqP3azBxHapoa1h48V+anFC4+C/uW+rFlM9sKubBV7EAF3sArw0thTTKrN4iQ5W+FDfr+GZaM6Omr/80FaPQCY2sGfCSjsBASxnz1fGx201yZRhpcPrtSqb560UyCt0GugcaXZWEr0d3diEWguhn851IGzcNwJC/OJsSfsWyjXW9TdztTGG75wBvkaH09oMw8qjrVMGXTUkhQcZ58joOzXd3BrfZqsvDscGbN7FHPH88GHtyXtPDUzvQ9VcTQDMYLYGFizpFMPtcAG7qQGelK+bmMkv6wmsrqLKXvsB2c3ddYYOkZsHGXN3xjuqpF71p/6mEZONiFpcqPfvMvQ9+RObyLu0te9i6YQFuQxjJPojkO2AsYSdz0HwZWzdwuw9C4IrOD3sGCwb1+Spzmpg2WYYs4rbYlX4d+L1DMsIb3DOh7zxL/4NWyllOCsZlS9MrEzTPgSnhtltTiz6q5O+BRTO+MVn7Mr7FsjaEYt5znfrpEHPNVZHrPAdCr/ODiVj5/9V7NqXyaaIgYBedTmnTGy8FTCNqwjQ==";

                        existingUidCommand.CommandText =
                            //// before
                            //
                            //"SELECT ServerUids.ServerUid, FileSystemObjects.CalculatedFullPath " +
                            //"FROM FileSystemObjects " +
                            //"INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidID " +
                            //"WHERE FileSystemObjects.CalculatedFullPathCIHashes = ? " + // <-- parameter 1
                            //"ORDER BY " +
                            //"CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                            //"THEN 0 " +
                            //"ELSE FileSystemObjects.EventOrder " +
                            //"END DESC"
                            //
                            //// after (decrypted)
                            //SELECT ServerUids.ServerUid, FileSystemObjects.CalculatedFullPath
                            //FROM FileSystemObjects
                            //INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidID
                            //WHERE FileSystemObjects.CalculatedFullPathCIHashes = ?
                            //ORDER BY
                            //CASE WHEN FileSystemObjects.EventOrder IS NULL
                            //THEN 0
                            //ELSE FileSystemObjects.EventOrder
                            //END DESC
                            Helpers.DecryptString(
                                selectServerUidByFullPathHash,
                                Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword)));

                        ISQLiteParameter pathCIHashesParam = existingUidCommand.CreateParameter();
                        pathCIHashesParam.Value = pathCIHashes;
                        existingUidCommand.Parameters.Add(pathCIHashesParam);

                        using (ISQLiteDataReader existingObjectReader = existingUidCommand.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            serverUid = null;

                            while (existingObjectReader.Read())
                            {
                                if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(existingObjectReader[Resources.NotTranslatedSqlIndexerCalculatedFullPath]), newPath))
                                {
                                    serverUid = Convert.ToString(existingObjectReader[Resources.NotTranslatedSqlIndexerServerUid]);

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                serverUid = Helpers.DefaultForType<string>();

                return ex;
            }
            return null;
        }

        //// whole method removed because SyncedObject class was removed (no more server-linked sync states since ServerName is a property of FileSystemObject)
        //
        ///// <summary>
        ///// Retrieve the complete file system state at the time of the last sync
        ///// </summary>
        ///// <param name="syncStates">Outputs the file system state</param>
        ///// <returns>Returns an error that occurred retrieving the file system state, if any</returns>
        //public CLError GetLastSyncStates(out FilePathDictionary<SyncedObject> syncStates)
        //{
            //if (disposed)
            //{
            //    try
            //    {
            //        throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
            //    }
            //    catch (Exception ex)
            //    {
            //        return ex;
            //    }
            //}
        //    throw new NotImplementedException("2");
        //    //ExternalSQLLocker.EnterReadLock();
        //    //try
        //    //{
        //    //    using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
        //    //    {
        //    //        // Pull the last sync from the database
        //    //        SqlSync lastSync = SqlAccessor<SqlSync>
        //    //            .SelectResultSet(indexDB,
        //    //                //// before // // "SELECT TOP 1 * FROM [Syncs] ORDER BY [Syncs].[SyncCounter] DESC" // //// after // // [missing]
        //    //                )
        //    //            .SingleOrDefault();

        //    //        // Default the sync states (to null) if there was never a sync
        //    //        if (lastSync == null)
        //    //        {
        //    //            syncStates = Helpers.DefaultForType<FilePathDictionary<SyncedObject>>();
        //    //        }
        //    //        // If there was a sync, continue on to build the sync state
        //    //        else
        //    //        {
        //    //            // Create the dictionary of sync states to output
        //    //            CLError createDictError = FilePathDictionary<SyncedObject>.CreateAndInitialize(lastSync.RootPath,
        //    //                out syncStates);
        //    //            if (createDictError != null)
        //    //            {
        //    //                return createDictError;
        //    //            }

        //    //            Dictionary<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> mappedSyncStates = new Dictionary<long,KeyValuePair<GenericHolder<FileSystemObject>,GenericHolder<FileSystemObject>>>();

        //    //            // Loop through all sync states for the last sync
        //    //            foreach (FileSystemObject currentSyncState in SqlAccessor<FileSystemObject>
        //    //                .SelectResultSet(indexDB,
        //    //                    //// before // // "SELECT * FROM [FileSystemObjects] WHERE [FileSystemObjects].[SyncCounter] = " + lastSync.SyncCounter // //// after // // [missing]
        //    //                    ))
        //    //            {
        //    //                if (mappedSyncStates.ContainsKey(currentSyncState.FileSystemObjectId))
        //    //                {
        //    //                    if (currentSyncState.ServerLinked)
        //    //                    {
        //    //                        mappedSyncStates[currentSyncState.FileSystemObjectId].Value.Value = currentSyncState;
        //    //                    }
        //    //                    else
        //    //                    {
        //    //                        mappedSyncStates[currentSyncState.FileSystemObjectId].Key.Value = currentSyncState;
        //    //                    }
        //    //                }
        //    //                else if (currentSyncState.ServerLinked)
        //    //                {
        //    //                    mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
        //    //                        new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
        //    //                            new GenericHolder<FileSystemObject>(),
        //    //                            new GenericHolder<FileSystemObject>(currentSyncState)));
        //    //                }
        //    //                else
        //    //                {
        //    //                    mappedSyncStates.Add(currentSyncState.FileSystemObjectId,
        //    //                        new KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>(
        //    //                            new GenericHolder<FileSystemObject>(currentSyncState),
        //    //                            new GenericHolder<FileSystemObject>()));
        //    //                }
        //    //            }

        //    //            foreach (KeyValuePair<long, KeyValuePair<GenericHolder<FileSystemObject>, GenericHolder<FileSystemObject>>> currentSyncState in mappedSyncStates)
        //    //            {
        //    //                // Add the current sync state from the last sync to the output dictionary
        //    //                syncStates.Add(currentSyncState.Value.Key.Value.Path,
        //    //                    new SyncedObject()
        //    //                    {
        //    //                        ServerLinkedPath = currentSyncState.Value.Value.Value == null
        //    //                            ? null
        //    //                            : currentSyncState.Value.Value.Value.Path,
        //    //                        Metadata = new FileMetadata()
        //    //                        {
        //    //                            // TODO: add server id
        //    //                            HashableProperties = new FileMetadataHashableProperties(currentSyncState.Value.Key.Value.IsFolder,
        //    //                                currentSyncState.Value.Key.Value.LastTime,
        //    //                                currentSyncState.Value.Key.Value.CreationTime,
        //    //                                currentSyncState.Value.Key.Value.Size),
        //    //                            LinkTargetPath = currentSyncState.Value.Key.Value.TargetPath,
        //    //                            Revision = currentSyncState.Value.Key.Value.Revision,
        //    //                            StorageKey = currentSyncState.Value.Key.Value.StorageKey
        //    //                        }
        //    //                    });
        //    //            }
        //    //        }
        //    //    }
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    syncStates = null;
        //    //    return ex;
        //    //}
        //    //finally
        //    //{
        //    //    ExternalSQLLocker.ExitReadLock();
        //    //}
        //    //return null;
        //}

        public CLError GetMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    metadata = Helpers.DefaultForType<FileMetadata>();

                    return ex;
                }
            }

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new NullReferenceException("path cannot be null");
                }

                FilePath pathObject = path;

                List<string> namePortions = new List<string>();

                while (!FilePathComparer.Instance.Equals(pathObject, indexedPath))
                {
                    namePortions.Add(pathObject.Name);

                    pathObject = pathObject.Parent;
                }

                namePortions.Add(indexedPath);

                namePortions.Reverse();

                string pathCIHashes = string.Join(((char)0x5c /* \ */).ToString(),
                    namePortions.Select(currentPortion => StringComparer.OrdinalIgnoreCase.GetHashCode(currentPortion).ToString()));

                using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                {
                    const string selectFileSystemObjectByFullPathHash = "t3Ee1ulQLjs62aHw5E7nEHC3Yt6pkKQiMjDOMA00p+Qo1PZHGpfRx91FJNSloGZ3xDH11QktFYyaPHTl7mAN/XsQPT9eyFcyOPzFnJGIGwO7e3uf9CaURqWU8Fck+B4hGqcB1XKdle+E3yJPqcg821CpRNu1/VdZifFtH/jz/TRaoON1XE/nxcfUWCCpyHZQavqxfWY8yic2EYWylYcApe4I9V2ctpQkFQQ/dk+aEVcae+yRZFdt3L/WRzuRH9oObDpG0ciS9Z8wfe5ChHgQDiLJXyQjKX6H8g1kvADUWfIQNb8o782MqyEfLEJfEAJlYX+0hwdWcoHKppIB0rItpwUJyhgzJ8v05jUA8tugjcPE0OFZ70hXYYsl83LwHiMSf5FDCToVE13XZJZrTJ0XA2DXhA3i31hiTwULvLU9Q0PwSptGfv/guDZ1IyrX3kWe8LLZ+5t7UVbq6GL2Fw+vISBRGTuXZaVvlZtKBABg+pT71ZPki1lyAfAZsP0bvkAT/DNrYVXDsD+HGfQGmvfSLys1gy1ngUg3CaIwIOOOx5fBMLJh534k91qQkkjBOSTLroxatmjMsTcxf78eMaYDtjbKOdldQU7hOSjVEbOrbqFxzJ2NXBfEzmsVoVbZV8tYFV/Ams7VUv7SfPyp2Fatw1vO4qguJhaxZv+2bflDTv6KjFdUVR6UA3RgUC38Z/6HsEkmpPP1A2wqFo48c0deKQ==";
                    const string conditionalWhereRevision = "rNW69hWt8J3a4azuZvJQbreriKnw3+XuHGnmYWaF38EQc7HQdmzg3V2uMC74da2lJ+nD7DeQlmItMZj/8v+VcA==";

                    FileSystemObject existingNonPending = SqlAccessor<FileSystemObject>.SelectResultSet(
                            indexDB,
                            //// before
                            //
                            //// selectFileSystemObjectByFullPathHash
                            //
                            //"SELECT * " +
                            //    "FROM FileSystemObjects " +
                            //    "INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidId " +
                            //    "WHERE CalculatedFullPathCIHashes = ? " + // <-- parameter 1
                            //    (revision == null
                            //        ? string.Empty
                            //        : conditionalWhereRevision) + // <-- conditional parameter 2
                            //    "ORDER BY " +
                            //    "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                            //    "THEN 0 " +
                            //    "ELSE FileSystemObjects.EventOrder " +
                            //    "END DESC"
                            //
                            //// conditionalWhereRevision
                            //
                            //"AND ServerUids.Revision = ? "
                            //
                            //// after (decrypted; {0}: (revision == null ? string.Empty : conditionalWhereRevision [decrypted]) )
                            //
                            //// selectFileSystemObjectByFullPathHash
                            //SELECT *
                            //FROM FileSystemObjects
                            //INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidId
                            //WHERE CalculatedFullPathCIHashes = ?
                            //{0}
                            //ORDER BY
                            //CASE WHEN FileSystemObjects.EventOrder IS NULL
                            //THEN 0
                            //ELSE FileSystemObjects.EventOrder
                            //END DESC
                            //
                            //// conditionalWhereRevision
                            //
                            //AND ServerUids.Revision = ?
                            string.Format(
                                Helpers.DecryptString(
                                    selectFileSystemObjectByFullPathHash,
                                    Encoding.ASCII.GetString(
                                        Convert.FromBase64String(indexDBPassword))),
                                (revision == null
                                    ? string.Empty
                                    : Helpers.DecryptString(
                                        conditionalWhereRevision,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword))))),

                                selectParameters: (revision == null ? Helpers.EnumerateSingleItem(pathCIHashes) : new[] { pathCIHashes, revision }))
                        .FirstOrDefault(currentNonPending => StringComparer.OrdinalIgnoreCase.Equals(currentNonPending.CalculatedFullPath, path));

                    if (existingNonPending == null)
                    {
                        throw new KeyNotFoundException("Unable to find existing FileSystemObject by path" + (revision == null ? string.Empty : " and revision"));
                    }

                    metadata = new FileMetadata(existingNonPending.ServerUidId)
                    {
                        EventTime = new DateTime(existingNonPending.EventTimeUTCTicks, DateTimeKind.Utc),
                        HashableProperties = new FileMetadataHashableProperties(
                            existingNonPending.IsFolder,
                            (existingNonPending.LastTimeUTCTicks == null
                                ? (Nullable<DateTime>)null
                                : new DateTime((long)existingNonPending.LastTimeUTCTicks, DateTimeKind.Utc)),
                            (existingNonPending.CreationTimeUTCTicks == null
                                ? (Nullable<DateTime>)null
                                : new DateTime((long)existingNonPending.CreationTimeUTCTicks, DateTimeKind.Utc)),
                            existingNonPending.Size),
                        IsShare = existingNonPending.IsShare,
                        MimeType = existingNonPending.MimeType,
                        Permissions = (existingNonPending.Permissions == null
                            ? (Nullable<POSIXPermissions>)null
                            : (POSIXPermissions)((int)existingNonPending.Permissions)),
                        StorageKey = existingNonPending.StorageKey,
                        Version = existingNonPending.Version
                    };
                }
            }
            catch (Exception ex)
            {
                metadata = Helpers.DefaultForType<FileMetadata>();

                return ex;
            }
            return null;
        }

        ///// <summary>
        ///// Retrieves all unprocessed events that occurred since the last sync
        ///// </summary>
        ///// <param name="changeEvents">Outputs the unprocessed events</param>
        ///// <returns>Returns an error that occurred filling the unprocessed events, if any</returns>
        //public CLError GetPendingEvents(out List<KeyValuePair<FilePath, FileChange>> changeEvents)
        //{            //if (disposed)
            //{
            //    try
            //    {
            //        throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
            //    }
            //    catch (Exception ex)
            //    {
            //        return ex;
            //    }
            //}
        //
        //
        //    ExternalSQLLocker.EnterReadLock();
        //    try
        //    {
        //        using (SqlCeConnection indexDB = new SqlCeConnection(buildConnectionString(this.indexDBLocation)))
        //        {
        //            // Create the output list
        //            changeEvents = new List<KeyValuePair<FilePath, FileChange>>();

        //            // Loop through all the events in the database after the last sync (if any)
        //            foreach (Event currentChange in
        //                SqlAccessor<Event>
        //                    .SelectResultSet(indexDB,
        //                        //// before
        //                        //
        //                        //"SELECT " +
        //                        //SqlAccessor<Event>.GetSelectColumns() + ", " +
        //                        //SqlAccessor<FileSystemObject>.GetSelectColumns(FileSystemObject.Name) +
        //                        //"FROM [Events] " +
        //                        //"INNER JOIN [FileSystemObjects] ON [Events].[EventId] = [FileSystemObjects].[EventId] " +
        //                        //"WHERE [FileSystemObjects].[SyncCounter] IS NULL " +
        //                        //"ORDER BY [Events].[EventId]"
        //                        //
        //                        //// after (decrypted)
        //                        //
        //                        // [missing]
        //                        new string[]
        //                        {
        //                            FileSystemObject.Name
        //                        }))
        //            {
        //                // For each event since the last sync (if any), add to the output dictionary
        //                changeEvents.Add(new KeyValuePair<FilePath, FileChange>(currentChange.FileSystemObject.Path,
        //                    new FileChange()
        //                    {
        //                        NewPath = currentChange.FileSystemObject.Path,
        //                        OldPath = currentChange.PreviousPath,
        //                        Type = changeEnums[currentChange.FileChangeTypeEnumId],
        //                        Metadata = new FileMetadata()
        //                        {
        //                            // TODO: add server id
        //                            HashableProperties = new FileMetadataHashableProperties(currentChange.FileSystemObject.IsFolder,
        //                                currentChange.FileSystemObject.LastTime,
        //                                currentChange.FileSystemObject.CreationTime,
        //                                currentChange.FileSystemObject.Size),
        //                            Revision = currentChange.FileSystemObject.Revision,
        //                            StorageKey = currentChange.FileSystemObject.StorageKey,
        //                            LinkTargetPath = currentChange.FileSystemObject.TargetPath
        //                        },
        //                        Direction = (currentChange.SyncFrom ? SyncDirection.From : SyncDirection.To)
        //                    }));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        changeEvents = Helpers.DefaultForType<List<KeyValuePair<FilePath, FileChange>>>();
        //        return ex;
        //    }
        //    finally
        //    {
        //        ExternalSQLLocker.ExitReadLock();
        //    }
        //    return null;
        //}

        /// <summary>
        /// Adds an unprocessed change since the last sync as a new event to the database,
        /// EventId property of the input event is set after database update
        /// </summary>
        /// <param name="newEvents">Change to add</param>
        /// <returns>Returns error that occurred when adding the event to database, if any</returns>
        public CLError AddEvents(IEnumerable<FileChange> newEvents, SQLTransactionalBase existingTransaction = null)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            return AddEvents(null, newEvents, existingTransaction);
        }
        private CLError AddEvents(Nullable<long> syncCounter, IEnumerable<FileChange> newEvents, SQLTransactionalBase existingTransaction, bool addCreateAtOldPathIfNotFound = false)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            CLError toReturn = null;
            try
            {
                // Ensure input parameter is set
                if (newEvents == null)
                {
                    throw new NullReferenceException("newEvents cannot be null");
                }

                FileChange[] newEventsArray;
                {
                    List<FileChange> newEventsList = new List<FileChange>();
                    foreach (FileChange currentEvent in newEvents)
                    {
                        if (currentEvent.Metadata == null)
                        {
                            throw new NullReferenceException("The Metadata property of every newEvent cannot be null");
                        }

                        newEventsList.Add(currentEvent);
                    }
                    newEventsArray = newEventsList.ToArray();
                }

                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                int lastHighestChangeIndex;
                int currentChangeIndex = 0;

                // template is only used to define the type structure needed to add a FileChange to the database
                // the values from this specific object instance are never copied anywhere and are never read
                var batchedItemTemplate = new
                {
                    change = (FileChange)null, // force FileChange type
                    parentFolderId = 0L, // force signed 64-bit integer type
                    previousId = (Nullable<long>)null // force nullable of signed 64-bit integer type
                };

                //// template is only used to define the type structure needed to track paths as the result of FileChanges;
                //// the values from this specific object instance are never copied anywhere and are never read
                //var batchedPathTrackingValueTemplate = new
                //    {
                //        previousPath = (FilePath)null, // force FilePath type; null value will represent that the object had not already existed in the database (such as on a create event in the same batch)
                //        objectId = 0L // force signed 64-bit integer type
                //    };

                FilePath indexedPathObject = indexedPath;

                Dictionary<long, Event> eventsByIdForPendingRevision = new Dictionary<long, Event>();

                do
                {
                    lastHighestChangeIndex = currentChangeIndex;

                    var currentBatchToAddList = Helpers.CreateEmptyListFromTemplate(batchedItemTemplate);
                    FilePathDictionary<object> currentBatchTrackPathChanges;
                    CLError createCurrentBatchTrackPathChangesError = FilePathDictionary<object>.CreateAndInitialize(
                        indexedPathObject,
                        out currentBatchTrackPathChanges);
                    //var currentBatchTrackPathChangesPair = Helpers.CreateEmptyFilePathDictionaryFromTemplate(
                    //    indexedPathObject,
                    //    batchedPathTrackingValueTemplate);

                    if (createCurrentBatchTrackPathChangesError != null)
                    {
                        throw new AggregateException("Unable to make a FilePathDictionary with current indexedPath", createCurrentBatchTrackPathChangesError.Exceptions);
                    }
                    //if (currentBatchTrackPathChangesPair.Value != null)
                    //{
                    //    throw new AggregateException("Unable to make a FilePathDictionary from anonymous type template batchedPathTrackingValueTemplate with current indexedPath", currentBatchTrackPathChangesPair.Value.Exceptions);
                    //}

                    //var currentBatchTrackPathChanges = currentBatchTrackPathChangesPair.Key;

                    for (/* currentChangeIndex defined above */ ; currentChangeIndex < newEventsArray.Length; currentChangeIndex++)
                    {
                        try
                        {
                            FileChange currentObjectToBatch = newEventsArray[currentChangeIndex];

                            if (currentObjectToBatch.DoNotAddToSQLIndex) // skip adding current event since it was marked DoNotAddToSQLIndex
                            {
                                continue;
                            }

                            long parentFolderId;
                            Nullable<long> previousId;

                            // prefers the latest rename which is pending,
                            // otherwise prefers non-pending,
                            // last take most recent event

                            Func<SQLTransactionalImplementation, FilePath, Nullable<long>> objectIdByPathSelect = (innerCastTransaction, pathToSearch) =>
                            {
                                List<string> namePortions = new List<string>();

                                FilePath currentParentPath = pathToSearch;

                                while (!FilePathComparer.Instance.Equals(currentParentPath, indexedPathObject))
                                {
                                    namePortions.Add(currentParentPath.Name);

                                    currentParentPath = currentParentPath.Parent;
                                }

                                namePortions.Add(indexedPath);

                                namePortions.Reverse();

                                string pathCIHashes = string.Join(((char)0x5c /* \ */).ToString(),
                                    namePortions.Select(currentPortion => StringComparer.OrdinalIgnoreCase.GetHashCode(currentPortion).ToString()));

                                using (ISQLiteCommand objectIdSearchCommand = innerCastTransaction.sqlConnection.CreateCommand())
                                {
                                    objectIdSearchCommand.Transaction = innerCastTransaction.sqlTransaction;

                                    const string selectFileSystemObjectIdByFullPathHash = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYM4A9CJRjJ2AJWbK+2XeEfjsHjbyLTBN0A1N3nA0Q4qVNOEMU6uWFsvHIyt+vO7mRotjaQ6thfD0JG4dEBb6CwJ89vQ95+fzquzuLa1c96VgT54coYBhS2C3tyaw2edsBVJbPHBY9AoOd9jpwEk6+Ny/P2L3tYZ6e5uNxcENQzO/pwOGbYha42a1U+38dVMSoqKkQlnmRH0i19cmrh06Kfa+HDXNoDtQGeRTdcqYxQ24UwH4iTCkbvmopF3okXukWm6IiVorCtfxanLcCgfSmsTYVyRksXaRRsiGGlY3B2CMrxHZ8fRTMEdXK+uxPUo09L+rVnigXHTZ59cdYTr7/Mtv63p8+MDJ0W5PQU6SxYTNMhpbbbxMXsgBAVOOY13PbeH+xv1sOrTlFHg2WNKC/LTASc/P6eagesR9E4M3YHu6G9pX8jX0vht0wwmhHJMKyoUWgZ/X0smf/FBqPjecsz2RG48AXFJssSbtOe4L8HaWXJZYGVGcJzRFaqa1URGRw6w60pQhzRiAGx7uPJzJhpBfmzkeFX0Acehvo84RdpN/I2hyYGYkyM/1hyP8DaH33oGKX/4h3ljgOoSWHg+wSiztzJkdUfD5txj9u36njaf194EP1/BBT0EI38d2caDWqLOioEZrRu1OeJ72tikM5770H28wY8m+TQdXHC39MBEKtfZC97KqF9kgPxU2Yq3GuzfJ57DH875K/31mKJg3o23MhwWbdtaCSB0rL6aExXg5NoqvTzskiby0MkagZNrn6jROJ4jWiSsAfyP2INKwPoEJB7RWpTaR/2x8y1mCjP6Ks5OKNC4tV7xDz3byNP1DrSUBKKY50WyZtQfgjDwTqOYm2H/NN+BIxLhIM8mWWy0SLiTWQSq71UedyEtNxAxnTbznMjX4/uL7s9ftatza7sn8lwuc6Hdw0tIIV/WpXQUA7JB+KB2msX6DoafQbMe60ySKHVTijiNB86EtgKWXcJTbgtWNTnk/9rwBJPRV7IDbCQg+JWjo/qSipmOONAcA96Xp9DTePG2ih2UglnxoHnsubeKvhfdW1rM9GTZvsPYvQbxQEWtdTv0E5el0ryg1BjYI9K1QfTiKChVK9xYb/NdzQTbQ9j8zViX1QmUy1aVJVZGs49HFDKGYdzcORuugiTFBG2e3Arcn5NO4/YlWK7PHsgUe8TNpuyx8dyvNbzKanOt+mYKpBEUAU4Mw2pf3kpTPKTncuqsf1vMLZ4t7dFV9GfjLCWFD5uX+8M19lBiWwiCbvw6bsHA0c4klrWBY5HNr4H91hqIPiHhkU4dOaecMzbUGhttMzdCPYYDOBHKcYMZOaoLyM9znYtGJON+KaY";

                                    objectIdSearchCommand.CommandText =
                                        //// before
                                        //
                                        //"SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.CalculatedFullPath " +
                                        //    "FROM FileSystemObjects " +
                                        //    "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                                        //    "WHERE FileSystemObjects.CalculatedFullPathCIHashes = ? " + // <-- parameter 1
                                        //    "ORDER BY " +
                                        //    "CASE WHEN FileSystemObjects.EventId IS NOT NULL " +
                                        //    "AND Events.FileChangeTypeEnumId = " + changeEnumsBackward[FileChangeType.Renamed] +
                                        //    " AND FileSystemObjects.Pending = 1 " +
                                        //    "THEN 0 " +
                                        //    "ELSE 1 " +
                                        //    "END ASC, " +
                                        //    "FileSystemObjects.Pending ASC, " +
                                        //    "CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                        //    "THEN 0 " +
                                        //    "ELSE FileSystemObjects.EventOrder " +
                                        //    "END DESC"
                                        //
                                        //// after (decrypted; {0}: changeEnumsBackward[FileChangeType.Renamed])
                                        //
                                        //SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.CalculatedFullPath
                                        //FROM FileSystemObjects
                                        //LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId
                                        //WHERE FileSystemObjects.CalculatedFullPathCIHashes = ?
                                        //ORDER BY
                                        //CASE WHEN FileSystemObjects.EventId IS NOT NULL
                                        //AND Events.FileChangeTypeEnumId = {0}
                                        //AND FileSystemObjects.Pending = 1
                                        //THEN 0
                                        //ELSE 1
                                        //END ASC,
                                        //FileSystemObjects.Pending ASC,
                                        //CASE WHEN FileSystemObjects.EventOrder IS NULL
                                        //THEN 0
                                        //ELSE FileSystemObjects.EventOrder
                                        //END DESC
                                        string.Format(
                                            Helpers.DecryptString(
                                                selectFileSystemObjectIdByFullPathHash,
                                                Encoding.ASCII.GetString(
                                                    Convert.FromBase64String(indexDBPassword))),
                                            changeEnumsBackward[FileChangeType.Renamed]);

                                    ISQLiteParameter pathHashesParam = objectIdSearchCommand.CreateParameter();
                                    pathHashesParam.Value = pathCIHashes;
                                    objectIdSearchCommand.Parameters.Add(pathHashesParam);

                                    using (ISQLiteDataReader objectIdReader = objectIdSearchCommand.ExecuteReader(CommandBehavior.SingleResult))
                                    {
                                        while (objectIdReader.Read())
                                        {
                                            if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(objectIdReader[Resources.NotTranslatedSqlIndexerCalculatedFullPath]), pathToSearch.ToString()))
                                            {
                                                return Convert.ToInt64(objectIdReader[Resources.NotTranslatedSqlIndexerFileSystemObjectId]);
                                            }
                                        }
                                    }
                                }

                                return null;
                            };

                            const string missingParentErrorMessage =
                                "Unable to add a new FileSystemObject without a parent folder ID";

                            const string missingPreviousErrorMessage =
                                "Unable to add a rename Event without a previous ID";

                            bool foundExistingPathInSearch = false;
                            //long searchResultObjectId = 0;
                            //FilePath searchResultPreviousPath = null;

                            FilePath currentObjectParentPathSearch = currentObjectToBatch.NewPath.Parent;
                            while (!FilePathComparer.Instance.Equals(currentObjectParentPathSearch, indexedPathObject))
                            {
                                var pathSearchResult = Helpers.DictionaryTryGetValue(currentBatchTrackPathChanges, currentObjectParentPathSearch);

                                if (pathSearchResult.Success)
                                {
                                    foundExistingPathInSearch = true;
                                    //searchResultObjectId = pathSearchResult.Value.objectId;
                                    //searchResultPreviousPath = pathSearchResult.Value.previousPath;

                                    break;
                                }

                                currentObjectParentPathSearch = currentObjectParentPathSearch.Parent;
                            }

                            Nullable<long> parentFolderIdTemp;
                            if (foundExistingPathInSearch)
                            {
                                // existing event along the current change's parent paths has not yet been committed to database so we break this batch until the previous batch is added
                                break;

                                //if (searchResultPreviousPath == null)
                                //{
                                //    // existing event along the current change's parent paths has not yet been committed to database so we break this batch until the previous batch is added
                                //    break;
                                //}
                                //else if (FilePathComparer.Instance.Equals(currentObjectParentPathSearch, currentObjectToBatch.NewPath.Parent))
                                //{
                                //    parentFolderId = (long)searchResultObjectId;
                                //}
                                //else
                                //{
                                //    FilePath renamedNewPathParent = currentObjectToBatch.NewPath.Parent.Copy();
                                //    FilePath.ApplyRename(renamedNewPathParent, currentObjectParentPathSearch, searchResultPreviousPath);

                                //    if (!SqlAccessor<object>.TrySelectScalar(
                                //        castTransaction.sqlConnection,
                                //        objectIdByPathSelect,
                                //        out parentFolderId,
                                //        castTransaction.sqlTransaction,
                                //        selectParameters: Helpers.EnumerateSingleItem(renamedNewPathParent)))
                                //    {
                                //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingParentErrorMessage);
                                //    }
                                //}
                            }
                            else if ((parentFolderIdTemp = objectIdByPathSelect(castTransaction, currentObjectToBatch.NewPath.Parent)) == null)
                            {
                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingParentErrorMessage);
                            }
                            else
                            {
                                parentFolderId = (long)parentFolderIdTemp;
                            }

                            if (currentObjectToBatch.OldPath == null)
                            {
                                previousId = null;
                            }
                            else
                            {
                                foundExistingPathInSearch = false;
                                //searchResultObjectId = 0;
                                //searchResultPreviousPath = null;

                                currentObjectParentPathSearch = currentObjectToBatch.OldPath;
                                while (!FilePathComparer.Instance.Equals(currentObjectParentPathSearch, indexedPathObject))
                                {
                                    var pathSearchResult = Helpers.DictionaryTryGetValue(currentBatchTrackPathChanges, currentObjectParentPathSearch);

                                    if (pathSearchResult.Success)
                                    {
                                        foundExistingPathInSearch = true;
                                        //searchResultObjectId = pathSearchResult.Value.objectId;
                                        //searchResultPreviousPath = pathSearchResult.Value.previousPath;

                                        break;
                                    }

                                    currentObjectParentPathSearch = currentObjectParentPathSearch.Parent;
                                }

                                if (foundExistingPathInSearch)
                                {
                                    // existing event along the current change's previous path and its parents has not yet been committed to database so we break this batch until the previous batch is added
                                    break;

                                    //if (searchResultPreviousPath == null)
                                    //{
                                    //    // existing event along the current change's previous path and its parents has not yet been committed to database so we break this batch until the previous batch is added
                                    //    break;
                                    //}
                                    //else if (FilePathComparer.Instance.Equals(currentObjectParentPathSearch, currentObjectToBatch.OldPath))
                                    //{
                                    //    previousId = searchResultObjectId;
                                    //}
                                    //else
                                    //{
                                    //    FilePath renamedNewPathParent = currentObjectToBatch.OldPath.Copy();
                                    //    FilePath.ApplyRename(renamedNewPathParent, currentObjectParentPathSearch, searchResultPreviousPath);

                                    //    if (!SqlAccessor<object>.TrySelectScalar(
                                    //        castTransaction.sqlConnection,
                                    //        objectIdByPathSelect,
                                    //        out previousIdNotNull,
                                    //        castTransaction.sqlTransaction,
                                    //        selectParameters: Helpers.EnumerateSingleItem(renamedNewPathParent)))
                                    //    {
                                    //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingPreviousErrorMessage);
                                    //    }

                                    //    previousId = previousIdNotNull;
                                    //}
                                }
                                else if ((previousId = objectIdByPathSelect(castTransaction, currentObjectToBatch.OldPath)) == null)
                                {
                                    if (addCreateAtOldPathIfNotFound) // added condition when the first event on a file is a sync to conflict and the file has to be renamed to the conflict path without another existing event
                                    {
                                        Nullable<long> oldPathParentId;
                                        if ((oldPathParentId = objectIdByPathSelect(castTransaction, currentObjectToBatch.NewPath.Parent)) == null)
                                        {
                                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingParentErrorMessage);
                                        }

                                        previousId = SqlAccessor<FileSystemObject>.InsertRow<long>(
                                            castTransaction.sqlConnection,
                                            new FileSystemObject()
                                            {
                                                IsFolder = currentObjectToBatch.Metadata.HashableProperties.IsFolder,
                                                Name = currentObjectToBatch.OldPath.Name,
                                                NameCIHash = StringComparer.OrdinalIgnoreCase.GetHashCode(currentObjectToBatch.OldPath.Name),
                                                ParentFolderId = oldPathParentId,
                                                Pending = false,
                                                ServerUidId = currentObjectToBatch.Metadata.ServerUidId,
                                                Size = (currentObjectToBatch.Metadata.HashableProperties.IsFolder ? (Nullable<long>)null : 0)
                                            },
                                            transaction: castTransaction.sqlTransaction);
                                    }
                                    else
                                    {
                                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, missingPreviousErrorMessage);
                                    }
                                }
                            }

                            currentBatchToAddList.Add(new
                            {
                                change = currentObjectToBatch,
                                parentFolderId = parentFolderId,
                                previousId = previousId
                            });

                            switch (currentObjectToBatch.Type)
                            {
                                case FileChangeType.Created:
                                    currentBatchTrackPathChanges[currentObjectToBatch.NewPath]
                                        = new object();
                                    //new
                                    //{
                                    //    previousPath = (FilePath)null,
                                    //    objectId = 0L
                                    //});
                                    break;

                                case FileChangeType.Deleted:
                                    currentBatchTrackPathChanges.Remove(currentObjectToBatch.NewPath);
                                    break;

                                case FileChangeType.Renamed:
                                    var existingRenamePair = Helpers.DictionaryTryGetValue(currentBatchTrackPathChanges, currentObjectToBatch.OldPath);

                                    FilePathHierarchicalNode<object> oldPathHierarchy;
                                    CLError oldPathHierarchyError = currentBatchTrackPathChanges.GrabHierarchyForPath(currentObjectToBatch.OldPath, out oldPathHierarchy, suppressException: true);

                                    if (oldPathHierarchyError == null
                                        && oldPathHierarchy != null)
                                    {
                                        currentBatchTrackPathChanges.Rename(currentObjectToBatch.OldPath, currentObjectToBatch.NewPath);
                                    }

                                    if (!existingRenamePair.Success)
                                    {
                                        currentBatchTrackPathChanges[currentObjectToBatch.NewPath] =
                                            new object();
                                        //new
                                        //{
                                        //    previousPath = currentObjectToBatch.OldPath,
                                        //    objectId = ?? <-- this is where I realized there is nothing to track forward through renames except the path key in the dictionary
                                        //});
                                    }
                                    break;

                                //case FileChangeType.Modified: // <-- don't do anything with modified since it doesn't affect the FileSystemObjectId at any path
                            }
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    List<Event> eventsToAdd = new List<Event>();
                    Guid eventGroup = Guid.NewGuid();
                    int eventCounter = 0;
                    Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>> orderToChange = new Dictionary<int, KeyValuePair<FileChange, GenericHolder<long>>>();

                    // If change is marked for adding to SQL,
                    // then process database addition
                    foreach (var newEvent in currentBatchToAddList)
                    {
                        eventCounter++;
                        orderToChange.Add(eventCounter, new KeyValuePair<FileChange, GenericHolder<long>>(newEvent.change, new GenericHolder<long>()));

                        DateTime storeCreationTimeUTC;
                        DateTime storeLastTimeUTC;

                        byte[] getMD5 = newEvent.change.MD5;

                        // Define the new event to add for the unprocessed change
                        eventsToAdd.Add(new Event()
                        {
                            FileChangeTypeCategoryId = changeCategoryId,
                            FileChangeTypeEnumId = changeEnumsBackward[newEvent.change.Type],
                            FileSystemObject = new FileSystemObject()
                            {
                                CreationTimeUTCTicks = ((newEvent.change.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        || (storeCreationTimeUTC = newEvent.change.Metadata.HashableProperties.CreationTime.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                                    ? (Nullable<long>)0
                                    : storeCreationTimeUTC.Ticks),
                                EventTimeUTCTicks = DateTime.UtcNow.Ticks,
                                IsFolder = newEvent.change.Metadata.HashableProperties.IsFolder,
                                IsShare = newEvent.change.Metadata.IsShare,
                                LastTimeUTCTicks = ((newEvent.change.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        || (storeLastTimeUTC = newEvent.change.Metadata.HashableProperties.LastTime.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                                    ? (Nullable<long>)0
                                    : storeLastTimeUTC.Ticks),
                                MD5 = getMD5,
                                MimeType = newEvent.change.Metadata.MimeType,
                                Name = newEvent.change.NewPath.Name,
                                NameCIHash = StringComparer.OrdinalIgnoreCase.GetHashCode(newEvent.change.NewPath.Name),
                                ParentFolderId = newEvent.parentFolderId,
                                Pending = true,
                                Permissions = (newEvent.change.Metadata.Permissions == null ? (Nullable<int>)null : (int)((POSIXPermissions)newEvent.change.Metadata.Permissions)),
                                ServerUidId = newEvent.change.Metadata.ServerUidId,
                                //ServerName = newEvent.change.ServerPath // <-- need to add server paths to FileChange
                                Size = newEvent.change.Metadata.HashableProperties.Size,
                                StorageKey = newEvent.change.Metadata.StorageKey,
                                SyncCounter = syncCounter,
                                Version = newEvent.change.Metadata.Version
                            },
                            GroupId = eventGroup,
                            GroupOrder = eventCounter,
                            PreviousId = newEvent.previousId,
                            SyncFrom = (newEvent.change.Direction == SyncDirection.From)
                        });
                    }

                    if (eventsToAdd.Count > 0)
                    {
                        SqlAccessor<Event>.InsertRows(
                            castTransaction.sqlConnection,
                            eventsToAdd,
                            transaction: castTransaction.sqlTransaction);

                        Dictionary<int, long> groupOrderToId = new Dictionary<int, long>();

                        const string selectEventByGroupId = "t3Ee1ulQLjs62aHw5E7nEH7dXOynUtuW/12FIdbVC0CWiX+fSAMaI9sQqSNRe1ZtgB/byN+8ZmriXy9OJpaeBHEHVZEBlf6r6DA+CDovjutSkvjNpAWd5HNr+LyqOFXe";

                        foreach (Event createdEvent in SqlAccessor<Event>.SelectResultSet(
                            castTransaction.sqlConnection,
                            //// before
                            //
                            // "SELECT * FROM Events WHERE Events.GroupId = ?"
                            //
                            //// after (decrypted)
                            //
                            //SELECT * FROM Events WHERE Events.GroupId = ?
                            Helpers.DecryptString(
                                selectEventByGroupId,
                                Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword))),
                            transaction: castTransaction.sqlTransaction,
                            selectParameters: Helpers.EnumerateSingleItem(eventGroup)))
                        {
                            groupOrderToId.Add((int)createdEvent.GroupOrder, createdEvent.EventId);
                        }

                        Func<Event, FileSystemObject> setIdAndGrabObject = currentEvent =>
                        {
                            currentEvent.FileSystemObject.EventId = currentEvent.EventId = orderToChange[(int)currentEvent.GroupOrder].Value.Value = groupOrderToId[(int)currentEvent.GroupOrder];
                            eventsByIdForPendingRevision.Add((long)currentEvent.FileSystemObject.EventId, currentEvent);
                            return currentEvent.FileSystemObject;
                        };

                        SqlAccessor<FileSystemObject>.InsertRows(
                            castTransaction.sqlConnection,
                            eventsToAdd.Select(setIdAndGrabObject),
                            transaction: castTransaction.sqlTransaction);

                        foreach (KeyValuePair<FileChange, GenericHolder<long>> currentAddedEvent in orderToChange.Values)
                        {
                            currentAddedEvent.Key.EventId = currentAddedEvent.Value.Value;
                            _trace.writeToMemory(() => _trace.trcFmtStr(2, "IndexingAgent: AddEvents: Call MessageEvents.ApplyFileChangeMergeToChangeState."));
                            MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentAddedEvent.Key, null));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentAddedEvent.Key, null)
                        }
                    }
                }
                while (currentChangeIndex != lastHighestChangeIndex);

                for (int newEventIdx = 0; newEventIdx < newEventsArray.Length; newEventIdx++)
                {
                    FileChange changeWithPendingRevision = newEventsArray[newEventIdx];

                    if (changeWithPendingRevision.FileDownloadPendingRevision != null)
                    {
                        SetPendingRevision(castTransaction, changeWithPendingRevision, eventsByIdForPendingRevision[changeWithPendingRevision.EventId]);
                    }
                }

                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Removes a single event by its id
        /// </summary>
        /// <param name="eventId">Id of event to remove</param>
        /// <returns>Returns an error in removing the event, if any</returns>
        public CLError RemoveEventById(long eventId, SQLTransactionalBase existingTransaction = null)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            return RemoveEventsByIds(Helpers.EnumerateSingleItem(eventId), existingTransaction);
        }

        /// <summary>
        /// Removes a collection of events by their ids
        /// </summary>
        /// <param name="eventIds">Ids of events to remove</param>
        /// <returns>Returns an error in removing events, if any</returns>
        public CLError RemoveEventsByIds(IEnumerable<long> eventIds, SQLTransactionalBase existingTransaction = null)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                if (eventIds == null)
                {
                    throw new NullReferenceException("eventIds cannot be null");
                }

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            CLError toReturn = null;
            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                Func<Exception> notFoundException = () => new KeyNotFoundException("Event not found to delete");

                // Find the existing objects for the given ids
                List<long> toDeleteIds = new List<long>();

                StringBuilder multipleDeleteQuery = null;
                HashSet<long> deleteIdsToFind = null;

                // special enumerator processing so we can iterate the event ids to delete just once, but also be able to know and handle having only one event id
                Nullable<long> storeLastDelete = null;
                using (IEnumerator<long> deleteEnumerator = eventIds.GetEnumerator())
                {
                    bool lastDelete;
                    while (!(lastDelete = !deleteEnumerator.MoveNext()) || storeLastDelete != null)
                    {
                        if (storeLastDelete != null)
                        {
                            if (lastDelete
                                && multipleDeleteQuery == null)
                            {
                                // single delete

                                const string selectFileSystemObjectByEventId = "t3Ee1ulQLjs62aHw5E7nEHC3Yt6pkKQiMjDOMA00p+Qo1PZHGpfRx91FJNSloGZ3xDH11QktFYyaPHTl7mAN/QkLD0PnpC8sDmmRC3eIdnNwEv6VbgcYJMh2e8FkOh6pkTk+wvxmCRAw6xSk4LnkkKwhOy+K1PbDsM0gmAsv7FH9owvFUl6Kqdc6lKyE4dRdTOf9DNG6/aLfVe5cQd3mPbLxSTU0vNHhO+nzhf8FKmwO6jOUt5xMeCGlpmRPDrLEOew5YbL8NJni1eOU3FBfO2gHZ3OzqxaA6ccE89uTk86K/HUk03ajwafV53xHb9+02lsCU29yb3zvcNiHqwMm06RiWp00XVt9ugUTdHMsO+U=";

                                FileSystemObject toDelete;
                                if ((toDelete = SqlAccessor<FileSystemObject>.SelectResultSet(
                                    castTransaction.sqlConnection,
                                    //// before
                                    //
                                    //"SELECT * " +
                                    //    "FROM FileSystemObjects " +
                                    //    "WHERE FileSystemObjects.EventId = ? " + // <-- parameter 1
                                    //    "ORDER BY FileSystemObjects.FileSystemObjectId DESC " +
                                    //    "LIMIT 1"
                                    //
                                    //// after (decrypted)
                                    //
                                    //SELECT *
                                    //FROM FileSystemObjects
                                    //WHERE FileSystemObjects.EventId = ?
                                    //ORDER BY FileSystemObjects.FileSystemObjectId DESC
                                    //LIMIT 1
                                    Helpers.DecryptString(
                                        selectFileSystemObjectByEventId,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword))),
                                    transaction: castTransaction.sqlTransaction,
                                    selectParameters: Helpers.EnumerateSingleItem((long)storeLastDelete)).SingleOrDefault()) == null)
                                {
                                    throw notFoundException();
                                }

                                if (!SqlAccessor<FileSystemObject>.DeleteRow(
                                    castTransaction.sqlConnection,
                                    toDelete,
                                    castTransaction.sqlTransaction))
                                {
                                    throw notFoundException();
                                }
                                else if (!CheckForPendingAtNameAndParent(
                                    castTransaction,
                                    toDelete.ParentFolderId,
                                    toDelete.Name,
                                    toDelete.NameCIHash))
                                {
                                    bool unused;
                                    MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(toDelete.CalculatedFullPath), isDeleted: out unused);
                                }
                            }
                            else
                            {
                                if (multipleDeleteQuery == null)
                                {
                                    // start multiple delete query

                                    deleteIdsToFind = new HashSet<long>();
                                    deleteIdsToFind.Add((long)storeLastDelete);

                                    const string selectFileSystemObjectByEventIdPart1 = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYMauX+tt9OiUbBhMkdLPXm+ywU/y6BY6maDp7nCX4L3mNUxYOj1RyPZa6pCW+HpuP+M9dKeHh9YKX3qDDfOlZ1nSzSsgDKx9X8cmR2Y/nO4tDfrYpmThonsNh65EbGXZBZusSw23vEmfgD1nxUBx9I5vipHRKviGw1wkff0DRRjzA092ky/ySdI+TjFOt9Y9DSIh0FySML0zBwgwrlil5Ns8sFWkTD+eWMwxkHKJvt1fmSbRlRBEsQO3T9dfGXgEV08lbinJ89/i6TXxKNfCjAxKgOazxd2rLS+v5lrxZFwDTbST0vo2uQPvM4XDE60nrAkXje7c8IQKWzEuBL3kBp1jzRBORthX5+3Gg1MRRHrjthDNCWFfnRKwEF5JsvC5XeRemmkZVMqIzRjxUAq6YifLNE92mobolRi0Y8DOdaSQM=";

                                    multipleDeleteQuery = new StringBuilder(
                                        //// before
                                        //
                                        //"SELECT FileSystemObjects.* " +
                                        //    "FROM FileSystemObjects " +
                                        //    "INNER JOIN " +
                                        //    "(" +
                                        //    "SELECT EventId, MAX(FileSystemObjectId) AS MaxFileSystemObjectId " +
                                        //    "FROM FileSystemObjects " +
                                        //    "WHERE EventId IN (?" /*"[event ids]) " +
                                        //    "GROUP BY EventId" +
                                        //    ") InnerFileSystemObjects " +
                                        //    "WHERE InnerFileSystemObjects.EventId = FileSystemObjects.EventId " +
                                        //    "AND InnerFileSystemObjects.MaxFileSystemObjectId = FileSystemObjects.FileSystemObjectId" */
                                        //
                                        //// after (decrypted)
                                        //
                                        //SELECT FileSystemObjects.*
                                        //FROM FileSystemObjects
                                        //INNER JOIN
                                        //(
                                        //SELECT EventId, MAX(FileSystemObjectId) AS MaxFileSystemObjectId
                                        //FROM FileSystemObjects
                                        //WHERE EventId IN (?
                                        Helpers.DecryptString(
                                            selectFileSystemObjectByEventIdPart1,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword))));
                                }
                                else
                                {
                                    if (deleteIdsToFind.Add((long)storeLastDelete))
                                    {
                                        // append current item
                                        multipleDeleteQuery.Append(new string(new[] { ((char)0x2c) /* ',' */, ((char)0x3f) /* '?' */ }));
                                    }
                                }
                            }
                        }

                        storeLastDelete = (lastDelete
                            ? (Nullable<long>)null
                            : deleteEnumerator.Current);
                    }
                }

                if (multipleDeleteQuery != null)
                {
                    const string selectFileSystemObjectByEventIdPart2 = "nH+On2C9vdfAOu+75fb4JkP+K/ZWz4r22CxFGTsLWJmIouas8uXRHJp8fSYn9nVoRF1/cSVAS7KzaMB3dCgq7UZjq58f3QAVF3ZOURIJPcYFpkUqabDCThja2C5olTud3N76JXcxxbFITUsgLisSSbZaWJ+AZq4qxcNKV1imnfHx12TP5caI7hTKcbYxIMkEQ2HMgtFymjdxMO+2XVZ+tIVDQ/PqYFUcnWnltT3PAr8AFlaOIL1ryZsm+o7u9dpHoI6dAfmysr9fKr33jccTZvZrbxs73I/Dher26WyS+lH30U84YfsOqVHlmqJB2wIWLErp/65cRdbrJPFBTYJvJ4ost1tBYliQNdaYHXj2OiH7wYZ1n0Hw9/cKl++my4QAh34Y1OafWnO8MOgaPO6Kg4jbUFGX1lLDy1jInCmd2eKIpVwKtNVybPmNL8dPyLTuyuz7G4pSOTg3YOM+3gxjf5LCuPdYG1bof3z1NIFfj/8uVIXdU7xX/ub5AWSH5vic1V2m+mjto8GZAYPvX0eEmLXhVBWSG1B4Y1PE18y/Lao=";

                    multipleDeleteQuery.Append(
                        //// before
                        //
                        //") " +
                        //"GROUP BY EventId" +
                        //") InnerFileSystemObjects " +
                        //"WHERE InnerFileSystemObjects.EventId = FileSystemObjects.EventId " +
                        //"AND InnerFileSystemObjects.MaxFileSystemObjectId = FileSystemObjects.FileSystemObjectId"
                        //
                        //// after (decrypted)
                        //
                        //)
                        //GROUP BY EventId
                        //) InnerFileSystemObjects
                        //WHERE InnerFileSystemObjects.EventId = FileSystemObjects.EventId
                        //AND InnerFileSystemObjects.MaxFileSystemObjectId = FileSystemObjects.FileSystemObjectId
                        Helpers.DecryptString(
                            selectFileSystemObjectByEventIdPart2,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))));

                    List<FileSystemObject> fileSystemObjectsToDelete = new List<FileSystemObject>(deleteIdsToFind.Count);

                    foreach (FileSystemObject currentMatchedDelete in SqlAccessor<FileSystemObject>.SelectResultSet(
                        castTransaction.sqlConnection,
                        multipleDeleteQuery.ToString(),
                        transaction: castTransaction.sqlTransaction,
                        selectParameters: deleteIdsToFind))
                    {
                        try
                        {
                            if (!deleteIdsToFind.Remove((long)currentMatchedDelete.EventId))
                            {
                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Query of FileSystemObjectIds to delete returned a row with an EventId not in the query list or which was already marked found");
                            }
                            else
                            {
                                fileSystemObjectsToDelete.Add(currentMatchedDelete);
                            }
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    foreach (long deleteIdNotFound in deleteIdsToFind)
                    {
                        try
                        {
                            throw new KeyNotFoundException("Unable to find FileSystemObject with EventId " + deleteIdsToFind.ToString() + " to delete");
                        }
                        catch (Exception ex)
                        {
                            toReturn += ex;
                        }
                    }

                    IEnumerable<int> unableToFindIndexes;
                    SqlAccessor<FileSystemObject>.DeleteRows(
                        castTransaction.sqlConnection,
                        fileSystemObjectsToDelete,
                        out unableToFindIndexes,
                        castTransaction.sqlTransaction);

                    using (IEnumerator<int> notFoundIndexFinder = (unableToFindIndexes ?? Enumerable.Empty<int>()).GetEnumerator())
                    {
                        Func<IEnumerator<int>, Nullable<int>> moveNextAndReturn = innerNotFoundIndexFinder =>
                            {
                                if (innerNotFoundIndexFinder.MoveNext())
                                {
                                    return innerNotFoundIndexFinder.Current;
                                }
                                else
                                {
                                    return null;
                                }
                            };

                        Nullable<int> nextNotFound = moveNextAndReturn(notFoundIndexFinder);

                        for (int currentIndexToDelete = 0; currentIndexToDelete < fileSystemObjectsToDelete.Count; currentIndexToDelete++)
                        {
                            FileSystemObject currentDeletedObject = fileSystemObjectsToDelete[currentIndexToDelete];

                            if (currentIndexToDelete == nextNotFound)
                            {
                                // if it is normal to throw an exception below due to trigger-recursed deletes, then just comment out the exception-throwing below
                                try
                                {
                                    throw new KeyNotFoundException("Unable to find FileSystemObject by Id " + currentDeletedObject.FileSystemObjectId.ToString() + " even after confirming existing record; row possibly deleted by recursive trigger beforehand");
                                }
                                catch (Exception ex)
                                {
                                    toReturn += ex;
                                }

                                nextNotFound = moveNextAndReturn(notFoundIndexFinder);
                            }
                            else if (!CheckForPendingAtNameAndParent(
                                castTransaction,
                                currentDeletedObject.ParentFolderId,
                                currentDeletedObject.Name,
                                currentDeletedObject.NameCIHash))
                            {
                                bool unused;
                                MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(currentDeletedObject.CalculatedFullPath), isDeleted: out unused);
                            }
                        }
                    }
                }

                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Writes a new set of sync states to the database after a sync completes,
        /// requires newRootPath to be set on the first sync or on any sync with a new root path
        /// </summary>
        /// <param name="syncId">New sync Id from server</param>
        /// <param name="syncedEventIds">Enumerable of event ids processed in sync</param>
        /// <param name="syncCounter">Output sync counter local identity</param>
        /// <param name="newRootPath">Optional new root path for location of sync root, must be set on first sync</param>
        /// <returns>Returns an error that occurred during recording the sync, if any</returns>
        public CLError RecordCompletedSync(IEnumerable<PossiblyChangedFileChange> communicatedChanges, string syncId, IEnumerable<long> syncedEventIds, out long syncCounter, string rootFolderUID = null)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    syncCounter = Helpers.DefaultForType<long>();

                    return ex;
                }
            }

            if (copyDatabaseBetweenChanges)
            {
                try
                {
                    lock (dbCopyNumber)
                    {
                        string stack;
                        try
                        {
                            stack = (new System.Diagnostics.StackTrace()).ToString();
                        }
                        catch (Exception ex)
                        {
                            stack = ex.StackTrace;
                        }

                        string dbName = indexDBLocation.Substring(0, indexDBLocation.LastIndexOf((char)0x2e /* '.' */));

                        File.Copy(indexDBLocation,
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBCopiedName,
                                dbName,
                                dbCopyNumber.Value++));

                        File.AppendAllText(
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackName,
                                dbName),
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackFormat,
                                dbCopyNumber.Value,
                                Environment.NewLine,
                                stack,
                                Environment.NewLine,
                                Environment.NewLine));
                    }
                }
                catch
                {
                }
            }

            try
            {
                using (SQLTransactionalImplementation connAndTran = GetNewTransactionPrivate())
                {
                    SqlSync newSync = new SqlSync()
                    {
                        SID = syncId
                    };

                    syncCounter = newSync.SyncCounter = SqlAccessor<SqlSync>.InsertRow<long>(connAndTran.sqlConnection, newSync, transaction: connAndTran.sqlTransaction);

                    if (rootFolderUID != null)
                    {
                        using (ISQLiteCommand updateRootFolderUID = connAndTran.sqlConnection.CreateCommand())
                        {
                            updateRootFolderUID.Transaction = connAndTran.sqlTransaction;

                            const string updateRootObject = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFUab8xxXZlQuV2kLxgXhnElSwWqSmpGztyZC587guim2SDYSZDbZBg1dwEDtWbZSjipo6YN2Po6qT9K9Ixf944jPIFusG+xkt6ftZD6AKexd5J/2SzUaC21E5Ir4+lCENY1+ChpeKG6HR2YgXHfE6YhaxGsMkFuT1x/eLR84vHrzJpNkwN3Lr5nmN4OshbpZjOffLj7/c4nG6HyfO6pJabWFt+1ybdyW6ChRzuDcYwfbZAHa7teAW6u+cEEdRlVQHa/FwREpu5EQTb37sgkJjwF8Uu4OIZvs0AVntpdFFddn2hmrmEzKp+OsG+IV9xOh4";

                            updateRootFolderUID.CommandText =
                                //// before
                                //
                                //"UPDATE FileSystemObjects " +
                                //"SET SyncCounter = ?" + // <-- parameter 1
                                //"WHERE FileSystemObjectId = ?;" + // <-- parameter 2
                                //"UPDATE ServerUids " +
                                //"SET ServerUid = ? " + // <-- parameter 3
                                //"WHERE ServerUidId = ?;" // <-- parameter 4
                                //
                                //// after (decrypted)
                                //
                                //UPDATE FileSystemObjects
                                //SET SyncCounter = ?
                                //WHERE FileSystemObjectId = ?;
                                //UPDATE ServerUids
                                //SET ServerUid = ?
                                //WHERE ServerUidId = ?;
                                Helpers.DecryptString(
                                    updateRootObject,
                                    Encoding.ASCII.GetString(
                                        Convert.FromBase64String(indexDBPassword)));

                            ISQLiteParameter firstSyncCounter = updateRootFolderUID.CreateParameter();
                            firstSyncCounter.Value = syncCounter;
                            updateRootFolderUID.Parameters.Add(firstSyncCounter);

                            ISQLiteParameter rootPK = updateRootFolderUID.CreateParameter();
                            rootPK.Value = rootFileSystemObjectId;
                            updateRootFolderUID.Parameters.Add(rootPK);

                            ISQLiteParameter rootUID = updateRootFolderUID.CreateParameter();
                            rootUID.Value = rootFolderUID;
                            updateRootFolderUID.Parameters.Add(rootUID);

                            ISQLiteParameter rootUIDID = updateRootFolderUID.CreateParameter();
                            rootUIDID.Value = rootFileSystemObjectServerUidId;
                            updateRootFolderUID.Parameters.Add(rootUIDID);

                            updateRootFolderUID.ExecuteNonQuery();
                        }
                    }

                    if (communicatedChanges != null)
                    {
                        List<long> notMarkedAsChanged = new List<long>();

                        CLError mergeChangedError = MergeEventsIntoDatabase(
                            newSync.SyncCounter,
                            communicatedChanges.OrderBy(currentCommunicatedChange => currentCommunicatedChange.ResultOrder)
                                .Where(currentCommunicatedChange =>
                                {
                                    if (currentCommunicatedChange.Changed)
                                    {
                                        currentCommunicatedChange.FileChange.DoNotAddToSQLIndex = false;

                                        string serverUid;
                                        string revision;
                                        CLError queryUidError = QueryServerUid(currentCommunicatedChange.FileChange.Metadata.ServerUidId, out serverUid, out revision, connAndTran);

                                        if (queryUidError != null)
                                        {
                                            throw new AggregateException(string.Format("Unable to query ServerUid with id {0}", currentCommunicatedChange.FileChange.Metadata.ServerUidId), queryUidError.Exceptions);
                                        }

                                        if (serverUid == null)
                                        {
                                            throw new NullReferenceException("communicatedChange with Changed equals true requires FileChange Metadata ServerUid");
                                        }
                                        return true;
                                    }

                                    notMarkedAsChanged.Add(currentCommunicatedChange.FileChange.EventId);
                                    return false;
                                })
                                .Select(currentCommunicatedChange => new FileChangeMerge(currentCommunicatedChange.FileChange))
                                .ToArray(), // ToArray prevents multiple enumeration from running select logic a second time
                            connAndTran);

                        if (mergeChangedError != null)
                        {
                            throw new AggregateException("An error occurred merging a batch of communicated changes before completing a new sync", mergeChangedError.Exceptions);
                        }

                        if (notMarkedAsChanged.Count > 0)
                        {
                            using (ISQLiteCommand updateSyncCounterOnly = connAndTran.sqlConnection.CreateCommand())
                            {
                                updateSyncCounterOnly.Transaction = connAndTran.sqlTransaction;

                                ISQLiteParameter newSyncCounter = updateSyncCounterOnly.CreateParameter();
                                newSyncCounter.Value = newSync.SyncCounter;
                                updateSyncCounterOnly.Parameters.Add(newSyncCounter);

                                StringBuilder updateSyncCounterOnlyText = null;

                                foreach (long currentToUpdate in notMarkedAsChanged)
                                {
                                    if (updateSyncCounterOnlyText == null)
                                    {
                                        const string updateFileSystemObjectSyncCounter = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFUab8xxXZlQuV2kLxgXhnElSwWqSmpGztyZC587guim2SDYSZDbZBg1dwEDtWbZSjSDuOkIW9hZFE9MHpLB9KB/in4st6FXttI2o9R37bycGhQz7ttyUZis5pHcYkctSf+5ch9Xfx3J5cBteRs5tDXUdRc129bYzlkEBzO+kV1yHaF4GHz5D0Tkqjkodr50rt";

                                        updateSyncCounterOnlyText = new StringBuilder(
                                            //// before
                                            //
                                            //"UPDATE FileSystemObjects " +
                                            //"SET SyncCounter = ? " +
                                            //"WHERE SyncCounter IS NULL " +
                                            //"AND EventId IN (?"
                                            //
                                            //// after (decrypted)
                                            //
                                            //UPDATE FileSystemObjects
                                            //SET SyncCounter = ?
                                            //WHERE SyncCounter IS NULL
                                            //AND EventId IN (?
                                            Helpers.DecryptString(
                                                updateFileSystemObjectSyncCounter,
                                                Encoding.ASCII.GetString(
                                                    Convert.FromBase64String(indexDBPassword))));
                                    }
                                    else
                                    {
                                        updateSyncCounterOnlyText.Append(new string(new[] { ((char)0x2c) /* ',' */, ((char)0x3f) /* '?' */ }));
                                    }

                                    ISQLiteParameter currentEventId = updateSyncCounterOnly.CreateParameter();
                                    currentEventId.Value = currentToUpdate;
                                    updateSyncCounterOnly.Parameters.Add(currentEventId);
                                }

                                updateSyncCounterOnlyText.Append(((char)0x29).ToString());

                                updateSyncCounterOnly.CommandText = updateSyncCounterOnlyText.ToString();

                                updateSyncCounterOnly.ExecuteNonQuery();
                            }
                        }
                    }

                    foreach (long synchronouslyCompletedEventId in syncedEventIds ?? Enumerable.Empty<long>())
                    {
                        CLError markCompletionError = MarkEventAsCompletedOnPreviousSync(synchronouslyCompletedEventId, connAndTran);
                        if (markCompletionError != null)
                        {
                            throw new AggregateException("Error marking Event at synchronouslyCompletedEventId completed on RecordCompleted", markCompletionError.Exceptions);
                        }
                    }

                    LastSyncId = syncId;

                    connAndTran.Commit();
                }
            }
            catch (Exception ex)
            {
                syncCounter = Helpers.DefaultForType<long>();

                return ex;
            }
            return null;
        }

        /// <summary>
        /// ¡¡ Call this carefully, completely wipes index database (use when user deletes local repository or relinks) !!
        /// </summary>
        /// <returns></returns>
        public CLError WipeIndex(string newRootPath)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            try
            {
                InitializeDatabase(newRootPath, createEvenIfExisting: true);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Creates a new transactional object which can be passed back into database access calls and externalizes the ability to dispose or commit the transaction
        /// </summary>
        public SQLTransactionalBase GetNewTransaction()
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    // &&&& todo: since this cannot return the disposed exception, make sure the caller checks for null to throws an appropriate dispose exception
                    return null;
                }
            }

            if (copyDatabaseBetweenChanges)
            {
                try
                {
                    lock (dbCopyNumber)
                    {
                        string stack;
                        try
                        {
                            stack = (new System.Diagnostics.StackTrace()).ToString();
                        }
                        catch (Exception ex)
                        {
                            stack = ex.StackTrace;
                        }

                        string dbName = indexDBLocation.Substring(0, indexDBLocation.LastIndexOf((char)0x2e /* '.' */));

                        File.Copy(indexDBLocation,
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBCopiedName,
                                dbName,
                                dbCopyNumber.Value++));

                        File.AppendAllText(
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackName,
                                dbName),
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackFormat,
                                dbCopyNumber.Value,
                                Environment.NewLine,
                                stack,
                                Environment.NewLine,
                                Environment.NewLine));
                    }
                }
                catch
                {
                }
            }

            return GetNewTransactionPrivate();
        }

        private SQLTransactionalImplementation GetNewTransactionPrivate()
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    // &&&& todo: since this cannot return the disposed exception, make sure the caller checks for null to throws an appropriate dispose exception
                    return null;
                }
            }

            ISQLiteConnection indexDB;
            return new SQLTransactionalImplementation(
                indexDB = CreateAndOpenCipherConnection(),
                indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
        }

        /// <summary>
        /// Method to merge event into database,
        /// used when events are modified or replaced with new events
        /// </summary>
        /// <returns>Returns an error from merging the events, if any</returns>
        public CLError MergeEventsIntoDatabase(IEnumerable<FileChangeMerge> mergeToFroms, SQLTransactionalBase existingTransaction = null, bool addCreateAtOldPathIfNotFound = false)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            if (existingTransaction == null
                && copyDatabaseBetweenChanges)
            {
                try
                {
                    lock (dbCopyNumber)
                    {
                        string stack;
                        try
                        {
                            stack = (new System.Diagnostics.StackTrace()).ToString();
                        }
                        catch (Exception ex)
                        {
                            stack = ex.StackTrace;
                        }

                        string dbName = indexDBLocation.Substring(0, indexDBLocation.LastIndexOf((char)0x2e /* '.' */));

                        File.Copy(indexDBLocation,
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBCopiedName,
                                dbName,
                                dbCopyNumber.Value++));

                        File.AppendAllText(
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackName,
                                dbName),
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackFormat,
                                dbCopyNumber.Value,
                                Environment.NewLine,
                                stack,
                                Environment.NewLine,
                                Environment.NewLine));
                    }
                }
                catch
                {
                }
            }

            return MergeEventsIntoDatabase(null, mergeToFroms, existingTransaction, addCreateAtOldPathIfNotFound);
        }
        private CLError MergeEventsIntoDatabase(Nullable<long> syncCounter, IEnumerable<FileChangeMerge> mergeToFroms, SQLTransactionalBase existingTransaction, bool addCreateAtOldPathIfNotFound = false)
        {
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }

                if (existingTransaction != null
                    && castTransaction == null)
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }

                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            // no point trying to perform multiple simultaneous merges since they will block each other via the SQLite transaction
            //
            // actually, there is a point in blocking with a local lock: if we decide two identical FileChanges need to be added to sql before the first contention happens,
            // then it will try adding twice instead of one add and one update
            lock (MergeEventsLocker)
            {
                CLError toReturn = null;
                try
                {
                    if (mergeToFroms != null)
                    {
                        HashSet<long> updatedIds = new HashSet<long>();

                        List<FileChange> toAddList = new List<FileChange>();
                        List<long> toDeleteList = new List<long>();

                        // special enumerator processing so we can know when we're processing the last item since we cannot simply queue its item for batch accumulation
                        Nullable<FileChangeMerge> storeLastMerge = null;
                        using (IEnumerator<FileChangeMerge> mergeEnumerator = mergeToFroms.GetEnumerator())
                        {
                            bool finalMergeEvent;
                            while (!(finalMergeEvent = !mergeEnumerator.MoveNext()) || storeLastMerge != null)
                            {
                                try
                                {
                                    try
                                    {
                                        FileChange toAdd;
                                        long toDelete;
                                        FileChange toUpdate;

                                        if (storeLastMerge != null)
                                        {
                                            FileChangeMerge currentMerge = (FileChangeMerge)storeLastMerge;

                                            try
                                            {
                                                // Continue to next iteration if boolean set indicating not to add to SQL
                                                if (currentMerge.MergeTo != null
                                                    && currentMerge.MergeTo.DoNotAddToSQLIndex
                                                    && currentMerge.MergeTo.EventId != 0)
                                                {
                                                    MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentMerge.MergeTo, currentMerge.MergeFrom));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom)

                                                    // normally we assign the next event to process at the end of the looping section, but since we short circuit it with continue, need to assign next event now
                                                    storeLastMerge = (finalMergeEvent
                                                        ? (Nullable<FileChangeMerge>)null
                                                        : mergeEnumerator.Current);

                                                    continue;
                                                }

                                                // Ensure input variables have proper references set
                                                if (currentMerge.MergeTo == null)
                                                {
                                                    // null merge events are only valid if there is an oldEvent to remove
                                                    if (currentMerge.MergeFrom == null)
                                                    {
                                                        throw new NullReferenceException("currentMerge.MergeTo cannot be null");
                                                    }
                                                }
                                                else if (currentMerge.MergeTo.Metadata == null)
                                                {
                                                    throw new NullReferenceException("currentMerge.MergeTo cannot have null Metadata");
                                                }
                                                else if (currentMerge.MergeTo.NewPath == null)
                                                {
                                                    throw new NullReferenceException("currentMerge.MergeTo cannot have null NewPath");
                                                }

                                                ////possibilities for old event:
                                                ////none,
                                                ////not in database, <-- causes old to be ignored (acts like none)
                                                ////exists in database
                                                //
                                                //
                                                ////possibilities for new event:
                                                ////none,
                                                ////not in database, (new event)
                                                ////exists in database
                                                //
                                                //
                                                ////mutually exclusive:
                                                ////none and none
                                                //
                                                //
                                                ////if there is an old exists and a new none, then delete old row
                                                //
                                                ////if old does not exists and a new none, do nothing (already not in database)
                                                //
                                                ////if old none
                                                ////    if new not in database, add new to database
                                                ////    else if new in database, update new
                                                //
                                                ////if there is an old exists and new not in database, update old row with new data
                                                //
                                                ////if there is an old exists and new in database and neither match, delete new row and update old row with new data
                                                //
                                                ////if there is an old exists and new in database and they do match by row primary key (EventId), update new in database
                                                //
                                                ////(ignore old:)
                                                ////if old does not exist and new new not in database, add new to database
                                                //
                                                ////(ignore old:)
                                                ////if old does not exist and new exists in database, update new in database


                                                // byte definitions:
                                                // 0 = null
                                                // 1 = not in database (EventId == 0)
                                                // 2 = exists in database (EventId > 0)

                                                byte oldEventState = (currentMerge.MergeFrom == null
                                                    ? (byte)0
                                                    : (currentMerge.MergeFrom.EventId > 0
                                                        ? (byte)2
                                                        : (byte)1));

                                                byte newEventState = (currentMerge.MergeTo == null
                                                    ? (byte)0
                                                    : (currentMerge.MergeTo.EventId > 0
                                                        ? (byte)2
                                                        : (byte)1));

                                                switch (oldEventState)
                                                {
                                                    // old event is null or not null but does not already exist in database
                                                    case (byte)0:
                                                    case (byte)1: // <-- not in database treated like null for old event
                                                        switch (newEventState)
                                                        {
                                                            // 0 for new event is only possible if old event was 1 (null and null are mutually excluded via exceptions above)
                                                            case (byte)0:
                                                                // already not in database, do nothing
                                                                toAdd = null;
                                                                toUpdate = null;
                                                                toDelete = 0;
                                                                break;

                                                            case (byte)1:
                                                                // nothing to delete for the old row since it never existed in database;
                                                                // new row doesn't exist in database so it will be added
                                                                toAdd = currentMerge.MergeTo;
                                                                toUpdate = null;
                                                                toDelete = 0;
                                                                break;

                                                            default: //case (byte)2:
                                                                // nothing to delete for old row since it never existeed in database;
                                                                // new row exists in database so update it
                                                                toAdd = null;
                                                                toUpdate = currentMerge.MergeTo;
                                                                toDelete = 0;
                                                                break;
                                                        }
                                                        break;

                                                    // old event already exists in database
                                                    default: //case (byte)2:
                                                        switch (newEventState)
                                                        {
                                                            case (byte)0:
                                                                // old row exists in database but merging it into nothingness, simply delete old row
                                                                toAdd = null;
                                                                toUpdate = null;
                                                                toDelete = currentMerge.MergeFrom.EventId;
                                                                break;

                                                            case (byte)1:
                                                                // old row exists in database and needs to be updated with latest metadata which is not in an existing new row
                                                                currentMerge.MergeTo.EventId = currentMerge.MergeFrom.EventId; // replace merge to event id with the one from the sync from

                                                                toAdd = null;
                                                                toUpdate = currentMerge.MergeTo;
                                                                toDelete = 0;
                                                                break;

                                                            default: //case (byte)2:
                                                                // old row exists in database and a new row exists

                                                                // if the rows match, then update the new row only
                                                                if (currentMerge.MergeFrom.EventId == currentMerge.MergeTo.EventId)
                                                                {
                                                                    toAdd = null;
                                                                    toUpdate = currentMerge.MergeTo;
                                                                    toDelete = 0;
                                                                }
                                                                // else if the rows do not match, then delete the new row, and put the new metadata in the old row (prefers keeping lowest EventId in database for dependency hierarchy reasons)
                                                                else
                                                                {
                                                                    // set toDelete first since the event Id at the reference we are grabbing is going to be changed in between setting toDelete and toUpdate

                                                                    toDelete = currentMerge.MergeTo.EventId;

                                                                    currentMerge.MergeTo.EventId = currentMerge.MergeFrom.EventId; // replace merge to event id with the one from the sync from

                                                                    toAdd = null;
                                                                    toUpdate = currentMerge.MergeTo;
                                                                }
                                                                break;
                                                        }
                                                        break;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                toDelete = 0;
                                                toUpdate = null;
                                                toAdd = null;

                                                toReturn += ex;
                                            }
                                        }
                                        else
                                        {
                                            toDelete = 0;
                                            toUpdate = null;
                                            toAdd = null;
                                        }

                                        // determine if a previous batch has finished, if there will be no more events (process any existing batch as final), or if there is an update to process immediately,
                                        // and create an action priority to perform operations by the original event order

                                        // changeType byte enum:
                                        // 0 = deletion action
                                        // 1 = addition action
                                        // 2 = update action

                                        List<byte> actionOrder = new List<byte>();

                                        if (toDeleteList.Count > 0

                                            // if the current event cannot be appended to the delete list, then the delete list must process first
                                            && (toUpdate != null
                                                || toAdd != null))
                                        {
                                            actionOrder.Add((byte)0);
                                        }

                                        if (toAddList.Count > 0

                                            // if the current event cannot be appended to the add list, then the add list must process first
                                            && (toDelete > 0
                                                || toUpdate != null))
                                        {
                                            actionOrder.Add((byte)1);
                                        }

                                        // process the current event; deletes and adds will be added to a batch to process, but update is processed by itself
                                        if (toDelete > 0)
                                        {
                                            // if last event, process what's in the delete batch now
                                            if (finalMergeEvent

                                                // also condition on whether delete was already added to actionOrder to not add it twice
                                                && (toDeleteList.Count == 0
                                                    || (toUpdate == null
                                                        && toAdd == null)))
                                            {
                                                actionOrder.Add((byte)0);
                                            }

                                            toDeleteList.Add(toDelete);

                                            // possible to have both a delete and an update if the rows are being merged
                                            if (toUpdate != null)
                                            {
                                                actionOrder.Add((byte)2);
                                            }
                                        }
                                        else if (toAdd != null)
                                        {
                                            toAddList.Add(toAdd);

                                            // if last event, process what's in the add batch now
                                            if (finalMergeEvent)
                                            {
                                                actionOrder.Add((byte)1);
                                            }
                                        }
                                        else if (toUpdate != null)
                                        {
                                            // always process every update one at a time
                                            actionOrder.Add((byte)2);
                                        }

                                        bool restartForAddFound = true; // set to true to run at least once, if set to true again then it was reset
                                        while (restartForAddFound)
                                        {
                                            restartForAddFound = false;

                                            foreach (byte currentAction in actionOrder.ToArray())
                                            {
                                                actionOrder.RemoveAt(0);

                                                switch (currentAction)
                                                {
                                                    // action is delete
                                                    case (byte)0:
                                                        CLError removeBatchError = RemoveEventsByIds(toDeleteList, castTransaction);

                                                        if (removeBatchError != null)
                                                        {
                                                            toReturn += new AggregateException("One or more errors occurred removing a batch of events by ids", removeBatchError.Exceptions);
                                                        }

                                                        // no point wasting effort to clear the list for future batches if there will be no future batches
                                                        if (!finalMergeEvent)
                                                        {
                                                            toDeleteList.Clear();
                                                        }
                                                        break;

                                                    // action is add
                                                    case (byte)1:
                                                        CLError addBatchError = AddEvents(syncCounter, toAddList, castTransaction, addCreateAtOldPathIfNotFound);

                                                        if (addBatchError != null)
                                                        {
                                                            toReturn += new AggregateException("One or more errors occurred adding a batch of new events");
                                                        }

                                                        // no point wasting effort to clear the list for future batches if there will be no future batches
                                                        if (!finalMergeEvent)
                                                        {
                                                            toAddList.Clear();
                                                        }
                                                        break;

                                                    // action is update
                                                    default: //case (byte)2:
                                                        const string selectFileSystemObjectByEventId = "XiF/n8DAmECRcpl1q3g5SOaFkrEO/c+iI1V66stCO9bB3hEK7nYGLuijwAsZ69MKnic6eyxsf+Dhl/eivVXxxImu7p3ZeiPkoWpKpw7GfpirlnO5pPnFAKd5rOvejfiQ+f+F7lY+YU/V1KMowxqZEGu1AajCX2H/4GUc3jQeSjD3dxwtLwJDcnn2QDUbUiXtDme/ofrrLG/Il/GuRa0InF7G4uAn1SZygmbHHk/QnpbQdC7DG7E8m07IMa7nVI/jp4bYUAI408ROMsmsPXhhgaKcimmirqjisCP2Uj3c1TLvIRXcCr1WUzg0TBWevK3b3+f3P67z6YZ6VJshTZy4S9vRxgSF/w2vt0MAb27FNlJ2yQqDnhUFp4JfhaeaRoKZGv0UsxXGWiLxNDBEDMEZ/4p3EvqyUO/TynHwFDNDg2VaJuiLJYYepCgRopHaMhl/FVSc+zb+btspNIiPGQaUHEFSKv65ThADVy3giKPeQh/VWKVVci1Flo/ren1+rNiChwJ/WTem5lynbdORDxTZ8zGt9zo8FE81xrS/7gW7BWw4HP7zBKrFvcOySQkahJ5KFwOD5YUcoKpY1LRki4EUFBXbOF5cLzQHAsiBM0drd9sl46BVwBig8Kh+yf6uy2dRclmsxBRvvXMvAYJLcwFsu1ehgqBVkey0pu4aTutu9vZC65007GiIY/zVvkTLJsOS7unp4lVb/X4LrXoAY2x2EnBZMJ2rZCHOZcgGkD8tBR6ooTx2uKe8S4ZsAtx+W1KSe2tNLe0+Ix2xyfqTrAJN+G1zWbuh4rf0qRVvfC9F0Dg=";

                                                        FileSystemObject existingRow = SqlAccessor<FileSystemObject>.SelectResultSet(
                                                                castTransaction.sqlConnection,
                                                                //// before
                                                                //
                                                                //"SELECT " +
                                                                //    SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                                                                //    SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent) + ", " +
                                                                //    SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEventPrevious, Resources.NotTranslatedSqlIndexerPreviouses) +
                                                                //    " FROM FileSystemObjects" +
                                                                //    " INNER JOIN Events ON FileSystemObjects.EventId = Events.EventId" +
                                                                //    " LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = Previouses.FileSystemObjectId" +
                                                                //    " WHERE Events.EventId = ?" + // <-- parameter 1
                                                                //    " AND FileSystemObjects.ParentFolderId IS NOT NULL" +
                                                                //    " LIMIT 1"
                                                                //
                                                                //// after (decrypted; {0}: SqlAccessor<FileSystemObject>.GetSelectColumns()
                                                                //// {1}: SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent)
                                                                //// {2}: SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEventPrevious, Resources.NotTranslatedSqlIndexerPreviouses) )
                                                                //
                                                                //SELECT
                                                                //{0},
                                                                //{1},
                                                                //{2}
                                                                //FROM FileSystemObjects
                                                                //INNER JOIN Events ON FileSystemObjects.EventId = Events.EventId
                                                                //LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = Previouses.FileSystemObjectId
                                                                //WHERE Events.EventId = ?
                                                                //AND FileSystemObjects.ParentFolderId IS NOT NULL
                                                                //LIMIT 1
                                                                string.Format(
                                                                    Helpers.DecryptString(
                                                                        selectFileSystemObjectByEventId,
                                                                        Encoding.ASCII.GetString(
                                                                            Convert.FromBase64String(indexDBPassword))),
                                                                    SqlAccessor<FileSystemObject>.GetSelectColumns(),
                                                                    SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent),
                                                                    SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEventPrevious, Resources.NotTranslatedSqlIndexerPreviouses)),
                                                                new[]
                                                                {
                                                                    Resources.NotTranslatedSqlIndexerEvent,
                                                                    Resources.NotTranslatedSqlIndexerEventPrevious
                                                                },
                                                                castTransaction.sqlTransaction,
                                                                Helpers.EnumerateSingleItem((long)toUpdate.EventId))
                                                            .SingleOrDefault();

                                                        if (existingRow == null)
                                                        {
                                                            // couldn't find existing row to update, add a new one instead (will overwrite the EventId)

                                                            toAddList.Add(toUpdate);

                                                            if (!actionOrder.Contains((byte)1)
                                                                && (finalMergeEvent
                                                                    || toDeleteList.Count > 0))
                                                            {
                                                                actionOrder.Add((byte)1);
                                                            }

                                                            restartForAddFound = true;
                                                        }
                                                        else
                                                        {
                                                            if (existingRow.ParentFolderId == null)
                                                            {
                                                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing FileSystemObject to update did not have a parent folder");
                                                            }

                                                            Nullable<long> toUpdateParentFolderId;
                                                            Nullable<long> toUpdatePreviousId;

                                                            FilePath previousRowPath = existingRow.CalculatedFullPath;
                                                            if (previousRowPath != null
                                                                && FilePathComparer.Instance.Equals(previousRowPath.Parent, toUpdate.NewPath.Parent))
                                                            {
                                                                toUpdateParentFolderId = existingRow.ParentFolderId;
                                                            }
                                                            // prefer latest event even if pending
                                                            else
                                                            {
                                                                using (ISQLiteCommand findParentCommand = castTransaction.sqlConnection.CreateCommand())
                                                                {
                                                                    findParentCommand.Transaction = castTransaction.sqlTransaction;

                                                                    const string selectFileSystemObjectIdByFullPathHash = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYM4A9CJRjJ2AJWbK+2XeEfjsHjbyLTBN0A1N3nA0Q4qVNOEMU6uWFsvHIyt+vO7mRotjaQ6thfD0JG4dEBb6CwJ89vQ95+fzquzuLa1c96VgT54coYBhS2C3tyaw2edsBVJbPHBY9AoOd9jpwEk6+Ny/P2L3tYZ6e5uNxcENQzO/pwOGbYha42a1U+38dVMSoqKkQlnmRH0i19cmrh06Kfa3+qwDJXThCp9i/TtZJ4Q3xj5rDfd/s8qMLK/NohgujicfhNBKw+NDq0Sau8MibFLqvhBztuVNjnrYnJlUSqirB7BE3qrTU1pWGlCZMAG+0lBXq8i57q+s5vZlFTFklVrgQ2c4v8NihfLFVBEFrLJk/z0MTC41p3UA923AW6bP1E/VYhemc22xs1HhG2ghx7K1ZlktcUxmUdJEcEltq9yvrj6Zu2UpjnYf06y/SyexLSdU6e1Gyr2nU0imuWhH7JiLQyNOsYUAw3fhu1yrLDKpZrKqBHzdYYmalqEMJb4hjXDmSh7bxCrAMaJ39Q3D0MIGFew4cQO9v08IsEO8eXN33ncIlWg5Ie+JMMtR8GdAXoEX14BvqmJm0C24iA6ZNN9Os0epQQKqikT2w/zvDm3TE=";

                                                                    findParentCommand.CommandText =
                                                                        //// before
                                                                        //
                                                                        //"SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.CalculatedFullPath " +
                                                                        //"FROM FileSystemObjects " +
                                                                        //"WHERE CalculatedFullPathCIHashes = ? " + // <-- parameter 1
                                                                        //"ORDER BY " +
                                                                        //"CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                                                        //"THEN 0 " +
                                                                        //"ELSE FileSystemObjects.EventOrder " +
                                                                        //"END DESC"
                                                                        //
                                                                        //// after (decrypted)
                                                                        //
                                                                        //SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.CalculatedFullPath
                                                                        //FROM FileSystemObjects
                                                                        //WHERE CalculatedFullPathCIHashes = ?
                                                                        //ORDER BY
                                                                        //CASE WHEN FileSystemObjects.EventOrder IS NULL
                                                                        //THEN 0
                                                                        //ELSE FileSystemObjects.EventOrder
                                                                        //END DESC
                                                                        Helpers.DecryptString(
                                                                            selectFileSystemObjectIdByFullPathHash,
                                                                            Encoding.ASCII.GetString(
                                                                                Convert.FromBase64String(indexDBPassword)));

                                                                    FilePath currentParentPath = toUpdate.NewPath.Parent;

                                                                    List<string> namePortions = new List<string>();

                                                                    while (!FilePathComparer.Instance.Equals(currentParentPath, indexedPath))
                                                                    {
                                                                        namePortions.Add(currentParentPath.Name);

                                                                        currentParentPath = currentParentPath.Parent;
                                                                    }

                                                                    namePortions.Add(indexedPath);

                                                                    namePortions.Reverse();

                                                                    string pathCIHashes = string.Join(((char)0x5c /* \ */).ToString(),
                                                                        namePortions.Select(currentPortion => StringComparer.OrdinalIgnoreCase.GetHashCode(currentPortion).ToString()));

                                                                    ISQLiteParameter previousHashesParam = findParentCommand.CreateParameter();
                                                                    previousHashesParam.Value = pathCIHashes;
                                                                    findParentCommand.Parameters.Add(previousHashesParam);

                                                                    using (ISQLiteDataReader previousObjectReader = findParentCommand.ExecuteReader(CommandBehavior.SingleResult))
                                                                    {
                                                                        toUpdateParentFolderId = null;

                                                                        while (previousObjectReader.Read())
                                                                        {
                                                                            if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(previousObjectReader[Resources.NotTranslatedSqlIndexerCalculatedFullPath]), toUpdate.NewPath.Parent.ToString()))
                                                                            {
                                                                                toUpdateParentFolderId = Convert.ToInt64(previousObjectReader[Resources.NotTranslatedSqlIndexerFileSystemObjectId]);

                                                                                break;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            if (toUpdateParentFolderId == null)
                                                            {
                                                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find FileSystemObject with path of parent folder to use as containing folder");
                                                            }

                                                            if (toUpdate.OldPath == null)
                                                            {
                                                                toUpdatePreviousId = null;
                                                            }
                                                            else if (existingRow.Event.Previous == null
                                                                || !FilePathComparer.Instance.Equals(existingRow.Event.Previous.CalculatedFullPath, toUpdate.OldPath))
                                                            {
                                                                // prefers the latest rename which is pending,
                                                                // otherwise prefers non-pending,
                                                                // last take most recent event

                                                                using (ISQLiteCommand findPreviousCommand = castTransaction.sqlConnection.CreateCommand())
                                                                {
                                                                    findPreviousCommand.Transaction = castTransaction.sqlTransaction;

                                                                    const string selectFileSystemObjectIdByOldFullPathHash = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYM4A9CJRjJ2AJWbK+2XeEfjsHjbyLTBN0A1N3nA0Q4qVNOEMU6uWFsvHIyt+vO7mRotjaQ6thfD0JG4dEBb6CwJ89vQ95+fzquzuLa1c96VgT54coYBhS2C3tyaw2edsBVJbPHBY9AoOd9jpwEk6+Ny/P2L3tYZ6e5uNxcENQzO/pwOGbYha42a1U+38dVMSoqKkQlnmRH0i19cmrh06Kfa+HDXNoDtQGeRTdcqYxQ24UwH4iTCkbvmopF3okXukWm6IiVorCtfxanLcCgfSmsTYVyRksXaRRsiGGlY3B2CMrxHZ8fRTMEdXK+uxPUo09L+rVnigXHTZ59cdYTr7/Mtv63p8+MDJ0W5PQU6SxYTNMhpbbbxMXsgBAVOOY13PbeH+xv1sOrTlFHg2WNKC/LTASc/P6eagesR9E4M3YHu6G9pX8jX0vht0wwmhHJMKyoUWgZ/X0smf/FBqPjecsz2RG48AXFJssSbtOe4L8HaWXJZYGVGcJzRFaqa1URGRw6w60pQhzRiAGx7uPJzJhpBfmzkeFX0Acehvo84RdpN/I2hyYGYkyM/1hyP8DaH33oGKX/4h3ljgOoSWHg+wSiztzJkdUfD5txj9u36njaf194EP1/BBT0EI38d2caDWqLOioEZrRu1OeJ72tikM5770H28wY8m+TQdXHC39MBEKtfZC97KqF9kgPxU2Yq3GuzfJ57DH875K/31mKJg3o23MhwWbdtaCSB0rL6aExXg5NoqvTzskiby0MkagZNrn6jROJ4jWiSsAfyP2INKwPoEJB7RWpTaR/2x8y1mCjP6Ks5OKNC4tV7xDz3byNP1DrSUBKKY50WyZtQfgjDwTqOYm2H/NN+BIxLhIM8mWWy0SLiTWQSq71UedyEtNxAxnTbznMjX4/uL7s9ftatza7sn8lwuc6Hdw0tIIV/WpXQUA7JB+KB2msX6DoafQbMe60ySKHVTijiNB86EtgKWXcJTbgtWNTnk/9rwBJPRV7IDbCQg+JWjo/qSipmOONAcA96Xp9DTePG2ih2UglnxoHnsubeKvhfdW1rM9GTZvsPYvQbxQEWtdTv0E5el0ryg1BjYI9K1QfTiKChVK9xYb/NdzQTbQ9j8zViX1QmUy1aVJVZGs49HFDKGYdzcORuugiTFBG2e3Arcn5NO4/YlWK7PHsgUe8TNpuyx8dyvNbzKanOt+mYKpBEUAU4Mw2pf3kpTPKTncuqsf1vMLZ4t7dFV9GfjLCWFD5uX+8M19lBiWwiCbvw6bsHA0c4klrWBY5HNr4H91hqIPiHhkU4dOaecMzbUGhttMzdCPYYDOBHKcag5+nyz4ou5lsn0v8UtLTl";

                                                                    findPreviousCommand.CommandText =
                                                                        //// before
                                                                        //
                                                                        //"SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.CalculatedFullPath " +
                                                                        //"FROM FileSystemObjects " +
                                                                        //"LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                                                                        //"WHERE FileSystemObjects.CalculatedFullPathCIHashes = ? " + // <-- parameter 1
                                                                        //"ORDER BY " +
                                                                        //"CASE WHEN FileSystemObjects.EventId IS NOT NULL " +
                                                                        //"AND Events.FileChangeTypeEnumId = " + changeEnumsBackward[FileChangeType.Renamed] +
                                                                        //" AND FileSystemObjects.Pending = 1 " +
                                                                        //"THEN 0 " +
                                                                        //"ELSE 1 " +
                                                                        //"END ASC, " +
                                                                        //"FileSystemObjects.Pending ASC, " +
                                                                        //"CASE WHEN FileSystemObjects.EventOrder IS NULL " +
                                                                        //"THEN 0 " +
                                                                        //"ELSE FileSystemObjects.EventOrder " +
                                                                        //"END DESC"
                                                                        //
                                                                        //// after (decrypted; {0}: changeEnumsBackward[FileChangeType.Renamed])
                                                                        //
                                                                        //SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.CalculatedFullPath
                                                                        //FROM FileSystemObjects
                                                                        //LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId
                                                                        //WHERE FileSystemObjects.CalculatedFullPathCIHashes = ?
                                                                        //ORDER BY
                                                                        //CASE WHEN FileSystemObjects.EventId IS NOT NULL
                                                                        //AND Events.FileChangeTypeEnumId = {0}
                                                                        //AND FileSystemObjects.Pending = 1
                                                                        //THEN 0
                                                                        //ELSE 1
                                                                        //END ASC,
                                                                        //FileSystemObjects.Pending ASC,
                                                                        //CASE WHEN FileSystemObjects.EventOrder IS NULL
                                                                        //THEN 0
                                                                        //ELSE FileSystemObjects.EventOrder
                                                                        //END DESC
                                                                        string.Format(
                                                                            Helpers.DecryptString(
                                                                                selectFileSystemObjectIdByOldFullPathHash,
                                                                                Encoding.ASCII.GetString(
                                                                                    Convert.FromBase64String(indexDBPassword))),
                                                                            changeEnumsBackward[FileChangeType.Renamed]);

                                                                    FilePath currentOldPath = toUpdate.OldPath;

                                                                    List<string> namePortions = new List<string>();

                                                                    while (!FilePathComparer.Instance.Equals(currentOldPath, indexedPath))
                                                                    {
                                                                        namePortions.Add(currentOldPath.Name);

                                                                        currentOldPath = currentOldPath.Parent;
                                                                    }

                                                                    namePortions.Add(indexedPath);

                                                                    namePortions.Reverse();

                                                                    string pathCIHashes = string.Join(((char)0x5c /* \ */).ToString(),
                                                                        namePortions.Select(currentPortion => StringComparer.OrdinalIgnoreCase.GetHashCode(currentPortion).ToString()));

                                                                    ISQLiteParameter previousHashesParam = findPreviousCommand.CreateParameter();
                                                                    previousHashesParam.Value = pathCIHashes;
                                                                    findPreviousCommand.Parameters.Add(previousHashesParam);

                                                                    using (ISQLiteDataReader previousObjectReader = findPreviousCommand.ExecuteReader(CommandBehavior.SingleResult))
                                                                    {
                                                                        toUpdatePreviousId = null;

                                                                        while (previousObjectReader.Read())
                                                                        {
                                                                            if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(previousObjectReader[Resources.NotTranslatedSqlIndexerCalculatedFullPath]), toUpdate.OldPath.ToString()))
                                                                            {
                                                                                toUpdatePreviousId = Convert.ToInt64(previousObjectReader[Resources.NotTranslatedSqlIndexerFileSystemObjectId]);

                                                                                break;
                                                                            }
                                                                        }

                                                                        if (toUpdatePreviousId == null)
                                                                        {
                                                                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find FileSystemObject with old path of toUpdate before rename\\move operation");
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                toUpdatePreviousId = existingRow.Event.PreviousId;
                                                            }

                                                            #region update fields in FileSystemObject

                                                            // only associate an event to a sync counter once, later events should get new objects with a new SyncCounter anyways
                                                            if (existingRow.SyncCounter == null)
                                                            {
                                                                existingRow.SyncCounter = syncCounter;
                                                            }

                                                            if (toUpdate.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks)
                                                            {
                                                                existingRow.CreationTimeUTCTicks = null;
                                                            }
                                                            else
                                                            {
                                                                DateTime creationTimeUTC = toUpdate.Metadata.HashableProperties.CreationTime.ToUniversalTime();

                                                                existingRow.CreationTimeUTCTicks = (creationTimeUTC.Ticks == FileConstants.InvalidUtcTimeTicks
                                                                    ? (Nullable<long>)null
                                                                    : creationTimeUTC.Ticks);
                                                            }
                                                            existingRow.EventTimeUTCTicks = DateTime.UtcNow.Ticks;
                                                            existingRow.IsFolder = toUpdate.Metadata.HashableProperties.IsFolder;
                                                            existingRow.IsShare = toUpdate.Metadata.IsShare;
                                                            if (toUpdate.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks)
                                                            {
                                                                existingRow.LastTimeUTCTicks = null;
                                                            }
                                                            else
                                                            {
                                                                DateTime lastTimeUTC = toUpdate.Metadata.HashableProperties.LastTime.ToUniversalTime();

                                                                existingRow.LastTimeUTCTicks = (lastTimeUTC.Ticks == FileConstants.InvalidUtcTimeTicks
                                                                    ? (Nullable<long>)null
                                                                    : lastTimeUTC.Ticks);
                                                            }
                                                            byte[] getMD5 = toUpdate.MD5;
                                                            existingRow.MD5 = getMD5;
                                                            existingRow.MimeType = toUpdate.Metadata.MimeType;
                                                            existingRow.Name = toUpdate.NewPath.Name;
                                                            existingRow.NameCIHash = StringComparer.OrdinalIgnoreCase.GetHashCode(toUpdate.NewPath.Name);
                                                            existingRow.ParentFolderId = toUpdateParentFolderId;
                                                            //existingRow.Pending = true; // <-- true on insert, no need to update here
                                                            existingRow.Permissions = (toUpdate.Metadata.Permissions == null
                                                                ? (Nullable<int>)null
                                                                : (int)((POSIXPermissions)toUpdate.Metadata.Permissions));
                                                            existingRow.ServerUidId = toUpdate.Metadata.ServerUidId;
                                                            //existingRow.ServerName // <-- add support for server name
                                                            existingRow.Size = toUpdate.Metadata.HashableProperties.Size;
                                                            existingRow.StorageKey = toUpdate.Metadata.StorageKey;
                                                            existingRow.Version = toUpdate.Metadata.Version;
                                                            #endregion

                                                            #region update fields in Event
                                                            //existingRow.Event.FileChangeTypeCategoryId = changeCategoryId; // <-- changeCategoryId on insert, no need to update here
                                                            existingRow.Event.FileChangeTypeEnumId = changeEnumsBackward[toUpdate.Type];
                                                            existingRow.Event.PreviousId = toUpdatePreviousId;
                                                            existingRow.Event.SyncFrom = (toUpdate.Direction == SyncDirection.From);
                                                            #endregion

                                                            if (!SqlAccessor<Event>.UpdateRow(castTransaction.sqlConnection, existingRow.Event, castTransaction.sqlTransaction)
                                                                || !SqlAccessor<FileSystemObject>.UpdateRow(castTransaction.sqlConnection, existingRow, castTransaction.sqlTransaction))
                                                            {
                                                                toAddList.Add(toUpdate);

                                                                if (!actionOrder.Contains((byte)1)
                                                                    && (finalMergeEvent
                                                                        || toDeleteList.Count > 0))
                                                                {
                                                                    actionOrder.Add((byte)1);
                                                                }

                                                                restartForAddFound = true;
                                                            }

                                                            SetPendingRevision(castTransaction, toUpdate, existingRow.Event);
                                                        }

                                                        updatedIds.Add(toUpdate.EventId);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        storeLastMerge = (finalMergeEvent
                                            ? (Nullable<FileChangeMerge>)null
                                            : mergeEnumerator.Current);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    toReturn += ex;
                                }
                            }
                        }

                        foreach (FileChangeMerge currentMergeToFrom in mergeToFroms)
                        {
                            // If mergedEvent was not processed in AddEvents,
                            // then process badging (AddEvents processes badging for the rest)
                            if (currentMergeToFrom.MergeTo == null
                                || updatedIds.Contains(currentMergeToFrom.MergeTo.EventId))
                            {
                                MessageEvents.ApplyFileChangeMergeToChangeState(this, new FileChangeMerge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom));   // Message to invoke BadgeNet.IconOverlay.QueueNewEventBadge(currentMergeToFrom.MergeTo, currentMergeToFrom.MergeFrom)
                            }
                        }
                    }

                    if (!inputTransactionSet
                        && castTransaction != null)
                    {
                        castTransaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
                finally
                {
                    if (!inputTransactionSet
                        && castTransaction != null)
                    {
                        castTransaction.Dispose();
                    }
                }
                return toReturn;
            }
        }

        private static void SetPendingRevision(SQLTransactionalImplementation castTransaction, FileChange toUpdate, Event existingEvent)
        {
            if (toUpdate.FileDownloadPendingRevision != null
                && existingEvent.FileDownloadPendingRevision != toUpdate.FileDownloadPendingRevision)
            {
                using (ISQLiteCommand updatePendingRevision = castTransaction.sqlConnection.CreateCommand())
                {
                    updatePendingRevision.Transaction = castTransaction.sqlTransaction;

                    const string updateEventById = "+0VaKzE7jzykHTCl1rvaASO1D5+/dPEGla9m/CdyBNseDzCGBYyKXhxxmweeR3Fvl9u6+P6t2tZMj+jZsDv9vPkpTknhmoBgEVaUb622llBpQtpdyLaoVbDfWwmJ27ZhLg1xaAzgYiH5bFdM8RII5QGSExEVlcJP1CRWV6KAy2xStx8ENFZfh07cLm/WMr7q";

                    updatePendingRevision.CommandText =
                        //// before
                        //
                        //"UPDATE Events " +
                        //"SET FileDownloadPendingRevision = ? " +
                        //"WHERE EventId = ?"
                        //
                        //// after (decrypted)
                        //
                        //UPDATE Events
                        //SET FileDownloadPendingRevision = ?
                        //WHERE EventId = ?
                        Helpers.DecryptString(
                            updateEventById,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword)));

                    ISQLiteParameter updateRevisionParam = updatePendingRevision.CreateParameter();
                    updateRevisionParam.Value = toUpdate.FileDownloadPendingRevision;
                    updatePendingRevision.Parameters.Add(updateRevisionParam);

                    ISQLiteParameter eventKeyParam = updatePendingRevision.CreateParameter();
                    eventKeyParam.Value = existingEvent.EventId;
                    updatePendingRevision.Parameters.Add(eventKeyParam);

                    updatePendingRevision.ExecuteNonQuery();
                }
            }
        }
        private readonly object MergeEventsLocker = new object();

        /// <summary>
        /// The way completing an event works has changed. The following comments may be wrong: Includes an event in the last set of sync states,
        /// or in other words processes it as complete
        /// (event will no longer be included in GetEventsSinceLastSync)
        /// </summary>
        /// <param name="eventId">Primary key value of the event to process</param>
        /// <returns>Returns an error that occurred marking the event complete, if any</returns>
        public CLError MarkEventAsCompletedOnPreviousSync(long eventId, SQLTransactionalBase existingTransaction = null)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            if (existingTransaction == null
                && copyDatabaseBetweenChanges)
            {
                try
                {
                    lock (dbCopyNumber)
                    {
                        string stack;
                        try
                        {
                            stack = (new System.Diagnostics.StackTrace()).ToString();
                        }
                        catch (Exception ex)
                        {
                            stack = ex.StackTrace;
                        }

                        string dbName = indexDBLocation.Substring(0, indexDBLocation.LastIndexOf((char)0x2e /* '.' */));

                        File.Copy(indexDBLocation,
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBCopiedName,
                                dbName,
                                dbCopyNumber.Value++));

                        File.AppendAllText(
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackName,
                                dbName),
                            string.Format(
                                Resources.NotTranslatedSqlIndexerDBStackFormat,
                                dbCopyNumber.Value,
                                Environment.NewLine,
                                stack,
                                Environment.NewLine,
                                Environment.NewLine));
                    }
                }
                catch
                {
                }
            }

            CLError toReturn = null;
            SQLTransactionalImplementation castTransaction = existingTransaction as SQLTransactionalImplementation;
            bool inputTransactionSet = castTransaction != null;
            if (existingTransaction != null
                && castTransaction == null)
            {
                try
                {
                    throw new NullReferenceException("existingTransaction is not implemented as private derived type. It should be retrieved via method GetNewTransaction method. Creating a new transaction instead which will be committed immediately.");
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }

            FileChangeType storeExistingChangeType;
            string storeNewPath;
            string storeOldPath;
            bool storeWhetherEventIsASyncFrom;

            bool foundOtherPendingAtCompletedPath;
            try
            {
                if (castTransaction == null)
                {
                    ISQLiteConnection indexDB;
                    castTransaction = new SQLTransactionalImplementation(
                        indexDB = CreateAndOpenCipherConnection(),
                        indexDB.BeginTransaction(System.Data.IsolationLevel.Serializable));
                }

                //// don't think I need to change the SyncCounter ever when just completing an event, it should already be set
                //
                //long lastSyncCount;
                //if (!SqlAccessor<object>.TrySelectScalar(
                //    indexDB,
                //    "SELECT Syncs.SyncCounter " +
                //    "FROM Syncs " +
                //    "ORDER BY Syncs.SyncCounter DESC " +
                //    "LIMIT 1",
                //    out lastSyncCount,
                //    indexTran))
                //{
                //    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Cannot complete an event without a previous sync point");
                //}

                GenericHolder<CLError> moveObjectsToNewParentError = new GenericHolder<CLError>(null);
                var moveObjectsToNewParent = DelegateAndDataHolderBase.Create(
                    new
                    {
                        castTransaction = castTransaction,
                        oldId = new GenericHolder<long>(),
                        newId = new GenericHolder<long>()
                    },
                    (Data, errorToAccumulate) =>
                    {
                        try
                        {
                            using (ISQLiteCommand moveChildrenCommand = Data.castTransaction.sqlConnection.CreateCommand())
                            {
                                moveChildrenCommand.Transaction = Data.castTransaction.sqlTransaction;

                                const string updateFileSystemObjectByParentId = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFVHLHxUuWhXcYIIDQlVL5HeF4UTFjYSdKBH0wm0SsApDR77FTEJf3TPQXB4/rBAm+Q1+CWo5fRZWJr88LHe1DnN90L3GNZi6wRW7lGXeTHCUmFj/D2S4qcja3kxNFznRny8ZKrvRqJE2hMpzYaS9sig==";

                                moveChildrenCommand.CommandText =
                                    //// before
                                    //
                                    //"UPDATE FileSystemObjects " +
                                    //"SET ParentFolderId = ? " +
                                    //"WHERE ParentFolderId = ?"
                                    //
                                    //// after (decrypted)
                                    //
                                    //UPDATE FileSystemObjects
                                    //SET ParentFolderId = ?
                                    //WHERE ParentFolderId = ?
                                    Helpers.DecryptString(
                                        updateFileSystemObjectByParentId,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword)));

                                ISQLiteParameter newObjectId = moveChildrenCommand.CreateParameter();
                                newObjectId.Value = Data.newId.Value;
                                moveChildrenCommand.Parameters.Add(newObjectId);

                                ISQLiteParameter oldObjectId = moveChildrenCommand.CreateParameter();
                                oldObjectId.Value = Data.oldId.Value;
                                moveChildrenCommand.Parameters.Add(oldObjectId);

                                moveChildrenCommand.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            errorToAccumulate.Value += ex;
                        }
                    },
                    moveObjectsToNewParentError);

                const string selectFileSystemObjectByEventId = "XiF/n8DAmECRcpl1q3g5SOaFkrEO/c+iI1V66stCO9Yv7K3QnlXqj68P0vDvLKPEvsMAT4aYICpfeh5DRpnmvL5qfP/ZUoDajTeFKO752ECwpnxQ1+SCCaqYKnwzeo/Wh2QtqIOGL30cai1QEsJGsX2wbiW96Ht+tWlP7fwugSNF9xClHx6bAQUDBjV7POG2BBOYOxclGuxCoxdfH4INOIe2krCYjkG4XnXE0yToE4GK/gFFSYRGTEoZ2aW7sCxPVdr9EGukZZcgDMbGgxDI40f6DqRXW59ORN/PomUqqelyKGq4Ph0qCZmQVVG2UCoQATQn24CQENDgWyjgYHguK86KqVjDXuRbyhnvqNofwp3J/mdKy+Ov+h9eO/QpeqTAsCp9Gg9hvoxNH+IiJYp4SGP3/tT3KXT+MYwdll2uPkaquDhrkSLBeCcbUQB4Qr2sJylOE8sFIfJjvr377C1RzAPtrFNkaTd9P22gnH+0nlK5RRRHA7d85B9zRqEp1HRcks78J3FTJUPpgjRfVnWKH3SXZgxGxBsctkwM7yo/DfH79d6wdvLsU/cdEHTlEeLx2STEfxd2l51en+Nu/+ZaH9gei1Kck6eAIe2Smrcm2C+lKqttvuYFSbuXcMhtuZkOj6BmM8dmh+Z3yO7FqQhoqdDQG6GVQqvhe7x3CokZQ4AJ+JkbsXiw8Z4PrDQSWGvChuk8nhjwyg0kWXBDSHxLrgaYEUVFbf7dioO68bMLeH3mCwssEZHmbkOmcms75RZf95D+FupARaHWpFvUQukUV3IxKwhlaA9dKcJoPnHXst176HqIrVYYApZyt4tjPq1+9GX1zbbnZxriyAdobDMplco1LdHqp8Fy7anhTocS8gMmDzytyVwtGg/JyJtw70eOJxp6xGm6hrOie0UP8xcCIW2LqRKQEWx/wZT2g2RR5vefEOakrmD6CroLA725QbjNpbMQ+Z6hgEw08tnsn8IG7r7WZfrfsYGrGdju47PIWw997WI/Fx+0zXojF9QkPCbiBuYfXx0UH5vKPgDYxfWATyEhfLuFiF+uxzv9x0nHsVM=";

                FileSystemObject existingEventObject = SqlAccessor<FileSystemObject>.SelectResultSet(
                        castTransaction.sqlConnection,
                        //// before
                        //
                        //"SELECT " +
                        //SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                        //SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent) + ", " +
                        //SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEventPrevious, Resources.NotTranslatedSqlIndexerPreviouses) + ", " +
                        //SqlAccessor<SqlServerUid>.GetSelectColumns(Resources.NotTranslatedSqlIndexerServerUid) +
                        //" FROM FileSystemObjects" +
                        //" INNER JOIN Events ON FileSystemObjects.EventId = Events.EventId" +
                        //" LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = Previouses.FileSystemObjectId" +
                        //" INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidId" +
                        //" WHERE FileSystemObjects.EventId = ?" + // <-- parameter 1
                        //" ORDER BY FileSystemObjects.FileSystemObjectId DESC" +
                        //" LIMIT 1"
                        //
                        //// after (decrypted; {0}: SqlAccessor<FileSystemObject>.GetSelectColumns()
                        //// {1}: SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent)
                        //// {2}: SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEventPrevious, Resources.NotTranslatedSqlIndexerPreviouses)
                        //// {3}: SqlAccessor<SqlServerUid>.GetSelectColumns(Resources.NotTranslatedSqlIndexerServerUid) )
                        //
                        //SELECT
                        //{0},
                        //{1},
                        //{2},
                        //{3}
                        //FROM FileSystemObjects
                        //INNER JOIN Events ON FileSystemObjects.EventId = Events.EventId
                        //LEFT OUTER JOIN FileSystemObjects Previouses ON Events.PreviousId = Previouses.FileSystemObjectId
                        //INNER JOIN ServerUids ON FileSystemObjects.ServerUidId = ServerUids.ServerUidId
                        //WHERE FileSystemObjects.EventId = ?
                        //ORDER BY FileSystemObjects.FileSystemObjectId DESC
                        //LIMIT 1
                        string.Format(
                            Helpers.DecryptString(
                                selectFileSystemObjectByEventId,
                                Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))),
                            SqlAccessor<FileSystemObject>.GetSelectColumns(),
                            SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent),
                            SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEventPrevious, Resources.NotTranslatedSqlIndexerPreviouses),
                            SqlAccessor<SqlServerUid>.GetSelectColumns(Resources.NotTranslatedSqlIndexerServerUid)),
                        new[]
                        {
                            Resources.NotTranslatedSqlIndexerEvent,
                            Resources.NotTranslatedSqlIndexerEventPrevious,
                            Resources.NotTranslatedSqlIndexerServerUid
                        },
                        castTransaction.sqlTransaction,
                        Helpers.EnumerateSingleItem(eventId))
                    .SingleOrDefault();

                if (existingEventObject == null
                    || existingEventObject.Event == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find existing event to complete");
                }
                if (!existingEventObject.Pending)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing event already not pending");
                }
                if (existingEventObject.ParentFolderId == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "The root folder object should never have been pending to complete");
                }
                if (existingEventObject.ServerUid.ServerUid == null)
                {
                    // server "uid" can be null for conflicts where we rename the local item and have a dependent creation at the new path (since the rename was technically never communicated)

                    const string selectWhetherFileSystemObjectExistsForServerUid = "6dvoJhuOH09lpRVxcMPuf6zJlm1x1mPQwh0GHA5i+ZnugtIOHJ+83KhGWVKyKcFIWN5b7j4nF/b02+876/EtRm4nLlKBd/lfUruqxyv1XuZjoGD1DtrqiiYL1PD6RIeR8E8h5ofG9jHvmi2HwW5F+k72jxpHCdarqCydoTpF+dL2jhZveV7S82vbUDoI0suofkXx+tHp2E2/sViH8ByHrg0dVOv53TjY5E8m55rWMZpKiaT1FUCgFjpkK1/0Gu+avQuhdZCBXTS8g8POnXwG1Gn88UhPf0dPZxOs3GEdowNnI/Bc+F7G2xHzCedjPUwrA7cSGu6TPwSXdtsy9QzuZ3EObv6uuHKpES7B8eUiivUWAGJdjZB+o5FHDbzx3wWasm3bHTiB7M0EDGejKgi1H5tFCKTibeyeRrp9apBYWlUzcfGmn1uZDuJZ2C9nrvAp5vvrvFwzI4MRE9LLOL6o5rWml0MXdm8kd+o4bfoW937jVjA0NBO+fcwD/EeGUkpn80YvkMdyiFzCo5OzkmSKCjmyRJl96PfLekuWSlVBUfYdK7gvEcrgH6FZ69LrLf9qLXo+SaVsGp+wEotVY+6v3pIQaoscoIHYufeoUHVqliUc6r7oMPQtPixZ9EsAbGNkN2QK1wFHJj+XBDniMZ0HXa0iGRrSg/FcPLXnafjRZXLV4AtnZbOMYFajVCUdI7prFSXTUBmImJwdzAmtJzXK1Q==";

                    bool foundLaterCreate;
                    if (existingEventObject.EventOrder == null
                        || !SqlAccessor<object>.TrySelectScalar<bool>(
                            castTransaction.sqlConnection,
                            //// before
                            //
                            //"SELECT EXISTS " +
                            //"(" +
                            //    "SELECT NULL " +
                            //    "FROM FileSystemObjects " +
                            //    "INNER JOIN Events ON Events.EventId = FileSystemObjects.EventId " +
                            //    "WHERE FileSystemObjects.ServerUidId = ? " + // <-- parameter 1
                            //    "AND FileSystemObjects.Pending = 1 " +
                            //    "AND Events.FileChangeTypeEnumId = " + changeEnumsBackward[FileChangeType.Created] +
                            //    " AND EventOrder > ?" + // <-- parameter 2
                            //") AS EXIST"
                            //
                            //// after (decrypted; {0}: changeEnumsBackward[FileChangeType.Created])
                            //
                            //SELECT EXISTS
                            //(
                            //SELECT NULL
                            //FROM FileSystemObjects
                            //INNER JOIN Events ON Events.EventId = FileSystemObjects.EventId
                            //WHERE FileSystemObjects.ServerUidId = ?
                            //AND FileSystemObjects.Pending = 1
                            //AND Events.FileChangeTypeEnumId = {0}
                            //AND EventOrder > ?
                            //) AS EXIST
                            string.Format(
                                Helpers.DecryptString(
                                    selectWhetherFileSystemObjectExistsForServerUid,
                                    Encoding.ASCII.GetString(
                                        Convert.FromBase64String(indexDBPassword))),
                                changeEnumsBackward[FileChangeType.Created]),
                            out foundLaterCreate,
                            castTransaction.sqlTransaction,
                            new[] { existingEventObject.ServerUidId, (long)existingEventObject.EventOrder })
                        || !foundLaterCreate)
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing event cannot be completed if it does not have a ServerUid");
                    }
                }
                if (existingEventObject.Event.PreviousId != null
                    && existingEventObject.Event.Previous == null)
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find previous object for rename");
                }

                storeExistingChangeType = changeEnums[existingEventObject.Event.FileChangeTypeEnumId];
                storeNewPath = existingEventObject.CalculatedFullPath;
                storeOldPath = (existingEventObject.Event.Previous == null ? null : existingEventObject.Event.Previous.CalculatedFullPath);
                storeWhetherEventIsASyncFrom = existingEventObject.Event.SyncFrom;

                using (ISQLiteCommand existingObjectCommand = castTransaction.sqlConnection.CreateCommand())
                {
                    existingObjectCommand.Transaction = castTransaction.sqlTransaction;

                    const string selectFileSystemObjectIdByNameHashAndParentId = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYM4A9CJRjJ2AJWbK+2XeEfjsHjbyLTBN0A1N3nA0Q4qVNOEMU6uWFsvHIyt+vO7mRotjaQ6thfD0JG4dEBb6CwJ86VmbNFzR840DvU9mAN+U2/rU023TbaueJ53hdMdiMrsHsu2emU03kLqHuA9I1RdnUArec/ZYboGWZS7mPtAmKEkUdrfKdetzs/JO0NWcMsHuZ7mKfmHiqXUHNr/qwsUiM6TaJREacTRgqaAoEVEXyRjnzCYvqQm1xobLZ0xn7SNGaExMY6AbrlyrqwsC0Xfxjh9IJX7uGxnwk0ifajdMtJIujvhxFfBYfxqqNGY+7WhygNIifBFkxj3ajby0KYaU2UZtw99w1FjVPu0JcRlxAFNN7ldVy2O1/t6PzqAIQ4kWQ3JpyhhQSCC0gqibdZ9jYj3LijDbhgChrrNXwOm/HG8ktPgsOwWvR5vapyY352MR6iCUVyCDfkdGPWjcfRg5dchyoKzfwoLliFjnCIQugMOOExL3KrQv/p/9ZWQHKDzBHFHiE9QrzUUefcIeSD9xL8SiIexC/9m5h1rbVk/A6xKOuhgCxzoA+VQKmKgCkp8/0D1y+0to1Zug7xoopb8W5q5Z9veDiXnJf0Gh8thwpolDI91aKDfxSAwAsIutEs";

                    existingObjectCommand.CommandText =
                        //// before
                        //
                        //"SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.Name " +
                        //"FROM FileSystemObjects " +
                        //"WHERE FileSystemObjects.ParentFolderId = ? " + // <-- parameter 1
                        //"AND FileSystemObjects.NameCIHash = ? " + // <-- parameter 2
                        //"AND FileSystemObjects.Pending = 0 " +
                        //"ORDER BY FileSystemObjects.FileSystemObjectId DESC"
                        //
                        //// after (decrypted)
                        //
                        //SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.Name
                        //FROM FileSystemObjects
                        //WHERE FileSystemObjects.ParentFolderId = ?
                        //AND FileSystemObjects.NameCIHash = ?
                        //AND FileSystemObjects.Pending = 0
                        //ORDER BY FileSystemObjects.FileSystemObjectId DESC
                        Helpers.DecryptString(
                            selectFileSystemObjectIdByNameHashAndParentId,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword)));

                    ISQLiteParameter existingParentParam = existingObjectCommand.CreateParameter();
                    existingParentParam.Value = (long)existingEventObject.ParentFolderId;
                    existingObjectCommand.Parameters.Add(existingParentParam);

                    ISQLiteParameter existingNameCIHashParam = existingObjectCommand.CreateParameter();
                    existingNameCIHashParam.Value = existingEventObject.NameCIHash;
                    existingObjectCommand.Parameters.Add(existingNameCIHashParam);

                    using (ISQLiteDataReader existingObjectReader = existingObjectCommand.ExecuteReader(CommandBehavior.SingleResult))
                    {
                        while (existingObjectReader.Read())
                        {
                            if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(existingObjectReader[Resources.NotTranslatedSqlIndexerName]), existingEventObject.Name))
                            {
                                long existingNonPendingIdToMerge = Convert.ToInt64(existingObjectReader[Resources.NotTranslatedSqlIndexerFileSystemObjectId]);

                                //// The following cases below seemed to happen under normal use and we don't wish to kill the sync engine,
                                //// it would be better if we fixed the causes of the conditions below from happening
                                //
                                //switch (storeExistingChangeType)
                                //{
                                //    case FileChangeType.Created:
                                //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Should not have an existing object with the same name under the same parent already not pending if this pending event represents a create");

                                //    case FileChangeType.Renamed:
                                //        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Should not have an existing object with the same name under the same parent already not pending if this pending event represents a rename");
                                //}

                                moveObjectsToNewParent.TypedData.oldId.Value = existingNonPendingIdToMerge;
                                moveObjectsToNewParent.TypedData.newId.Value = existingEventObject.FileSystemObjectId;
                                moveObjectsToNewParent.Process();
                                if (moveObjectsToNewParentError.Value != null)
                                {
                                    throw new AggregateException("An error occurred moving objects to new parent", moveObjectsToNewParentError.Value.Exceptions);
                                }

                                using (ISQLiteCommand movePreviousesCommand = castTransaction.sqlConnection.CreateCommand())
                                {
                                    movePreviousesCommand.Transaction = castTransaction.sqlTransaction;

                                    const string updateEventByPreviousId = "+0VaKzE7jzykHTCl1rvaASO1D5+/dPEGla9m/CdyBNstTMfPv6533filFJcEBqd5j5afu13lzZLCKCfJcQO6mbX2EPtGlVAJN3vWsR7TBZe4UoYru96Uqn90xoqlWOF4bIac6eujNY1LKdkwljiTVA==";

                                    movePreviousesCommand.CommandText =
                                        //// before
                                        //
                                        //"UPDATE Events " +
                                        //"SET PreviousId = ? " +
                                        //"WHERE PreviousId = ?"
                                        //
                                        //// after (decrypted)
                                        //
                                        //UPDATE Events
                                        //SET PreviousId = ?
                                        //WHERE PreviousId = ?
                                        Helpers.DecryptString(
                                            updateEventByPreviousId,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword)));

                                    ISQLiteParameter newPreviousId = movePreviousesCommand.CreateParameter();
                                    newPreviousId.Value = existingEventObject.FileSystemObjectId;
                                    movePreviousesCommand.Parameters.Add(newPreviousId);

                                    ISQLiteParameter oldPreviousId = movePreviousesCommand.CreateParameter();
                                    oldPreviousId.Value = existingNonPendingIdToMerge;
                                    movePreviousesCommand.Parameters.Add(oldPreviousId);

                                    movePreviousesCommand.ExecuteNonQuery();
                                }

                                SqlAccessor<FileSystemObject>.DeleteRow(
                                    castTransaction.sqlConnection,
                                    new FileSystemObject()
                                    {
                                        FileSystemObjectId = existingNonPendingIdToMerge
                                    },
                                    castTransaction.sqlTransaction);

                                break;
                            }
                            //// The following cases below seemed to happen under normal use and we don't wish to kill the sync engine,
                            //// it would be better if we fixed the causes of the conditions below from happening
                            //
                            //else if ([no more rows left])
                            //{
                            //    switch (storeExistingChangeType)
                            //    {
                            //        case FileChangeType.Modified:
                            //            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Must have an existing object with the same name under the same parent already not pending if this pending event represents a modify");

                            //        case FileChangeType.Deleted:
                            //            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Must have an existing object with the same name under the same parent already not pending if this pending event represents a delete");
                            //    }
                            //}
                        }
                    }
                }

                switch (storeExistingChangeType)
                {
                    case FileChangeType.Created:
                    case FileChangeType.Modified:
                        existingEventObject.Pending = false;
                        existingEventObject.EventTimeUTCTicks = DateTime.UtcNow.Ticks;
                        if (!SqlAccessor<FileSystemObject>.UpdateRow(
                            castTransaction.sqlConnection,
                            existingEventObject,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to update existing event to not be pending");
                        }

                        if (storeWhetherEventIsASyncFrom
                            && existingEventObject.Event.FileDownloadPendingRevision != null)
                        {
                            using (ISQLiteCommand updateRevision = castTransaction.sqlConnection.CreateCommand())
                            {
                                updateRevision.Transaction = castTransaction.sqlTransaction;

                                const string updateServerUidById = "qvD42Gwb7pnBtXYpI3VuUXDJmBfa6scsRn9hnyU8DIa2GT9xBMRBJ+KNWYdhNGu7pDRJvGlKOg0IwTaPJgbvevTCpUFEfCvRuXT1AlfsEFHQq5F27IAn5i+KFAlj6P1dnc52kgd3d9fsGLgWfUPYsRv4XoHsUe2uN+PWvjRM0Qo=";

                                updateRevision.CommandText =
                                    //// before
                                    //
                                    //"UPDATE ServerUids " +
                                    //"SET Revision = ? " +
                                    //"WHERE ServerUidId = ?"
                                    //
                                    //// after (decrypted)
                                    //
                                    //UPDATE ServerUids
                                    //SET Revision = ?
                                    //WHERE ServerUidId = ?
                                    Helpers.DecryptString(
                                        updateServerUidById,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword)));

                                ISQLiteParameter revisionParameter = updateRevision.CreateParameter();
                                revisionParameter.Value = existingEventObject.Event.FileDownloadPendingRevision;
                                updateRevision.Parameters.Add(revisionParameter);

                                ISQLiteParameter serverUidKey = updateRevision.CreateParameter();
                                serverUidKey.Value = existingEventObject.ServerUidId;
                                updateRevision.Parameters.Add(serverUidKey);

                                updateRevision.ExecuteNonQuery();
                            }
                        }
                        break;

                    case FileChangeType.Deleted:
                        if (!SqlAccessor<FileSystemObject>.DeleteRow(
                            castTransaction.sqlConnection,
                            existingEventObject,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to apply deletion to complete a delete event");
                        }
                        break;

                    case FileChangeType.Renamed:
                        existingEventObject.Pending = false;
                        existingEventObject.EventTimeUTCTicks = DateTime.UtcNow.Ticks;
                        if (existingEventObject.Event.PreviousId == null)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Rename event cannot have a null PreviousId");
                        }
                        else if (existingEventObject.Event.Previous == null)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Rename event has a PreviousId, but the previous object was not retrieved");
                        }

                        long storePreviousId = (long)existingEventObject.Event.PreviousId; // store previous id, since we are about to nullify the event value but still need it to delete\move children

                        List<long> objectIdsToMove = null;

                        using (ISQLiteCommand findObjectIdsToMove = castTransaction.sqlConnection.CreateCommand())
                        {
                            findObjectIdsToMove.Transaction = castTransaction.sqlTransaction;

                            const string selectFileSystemObjectIdByNameHash = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYM4A9CJRjJ2AJWbK+2XeEfjsHjbyLTBN0A1N3nA0Q4qVNOEMU6uWFsvHIyt+vO7mRotjaQ6thfD0JG4dEBb6CwJ86VmbNFzR840DvU9mAN+U2/rU023TbaueJ53hdMdiMrsHsu2emU03kLqHuA9I1RdnUArec/ZYboGWZS7mPtAmKEkUdrfKdetzs/JO0NWcMsHuZ7mKfmHiqXUHNr/qwsUiM6TaJREacTRgqaAoEVEXyRjnzCYvqQm1xobLZ0xn7SNGaExMY6AbrlyrqwsC0Xfxjh9IJX7uGxnwk0ifajdMtJIujvhxFfBYfxqqNGY+7WhygNIifBFkxj3ajby0KYaU2UZtw99w1FjVPu0JcRlxAFNN7ldVy2O1/t6PzqAIQ4kWQ3JpyhhQSCC0gqibdZ9jYj3LijDbhgChrrNXwOm/HG8ktPgsOwWvR5vapyY352NAw8BDv+G1A5YCq1ycEew2IPY71itsEKGgoOQgF7FLeZw5Q7V3hxJo8/sNls2t7WEcsynBejuzDV2ZAuaCQhmjJ4umDM3A7+ksJdM1dc3efcDkUAMqDRoCztsacidTYKoTUlLBAw0+kKqnvOF4FLuuhT2L3A3Qu38xusCQeXTDj9gKrsRGyHn0570d0tGmQvOCUuqwe+D+b2Vby+Gl262NTbzCqPnqrtsyy7hv+kKRHXnuL6xpVbUamCi0G93WqtYBrqwdwK1nTG4APDZxh/+i6BT2H3zbfkkfO0S1Temr/6DPOG+f1VS0ZRVTQs12lyl5CC7EKcq38tnb1ZXb0SAT6PMElzveAzfkXs0X+WqtritqMMJpKmpvtifwYC8PlQM9xsHNjdIvrJ8ITk9wuamFhlN/DX83H6K64T3Oruzohqv4PPlisgce3g/wBqOhKiSPz7x7jnS+TVFu5GsAms/Zu/ddMJE3Vti7CNfK1cTpatNgZiIZNnFTgeAZi0gEHrTpx1KWcPeRJWrGFaOWGw/P9dv1uGapbf/98ET5ZFYATcWuLvclLUvscTtypM+M+iLae+zyqZLV0uXPdsRQ65KcAzEsoEU/E9aiGeEdIvPBqgnc8ak99lp9DhgY5FjLtgvGdCufoj2LBndH/ApBX0dUKDEWAsBH3g32kOCCtZPchlWO7D0d3ZOFFAzx8Bpm/Qo/rN3kLNt8jRPLGuT5lKoi6QkJBTPeScHad97aUO3l6bAhEzNRDxE1pOpSxrHNnyhXpXrsWTG+T0rp+ba5FQP5WpQrvYQTMBzKzGNjU1NofeSuJfErG4XtMZne3uK2dd2Ci5qK7+mYAd4xWvdlKZTEW80xAbuf7/dXMS4r4JlnZ28LlCdvaYSk6ep2Y/OX07XCmJb8VB3Uw3m3C/PBaLVza/UB6z3Wo1VO7/nURkMUNhTFUG8cZzCH/RSGqwcRQmh3Z+zlF8S0WLU4izx1qWa5Y5QHqP6x4nbDw55FJmlIEbgomC/HILywnUiqfd4/OdH8QRdvcefZio8zh4HgOgPd003Vh02tOkisGMJdt2z1P36AqRl+CSAvtjoK5oMmV/3gqG7EJo6y7geaM/XIhBxXKqeu5hcSkkNCwgsy0wiqru8ktOv31l4Y57yfmhYQqFyTIwiF4X+SvlhYcrq2dlESlq7Mf+t4fbg5+aqrIV0L3MTFthnGYurxudQ7qkOk+6//3HjOWYCx/4/F/1e80qAUU2hqczyueQrPYbquv+BFngWmdFr+XzLTDX6aCXvz+wqklpmzSMSh6Mcl2gMZ48rA==";

                            findObjectIdsToMove.CommandText =
                                //// before
                                //
                                //"SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.Name " +
                                //"FROM FileSystemObjects " +
                                //"WHERE FileSystemObjects.ParentFolderId = ? " + // <-- parameter 1
                                //"AND FileSystemObjects.NameCIHash = ? " + // <-- parameter 2
                                //"AND FileSystemObjects.EventOrder < " +
                                //"(" +
                                //    "SELECT UpperExclusiveOrder " +
                                //    "FROM " +
                                //    "(" +
                                //        "SELECT MAX(InnerObjects.EventOrder) + 1 as UpperExclusiveOrder " +
                                //        "FROM FileSystemObjects InnerObjects" +
                                //        " " +
                                //        "UNION SELECT MIN(InnerObjects.EventOrder) " +
                                //        "FROM FileSystemObjects InnerObjects " +
                                //        "INNER JOIN Events ON InnerObjects.EventId = Events.EventId " +
                                //        "WHERE InnerObjects.EventOrder > ? " + // <-- parameter 3
                                //        "AND Events.FileChangeTypeEnumId IN (" + changeEnumsBackward[FileChangeType.Created] + ", " + changeEnumsBackward[FileChangeType.Renamed] + ")" +
                                //    ") " +

                                //    // first order to grab non-null event orders first
                                //    "ORDER BY " +
                                //    "CASE WHEN UpperExclusiveOrder IS NULL " +
                                //    "THEN 1 " +
                                //    "ELSE 0 " +
                                //    "END, " +

                                //    // then order by the event order itself (ascending)
                                //    "UpperExclusiveOrder " +

                                //    // only take the lowest event order (either the maximum or the lowest create or rename)
                                //    "LIMIT 1" +
                                //")"
                                //
                                //// after (decrypted; {0}: changeEnumsBackward[FileChangeType.Created]
                                //// {1}: changeEnumsBackward[FileChangeType.Renamed] )
                                //
                                //SELECT FileSystemObjects.FileSystemObjectId, FileSystemObjects.Name
                                //FROM FileSystemObjects
                                //WHERE FileSystemObjects.ParentFolderId = ?
                                //AND FileSystemObjects.NameCIHash = ?
                                //AND FileSystemObjects.EventOrder <
                                //(
                                //SELECT UpperExclusiveOrder
                                //FROM
                                //(
                                //SELECT MAX(InnerObjects.EventOrder) + 1 as UpperExclusiveOrder
                                //FROM FileSystemObjects InnerObjects
                                //UNION SELECT MIN(InnerObjects.EventOrder)
                                //FROM FileSystemObjects InnerObjects
                                //INNER JOIN Events ON InnerObjects.EventId = Events.EventId
                                //WHERE InnerObjects.EventOrder > ?
                                //AND Events.FileChangeTypeEnumId IN ({0}, {1})
                                //)
                                //ORDER BY
                                //CASE WHEN UpperExclusiveOrder IS NULL
                                //THEN 1
                                //ELSE 0
                                //END,
                                //UpperExclusiveOrder
                                //LIMIT 1
                                //)
                                string.Format(
                                    Helpers.DecryptString(
                                        selectFileSystemObjectIdByNameHash,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword))),
                                    changeEnumsBackward[FileChangeType.Created],
                                    changeEnumsBackward[FileChangeType.Renamed]);

                            ISQLiteParameter previousParentParam = findObjectIdsToMove.CreateParameter();
                            previousParentParam.Value = (long)existingEventObject.Event.Previous.ParentFolderId;
                            findObjectIdsToMove.Parameters.Add(previousParentParam);

                            ISQLiteParameter previousNameCIHashParam = findObjectIdsToMove.CreateParameter();
                            previousNameCIHashParam.Value = existingEventObject.Event.Previous.NameCIHash;
                            findObjectIdsToMove.Parameters.Add(previousNameCIHashParam);

                            ISQLiteParameter renameEventOrderParam = findObjectIdsToMove.CreateParameter();
                            renameEventOrderParam.Value = existingEventObject.EventOrder;
                            findObjectIdsToMove.Parameters.Add(renameEventOrderParam);

                            using (ISQLiteDataReader existingObjectReader = findObjectIdsToMove.ExecuteReader(CommandBehavior.SingleResult))
                            {
                                while (existingObjectReader.Read())
                                {
                                    if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(existingObjectReader[Resources.NotTranslatedSqlIndexerName]), existingEventObject.Name))
                                    {
                                        long previousObjectId = Convert.ToInt64(existingObjectReader[Resources.NotTranslatedSqlIndexerFileSystemObjectId]);

                                        if (objectIdsToMove == null)
                                        {
                                            objectIdsToMove = new List<long>(Helpers.EnumerateSingleItem(previousObjectId));
                                        }
                                        else
                                        {
                                            objectIdsToMove.Add(previousObjectId);
                                        }
                                    }
                                }
                            }
                        }

                        if (objectIdsToMove != null)
                        {
                            using (ISQLiteCommand moveOtherMatchingOldNames = castTransaction.sqlConnection.CreateCommand())
                            {
                                moveOtherMatchingOldNames.Transaction = castTransaction.sqlTransaction;

                                //// initial attempt to move all the other events at the same location to follow the rename to its new path
                                //
                                //moveOtherMatchingOldNames.CommandText =
                                    //// before
                                    //
                                    //"UPDATE FileSystemObjects " +
                                    //    "SET ParentFolderId = ?, " + // <-- parameter 1
                                    //    "Name = ? " + // <-- parameter 2
                                    //    "WHERE ParentFolderId = ? " + // <-- parameter 3
                                    //    "AND Name = ?" // <-- parameter 4
                                    //
                                    //// after (decrypted)
                                    //
                                    // [missing]

                                // find lowest EventOrder with same ParentFolderId and Name with a greater EventOrder than the current object which also has an Event with a FileChangeTypeEnumId which represents either "created" or "renamed", if any
                                // if this EventOrder is found, limit the above query by "AND EventOrder < [lowest EventOrder of create\rename]"

                                const string updateFileSystemObjectNameById = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFVHLHxUuWhXcYIIDQlVL5HeF4UTFjYSdKBH0wm0SsApDR77FTEJf3TPQXB4/rBAm+DHc/VlscUu41BMhP8dUMg8ulzLseduyF32AWS/CSs7mWnojj3v9neU5pUlYb5fgEKZ8nPdkQIwr4RZCRGZ7799a+AvNuV/BUXo/snEzU1D7JmDb9RS00QS96KnjosppMifdxgKnDxO9qPVzgOyzAIXeONVu4zAPxTgr5I9YQF3c=";

                                moveOtherMatchingOldNames.CommandText =
                                    //// before
                                    //
                                    //"UPDATE FileSystemObjects " +
                                    //"SET ParentFolderId = ?, " + // <-- parameter 1
                                    //"Name = ?, " + // <-- parameter 2
                                    //"NameCIHash = ? " + // <-- parameter 3
                                    //"WHERE FileSystemObjectId IN (" +
                                    //string.Join(new string(new [] { ((char)0x2c) /* ',' */, ((char)0x20) /* ' ' */ }), Enumerable.Repeat<char>(((char)0x3f) /* '?' */, objectIdsToMove.Count)) + // <-- parameter 4 through 'n + 3' where 'n' is the number of ids to move
                                    //")"
                                    //
                                    //// after (decrypted; {0}: string.Join(new string(new [] { ((char)0x2c) /* ',' */, ((char)0x20) /* ' ' */ }), Enumerable.Repeat<char>(((char)0x3f) /* '?' */, objectIdsToMove.Count)) )
                                    //
                                    //UPDATE FileSystemObjects
                                    //SET ParentFolderId = ?,
                                    //Name = ?,
                                    //NameCIHash = ?
                                    //WHERE FileSystemObjectId IN ({0})
                                    string.Format(
                                        Helpers.DecryptString(
                                            updateFileSystemObjectNameById,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword))),
                                        string.Join(new string(new[] { ((char)0x2c) /* ',' */, ((char)0x20) /* ' ' */ }), Enumerable.Repeat<char>(((char)0x3f) /* '?' */, objectIdsToMove.Count)));

                                ISQLiteParameter newParentParam = moveOtherMatchingOldNames.CreateParameter();
                                newParentParam.Value = existingEventObject.ParentFolderId;
                                moveOtherMatchingOldNames.Parameters.Add(newParentParam);

                                ISQLiteParameter newNameParam = moveOtherMatchingOldNames.CreateParameter();
                                newNameParam.Value = existingEventObject.Name;
                                moveOtherMatchingOldNames.Parameters.Add(newNameParam);

                                ISQLiteParameter newNameCIHashParam = moveOtherMatchingOldNames.CreateParameter();
                                newNameCIHashParam.Value = existingEventObject.NameCIHash;
                                moveOtherMatchingOldNames.Parameters.Add(newNameCIHashParam);

                                foreach (long objectIdToMove in objectIdsToMove)
                                {
                                    ISQLiteParameter objectIdParam = moveOtherMatchingOldNames.CreateParameter();
                                    objectIdParam.Value = objectIdToMove;
                                    moveOtherMatchingOldNames.Parameters.Add(objectIdParam);
                                }

                                moveOtherMatchingOldNames.ExecuteNonQuery();
                            }
                        }

                        existingEventObject.Event.PreviousId = null; // allows us to delete the FileSystemObject for the previous location so we don't have two of them non-pending to represent the same item
                        if (!SqlAccessor<Event>.UpdateRow(
                            castTransaction.sqlConnection,
                            existingEventObject.Event,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to disconnect rename event from previous id in order to delete it");
                        }

                        moveObjectsToNewParent.TypedData.oldId.Value = storePreviousId;
                        moveObjectsToNewParent.TypedData.newId.Value = existingEventObject.FileSystemObjectId;
                        moveObjectsToNewParent.Process();
                        if (moveObjectsToNewParentError.Value != null)
                        {
                            throw new AggregateException("An error occurred moving objects to new parent", moveObjectsToNewParentError.Value.Exceptions);
                        }

                        if (!existingEventObject.Event.Previous.Pending
                            && !SqlAccessor<FileSystemObject>.DeleteRow(
                                castTransaction.sqlConnection,
                                new FileSystemObject()
                                {
                                    FileSystemObjectId = storePreviousId
                                },
                                castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to delete previous object for rename event");
                        }

                        if (!SqlAccessor<FileSystemObject>.UpdateRow(
                            castTransaction.sqlConnection,
                            existingEventObject,
                            castTransaction.sqlTransaction))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to update existing event to not be pending");
                        }
                        break;

                    default:
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Existing event object had a FileChangeTypeEnumId which did not match to a known FileChangeType");
                }

                foundOtherPendingAtCompletedPath = CheckForPendingAtNameAndParent(castTransaction, (long)existingEventObject.ParentFolderId, existingEventObject.Name, existingEventObject.NameCIHash);

                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                storeExistingChangeType = Helpers.DefaultForType<FileChangeType>();
                storeNewPath = Helpers.DefaultForType<string>();
                storeOldPath = Helpers.DefaultForType<string>();
                storeWhetherEventIsASyncFrom = Helpers.DefaultForType<bool>();
                foundOtherPendingAtCompletedPath = Helpers.DefaultForType<bool>();
                toReturn += ex;
            }
            finally
            {
                if (!inputTransactionSet
                    && castTransaction != null)
                {
                    castTransaction.Dispose();
                }
            }

            if (toReturn == null
                && !foundOtherPendingAtCompletedPath)
            {
                try
                {
                    MarkBadgeSyncedAfterEventCompletion(storeExistingChangeType, storeNewPath, storeOldPath, storeWhetherEventIsASyncFrom);
                }
                catch (Exception ex)
                {
                    toReturn += ex;
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Call this when the location of the sync folder has changed (while syncing is stopped) to update the entire index to all new paths based in the new root folder
        /// </summary>
        public CLError ChangeSyncboxPath(string newSyncboxPath)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            try
            {
                if (string.IsNullOrEmpty(newSyncboxPath))
                {
                    throw new NullReferenceException("new syncbox path cannot be null");
                }

                // initializing the database may create the database starting at the newSyncboxPath so no setting is required;
                // otherwise, still need to set the root
                if (!InitializeDatabase(newSyncboxPath))
                {
                    using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                    {
                        using (ISQLiteCommand changeRoot = indexDB.CreateCommand())
                        {
                            const string updateFileSystemObjectByRoot = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAF2IoquE2G2pJ4mXiQt79qfbhdjuiJyDE49icAMJwKlcbipzqc2Hv5y+rBvw93KEqImivPzDQwqoOfMCTlDoZ9IITY7VCmJOPqBlzJBJdQpRuIAEtZqhcLUr0HgnEuYW9T";

                            changeRoot.CommandText =
                                //// before
                                //
                                //"UPDATE FileSystemObjects " +
                                //"SET Name = ? " + // <-- parameter 1
                                //"WHERE ParentFolderId IS NULL" // condition for root folder object
                                //
                                //// after (decrypted)
                                //
                                //UPDATE FileSystemObjects
                                //SET Name = ?
                                //WHERE ParentFolderId IS NULL
                                Helpers.DecryptString(
                                    updateFileSystemObjectByRoot,
                                    Encoding.ASCII.GetString(
                                        Convert.FromBase64String(indexDBPassword)));

                            ISQLiteParameter newRootParam = changeRoot.CreateParameter();
                            newRootParam.Value = newSyncboxPath;
                            changeRoot.Parameters.Add(newRootParam);

                            changeRoot.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region private methods

        /// <summary>
        /// Private constructor to ensure IndexingAgent is created through public static initializer (to return a CLError)
        /// </summary>
        /// <param name="syncbox">Syncbox to index</param>
        private IndexingAgent(CLSyncbox syncbox, bool copyDatabaseBetweenChanges)
        {
            if (syncbox == null)
            {
                throw new NullReferenceException(Resources.SyncboxMustNotBeNull);
            }
            if (string.IsNullOrEmpty(syncbox.CopiedSettings.DeviceId))
            {
                throw new NullReferenceException(Resources.CLHttpRestDeviceIDCannotBeNull);
            }

            this.indexDBLocation = Helpers.CalculateDatabasePath(syncbox);

            this.syncbox = syncbox;
            this.copyDatabaseBetweenChanges = copyDatabaseBetweenChanges;
        }

        private bool InitializeDatabase(string syncRoot, bool createEvenIfExisting = false)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    return true; // logically it makes sense to return false, but the only place the return value is used will actually perform more code if false so instead return true
                }
            }

            FileInfo dbInfo;
            bool dbNeedsDeletion;
            bool dbNeedsCreation;
            string notUsedExistingFullPath;

            CheckDatabaseFileState(createEvenIfExisting, out dbInfo, out dbNeedsDeletion, out dbNeedsCreation, indexDBLocation, out notUsedExistingFullPath, syncbox, out rootFileSystemObjectId, out rootFileSystemObjectServerUidId);

            if (dbNeedsDeletion)
            {
                dbInfo.Delete();
            }

            if (dbNeedsCreation)
            {
                FileInfo indexDBInfo = new FileInfo(indexDBLocation);
                if (!indexDBInfo.Directory.Exists)
                {
                    indexDBInfo.Directory.Create();
                }

                using (ISQLiteConnection newDBConnection = CreateAndOpenCipherConnection(enforceForeignKeyConstraints: false)) // circular reference between Events and FileSystemObjects tables
                {
                    // read creation scripts in here

                    System.Reflection.Assembly indexingAssembly = System.Reflection.Assembly.GetAssembly(typeof(IndexingAgent));

                    List<KeyValuePair<int, string>> indexDBScripts = new List<KeyValuePair<int, string>>();

                    string scriptDirectory = indexingAssembly.GetName().Name + indexScriptsResourceFolder;

                    Encoding ansiEncoding = Encoding.GetEncoding(1252); //ANSI saved from NotePad on a US-EN Windows machine

                    foreach (string currentScriptName in indexingAssembly.GetManifestResourceNames()
                        .Where(resourceName => resourceName.StartsWith(scriptDirectory)))
                    {
                        if (!string.IsNullOrWhiteSpace(currentScriptName)
                            && currentScriptName.Length >= 5 // length of 1+-digit number plus Resources.NotTranslatedSqlIndexerSqlExtension file extension
                            && currentScriptName.EndsWith(Resources.NotTranslatedSqlIndexerSqlExtension, StringComparison.InvariantCultureIgnoreCase))
                        {
                            int numChars = 0;
                            for (int numberCharIndex = scriptDirectory.Length; numberCharIndex < currentScriptName.Length; numberCharIndex++)
                            {
                                if (!char.IsDigit(currentScriptName[numberCharIndex]))
                                {
                                    numChars = numberCharIndex - scriptDirectory.Length;
                                    break;
                                }
                            }
                            if (numChars > 0)
                            {
                                string nameNumberPortion = currentScriptName.Substring(scriptDirectory.Length, numChars);
                                int nameNumber;
                                if (int.TryParse(nameNumberPortion, out nameNumber))
                                {
                                    using (Stream resourceStream = indexingAssembly.GetManifestResourceStream(currentScriptName))
                                    {
                                        using (StreamReader resourceReader = new StreamReader(resourceStream, ansiEncoding))
                                        {
                                            indexDBScripts.Add(new KeyValuePair<int, string>(nameNumber, resourceReader.ReadToEnd()));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    using (ISQLiteConnection creationConnection = CreateAndOpenCipherConnection(enforceForeignKeyConstraints: false)) // do not enforce constraints since part of the creation scripts are to create two tables which foreign key reference each other
                    {
                        // special enumerator processing so we can inject an operation immediately before processing the last script:
                        // we need to add the root FileSystemObject before updating the user versions via PRAGMA (which should be the last SQL script)
                        string storeLastScript = null;
                        using (IEnumerator<string> insertEnumerator = indexDBScripts.OrderBy(scriptPair => scriptPair.Key)
                            .Select(scriptPair => scriptPair.Value)
                            .GetEnumerator())
                        {
                            bool lastInsert;
                            while (!(lastInsert = !insertEnumerator.MoveNext()) || storeLastScript != null)
                            {
                                if (storeLastScript != null)
                                {
                                    if (lastInsert)
                                    {
                                        CLError createRootServerUid = CreateNewServerUid(serverUid: null, revision: null, serverUidId: out rootFileSystemObjectServerUidId);

                                        if (createRootServerUid != null)
                                        {
                                            throw new AggregateException("Unable to create ServerUid", createRootServerUid.Exceptions);
                                        }

                                        rootFileSystemObjectId = SqlAccessor<FileSystemObject>.InsertRow<long>
                                            (creationConnection,
                                                new FileSystemObject()
                                                {
                                                    EventTimeUTCTicks = 0, // never need to show the root folder in recents, so it should have the oldest event time
                                                    IsFolder = true,
                                                    Name = syncRoot,
                                                    NameCIHash = StringComparer.OrdinalIgnoreCase.GetHashCode(syncRoot),
                                                    Pending = false,
                                                    ServerUidId = rootFileSystemObjectServerUidId
                                                });
                                    }

                                    using (ISQLiteCommand scriptCommand = creationConnection.CreateCommand())
                                    {
                                        scriptCommand.CommandText = Helpers.DecryptString(storeLastScript,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword)));
                                        scriptCommand.ExecuteNonQuery();
                                    }
                                }

                                storeLastScript = (lastInsert
                                    ? null
                                    : insertEnumerator.Current);
                            }
                        }
                    }
                }
            }

            lock (changeEnumsLocker)
            {
                if (changeEnums == null)
                {
                    try
                    {
                        int changeEnumsCount = System.Enum.GetNames(typeof(FileChangeType)).Length;
                        changeEnums = new Dictionary<long, FileChangeType>(changeEnumsCount);
                        changeEnumsBackward = new Dictionary<FileChangeType, long>(changeEnumsCount);

                        using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
                        {
                            long storeCategoryId = -1;

                            const string selectEnumCategoryForFileChangeType = "t3Ee1ulQLjs62aHw5E7nEJ4OVRMxHSv05KKk8yteDDLqGGMwOjLNITW5GZUQs9PX7chtt7prSw/TgDJnhFowwiGZph64jP+b4f+mvSNtb79WBikCDdlN4FcpEA1YqN5J";

                            foreach (EnumCategory currentCategory in SqlAccessor<EnumCategory>
                                .SelectResultSet(indexDB,
                                    //// before
                                    //
                                    //"SELECT * FROM EnumCategories WHERE Name = '" + typeof(FileChangeType).Name.Replace(((char)0x27 /* '\'' */).ToString(), new string((char)0x27 /* '\'' */, 2)) + "'"
                                    //
                                    //// after (decrypted; {0}: typeof(FileChangeType).Name.Replace(((char)0x27 /* '\'' */).ToString(), new string((char)0x27 /* '\'' */, 2)) )
                                    //
                                    //SELECT * FROM EnumCategories WHERE Name = '{0}'
                                    string.Format(
                                        Helpers.DecryptString(
                                            selectEnumCategoryForFileChangeType,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword))),
                                        typeof(FileChangeType).Name.Replace(((char)0x27 /* '\'' */).ToString(), new string((char)0x27 /* '\'' */, 2)))))
                            {
                                if (storeCategoryId == -1)
                                {
                                    storeCategoryId = currentCategory.EnumCategoryId;
                                }
                                else
                                {
                                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "More than one type with name FileChangeType found");
                                }
                            }

                            if (storeCategoryId == -1)
                            {
                                throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "No EnumCategory found with name FileChangeType");
                            }

                            const string selectEnumByCategoryId = "t3Ee1ulQLjs62aHw5E7nEJ4OVRMxHSv05KKk8yteDDKPDczN41IFJrTTT/8+SbVUsPZDoZybLGstb13Eo8o/f9PulMXMsbcT40Gd9kvjsqVkTHd09OM8L3VbKsrJVsIj2bt6sri9CJiCGLxwBNEZIQ==";

                            foreach (SqlEnum currentChangeEnum in SqlAccessor<SqlEnum>
                                .SelectResultSet(indexDB,
                                    //// before
                                    //
                                    //"SELECT * FROM Enums WHERE Enums.EnumCategoryId = " + storeCategoryId
                                    //
                                    //// after (decrypted; {0}: storeCategoryId)
                                    //
                                    //SELECT * FROM Enums WHERE Enums.EnumCategoryId = {0}
                                    string.Format(
                                        Helpers.DecryptString(
                                            selectEnumByCategoryId,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword))),
                                        storeCategoryId)))
                            {
                                changeCategoryId = currentChangeEnum.EnumCategoryId;
                                long forwardKey = currentChangeEnum.EnumId;

                                FileChangeType forwardValue;
                                if (!System.Enum.TryParse<FileChangeType>(currentChangeEnum.Name, out forwardValue))
                                {
                                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Name of Enum for FileChangeType EnumCategory does not parse as a FileChangeType");
                                }

                                changeEnums.Add(forwardKey,
                                    forwardValue);
                                changeEnumsBackward.Add(forwardValue,
                                    forwardKey);
                            }
                        }

                        if (changeEnums.Count != changeEnumsCount)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "FileChangeType enumerated values do not match count with names in the database");
                        }
                    }
                    catch
                    {
                        changeEnums = null; // used as condition to rebuild the static dictionaries

                        throw;
                    }
                }

                if (copyDatabaseBetweenChanges)
                {
                    string indexLocationWithoutExtension = indexDBLocation.Substring(0, indexDBLocation.LastIndexOf((char)0x2e /* '.' */));
                    int highestExistingCopyIndex = 2;
                    while (File.Exists(string.Format(Resources.NotTranslatedSqlIndexerDBCopiedName, indexLocationWithoutExtension, highestExistingCopyIndex)))
                    {
                        highestExistingCopyIndex++;
                    }

                    // no need to lock since initialization must be atomic with initial database operations
                    dbCopyNumber.Value = highestExistingCopyIndex;
                }
            }

            return dbNeedsCreation;
        }

        public static void CheckDatabaseFileState(bool createEvenIfExisting, out FileInfo dbInfo, out bool dbNeedsDeletion, out bool dbNeedsCreation, string indexDBLocation, out string rootObjectCalculatedFullPath)
        {
            long doNotUse;
            CheckDatabaseFileState(createEvenIfExisting, out dbInfo, out dbNeedsDeletion, out dbNeedsCreation, indexDBLocation, out rootObjectCalculatedFullPath, syncbox: null, rootObjectId: out doNotUse, rootObjectServerUidId: out doNotUse);
        }

        private static void CheckDatabaseFileState(bool createEvenIfExisting, out FileInfo dbInfo, out bool dbNeedsDeletion, out bool dbNeedsCreation, string indexDBLocation, out string rootObjectCalculatedFullPath, CLSyncbox syncbox, out long rootObjectId, out long rootObjectServerUidId)
        {
            dbInfo = new FileInfo(indexDBLocation);

            if (dbInfo.Exists)
            {
                if (createEvenIfExisting)
                {
                    rootObjectCalculatedFullPath = Helpers.DefaultForType<string>();
                    rootObjectId = Helpers.DefaultForType<long>();
                    rootObjectServerUidId = Helpers.DefaultForType<long>();

                    dbNeedsDeletion = true;
                    dbNeedsCreation = true;
                }
                else
                {
                    try
                    {
                        using (ISQLiteConnection verifyAndUpdateConnection = StaticCreateAndOpenCipherConnection(enforceForeignKeyConstraints: true, indexDBLocation: indexDBLocation))
                        {
                            CheckIntegrity(verifyAndUpdateConnection);

                            int existingVersion;

                            using (ISQLiteCommand getVersionCommand = verifyAndUpdateConnection.CreateCommand())
                            {
                                const string pragmaUserVersion = "dVUjkeheWiZPNrSaG7rHi7OyvtsVcYtTcI35poPgL5vRNOngfRudvZB0QP8OQZ6W";

                                getVersionCommand.CommandText =
                                    //// before
                                    //
                                    //"PRAGMA user_version;"
                                    //
                                    //// after (decrypted)
                                    //
                                    //PRAGMA user_version;
                                    Helpers.DecryptString(
                                        pragmaUserVersion,
                                        Encoding.ASCII.GetString(
                                            Convert.FromBase64String(indexDBPassword)));

                                existingVersion = Convert.ToInt32(getVersionCommand.ExecuteScalar());
                            }

                            if (existingVersion < 2)
                            {
                                // database was never finalized (version is changed from 1 to [current database version] via the last initialization script, which identifies successful creation)
                                // the very first implementation of this database will be version 2 so we can compare on less than 2

                                rootObjectCalculatedFullPath = Helpers.DefaultForType<string>();
                                rootObjectId = Helpers.DefaultForType<long>();
                                rootObjectServerUidId = Helpers.DefaultForType<long>();

                                dbNeedsCreation = true;
                                dbNeedsDeletion = true;
                            }
                            else
                            {
                                int newVersion = -1;

                                foreach (KeyValuePair<int, IMigration> currentDBMigration in MigrationList.GetMigrationsAfterVersion(existingVersion))
                                {
                                    currentDBMigration.Value.Apply(
                                        verifyAndUpdateConnection,
                                        indexDBPassword);

                                    newVersion = currentDBMigration.Key;
                                }

                                if (newVersion > existingVersion)
                                {
                                    using (ISQLiteCommand updateVersionCommand = verifyAndUpdateConnection.CreateCommand())
                                    {
                                        const string updatePragmaUserVersion = "dVUjkeheWiZPNrSaG7rHi7OyvtsVcYtTcI35poPgL5uZ8NDxTazLyTLGOX19X8I7LmCmaEtIOYSzDP/t8rX+Vw==";

                                        updateVersionCommand.CommandText =
                                            //// before
                                            //
                                            //"PRAGMA user_version = " + newVersion
                                            //
                                            //// after (decrypted; {0}: newVersion)
                                            //
                                            //PRAGMA user_version = {0}
                                            string.Format(
                                                Helpers.DecryptString(
                                                    updatePragmaUserVersion,
                                                    Encoding.ASCII.GetString(
                                                        Convert.FromBase64String(indexDBPassword))),
                                                newVersion);

                                        updateVersionCommand.ExecuteNonQuery();
                                    }
                                }

                                const string selectRootObject = "t3Ee1ulQLjs62aHw5E7nEHC3Yt6pkKQiMjDOMA00p+Qo1PZHGpfRx91FJNSloGZ3xDH11QktFYyaPHTl7mAN/QkLD0PnpC8sDmmRC3eIdnNwEv6VbgcYJMh2e8FkOh6pkTk+wvxmCRAw6xSk4LnkkIBkbxHbXULufC0P8A7txr+KYvXKLpwm5BBu8bvcAoOUm2ktCSGamUmo73Gmpd/w6ZmrmHM9l5akpRPajdsJpThd0gIFR+xaNdwqm5boi5Op";

                                FileSystemObject rootObject = SqlAccessor<FileSystemObject>.SelectResultSet(
                                        verifyAndUpdateConnection,
                                        //// before
                                        //
                                        //"SELECT * " +
                                        //"FROM FileSystemObjects " +
                                        //"WHERE FileSystemObjects.ParentFolderId IS NULL " +
                                        //"LIMIT 1"
                                        //
                                        //// after (decrypted)
                                        //
                                        //SELECT *
                                        //FROM FileSystemObjects
                                        //WHERE FileSystemObjects.ParentFolderId IS NULL
                                        //LIMIT 1
                                        Helpers.DecryptString(
                                            selectRootObject,
                                            Encoding.ASCII.GetString(
                                                Convert.FromBase64String(indexDBPassword))))
                                    .FirstOrDefault();

                                if (rootObject == null)
                                {
                                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find FileSystemObjects row for root object");
                                }

                                rootObjectCalculatedFullPath = rootObject.CalculatedFullPath;
                                rootObjectId = rootObject.FileSystemObjectId;
                                rootObjectServerUidId = rootObject.ServerUidId;

                                dbNeedsCreation = false;
                                dbNeedsDeletion = false;
                            }
                        }
                    }
                    catch (SQLiteExceptionBase ex)
                    {
                        if (syncbox != null)
                        {
                            // notify database replaced due to corruption
                            MessageEvents.FireNewEventMessage(
                                "Database corruption found on initializing index. Replacing database with a fresh one. Files and folders changed while offline will be grabbed again from server. Error message: " + ex.Message,
                                EventMessageLevel.Important,
                                new GeneralErrorInfo(),
                                syncbox,
                                syncbox.CopiedSettings.DeviceId);
                        }

                        rootObjectCalculatedFullPath = Helpers.DefaultForType<string>();
                        rootObjectId = Helpers.DefaultForType<long>();
                        rootObjectServerUidId = Helpers.DefaultForType<long>();

                        dbNeedsDeletion = true;
                        dbNeedsCreation = true;
                    }
                }
            }
            else
            {
                if (syncbox != null)
                {
                    MessageEvents.FireNewEventMessage(
                        "Existing database not found, possibly due to new SyncboxId\\DeviceId combination. Starting fresh.",
                        EventMessageLevel.Minor,
                        Syncbox: syncbox,
                        DeviceId: syncbox.CopiedSettings.DeviceId);
                }

                rootObjectCalculatedFullPath = Helpers.DefaultForType<string>();
                rootObjectId = Helpers.DefaultForType<long>();
                rootObjectServerUidId = Helpers.DefaultForType<long>();

                dbNeedsCreation = true;
                dbNeedsDeletion = false;
            }
        }

        private static bool CheckForPendingAtNameAndParent(SQLTransactionalImplementation castTransaction, Nullable<long> parentFolderId, string name, Nullable<int> nameCIHash = null)
        {
            using (ISQLiteCommand pendingEventsCommand = castTransaction.sqlConnection.CreateCommand())
            {
                pendingEventsCommand.Transaction = castTransaction.sqlTransaction;

                const string selectWhetherFileSystemObjectIsPendingByNameHash = "AgH+ETlwi8JFbGkqRXjqdKTh9CEhp0rFMSM6QTpmaYxrMgdgf7inZdXwgEuqXuYM7yDvB56f0l5Zp7IP4IxcphqihonQ8M4bjAlSZUpcSIgzICNmKDgdlRSpbD+R0xj7um6pmvl+sEKpMp8GK8mlF9fIl8LMc2Stuut4SUWk9J3OZz2OgxuK+PFE/VYQVHRpeZ3jGnQGCMRgmCKXv9W8An+89Utxh+4hE5X3x2Qctbm8UJP37S56dlDcaIEMv8N+I5HYdqsSYaIWa7PzWHqB51fQg/kD9biEhbdHbICSfAkeSjgVRqxr+wWDtMZjaOyO8Vt8k5hLEGwfCVkmkN9C4BQhfllUvKiWr0vXm+IT3vs=";
                const string conditionalWhereParentIdNull = "O2ELBOgTDqHwNV57vVDPJfPCQkbaoRhLwWcdhqd/+6tSgv3f9QYjjlzGhCCs7EiXwiRcZkyvLX7gsE6Sn1ACm8p0QmC41RzZbAuTFTVsk9+gHgLvgm3ouuQaJkkjhgPm";
                const string conditionalWhereParentIdValue = "O2ELBOgTDqHwNV57vVDPJfPCQkbaoRhLwWcdhqd/+6tSgv3f9QYjjlzGhCCs7EiXwiRcZkyvLX7gsE6Sn1ACm/olYedr4HnrWA/h/NNEAB/WxtKjvzBljTkNQEfnFUvd";

                pendingEventsCommand.CommandText =
                    //// before
                    //
                    //// selectWhetherFileSystemObjectIsPendingByNameHash
                    //
                    //"SELECT FileSystemObjects.Name " +
                    //    "FROM FileSystemObjects " +
                    //    "WHERE FileSystemObjects.NameCIHash = ? " + // <-- parameter 1
                    //    (parentFolderId == null
                    //        ? conditionalWhereParentIdNull
                    //        : conditionalWhereParentIdValue) + // <-- conditional parameter 2
                    //    " AND FileSystemObjects.Pending = 1"
                    //
                    //// conditionalWhereParentIdNull
                    //
                    //"AND FileSystemObjects.ParentFolderId IS NULL"
                    //
                    //// conditionalWhereParentIdValue
                    //
                    //"AND FileSystemObjects.ParentFolderId = ?"
                    //
                    //// after (decrypted; {0}: (parentFolderId == null ? conditionalWhereParentIdNull [decrypted] : conditionalWhereParentIdValue [decrypted]) )
                    //
                    //// selectWhetherFileSystemObjectIsPendingByNameHash
                    //
                    //SELECT FileSystemObjects.Name
                    //FROM FileSystemObjects
                    //WHERE FileSystemObjects.NameCIHash = ?
                    //{0}
                    //AND FileSystemObjects.Pending = 1
                    //
                    //// conditionalWhereParentIdNull
                    //
                    //AND FileSystemObjects.ParentFolderId IS NULL
                    //
                    //// conditionalWhereParentIdValue
                    //
                    //AND FileSystemObjects.ParentFolderId = ?
                    string.Format(
                        Helpers.DecryptString(
                            selectWhetherFileSystemObjectIsPendingByNameHash,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))),
                        (parentFolderId == null
                            ? Helpers.DecryptString(
                                conditionalWhereParentIdNull,
                                Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword)))
                            : Helpers.DecryptString(
                                conditionalWhereParentIdValue,
                                Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword)))));

                ISQLiteParameter existingNameCIHashParam = pendingEventsCommand.CreateParameter();
                existingNameCIHashParam.Value = nameCIHash ?? StringComparer.OrdinalIgnoreCase.GetHashCode(name);
                pendingEventsCommand.Parameters.Add(existingNameCIHashParam);

                if (parentFolderId != null)
                {
                    ISQLiteParameter existingParentParam = pendingEventsCommand.CreateParameter();
                    existingParentParam.Value = (long)parentFolderId;
                    pendingEventsCommand.Parameters.Add(existingParentParam);
                }

                using (ISQLiteDataReader pendingEventsReader = pendingEventsCommand.ExecuteReader(CommandBehavior.SingleResult))
                {
                    while (pendingEventsReader.Read())
                    {
                        if (StringComparer.OrdinalIgnoreCase.Equals(Convert.ToString(pendingEventsReader[Resources.NotTranslatedSqlIndexerName]), name))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void MarkBadgeSyncedAfterEventCompletion(FileChangeType storeExistingChangeType, string storeNewPath, string storeOldPath, bool storeWhetherEventIsASyncFrom)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    return;
                }
            }

            Action<FilePath> setBadgeSynced = syncedPath =>
            {
                MessageEvents.QueueSetBadge(this, new SetBadge(PathState.Synced, syncedPath));   // Message to invoke BadgeNet.IconOverlay.QueueSetBadge(PathState.Synced, syncedPath);
            };

            // Adjust the badge for this completed event.
            switch (storeExistingChangeType)
            {
                case FileChangeType.Created:
                case FileChangeType.Modified:
                    setBadgeSynced(storeNewPath);
                    break;
                case FileChangeType.Deleted:
                    bool isDeleted;
                    MessageEvents.DeleteBadgePath(this, new DeleteBadgePath(storeNewPath), out isDeleted);   // Message to invoke BadgeNet.IconOverlay.DeleteBadgePath(currentEvent.FileSystemObject.Path, out isDeleted);
                    break;
                case FileChangeType.Renamed:
                    if (storeWhetherEventIsASyncFrom)
                    {
                        MessageEvents.RenameBadgePath(this, new RenameBadgePath(storeOldPath, storeNewPath));   // Message to invoke BadgeNet.IconOverlay.RenameBadgePath(currentEvent.PreviousPath, currentEvent.FileSystemObject.Path);
                    }

                    setBadgeSynced(storeNewPath);
                    break;
            }
        }

        private static void CheckIntegrity(ISQLiteConnection conn)
        {
            using (ISQLiteCommand integrityCheckCommand = conn.CreateCommand())
            {
                const string pragmaIntegrityCheck = "J9U+678j+ydvhtUjalI5pWNQYrflpMTHY/Ioy77zQez6LlyAnE8f0RAgdJDbuaAtB+DaOSzzppV5jCJReHkKIg==";

                // it's possible integrity_check could be replaced with quick_check for faster performance if it doesn't risk missing any corruption
                integrityCheckCommand.CommandText =
                    //// before
                    //
                    //"PRAGMA integrity_check(1);" // we don't output all the corruption results, only need to grab 1
                    //
                    //// after (decrypted)
                    //
                    //PRAGMA integrity_check(1);
                    Helpers.DecryptString(
                        pragmaIntegrityCheck,
                        Encoding.ASCII.GetString(
                            Convert.FromBase64String(indexDBPassword)));

                using (ISQLiteDataReader integrityReader = integrityCheckCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                {
                    if (integrityReader.Read())
                    {
                        int integrityCheckColumnOrdinal = integrityReader.GetOrdinal(Resources.NotTranslatedSqlIndexerIntegrityCheck);
                        if (integrityCheckColumnOrdinal == -1)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Result from integrity_check does not contain integrity_check column");
                        }

                        if (integrityReader.IsDBNull(integrityCheckColumnOrdinal))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "First result from integrity_check contains a null value");
                        }

                        string integrityCheckValue;
                        try
                        {
                            integrityCheckValue = Convert.ToString(integrityReader[Resources.NotTranslatedSqlIndexerIntegrityCheck]);
                        }
                        catch
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Value of first result from integrity_check is not convertable to String");
                        }

                        if (integrityCheckValue != "ok")
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Value of first result from integrity_check indicates failure. Message: " +
                                (string.IsNullOrWhiteSpace(integrityCheckValue) ? "{empty}" : integrityCheckValue));
                        }
                    }
                    else
                    {
                        throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Corrupt, "Unable to read result of integrity_check");
                    }
                }
            }
        }

        private ISQLiteConnection CreateAndOpenCipherConnection(bool enforceForeignKeyConstraints = true)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    return null;
                }
            }

            return StaticCreateAndOpenCipherConnection(enforceForeignKeyConstraints, indexDBLocation);
        }

        private static ISQLiteConnection StaticCreateAndOpenCipherConnection(bool enforceForeignKeyConstraints, string indexDBLocation)
        {
            const string CipherConnectionString = "cMGxaRQ9SkHpCFyvjSbF7Toe5tWrH6Y3w8+OqfAUP10JOZ8GhYldUniwo4S6JRoV90e2+kaVm6s3KFTa47y4u91luQTEw15UGH2XrDDGJwIPFmTwVxTAYc+4fhKngxcRSjgb5sUyl+dggY20EfmLnMB8RL9yI8+wWBfzS0qWj2Z4zmuUveHcPqCL5uHiukihAlG37/fD5mOL9NL6qZXyTVpguHpbBBNaIC27vu9cu9Z2Judle1nGAgJn/+EtMkSL+Y4vCR2yS9/Nm9t20b5yTH73ztFKsyl8hCOQDrXfmFo=";

            ISQLiteConnection cipherConn = SQLConstructors.SQLiteConnection(
                string.Format(
                    //// before
                    //
                    //"Data Source=\"{0}\";Pooling=false;Synchronous=Full;UTF8Encoding=True;Foreign Keys={1};Default Timeout=3000"
                    //
                    //// after (decrypted)
                    //
                    //Data Source="{0}";Pooling=false;Synchronous=Full;UTF8Encoding=True;Foreign Keys={1};Default Timeout=3000
                    Helpers.DecryptString(
                        CipherConnectionString,
                        Encoding.ASCII.GetString(
                            Convert.FromBase64String(indexDBPassword))),
                    indexDBLocation,
                    enforceForeignKeyConstraints.ToString()));

            try
            {
                cipherConn.SetPassword(
                    Encoding.ASCII.GetString(
                        Convert.FromBase64String(indexDBPassword)));

                cipherConn.Open();

                return cipherConn;
            }
            catch
            {
                cipherConn.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Action fired on a user worker thread which traverses the root path to build an initial index on application startup
        /// </summary>
        /// <param name="indexCompletionCallback">Callback should be the BeginProcessing method of the FileMonitor to forward the initial index</param>
        private void BuildIndex(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    return;
                }
            }

            FilePath baseComparePath = indexedPath;

            // Create the initial index dictionary, throwing any exceptions that occurred in the process
            FilePathDictionary<FileMetadata> indexPaths;
            CLError indexPathCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(baseComparePath,
                out indexPaths);
            if (indexPathCreationError != null)
            {
                throw indexPathCreationError.PrimaryException;
            }

            FilePathDictionary<FileMetadata> combinedIndexPlusChanges;
            CLError combinedIndexCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(baseComparePath,
                out combinedIndexPlusChanges);
            if (combinedIndexCreationError != null)
            {
                throw combinedIndexCreationError.PrimaryException;
            }

            FilePathDictionary<GenericHolder<bool>> pathDeletions;
            CLError pathDeletionsCreationError = FilePathDictionary<GenericHolder<bool>>.CreateAndInitialize(baseComparePath,
                out pathDeletions);
            if (pathDeletionsCreationError != null)
            {
                throw pathDeletionsCreationError.PrimaryException;
            }

            using (ISQLiteConnection indexDB = CreateAndOpenCipherConnection())
            {
                const string selectHighestSync = "t3Ee1ulQLjs62aHw5E7nEFP/XcNQUnMlRSmbt+hjuY9j+ruu1iqJ4alK3hQ0jevAIe2VSL4E2w/8VkHGfnQlPMTGCvol1kdohe3dApqAER8e3MsgNTqsh4JeOvsq1DLx4wobMs04CvKFRcnQM7bOTLICfya3G53laxKL5G0cnE0=";

                // Grab the most recent sync from the database to pull sync states
                SqlSync lastSync = SqlAccessor<SqlSync>.SelectResultSet(indexDB,
                    //// before
                    //
                    //"SELECT * FROM Syncs ORDER BY Syncs.SyncCounter DESC LIMIT 1"
                    //
                    //// after (decrypted)
                    //
                    //SELECT * FROM Syncs ORDER BY Syncs.SyncCounter DESC LIMIT 1
                    Helpers.DecryptString(
                        selectHighestSync,
                        Encoding.ASCII.GetString(
                            Convert.FromBase64String(indexDBPassword))))
                    .SingleOrDefault();
                // Store the sync counter from the last sync, defaulting to null
                Nullable<long> lastSyncCounter = (lastSync == null
                    ? (Nullable<long>)null
                    : lastSync.SyncCounter);

                // Update the exposed last sync id string under a lock
                LastSyncLocker.EnterWriteLock();
                try
                {
                    this.LastSyncId = (lastSync == null
                        ? null
                        : lastSync.SID);
                }
                finally
                {
                    LastSyncLocker.ExitWriteLock();
                }

                Dictionary<long, string> objectIdsToFullPath = new Dictionary<long, string>();
                SortedDictionary<KeyValuePair<bool, long>, FileSystemObject> sortedFileSystemObjects = new SortedDictionary<KeyValuePair<bool, long>, FileSystemObject>(pendingThenIdComparer.Instance);
                long missingOrderAppend = 0;

                const string selectAllFileSystemObjects = "XiF/n8DAmECRcpl1q3g5SOaFkrEO/c+iI1V66stCO9Yv7K3QnlXqj68P0vDvLKPEvsMAT4aYICpfeh5DRpnmvL5qfP/ZUoDajTeFKO752ECwpnxQ1+SCCaqYKnwzeo/W5zqNKJoarqJ7Ykx2KEJCxMsQaMIsLVdd6b7fCvGImiHvrT1P6h6WqhmdCN+kRN88565+TGDJMAEH2GSck84CzbgfHewvatq4KJ61CYL9I9oCI8iDog94ucymzcnoaLjGGQGL55YSImeP1E0jFhh6akovPplqGUVSFzuCZD5jVcCAin8IP1IUJBI6T2FqPtifjIphBfIOwQkPE9q2NfVXhtSc9ftVfxNeuGVQZHpKmee6I1SYRaciRzloKOKEJHtX0ToGMFy4gwMGzjl2jnIGsUQglASQSkOg8+PogitJWXtelu9wA862oC3JrWUEbHeTaeghBu0Dt8Vt2mOM2Sssk5QQ9Ol++V5WKgv6iKjg2z5ZGjrruCtgNXAtnLNqSapSDk98gSWnH/i8G0d89/4xEk7ALAcN/lTJFL/NF3GmQn/Ohq+chmX31+GDK7Paqqx2G7Te99wrXlYcKXOsdTvQ6EMtOSDJQX2BuT3UdF5SDvuOXgpckZNaWB7R4qFXmuLOLZ6wBDi4mXCG1dg27HTnI7r6FHPutWAF75Ty+MxyD6WJp82+5u6ftOenlpZqb6z6UMHm8O+/rt9V9gQ8ooyrroXlcJqmAGKEeV9TYyJVNE4Q7aAzvGqRwp5sdrzMHyyBOBFCpk8qcY7urn22ICIYNLiYC+xcQqgU+qw9yG2VApactu1aBTKWX/ggA4M5cyIuouksUZdb7xrYb+Kt+BQ6KR+sFMYXWwVmQqMSrjTEem5wP0u0l3UUQRMqjpRj1YOPUgrYwVl7UBOJYjU+QTPhUw84wmCYyzD5VE0n3q0H9rNcuiDJABzHpIDEz9ImEtttoCpUVwuOHZEI26WFbKRW76jvK87oUlknTq6kKpQbL0nzfrVuh00fKDYs0626tDO2t8czjXDT3gc2EgoQrvRW8Q==";

                foreach (FileSystemObject combinedPendingNonPending in SqlAccessor<FileSystemObject>.SelectResultSet(
                    indexDB,
                    //// before
                    //
                    //"SELECT " +
                    //    SqlAccessor<FileSystemObject>.GetSelectColumns() + ", " +
                    //    SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent) + ", " +
                    //    SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerParent, Resources.NotTranslatedSqlIndexer) + ", " +
                    //    SqlAccessor<SqlServerUid>.GetSelectColumns(Resources.NotTranslatedSqlIndexerParentServerUid) +
                    //    " FROM FileSystemObjects" +
                    //    " LEFT OUTER JOIN Events ON " +
                    //    "(" +
                    //    "  FileSystemObjects.EventId = Events.EventId" +
                    //    "  AND FileSystemObjects.Pending = 1" +
                    //    ")" +
                    //    " LEFT OUTER JOIN FileSystemObjects Parents ON " +
                    //    "(" +
                    //    "  FileSystemObjects.ParentFolderId = Parents.FileSystemObjectId" +
                    //    "  AND FileSystemObjects.Pending = 1" +
                    //    ")" +
                    //    " LEFT OUTER JOIN ServerUids ON Parents.ServerUidId = ServerUids.ServerUidId"
                    //
                    //// after (decrypted; {0}: SqlAccessor<FileSystemObject>.GetSelectColumns()
                    //// {1}: SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent)
                    //// {2}: SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerParent, Resources.NotTranslatedSqlIndexer)
                    //// {3}: SqlAccessor<SqlServerUid>.GetSelectColumns(Resources.NotTranslatedSqlIndexerParentServerUid) )
                    //
                    //SELECT
                    //{0},
                    //{1},
                    //{2},
                    //{3}
                    //FROM FileSystemObjects
                    //LEFT OUTER JOIN Events ON
                    //(
                    //FileSystemObjects.EventId = Events.EventId
                    //AND FileSystemObjects.Pending = 1
                    //)
                    //LEFT OUTER JOIN FileSystemObjects Parents ON
                    //(
                    //FileSystemObjects.ParentFolderId = Parents.FileSystemObjectId
                    //AND FileSystemObjects.Pending = 1
                    //)
                    //LEFT OUTER JOIN ServerUids ON Parents.ServerUidId = ServerUids.ServerUidId
                    string.Format(
                        Helpers.DecryptString(
                            selectAllFileSystemObjects,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))),
                        SqlAccessor<FileSystemObject>.GetSelectColumns(),
                        SqlAccessor<Event>.GetSelectColumns(Resources.NotTranslatedSqlIndexerEvent),
                        SqlAccessor<FileSystemObject>.GetSelectColumns(Resources.NotTranslatedSqlIndexerParent, Resources.NotTranslatedSqlIndexerParents),
                        SqlAccessor<SqlServerUid>.GetSelectColumns(Resources.NotTranslatedSqlIndexerParentServerUid)),
                    includes: new[] { Resources.NotTranslatedSqlIndexerEvent, Resources.NotTranslatedSqlIndexerParent, Resources.NotTranslatedSqlIndexerParentServerUid }))
                {
                    if (combinedPendingNonPending.ParentFolderId == null)
                    {
                        // set the root metadata
                        combinedIndexPlusChanges[null] = indexPaths[null] = new FileMetadata(combinedPendingNonPending.ServerUidId)
                        {
                            EventTime = new DateTime(0, DateTimeKind.Utc),
                            HashableProperties = new FileMetadataHashableProperties(
                                isFolder: true,
                                lastTime: null,
                                creationTime: null,
                                size: null)
                        };
                    }
                    else
                    {
                        objectIdsToFullPath.Add(combinedPendingNonPending.FileSystemObjectId, combinedPendingNonPending.CalculatedFullPath);
                        sortedFileSystemObjects.Add(
                            new KeyValuePair<bool, long>(
                                combinedPendingNonPending.Pending,
                                combinedPendingNonPending.EventOrder ?? ((missingOrderAppend++) + Int64.MinValue)),
                            combinedPendingNonPending);
                    }
                }

                List<FileChange> pendingsWithSyncCounter = new List<FileChange>();
                List<FileChange> pendingsWithoutSyncCounter = new List<FileChange>();

                foreach (KeyValuePair<KeyValuePair<bool, long>, FileSystemObject> currentObject in sortedFileSystemObjects)
                {
                    if (currentObject.Key.Key) // true for pending
                    {
                        FileChange currentChange = new FileChange()
                        {
                            Direction = (currentObject.Value.Event.SyncFrom ? SyncDirection.From : SyncDirection.To),
                            EventId = currentObject.Value.Event.EventId,
                            Metadata = new FileMetadata(currentObject.Value.ServerUidId)
                            {
                                EventTime = new DateTime(currentObject.Value.EventTimeUTCTicks, DateTimeKind.Utc),
                                HashableProperties = new FileMetadataHashableProperties(
                                    isFolder: currentObject.Value.IsFolder,
                                    lastTime: (currentObject.Value.LastTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)currentObject.Value.LastTimeUTCTicks, DateTimeKind.Utc)),
                                    creationTime: (currentObject.Value.CreationTimeUTCTicks == null
                                        ? (Nullable<DateTime>)null
                                        : new DateTime((long)currentObject.Value.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                    size: currentObject.Value.Size),
                                IsShare = currentObject.Value.IsShare,
                                MimeType = currentObject.Value.MimeType,
                                Permissions = (currentObject.Value.Permissions == null ? (Nullable<POSIXPermissions>)null : (POSIXPermissions)((int)currentObject.Value.Permissions)),
                                ParentFolderServerUid = (currentObject.Value.Parent == null ? null : (currentObject.Value.Parent.ServerUid == null ? null : currentObject.Value.Parent.ServerUid.ServerUid)),
                                StorageKey = currentObject.Value.StorageKey,
                                Version = currentObject.Value.Version
                            },
                            NewPath = currentObject.Value.CalculatedFullPath,
                            OldPath = (currentObject.Value.Event.PreviousId == null
                                ? null
                                : objectIdsToFullPath[(long)currentObject.Value.Event.PreviousId]),
                            Type = changeEnums[currentObject.Value.Event.FileChangeTypeEnumId]
                        };

                        CLError setCurrentChangeMD5Error = currentChange.SetMD5(currentObject.Value.MD5);
                        if (setCurrentChangeMD5Error != null)
                        {
                            throw new AggregateException("Error setting currentChange MD5", setCurrentChangeMD5Error.Exceptions);
                        }

                        (currentObject.Value.SyncCounter == null
                                ? pendingsWithoutSyncCounter
                                : pendingsWithSyncCounter)
                            .Add(currentChange);
                    }
                    else
                    {
                        FileMetadata currentToAdd = new FileMetadata(currentObject.Value.ServerUidId)
                        {
                            EventTime = new DateTime(currentObject.Value.EventTimeUTCTicks, DateTimeKind.Utc),
                            HashableProperties = new FileMetadataHashableProperties(
                                isFolder: currentObject.Value.IsFolder,
                                lastTime: (currentObject.Value.LastTimeUTCTicks == null
                                    ? (Nullable<DateTime>)null
                                    : new DateTime((long)currentObject.Value.LastTimeUTCTicks, DateTimeKind.Utc)),
                                creationTime: (currentObject.Value.CreationTimeUTCTicks == null
                                    ? (Nullable<DateTime>)null
                                    : new DateTime((long)currentObject.Value.CreationTimeUTCTicks, DateTimeKind.Utc)),
                                size: currentObject.Value.Size),
                            IsShare = currentObject.Value.IsShare,
                            MimeType = currentObject.Value.MimeType,
                            Permissions = (currentObject.Value.Permissions == null ? (Nullable<POSIXPermissions>)null : (POSIXPermissions)((int)currentObject.Value.Permissions)),
                            ParentFolderServerUid = (currentObject.Value.Parent == null ? null : (currentObject.Value.Parent.ServerUid == null ? null : currentObject.Value.Parent.ServerUid.ServerUid)),
                            StorageKey = currentObject.Value.StorageKey,
                            Version = currentObject.Value.Version
                        };

                        FilePath currentPath = currentObject.Value.CalculatedFullPath;
                        indexPaths.Add(currentPath, currentToAdd);
                        combinedIndexPlusChanges.Add(currentPath, currentToAdd);
                    }
                }

                // Create a list for pending changes which need to be processed
                List<FileChange> changeList = new List<FileChange>();

                foreach (FileChange pendingWithSyncCounter in pendingsWithSyncCounter)
                {
                    changeList.Add(pendingWithSyncCounter);
                }

                foreach (FileChange pendingWithoutSyncCounter in pendingsWithoutSyncCounter)
                {
                    changeList.Add(pendingWithoutSyncCounter);

                    switch (pendingWithoutSyncCounter.Type)
                    {

                        case FileChangeType.Modified:
                        case FileChangeType.Created:
                            combinedIndexPlusChanges[pendingWithoutSyncCounter.NewPath.Copy()] = pendingWithoutSyncCounter.Metadata;

                            GenericHolder<bool> reverseDeletion;
                            if (pathDeletions.TryGetValue(pendingWithoutSyncCounter.NewPath, out reverseDeletion))
                            {
                                reverseDeletion.Value = false;
                            }
                            break;

                        case FileChangeType.Deleted:
                            if (combinedIndexPlusChanges.Remove(pendingWithoutSyncCounter.NewPath))
                            {
                                pathDeletions.Remove(pendingWithoutSyncCounter.NewPath);
                                pathDeletions.Add(pendingWithoutSyncCounter.NewPath, new GenericHolder<bool>(true));
                            }
                            break;

                        case FileChangeType.Renamed:
                            if (combinedIndexPlusChanges.ContainsKey(pendingWithoutSyncCounter.OldPath))
                            {
                                FilePathHierarchicalNode<FileMetadata> newRename;
                                CLError hierarchyError = combinedIndexPlusChanges.GrabHierarchyForPath(pendingWithoutSyncCounter.NewPath, out newRename, suppressException: true);
                                if (hierarchyError == null
                                    && newRename == null)
                                {
                                    FilePath copiedNewPath = pendingWithoutSyncCounter.NewPath.Copy();
                                    combinedIndexPlusChanges.Rename(pendingWithoutSyncCounter.OldPath, copiedNewPath);
                                    combinedIndexPlusChanges[pendingWithoutSyncCounter.NewPath.Copy()] = pendingWithoutSyncCounter.Metadata;

                                    FilePathHierarchicalNode<GenericHolder<bool>> newDeletion;
                                    CLError deletionHierarchyError = pathDeletions.GrabHierarchyForPath(pendingWithoutSyncCounter.NewPath, out newDeletion, suppressException: true);

                                    if (deletionHierarchyError == null
                                        && newDeletion == null)
                                    {
                                        FilePathHierarchicalNode<GenericHolder<bool>> oldDeletionsToMove;
                                        CLError oldDeletionsHierarchicalError = pathDeletions.GrabHierarchyForPath(pendingWithoutSyncCounter.OldPath, out oldDeletionsToMove, suppressException: true);

                                        if (oldDeletionsHierarchicalError == null
                                            && oldDeletionsToMove != null)
                                        {
                                            pathDeletions.Rename(pendingWithoutSyncCounter.OldPath, pendingWithoutSyncCounter.NewPath);
                                        }

                                        GenericHolder<bool> previousDeletion;
                                        if (pathDeletions.TryGetValue(pendingWithoutSyncCounter.NewPath, out previousDeletion))
                                        {
                                            previousDeletion.Value = false;
                                        }
                                        else
                                        {
                                            pathDeletions.Add(pendingWithoutSyncCounter.NewPath, new GenericHolder<bool>(false));
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }

                // Define DirectoryInfo at current path which will be traversed
                DirectoryInfo indexRootPath = new DirectoryInfo(indexedPath);

                // RecurseIndexDirectory both adds the new changes to the list that are found on disc
                // and returns a list of all paths traversed for comparison to the existing index
                string[] recursedIndexes =
                    Helpers.EnumerateSingleItem(indexedPath)
                        .Concat(
                            RecurseIndexDirectory(
                                changeList,
                                indexPaths,
                                combinedIndexPlusChanges,
                                new Func<long, CLError>(
                                    delegate(long eventId)
                                    {
                                        return this.RemoveEventById(eventId);
                                    }),
                                indexedPath))
                        .ToArray();

                // Define a list to store indexes that previously existed in the last index, but were not found upon reindexing
                List<FileChange> possibleDeletions = new List<FileChange>();

                // Loop through the paths that previously existed in the index, but were not found when traversing the indexed path
                foreach (string deletedPath in
                    indexPaths.Select(currentIndex => currentIndex.Key.ToString())
                        .Except(recursedIndexes,
                            StringComparer.OrdinalIgnoreCase))
                {
                    FilePath deletedPathObject = deletedPath;

                    FileMetadata parentFolderMetadata;
                    string parentFolderServerUid;
                    if (combinedIndexPlusChanges.TryGetValue(deletedPathObject.Parent, out parentFolderMetadata))
                    {
                        // Get the ServerUid for the deleted path's parent folder
                        string serverUid;
                        string revision;
                        CLError queryUidError = QueryServerUid(parentFolderMetadata.ServerUidId, out serverUid, out revision);

                        if (queryUidError != null)
                        {
                            throw new AggregateException(string.Format("Unable to query ServerUid with id {0}", parentFolderMetadata.ServerUidId), queryUidError.Exceptions);
                        }

                        parentFolderServerUid = serverUid;
                    }
                    else
                    {
                        // Parent might not be communicated yet.
                        parentFolderServerUid = null;
                    }

                    // For the path that existed previously in the index but is no longer found on disc, process as a deletion
                    possibleDeletions.Add(new FileChange()
                    {
                        NewPath = deletedPath,
                        Type = FileChangeType.Deleted,
                        Metadata = indexPaths[deletedPathObject],
                        Direction = SyncDirection.To // detected that a file or folder was deleted locally, so Sync To to update server
                    });
                    pathDeletions.Remove(deletedPath);
                    pathDeletions.Add(deletedPath, new GenericHolder<bool>(true));
                }

                // Only add possible deletion if a parent wasn't already marked as deleted
                foreach (FileChange possibleDeletion in possibleDeletions)
                {
                    bool foundDeletedParent = false;
                    FilePath levelToCheck = possibleDeletion.NewPath.Parent;
                    while (levelToCheck.Contains(baseComparePath))
                    {
                        GenericHolder<bool> parentDeletion;
                        if (pathDeletions.TryGetValue(levelToCheck, out parentDeletion)
                            && parentDeletion.Value)
                        {
                            foundDeletedParent = true;
                            break;
                        }

                        levelToCheck = levelToCheck.Parent;
                    }
                    if (!foundDeletedParent)
                    {
                        changeList.Insert(0, possibleDeletion);
                    }
                }

                foreach (FilePath initiallySyncedBadge in indexPaths.Keys)
                {
                    _trace.writeToMemory(() => _trace.trcFmtStr(1, "IndexingAgent: BuildIndex: Call MessageEvents.SetPathState synced."));
                    MessageEvents.SetPathState(this, new SetBadge(PathState.Synced, initiallySyncedBadge));
                }

                // Callback on initial index completion
                // (will process detected changes and begin normal folder monitor processing)
                indexCompletionCallback(indexPaths,
                    changeList);
            }
        }
        /// <summary>
        /// bool in KeyValuePair Key should be true for pending, the long Value should be the FileSystemObjectId;
        /// use in a SortedList or SortedDictionary should give all non-pendings first then all pendings; within each group, they are ascending sorted by id
        /// </summary>
        private sealed class pendingThenIdComparer : IComparer<KeyValuePair<bool, long>>
        {
            public static pendingThenIdComparer Instance = new pendingThenIdComparer();

            private pendingThenIdComparer() { }

            int IComparer<KeyValuePair<bool, long>>.Compare(KeyValuePair<bool, long> x, KeyValuePair<bool, long> y)
            {
                if (x.Key == y.Key)
                {
                    return (x.Value == y.Value
                        ? 0
                        : (x.Value > y.Value
                            ? 1
                            : -1));
                }
                else if (x.Key)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Process changes found on disc that are different from the initial index to produce FileChanges
        /// and return the enumeration of paths traversed; recurses on self for inner folders
        /// </summary>
        /// <param name="changeList">List of FileChanges to add/update with new changes</param>
        /// <param name="currentDirectory">Current directory to scan</param>
        /// <param name="indexPaths">Initial index</param>
        /// <param name="combinedIndexPlusChanges">Initial index plus all previous FileChanges in database and changes made up through current reindexing</param>
        /// <param name="AddEventCallback">Callback to fire if a database event needs to be added</param>
        /// <param name="uncoveredChanges">Optional list of changes which no longer have a corresponding local path, only set when self-recursing</param>
        /// <returns>Returns the list of paths traversed</returns>
        private IEnumerable<string> RecurseIndexDirectory(List<FileChange> changeList, FilePathDictionary<FileMetadata> indexPaths, FilePathDictionary<FileMetadata> combinedIndexPlusChanges, Func<long, CLError> RemoveEventCallback, string currentDirectoryFullPath, FindFileResult currentDirectory = null, Dictionary<FilePath, LinkedList<FileChange>> uncoveredChanges = null)
        {
            if (disposed)
            {
                try
                {
                    throw new ObjectDisposedException(Resources.IndexingAgentThisIndexingAgent);
                }
                catch
                {
                    return null;
                }
            }

            // Store whether the current method call is outermost or a recursion,
            // only the outermost method call has a null uncoveredChanges parameter
            bool outermostMethodCall = (uncoveredChanges == null);

            // If current method call is not a self-recursion,
            // build the uncoveredChanges dictionary with initial values from the values in changeList
            if (outermostMethodCall)
            {
                uncoveredChanges = new Dictionary<FilePath, LinkedList<FileChange>>(FilePathComparer.Instance);
                //new Dictionary<FilePath, FileChange>(changeList.Count,
                //FilePathComparer.Instance);
                foreach (FileChange currentChange in changeList)
                {
                    if (uncoveredChanges.ContainsKey(currentChange.NewPath))
                    {
                        uncoveredChanges[currentChange.NewPath].AddFirst(currentChange);
                    }
                    else
                    {
                        uncoveredChanges.Add(currentChange.NewPath, new LinkedList<FileChange>(new FileChange[] { currentChange }));
                    }
                }
            }

            // Current path traversed, remove from uncoveredChanges
            uncoveredChanges.Remove(currentDirectoryFullPath);

            // Create a list of the traversed paths at or below the current level
            List<string> filePathsFound = new List<string>();

            IEnumerable<FindFileResult> innerDirectories;
            IEnumerable<FindFileResult> innerFiles;
            if (currentDirectory == null)
            {
                bool rootNotFound;
                IList<FindFileResult> allInnerPaths = FindFileResult.RecursiveDirectorySearch(currentDirectoryFullPath,
                    (FileAttributes.Hidden// ignore hidden files
                        | FileAttributes.Offline// ignore offline files (data is not available on them)
                        | FileAttributes.System// ignore system files
                        | FileAttributes.Temporary),// ignore temporary files
                    out rootNotFound);

                if (rootNotFound)
                {
                    // the following should NOT be a HaltAll: TODO: add appropriate event bubbling to halt engine
                    MessageEvents.FireNewEventMessage(
                        "Unable to find Cloud directory at path: " + currentDirectoryFullPath,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo(),
                        this.syncbox,
                        this.syncbox.CopiedSettings.DeviceId);

                    // This is a really bad error.  It means the connection to the file system is broken, and if we just ignore this error,
                    // sync will determine that there are no files in the Syncbox folder, and it will actually delete all of the files on the server.
                    // We have to stop this thread dead in its tracks, and do it in such a way that it is not recoverable.
                    CLError error = new Exception("Unable to find cloud directory at path: " + currentDirectoryFullPath);
                    error.Log(_trace.TraceLocation, _trace.LogErrors);
                    _trace.writeToLog(1, "IndexingAgent: RecursiveIndexDirectory: ERROR: Exception: Msg: <{0}>.", error.PrimaryException.Message);

                    // root path required, blow up
                    throw new DirectoryNotFoundException("Unable to find Cloud directory at path: " + currentDirectoryFullPath);
                }

                innerDirectories = allInnerPaths.Where(currentInnerDirectory => currentInnerDirectory.IsFolder);
                innerFiles = allInnerPaths.Where(currentInnerFile => !currentInnerFile.IsFolder);
            }
            else
            {
                innerDirectories = currentDirectory.Children.Where(currentInnerDirectory => currentInnerDirectory.IsFolder);
                innerFiles = currentDirectory.Children.Where(currentInnerFile => !currentInnerFile.IsFolder);
            }

            try
            {
                // Loop through all subdirectories under the current directory
                foreach (FindFileResult subDirectory in innerDirectories)
                {
                    string subDirectoryFullPath = currentDirectoryFullPath + ((char)0x5c /* \ */).ToString() + subDirectory.Name;
                    FilePath subDirectoryPathObject = subDirectoryFullPath;

                    // Store current subdirectory path as traversed
                    filePathsFound.Add(subDirectoryFullPath);
                    // Create properties for the current subdirectory
                    FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(true,
                        (subDirectory.LastWriteTime == null ? (Nullable<DateTime>)null : ((DateTime)subDirectory.LastWriteTime)),
                        (subDirectory.CreationTime == null ? (Nullable<DateTime>)null : ((DateTime)subDirectory.CreationTime)),
                        null);

                    // Grab the last metadata that matches the current directory path, if any
                    FilePathHierarchicalNode<FileMetadata> existingHierarchy;
                    CLError hierarchyError = combinedIndexPlusChanges.GrabHierarchyForPath(subDirectoryPathObject, out existingHierarchy, true);
                    // If there is no existing event, a directory was added
                    if (hierarchyError == null
                        && existingHierarchy == null)
                    {
                        FileMetadata parentFolderMetadata;
                        string parentFolderServerUid;
                        if (combinedIndexPlusChanges.TryGetValue(subDirectoryPathObject.Parent, out parentFolderMetadata))
                        {
                            // Get the ServerUid for the current directory's parent folder
                            string serverUid;
                            string revision;
                            CLError queryUidError = QueryServerUid(parentFolderMetadata.ServerUidId, out serverUid, out revision);

                            if (queryUidError != null)
                            {
                                throw new AggregateException(string.Format("Unable to query ServerUid with id {0}", parentFolderMetadata.ServerUidId), queryUidError.Exceptions);
                            }

                            parentFolderServerUid = serverUid;
                        }
                        else
                        {
                            // This change may not have been communicated.
                            parentFolderServerUid = null;
                        }

                        long serverUidId;
                        CLError createServerUidError = CreateNewServerUid(serverUid: null, revision: null, serverUidId: out serverUidId);

                        if (createServerUidError != null)
                        {
                            throw new AggregateException("Error creating new ServerUid", createServerUidError.Exceptions);
                        }

                        FileMetadata newDirectoryMetadata = new FileMetadata(serverUidId)
                        {
                            HashableProperties = compareProperties,
                            ParentFolderServerUid = parentFolderServerUid
                        };

                        changeList.Add(new FileChange()
                        {
                            NewPath = subDirectoryPathObject,
                            Type = FileChangeType.Created,
                            Metadata = newDirectoryMetadata,
                            Direction = SyncDirection.To // detected that a folder was created locally, so Sync To to update server
                        });

                        combinedIndexPlusChanges.Add(subDirectoryPathObject, newDirectoryMetadata);
                    }

                    // Add the inner paths to the output list by recursing (which will also process inner changes)
                    filePathsFound.AddRange(RecurseIndexDirectory(changeList,
                        indexPaths,
                        combinedIndexPlusChanges,
                        RemoveEventCallback,
                        subDirectoryFullPath,
                        subDirectory,
                        uncoveredChanges));
                }

                // Loop through all files under the current directory
                foreach (FindFileResult currentFile in innerFiles)
                {
                    string currentFileFullPath = currentDirectoryFullPath + ((char)0x5c /* \ */).ToString() + currentFile.Name;
                    FilePath currentFilePathObject = currentFileFullPath;

                    // Remove file from list of changes which have not yet been traversed (since it has been traversed)
                    uncoveredChanges.Remove(currentFilePathObject);

                    // Add file path to traversed output list
                    filePathsFound.Add(currentFileFullPath);
                    // Find file properties
                    FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(false,
                        (currentFile.LastWriteTime == null ? (Nullable<DateTime>)null : ((DateTime)currentFile.LastWriteTime).DropSubSeconds()),
                        (currentFile.CreationTime == null ? (Nullable<DateTime>)null : ((DateTime)currentFile.CreationTime).DropSubSeconds()),
                        currentFile.Size);

                    // Grab the last metadata that matches the current file path, if any
                    FileMetadata existingFileMetadata;
                    // If a change does not already exist for the current file path,
                    // check if file has changed since last index to process changes
                    if (combinedIndexPlusChanges.TryGetValue(currentFilePathObject, out existingFileMetadata))
                    {
                        // If the file has changed (different metadata), then process a file modification change
                        if (!FileMetadataHashableComparer.Instance.Equals(compareProperties, existingFileMetadata.HashableProperties))
                        {
                            FileMetadata parentFolderMetadata;
                            string parentFolderServerUid;
                            if (combinedIndexPlusChanges.TryGetValue(currentFilePathObject.Parent, out parentFolderMetadata))
                            {
                                // Get the ServerUid for the current directory's parent folder
                                string serverUid;
                                string revision;
                                CLError queryUidError = QueryServerUid(parentFolderMetadata.ServerUidId, out serverUid, out revision);

                                if (queryUidError != null)
                                {
                                    throw new AggregateException(string.Format("Unable to query ServerUid with id {0}", parentFolderMetadata.ServerUidId), queryUidError.Exceptions);
                                }

                                parentFolderServerUid = serverUid;
                            }
                            else
                            {
                                // May not have been communicated yet.
                                parentFolderServerUid = null;
                            }

                            FileMetadata modifiedMetadata = new FileMetadata(existingFileMetadata.ServerUidId)
                            {
                                HashableProperties = compareProperties,
                                ParentFolderServerUid = parentFolderServerUid/*,
                                    StorageKey = existingFileMetadata.StorageKey*/
                                // DO NOT copy StorageKey because this metadata is for a modified change which would therefore require a new StorageKey
                            };

                            changeList.Add(new FileChange()
                            {
                                NewPath = currentFilePathObject,
                                Type = FileChangeType.Modified,
                                Metadata = modifiedMetadata,
                                Direction = SyncDirection.To // detected that a file was modified locally, so Sync To to update server
                            });

                            combinedIndexPlusChanges[currentFilePathObject] = modifiedMetadata;
                        }
                    }
                    // else if index doesn't contain the current path, then the file has been created
                    else
                    {
                        FileMetadata parentFolderMetadata;
                        string parentFolderServerUid;
                        if (combinedIndexPlusChanges.TryGetValue(currentFilePathObject.Parent, out parentFolderMetadata))
                        {
                            // Get the ServerUid for the current directory's parent folder
                            string serverUid;
                            string revision;
                            CLError queryUidError = QueryServerUid(parentFolderMetadata.ServerUidId, out serverUid, out revision);

                            if (queryUidError != null)
                            {
                                throw new AggregateException(string.Format("Unable to query ServerUid with id {0}", parentFolderMetadata.ServerUidId), queryUidError.Exceptions);
                            }

                            parentFolderServerUid = serverUid;
                        }
                        else
                        {
                            // May not have been communicated yet.
                            parentFolderServerUid = null;
                        }

                        long serverUidId;
                        CLError createServerUidError = CreateNewServerUid(serverUid: null, revision: null, serverUidId: out serverUidId);

                        if (createServerUidError != null)
                        {
                            throw new AggregateException("Error creating new ServerUid", createServerUidError.Exceptions);
                        }

                        FileMetadata fileCreatedMetadata = new FileMetadata(serverUidId)
                        {
                            HashableProperties = compareProperties,
                            ParentFolderServerUid = parentFolderServerUid//,
                            //LinkTargetPath = //Todo: needs to read target path
                        };

                        changeList.Add(new FileChange()
                        {
                            NewPath = currentFilePathObject,
                            Type = FileChangeType.Created,
                            Metadata = fileCreatedMetadata,
                            Direction = SyncDirection.To // detected that a file was created locally, so Sync To to update server
                        });

                        combinedIndexPlusChanges.Add(currentFilePathObject, fileCreatedMetadata);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (outermostMethodCall)
                {
                    // TODO: may not wish to cause the entire SDK to halt here, instead this should only halt the current engine
                    MessageEvents.FireNewEventMessage(
                        "Unable to scan files/folders in Cloud folder. Location not accessible:" + Environment.NewLine + ex.Message,
                        EventMessageLevel.Important,
                        new HaltAllOfCloudSDKErrorInfo(),
                        this.syncbox,
                        this.syncbox.CopiedSettings.DeviceId);
                }
            }
            catch { }

            // If this method call was the outermost one (not recursed),
            // then the uncoveredChanges list was depleted of all traversed paths leaving
            // only file changes that no longer match anything existing on the disc
            // (meaning the change needs to be reversed since the file/folder was deleted)
            if (outermostMethodCall)
            {
                // Loop through the uncovered file changes
                foreach (KeyValuePair<FilePath, LinkedList<FileChange>> uncoveredChange in uncoveredChanges)
                {
                    // Take all the changes at a path which no longer has a file/folder and
                    // either remove all the events (if the last sync index did not contain the folder)
                    // or turn all changes into a single deletion change (if the last sync index did contain the folder)
                    bool existingDeletion = false;
                    LinkedListNode<FileChange> currentUncoveredChange = uncoveredChange.Value.First;
                    bool existsInIndex = indexPaths.ContainsKey(uncoveredChange.Key);
                    // Continue checking the linked list nodes until it is past the end (thus null)
                    while (currentUncoveredChange != null)
                    {
                        // Only keep the first deletion event and only if there is a path in the index for the corresponding delete
                        if (existsInIndex
                            && !existingDeletion
                            && currentUncoveredChange.Value.Type == FileChangeType.Deleted)
                        {
                            existingDeletion = true;
                        }
                        else if (currentUncoveredChange.Value.Direction == SyncDirection.To)
                        {
                            changeList.Remove(currentUncoveredChange.Value);
                            if (currentUncoveredChange.Value.EventId > 0)
                            {
                                RemoveEventCallback(currentUncoveredChange.Value.EventId);
                            }
                        }

                        // Move to the next FileChange in the linked list
                        currentUncoveredChange = currentUncoveredChange.Next;
                    }
                }
            }

            // return the list of all traversed paths at or below the current directory
            return filePathsFound;
        }

        #endregion private methods

        #region dispose

        #region IDisposable members
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~IndexingAgent()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!disposed)
                {
                    // Run dispose on inner managed objects based on disposing condition
                    if (disposing)
                    {
                        //// found case where changeEnumsBackward is null on normal condition;
                        //// (probably cause dictionaries are marked static but dispose is on an instance)
                        //// commented out:
                        //
                        //lock (changeEnumsLocker)
                        //{
                        //    if (changeEnums != null)
                        //    {
                        //        changeEnums.Clear();
                        //        changeEnums = null;
                        //    }
                        //
                        //    if (changeEnumsBackward != null)
                        //    {
                        //        changeEnumsBackward.Clear();
                        //        changeEnumsBackward = null;
                        //    }
                        //}
                    }

                    disposed = true;
                }
            }
        }
        #endregion

        #region SQLTransactionalBase implementation
        private sealed class SQLTransactionalImplementation : SQLTransactionalBase
        {
            public readonly ISQLiteConnection sqlConnection;
            public readonly ISQLiteTransaction sqlTransaction;

            private readonly GenericHolder<bool> transactionCommitted;

            public SQLTransactionalImplementation(ISQLiteConnection sqlConnection, ISQLiteTransaction sqlTransaction)
            {
                this.sqlConnection = sqlConnection;
                this.sqlTransaction = sqlTransaction;
                if (sqlTransaction != null)
                {
                    transactionCommitted = new GenericHolder<bool>(false);
                }
            }

            #region SQLTransactionalBase overrides
            public override void Commit()
            {
                base.CheckDisposed();

                if (sqlTransaction != null)
                {
                    lock (transactionCommitted)
                    {
                        if (transactionCommitted.Value)
                        {
                            throw new NotSupportedException("Cannot commit same database transaction more than once");
                        }

                        sqlTransaction.Commit();

                        transactionCommitted.Value = true;
                    }
                }
            }

            protected override bool _disposed
            {
                get
                {
                    return _localDisposed;
                }
            }
            private bool _localDisposed = false;

            protected override void Dispose(bool disposing)
            {
                // Check to see if Dispose has already been called. 
                if (!this._localDisposed)
                {
                    // If disposing equals true, dispose all managed 
                    // and unmanaged resources. 
                    if (disposing)
                    {
                        // Dispose managed resources.
                        if (sqlTransaction != null)
                        {
                            lock (transactionCommitted)
                            {
                                if (!transactionCommitted.Value)
                                {
                                    try
                                    {
                                        sqlTransaction.Rollback();
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                            try
                            {
                                sqlTransaction.Dispose();
                            }
                            catch
                            {
                            }
                        }

                        if (sqlConnection != null)
                        {
                            try
                            {
                                sqlConnection.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }

                    // Call the appropriate methods to clean up 
                    // unmanaged resources here. 
                    // If disposing is false, 
                    // only the following code is executed.

                    /* [ My code here ] */

                    // Note disposing has been done.
                    this._localDisposed = true;
                }
            }
            #endregion
        }
        #endregion
    }
}