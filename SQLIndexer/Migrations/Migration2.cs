using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;

namespace SQLIndexer.Migrations
{
    public class Migration2 : IMigration
    {
        public static Migration2 Instance = new Migration2();

        private Migration2() { }

        #region IMigration member
        public void Apply(SqlCeConnection connection)
        {

        }
        #endregion
    }
}
