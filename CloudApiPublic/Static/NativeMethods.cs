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

namespace CloudApiPublic.Static
{
    internal static class NativeMethods
    {
        #region byte array compare
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(byte[] b1, byte[] b2, UIntPtr count);
        #endregion

        #region OS info
        #region GET
        #region PRODUCT INFO
        [DllImport("Kernel32.dll")]
        internal static extern bool GetProductInfo(
            int osMajorVersion,
            int osMinorVersion,
            int spMajorVersion,
            int spMinorVersion,
            out int edition);
        #endregion PRODUCT INFO

        #region VERSION
        [DllImport("kernel32.dll")]
        public static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);
        #endregion VERSION
        #endregion GET

        #region OSVERSIONINFOEX
        [StructLayout(LayoutKind.Sequential)]
        public struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
        #endregion OSVERSIONINFOEX

        #region PRODUCT
        public const int PRODUCT_UNDEFINED = 0x00000000;
        public const int PRODUCT_ULTIMATE = 0x00000001;
        public const int PRODUCT_HOME_BASIC = 0x00000002;
        public const int PRODUCT_HOME_PREMIUM = 0x00000003;
        public const int PRODUCT_ENTERPRISE = 0x00000004;
        public const int PRODUCT_HOME_BASIC_N = 0x00000005;
        public const int PRODUCT_BUSINESS = 0x00000006;
        public const int PRODUCT_STANDARD_SERVER = 0x00000007;
        public const int PRODUCT_DATACENTER_SERVER = 0x00000008;
        public const int PRODUCT_SMALLBUSINESS_SERVER = 0x00000009;
        public const int PRODUCT_ENTERPRISE_SERVER = 0x0000000A;
        public const int PRODUCT_STARTER = 0x0000000B;
        public const int PRODUCT_DATACENTER_SERVER_CORE = 0x0000000C;
        public const int PRODUCT_STANDARD_SERVER_CORE = 0x0000000D;
        public const int PRODUCT_ENTERPRISE_SERVER_CORE = 0x0000000E;
        public const int PRODUCT_ENTERPRISE_SERVER_IA64 = 0x0000000F;
        public const int PRODUCT_BUSINESS_N = 0x00000010;
        public const int PRODUCT_WEB_SERVER = 0x00000011;
        public const int PRODUCT_CLUSTER_SERVER = 0x00000012;
        public const int PRODUCT_HOME_SERVER = 0x00000013;
        public const int PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014;
        public const int PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015;
        public const int PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016;
        public const int PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017;
        public const int PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018;
        public const int PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x00000019;
        public const int PRODUCT_HOME_PREMIUM_N = 0x0000001A;
        public const int PRODUCT_ENTERPRISE_N = 0x0000001B;
        public const int PRODUCT_ULTIMATE_N = 0x0000001C;
        public const int PRODUCT_WEB_SERVER_CORE = 0x0000001D;
        public const int PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x0000001E;
        public const int PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x0000001F;
        public const int PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x00000020;
        public const int PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x00000023;
        public const int PRODUCT_STANDARD_SERVER_V = 0x00000024;
        public const int PRODUCT_ENTERPRISE_SERVER_V = 0x00000026;
        public const int PRODUCT_STANDARD_SERVER_CORE_V = 0x00000028;
        public const int PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x00000029;
        public const int PRODUCT_HYPERV = 0x0000002A;
        #endregion PRODUCT

        #region VERSIONS
        public const int VER_NT_WORKSTATION = 1;
        public const int VER_NT_DOMAIN_CONTROLLER = 2;
        public const int VER_NT_SERVER = 3;
        public const int VER_SUITE_SMALLBUSINESS = 1;
        public const int VER_SUITE_ENTERPRISE = 2;
        public const int VER_SUITE_TERMINAL = 16;
        public const int VER_SUITE_DATACENTER = 128;
        public const int VER_SUITE_SINGLEUSERTS = 256;
        public const int VER_SUITE_PERSONAL = 512;
        public const int VER_SUITE_BLADE = 1024;
        #endregion VERSIONS
        #endregion

        #region client to screen
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x = 0;
            public int y = 0;
        }
        [DllImport("user32.dll", EntryPoint = "ClientToScreen", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int ClientToScreen(IntPtr hWnd, [In, Out] POINT pt);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos([Out] POINT lpPoint);

        #endregion
    }
}