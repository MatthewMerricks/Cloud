using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;

namespace SyncTestServer
{
    public abstract class HttpActionSubprocessor
    {
        public abstract string InnerMethod { get; }
        public abstract void ProcessContext(HttpListenerContext toProcess, IServerData serverData, NameValueCollection queryString);
    }
}