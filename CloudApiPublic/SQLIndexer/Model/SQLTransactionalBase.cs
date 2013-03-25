//
// SQLTransactionalBase.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.SQLProxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cloud.SQLIndexer.Model
{
    /// <summary>
    /// Must be implemented internal to IndexingAgent alone
    /// </summary>
    internal abstract class SQLTransactionalBase : IDisposable
    {
        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        public abstract void Commit();

        #region IDisposable members
        protected void CheckDisposed([CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(filePath == null && memberName == null
                    ? "Code was not compiled with C# 5 or greater (VS 2012 or greater)"
                    : (filePath == null ? string.Empty : (AfterLastBackslash(filePath) + (memberName == null ? string.Empty : " "))) + (memberName == null ? string.Empty : memberName));
            }
        }
        private static string AfterLastBackslash(string filePath)
        {
            string[] filePathSplit = filePath.Split('\\');
            return filePathSplit[filePathSplit.Length - 1];
        }

        // Track whether Dispose has been called. 
        protected abstract bool _disposed { get; }

        // Implement IDisposable. 
        // Do not make this method virtual. 
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            // Pass on the Dispose to the protected virtual helper with 'true' to dispose all managed and unmanaged resources.
            Dispose(true);

            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. 
        // If disposing equals true, the method has been called directly 
        // or indirectly by a user's code. Managed and unmanaged resources 
        // can be disposed. 
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        protected abstract void Dispose(bool disposing);

        // Use C# destructor syntax for finalization code. 
        // This destructor will run only if the Dispose method 
        // does not get called. 
        // It gives your base class the opportunity to finalize. 
        // Do not provide destructors in types derived from this class.
        ~SQLTransactionalBase()
        {
            // Do not re-create Dispose clean-up code here. 
            // Calling Dispose(false) is optimal in terms of 
            // readability and maintainability.
            Dispose(false);
        }
        #endregion
    }
}