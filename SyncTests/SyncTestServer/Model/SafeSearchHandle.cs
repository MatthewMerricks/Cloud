using Microsoft.Win32.SafeHandles;
using SyncTestServer.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace SyncTestServer.Model
{
    [SecurityCritical]
    /// <summary>
    /// Class to encapsulate a seach handle returned from FindFirstFile or FindFirstFileEx.  Using a wrapper
    /// like this ensures that the handle is properly cleaned up with FindClose.
    /// </summary>
    internal class SafeSearchHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityCritical]
        public SafeSearchHandle() : base(true) { }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.FindClose(base.handle);
        }
    }
}