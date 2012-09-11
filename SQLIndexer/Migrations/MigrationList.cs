using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;

namespace SQLIndexer.Migrations
{
    public static class MigrationList
    {
        public static IEnumerable<KeyValuePair<int, IMigration>> GetMigrationsAfterVersion(int version)
        {
            int searchedIndex = Array.BinarySearch(Versions, version);
            for (int currentMigration =
                (searchedIndex > -1
                    ? searchedIndex + 1
                    : ~searchedIndex);
                currentMigration < Versions.Length;
                currentMigration++)
            {
                yield return new KeyValuePair<int, IMigration>(Versions[currentMigration], Migrations[currentMigration]);
            }
        }

        // ¡¡ Keep versions in ascending order !!
        private static readonly int[] Versions = new int[]
        {
            2
        };

        // ¡¡ Match indexes of Migrations to Versions !!
        private static readonly IMigration[] Migrations = new IMigration[]
        {
            Migration2.Instance
        };
    }

    public interface IMigration
    {
        void Apply(SqlCeConnection connection);
    }
}