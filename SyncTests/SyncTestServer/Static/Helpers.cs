using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
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

        public static User FindUserDevice(IServerData serverData, HttpListenerContext toProcess, out Device currentDev)
        {
            return serverData.FindUserByAKey(toProcess.Request.Headers[CLDefinitions.HeaderKeyAuthorization].Substring(CLDefinitions.HeaderAppendToken.Length).Trim('"'),
                out currentDev);
        }

        public static void WriteStandardHeaders(HttpListenerContext toProcess)
        {
            toProcess.Response.KeepAlive = true;
            toProcess.Response.AddHeader("X-Powered-By", "Phusion Passenger (mod_rails/mod_rack) 3.0.13");
            toProcess.Response.AddHeader("X-UA-Compatible", "IE=Edge,chrome=1");
            toProcess.Response.AddHeader("Cache-Control", "no-cache");
            toProcess.Response.AddHeader("X-Request-Id", Guid.NewGuid().ToString("N"));
            toProcess.Response.AddHeader("X-Runtime", "0.168369");
            toProcess.Response.AddHeader("Date", DateTime.UtcNow.ToString("R"));
            toProcess.Response.AddHeader("X-Rack-Cache", "invalidate, pass");
            //Cannot set Server header, HttpListener always appends "Microsoft-HTTPAPI/2.0" //toProcess.Response.AddHeader("Server", "nginx/1.2.1 + Phusion Passenger 3.0.13 (mod_rails/mod_rack)");
        }

        public static void WriteNotFoundResponse(HttpListenerContext toProcess)
        {
            toProcess.Response.ContentType = "text/html; charset=utf-8";
            toProcess.Response.SendChunked = false;
            toProcess.Response.StatusCode = 404;

            byte[] notFoundBytes = Encoding.UTF8.GetBytes(
                "<!DOCTYPE html>\n" +
                "<html>\n" +
                "<head>\n" +
                Tab() + "<title>The page you were looking for doesn't exist (404)</title>\n" +
                Tab() + "<style type=\"text/css\">\n" +
                    Tab(2) + "body { background-color: #fff; color: #666; text-align: center; font-family: arial, sans-serif; }\n" +
                    Tab(2) + "div.dialog {\n" +
                        Tab(3) + "width: 25em;\n" +
                        Tab(3) + "padding: 0 4em;\n" +
                        Tab(3) + "margin: 4em auto 0 auto;\n" +
                        Tab(3) + "border: 1px solid #ccc;\n" +
                        Tab(3) + "border-right-color: #999;\n" +
                        Tab(3) + "border-bottom-color: #999;\n" +
                    Tab(2) + "}\n" +
                    Tab(2) + "h1 { font-size: 100%; color: #f00; line-height: 1.5em; }\n" +
                Tab() + "</style>\n" +
                "</head>\n" +
                "\n" +
                "<body>\n" +
                Tab() + "<!-- This file lives in public/404.html -->\n" +
                Tab() + "<div class=\"dialog\">\n" +
                    Tab(2) + "<h1>The page you were looking for doesn't exist.</h1>\n" +
                    Tab(2) + "<p>You may have mistyped the address or the page may have moved.</p>\n" +
                Tab() + "</div>\n" +
                "</body>\n" +
                "</html>\n");

            toProcess.Response.ContentLength64 = notFoundBytes.LongLength;

            toProcess.Response.OutputStream.Write(notFoundBytes, 0, notFoundBytes.Length);
        }

        public static string Tab(int count = 1)
        {
            return new string(' ', count * 2);
        }

        public static void WriteUnauthorizedResponse(HttpListenerContext toProcess)
        {
            CloudApiPublic.JsonContracts.Message notAuthorizedMessage = new CloudApiPublic.JsonContracts.Message()
            {
                Value = "You are not allowed to access this resource"
            };

            string notAuthorizedString;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                MessageSerializer.WriteObject(ms, notAuthorizedMessage);
                notAuthorizedString = Encoding.Default.GetString(ms.ToArray());
            }

            byte[] notAuthorizedResponseBytes = Encoding.UTF8.GetBytes(notAuthorizedString);

            toProcess.Response.ContentType = "application/json; charset=utf-8";
            toProcess.Response.SendChunked = true;
            toProcess.Response.StatusCode = 401;

            toProcess.Response.OutputStream.Write(notAuthorizedResponseBytes, 0, notAuthorizedResponseBytes.Length);
        }

        public static void WriteRandomETag(HttpListenerContext toProcess)
        {
            toProcess.Response.Headers.Add("ETag", "\"" + Guid.NewGuid().ToString("N") + "\"");
        }

        public static string GetEventAction(CloudApiPublic.Static.FileChangeType changeType, bool isFolder, string linkTarget)
        {
            if (isFolder
                && linkTarget != null)
            {
                throw new ArgumentException("Cannot have isFolder true if linkTarget is not null");
            }

            switch (changeType)
            {
                case CloudApiPublic.Static.FileChangeType.Created:
                    if (isFolder)
                    {
                        return CLDefinitions.CLEventTypeAddFolder;
                    }
                    if (linkTarget != null)
                    {
                        return CLDefinitions.CLEventTypeAddLink;
                    }
                    return CLDefinitions.CLEventTypeAddFile;

                case CloudApiPublic.Static.FileChangeType.Deleted:
                    if (isFolder)
                    {
                        return CLDefinitions.CLEventTypeDeleteFolder;
                    }
                    if (linkTarget != null)
                    {
                        return CLDefinitions.CLEventTypeDeleteLink;
                    }
                    return CLDefinitions.CLEventTypeDeleteFile;

                case CloudApiPublic.Static.FileChangeType.Modified:
                    if (isFolder)
                    {
                        throw new ArgumentException("Cannot have isFolder true if changeType is Modified");
                    }
                    if (linkTarget != null)
                    {
                        return CLDefinitions.CLEventTypeModifyLink;
                    }
                    return CLDefinitions.CLEventTypeModifyFile;

                case CloudApiPublic.Static.FileChangeType.Renamed:
                    if (isFolder)
                    {
                        return CLDefinitions.CLEventTypeRenameFolder;
                    }
                    if (linkTarget != null)
                    {
                        return CLDefinitions.CLEventTypeRenameLink;
                    }
                    return CLDefinitions.CLEventTypeRenameFile;

                default:
                    throw new ArgumentException("Unknown changeType: " + changeType.ToString());
            }
        }

        public static DataContractJsonSerializer MessageSerializer
        {
            get
            {
                lock (MessageSerializerLocker)
                {
                    return _messageSerializer
                        ?? (_messageSerializer = new DataContractJsonSerializer(typeof(CloudApiPublic.JsonContracts.Message)));
                }
            }
        }
        private static DataContractJsonSerializer _messageSerializer = null;
        private static readonly object MessageSerializerLocker = new object();
    }
}