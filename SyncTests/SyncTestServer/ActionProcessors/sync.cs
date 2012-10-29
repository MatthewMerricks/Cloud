using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SyncTestServer.ActionProcessors
{
    public sealed class sync : HttpActionProcessor
    {
        #region singleton pattern
        public static sync Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new sync());
                }
            }
        }
        private static sync _instance = null;
        private static readonly object InstanceLocker = new object();

        private sync() { }
        #endregion

        public override string HttpPrefix
        {
            get
            {
                lock (HttpPrefixLocker)
                {
                    return _httpPrefix
                        ?? (_httpPrefix = (CLDefinitions.CLMetaDataServerURL.EndsWith("/")
                                ? CLDefinitions.CLMetaDataServerURL
                                : CLDefinitions.CLMetaDataServerURL + "/") +
                            this.GetType().Name + "/");
                }
            }
        }
        private string _httpPrefix = null;
        private readonly object HttpPrefixLocker = new object();

        public override bool ProcessContext(HttpListenerContext toProcess, IServerData serverData, string listenerFirstPath, string listenerFullPath)
        {
            throw new NotImplementedException();
        }
    }
}