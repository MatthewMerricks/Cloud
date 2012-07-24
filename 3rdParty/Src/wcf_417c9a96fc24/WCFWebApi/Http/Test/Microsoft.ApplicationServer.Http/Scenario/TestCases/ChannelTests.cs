// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.CIT.Scenario.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BasicChannelTests
    {
        const string ChannelHttpReasonPhrase = "Hi there!";
        static readonly TimeSpan waitTimeout = TimeSpan.FromMilliseconds(500);

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicChannelTest1()
        {
            this.BasicChannelTestA(false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicChannelTest2()
        {
            this.BasicChannelTestA(true);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicChannelTest3()
        {
            this.BasicChannelTestB(false);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        public void BasicChannelTest4()
        {
            this.BasicChannelTestB(true);
        }

        private static void SubmitRequests(object data)
        {
            ManualResetEvent done = data as ManualResetEvent;
            Assert.IsNotNull(done);
            try
            {
                // Wait for channel to start receiving requests. The HttpListener should have started accepting 
                // requests at this point but on a few occasions there seems to be a delay causing the HttpClient
                // requests that we start below to fail. By inserting this sleep we should be able to rule out 
                // some lower level race condition.
                Thread.Sleep(BasicChannelTests.waitTimeout);

                using (var client = TestServiceClient.CreateClient())
                {
                    var result = TestServiceClient.RunClient(client, TestHeaderOptions.InsertRequest | TestHeaderOptions.ValidateResponse);
                    int cnt = 0;
                    foreach (var response in result)
                    {
                        cnt++;
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, string.Format("Check failed in response {0}", cnt));
                        Assert.AreEqual(BasicChannelTests.ChannelHttpReasonPhrase, response.ReasonPhrase, string.Format("Check failed in response {0}", cnt));
                    }
                }
            }
            finally
            {
                done.Set();
            }
        }

        private IReplyChannel OpenChannel(bool customBinding, out IChannelListener<IReplyChannel> listener)
        {
            var binding = TestServiceHost.CreateBinding(customBinding, new HttpMessageHandlerFactory(typeof(TestHandler)));
            listener = binding.BuildChannelListener<IReplyChannel>(TestServiceCommon.ServiceAddress);
            Assert.IsNotNull(listener);
            listener.Open();
            Assert.AreEqual(CommunicationState.Opened, listener.State);

            var channel = listener.AcceptChannel();
            Assert.IsNotNull(channel);
            channel.Open();
            Assert.AreEqual(CommunicationState.Opened, channel.State);

            return channel;
        }

        private void SendResponse(RequestContext context)
        {
            Assert.IsNotNull(context);

            // Validate request
            var request = context.RequestMessage;
            Assert.IsNotNull(request);
            var httpRequest = request.ToHttpRequestMessage();
            Assert.IsNotNull(httpRequest);

            // Create response
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK, BasicChannelTests.ChannelHttpReasonPhrase);
            Assert.IsNotNull(httpResponse);
            TestServiceCommon.CopyTestHeader(httpRequest, httpResponse);

            // Send response
            var response = httpResponse.ToMessage();
            Assert.IsNotNull(response);
            context.Reply(response);
        }

        private void BasicChannelTestA(bool customBinding)
        {
            IChannelListener<IReplyChannel> listener = null;
            IReplyChannel channel = null;
            ManualResetEvent done = new ManualResetEvent(false);
            Thread t = null;

            try
            {
                channel = this.OpenChannel(customBinding, out listener);
                Assert.IsNotNull(channel);
                Assert.IsNotNull(listener);

                t = new Thread(BasicChannelTests.SubmitRequests);
                t.Start(done);

                for (var cnt = 0; cnt < TestServiceCommon.Iterations; cnt++)
                {
                    using (var context = channel.ReceiveRequest())
                    {
                        this.SendResponse(context);
                    }
                }

                channel.Close(TestServiceCommon.DefaultHostTimeout);
                listener.Close(TestServiceCommon.DefaultHostTimeout);
            }
            catch (Exception e)
            {
                channel.Abort();
                listener.Abort();
                Assert.Fail("Unexpected exception: " + e);
            }
            finally
            {
                if (t != null && !done.WaitOne(TestServiceCommon.DefaultHostTimeout))
                {
                    t.Abort();
                }

                done.Dispose();
            }
        }

        private void BasicChannelTestB(bool customBinding)
        {
            IChannelListener<IReplyChannel> listener = null;
            IReplyChannel channel = null;
            ManualResetEvent done = new ManualResetEvent(false);
            Thread t = null;

            try
            {
                channel = this.OpenChannel(customBinding, out listener);
                Assert.IsNotNull(channel);
                Assert.IsNotNull(listener);

                t = new Thread(BasicChannelTests.SubmitRequests);
                t.Start(done);

                for (var cnt = 0; cnt < TestServiceCommon.Iterations; cnt++)
                {
                    RequestContext context;
                    if (channel.TryReceiveRequest(TestServiceCommon.DefaultHostTimeout, out context))
                    {
                        this.SendResponse(context);
                    }
                    else
                    {
                        Assert.Fail("TryReceiveRequest failed.");
                    }
                }

                channel.Close(TestServiceCommon.DefaultHostTimeout);
                listener.Close(TestServiceCommon.DefaultHostTimeout);
            }
            catch (Exception e)
            {
                channel.Abort();
                listener.Abort();
                Assert.Fail("Unexpected exception: " + e);
            }
            finally
            {
                if (t != null && !done.WaitOne(TestServiceCommon.DefaultHostTimeout))
                {
                    t.Abort();
                }

                done.Dispose();
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
}