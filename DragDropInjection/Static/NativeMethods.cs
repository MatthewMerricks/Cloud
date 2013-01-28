//
// NativeMethods.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DragDropInjection.Static
{
    internal static class NativeMethods
    {
        //TODO: Needed? [UIPermissionAttribute(SecurityAction.Demand, Clipboard = UIPermissionClipboard.OwnClipboard)]
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate Int32 DDoDragDrop(
            IntPtr InData,
            IntPtr InDropSource,
            UInt32 InOkEffects,
            out UInt32[] OutEffect);

        // just use a P-Invoke implementation to get native API access from C# (this step is not necessary for C++.NET)
        [DllImport("ole32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 DoDragDrop(
            IntPtr InData,
            IntPtr InDropSource,
            UInt32 InOkEffects,
            out UInt32[] OutEffect);
    }
}