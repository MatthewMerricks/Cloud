// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.ServiceModel;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// MSTest utility for testing a self-hosted WCF service.
    /// </summary>
    public static class ServiceHostAssert
    {
        private static int portNumber = 1000;

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted service provided by the <paramref name="onGetServiceHost"/> 
        /// <see cref="Func<,>"/> results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="onGetServiceHost">A <see cref="Func<,>"/> that returns a <see cref="ServiceHost"/> created with the given base address URI. Should not be <c>null</c>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted HTTP service. Should not be <c>null</c>.</param>
        /// <param name="expectedResponse">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        public static void Execute(Func<Uri, ServiceHost> onGetServiceHost, HttpRequestMessage request, HttpResponseMessage expectedResponse)
        {
            Assert.IsNotNull(onGetServiceHost, "The 'onGetServiceHost' parameter should not be null.");
            Assert.IsNotNull(request, "The 'request' parameter should not be null.");
            Assert.IsNotNull(expectedResponse, "The 'expectedResponse' parameter should not be null.");

            Execute(onGetServiceHost, request, (actualResponse) => HttpAssert.AreEqual(expectedResponse, actualResponse));
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted service provided by the <paramref name="onGetServiceHost"/> 
        /// <see cref="Func<,>"/> results in the actual <see cref="HttpResponseMessage"/> given by the <paramref name="onGetActualResponse"/> <see cref="Action<>"/>.
        /// </summary>
        /// <param name="onGetServiceHost">A <see cref="Func<,>"/> that returns a <see cref="ServiceHost"/> created with the given base address URI. Should not be <c>null</c>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted HTTP service. Should not be <c>null</c>.</param>
        /// <param name="onGetActualResponse">The actual <see cref="HttpResponseMessage"/> instance provided as an <see cref="Action<>"/>. Should not be <c>null</c>.</param>
        public static void Execute(Func<Uri, ServiceHost> onGetServiceHost, HttpRequestMessage request, Action<HttpResponseMessage> onGetActualResponse)
        {
            Assert.IsNotNull(onGetServiceHost, "The 'onGetServiceHost' parameter should not be null.");
            Assert.IsNotNull(request, "The 'request' parameter should not be null.");
            Assert.IsNotNull(onGetActualResponse, "The 'onGetActualResponse' parameter should not be null.");

            ServiceHost host = GetServiceHost(onGetServiceHost);
            
            try
            {
                HttpResponseMessage actualResponse = GetResponse(host, request);
                onGetActualResponse(actualResponse);
            }
            finally
            {
                host.Close();
            }
        }

        private static HttpResponseMessage GetResponse(ServiceHost host, HttpRequestMessage request)
        {
            UriBuilder builder = new UriBuilder(host.BaseAddresses[0]);
            builder.Host = Environment.MachineName;

            request.RequestUri = new Uri(request.RequestUri.ToString(), UriKind.Relative);

            using (HttpClient client = new HttpClient(builder.Uri))
            {
                client.Channel = new WebRequestChannel();            
                return client.Send(request);
            }
        }

        private static ServiceHost GetServiceHost(Func<Uri, ServiceHost> onGetServiceHost)
        {
            ServiceHost host = null;
            while (host == null)
            {
                try
                {
                    string baseAddress = string.Format("http://localhost:{0}/SomeBasePath/", GetNextPortNumber());
                    host = onGetServiceHost(new Uri(baseAddress));
                    if (host.State == CommunicationState.Created)
                    {
                        host.Open();
                    }

                    if (host.State != CommunicationState.Opened)
                    {
                        host.Close();
                        host = null;
                    }
                }
                catch (AddressAlreadyInUseException)
                {
                    host = null;
                }
                catch (UriFormatException)
                {
                    host = null;
                }
            }

            return host;
        }

        private static int GetNextPortNumber()
        {
            return Interlocked.Increment(ref portNumber);
        }
    }
}
