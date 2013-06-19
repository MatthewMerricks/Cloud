//
// FileSystemObject.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.SQLProxies;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;
using System.Reflection;

namespace Cloud.SQLIndexer.SqlModel
{
    internal static class FileSystemObjectHolder
    {
        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases

        [Obfuscation(Exclude = true)]
        [SqlAccess.Class(CLDefinitions.SqlModel_FileSystemObject)]
        internal sealed class FileSystemObject : IBeforeDeleteTrigger
        {
            #region IBeforeDeleteTrigger member

            public void BeforeDelete(ISQLiteConnection sqlConn, ISQLiteTransaction sqlTran = null)
            {
                const string selectPreviousIdByFileSystemObjectId = "XiF/n8DAmECRcpl1q3g5SCMu4j5pzQFxK+sQhCxNZR9puPEreUwrj6A/1UwE8ktpAuGt57Bm1IbmmHMk/kY3G/4yUbjguVrv6muVtiWsqJ5pjAId+ObJbHK/Aw7LtTu2CjfbbPliin7nTaB6J/0rnf7HjtuQy4v2VQQXlplwZmYynJ824UCNhYhpXvqaZmzJyr5p0DAmGMScTZHzRwOaM7YhK6H+FOdRybblLMQRL5Y3BAxKFFmF8yXEeGgY5t6bbAjBJ8b94IaG84Zn1atx7mKBi6wvTsIGh8AmJdusU2rle7Bi6jPW4IcgTSi2FBVQx3NVzl65tNskbMJ3NvTWaqihDL94BcKZyDYn8wyq7h3sA0pfjuocI6X/77E2Bp2dVLe4mW0+HVodjQmDj0nDX1TJyPEGbMtNcgFc76Jefuz9jKBGV2dhPTOVfWuKICkD+woEsBNlhNN285Mx6P1bxDHgZ24Wif3Z3T6jxrc5L7cZBqw6Ibrgg0mngg1NCixfD3MZmX3Exq9yGILrAHcEGd0IYYVXRLmst7dzvUnEVDiBnz6HQ1CeWD4mCqqgSLP8RC0Ki45l5lt+3sK0goVMIgUMDPkslff4wLnN5xlbzsMZxQ/ZaHTUGieXhR8fUEHvnqBD4BI87cCFc4T2KebKzZj0HY6pg7EjubIGg09rYnZF+apJidQ+hO16fpdO1cRh3ZcR2dDTUuC/1eKjfR8yZD+sGZKDoa4Ei1t+CA660HOcObZQ0vvaLcAv/pMBFCFrIDJoOupKGPOiLrrx6TnFex1Dc10ekhOHbM4bcxGsGuD+nK5iJfmMQouRxOgYnnxgWfqOcm+1t+b9+ArHHaN1MtUp8e9R/KyWNtp9geRJqJpav+HLKtdM+WHw68a35N0JQLfTbJD7e7LAs3a82ZILTKf0lZ6A5E3uXPfGGcj4p8oSi5FkgERSEAasE9qyzWZ+3R2QDZ35h8M9jk5n/TlVIy2HxRvaxbrFuMXfN+kjWgo2Ta1vsZFhA236f8Ih8VyLD+OCZRK9UHEK3o23Y6N/0weRKdj8bOq5gZR7T20GWIVs9w0Mg0mK96YY2qCKJCxd8AtKc5eqQdAH5rOPx/dYfw==";

                long earlierParentId;
                if (!SqlAccessor<object>.TrySelectScalar<long>(
                    sqlConn,
                    //// before
                    //
                    //"SELECT " +
                    //    "CASE WHEN Enums.Name = '" + Enum.GetName(typeof(FileChangeType), FileChangeType.Renamed) + "' " +
                    //    "AND Events.PreviousId IS NOT NULL " +
                    //    "THEN Events.PreviousId " +
                    //    "ELSE FileSystemObjects.FileSystemObjectId END " +
                    //    "FROM FileSystemObjects " +
                    //    "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                    //    "LEFT OUTER JOIN Enums ON Events.FileChangeTypeEnumId = Enums.EnumId " +
                    //    "AND Events.FileChangeTypeCategoryId = Enums.EnumCategoryId " +
                    //    "WHERE FileSystemObjects.FileSystemObjectId = ?"
                    //
                    //// after (decrypted; {0}: Enum.GetName(typeof(FileChangeType), FileChangeType.Renamed) )
                    //
                    //SELECT
                    //CASE WHEN Enums.Name = '{0}'
                    //AND Events.PreviousId IS NOT NULL
                    //THEN Events.PreviousId
                    //ELSE FileSystemObjects.FileSystemObjectId END
                    //FROM FileSystemObjects
                    //LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId
                    //LEFT OUTER JOIN Enums ON Events.FileChangeTypeEnumId = Enums.EnumId
                    //AND Events.FileChangeTypeCategoryId = Enums.EnumCategoryId
                    //WHERE FileSystemObjects.FileSystemObjectId = ?
                    string.Format(
                        Helpers.DecryptString(
                            selectPreviousIdByFileSystemObjectId,
                            Encoding.ASCII.GetString(
                                Convert.FromBase64String(indexDBPassword))),
                        Enum.GetName(typeof(FileChangeType), FileChangeType.Renamed)),
                    out earlierParentId,
                    sqlTran,
                    Helpers.EnumerateSingleItem(this.FileSystemObjectId)))
                {
                    throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Unable to find the current or previous object id (for renames only) before deletion of a FileSystemObject");
                }

                if (earlierParentId != this.FileSystemObjectId)
                {
                    using (ISQLiteCommand beforeDelete = sqlConn.CreateCommand())
                    {
                        if (sqlTran != null)
                        {
                            beforeDelete.Transaction = sqlTran;
                        }

                        const string updateFileSystemObjectByParentId = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFVHLHxUuWhXcYIIDQlVL5HeF4UTFjYSdKBH0wm0SsApDR77FTEJf3TPQXB4/rBAm+Q1+CWo5fRZWJr88LHe1DnN90L3GNZi6wRW7lGXeTHCUmFj/D2S4qcja3kxNFznRnWLpb3SUL78Y8otvkprgrSw==";

                        beforeDelete.CommandText =
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

                        ISQLiteParameter replaceParentIdParam = beforeDelete.CreateParameter();
                        replaceParentIdParam.Value = earlierParentId;
                        beforeDelete.Parameters.Add(replaceParentIdParam);

                        ISQLiteParameter findParentIdParam = beforeDelete.CreateParameter();
                        findParentIdParam.Value = this.FileSystemObjectId;
                        beforeDelete.Parameters.Add(findParentIdParam);

                        beforeDelete.ExecuteNonQuery();
                    }
                }
            }

