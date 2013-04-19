//
// DisposableProxyObject.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cloud.SQLProxies
{
    internal abstract class DisposableProxyObject : IDisposable
    {
        /// <summary>
        /// Implement an override for this property which gets the baseObject which implements IDisposable
        /// </summary>
        protected abstract IDisposable BaseDisposable { get; }

        internal protected DisposableProxyObject() { }

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
        private bool _disposed = false;

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
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    // Dispose managed resources.
                    BaseDisposable.Dispose();
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here. 
                // If disposing is false, 
                // only the following code is executed.

                /* [ My code here ] */

                // Note disposing has been done.
                _disposed = true;
            }
        }

        // Use C# destructor syntax for finalization code. 
        // This destructor will run only if the Dispose method 
        // does not get called. 
        // It gives your base class the opportunity to finalize. 
        // Do not provide destructors in types derived from this class.
        ~DisposableProxyObject()
        {
            // Do not re-create Dispose clean-up code here. 
            // Calling Dispose(false) is optimal in terms of 
            // readability and maintainability.
            Dispose(false);
        }
        #endregion
    }
}