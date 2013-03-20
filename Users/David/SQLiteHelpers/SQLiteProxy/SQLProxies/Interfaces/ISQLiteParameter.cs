using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite implementation of DbParameter.
    /// </summary>
    public interface ISQLiteParameter
    {
        /// <summary>
        /// Gets and sets the parameter value. If no datatype was specified, the datatype
        /// will assume the type from the value given.
        /// </summary>
        object Value { get; set; }
    }
}