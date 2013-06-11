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

namespace Cloud.SQLIndexer.SqlModel
// \cond
{
    [SqlAccess.Class(CLDefinitions.FileSystemObjects)]
    public sealed class FileSystemObject : IBeforeDeleteTrigger
    {
        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases
        
                    //"SELECT " +
                    //"CASE WHEN Enums.Name = 'Renamed' " +
                    //"AND Events.PreviousId IS NOT NULL " +
                    //"THEN Events.PreviousId " +
                    //"ELSE FileSystemObjects.FileSystemObjectId END " +
                    //"FROM FileSystemObjects " +
                    //"LEFT OUTER JOIN Events ON FileSystemObjects.EventId = Events.EventId " +
                    //"LEFT OUTER JOIN Enums ON Events.FileChangeTypeEnumId = Enums.EnumId " +
                    //"AND Events.FileChangeTypeCategoryId = Enums.EnumCategoryId " +
                    //"WHERE FileSystemObjects.FileSystemObjectId = ?",

        private const string sql_conn = "XiF/n8DAmECRcpl1q3g5SCMu4j5pzQFxK+sQhCxNZR9puPEreUwrj6A/1UwE8ktpAuGt57Bm1IbmmHMk/kY3G+MKe58tbwnAe4daX+qgjdRDYBxfrMfx0aKiW+yzNx9Q9WiQZaazuBIzKOw6pz/q/cxB0/puYxQypNcDyJbgqTA79VQemSqy98fJnnVvC6hyxrkHeIF+fs9GeVL0PqGpexS0eTKGId94P8ToCbWsi7eaAtuSsYUKWTGwQK35rTXU1XHBpU6IwIFo41teYe0iEFTmRiVoIG7j3RvmJmKOUxfEpe3PhYjplboINKtAdGnOuQYV8wyaoISTJKVJSbELlnZoAuMxivvksrQLxoMzobpwqpxPYdmwPMBamp1LnECTsjn+8nTi3mNdReAPBSQt5SYMdcUd5Bb5XaWxu5OqxfoipUdC+2OppDkHQC6YCk2I/k9AIr1W26mxKuNJrYQlWnF3ac0OqwWr9T8dHY9AhfHEblnPZ2yZ3HsA83JM+IObgzJjKCJzq6A+CMrH92tj+ee/2vc7mgYKBzlPymGCJo4Y0XR/Ahkd4I7OkmOb3gdjDwBuyTW0Au06lzRePEYgpxTFd+b6cjVE45oc9oXtYQEYWW9kq9bUB8ykxmGndB/qxhGPAqEhOpYPa01NO1Glhk+3vsvqCaRTo/UAMBHozvMeVwnTOpDu8gAXjJbzdW3/+rjmBUTqn6WO7pZRqUvnbajUrQgECjlTeu+cIDItrhcClnMIk+T1e9RJh0SQvAEbIRcYXFKUd8e9eHPNGmErgkxPZ4DQYflsdstIOo5yIES0UWtSxn5SEhcSz/4Sxo0LRcZ1JTl4r2jq/tnVna/RuLvb/Ny/6zF9OIimv5rKKCyhKpHzMqylDVyR9PSbNuzFe95nuSAoke43CJgb3zgnGtVMQzk4yaOnhg5XYiTD4uI2rWWtXnCW6YU+jx4a433oVRtzl/Qg+Fn9ekK5eFpEXJtrzaLvxCbH0tEMBEopJf016Hci8uCUNbUNTmb0iDcSg5YTLPHLtD2hbuzxJYkp+TRmVz3kDl2l+yKoA49rH/3mOjxrFzWQlkzIwIoIR8+LLGJxZTh5uExG6/9is5pUBqllTL+IF0sXZP73g+PHBTY=";
        

                        //"UPDATE FileSystemObjects " +
                        //"SET ParentFolderId = ? " +
                        //"WHERE ParentFolderId = ?"
        
        private const string before_delete = "9tA4A9qheaxmqn5OBpSv86o8u/HE1U3uoVPGDIvO8uxFwbNTMjsBNV0TBKek0RAFVHLHxUuWhXcYIIDQlVL5HeF4UTFjYSdKBH0wm0SsApDR77FTEJf3TPQXB4/rBAm+Q1+CWo5fRZWJr88LHe1DnN90L3GNZi6wRW7lGXeTHCUmFj/D2S4qcja3kxNFznRnFeG18HbQy30IaSQ+JigcJw==";
        #region IBeforeDeleteTrigger member

        public void BeforeDelete(ISQLiteConnection sqlConn, ISQLiteTransaction sqlTran = null)
        {
            long earlierParentId;
            if (!SqlAccessor<object>.TrySelectScalar<long>(
                sqlConn,

                Helpers.DecryptString(sql_conn,Encoding.ASCII.GetString(Convert.FromBase64String(indexDBPassword))),
                   
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

                    beforeDelete.CommandText =

                       Helpers.DecryptString(before_delete, Encoding.ASCII.GetString(Convert.FromBase64String(indexDBPassword)))
                        ;

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
    // \endcond
}