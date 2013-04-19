//  MigrationList.cs
//  Cloud Windows
//
//  Created by David Bruck.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.SQLProxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLIndexer.Migrations
{
    internal static class MigrationList
    {
        public static IEnumerable<KeyValuePair<int, IMigration>> GetMigrationsAfterVersion(int version)
        {
            int searchedIndex = Array.BinarySearch(Versions, version);
            for (int currentMigration =

                    // allow for versions in between migrations (i.e. we're on version 4 and we only have migrations (2->3, 3->4, 4->6, 6->7) with no migration 5
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
        //
        // ¡¡ Always change the Version column in the Version table after applying migrations !!
        //
        //// NOTE! SQLite change: no longer write version in a Version table, now write as PRAGMA user_version after migration is succesfully applied
        // ¡¡ Always update the IndexDBScripts so that new database creations are built up to the current
        //    version and write that version number in the Version column in the Version table via the last script !!
        private static readonly int[] Versions = new int[]
        {
            // Starting version is 2 (1 was reserved for incomplete) so earliest migration will be 2->3

            //Version 3 will go here as "3" (without the quotes)
        };

        // ¡¡ Match indexes of Migrations to Versions !!
        private static readonly IMigration[] Migrations = new IMigration[]
        {
            //Version 3 will go here as "Migration3.Instance" (without the quotes), class Migration3 needs to be created when it is needed; it will be singleton using Instance
        };
    }

    /// <summary>
    /// Interface to implement to support a version migration of the database
    /// </summary>
    internal interface IMigration
    {
        /// <summary>
        /// Method to implement which is called to perform the actual migration.
        /// </summary>
        /// <param name="connection">Already open connection with password already set for encrypted-access. Foreign key constraints are set on by default and may be turned on\off by PRAGMA statements</param>
        /// <param name="indexDBPassword">Encoded database password which can be decoded and used for string decryption (Helpers.DecryptString method) to protect the contents of statement changes</param>
        void Apply(ISQLiteConnection connection, string indexDBPassword);
    }
}