// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Net;

    [Flags]
    public enum TestHeaderOptions
    {
        None = 0x00,
        InsertRequest = 0x01,
        ValidateResponse = 0x02
    }

    public static class TestServiceClient
    {
        public static HttpClient CreateClient()
        {
            return TestServiceClient.CreateClient(true);
        }

        public static HttpClient CreateClient(bool allowAutoRedirect)
        {
            var client = new HttpClient(TestServiceCommon.ServiceAddress);
            client.Channel = new WebRequestChannel()
            {
                 AllowAutoRedirect = allowAutoRedirect
            };
            return client;
        }

        public static HttpClient CreateClient(bool allowAutoRedirect, Uri serviceAddress)
        {
            var client = new HttpClient(serviceAddress);
            client.Channel = new WebRequestChannel()
            {
                AllowAutoRedirect = allowAutoRedirect
            }; 
            return client;
        }

        public static ICollection<HttpResponseMessage> RunClient(HttpClient client, TestHeaderOptions options)
        {
            return TestServiceClient.RunClient(client, options, HttpMethod.Get);
        }
        
        public static ICollection<HttpResponseMessage> RunClient(HttpClient client, TestHeaderOptions options, HttpMethod method)
        {
            var result = new HttpResponseMessage[TestServiceCommon.Iterations];
            for (var cnt = 0; cnt < TestServiceCommon.Iterations; cnt++)
            {
                var httpRequest = new HttpRequestMessage(method, "");
                if ((options & TestHeaderOptions.InsertRequest) > 0)
                {
                    TestServiceCommon.AddRequestHeader(httpRequest, cnt);
                }

                try
                {
                    result[cnt] = client.Send(httpRequest);
                    Assert.IsNotNull(result[cnt]);
                }
                catch (HttpException he)
                {
                    var we = he.InnerException as WebException;
                    Assert.IsNull(we.Response, "Response should not be null.");
                    continue;
                }

                if ((options & TestHeaderOptions.ValidateResponse) > 0)
                {
                    TestServiceCommon.ValidateResponseTestHeader(result[cnt], cnt);
                }
            }

            Assert.AreEqual(TestServiceCommon.Iterations, result.Length);
            return result;
        }

        public static ICollection<HttpResponseMessage> RunClient(HttpClient client, TestHeaderOptions options, TimeSpan timeout)
        {
            var result = new HttpResponseMessage[TestServiceCommon.Iterations];
            using (var timer = new Timer(TestServiceClient.TimeoutHandler, client, timeout, timeout))
            {
                for (var cnt = 0; cnt < TestServiceCommon.Iterations; cnt++)
                {
                    var httpRequest = new HttpRequestMessage(HttpMethod.Get, "");
                    if ((options & TestHeaderOptions.InsertRequest) > 0)
                    {
                        TestServiceCommon.AddRequestHeader(httpRequest, cnt);
                    }

                    try
                    {
                        result[cnt] = client.Send(httpRequest);
                        Assert.IsNotNull(result[cnt]);
                    }
                    catch (OperationCanceledException)
                    {
                        continue;
                    }

                    if ((options & TestHeaderOptions.ValidateResponse) > 0)
                    {
                        TestServiceCommon.ValidateResponseTestHeader(result[cnt], cnt);
                    }
                }
            }

            Assert.AreEqual(TestServiceCommon.Iterations, result.Length);
            return result;
        }

        private static void TimeoutHandler(object state)
        {
            var client = state as HttpClient;
            try
            {
                client.CancelPendingRequests();
            }
            catch { }
        }
    }
}
