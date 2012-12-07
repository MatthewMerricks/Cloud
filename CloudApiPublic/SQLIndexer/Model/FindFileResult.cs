//
// FindFileResult.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.SQLIndexer.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudApiPublic.SQLIndexer.Model
{
    internal sealed class FindFileResult : IFileResultParent
    {
        public Nullable<DateTime> CreationTime { get; private set; } // DateTime.FromFileTimeUtc((ftCreationTime.dwHighDateTime * uint.MaxValue) + ftCreationTime.dwLowDateTime)
        public Nullable<DateTime> LastWriteTime { get; private set; } // DateTime.FromFileTimeUtc((ftLastWriteTime.dwHighDateTime * uint.MaxValue) + ftLastWriteTime.dwLowDateTime)
        public Nullable<long> Size { get; private set; } // (nFileSizeHigh * uint.MaxValue) + nFileSizeLow
        public bool IsFolder
        {
            get
            {
                return Children != null;
            }
        }
        public string Name { get; private set; }

        public IList<FindFileResult> Children { get; private set; }

        public string FullName
        {
            get
            {
                return Parent.FullName + "\\" + Name;
            }
        }
        private IFileResultParent Parent { get; set; }

        public static IList<FindFileResult> RecursiveDirectorySearch(string fullDirectoryPath, FileAttributes toIgnore, out bool rootNotFound)
        {
            return RecursiveDirectorySearch(fullDirectoryPath, toIgnore, out rootNotFound, null);
        }

        private static bool IsDotDirectory(string directoryName)
        {
            if (directoryName == null)
            {
                return true;
            }

            switch (directoryName.Length)
            {
                case 1:
                    return directoryName[0] == '.';
                case 2:
                    return directoryName[0] == '.'
                        && directoryName[1] == '.';
                default:
                    return false;
            }
        }

        private static IList<FindFileResult> RecursiveDirectorySearch(string fullDirectoryPath, FileAttributes toIgnore, out bool rootNotFound, IFileResultParent searchParent)
        {
            List<FindFileResult> toReturn = new List<FindFileResult>();

            if (searchParent == null)
            {
                searchParent = new FileResultRoot(fullDirectoryPath);
            }

            NativeMethods.WIN32_FIND_DATA fileData;
            SafeSearchHandle searchHandle = null;
            try
            {
                searchHandle = NativeMethods.FindFirstFileEx(//"\\\\?\\" + // Allows searching paths up to 32,767 characters in length, but not supported on XP
                    fullDirectoryPath + "\\*.*",
                    NativeMethods.FINDEX_INFO_LEVELS.FindExInfoStandard,// Basic would be optimal but it's only supported in Windows 7 on up
                    out fileData,
                    NativeMethods.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                    IntPtr.Zero,
                    NativeMethods.FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

                if (!searchHandle.IsInvalid)
                {
                    rootNotFound = false;

                    do
                    {
                        if ((FileAttributes)0 ==// compare bitwise and of FileAttributes and all unwanted attributes to '0'
                            (fileData.dwFileAttributes & toIgnore)
                            && ((fileData.dwFileAttributes & FileAttributes.Directory) == (FileAttributes)0
                                || !IsDotDirectory(fileData.cFileName)))
                        {
                            toReturn.Add(new FindFileResult()
                            {
                                CreationTime = ((fileData.ftCreationTime.dwHighDateTime == (uint)0
                                        && fileData.ftCreationTime.dwLowDateTime == (uint)0)
                                    ? (Nullable<DateTime>)null
                                    : DateTime.FromFileTimeUtc((long)(((ulong)fileData.ftCreationTime.dwHighDateTime << 32) + fileData.ftCreationTime.dwLowDateTime))),
                                LastWriteTime = (fileData.ftLastWriteTime.dwHighDateTime == (uint)0
                                        && fileData.ftLastWriteTime.dwLowDateTime == (uint)0)
                                    ? (Nullable<DateTime>)null
                                    : DateTime.FromFileTimeUtc((long)(((ulong)fileData.ftLastWriteTime.dwHighDateTime << 32) + fileData.ftLastWriteTime.dwLowDateTime)),
                                Name = fileData.cFileName,
                                Size = ((fileData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory
                                    ? (Nullable<long>)null
                                    : (long)(((ulong)fileData.nFileSizeHigh << 32) + fileData.nFileSizeLow)),
                                Parent = searchParent
                            });
                        }
                    }
                    while (NativeMethods.FindNextFile(searchHandle, out fileData));
                }
                else
                {
                    rootNotFound = true;
                }
            }
            finally
            {
                if (searchHandle != null
                    && !searchHandle.IsInvalid)
                {
                    searchHandle.Dispose();
                }
            }

            foreach (FindFileResult currentInnerDirectory in toReturn.Where(currentReturn => currentReturn.Size == null))
            {
                bool innerDirectoryNotFound;
                currentInnerDirectory.Children = RecursiveDirectorySearch(fullDirectoryPath + "\\" + currentInnerDirectory.Name, toIgnore, out innerDirectoryNotFound, currentInnerDirectory);
            }

            return toReturn;
        }

        public static FindFileResult FillResultAtPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            FindFileResult toReturn;

            NativeMethods.WIN32_FIND_DATA fileData;
            SafeSearchHandle searchHandle = null;
            try
            {
                searchHandle = NativeMethods.FindFirstFileEx(//"\\\\?\\" + // Allows searching paths up to 32,767 characters in length, but not supported on Windows XP
                    fullPath,
                    NativeMethods.FINDEX_INFO_LEVELS.FindExInfoStandard,// Basic would be optimal but it's only supported in Windows 7 on up
                    out fileData,
                    NativeMethods.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                    IntPtr.Zero,
                    NativeMethods.FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

                if (!searchHandle.IsInvalid)
                {
                    toReturn = new FindFileResult()
                        {
                            CreationTime = ((fileData.ftCreationTime.dwHighDateTime == (uint)0
                                    && fileData.ftCreationTime.dwLowDateTime == (uint)0)
                                ? (Nullable<DateTime>)null
                                : DateTime.FromFileTimeUtc((long)(((ulong)fileData.ftCreationTime.dwHighDateTime << 32) + fileData.ftCreationTime.dwLowDateTime))),
                            LastWriteTime = ((fileData.ftLastWriteTime.dwHighDateTime == (uint)0
                                    && fileData.ftLastWriteTime.dwLowDateTime == (uint)0)
                                ? (Nullable<DateTime>)null
                                : DateTime.FromFileTimeUtc((long)(((ulong)fileData.ftLastWriteTime.dwHighDateTime << 32) + fileData.ftLastWriteTime.dwLowDateTime))),
                            Name = fileData.cFileName,
                            Size = ((fileData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory
                                ? (Nullable<long>)null
                                : (long)(((ulong)fileData.nFileSizeHigh << 32) + fileData.nFileSizeLow)),
                            Children = ((fileData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory
                                ? new List<FindFileResult>(0)
                                : null)
                        };
                }
                else
                {
                    toReturn = null;
                }
            }
            finally
            {
                if (searchHandle != null
                    && !searchHandle.IsInvalid)
                {
                    searchHandle.Dispose();
                }
            }

            if (toReturn != null)
            {
                toReturn.Parent = new FileResultRoot(fullPath.Substring(0, fullPath.Length - toReturn.Name.Length - 1));
            }

            return toReturn;
        }
    }
}