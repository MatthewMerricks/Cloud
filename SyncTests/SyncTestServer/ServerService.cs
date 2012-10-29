using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SyncTestServer
{
    public class ServerService : IDisposable
    {
        #region singleton pattern
        /// <summary>
        /// Optional parameter is required if Instance has not been successfully retrieved before
        /// </summary>
        /// <param name="initialData"></param>
        /// <returns></returns>
        public static ServerService GetInstance(IServerData initialData = null)
        {
            lock (InstanceLocker)
            {
                return _instance
                    ?? (_instance = new ServerService(initialData));
            }
        }
        private static ServerService _instance = null;
        private static readonly object InstanceLocker = new object();
        #endregion

        #region private fields
        private static readonly HttpActionProcessor[] ActionProcessors = new HttpActionProcessor[]
        {
            global::SyncTestServer.ActionProcessors.sync.Instance,
            global::SyncTestServer.ActionProcessors.@private.Instance
        };
        private readonly Dictionary<string, HttpActionProcessor> HttpPrefixToActionProcessor = ActionProcessors.ToDictionary(httpPrefix => httpPrefix.HttpPrefix,
            StringComparer.InvariantCultureIgnoreCase);

        private readonly HttpListener listener = new HttpListener();
        private readonly Thread listenerThread = new Thread(new ParameterizedThreadStart(HandleListenerRequests));
        private readonly Thread[] listenerContextProcessors = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(currentProcessor => new Thread(new ParameterizedThreadStart(ListenerContextProcessor)))
            .ToArray();
        private readonly ManualResetEvent listenerStop = new ManualResetEvent(false);
        private readonly ManualResetEvent listenerReady = new ManualResetEvent(false);
        private readonly Queue<HttpListenerContext> listenerContexts = new Queue<HttpListenerContext>();
        private readonly IServerData ServerData;
        #endregion

        private ServerService(IServerData initialData)
        {
            if (!HttpListener.IsSupported)
            {
                throw new PlatformNotSupportedException("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            }

            if (initialData == null)
            {
                throw new NullReferenceException("initialData cannot be null");
            }
            this.ServerData = initialData;

            for (int actionProcessorIndex = 0; actionProcessorIndex < ActionProcessors.Length; actionProcessorIndex++)
            {
                listener.Prefixes.Add(ActionProcessors[actionProcessorIndex].HttpPrefix);
            }
            listener.Start();
            listenerThread.Start(this);

            for (int listenerContextProcessorIndex = 0; listenerContextProcessorIndex < listenerContextProcessors.Length; listenerContextProcessorIndex++)
            {
                listenerContextProcessors[listenerContextProcessorIndex].Start(this);
            }
        }

        #region listener async processing methods
        private static void HandleListenerRequests(object state)
        {
            ServerService thisService = state as ServerService;

            if (thisService == null)
            {
                System.Windows.MessageBox.Show("HandleListenerRequests state must be castable as ServerService");
            }
            else
            {
                while (thisService.listener.IsListening)
                {
                    IAsyncResult listenerAsync = thisService.listener.BeginGetContext(thisService.ListenerContextReady, null);

                    if (0 == WaitHandle.WaitAny(new WaitHandle[] { thisService.listenerStop, listenerAsync.AsyncWaitHandle }))
                    {
                        return;
                    }
                }
            }
        }

        private void ListenerContextReady(IAsyncResult listenerAsync)
        {
            try
            {
                lock (listenerContexts)
                {
                    listenerContexts.Enqueue(listener.EndGetContext(listenerAsync));
                    listenerReady.Set();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error on ListenerContextReady: " + ex.Message);
            }
        }

        private static void ListenerContextProcessor(object state)
        {
            ServerService thisService = state as ServerService;

            if (thisService == null)
            {
                System.Windows.MessageBox.Show("HandleListenerRequests state must be castable as ServerService");
            }
            else
            {
                WaitHandle[] toWaitOn = new WaitHandle[] { thisService.listenerReady, thisService.listenerStop };
                while (0 == WaitHandle.WaitAny(toWaitOn))
                {
                    HttpListenerContext dequeuedContext;
                    lock (thisService.listenerContexts)
                    {
                        if (thisService.listenerContexts.Count > 0)
                        {
                            dequeuedContext = thisService.listenerContexts.Dequeue();
                        }
                        else
                        {
                            thisService.listenerReady.Reset();
                            continue;
                        }
                    }

                    try
                    {
                        //process request here
                        ////CloudApiPublic.Model.CLDefinitions.HttpPrefix
                        // http://

                        ////dequeuedContext.Request.UserHostName
                        // mds-edge.cloudburrito.com

                        ////dequeuedContext.Request.RawUrl
                        // /private/purge-pending

                        int secondForwardSlash;
                        string addressThroughDomain = CloudApiPublic.Model.CLDefinitions.HttpPrefix + // http://
                            dequeuedContext.Request.UserHostName; // mds-edge.cloudburrito.com
                        string listenerFirstPath = addressThroughDomain +
                            ((secondForwardSlash = dequeuedContext.Request.RawUrl.IndexOf('/', 1)) < 0
                                ? dequeuedContext.Request.RawUrl // /get_file
                                : dequeuedContext.Request.RawUrl.Substring(0, secondForwardSlash)) + "/"; // /private/purge-pending
                        // result:
                        // http://mds-edge.cloudburrito.com/private/
                        // or
                        // http://upd-edge.cloudburrito.com/get_file/

                        string listenerFullPath = addressThroughDomain +
                            dequeuedContext.Request.RawUrl;

                        HttpActionProcessor retrieveProcessor;
                        if (!thisService.HttpPrefixToActionProcessor.TryGetValue(listenerFirstPath, out retrieveProcessor)
                            || !retrieveProcessor.ProcessContext(dequeuedContext, thisService.ServerData, listenerFirstPath, listenerFullPath))
                        {
                            System.Windows.MessageBox.Show("Unable to find processor at address: " +
                                listenerFullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error in ListenerContextProcessor: " + ex.Message);
                    }
                }
            }
        }
        #endregion

        #region private fields
        public bool Disposed
        {
            get
            {
                lock (this)
                {
                    return _disposed;
                }
            }
        }
        private bool _disposed = false;
        #endregion

        #region IDisposable members
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~ServerService()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private methods
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            // lock on current object for changing DelayCompleted so it cannot be stopped/started simultaneously
            lock (this)
            {
                if (!_disposed)
                {
                    // set delay completed so processing will not fire
                    _disposed = true;

                    if (disposing)
                    {
                        listenerStop.Set();
                        listenerThread.Join();
                        foreach (Thread contextProcessor in listenerContextProcessors)
                        {
                            contextProcessor.Join();
                        }
                        listener.Stop();
                    }
                    
                    // Dispose local unmanaged resources last
                }
            }
        }
        #endregion
    }
}