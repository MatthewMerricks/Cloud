using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.SQLProxies
{
    extern alias SQL32;

    /// <summary>
    /// SQLite implementation of DbTransaction.
    /// </summary>
    internal sealed class SQLiteTransaction32 : DisposableProxyObject, ISQLiteTransaction
    {
        private readonly SQL32.System.Data.SQLite.SQLiteTransaction baseObject;

        protected override IDisposable BaseDisposable
        {
            get
            {
                return baseObject;
            }
        }

        internal SQLiteTransaction32(SQL32.System.Data.SQLite.SQLiteTransaction baseObject)
        {
            this.baseObject = baseObject;
        }

        public static explicit operator SQL32.System.Data.SQLite.SQLiteTransaction(SQLiteTransaction32 thisObject)
        {
            if (thisObject == null)
            {
                return null;
            }
            return thisObject.baseObject;
        }

        #region ISQLiteTransaction members
        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        void ISQLiteTransaction.Commit()
        {
            base.CheckDisposed();
            try
            {
                baseObject.Commit();
            }
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
            }
        }

        /// <summary>
        /// Rolls back the active transaction.
        /// </summary>
        void ISQLiteTransaction.Rollback()
        {
            base.CheckDisposed();
            try
            {
                baseObject.Rollback();
            }
            catch (SQL32.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException32(ex);
            }
        }
        #endregion
    }
}