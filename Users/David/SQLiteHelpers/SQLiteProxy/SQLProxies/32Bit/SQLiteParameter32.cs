using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    extern alias SQL32;

    /// <summary>
    /// SQLite implementation of DbParameter.
    /// </summary>
    internal sealed class SQLiteParameter32 : ISQLiteParameter
    {
        private readonly SQL32.System.Data.SQLite.SQLiteParameter baseObject;

        internal SQLiteParameter32(SQL32.System.Data.SQLite.SQLiteParameter baseObject)
        {
            this.baseObject = baseObject;
        }

        public static explicit operator SQL32.System.Data.SQLite.SQLiteParameter(SQLiteParameter32 thisObject)
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
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
                }
            }
            set
            {
                try
                {
                    baseObject.Value = value;
                }
                catch (SQL32.System.Data.SQLite.SQLiteException ex)
                {
                    throw new SQLiteException32(ex);
                }
            }
        }
        #endregion
    }
}