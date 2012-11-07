using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;

namespace SyncTestServer
{
    public abstract class HttpActionProcessor
    {
        public abstract string HttpPrefix { get; }
        public abstract bool ProcessContext(HttpListenerContext toProcess, IServerData serverData, string listenerFirstPath, string listenerFullPath, NameValueCollection queryString);
    }
}