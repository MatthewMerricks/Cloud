using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    extern alias SQL64;

    /// <summary>
    /// SQLite implementation of DbParameter.
    /// </summary>
    internal sealed class SQLiteParameter64 : ISQLiteParameter
    {
        private readonly SQL64.System.Data.SQLite.SQLiteParameter baseObject;

        internal SQLiteParameter64(SQL64.System.Data.SQLite.SQLiteParameter baseObject)
        {
            this.baseObject = baseObject;
        }

        public static explicit operator SQL64.System.Data.SQLite.SQLiteParameter(SQLiteParameter64 thisObject)
        {
            if (thisObject == null)
            {
                return null;
            }
            return thisObject.baseObject;
        }

        #region ISQLiteParameter members
        /// <summary>
        /// Gets and sets the parameter value. If no datatype was specified, the datatype
        /// will assume the type from the value given.
        /// </summary>
        object ISQLiteParameter.Value
        {
            get
            {
                try
                {
                    return baseObject.Value;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
            }
            set
            {
                try
                {
                    baseObject.Value = value;
                }
                catch (SQL64.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException64(ex);
                }
            }
        }
        #endregion
    }
}