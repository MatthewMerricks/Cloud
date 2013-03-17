//
// SQLiteParameterCollection64.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    extern alias SQL64;

    /// <summary>
    /// SQLite implementation of DbParameterCollection.
    /// </summary>
    internal sealed class SQLiteParameterCollection64 : ISQLiteParameterCollection
    {
        private readonly SQL64.System.Data.SQLite.SQLiteParameterCollection baseObject;

        internal SQLiteParameterCollection64(SQL64.System.Data.SQLite.SQLiteParameterCollection baseObject)
        {
            this.baseObject = baseObject;
        }

        #region ISQLiteParameterCollection members
        /// <summary>
        /// Adds a parameter to the collection
        /// </summary>
        /// <param name="parameter">The parameter to add</param>
        /// <returns>A zero-based index of where the parameter is located in the array</returns>
        int ISQLiteParameterCollection.Add(ISQLiteParameter parameter)
        {
            SQLiteParameter64 castParameter;
            if (parameter == null)
            {
                castParameter = null;
            }
            else
            {
                castParameter = parameter as SQLiteParameter64;
                if (castParameter == null)
                {
                    throw new ArgumentException("parameter cannot be cast as SQLiteParameter64");
                }
            }
            try
            {
                return baseObject.Add((SQL64.System.Data.SQLite.SQLiteParameter)castParameter);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }
        #endregion
    }
}