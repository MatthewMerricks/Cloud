//
// metadata.cs
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
using System.Text;

namespace SyncTestServer.SubProcessors
{
    public sealed class metadata : HttpActionSubprocessor
    {
        #region singleton pattern
        public static metadata Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new metadata());
                }
            }
        }
        private static metadata _instance = null;
        private static readonly object InstanceLocker = new object();

        private metadata() { }
        #endregion

        public override string InnerMethod
        {
            get
            {
                lock (InnerMethodLocker)
                {
                    return _innerMethod
                        ?? (_innerMethod = (this.GetType().Name));
                }
            }
        }
        private string _innerMethod = null;
        private readonly object InnerMethodLocker = new object();

        public override void ProcessContext(HttpListenerContext toProcess, IServerData serverData, NameValueCollection queryString)
        {
            Device currentDevice;
            User currentUser;
            int queryUserId;
            if ((currentUser = SyncTestServer.Static.Helpers.FindUserDevice(serverData, toProcess, out currentDevice)) == null
                || !int.TryParse(queryString["user_id"], out queryUserId)
                || queryUserId != currentUser.Id)
            {
                SyncTestServer.Static.Helpers.WriteUnauthorizedResponse(toProcess);
            }
            else
            {
                bool isFolder;
                if (bool.TryParse(queryString["IsFolder"], out isFolder))
                {
                    string pathString = queryString["path"];
                }
                else
                {
                    throw new ArgumentException("Is-Folder query string tag was not set to a bool-parsable value");
                }

                CloudApiPublic.JsonContracts.Metadata toWrite = serverData.GetLatestMetadataAtPath(currentUser, queryString["path"], isFolder);

                if (toWrite == null)
                {
                    toProcess.Response.StatusCode = 204;
                }
                else
                {
                    string responseBody;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        CloudApiPublic.JsonContracts.JsonContractHelpers.GetMetadataResponseSerializer.WriteObject(ms, toWrite);
                        responseBody = Encoding.Default.GetString(ms.ToArray());
                    }

                    byte[] responseBodyBytes = Encoding.UTF8.GetBytes(responseBody);

                    toProcess.Response.ContentType = "application/json; charset=utf-8";
                    toProcess.Response.SendChunked = true;
                    toProcess.Response.StatusCode = 200;

                    toProcess.Response.OutputStream.Write(responseBodyBytes, 0, responseBodyBytes.Length);
                }
            }
        }
    }
}