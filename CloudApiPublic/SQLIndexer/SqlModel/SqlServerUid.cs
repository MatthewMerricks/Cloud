using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLIndexer.SqlModel
{
    [SqlAccess.Class("ServerUids")]
    internal sealed class SqlServerUid
    {
        [SqlAccess.Property]
        public long ServerUidId { get; set; }

        [SqlAccess.Property]
        public string ServerUid { get; set; }

        [SqlAccess.Property]
        public string Revision { get; set; }
    }
}