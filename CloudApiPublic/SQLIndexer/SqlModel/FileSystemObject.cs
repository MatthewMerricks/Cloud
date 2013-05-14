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

namespace Cloud.SQLIndexer.SqlModel
{
    [SqlAccess.Class("FileSystemObjects")]
    internal sealed class FileSystemObject : IBeforeDeleteTrigger
    {
        #region IBeforeDeleteTrigger member

        public void BeforeDelete(ISQLiteConnection sqlConn, ISQLiteTransaction sqlTran = null)
        {
            long earlierParentId;
            if (!SqlAccessor<object>.TrySelectScalar<long>(
                sqlConn,
                "SELECT " +
                    "CASE WHEN Enums.Name = 'Renamed' " +
                    "AND Events.PreviousId IS NOT NULL " +
                    "THEN Events.PreviousId " +
                    "ELSE FileSystemObjects.FileSystemObjectId END " +
                    "FROM FileSystemObjects " +
                    "LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                    "LEFT OUTER JOIN Enums ON Events.FileChangeTypeEnumId = Enums.EnumId " +
                    "AND Events.FileChangeTypeCategoryId = Enums.EnumCategoryId " +
                    "WHERE FileSystemObjects.FileSystemObjectId = ?",
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

                    beforeDelete.CommandText = "UPDATE FileSystemObjects " +
                        "SET ParentFolderId = ? " +
                        "WHERE ParentFolderId = ?";

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
        public Nullable<long> ParentFolderId { get; set; }

        [SqlAccess.Property]
        public Nullable<long> LastTimeUTCTicks { get; set; }

        [SqlAccess.Property]
        public Nullable<long> CreationTimeUTCTicks { get; set; }

        [SqlAccess.Property]
        public bool IsFolder { get; set; }

        [SqlAccess.Property]
        public Nullable<long> Size { get; set; }

        [SqlAccess.Property]
        public string StorageKey { get; set; }

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