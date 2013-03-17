//
// SQLiteParameterCollection32.cs
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
    extern alias SQL32;

    /// <summary>
    /// SQLite implementation of DbParameterCollection.
    /// </summary>
    internal sealed class SQLiteParameterCollection32 : ISQLiteParameterCollection
    {
        private readonly SQL32.System.Data.SQLite.SQLiteParameterCollection baseObject;

        internal SQLiteParameterCollection32(SQL32.System.Data.SQLite.SQLiteParameterCollection baseObject)
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
            SQLiteParameter32 castParameter;
            if (parameter == null)
            {
                castParameter = null;
            }
            else
            {
                castParameter = parameter as SQLiteParameter32;
                if (castParameter == null)
                {
                    throw new ArgumentException("parameter not castable as SQLiteParameter32");
                }
            }
            try
            {
                return baseObject.Add((SQL32.System.Data.SQLite.SQLiteParameter)castParameter);
            }
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
            }
        }
        #endregion
    }
}