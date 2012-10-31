using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Configuration;
using System.Net;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Web;

namespace Microsoft.WebSolutionsPlatform.Event
{
    class RequestListenMgr
    {
        static public HttpListener listener = null;

        static public void SetServicePrefix(string servicePrefix)
        {
            if (listener == null)
            {
                listener = new HttpListener();

                listener.Prefixes.Add(servicePrefix);

                listener.Start();

                IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
            }
            else
            {
                if (listener.Prefixes.Count > 0)
                {
                    foreach (string oldServicePrefix in listener.Prefixes)
                    {
                        if (string.Compare(oldServicePrefix, servicePrefix, true) == 0)
                        {
                            return;
                        }
                        else
                        {
                            listener.Prefixes.Add(servicePrefix);
                            listener.Prefixes.Remove(oldServicePrefix);
                        }
                    }
                }
                else
                {
                    listener.Prefixes.Add(servicePrefix);
                }
            }
        }

        public static void ListenerCallback(IAsyncResult result)
        {
            try
            {
                HttpListener listener = (HttpListener)result.AsyncState;
                HttpListenerContext context = listener.EndGetContext(result);

                listener.BeginGetContext(ListenerCallback, listener);

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                byte[] buffer = new byte[0];

                System.IO.Stream output = response.OutputStream;

                response.StatusCode = 200;
                response.AddHeader("Cache-Control", "no-cache");

                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + 
                    AppDomain.CurrentDomain.FriendlyName + ".config";

                buffer = System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(configFile));

                response.ContentLength64 = buffer.Length;

                output.Write(buffer, 0, buffer.Length);

                output.Close();

                response.Close();
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
            }
        }
    }
}