            #endregion

            [SqlAccess.Property]
            public long FileSystemObjectId { get; set; }

            [SqlAccess.Property]
            public string Name { get; set; }

            [SqlAccess.Property]
            public int NameCIHash { get; set; }

            [SqlAccess.Property]
            public Nullable<long> ParentFolderId { get; set; }

            [SqlAccess.Property]
            public Nullable<long> LastTimeUTCTicks { get; set; }

            [SqlAccess.Property]
            public Nullable<long> CreationTimeUTCTicks { get; set; }

            [SqlAccess.Property]
            public bool IsFolder { get; set; }

            [SqlAccess.Property]
            public Nullable<long> Size { get; set; }

            private const string storageKeyName = "StorageKey";
            [SqlAccess.Property(storageKeyName)]
            public string Revision { get; set; }

            [SqlAccess.Property]
            public string ServerName { get; set; }

            [SqlAccess.Property]
            public Nullable<long> EventId { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.ReadOnly)]
            public Nullable<long> EventOrder { get; set; }

            [SqlAccess.Property]
            public Nullable<bool> IsShare { get; set; }

            [SqlAccess.Property]
            public byte[] MD5 { get; set; }

            [SqlAccess.Property]
            public Nullable<int> Version { get; set; }

            [SqlAccess.Property]
            public long ServerUidId { get; set; }

            [SqlAccess.Property]
            public bool Pending { get; set; }

            [SqlAccess.Property]
            public Nullable<long> SyncCounter { get; set; }

            [SqlAccess.Property]
            public string MimeType { get; set; }

            [SqlAccess.Property]
            public Nullable<int> Permissions { get; set; }

            [SqlAccess.Property]
            public long EventTimeUTCTicks { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.ReadOnly)]
            public string CalculatedFullPath { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.ReadOnly)]
            public string CalculatedFullPathCIHashes { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
            public FileSystemObject Parent { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
            public FileSystemObject Child { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
            public Event Event { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
            public Event ReversePrevious { get; set; }

            [SqlAccess.Property(SqlAccess.FieldType.JoinedTable)]
            public Sync Sync { get; set; }

            [SqlAccess.Property(Constants.SqlServerUidName, SqlAccess.FieldType.JoinedTable)]
            public SqlServerUid ServerUid { get; set; }
        }
    }
}