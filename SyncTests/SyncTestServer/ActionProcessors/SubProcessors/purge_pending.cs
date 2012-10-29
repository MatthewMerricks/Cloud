using System;
using System.Collections.Generic;
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
                        ?? (_innerMethod = this.GetType().Name);
                }
            }
        }
        private string _innerMethod = null;
        private readonly object InnerMethodLocker = new object();

        public override void ProcessContext(HttpListenerContext toProcess, IServerData serverData)
        {
            throw new NotImplementedException();
        }
    }
}