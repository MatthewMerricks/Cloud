//
// put_file.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SyncTestServer.ActionProcessors
{
    public sealed class put_file : HttpActionProcessor
    {
        #region singleton pattern
        public static put_file Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new put_file());
                }
            }
        }
        private static put_file _instance = null;
        private static readonly object InstanceLocker = new object();

        private put_file() { }
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
                    toProcess.Response.Headers.Add("X-Frame-Options", "sameorigin");
                    toProcess.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

                    if (serverData.WriteUpload(toProcess.Request.InputStream,
                        toProcess.Request.Headers[CLDefinitions.HeaderAppendStorageKey],
                        toProcess.Request.ContentLength64,
                        toProcess.Request.Headers[CLDefinitions.HeaderAppendContentMD5],
                        currentUser,
                        false))
                    {
                        toProcess.Response.ContentType = "text/html;charset=utf-8";
                        toProcess.Response.SendChunked = false;

                        toProcess.Response.StatusCode = 200;

                        CloudApiPublic.JsonContracts.PutFileResponse toWrite = new CloudApiPublic.JsonContracts.PutFileResponse()
                        {
                            StorageKey = toProcess.Request.Headers[CLDefinitions.HeaderAppendStorageKey]
                        };

                        string responseBody;

                        using (MemoryStream ms = new MemoryStream())
                        {
                            PutFileResponseSerializer.WriteObject(ms, toWrite);
                            responseBody = Encoding.Default.GetString(ms.ToArray());
                        }

                        byte[] responseBodyBytes = Encoding.UTF8.GetBytes(responseBody);

                        toProcess.Response.ContentLength64 = responseBodyBytes.LongLength;

                        toProcess.Response.OutputStream.Write(responseBodyBytes, 0, responseBodyBytes.Length);
                    }
                    else
                    {
                        toProcess.Response.StatusCode = 304;
                    }
                }

                return true;
            }
        }

        public static DataContractJsonSerializer PutFileResponseSerializer
        {
            get
            {
                lock (PutFileResponseLocker)
                {
                    return _putFileResponseSerializer
                        ?? (_putFileResponseSerializer = new DataContractJsonSerializer(typeof(CloudApiPublic.JsonContracts.PutFileResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _putFileResponseSerializer = null;
        private static readonly object PutFileResponseLocker = new object();
    }
}