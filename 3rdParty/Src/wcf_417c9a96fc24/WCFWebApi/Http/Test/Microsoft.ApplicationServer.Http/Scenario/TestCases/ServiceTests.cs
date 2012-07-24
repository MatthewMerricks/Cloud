// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.CIT.Scenario.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NoFactoryTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoFactoryTest1()
        {
            this.NoFactoryTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoFactoryTest2()
        {
            this.NoFactoryTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoFactoryTest3()
        {
            this.NoFactoryTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoFactoryTest4()
        {
            this.NoFactoryTest(true, true);
        }

        private void NoFactoryTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, null))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var response in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, response.ReasonPhrase);
                    }
                }
            }
        }

        class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                return this.CreateMessage(httpResponse);
            }
        }

    }

    [TestClass]
    public class NoHandlerTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoHandlerTest1()
        {
            this.NoHandlerTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoHandlerTest2()
        {
            this.NoHandlerTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoHandlerTest3()
        {
            this.NoHandlerTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NoHandlerTest4()
        {
            this.NoHandlerTest(true, true);
        }

        private void NoHandlerTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory()))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var response in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, response.ReasonPhrase);
                    }
                }
            }
        }

        class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                return this.CreateMessage(httpResponse);
            }
        }
    }

    [TestClass]
    public class BasicHandlerTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicHandlerTest1()
        {
            this.BasicHandlerTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicHandlerTest2()
        {
            this.BasicHandlerTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicHandlerTest3()
        {
            this.BasicHandlerTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicHandlerTest4()
        {
            this.BasicHandlerTest(true, true);
        }

        private void BasicHandlerTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var response in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, response.ReasonPhrase);
                    }
                }
            }
        }

        class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                return this.CreateMessage(httpResponse);
            }
        }

        class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
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
    public class ShortCircuitHandlerTests
    {
        const string ReasonPhrase = "No way!";

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest1()
        {
            this.ShortCircuitHandlerTest(false, false, false);
        }
        
        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest2()
        {
            this.ShortCircuitHandlerTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest3()
        {
            this.ShortCircuitHandlerTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest4()
        {
            this.ShortCircuitHandlerTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest5()
        {
            this.ShortCircuitHandlerTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest6()
        {
            this.ShortCircuitHandlerTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest7()
        {
            this.ShortCircuitHandlerTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ShortCircuitHandlerTest8()
        {
            this.ShortCircuitHandlerTest(true, true, true);
        }

        private void ShortCircuitHandlerTest(bool asynchronousSendEnabled, bool customBinding, bool insertDummyHandler)
        {
            var handlers = insertDummyHandler ? new Type[] { typeof(TestHandler), typeof(DummyHandler) } : new Type[] { typeof(TestHandler) };
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(handlers)))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var response in result)
                    {
                        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                        Assert.AreEqual(ShortCircuitHandlerTests.ReasonPhrase, response.ReasonPhrase);
                    }
                }
            }
        }

        class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                Assert.Fail();
                return null;
            }
        }

        class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return Task.Factory.StartNew<HttpResponseMessage>(
                    task =>
                    {
                        var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized, ShortCircuitHandlerTests.ReasonPhrase);
                        TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                        return httpResponse;
                    },
                    cancellationToken);
            }
        }

        class DummyHandler : DelegatingChannel
        {
            public DummyHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
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
    public class HeaderHandlerTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void HeaderHandlerTest1()
        {
            this.HeaderHandlerTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void HeaderHandlerTest2()
        {
            this.HeaderHandlerTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void HeaderHandlerTest3()
        {
            this.HeaderHandlerTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void HeaderHandlerTest4()
        {
            this.HeaderHandlerTest(true, true);
        }
    
        private void HeaderHandlerTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var response in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, response.ReasonPhrase);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return this.CreateMessage(httpResponse);
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
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
    public class RequestFailureHandlerTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest1()
        {
            this.RequestFailureHandlerTest(false, false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest2()
        {
            this.RequestFailureHandlerTest(true, false, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest3()
        {
            this.RequestFailureHandlerTest(false, true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest4()
        {
            this.RequestFailureHandlerTest(true, true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest5()
        {
            this.RequestFailureHandlerTest(false, false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest6()
        {
            this.RequestFailureHandlerTest(true, false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest7()
        {
            this.RequestFailureHandlerTest(false, true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest8()
        {
            this.RequestFailureHandlerTest(true, true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest9()
        {
            this.RequestFailureHandlerTest(false, false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest10()
        {
            this.RequestFailureHandlerTest(true, false, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest11()
        {
            this.RequestFailureHandlerTest(false, true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest12()
        {
            this.RequestFailureHandlerTest(true, true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest13()
        {
            this.RequestFailureHandlerTest(false, false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest14()
        {
            this.RequestFailureHandlerTest(true, false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest15()
        {
            this.RequestFailureHandlerTest(false, true, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void RequestFailureHandlerTest16()
        {
            this.RequestFailureHandlerTest(true, true, true, true);
        }

        private void RequestFailureHandlerTest(bool asynchronousSendEnabled, bool customBinding, bool streamed, bool insertDummyHandler)
        {
            var handlers = insertDummyHandler ? new Type[] { typeof(TestHandler), typeof(DummyHandler) } : new Type[] { typeof(TestHandler) };
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, true, streamed, new HttpMessageHandlerFactory(handlers)))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateInternalServerErrorResponse(httpResponse, true);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return this.CreateMessage(httpResponse);
            }
        }

        public class TestHandler : DelegatingChannel
        {
            public TestHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                throw new Exception("Request Path Failure");
            }
        }

        class DummyHandler : DelegatingChannel
        {
            public DummyHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class ResponseFailureHandlerTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest1()
        {
            this.ResponseFailureHandlerTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest2()
        {
            this.ResponseFailureHandlerTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest3()
        {
            this.ResponseFailureHandlerTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest4()
        {
            this.ResponseFailureHandlerTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest5()
        {
            this.ResponseFailureHandlerTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest6()
        {
            this.ResponseFailureHandlerTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest7()
        {
            this.ResponseFailureHandlerTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void ResponseFailureHandlerTest8()
        {
            this.ResponseFailureHandlerTest(true, true, true);
        }

        private void ResponseFailureHandlerTest(bool asynchronousSendEnabled, bool customBinding, bool insertDummyHandler)
        {
            var handlers = insertDummyHandler ? new Type[] { typeof(TestHandler), typeof(DummyHandler) } : new Type[] { typeof(TestHandler) };
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(handlers)))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateInternalServerErrorResponse(httpResponse, true);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                return this.CreateMessage(httpResponse);
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
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        throw new Exception("Response Path Failure");
                    },
                    cancellationToken);
            }
        }

        class DummyHandler : DelegatingChannel
        {
            public DummyHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class CloseRequestContextTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestA1()
        {
            this.CloseRequestContextTestA(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestA2()
        {
            this.CloseRequestContextTestA(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestA3()
        {
            this.CloseRequestContextTestA(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestA4()
        {
            this.CloseRequestContextTestA(true, true);
        }

        private void CloseRequestContextTestA(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestServiceA>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.Accepted, httpResponse.StatusCode);
                        Assert.AreNotEqual(TestServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                    }
                }
            }
        }

        public class TestServiceA : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                OperationContext.Current.RequestContext.Close();
                return this.CreateMessage(httpResponse);
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestB1()
        {
            this.CloseRequestContextTestB(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestB2()
        {
            this.CloseRequestContextTestB(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestB3()
        {
            this.CloseRequestContextTestB(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseRequestContextTestB4()
        {
            this.CloseRequestContextTestB(true, true);
        }

        private void CloseRequestContextTestB(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestServiceB>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.Accepted, httpResponse.StatusCode);
                        Assert.AreNotEqual(TestServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                    }
                }
            }
        }

        public class TestServiceB : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                OperationContext.Current.RequestContext.Close(TestServiceCommon.DefaultHostTimeout);
                return this.CreateMessage(httpResponse);
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
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class AbortRequestContextTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortRequestContextTest1()
        {
            this.AbortRequestContextTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortRequestContextTest2()
        {
            this.AbortRequestContextTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortRequestContextTest3()
        {
            this.AbortRequestContextTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortRequestContextTest4()
        {
            this.AbortRequestContextTest(true, true);
        }

        private void AbortRequestContextTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                OperationContext.Current.RequestContext.Abort();
                return this.CreateMessage(httpResponse);
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
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class BadServiceTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest1()
        {
            this.BadServiceTest(false, false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest2()
        {
            this.BadServiceTest(true, false, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest3()
        {
            this.BadServiceTest(false, true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest4()
        {
            this.BadServiceTest(true, true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest5()
        {
            this.BadServiceTest(false, false, true, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest6()
        {
            this.BadServiceTest(true, false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest7()
        {
            this.BadServiceTest(false, true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest8()
        {
            this.BadServiceTest(true, true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest9()
        {
            this.BadServiceTest(false, false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest10()
        {
            this.BadServiceTest(true, false, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest11()
        {
            this.BadServiceTest(false, true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest12()
        {
            this.BadServiceTest(true, true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest13()
        {
            this.BadServiceTest(false, false, true, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest14()
        {
            this.BadServiceTest(true, false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest15()
        {
            this.BadServiceTest(false, true, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BadServiceTest16()
        {
            this.BadServiceTest(true, true, true, true);
        }


        private void BadServiceTest(bool asynchronousSendEnabled, bool customBinding, bool faultDetail, bool streamed)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, faultDetail, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);
                    foreach (var httpResponse in result)
                    {
                        TestServiceCommon.ValidateInternalServerErrorResponse(httpResponse, faultDetail);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                throw new Exception("Service failure");
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
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class OneWayServiceTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest1()
        {
            this.OneWayServiceTest(false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest2()
        {
            this.OneWayServiceTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest3()
        {
            this.OneWayServiceTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest4()
        {
            this.OneWayServiceTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest5()
        {
            this.OneWayServiceTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest6()
        {
            this.OneWayServiceTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest7()
        {
            this.OneWayServiceTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void OneWayServiceTest8()
        {
            this.OneWayServiceTest(true, true, true);
        }

        private void OneWayServiceTest(bool asynchronousSendEnabled, bool customBinding, bool streamed)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, false, streamed, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.Accepted, httpResponse.StatusCode);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
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
    public class TimedoutServiceTests
    {
        private static TimeSpan clientTimeout = TimeSpan.FromMilliseconds(1000);
        private static TimeSpan serverTimeout = TimeSpan.FromMilliseconds(100);

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void TimedoutServiceTest1()
        {
            this.TimedoutServiceTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void TimedoutServiceTest2()
        {
            this.TimedoutServiceTest(true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void TimedoutServiceTest3()
        {
            this.TimedoutServiceTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void TimedoutServiceTest4()
        {
            this.TimedoutServiceTest(true, true);
        }

        private void TimedoutServiceTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest, TimedoutServiceTests.clientTimeout);
                    foreach (var httpResponse in result)
                    {
                        Assert.IsNull(httpResponse);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                for (var cnt = 0; cnt < 20; cnt++)
                {
                    Thread.Sleep(TimedoutServiceTests.serverTimeout);
                }

                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                return this.CreateMessage(httpResponse);
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
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class NestedHandlerTests
    {
        public const string RequestHandlerHeader = "RequestHandler";

        public const string ResponseHandlerHeader = "ResponseHandler";

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NestedHandlerTest1()
        {
            this.NestedHandlerTest(false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NestedHandlerTest2()
        {
            this.NestedHandlerTest(true, false);
        }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NestedHandlerTest3()
        {
            this.NestedHandlerTest(false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void NestedHandlerTest4()
        {
            this.NestedHandlerTest(true, true);
        }

        private void NestedHandlerTest(bool asynchronousSendEnabled, bool customBinding)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding,
                new HttpMessageHandlerFactory(
                    typeof(TestHandler9),
                    typeof(TestHandler8),
                    typeof(TestHandler7),
                    typeof(TestHandler6),
                    typeof(TestHandler5),
                    typeof(TestHandler4),
                    typeof(TestHandler3),
                    typeof(TestHandler2),
                    typeof(TestHandler1),
                    typeof(TestHandler0))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var response in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, response.ReasonPhrase);

                        var responseHeader = response.Headers.GetValues(NestedHandlerTests.ResponseHandlerHeader);
                        Assert.AreEqual(1, responseHeader.Count());
                        int cnt = 9;
                        foreach (var value in responseHeader.ElementAt(0).Split(','))
                        {
                            Assert.AreEqual(cnt.ToString(), value.Trim());
                            cnt--;
                        }
                    }
                }
            }
        }

        class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);

                var requestHeader = httpRequest.Headers.GetValues(NestedHandlerTests.RequestHandlerHeader);
                Assert.AreEqual(10, requestHeader.Count());
                int cnt = 0;
                foreach (var value in requestHeader)
                {
                    Assert.AreEqual(cnt.ToString(), value);
                    cnt++;
                }

                var httpResponse = this.CreateResponse();
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                return this.CreateMessage(httpResponse);
            }
        }

        class TestHandler0 : DelegatingChannel
        {
            public TestHandler0(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 0);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 0);
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler1 : DelegatingChannel
        {
            public TestHandler1(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 1);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 1);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler2 : DelegatingChannel
        {
            public TestHandler2(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 2);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 2);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler3 : DelegatingChannel
        {
            public TestHandler3(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 3);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 3);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler4 : DelegatingChannel
        {
            public TestHandler4(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 4);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 4);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler5 : DelegatingChannel
        {
            public TestHandler5(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 5);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 5);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler6 : DelegatingChannel
        {
            public TestHandler6(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 6);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 6);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler7 : DelegatingChannel
        {
            public TestHandler7(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 7);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 7);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler8 : DelegatingChannel
        {
            public TestHandler8(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 8);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 8);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

        class TestHandler9 : DelegatingChannel
        {
            public TestHandler9(HttpMessageChannel innerChannel) : base(innerChannel) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
            {
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.AddRequestHeader(httpRequest, NestedHandlerTests.RequestHandlerHeader, 9);
                return base.SendAsync(httpRequest, cancellationToken).ContinueWith(
                    task =>
                    {
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        TestServiceCommon.AddResponseHeader(task.Result, NestedHandlerTests.ResponseHandlerHeader, 9);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }

    }

    [TestClass]
    public class CloseChannelTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA1()
        {
            this.CloseChannelTestA(false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA2()
        {
            this.CloseChannelTestA(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA3()
        {
            this.CloseChannelTestA(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA4()
        {
            this.CloseChannelTestA(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA5()
        {
            this.CloseChannelTestA(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA6()
        {
            this.CloseChannelTestA(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA7()
        {
            this.CloseChannelTestA(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestA8()
        {
            this.CloseChannelTestA(true, true, true);
        }

        private void CloseChannelTestA(bool asynchronousSendEnabled, bool customBinding, bool faultDetail)
        {
            using (var host = TestServiceHost.CreateHost<TestServiceA>(asynchronousSendEnabled, customBinding, faultDetail, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                    }
                }
            }
        }

        public class TestServiceA : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                OperationContext.Current.Channel.Close();
                return this.CreateMessage(httpResponse);
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB1()
        {
            this.CloseChannelTestB(false, false, false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB2()
        {
            this.CloseChannelTestB(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB3()
        {
            this.CloseChannelTestB(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB4()
        {
            this.CloseChannelTestB(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB5()
        {
            this.CloseChannelTestB(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB6()
        {
            this.CloseChannelTestB(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB7()
        {
            this.CloseChannelTestB(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void CloseChannelTestB8()
        {
            this.CloseChannelTestB(true, true, true);
        }

        private void CloseChannelTestB(bool asynchronousSendEnabled, bool customBinding, bool faultDetail)
        {
            using (var host = TestServiceHost.CreateHost<TestServiceB>(asynchronousSendEnabled, customBinding, faultDetail, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    foreach (var httpResponse in result)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, httpResponse.ReasonPhrase);
                    }
                }
            }
        }

        public class TestServiceB : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);
                OperationContext.Current.Channel.Close(TestServiceCommon.DefaultHostTimeout);
                return this.CreateMessage(httpResponse);
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
                        TestServiceCommon.DetectResponseTestHeader(task.Result);
                        return task.Result;
                    },
                    cancellationToken);
            }
        }
    }

    [TestClass]
    public class AbortChannelTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest1()
        {
            this.AbortChannelTest(false, false, false);
        }


        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest2()
        {
            this.AbortChannelTest(true, false, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest3()
        {
            this.AbortChannelTest(false, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest4()
        {
            this.AbortChannelTest(true, true, false);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest5()
        {
            this.AbortChannelTest(false, false, true);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest6()
        {
            this.AbortChannelTest(true, false, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest7()
        {
            this.AbortChannelTest(false, true, true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void AbortChannelTest8()
        {
            this.AbortChannelTest(true, true, true);
        }

        private void AbortChannelTest(bool asynchronousSendEnabled, bool customBinding, bool faultDetail)
        {
            using (var host = TestServiceHost.CreateHost<TestService>(asynchronousSendEnabled, customBinding, faultDetail, new HttpMessageHandlerFactory(typeof(TestHandler))))
            {
                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest);

                    // First response is 202 because we tear down the channel. Subsequent requests are normal as channel is new 
                    // even though OperationContext.Current.Channel still points to the old aborted channel
                    Assert.AreEqual(HttpStatusCode.Accepted, result.ElementAt(0).StatusCode);

                    for (var cnt = 1; cnt < TestServiceCommon.Iterations; cnt++)
                    {
                        Assert.AreEqual(HttpStatusCode.OK, result.ElementAt(cnt).StatusCode);
                        Assert.AreEqual(TestServiceBase.HttpReasonPhrase, result.ElementAt(cnt).ReasonPhrase);
                    }
                }
            }
        }

        public class TestService : TestServiceBase
        {
            public override Message HandleMessage(Message request)
            {
                var httpRequest = this.ValidateRequest(request);
                var httpResponse = this.CreateResponse();
                TestServiceCommon.DetectRequestTestHeader(httpRequest);
                OperationContext.Current.Channel.Abort();
                return this.CreateMessage(httpResponse);
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
}