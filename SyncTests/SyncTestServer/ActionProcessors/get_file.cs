using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SyncTestServer.ActionProcessors
{
    public sealed class get_file : HttpActionProcessor
    {
        #region singleton pattern
        public static get_file Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new get_file());
                }
            }
        }
        private static get_file _instance = null;
        private static readonly object InstanceLocker = new object();

        private get_file() { }
        #endregion

        public override string HttpPrefix
        {
            get
            {
                lock (HttpPrefixLocker)
                {
                    return _httpPrefix
                        ?? (_httpPrefix = (CLDefinitions.CLUploadDownloadServerURL.EndsWith("/")
                                ? CLDefinitions.CLUploadDownloadServerURL
                                : CLDefinitions.CLUploadDownloadServerURL + "/") +
                            this.GetType().Name + "/");
                }
            }
        }
        private string _httpPrefix = null;
        private readonly object HttpPrefixLocker = new object();

        public override bool ProcessContext(HttpListenerContext toProcess, IServerData serverData, string listenerFirstPath, string listenerFullPath, NameValueCollection queryString)
        {
            if (listenerFirstPath.Length <= listenerFullPath.Length)
            {
                return false;
            }
            else // ensures no trailing slash or sub-method
            {
                Device currentDevice;
                User currentUser;
                if ((currentUser = SyncTestServer.Static.Helpers.FindUserDevice(serverData, toProcess, out currentDevice)) == null)
                {
                    SyncTestServer.Static.Helpers.WriteUnauthorizedResponse(toProcess);
                }
                else
                {
                    CloudApiPublic.JsonContracts.Download downloadRequest = (CloudApiPublic.JsonContracts.Download)CloudApiPublic.JsonContracts.JsonContractHelpers.DownloadSerializer.ReadObject(toProcess.Request.InputStream);

                    long fileSize;
                    Stream fileDownload = serverData.GetDownload(downloadRequest.StorageKey, currentUser, out fileSize);

                    toProcess.Response.Headers.Add("X-Frame-Options", "sameorigin");
                    toProcess.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                    toProcess.Response.SendChunked = false;

                    if (fileDownload == null)
                    {
                        toProcess.Response.StatusCode = 404;

                        toProcess.Response.ContentType = "text/html;charset=utf-8";

                        string responseString = "storage_key " + (downloadRequest.StorageKey ?? "{null}") + " does not exist.";

                        byte[] notAuthorizedResponseBytes = Encoding.UTF8.GetBytes(responseString);

                        toProcess.Response.ContentLength64 = notAuthorizedResponseBytes.LongLength;

                        toProcess.Response.OutputStream.Write(notAuthorizedResponseBytes, 0, notAuthorizedResponseBytes.Length);
                    }
                    else
                    {
                        try
                        {
                            toProcess.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + downloadRequest.StorageKey + "\"");

                            toProcess.Response.StatusCode = 200;

                            toProcess.Response.ContentType = "application/octet-stream";
                            toProcess.Response.ContentLength64 = fileSize;

                            byte[] downloadBuffer = new byte[CloudApiPublic.Static.FileConstants.BufferSize];
                            int readSize;
                            while ((readSize = fileDownload.Read(downloadBuffer, 0, downloadBuffer.Length)) > 0)
                            {
                                toProcess.Response.OutputStream.Write(downloadBuffer, 0, readSize);
                            }
                        }
                        finally
                        {
                            fileDownload.Dispose();
                        }
                    }
                }

                return true;
            }
        }
    }
}