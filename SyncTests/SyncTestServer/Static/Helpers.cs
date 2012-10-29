using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SyncTestServer.Static
{
    public static class Helpers
    {
        public static Exception FillInMetadataDictionaryFromPhysicalPath(FilePathDictionary<Tuple<FileMetadata, byte[]>> toFill, string searchDirectory, Nullable<FileAttributes> ignoreAttributes = null)
        {
            try
            {
                FileAttributes ignoreAttributesNotNull;
                if (ignoreAttributes == null)
                {
                    ignoreAttributesNotNull = FileAttributes.Hidden// ignore hidden files
                        | FileAttributes.Offline// ignore offline files (data is not available on them)
                        | FileAttributes.System// ignore system files
                        | FileAttributes.Temporary;// ignore temporary files
                }
                else
                {
                    ignoreAttributesNotNull = (FileAttributes)ignoreAttributes;
                }

                bool rootNotFound;
                IList<FindFileResult> allInnerPaths = FindFileResult.RecursiveDirectorySearch(searchDirectory,
                    ignoreAttributesNotNull,
                    out rootNotFound);

                if (rootNotFound)
                {
                    return new Exception("Unable to find directory at path to fill MetadataDictionary: " + searchDirectory);
                }

                if (allInnerPaths != null)
                {
                    FillInMetadataDictionaryFromPhysicalPath(toFill, allInnerPaths);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private static void FillInMetadataDictionaryFromPhysicalPath(FilePathDictionary<Tuple<FileMetadata, byte[]>> toFill, IList<FindFileResult> currentPaths)
        {
            foreach (FindFileResult currentPath in currentPaths)
            {
                toFill.Add(currentPath.FullName, new Tuple<FileMetadata, byte[]>(new FileMetadata()
                    {
                        HashableProperties = new FileMetadataHashableProperties(currentPath.IsFolder,
                            (currentPath.IsFolder ? currentPath.CreationTime : currentPath.LastWriteTime),
                            currentPath.CreationTime,
                            currentPath.Size)
                    },
                    (currentPath.IsFolder ? null : Helpers.MD5ForPath(currentPath.FullName))));

                if (currentPath.IsFolder
                    && currentPath.Children != null
                    && currentPath.Children.Count > 0)
                {
                    FillInMetadataDictionaryFromPhysicalPath(toFill, currentPath.Children);
                }
            }
        }

        public static byte[] MD5ForPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new NullReferenceException("fullPath cannot be null");
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found at path: " + fullPath);
            }

            using (FileStream readStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return MD5.Create().ComputeHash(readStream);
            }
        }

        public static FilePath BuildFilePathFromEmptyRootRelativePath(string relativePath, bool forwardSlashSeperated = false)
        {
            if (relativePath == null)
            {
                return null;
            }

            string[] splitPaths = (forwardSlashSeperated
                ? relativePath.Split('/')
                : relativePath.Split('\\'));

            return BuildInnerPath(splitPaths, splitPaths.Length);
        }

        private static FilePath BuildInnerPath(string[] splitPaths, int splitPathIndex)
        {
            int nextIndex = splitPathIndex - 1;
            if (nextIndex == 0)
            {
                return new FilePath(splitPaths[0]);
            }
            else
            {
                return new FilePath(splitPaths[nextIndex], BuildInnerPath(splitPaths, nextIndex));
            }
        }
    }
}