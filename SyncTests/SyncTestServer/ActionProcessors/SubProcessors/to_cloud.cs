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
    public sealed class to_cloud : HttpActionSubprocessor
    {
        #region singleton pattern
        public static to_cloud Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new to_cloud());
                }
            }
        }
        private static to_cloud _instance = null;
        private static readonly object InstanceLocker = new object();

        private to_cloud() { }
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
            int queryUser;
            if ((currentUser = SyncTestServer.Static.Helpers.FindUserDevice(serverData, toProcess, out currentDevice)) == null
                || !int.TryParse(queryString[CloudApiPublic.Model.CLDefinitions.QueryStringUserId], out queryUser)
                || currentUser.Id != queryUser)
            {
                SyncTestServer.Static.Helpers.WriteUnauthorizedResponse(toProcess);
            }
            else
            {
                SyncTestServer.Static.Helpers.WriteRandomETag(toProcess);

                CloudApiPublic.JsonContracts.To syncRequest = (CloudApiPublic.JsonContracts.To)CloudApiPublic.JsonContracts.JsonContractHelpers.ToSerializer.ReadObject(toProcess.Request.InputStream);

                long newSyncId = serverData.NewSyncIdBeforeStart;
                
                IEnumerable<CloudApiPublic.JsonContracts.Event> fromEvents;
                lock (currentUser)
                {
                    fromEvents = serverData.GrabEventsAfterLastSync(syncRequest.SyncId, null, currentUser, newSyncId);

                    foreach (CloudApiPublic.JsonContracts.Event toEvent in syncRequest.Events)
                    {
                        serverData.ApplyClientEventToServer(newSyncId, currentUser, currentDevice, toEvent);
                    }
                }

                CloudApiPublic.JsonContracts.To toWrite = new CloudApiPublic.JsonContracts.To()
                {
                    SyncId = newSyncId.ToString(),
                    Events = fromEvents.Concat(syncRequest.Events).ToArray()
                };

                string responseBody;

                using (MemoryStream ms = new MemoryStream())
                {
                    CloudApiPublic.JsonContracts.JsonContractHelpers.ToSerializer.WriteObject(ms, toWrite);
                    responseBody = Encoding.Default.GetString(ms.ToArray());
                }

                byte[] requestBodyBytes = Encoding.UTF8.GetBytes(responseBody);

                toProcess.Response.ContentType = "application/json; charset=utf-8";
                toProcess.Response.SendChunked = true;
                toProcess.Response.StatusCode = 200;

                toProcess.Response.OutputStream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
            }
        }
    }
}