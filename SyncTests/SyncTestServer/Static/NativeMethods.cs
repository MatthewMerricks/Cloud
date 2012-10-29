using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SyncTestServer.Static
{
    internal static class NativeMethods
    {
        #region byte array compare
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(byte[] b1, byte[] b2, UIntPtr count);
        #endregion

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
    }
}