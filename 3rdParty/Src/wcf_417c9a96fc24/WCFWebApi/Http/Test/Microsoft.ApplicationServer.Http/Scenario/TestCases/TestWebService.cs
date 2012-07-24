// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.CIT.Scenario.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OkServiceTests
    {
        static bool CopyHeaderInHandler { get; set; }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest1()
        {
            this.OkServiceTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest2()
        {
            this.OkServiceTest(true, false, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest3()
        {
            this.OkServiceTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest4()
        {
            this.OkServiceTest(true, true, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest5()
        {
            this.OkServiceTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest6()
        {
            this.OkServiceTest(true, false, true);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkServiceTest7()
        {
            this.OkServiceTest(false, true, true);
        }

        private void OkServiceTest(bool asynchronousSendEnabled, bool streamed, bool copyHeaderInHandler)
        {
            OkServiceTests.CopyHeaderInHandler = copyHeaderInHandler;
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestWebServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                        Assert.AreEqual("text/plain", httpResponse.Content.Headers.ContentType.MediaType);
                        Assert.AreEqual("utf-8", httpResponse.Content.Headers.ContentType.CharSet);
                        var body = httpResponse.Content.ReadAsString();
                        Assert.AreEqual(body, TestWebServiceBase.HttpResponseContent);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK, TestWebServiceBase.HttpReasonPhrase);
                response.Content = new StringContent(TestWebServiceBase.HttpResponseContent, Encoding.UTF8);
                if (!OkServiceTests.CopyHeaderInHandler)
                {
                    TestServiceCommon.CopyTestHeader(request, response);
                }

                return response;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        if (OkServiceTests.CopyHeaderInHandler)
                        {
                            TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        }
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class OkAfterRedirectServiceTests
    {

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkAfterRedirectServiceTest1()
        {
            this.OkAfterRedirectServiceTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkAfterRedirectServiceTest2()
        {
            this.OkAfterRedirectServiceTest(true, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkAfterRedirectServiceTest3()
        {
            this.OkAfterRedirectServiceTest(false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void OkAfterRedirectServiceTest4()
        {
            this.OkAfterRedirectServiceTest(true, true);
        }

        private void OkAfterRedirectServiceTest(bool asynchronousSendEnabled, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient(true, TestServiceCommon.RedirectAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestWebServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                        Assert.AreEqual("text/plain", httpResponse.Content.Headers.ContentType.MediaType);
                        Assert.AreEqual("utf-8", httpResponse.Content.Headers.ContentType.CharSet);
                        var body = httpResponse.Content.ReadAsString();
                        Assert.AreEqual(body, TestWebServiceBase.HttpResponseContent);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage  BasicOperation(HttpRequestMessage request)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK, TestWebServiceBase.HttpReasonPhrase);
                response.Content = new StringContent(TestWebServiceBase.HttpResponseContent, Encoding.UTF8);

                return response;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class NoDataServiceTests
    {
        static bool CopyHeaderInHandler { get; set; }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest1()
        {
            this.NoDataServiceTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest2()
        {
            this.NoDataServiceTest(true, false, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest3()
        {
            this.NoDataServiceTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest4()
        {
            this.NoDataServiceTest(true, true, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest5()
        {
            this.NoDataServiceTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest6()
        {
            this.NoDataServiceTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest7()
        {
            this.NoDataServiceTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NoDataServiceTest8()
        {
            this.NoDataServiceTest(true, true, true);
        }

        private void NoDataServiceTest(bool asynchronousSendEnabled, bool streamed, bool copyHeaderInHandler)
        {
            NoDataServiceTests.CopyHeaderInHandler = copyHeaderInHandler;
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestWebServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                        Assert.AreEqual(0, httpResponse.Content.Headers.ContentLength);
                        Assert.IsNull(httpResponse.Content.Headers.ContentType);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK, TestWebServiceBase.HttpReasonPhrase);
                if (!NoDataServiceTests.CopyHeaderInHandler)
                {
                    TestServiceCommon.CopyTestHeader(request, response);
                }

                return response;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        if (NoDataServiceTests.CopyHeaderInHandler)
                        {
                            TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        }
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class RedirectServiceTests
    {

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void RedirectServiceTest1()
        {
            this.RedirectServiceTest(false, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void RedirectServiceTest2()
        {
            this.RedirectServiceTest(true, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void RedirectServiceTest3()
        {
            this.RedirectServiceTest(false, true);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void RedirectServiceTest4()
        {
            this.RedirectServiceTest(true, true);
        }

        private void RedirectServiceTest(bool asynchronousSendEnabled, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient(false, TestServiceCommon.RedirectAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateRedirectResponse(httpResponse);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                Assert.Fail("Redirect didn't happen");

                return null;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class NotFoundServiceTests
    {

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NotFoundServiceTest1()
        {
            this.NotFoundServiceTest(false, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NotFoundServiceTest2()
        {
            this.NotFoundServiceTest(true, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NotFoundServiceTest3()
        {
            this.NotFoundServiceTest(false, true);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void NotFoundServiceTest4()
        {
            this.NotFoundServiceTest(true, true);
        }

        private void NotFoundServiceTest(bool asynchronousSendEnabled, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient(false, TestServiceCommon.NotFoundAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateNotFoundResponse(httpResponse);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                Assert.Fail("Not Found didn't happen");

                return null;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class MethodNotAllowedServiceTests
    {

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MethodNotAllowedServiceTest1()
        {
            this.MethodNotAllowedServiceTest(false, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MethodNotAllowedServiceTest2()
        {
            this.MethodNotAllowedServiceTest(true, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MethodNotAllowedServiceTest3()
        {
            this.MethodNotAllowedServiceTest(false, true);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MethodNotAllowedServiceTest4()
        {
            this.MethodNotAllowedServiceTest(true, true);
        }

        private void MethodNotAllowedServiceTest(bool asynchronousSendEnabled, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient(false, TestServiceCommon.ServiceAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse, HttpMethod.Options);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateMethodNotAllowedResponse(httpResponse);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                Assert.Fail("Method not allowed didn't happen");

                return null;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class MissingPropertyServiceTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest1()
        {
            this.MissingPropertyServiceTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest2()
        {
            this.MissingPropertyServiceTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest3()
        {
            this.MissingPropertyServiceTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest4()
        {
            this.MissingPropertyServiceTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest5()
        {
            this.MissingPropertyServiceTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest6()
        {
            this.MissingPropertyServiceTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest7()
        {
            this.MissingPropertyServiceTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void MissingPropertyServiceTest8()
        {
            this.MissingPropertyServiceTest(true, true, true);
        }

        private void MissingPropertyServiceTest(bool asynchronousSendEnabled, bool faultDetail, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, faultDetail, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient(false, TestServiceCommon.ServiceAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateInternalServerErrorResponse(httpResponse, faultDetail);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                Assert.Fail("Missing property didn't happen");

                return null;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);

                httpRequest.Properties.Clear();

                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        TestServiceCommon.CopyTestHeader(httpRequest, task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class PersistenceTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void PersistenceTest1()
        {
            this.PersistenceTest(false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void PersistenceTest2()
        {
            this.PersistenceTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void PersistenceTest3()
        {
            this.PersistenceTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void PersistenceTest4()
        {
            this.PersistenceTest(true, true);
        }

        private void PersistenceTest(bool asynchronousSendEnabled, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestWebServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                        Assert.AreEqual("text/plain", httpResponse.Content.Headers.ContentType.MediaType);
                        Assert.AreEqual("utf-8", httpResponse.Content.Headers.ContentType.CharSet);
                        var body = httpResponse.Content.ReadAsString();
                        Assert.AreEqual(body, TestWebServiceBase.HttpResponseContent);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK, TestWebServiceBase.HttpReasonPhrase);
                response.Content = new StringContent(TestWebServiceBase.HttpResponseContent, Encoding.UTF8);
                TestServiceCommon.CopyTestHeader(request, response);

                return response;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            int count = 0;

            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.AddRequestHeader(httpRequest, this.count++);

                return base.SendAsync(httpRequest, cancellationToken).ContinueWith<HttpResponseMessage>(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class DisposeHandlerTests
    {
        private static List<TestHandler> handlerList;

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void DisposeHandlerTest1()
        {
            this.DisposeHandlerTest(false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void DisposeHandlerTest2()
        {
            this.DisposeHandlerTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void DisposeHandlerTest3()
        {
            this.DisposeHandlerTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void DisposeHandlerTest4()
        {
            this.DisposeHandlerTest(true, true);
        }

        private void DisposeHandlerTest(bool asynchronousSendEnabled, bool streamed)
        {
            DisposeHandlerTests.handlerList = new List<TestHandler>();
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestWebServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                    }

                    Assert.AreEqual(1, DisposeHandlerTests.handlerList.Count);
                    Assert.IsFalse(DisposeHandlerTests.handlerList[0].IsDisposed);
                }
            }

            Assert.AreEqual(1, DisposeHandlerTests.handlerList.Count);
            Assert.IsTrue(DisposeHandlerTests.handlerList[0].IsDisposed);
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK, TestWebServiceBase.HttpReasonPhrase);
                TestServiceCommon.CopyTestHeader(request, response);

                return response;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            bool isDisposed;

            public TestHandler(HttpMessageChannel innerChannel)
                : base(innerChannel)
            {
                DisposeHandlerTests.handlerList.Add(this);
            }

            public bool IsDisposed
            {
                get { return this.isDisposed; }
            }

            protected override void Dispose(bool disposing)
            {
                Assert.IsFalse(this.IsDisposed);
                if (disposing)
                {
                    this.isDisposed = true;
                }
                base.Dispose(disposing);
            }
        }
    }

    [TestClass]
    public class BadHandlerConstructorServiceTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest1()
        {
            this.BadHandlerConstructorServiceTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest2()
        {
            this.BadHandlerConstructorServiceTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest3()
        {
            this.BadHandlerConstructorServiceTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest4()
        {
            this.BadHandlerConstructorServiceTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest5()
        {
            this.BadHandlerConstructorServiceTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest6()
        {
            this.BadHandlerConstructorServiceTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest7()
        {
            this.BadHandlerConstructorServiceTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadHandlerConstructorServiceTest8()
        {
            this.BadHandlerConstructorServiceTest(true, true, true);
        }

        private void BadHandlerConstructorServiceTest(bool asynchronousSendEnabled, bool faultDetail, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, faultDetail, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient(false, TestServiceCommon.ServiceAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.None);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateInternalServerErrorResponse(httpResponse, faultDetail);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                Assert.Fail("Bad handler constructor didn't happen 1");

                return null;
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel)
                : base(innerChannel)
            {
                throw new Exception("Handler constructor failure");
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                Assert.Fail("Bad handler constructor didn't happen 2");
                return null;
            }
        }
    }

    [TestClass]
    public class BadFactoryCreateServiceTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest1()
        {
            this.BadFactoryCreateServiceTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest2()
        {
            this.BadFactoryCreateServiceTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest3()
        {
            this.BadFactoryCreateServiceTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest4()
        {
            this.BadFactoryCreateServiceTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest5()
        {
            this.BadFactoryCreateServiceTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest6()
        {
            this.BadFactoryCreateServiceTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest7()
        {
            this.BadFactoryCreateServiceTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TestServiceCommon.DefaultTimeout)]
        
        public void BadFactoryCreateServiceTest8()
        {
            this.BadFactoryCreateServiceTest(true, true, true);
        }

        private void BadFactoryCreateServiceTest(bool asynchronousSendEnabled, bool faultDetail, bool streamed)
        {
            using (var host = TestServiceHost.CreateWebHost<TestService>(asynchronousSendEnabled, faultDetail, streamed, new TestHttpMessageHandlerFactory()))
            {
                using (var client = TestServiceClient.CreateClient(false, TestServiceCommon.ServiceAddress))
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.None);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateInternalServerErrorResponse(httpResponse, faultDetail);
                    }
                }
            }
        }

        public class TestService : TestWebServiceBase
        {
            public override HttpResponseMessage BasicOperation(HttpRequestMessage request)
            {
                Assert.Fail("Bad factory create method didn't happen 1");

                return null;
            }
        }

        public class TestHttpMessageHandlerFactory : HttpMessageHandlerFactory
        {
            protected override HttpMessageChannel OnCreate(HttpMessageChannel innerChannel)
            {
                return null;
            }
        }
    }
}