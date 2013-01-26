﻿//
// NativeMethods.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.SQLIndexer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CloudApiPublic.Static
{
    internal static class NativeMethods
    {
        #region GetModuleFileName
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);
        #endregion

        #region byte array compare
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(byte[] b1, byte[] b2, UIntPtr count);
        #endregion

        #region OS info
        #region GET
        #region PRODUCT INFO
        [DllImport("Kernel32.dll")]
        public static extern bool GetProductInfo(
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
        #region shell32.dll SHChangeNotify
        /// <summary>
        /// This is the Win32 API call to force a refresh (for all icons or for a single one)
        /// </summary>
        /// <param name="wEventId">Use SHCNE_ASSOCCHANGED for all icons (makes everything blink) or SHCNE_ATTRIBUTES for one icon</param>
        /// <param name="uFlags">Check which value to use based on previous param (HChangeNotifyEventID wEventId)</param>
        /// <param name="dwItem1">Points to the single item or nothing for all icons</param>
        /// <param name="dwItem2">Points to nothing</param>
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(HChangeNotifyEventID wEventId,
            HChangeNotifyFlags uFlags,
            IntPtr dwItem1,
            IntPtr dwItem2);
        #region enum HChangeNotifyEventID
        /// <summary>
        /// Describes the event that has occurred. 
        /// Typically, only one event is specified at a time. 
        /// If more than one event is specified, the values contained 
        /// in the <i>dwItem1</i> and <i>dwItem2</i> 
        /// parameters must be the same, respectively, for all specified events. 
        /// This parameter can be one or more of the following values. 
        /// </summary>
        /// <remarks>
        /// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index 
        /// in the system image list that has changed. 
        /// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
        /// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index 
        /// in the system image list that has changed. 
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
        /// </remarks>
        [Flags]
        public enum HChangeNotifyEventID
        {
            /// <summary>
            /// All events have occurred. 
            /// </summary>
            SHCNE_ALLEVENTS = 0x7FFFFFFF,

            /// <summary>
            /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> 
            /// must be specified in the <i>uFlags</i> parameter. 
            /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>. 
            /// </summary>
            SHCNE_ASSOCCHANGED = 0x08000000,

            /// <summary>
            /// The attributes of an item or folder have changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the item or folder that has changed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
            /// </summary>
            SHCNE_ATTRIBUTES = 0x00000800,

            /// <summary>
            /// A nonfolder item has been created. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the item that was created. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
            /// </summary>
            SHCNE_CREATE = 0x00000002,

            /// <summary>
            /// A nonfolder item has been deleted. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the item that was deleted. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DELETE = 0x00000004,

            /// <summary>
            /// A drive has been added. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that was added. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DRIVEADD = 0x00000100,

            /// <summary>
            /// A drive has been added and the Shell should create a new window for the drive. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that was added. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DRIVEADDGUI = 0x00010000,

            /// <summary>
            /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that was removed.
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_DRIVEREMOVED = 0x00000080,

            /// <summary>
            /// Not currently used. 
            /// </summary>
            SHCNE_EXTENDED_EVENT = 0x04000000,

            /// <summary>
            /// The amount of free space on a drive has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_FREESPACE = 0x00040000,

            /// <summary>
            /// Storage media has been inserted into a drive. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive that contains the new media. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_MEDIAINSERTED = 0x00000020,

            /// <summary>
            /// Storage media has been removed from a drive. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the root of the drive from which the media was removed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_MEDIAREMOVED = 0x00000040,

            /// <summary>
            /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> 
            /// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that was created. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_MKDIR = 0x00000008,

            /// <summary>
            /// A folder on the local computer is being shared via the network. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that is being shared. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_NETSHARE = 0x00000200,

            /// <summary>
            /// A folder on the local computer is no longer being shared via the network. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that is no longer being shared. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_NETUNSHARE = 0x00000400,

            /// <summary>
            /// The name of a folder has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder. 
            /// <i>dwItem2</i> contains the new PIDL or name of the folder. 
            /// </summary>
            SHCNE_RENAMEFOLDER = 0x00020000,

            /// <summary>
            /// The name of a nonfolder item has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the previous PIDL or name of the item. 
            /// <i>dwItem2</i> contains the new PIDL or name of the item. 
            /// </summary>
            SHCNE_RENAMEITEM = 0x00000001,

            /// <summary>
            /// A folder has been removed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that was removed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_RMDIR = 0x00000010,

            /// <summary>
            /// The computer has disconnected from a server. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the server from which the computer was disconnected. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// </summary>
            SHCNE_SERVERDISCONNECT = 0x00004000,

            /// <summary>
            /// The contents of an existing folder have changed, 
            /// but the folder still exists and has not been renamed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
            /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
            /// <i>dwItem1</i> contains the folder that has changed. 
            /// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
            /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or 
            /// SHCNE_RENAMEFOLDER, respectively, instead. 
            /// </summary>
            SHCNE_UPDATEDIR = 0x00001000,

            /// <summary>
            /// An image in the system image list has changed. 
            /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>. 
            /// </summary>
            SHCNE_UPDATEIMAGE = 0x00008000,

            SHCNE_UPDATEITEM = 0x00002000,

        }
        #endregion // enum HChangeNotifyEventID
        #region public enum HChangeNotifyFlags
        /// <summary>
        /// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters. 
        /// The uFlags parameter must be one of the following values.
        /// </summary>
        [Flags]
        public enum HChangeNotifyFlags
        {
            /// <summary>
            /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values. 
            /// </summary>
            SHCNF_DWORD = 0x0003,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that 
            /// represent the item(s) affected by the change. 
            /// Each ITEMIDLIST must be relative to the desktop folder. 
            /// </summary>
            SHCNF_IDLIST = 0x0000,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of 
            /// maximum length MAX_PATH that contain the full path names 
            /// of the items affected by the change. 
            /// </summary>
            SHCNF_PATHA = 0x0001,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of 
            /// maximum length MAX_PATH that contain the full path names 
            /// of the items affected by the change. 
            /// </summary>
            SHCNF_PATHW = 0x0005,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that 
            /// represent the friendly names of the printer(s) affected by the change. 
            /// </summary>
            SHCNF_PRINTERA = 0x0002,
            /// <summary>
            /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that 
            /// represent the friendly names of the printer(s) affected by the change. 
            /// </summary>
            SHCNF_PRINTERW = 0x0006,
            /// <summary>
            /// The function should not return until the notification 
            /// has been delivered to all affected components. 
            /// As this flag modifies other data-type flags, it cannot by used by itself.
            /// </summary>
            SHCNF_FLUSH = 0x1000,
            /// <summary>
            /// The function should begin delivering notifications to all affected components 
            /// but should return as soon as the notification process has begun. 
            /// As this flag modifies other data-type flags, it cannot by used by itself.
            /// </summary>
            SHCNF_FLUSHNOWAIT = 0x2000
        }
        #endregion // enum HChangeNotifyFlags
        #endregion
;
        // Support for SQLIndexer
        #region find file

        public const int MAX_PATH = 260;
        public const uint MaxDWORD = 4294967295;

        /// <summary>
        /// Win32 FILETIME structure.  The win32 documentation says this:
        /// "Contains a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601 (UTC)."
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/ms724284%28VS.85%29.aspx"/>
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        /// <summary>
        /// The Win32 find data structure.  The documentation says:
        /// "Contains information about the file that is found by the FindFirstFile, FindFirstFileEx, or FindNextFile function."
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa365740%28VS.85%29.aspx"/>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        /// <summary>
        /// Searches a directory for a file or subdirectory with a name that matches a specific name (or partial name if wildcards are used).
        /// </summary>
        /// <param name="lpFileName">The directory or path, and the file name, which can include wildcard characters, for example, an asterisk (*) or a question mark (?). </param>
        /// <param name="lpFindData">A pointer to the WIN32_FIND_DATA structure that receives information about a found file or directory.</param>
        /// <returns>
        /// If the function succeeds, the return value is a search handle used in a subsequent call to FindNextFile or FindClose, and the lpFindFileData parameter contains information about the first file or directory found.
        /// If the function fails or fails to locate files from the search string in the lpFileName parameter, the return value is INVALID_HANDLE_VALUE and the contents of lpFindFileData are indeterminate.
        ///</returns>
        ///<see cref="http://msdn.microsoft.com/en-us/library/aa364418%28VS.85%29.aspx"/>
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeSearchHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindData);

        #region find first file extended
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeSearchHandle FindFirstFileEx(
            string lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            FINDEX_ADDITIONAL_FLAGS dwAdditionalFlags);

        public enum FINDEX_INFO_LEVELS : int
        {
            FindExInfoStandard = 0,
            /// <summary>
            /// Not supported until Windows 7
            /// </summary>
            FindExInfoBasic = 1
        }

        public enum FINDEX_SEARCH_OPS : int
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            /// <summary>
            /// Not supported anywhere
            /// </summary>
            FindExSearchLimitToDevices = 2
        }

        [Flags]
        public enum FINDEX_ADDITIONAL_FLAGS : int
        {
            None = 0x00,
            FIND_FIRST_EX_CASE_SENSITIVE = 0x01,
            FIND_FIRST_EX_LARGE_FETCH = 0x02
        }
        #endregion

        /// <summary>
        /// Continues a file search from a previous call to the FindFirstFile or FindFirstFileEx function.
        /// </summary>
        /// <param name="hFindFile">The search handle returned by a previous call to the FindFirstFile or FindFirstFileEx function.</param>
        /// <param name="lpFindData">A pointer to the WIN32_FIND_DATA structure that receives information about the found file or subdirectory.
        /// The structure can be used in subsequent calls to FindNextFile to indicate from which file to continue the search.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero and the lpFindFileData parameter contains information about the next file or directory found.
        /// If the function fails, the return value is zero and the contents of lpFindFileData are indeterminate.
        /// </returns>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa364428%28VS.85%29.aspx"/>
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FindNextFile(SafeSearchHandle hFindFile, out WIN32_FIND_DATA lpFindData);

        /// <summary>
        /// Closes a file search handle opened by the FindFirstFile, FindFirstFileEx, or FindFirstStreamW function.
        /// </summary>
        /// <param name="hFindFile">The file search handle.</param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. 
        /// </returns>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa364413%28VS.85%29.aspx"/>
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);
        #endregion

        #region network monitoring
        [StructLayout(LayoutKind.Sequential)]
        public sealed class WSAData
        {
            public Int16 wVersion;
            public Int16 wHighVersion;
            public String szDescription;
            public String szSystemStatus;
            public Int16 iMaxSockets;
            public Int16 iMaxUdpDg;
            public IntPtr lpVendorInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class WSAQUERYSET
        {
            public Int32 dwSize = 0;
            public String szServiceInstanceName = null;
            public IntPtr lpServiceClassId;
            public IntPtr lpVersion;
            public String lpszComment;
            public Int32 dwNameSpace;
            public IntPtr lpNSProviderId;
            public String lpszContext;
            public Int32 dwNumberOfProtocols;
            public IntPtr lpafpProtocols;
            public String lpszQueryString;
            public Int32 dwNumberOfCsAddrs;
            public IntPtr lpcsaBuffer;
            public Int32 dwOutputFlags;
            public IntPtr lpBlob;
        }

        [StructLayout(LayoutKind.Sequential)]
        public sealed class BLOB_INDIRECTION
        {
            public UInt32 cbSize;
            public IntPtr pInfo;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        public sealed class NLA_BLOB
        {
            [FieldOffset(0)]
            public NLA_Header header;

            // [FieldOffset(12)]
            // public UNIONED_DATA unionData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct NLA_Header
        {
            public NLA_BLOB_DATA_TYPE type;
            public Int32 dwSize;
            public Int32 nextOffset;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        public struct NLA_BLOB_CONNECTIVITY
        {
            [FieldOffset(0)]
            public NLA_Header header;

            [FieldOffset(12)]
            public NLA_Connectivity connectivity;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct NLA_Connectivity
        {
            public UInt32 type;
            public NLA_INTERNET internet;
        }

        public enum NLA_BLOB_DATA_TYPE : uint
        {
            NLA_RAW_DATA = 0,
            NLA_INTERFACE = 1,
            NLA_802_1X_LOCATION = 2,
            NLA_CONNECTIVITY = 3,
            NLA_ICS = 4
        }

        public enum NLA_INTERNET : uint
        {
            NLA_INTERNET_UNKNOWN = 0,
            NLA_INTERNET_NO = 1,
            NLA_INTERNET_YES = 2
        }

        [DllImport("Ws2_32.DLL", CharSet = CharSet.Auto,
        SetLastError = true)]
        public extern static
          Int32 WSAStartup(Int16 wVersionRequested, WSAData wsaData);

        [DllImport("Ws2_32.DLL", CharSet = CharSet.Auto,
        SetLastError = true)]
        public extern static
          Int32 WSACleanup();

        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static
          Int32 WSALookupServiceBegin(WSAQUERYSET qsRestrictions,
            Int32 dwControlFlags, ref Int32 lphLookup);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static
          Int32 WSALookupServiceNext(Int32 hLookup,
            Int32 dwControlFlags,
            ref Int32 lpdwBufferLength,
            IntPtr pqsResults);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static
          Int32 WSALookupServiceEnd(Int32 hLookup);

        #region network event
        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static Int32 WSANSPIoctl(
          Int32 hLookup,
          UInt32 dwControlCode,
          IntPtr lpvInBuffer,
          Int32 cbInBuffer,
          IntPtr lpvOutBuffer,
          Int32 cbOutBuffer,
          ref Int32 lpcbBytesReturned,
          IntPtr lpCompletion);
        #endregion
        #endregion
    }
}