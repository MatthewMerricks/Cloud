// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;

   [TestClass()]
    public static class TestServiceCommon
    {
        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(TaskScheduler_UnobservedTaskException);
        }

        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }

        private static readonly string testHeaderName = "TestHeader";

        private static readonly int clientIterations = 5;

        private static readonly Uri address = new Uri("http://localhost:8000/testservice/");

        private static readonly Uri redirectAddress = new Uri("http://localhost:8000/testservice");

        private static readonly Uri notFoundAddress = new Uri("http://localhost:8000/testservice/invalid");

        private static readonly TimeSpan defaultTimeout = TimeSpan.FromMilliseconds(TimeoutConstant.DefaultTimeout);

        public static TimeSpan DefaultHostTimeout { get { return TestServiceCommon.defaultTimeout; } }

        public static Uri ServiceAddress { get { return TestServiceCommon.address; } }

        public static Uri NotFoundAddress { get { return TestServiceCommon.notFoundAddress; } }

        public static Uri RedirectAddress { get { return TestServiceCommon.redirectAddress; } }

        public static string TestHeaderName { get { return TestServiceCommon.testHeaderName; } }

        public static int Iterations { get { return TestServiceCommon.clientIterations; } }

        public const int DefaultTimeout = 30 * 1000;

        public static void AddRequestHeader(HttpRequestMessage httpRequest, int index)
        {
            TestServiceCommon.AddRequestHeader(httpRequest, TestServiceCommon.TestHeaderName, index);
        }

        public static void AddRequestHeader(HttpRequestMessage httpRequest, string headerName, int index)
        {
            httpRequest.Headers.Add(headerName, index.ToString());
        }

        public static void AddResponseHeader(HttpResponseMessage httpResponse, int index)
        {
            TestServiceCommon.AddResponseHeader(httpResponse, TestServiceCommon.TestHeaderName, index);
        }

        public static void AddResponseHeader(HttpResponseMessage httpResponse, string headerName, int index)
        {
            httpResponse.Headers.Add(headerName, index.ToString());
        }

        public static void DetectRequestTestHeader(HttpRequestMessage httpRequest)
        {
            var requestHeader = httpRequest.Headers.GetValues(TestServiceCommon.TestHeaderName);
            Assert.AreEqual(1, requestHeader.Count());
        }

        public static void ValidateRequestTestHeader(HttpRequestMessage httpRequest, int index)
        {
            var requestHeader = httpRequest.Headers.GetValues(TestServiceCommon.TestHeaderName);
            Assert.AreEqual(1, requestHeader.Count());
            var headerValue = index.ToString();
            Assert.AreEqual(headerValue, requestHeader.ElementAt(0));
        }

        public static void DetectResponseTestHeader(HttpResponseMessage httpResponse)
        {
            var requestHeader = httpResponse.Headers.GetValues(TestServiceCommon.TestHeaderName);
            Assert.AreEqual(1, requestHeader.Count());
        }

        public static void ValidateResponseTestHeader(HttpResponseMessage httpResponse, int index)
        {
            var requestHeader = httpResponse.Headers.GetValues(TestServiceCommon.TestHeaderName);
            Assert.AreEqual(1, requestHeader.Count());
            var headerValue = index.ToString();
            Assert.AreEqual(headerValue, requestHeader.ElementAt(0));
        }

        public static void CopyTestHeader(HttpRequestMessage httpRequest, HttpResponseMessage httpResponse)
        {
            var requestHeader = httpRequest.Headers.GetValues(TestServiceCommon.TestHeaderName);
            Assert.AreEqual(1, requestHeader.Count());
            httpResponse.Headers.Add(TestServiceCommon.TestHeaderName, requestHeader);
        }

        public static void ValidateHtmlMediaType(MediaTypeHeaderValue mediatype)
        {
            Assert.AreEqual("text/html", mediatype.MediaType);
            Assert.AreEqual("utf-8", mediatype.CharSet);
        }

        public static void ValidateInternalServerErrorResponse(HttpResponseMessage httpResponse, bool faultDetail)
        {
            Assert.AreEqual(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
            Assert.AreEqual("Internal Server Error", httpResponse.ReasonPhrase);
            TestServiceCommon.ValidateHtmlMediaType(httpResponse.Content.Headers.ContentType);
            var body = httpResponse.Content.ReadAsString();
            Assert.IsTrue(body.IndexOf("Request Error", StringComparison.Ordinal) != -1);
            var index = body.IndexOf("The exception message is", StringComparison.Ordinal);
            if (faultDetail)
            {
                Assert.AreNotEqual(-1, index);
            }
            else
            {
                Assert.AreEqual(-1, index);
            }
        }

        public static void ValidateRedirectResponse(HttpResponseMessage httpResponse)
        {
            Assert.AreEqual(HttpStatusCode.RedirectKeepVerb, httpResponse.StatusCode);
            Assert.AreEqual("Temporary Redirect", httpResponse.ReasonPhrase);
            TestServiceCommon.ValidateHtmlMediaType(httpResponse.Content.Headers.ContentType);
            var body = httpResponse.Content.ReadAsString();
            Assert.IsTrue(body.IndexOf("you are being redirected there", StringComparison.Ordinal) != -1);
        }

        public static void ValidateNotFoundResponse(HttpResponseMessage httpResponse)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, httpResponse.StatusCode);
            Assert.AreEqual("Not Found", httpResponse.ReasonPhrase);
            TestServiceCommon.ValidateHtmlMediaType(httpResponse.Content.Headers.ContentType);
            var body = httpResponse.Content.ReadAsString();
            Assert.IsTrue(body.IndexOf("Endpoint not found", StringComparison.Ordinal) != -1);
        }

        public static void ValidateMethodNotAllowedResponse(HttpResponseMessage httpResponse)
        {
            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, httpResponse.StatusCode);
            Assert.AreEqual("Method Not Allowed", httpResponse.ReasonPhrase);
            TestServiceCommon.ValidateHtmlMediaType(httpResponse.Content.Headers.ContentType);
            var body = httpResponse.Content.ReadAsString();
            Assert.IsTrue(body.IndexOf("Method not allowed", StringComparison.Ordinal) != -1);
        }
    }
}
