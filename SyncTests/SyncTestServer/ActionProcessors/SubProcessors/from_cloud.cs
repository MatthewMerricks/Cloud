﻿//
// from_cloud.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

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
    public sealed class from_cloud : HttpActionSubprocessor
    {
        #region singleton pattern
        public static from_cloud Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new from_cloud());
                }
            }
        }
        private static from_cloud _instance = null;
        private static readonly object InstanceLocker = new object();

        private from_cloud() { }
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
            if ((currentUser = SyncTestServer.Static.Helpers.FindUserDevice(serverData, toProcess, out currentDevice)) == null)
            {
                SyncTestServer.Static.Helpers.WriteUnauthorizedResponse(toProcess);
            }
            else
            {
                SyncTestServer.Static.Helpers.WriteRandomETag(toProcess);

                CloudApiPublic.JsonContracts.Push pushRequest = (CloudApiPublic.JsonContracts.Push)CloudApiPublic.JsonContracts.JsonContractHelpers.PushSerializer.ReadObject(toProcess.Request.InputStream);

                long newSyncId = serverData.NewSyncIdBeforeStart;

                CloudApiPublic.JsonContracts.PushResponse toWrite = new CloudApiPublic.JsonContracts.PushResponse()
                {
                    SyncId = newSyncId.ToString(),
                    PartialResponse = false,
                    Events = serverData.GrabEventsAfterLastSync(pushRequest.LastSyncId, pushRequest.RelativeRootPath, currentUser, newSyncId).ToArray()
                };

                string responseBody;

                using (MemoryStream ms = new MemoryStream())
                {
                    CloudApiPublic.JsonContracts.JsonContractHelpers.PushResponseSerializer.WriteObject(ms, toWrite);
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