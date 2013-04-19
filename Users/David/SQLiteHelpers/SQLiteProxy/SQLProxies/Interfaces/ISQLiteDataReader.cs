using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite implementation of DbDataReader.
    /// </summary>
    public interface ISQLiteDataReader : IDisposable
    {
        /// <summary>
        /// Indexer to retrieve data from a column given its name
        /// </summary>
        /// <param name="name">The name of the column to retrieve data for</param>
        /// <returns>The value contained in the column</returns>
        object this[string name] { get; }

        /// <summary>
        /// Returns True if the specified column is null
        /// </summary>
        /// <param name="i">The index of the column to retrieve</param>
        /// <returns>True or False</returns>
        bool IsDBNull(int i);

        /// <summary>
        /// Retrieves the i of a column, given its name
        /// </summary>
        /// <param name="name">The name of the column to retrieve</param>
        /// <returns>Retrieves the i of a column, given its name</returns>
        int GetOrdinal(string name);
        
        /// <summary>
        /// Reads the next row from the resultset
        /// </summary>
        /// <returns>True if a new row was successfully loaded and is ready for processing</returns>
        bool Read();
    }
}