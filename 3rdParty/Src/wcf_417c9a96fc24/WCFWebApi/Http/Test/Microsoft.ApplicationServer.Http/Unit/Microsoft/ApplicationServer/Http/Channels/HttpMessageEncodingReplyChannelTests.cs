// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageEncodingReplyChannelTests
    {
        #region Type Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel is an internal, non-abstract class.")]
        public void HttpMessageEncodingReplyChannel_Is_An_Internal_Non_Abstract_Class()
        {
            Type type = typeof(HttpMessageEncodingReplyChannel);
            Assert.IsTrue(type.IsNotPublic, "HttpMessageEncodingReplyChannel should not be public.");
            Assert.IsFalse(type.IsVisible, "HttpMessageEncodingReplyChannel should not be visible.");
            Assert.IsFalse(type.IsAbstract, "HttpMessageEncodingReplyChannel should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessageEncodingReplyChannel should be a class.");
        }

        #endregion Type Tests

        #region Property Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.LocalAddress calls LocalAddress on the inner channel.")]
        public void LocalAddress_Calls_LocalAdress_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);

            innerChannel.SetLocalAddress(new EndpointAddress("http://somehost.org"));
            Assert.AreSame(innerChannel.LocalAddress, channel.LocalAddress, "HttpMessageEncodingReplyChannel.LocalAddress should call LocalAddress on the inner channel.");
        }

        #endregion Property Tests

        #region Abort Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.Abort calls Abort on the inner channel.")]
        public void Abort_Calls_Abort_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            channel.Abort();
            Assert.IsTrue(innerChannel.OnAbortCalled, "HttpMessageEncodingReplyChannel.Abort should call Abort on the inner channel.");
        }

        #endregion Abort Tests

        #region Close Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.BeginClose calls BeginClose on the inner channel.")]
        public void BeginClose_Calls_BeginClose_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0,1,0);
            IAsyncResult result = channel.BeginClose(timeout, null, null);
            Assert.IsTrue(innerChannel.OnBeginCloseCalled, "HttpMessageEncodingReplyChannel.BeginClose should call BeginClose on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.BeginClose should have passed the timeout parameter to the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndClose calls EndClose on the inner channel.")]
        public void EndClose_Calls_EndClose_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            IAsyncResult result = channel.BeginClose(null, null);
            channel.EndClose(result);
            Assert.IsTrue(innerChannel.OnEndCloseCalled, "HttpMessageEncodingReplyChannel.EndClose should call EndClose on the inner channel.");
        }

        #endregion Close Tests

        #region Open Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.BeginOpen calls BeginOpen on the inner channel.")]
        public void BeginOpen_Calls_BeginOpen_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            IAsyncResult result = channel.BeginOpen(timeout, null, null);
            Assert.IsTrue(innerChannel.OnBeginOpenCalled, "HttpMessageEncodingReplyChannel.BeginOpen should call BeginOpen on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.BeginOpen should have passed the timeout parameter to the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndOpen calls EndOpen on the inner channel.")]
        public void EndOpen_Calls_EndOpen_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);

            IAsyncResult result = channel.BeginOpen(null, null);
            channel.EndOpen(result);
            Assert.IsTrue(innerChannel.OnEndOpenCalled, "HttpMessageEncodingReplyChannel.EndOpen should call EndOpen on the inner channel.");
        }

        #endregion Open Tests

        #region ReceiveRequest Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.ReceiveRequest calls ReceiveRequest on the inner channel.")]
        public void ReceiveRequest_Calls_ReceiveRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            innerChannel.RequestContextToReturn = new MockRequestContext();
            RequestContext context = channel.ReceiveRequest(timeout);
            Assert.IsTrue(innerChannel.ReceiveRequestCalled, "HttpMessageEncodingReplyChannel.ReceiveRequest should call ReceiveRequest on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.ReceiveRequest should have passed the timeout parameter to the inner channel.");
            Assert.IsInstanceOfType(context, typeof(HttpMessageEncodingRequestContext), "HttpMessageEncodingReplyChannel.ReceiveRequest should have returned an HttpMessageEncodingRequestContext instance.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.ReceiveRequest returns null if the inner channel returns null.")]
        public void ReceiveRequest_Returns_Null_If_Inner_Channel_Returns_Null()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = null;
            RequestContext context = channel.ReceiveRequest();
            Assert.IsNull(context, "HttpMessageEncodingReplyChannel.ReceiveRequest should have returned null since the inner channel returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.BeginReceiveRequest calls BeginReceiveRequest on the inner channel.")]
        public void BeginReceiveRequest_Calls_BeginReceiveRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            IAsyncResult result = channel.BeginReceiveRequest(timeout, null, null);
            Assert.IsTrue(innerChannel.BeginReceiveRequestCalled, "HttpMessageEncodingReplyChannel.BeginReceiveRequest should call BeginReceiveRequest on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.BeginReceiveRequest should have passed the timeout parameter to the inner channel.");
            Assert.AreSame(innerChannel.AsyncResultReturned, result, "HttpMessageEncodingReplyChannel.BeginReceiveRequest should have returned the async result from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndReceiveRequest calls EndReceiveRequest on the inner channel.")]
        public void EndReceiveRequest_Calls_EndReceiveRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = new MockRequestContext();
            IAsyncResult result = channel.BeginReceiveRequest(null, null);
            RequestContext context = channel.EndReceiveRequest(result);
            Assert.IsTrue(innerChannel.EndReceiveRequestCalled, "HttpMessageEncodingReplyChannel.EndReceiveRequest should call EndReceiveRequest on the inner channel.");
            Assert.IsInstanceOfType(context, typeof(HttpMessageEncodingRequestContext), "HttpMessageEncodingReplyChannel.EndReceiveRequest should have returned an HttpMessageEncodingRequestContext instance.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndReceiveRequest returns null if the inner channel returns null.")]
        public void EndReceiveRequest_Returns_Null_If_The_Inner_Channel_Returns_Null()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = null;
            IAsyncResult result = channel.BeginReceiveRequest(null, null);
            RequestContext context = channel.EndReceiveRequest(result);
            Assert.IsNull(context, "HttpMessageEncodingReplyChannel.EndReceiveRequest should have returned null since the inner channel returned null.");
        }

        #endregion ReceiveRequest Tests

        #region TryReceiveRequest Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.TryReceiveRequest calls TryReceiveRequest on the inner channel.")]
        public void TryReceiveRequest_Calls_TryReceiveRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            innerChannel.RequestContextToReturn = new MockRequestContext();
            innerChannel.TryReceiveRequestReturnsTrue = true;
            RequestContext context;
            bool didRecievedRequest = channel.TryReceiveRequest(timeout, out context);
            Assert.IsTrue(innerChannel.TryReceiveRequestCalled, "HttpMessageEncodingReplyChannel.TryReceiveRequest should call TryReceiveRequest on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.TryReceiveRequest should have passed the timeout parameter to the inner channel.");
            Assert.IsInstanceOfType(context, typeof(HttpMessageEncodingRequestContext), "HttpMessageEncodingReplyChannel.ReceiveRequest should have returned an HttpMessageEncodingRequestContext instance.");
            Assert.IsTrue(didRecievedRequest, "HttpMessageEncodingReplyChannel.EndWaitForRequest should have returned the value returned from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.TryReceiveRequest returns null if the inner channel returns null.")]
        public void TryReceiveRequest_Returns_Null_If_Inner_Channel_Returns_Null()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            innerChannel.RequestContextToReturn = null;
            innerChannel.TryReceiveRequestReturnsTrue = true;
            RequestContext context;
            bool didRecievedRequest = channel.TryReceiveRequest(timeout, out context);
            Assert.IsNull(context, "HttpMessageEncodingReplyChannel.TryReceiveRequest should have returned null since the inner channel returned null.");
            Assert.IsTrue(didRecievedRequest, "HttpMessageEncodingReplyChannel.EndWaitForRequest should have returned the value returned from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.TryReceiveRequest returns null if the inner channel returns false.")]
        public void TryReceiveRequest_Returns_Null_If_Inner_Channel_Returns_False()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            innerChannel.RequestContextToReturn = new MockRequestContext();
            innerChannel.TryReceiveRequestReturnsTrue = false;
            RequestContext context;
            bool didRecievedRequest = channel.TryReceiveRequest(timeout, out context);
            Assert.IsNull(context, "HttpMessageEncodingReplyChannel.TryReceiveRequest should have returned null since the inner channel returned false.");
            Assert.IsFalse(didRecievedRequest, "HttpMessageEncodingReplyChannel.EndWaitForRequest should have returned the value returned from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.BeginTryReceiveRequest calls BeginTryReceiveRequest on the inner channel.")]
        public void BeginTryReceiveRequest_Calls_BeginTryReceiveRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            IAsyncResult result = channel.BeginTryReceiveRequest(timeout, null, null);
            Assert.IsTrue(innerChannel.BeginTryReceiveRequestCalled, "HttpMessageEncodingReplyChannel.BeginTryReceiveRequest should call BeginTryReceiveRequest on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.BeginTryReceiveRequest should have passed the timeout parameter to the inner channel.");
            Assert.AreSame(innerChannel.AsyncResultReturned, result, "HttpMessageEncodingReplyChannel.BeginTryReceiveRequest should have returned the async result from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndTryReceiveRequest calls EndTryReceiveRequest on the inner channel.")]
        public void EndTryReceiveRequest_Calls_EndTryReceiveRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = new MockRequestContext();
            innerChannel.TryReceiveRequestReturnsTrue = true;
            IAsyncResult result = channel.BeginTryReceiveRequest(new TimeSpan(0, 1, 0), null, null);
            RequestContext context;
            bool didRecievedRequest = channel.EndTryReceiveRequest(result, out context);
            Assert.IsTrue(innerChannel.EndTryReceiveRequestCalled, "HttpMessageEncodingReplyChannel.EndTryReceiveRequest should call EndTryReceiveRequest on the inner channel.");
            Assert.IsTrue(didRecievedRequest, "HttpMessageEncodingReplyChannel.EndTryReceiveRequest should have returned the value returned from the inner channel.");
            Assert.IsInstanceOfType(context, typeof(HttpMessageEncodingRequestContext), "HttpMessageEncodingReplyChannel.EndTryReceiveRequest should have returned an HttpMessageEncodingRequestContext instance.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndTryReceiveRequest returns null if the inner channel returns null.")]
        public void EndTryReceiveRequest_Returns_Null_If_The_Inner_Channel_Returns_Null()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = null;
            innerChannel.TryReceiveRequestReturnsTrue = true;
            IAsyncResult result = channel.BeginTryReceiveRequest(new TimeSpan(0, 1, 0), null, null);
            RequestContext context;
            bool didRecievedRequest = channel.EndTryReceiveRequest(result, out context);
            Assert.IsNull(context, "HttpMessageEncodingReplyChannel.EndTryReceiveRequest should have returned null since the inner channel returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndTryReceiveRequest returns null if the inner channel returns false.")]
        public void EndTryReceiveRequest_Returns_Null_If_The_Inner_Channel_Returns_False()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = new MockRequestContext();
            innerChannel.TryReceiveRequestReturnsTrue = false;
            IAsyncResult result = channel.BeginTryReceiveRequest(new TimeSpan(0, 1, 0), null, null);
            RequestContext context;
            bool didRecievedRequest = channel.EndTryReceiveRequest(result, out context);
            Assert.IsNull(context, "HttpMessageEncodingReplyChannel.EndTryReceiveRequest should have returned null since the inner channel returned false.");
        }

        #endregion TryReceiveRequest Tests

        #region WaitForRequest Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.WaitForRequest calls WaitForRequest on the inner channel.")]
        public void WaitForRequest_Calls_WaitForRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            bool didRecievedRequest = channel.WaitForRequest(timeout);
            Assert.IsTrue(innerChannel.WaitForRequestCalled, "HttpMessageEncodingReplyChannel.WaitForRequest should call WaitForRequest on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.WaitForRequest should have passed the timeout parameter to the inner channel.");
            Assert.IsFalse(didRecievedRequest, "HttpMessageEncodingReplyChannel.EndWaitForRequest should have returned the value returned from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.BeginWaitForRequest calls BeginWaitForRequest on the inner channel.")]
        public void BeginWaitForRequest_Calls_BeginWaitForRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            TimeSpan timeout = new TimeSpan(0, 1, 0);
            IAsyncResult result = channel.BeginWaitForRequest(timeout, null, null);
            Assert.IsTrue(innerChannel.BeginWaitForRequestCalled, "HttpMessageEncodingReplyChannel.BeginWaitForRequest should call BeginWaitForRequest on the inner channel.");
            Assert.AreEqual(timeout, innerChannel.TimeoutParameter, "HttpMessageEncodingReplyChannel.BeginWaitForRequest should have passed the timeout parameter to the inner channel.");
            Assert.AreSame(innerChannel.AsyncResultReturned, result, "HttpMessageEncodingReplyChannel.BeginWaitForRequest should have returned the async result from the inner channel.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingReplyChannel.EndWaitForRequest calls EndWaitForRequest on the inner channel.")]
        public void EndWaitForRequest_Calls_EndWaitForRequest_On_The_Inner_Channel()
        {
            MockChannelListener channelManager = new MockChannelListener();
            MockReplyChannel innerChannel = new MockReplyChannel(channelManager);
            HttpMessageEncodingReplyChannel channel = new HttpMessageEncodingReplyChannel(channelManager, innerChannel);
            channel.Open();

            innerChannel.RequestContextToReturn = new MockRequestContext();
            IAsyncResult result = channel.BeginWaitForRequest(new TimeSpan(0, 1, 0), null, null);
            bool didRecievedRequest = channel.EndWaitForRequest(result);
            Assert.IsTrue(innerChannel.EndWaitForRequestCalled, "HttpMessageEncodingReplyChannel.EndWaitForRequest should call EndWaitForRequest on the inner channel.");
            Assert.IsFalse(didRecievedRequest, "HttpMessageEncodingReplyChannel.EndWaitForRequest should have returned the value returned from the inner channel.");
        }

        #endregion WaitForRequest Tests
    }
}
