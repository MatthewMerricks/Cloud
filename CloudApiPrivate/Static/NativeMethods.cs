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

namespace CloudApiPrivate.Static
{
    internal static class NativeMethods
    {
        // For convenience's sake, I'm using the WritePrivateProfileString
        // Win32 API function here. Feel free to write your own .ini file
        // writing function if you wish.
        [DllImport("kernel32")]
        public static extern int WritePrivateProfileString(
                string iniSection,
                string iniKey,
                string iniValue,
                string iniFilePath);
    }
}