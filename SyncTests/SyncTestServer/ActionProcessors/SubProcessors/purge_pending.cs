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
    public sealed class purge_pending : HttpActionSubprocessor
    {
        #region singleton pattern
        public static purge_pending Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new purge_pending());
                }
            }
        }
        private static purge_pending _instance = null;
        private static readonly object InstanceLocker = new object();

        private purge_pending() { }
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
                CloudApiPublic.JsonContracts.PurgePending purgeRequest = (CloudApiPublic.JsonContracts.PurgePending)CloudApiPublic.JsonContracts.JsonContractHelpers.PurgePendingSerializer.ReadObject(toProcess.Request.InputStream);

                bool deviceNotInUser;
                IEnumerable<CloudApiPublic.JsonContracts.File> filesPurged = serverData.PurgePendingFiles(currentUser, purgeRequest, out deviceNotInUser);

                if (deviceNotInUser)
                {
                    SyncTestServer.Static.Helpers.WriteUnauthorizedResponse(toProcess);
                }
                else
                {
                    SyncTestServer.Static.Helpers.WriteRandomETag(toProcess);

                    CloudApiPublic.JsonContracts.PurgePendingResponse toWrite = new CloudApiPublic.JsonContracts.PurgePendingResponse()
                    {
                        Files = filesPurged.ToArray()
                    };

                    string responseBody;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        CloudApiPublic.JsonContracts.JsonContractHelpers.PurgePendingResponseSerializer.WriteObject(ms, toWrite);
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