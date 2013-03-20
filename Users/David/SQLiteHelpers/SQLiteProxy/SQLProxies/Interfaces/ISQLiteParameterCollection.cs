using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite implementation of DbParameterCollection.
    /// </summary>
    public interface ISQLiteParameterCollection
    {
        /// <summary>
        /// Adds a parameter to the collection
        /// </summary>
        /// <param name="parameter">The parameter to add</param>
        /// <returns>A zero-based index of where the parameter is located in the array</returns>
        int Add(ISQLiteParameter parameter);
    }
}