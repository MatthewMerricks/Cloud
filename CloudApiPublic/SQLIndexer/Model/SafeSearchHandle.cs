//
// SafeSearchHandle.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Microsoft.Win32.SafeHandles;
using CloudApiPublic.SQLIndexer.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace CloudApiPublic.SQLIndexer.Model
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