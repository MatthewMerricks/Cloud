//
// NativeMethods.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

#region Using directives
using Cloud.SQLIndexer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;                             
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Reflection;
#endregion

namespace Cloud.Static
{
    internal static class NativeMethods
    {
        #region GetModuleFileName
        public const string core = "core.dll";
        public const string msvcrt = "msvcrt.dll";

        [DllImport(core, SetLastError = true)]
        public static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);
        #endregion

        #region byte array compare
        [DllImport(msvcrt, CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(byte[] b1, byte[] b2, UIntPtr count);
        #endregion

        #region OS info
        #region GET
        #region PRODUCT INFO
        [DllImport(kernel32)]
        public static extern bool GetProductInfo(
            int osMajorVersion,
            int osMinorVersion,
            int spMajorVersion,
            int spMinorVersion,
            out int edition);
        #endregion PRODUCT INFO

        #region VERSION
        [DllImport(kernel32)]
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

        public const string user32 = "user32.dll";
        public const string shell32 = "shell32.dll";
        public const string clienttostring = "ClientToScreen";
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x = 0;
            public int y = 0;
        }
        [DllImport(user32, EntryPoint = clienttostring, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int ClientToScreen(IntPtr hWnd, [In, Out] POINT pt);

        [DllImport(user32)]
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
        [DllImport(shell32)]
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
        };
        #endregion // enum HChangeNotifyFlags
        #endregion
        // Support for SQLIndexer
        #region find file

        public const int MAX_PATH = 260;
        public const uint MaxDWORD = 4294967295;
        public const string kernel32 = "kernel32.dll";
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
        [DllImport(kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeSearchHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindData);

        #region find first file extended
        [DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
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
        [DllImport(kernel32, CharSet = CharSet.Auto, SetLastError = true)]
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
        [DllImport(kernel32, SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);
        #endregion

        // CreateFile along with extra attribute allowing locking a folder
        #region CreateFile
        [DllImport(kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flags,
            IntPtr template);

        public const int FileAttributesFileFlagBackupSemantics = 0x02000000;
        #endregion

        #region network monitoring

        private const string WinSock2_32dll = "Ws2_32.dll";

        [StructLayout(LayoutKind.Sequential)]
        public struct WSAData
        {
            public Int16 version;
            public Int16 highVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            public String description;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            public String systemStatus;

            public Int16 maxSockets;
            public Int16 maxUdpDg;
            public IntPtr vendorInfo;
        }

        [Obfuscation(Feature="preserve-name-binding")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class WSAQUERYSET
        {
            [Obfuscation(Feature = "preserve-name-binding")]
            public Int32 dwSize = 0;
            [Obfuscation(Feature = "preserve-name-binding")]
            public String szServiceInstanceName = null;
            [Obfuscation(Feature = "preserve-name-binding")]
            public IntPtr lpServiceClassId;
            [Obfuscation(Feature = "preserve-name-binding")]
            public IntPtr lpVersion;
            [Obfuscation(Feature = "preserve-name-binding")]
            public String lpszComment;
            [Obfuscation(Feature = "preserve-name-binding")]
            public NAMESPACE_PROVIDER_PTYPE dwNameSpace;
            [Obfuscation(Feature = "preserve-name-binding")]
            public IntPtr lpNSProviderId;
            [Obfuscation(Feature = "preserve-name-binding")]
            public String lpszContext;
            [Obfuscation(Feature = "preserve-name-binding")]
            public Int32 dwNumberOfProtocols;
            [Obfuscation(Feature = "preserve-name-binding")]
            public IntPtr lpafpProtocols;
            [Obfuscation(Feature = "preserve-name-binding")]
            public String lpszQueryString;
            [Obfuscation(Feature = "preserve-name-binding")]
            public Int32 dwNumberOfCsAddrs;
            [Obfuscation(Feature = "preserve-name-binding")]
            public IntPtr lpcsaBuffer;
            [Obfuscation(Feature = "preserve-name-binding")]
            public Int32 dwOutputFlags;
            [Obfuscation(Feature = "preserve-name-binding")]
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
            public UInt32 dwSize;
            public UInt32 nextOffset;
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

        public enum NAMESPACE_PROVIDER_PTYPE : uint
        {
            NS_DNS = 12,
            NS_NLA = 15,
            NS_BTH = 16,
            NS_NTDS = 32,
            NS_EMAIL = 37,
            NS_PNRPNAME = 38,
            NS_PNRPCLOUD = 39
        }

        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static Int32 WSAStartup(Int16 wVersionRequested, out WSAData wsaData);

        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static Int32 WSACleanup();

        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static
            Int32 WSALookupServiceBegin(WSAQUERYSET qsRestrictions,
                Int32 dwControlFlags, ref IntPtr lphLookup);

        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static
            Int32 WSALookupServiceNext(IntPtr hLookup,
                Int32 dwControlFlags,
                ref Int32 lpdwBufferLength,
                IntPtr pqsResults);

        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static
            Int32 WSALookupServiceEnd(IntPtr hLookup);

        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static Int32 WSAGetLastError();


        [StructLayoutAttribute(LayoutKind.Sequential)]
        public sealed class OVERLAPPED
        {
            public UIntPtr Internal;
            public UIntPtr InternalHigh;
            public Anonymous_7416d31a_1ce9_4e50_b1e1_0f2ad25c0196 Union1;
            public IntPtr hEvent;
        }

        [StructLayoutAttribute(LayoutKind.Explicit)]
        public struct Anonymous_7416d31a_1ce9_4e50_b1e1_0f2ad25c0196
        {
            [FieldOffsetAttribute(0)]
            public Anonymous_ac6e4301_4438_458f_96dd_e86faeeca2a6 Struct1;

            [FieldOffsetAttribute(0)]
            public IntPtr Pointer;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct Anonymous_ac6e4301_4438_458f_96dd_e86faeeca2a6
        {
            public uint Offset;
            public uint OffsetHigh;
        }



        public enum WSACOMPLETIONTYPE : uint
        {
            NSP_NOTIFY_IMMEDIATELY = 0,
            NSP_NOTIFY_HWND,
            NSP_NOTIFY_EVENT,
            NSP_NOTIFY_PORT,
            NSP_NOTIFY_APC,
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public sealed class WSACOMPLETION
        {
            public WSACOMPLETIONTYPE Type;
            public Anonymous_1773BCC2_3C29_4525_8847_432883EBCA5D Parameters;
        }

        [StructLayoutAttribute(LayoutKind.Explicit)]
        public struct Anonymous_1773BCC2_3C29_4525_8847_432883EBCA5D
        {
            [FieldOffsetAttribute(0)]
            public Anonymous_8ADAE2D7_95D3_4AC9_8922_D0C1D761BE43 WindowMessage;

            [FieldOffsetAttribute(0)]
            public Anonymous_2BBEA3C4_CD78_401E_8739_091AF8D0AE8A Event;

            [FieldOffsetAttribute(0)]
            public Anonymous_7A1561CB_C42C_463B_B7A1_EEAA812AF258 Apc;

            [FieldOffsetAttribute(0)]
            public Anonymous_EC48E59D_9E58_45E9_A59F_E0B7F0795DFB Port;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct Anonymous_EC48E59D_9E58_45E9_A59F_E0B7F0795DFB
        {
            public IntPtr lpOverlapped; // OVERLAPPED*
            public IntPtr hPort; // HANDLE
            public UIntPtr Key; // ULONG_PTR 
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct Anonymous_7A1561CB_C42C_463B_B7A1_EEAA812AF258
        {
            public IntPtr lpOverlapped; // WSAOVERLAPPED*
            public IntPtr lpfnCompletionProc; // LPOVERLAPPED_COMPLETION_ROUTINE lpfnCompletionProc (C# delegate?)
        }

        // I don't think this is possible
        //public delegate void LPOVERLAPPED_COMPLETION_ROUTINE(uint dwErrorCode, uint dwNumberOfBytesTransfered, ref OVERLAPPED lpOverlapped);

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct Anonymous_8ADAE2D7_95D3_4AC9_8922_D0C1D761BE43
        {
            public HWND hWnd;
            public uint uMsg;
            public UIntPtr context;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct HWND
        {
            public int unused;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct Anonymous_2BBEA3C4_CD78_401E_8739_091AF8D0AE8A
        {
            public IntPtr lpOverlapped; // OVERLAPPED*
        }



        public const UInt32 SIO_NSP_NOTIFY_CHANGE = 0x88000019; //_WSAIOW(IOC_WS2,25) -> (IOC_IN|(x)|(y)) -> 0x80000000 | 0x08000000 | 25
        public const int LUP_RETURN_BLOB = 0x0200;

        #region WSAErrors

        public enum WinSockErrors : int
        {
            SOCKET_ERROR = -1,
            /// <Summary>An application attempts to use an event object, but the specified handle is not valid. Note that this error is returned by the operating system, so the error number may change in future releases of Windows.</Summary>
            WSA_INVALID_HANDLE = 6,
            /// <Summary>Insufficient memory available.</Summary>
            WSA_NOT_ENOUGH_MEMORY = 8,
            /// <Summary>One or more parameters are invalid.</Summary>
            WSA_INVALID_PARAMETER = 87,
            /// <Summary>Overlapped operation aborted.</Summary>
            WSA_OPERATION_ABORTED = 995,
            /// <Summary>Overlapped I/O event object not in signaled state.</Summary>
            WSA_IO_INCOMPLETE = 996,
            /// <Summary>Overlapped operations will complete later.</Summary>
            WSA_IO_PENDING = 997,
            /// <Summary>Interrupted function call.</Summary>
            WSAEINTR = 10004,
            /// <Summary>File handle is not valid.</Summary>
            WSAEBADF = 10009,
            /// <Summary>Permission denied.</Summary>
            WSAEACCES = 10013,
            /// <Summary>Bad address.</Summary>
            WSAEFAULT = 10014,
            /// <Summary>Invalid argument.</Summary>
            WSAEINVAL = 10022,
            /// <Summary>Too many open files.</Summary>
            WSAEMFILE = 10024,
            /// <Summary>Resource temporarily unavailable.</Summary>
            WSAEWOULDBLOCK = 10035,
            /// <Summary>Operation now in progress.</Summary>
            WSAEINPROGRESS = 10036,
            /// <Summary>Operation already in progress.</Summary>
            WSAEALREADY = 10037,
            /// <Summary>Socket operation on nonsocket.</Summary>
            WSAENOTSOCK = 10038,
            /// <Summary>Destination address required.</Summary>
            WSAEDESTADDRREQ = 10039,
            /// <Summary>Message too long.</Summary>
            WSAEMSGSIZE = 10040,
            /// <Summary>Protocol wrong type for socket.</Summary>
            WSAEPROTOTYPE = 10041,
            /// <Summary>Bad protocol option.</Summary>
            WSAENOPROTOOPT = 10042,
            /// <Summary>Protocol not supported.</Summary>
            WSAEPROTONOSUPPORT = 10043,
            /// <Summary>Socket type not supported.</Summary>
            WSAESOCKTNOSUPPORT = 10044,
            /// <Summary>Operation not supported.</Summary>
            WSAEOPNOTSUPP = 10045,
            /// <Summary>Protocol family not supported.</Summary>
            WSAEPFNOSUPPORT = 10046,
            /// <Summary>Address family not supported by protocol family.</Summary>
            WSAEAFNOSUPPORT = 10047,
            /// <Summary>Address already in use.</Summary>
            WSAEADDRINUSE = 10048,
            /// <Summary>Cannot assign requested address.</Summary>
            WSAEADDRNOTAVAIL = 10049,
            /// <Summary>Network is down.</Summary>
            WSAENETDOWN = 10050,
            /// <Summary>Network is unreachable.</Summary>
            WSAENETUNREACH = 10051,
            /// <Summary>Network dropped connection on reset.</Summary>
            WSAENETRESET = 10052,
            /// <Summary>Software caused connection abort.</Summary>
            WSAECONNABORTED = 10053,
            /// <Summary>Connection reset by peer.</Summary>
            WSAECONNRESET = 10054,
            /// <Summary>No buffer space available.</Summary>
            WSAENOBUFS = 10055,
            /// <Summary>Socket is already connected.</Summary>
            WSAEISCONN = 10056,
            /// <Summary>Socket is not connected.</Summary>
            WSAENOTCONN = 10057,
            /// <Summary>Cannot send after socket shutdown.</Summary>
            WSAESHUTDOWN = 10058,
            /// <Summary>Too many references.</Summary>
            WSAETOOMANYREFS = 10059,
            /// <Summary>Connection timed out.</Summary>
            WSAETIMEDOUT = 10060,
            /// <Summary>Connection refused.</Summary>
            WSAECONNREFUSED = 10061,
            /// <Summary>Cannot translate name.</Summary>
            WSAELOOP = 10062,
            /// <Summary>Name too long.</Summary>
            WSAENAMETOOLONG = 10063,
            /// <Summary>Host is down.</Summary>
            WSAEHOSTDOWN = 10064,
            /// <Summary>No route to host.</Summary>
            WSAEHOSTUNREACH = 10065,
            /// <Summary>Directory not empty.</Summary>
            WSAENOTEMPTY = 10066,
            /// <Summary>Too many processes.</Summary>
            WSAEPROCLIM = 10067,
            /// <Summary>User quota exceeded.</Summary>
            WSAEUSERS = 10068,
            /// <Summary>Disk quota exceeded.</Summary>
            WSAEDQUOT = 10069,
            /// <Summary>Stale file handle reference.</Summary>
            WSAESTALE = 10070,
            /// <Summary>Item is remote.</Summary>
            WSAEREMOTE = 10071,
            /// <Summary>Network subsystem is unavailable.</Summary>
            WSASYSNOTREADY = 10091,
            /// <Summary>Winsock.dll version out of range.</Summary>
            WSAVERNOTSUPPORTED = 10092,
            /// <Summary>Successful WSAStartup not yet performed.</Summary>
            WSANOTINITIALISED = 10093,
            /// <Summary>Graceful shutdown in progress.</Summary>
            WSAEDISCON = 10101,
            /// <Summary>No more results.</Summary>
            WSAENOMORE = 10102,
            /// <Summary>Call has been canceled.</Summary>
            WSAECANCELLED = 10103,
            /// <Summary>Procedure call table is invalid.</Summary>
            WSAEINVALIDPROCTABLE = 10104,
            /// <Summary>Service provider is invalid.</Summary>
            WSAEINVALIDPROVIDER = 10105,
            /// <Summary>Service provider failed to initialize.</Summary>
            WSAEPROVIDERFAILEDINIT = 10106,
            /// <Summary>System call failure.</Summary>
            WSASYSCALLFAILURE = 10107,
            /// <Summary>Service not found.</Summary>
            WSASERVICE_NOT_FOUND = 10108,
            /// <Summary>Class type not found.</Summary>
            WSATYPE_NOT_FOUND = 10109,
            /// <Summary>No more results.</Summary>
            WSA_E_NO_MORE = 10110,
            /// <Summary>Call was canceled.</Summary>
            WSA_E_CANCELLED = 10111,
            /// <Summary>Database query was refused.</Summary>
            WSAEREFUSED = 10112,
            /// <Summary>Host not found.</Summary>
            WSAHOST_NOT_FOUND = 11001,
            /// <Summary>Nonauthoritative host not found.</Summary>
            WSATRY_AGAIN = 11002,
            /// <Summary>This is a nonrecoverable error.</Summary>
            WSANO_RECOVERY = 11003,
            /// <Summary>Valid name, no data record of requested type.</Summary>
            WSANO_DATA = 11004,
            /// <Summary>QoS receivers.</Summary>
            WSA_QOS_RECEIVERS = 11005,
            /// <Summary>QoS senders.</Summary>
            WSA_QOS_SENDERS = 11006,
            /// <Summary>No QoS senders.</Summary>
            WSA_QOS_NO_SENDERS = 11007,
            /// <Summary>QoS no receivers.</Summary>
            WSA_QOS_NO_RECEIVERS = 11008,
            /// <Summary>QoS request confirmed.</Summary>
            WSA_QOS_REQUEST_CONFIRMED = 11009,
            /// <Summary>QoS admission error.</Summary>
            WSA_QOS_ADMISSION_FAILURE = 11010,
            /// <Summary>QoS policy failure.</Summary>
            WSA_QOS_POLICY_FAILURE = 11011,
            /// <Summary>QoS bad style.</Summary>
            WSA_QOS_BAD_STYLE = 11012,
            /// <Summary>QoS bad object.</Summary>
            WSA_QOS_BAD_OBJECT = 11013,
            /// <Summary>QoS traffic control error.</Summary>
            WSA_QOS_TRAFFIC_CTRL_ERROR = 11014,
            /// <Summary>QoS generic error.</Summary>
            WSA_QOS_GENERIC_ERROR = 11015,
            /// <Summary>QoS service type error.</Summary>
            WSA_QOS_ESERVICETYPE = 11016,
            /// <Summary>QoS flowspec error.</Summary>
            WSA_QOS_EFLOWSPEC = 11017,
            /// <Summary>Invalid QoS provider buffer.</Summary>
            WSA_QOS_EPROVSPECBUF = 11018,
            /// <Summary>Invalid QoS filter style.</Summary>
            WSA_QOS_EFILTERSTYLE = 11019,
            /// <Summary>Invalid QoS filter type.</Summary>
            WSA_QOS_EFILTERTYPE = 11020,
            /// <Summary>Incorrect QoS filter count.</Summary>
            WSA_QOS_EFILTERCOUNT = 11021,
            /// <Summary>Invalid QoS object length.</Summary>
            WSA_QOS_EOBJLENGTH = 11022,
            /// <Summary>Incorrect QoS flow count.</Summary>
            WSA_QOS_EFLOWCOUNT = 11023,
            /// <Summary>Unrecognized QoS object.</Summary>
            WSA_QOS_EUNKOWNPSOBJ = 11024,
            /// <Summary>Invalid QoS policy object.</Summary>
            WSA_QOS_EPOLICYOBJ = 11025,
            /// <Summary>Invalid QoS flow descriptor.</Summary>
            WSA_QOS_EFLOWDESC = 11026,
            /// <Summary>Invalid QoS provider-specific flowspec.</Summary>
            WSA_QOS_EPSFLOWSPEC = 11027,
            /// <Summary>Invalid QoS provider-specific filterspec.</Summary>
            WSA_QOS_EPSFILTERSPEC = 11028,
            /// <Summary>Invalid QoS shape discard mode object.</Summary>
            WSA_QOS_ESDMODEOBJ = 11029,
            /// <Summary>Invalid QoS shaping rate object.</Summary>
            WSA_QOS_ESHAPERATEOBJ = 11030,
            /// <Summary>Reserved policy QoS element type.</Summary>
            WSA_QOS_RESERVED_PETYPE = 11031
        }
        #endregion

        #region network event
        [DllImport(WinSock2_32dll, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static Int32 WSANSPIoctl(
            IntPtr hLookup,
            UInt32 dwControlCode,
            IntPtr lpvInBuffer,
            Int32 cbInBuffer,
            IntPtr lpvOutBuffer,
            Int32 cbOutBuffer,
            ref Int32 lpcbBytesReturned,
            IntPtr lpCompletion);
        #endregion
        #endregion
        #region Token and Security
        // Token Specific Access Rights
        private const string advapi32= "advapi32.dll";
        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
        public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 TOKEN_IMPERSONATE = 0x0004;
        public const UInt32 TOKEN_QUERY = 0x0008;
        public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
        public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
        public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
            TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE |
            TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES |
            TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);


        public const Int32 ERROR_INSUFFICIENT_BUFFER = 122;


        // Integrity Levels

        public static SID_IDENTIFIER_AUTHORITY SECURITY_MANDATORY_LABEL_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] { 0, 0, 0, 0, 0, 16 });
        public const Int32 SECURITY_MANDATORY_UNTRUSTED_RID = 0x00000000;
        public const Int32 SECURITY_MANDATORY_LOW_RID = 0x00001000;
        public const Int32 SECURITY_MANDATORY_MEDIUM_RID = 0x00002000;
        public const Int32 SECURITY_MANDATORY_HIGH_RID = 0x00003000;
        public const Int32 SECURITY_MANDATORY_SYSTEM_RID = 0x00004000;

        
        // Group related SID Attributes

        public const UInt32 SE_GROUP_MANDATORY = 0x00000001;
        public const UInt32 SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        public const UInt32 SE_GROUP_ENABLED = 0x00000004;
        public const UInt32 SE_GROUP_OWNER = 0x00000008;
        public const UInt32 SE_GROUP_USE_FOR_DENY_ONLY = 0x00000010;
        public const UInt32 SE_GROUP_INTEGRITY = 0x00000020;
        public const UInt32 SE_GROUP_INTEGRITY_ENABLED = 0x00000040;
        public const UInt32 SE_GROUP_LOGON_ID = 0xC0000000;
        public const UInt32 SE_GROUP_RESOURCE = 0x20000000;
        public const UInt32 SE_GROUP_VALID_ATTRIBUTES = (SE_GROUP_MANDATORY |
            SE_GROUP_ENABLED_BY_DEFAULT | SE_GROUP_ENABLED | SE_GROUP_OWNER |
            SE_GROUP_USE_FOR_DENY_ONLY | SE_GROUP_LOGON_ID | SE_GROUP_RESOURCE |
            SE_GROUP_INTEGRITY | SE_GROUP_INTEGRITY_ENABLED);


        [DllImport(advapi32, SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

        /// <summary>
        /// Passed to <see cref="GetTokenInformation"/> to specify what
        /// information about the token to return.
        /// </summary>
        public enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUiAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// The elevation type for a user token.
        /// </summary>
        public enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }        /// <summary>
                 /// 
        /// The function opens the access token associated with a process.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process whose access token is opened.
        /// </param>
        /// <param name="desiredAccess">
        /// Specifies an access mask that specifies the requested types of 
        /// access to the access token. 
        /// </param>
        /// <param name="hToken">
        /// Outputs a handle that identifies the newly opened access token 
        /// when the function returns.
        /// </param>
        /// <returns></returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(
            IntPtr hProcess,
            UInt32 desiredAccess, 
            out SafeTokenHandle hToken);


        /// <summary>
        /// The DuplicateTokenEx function creates a new access token that 
        /// duplicates an existing token. This function can create either a 
        /// primary token or an impersonation token.
        /// </summary>
        /// <param name="hExistingToken">
        /// A handle to an access token opened with TOKEN_DUPLICATE access.
        /// </param>
        /// <param name="desiredAccess">
        /// Specifies the requested access rights for the new token.
        /// </param>
        /// <param name="pTokenAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a 
        /// security descriptor for the new token and determines whether 
        /// child processes can inherit the token. If lpTokenAttributes is 
        /// NULL, the token gets a default security descriptor and the handle 
        /// cannot be inherited. 
        /// </param>
        /// <param name="ImpersonationLevel">
        /// Specifies the impersonation level of the new token.
        /// </param>
        /// <param name="TokenType">
        /// TokenPrimary - The new token is a primary token that you can use 
        /// in the CreateProcessAsUser function.
        /// TokenImpersonation - The new token is an impersonation token.
        /// </param>
        /// <param name="hNewToken">
        /// Receives the new token.
        /// </param>
        /// <returns></returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateTokenEx(
            SafeTokenHandle hExistingToken,
            UInt32 desiredAccess, 
            IntPtr pTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE TokenType, 
            out SafeTokenHandle hNewToken);


        /// <summary>
        /// The function retrieves a specified type of information about an 
        /// access token. The calling process must have appropriate access 
        /// rights to obtain the information.
        /// </summary>
        /// <param name="hToken">
        /// A handle to an access token from which information is retrieved.
        /// </param>
        /// <param name="tokenInfoClass">
        /// Specifies a value from the TOKEN_INFORMATION_CLASS enumerated 
        /// type to identify the type of information the function retrieves.
        /// </param>
        /// <param name="pTokenInfo">
        /// A pointer to a buffer the function fills with the requested 
        /// information.
        /// </param>
        /// <param name="tokenInfoLength">
        /// Specifies the size, in bytes, of the buffer pointed to by the 
        /// TokenInformation parameter. 
        /// </param>
        /// <param name="returnLength">
        /// A pointer to a variable that receives the number of bytes needed 
        /// for the buffer pointed to by the TokenInformation parameter. 
        /// </param>
        /// <returns></returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTokenInformation(
            SafeTokenHandle hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr pTokenInfo,
            Int32 tokenInfoLength,
            out Int32 returnLength);


        /// <summary>
        /// The function sets various types of information for a specified 
        /// access token. The information that this function sets replaces 
        /// existing information. The calling process must have appropriate 
        /// access rights to set the information.
        /// </summary>
        /// <param name="hToken">
        /// A handle to the access token for which information is to be set.
        /// </param>
        /// <param name="tokenInfoClass">
        /// A value from the TOKEN_INFORMATION_CLASS enumerated type that 
        /// identifies the type of information the function sets. 
        /// </param>
        /// <param name="pTokenInfo">
        /// A pointer to a buffer that contains the information set in the 
        /// access token. 
        /// </param>
        /// <param name="tokenInfoLength">
        /// Specifies the length, in bytes, of the buffer pointed to by 
        /// TokenInformation.
        /// </param>
        /// <returns></returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetTokenInformation(
            SafeTokenHandle hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr pTokenInfo,
            Int32 tokenInfoLength);


        /// <summary>
        /// The function returns a pointer to a specified subauthority in a 
        /// security identifier (SID). The subauthority value is a relative 
        /// identifier (RID).
        /// </summary>
        /// <param name="pSid">
        /// A pointer to the SID structure from which a pointer to a 
        /// subauthority is to be returned.
        /// </param>
        /// <param name="nSubAuthority">
        /// Specifies an index value identifying the subauthority array 
        /// element whose address the function will return.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a pointer to the 
        /// specified SID subauthority. To get extended error information, 
        /// call GetLastError. If the function fails, the return value is 
        /// undefined. The function fails if the specified SID structure is 
        /// not valid or if the index value specified by the nSubAuthority 
        /// parameter is out of bounds.
        /// </returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetSidSubAuthority(
            IntPtr pSid, 
            UInt32 nSubAuthority);


        /// <summary>
        /// The AllocateAndInitializeSid function allocates and initializes a 
        /// security identifier (SID) with up to eight subauthorities.
        /// </summary>
        /// <param name="pIdentifierAuthority">
        /// A reference of a SID_IDENTIFIER_AUTHORITY structure. This 
        /// structure provides the top-level identifier authority value to 
        /// set in the SID.
        /// </param>
        /// <param name="nSubAuthorityCount">
        /// Specifies the number of subauthorities to place in the SID. 
        /// </param>
        /// <param name="dwSubAuthority0">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority1">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority2">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority3">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority4">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority5">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority6">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="dwSubAuthority7">
        /// Subauthority value to place in the SID.
        /// </param>
        /// <param name="pSid">
        /// Outputs the allocated and initialized SID structure.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is true. If the 
        /// function fails, the return value is false. To get extended error 
        /// information, call GetLastError.
        /// </returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocateAndInitializeSid(
            ref SID_IDENTIFIER_AUTHORITY pIdentifierAuthority,
            byte nSubAuthorityCount,
            int dwSubAuthority0, int dwSubAuthority1,
            int dwSubAuthority2, int dwSubAuthority3,
            int dwSubAuthority4, int dwSubAuthority5,
            int dwSubAuthority6, int dwSubAuthority7,
            out IntPtr pSid);


        /// <summary>
        /// The FreeSid function frees a security identifier (SID) previously 
        /// allocated by using the AllocateAndInitializeSid function.
        /// </summary>
        /// <param name="pSid">
        /// A pointer to the SID structure to free.
        /// </param>
        /// <returns>
        /// If the function succeeds, the function returns NULL. If the 
        /// function fails, it returns a pointer to the SID structure 
        /// represented by the pSid parameter.
        /// </returns>
        [DllImport(advapi32)]
        public static extern IntPtr FreeSid(IntPtr pSid);


        /// <summary>
        /// The function returns the length, in bytes, of a valid security 
        /// identifier (SID).
        /// </summary>
        /// <param name="pSID">
        /// A pointer to the SID structure whose length is returned. 
        /// </param>
        /// <returns>
        /// If the SID structure is valid, the return value is the length, in 
        /// bytes, of the SID structure.
        /// </returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetLengthSid(IntPtr pSID);


        /// <summary>
        /// Creates a new process and its primary thread. The new process 
        /// runs in the security context of the user represented by the 
        /// specified token.
        /// </summary>
        /// <param name="hToken">
        /// A handle to the primary token that represents a user. 
        /// </param>
        /// <param name="applicationName">
        /// The name of the module to be executed.
        /// </param>
        /// <param name="commandLine">
        /// The command line to be executed. The maximum length of this 
        /// string is 32K characters. 
        /// </param>
        /// <param name="pProcessAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a 
        /// security descriptor for the new process object and determines 
        /// whether child processes can inherit the returned handle to the 
        /// process.
        /// </param>
        /// <param name="pThreadAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a 
        /// security descriptor for the new thread object and determines 
        /// whether child processes can inherit the returned handle to the 
        /// thread.
        /// </param>
        /// <param name="bInheritHandles">
        /// If this parameter is true, each inheritable handle in the calling 
        /// process is inherited by the new process. If the parameter is 
        /// false, the handles are not inherited. 
        /// </param>
        /// <param name="dwCreationFlags">
        /// The flags that control the priority class and the creation of the 
        /// process.
        /// </param>
        /// <param name="pEnvironment">
        /// A pointer to an environment block for the new process. 
        /// </param>
        /// <param name="currentDirectory">
        /// The full path to the current directory for the process. 
        /// </param>
        /// <param name="startupInfo">
        /// References a STARTUPINFO structure.
        /// </param>
        /// <param name="processInformation">
        /// Outputs a PROCESS_INFORMATION structure that receives 
        /// identification information about the new process. 
        /// </param>
        /// <returns></returns>
        [DllImport(advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser(
            SafeTokenHandle hToken,
            string applicationName,
            string commandLine,
            IntPtr pProcessAttributes,
            IntPtr pThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr pEnvironment,
            string currentDirectory,
            ref STARTUPINFO startupInfo,
            out PROCESS_INFORMATION processInformation);


        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns></returns>
        [DllImport(kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Well-known folder paths
        /// </summary>
        public sealed class KnownFolder
        {
            private static readonly Guid LocalAppDataGuid = new Guid(
                "F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
            public static string LocalAppData
            {
                get { return SHGetKnownFolderPath(LocalAppDataGuid); }
            }

            private static readonly Guid LocalAppDataLowGuid = new Guid(
                "A520A1A4-1780-4FF6-BD18-167343C5AF16");
            public static string LocalAppDataLow
            {
                get { return SHGetKnownFolderPath(LocalAppDataLowGuid); }
            }


            /// <summary>
            /// Retrieves the full path of a known folder identified by the 
            /// folder's KNOWNFOLDERID.
            /// </summary>
            /// <param name="rfid">
            /// A reference to the KNOWNFOLDERID that identifies the folder.
            /// </param>
            /// <returns></returns>
            public static string SHGetKnownFolderPath(Guid rfid)
            {
                IntPtr pPath = IntPtr.Zero;
                string path = null;
                try
                {
                    int hr = SHGetKnownFolderPath(rfid, 0, IntPtr.Zero, out pPath);
                    if (hr != 0)
                    {
                        throw Marshal.GetExceptionForHR(hr);
                    }
                    path = Marshal.PtrToStringUni(pPath);
                }
                finally
                {
                    if (pPath != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(pPath);
                        pPath = IntPtr.Zero;
                    }
                }
                return path;
            }


            /// <summary>
            /// Retrieves the full path of a known folder identified by the 
            /// folder's KNOWNFOLDERID.
            /// </summary>
            /// <param name="rfid">
            /// A reference to the KNOWNFOLDERID that identifies the folder.
            /// </param>
            /// <param name="dwFlags">
            /// Flags that specify special retrieval options.
            /// </param>
            /// <param name="hToken">
            /// An access token that represents a particular user. If this 
            /// parameter is NULL, which is the most common usage, the function 
            /// requests the known folder for the current user.
            /// </param>
            /// <param name="pszPath">
            /// When this method returns, contains the address of a pointer to a 
            /// null-terminated Unicode string that specifies the path of the 
            /// known folder. The calling process is responsible for freeing this 
            /// resource once it is no longer needed by calling CoTaskMemFree.
            /// </param>
            /// <returns>HRESULT</returns>
            [DllImport(shell32, CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                uint dwFlags,
                IntPtr hToken,
                out IntPtr pszPath);
        }

        /// <summary>
        /// The TOKEN_INFORMATION_CLASS enumeration type contains values that 
        /// specify the type of information being assigned to or retrieved from 
        /// an access token.
        /// </summary>
        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        [Flags]
        public enum CreateProcessFlags
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }


        /// <summary>
        /// The SECURITY_IMPERSONATION_LEVEL enumeration type contains values 
        /// that specify security impersonation levels. Security impersonation 
        /// levels govern the degree to which a server process can act on behalf 
        /// of a client process.
        /// </summary>
        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        /// <summary>
        /// The TOKEN_TYPE enumeration type contains values that differentiate 
        /// between a primary token and an impersonation token. 
        /// </summary>
        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        /// <summary>
        /// The structure represents a security identifier (SID) and its 
        /// attributes. SIDs are used to uniquely identify users or groups.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public UInt32 Attributes;
        }

        /// <summary>
        /// The SID_IDENTIFIER_AUTHORITY structure represents the top-level 
        /// authority of a security identifier (SID).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_IDENTIFIER_AUTHORITY
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6,
                ArraySubType = UnmanagedType.I1)]
            public byte[] Value;

            public SID_IDENTIFIER_AUTHORITY(byte[] value)
            {
                this.Value = value;
            }
        }

        /// <summary>
        /// The structure specifies the mandatory integrity level for a token.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        }

        /// <summary>
        /// Specifies the window station, desktop, standard handles, and 
        /// appearance of the main window for a process at creation time.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        /// <summary>
        /// Contains information about a newly created process and its primary 
        /// thread.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        /// <summary>
        /// Represents a wrapper class for a token handle.
        /// </summary>
        public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle()
                : base(true)
            {
            }

            internal SafeTokenHandle(IntPtr handle)
                : base(true)
            {
                base.SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(base.handle);
            }
        }
        
        #endregion
    }
}