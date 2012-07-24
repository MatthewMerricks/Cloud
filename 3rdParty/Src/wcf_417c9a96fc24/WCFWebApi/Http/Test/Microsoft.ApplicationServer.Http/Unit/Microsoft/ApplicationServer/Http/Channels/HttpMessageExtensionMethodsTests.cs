// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageExtensionMethodsTests
    {
        #region Type Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MessageExtensionMethods is a public class.")]
        public void MessageExtensionMethods_Is_A_Public_Class()
        {
            Type type = typeof(HttpMessageExtensionMethods);
            Assert.IsTrue(type.IsPublic, "MessageExtensionMethods should be public.");
            Assert.IsTrue(type.IsVisible, "MessageExtensionMethods should be visible.");
            Assert.IsTrue(type.IsClass, "MessageExtensionMethods should be a class.");
        }

        #endregion Type Tests

        #region ToHttpRequestMessage Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage returns null for non-HttpMessage instances.")]
        public void ToHttpRequestMessage_Returns_Null()
        {
            Message message = Message.CreateMessage(MessageVersion.None, "");
            Assert.IsNull(message.ToHttpRequestMessage(), "Message.ToHttpRequestMessage should have returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage returns the original HttpRequestMessage instance with which the HttpMessage was created.")]
        public void ToHttpRequestMessage_Returns_The_Original_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message wcfRequest = request.ToMessage();
            HttpRequestMessage requestRoundTripped = wcfRequest.ToHttpRequestMessage();
            Assert.IsNotNull(requestRoundTripped, "Message.ToHttpRequestMessage should not have returned null.");
            Assert.AreSame(request, requestRoundTripped, "Message.ToHttpRequestMessage should have returned the orignal instance of HttpRequestMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage extracts the original HttpRequestMessage instance with which the HttpMessage was created.")]
        public void ToHttpRequestMessage_Extracts_The_Original_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message wcfRequest = request.ToMessage();

            HttpRequestMessage requestRoundTripped = wcfRequest.ExtractHttpRequestMessage();
            Assert.IsNotNull(requestRoundTripped, "Message.ExtractHttpRequestMessage should not have returned null.");
            Assert.AreSame(request, requestRoundTripped, "Message.ExtractHttpRequestMessage should have returned the orignal instance of HttpRequestMessage.");

            HttpRequestMessage requestSecond = wcfRequest.ExtractHttpRequestMessage();
            Assert.IsNull(requestSecond, "Message.ExtractHttpRequestMessage should have returned null.");

            HttpRequestMessage requestThird = wcfRequest.ToHttpRequestMessage();
            Assert.IsNull(requestThird, "Message.ToHttpRequestMessage should have returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage returns null if the HttpMessage was created with an HttpResponseMessage.")]
        public void ToHttpRequestMessage_Returns_Null_For_HttpResponseMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Message wcfResponse = response.ToMessage();
            Assert.IsNull(wcfResponse.ToHttpRequestMessage(), "Message.ToHttpRequestMessage should have returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage throws ObjectDisposedException if the message has been closed.")]
        public void ToHttpRequestMessage_Throws_If_Message_Is_Closed()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message wcfRequest = request.ToMessage();
            wcfRequest.Close();

            ExceptionAssert.Throws<ObjectDisposedException>(
                SR.MessageClosed,
                () =>
                {
                    wcfRequest.ToHttpRequestMessage();
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage returns the original HttpRequestMessage instance even when called multiple times.")]
        public void ToHttpRequestMessage_Can_Be_Called_Multiple_Times()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message wcfRequest = request.ToMessage();
            HttpRequestMessage requestRoundTripped = wcfRequest.ToHttpRequestMessage();
            Assert.IsNotNull(requestRoundTripped, "Message.ToHttpRequestMessage should not have returned null.");
            Assert.AreSame(request, requestRoundTripped, "Message.ToHttpRequestMessage should have returned the orignal instance of HttpRequestMessage.");

            HttpRequestMessage requestRoundTrippedAgain = wcfRequest.ToHttpRequestMessage();
            Assert.IsNotNull(requestRoundTrippedAgain, "Message.ToHttpRequestMessage should not have returned null.");
            Assert.AreSame(request, requestRoundTrippedAgain, "Message.ToHttpRequestMessage should have returned the orignal instance of HttpRequestMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage does not change the Message state.")]
        public void ToHttpRequestMessage_Does_Not_Change_Message_State()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message wcfRequest = request.ToMessage();
            MessageState state = wcfRequest.State;
            wcfRequest.ToHttpRequestMessage();
            Assert.AreEqual(state, wcfRequest.State, "Message.State should be the same before and after calling Message.ToHttpRequestMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpRequestMessage throws ArgumentNullException when the message is null.")]
        public void ToHttpRequestMessage_Throws_When_This_Is_Null()
        {
            Message message = null;
            ExceptionAssert.ThrowsArgumentNull(
                "message",
                () =>
                {
                    message.ToHttpRequestMessage();
                });
        }

        #endregion ToHttpRequestMessage Tests

        #region ToHttpResponseMessage Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage returns null for non-HttpMessage instances")]
        public void ToHttpResponseMessage_Returns_Null()
        {
            Message message = Message.CreateMessage(MessageVersion.None, "");
            Assert.IsNull(message.ToHttpResponseMessage(), "Message.ToHttpResponseMessage should have returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage returns the original HttpResponseMessage instance with which the HttpMessage was created.")]
        public void ToHttpResponseMessage_Returns_The_Original_HttpResponseMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Message wcfResponse = response.ToMessage();
            HttpResponseMessage responseRoundTripped = wcfResponse.ToHttpResponseMessage();
            Assert.IsNotNull(responseRoundTripped, "Message.ToHttpResponseMessage should not have returned null.");
            Assert.AreSame(response, responseRoundTripped, "Message.ToHttpResponseMessage should have returned the orignal instance of HttpResponseMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage extracts the original HttpResponseMessage instance with which the HttpMessage was created.")]
        public void ToHttpResponseMessage_Extracts_The_Original_HttpResponseMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Message wcfResponse = response.ToMessage();

            HttpResponseMessage responseRoundTripped = wcfResponse.ExtractHttpResponseMessage();
            Assert.IsNotNull(responseRoundTripped, "Message.ExtractHttpResponseMessage should not have returned null.");
            Assert.AreSame(response, responseRoundTripped, "Message.ExtractHttpResponseMessage should have returned the orignal instance of HttpResponseMessage.");

            HttpResponseMessage responseSecond = wcfResponse.ExtractHttpResponseMessage();
            Assert.IsNull(responseSecond, "Message.ExtractHttpResponseMessage should have returned null.");

            HttpResponseMessage responseThird = wcfResponse.ToHttpResponseMessage();
            Assert.IsNull(responseThird, "Message.ToHttpResponseMessage should have returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage returns null if the HttpMessage was created with an HttpRequestMessage.")]
        public void ToHttpResponseMessage_Returns_Null_For_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message wcfRequest = request.ToMessage();
            Assert.IsNull(wcfRequest.ToHttpResponseMessage(), "Message.ToHttpResponseMessage should have returned null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage throws ObjectDisposedException if the message has been closed.")]
        public void ToHttpResponseMessage_Throws_If_Message_Is_Closed()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Message wcfResponse = response.ToMessage();
            wcfResponse.Close();

            ExceptionAssert.Throws<ObjectDisposedException>(
                SR.MessageClosed,
                () =>
                {
                    wcfResponse.ToHttpResponseMessage();
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage returns the original HttpResponseMessage instance even when called multiple times.")]
        public void ToHttpResponseMessage_Can_Be_Called_Multiple_Times()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Message wcfResponse = response.ToMessage();
            HttpResponseMessage responseRoundTripped = wcfResponse.ToHttpResponseMessage();
            Assert.IsNotNull(responseRoundTripped, "Message.ToHttpResponseMessage should not have returned null.");
            Assert.AreSame(response, responseRoundTripped, "Message.ToHttpResponseMessage should have returned the orignal instance of HttpResponseMessage.");

            HttpResponseMessage responseRoundTrippedAgain = wcfResponse.ToHttpResponseMessage();
            Assert.IsNotNull(responseRoundTrippedAgain, "Message.ToHttpResponseMessage should not have returned null.");
            Assert.AreSame(response, responseRoundTrippedAgain, "Message.ToHttpResponseMessage should have returned the orignal instance of HttpResponseMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage does not change the Message state.")]
        public void ToHttpResponseMessage_Does_Not_Change_Message_State()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            Message wcfResponse = response.ToMessage();
            MessageState state = wcfResponse.State;
            wcfResponse.ToHttpResponseMessage();
            Assert.AreEqual(state, wcfResponse.State, "Message.State should be the same before and after calling Message.ToHttpResponseMessage.");
            HttpResponseMessage responseRoundTripped = wcfResponse.ToHttpResponseMessage();
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Message.ToHttpResponseMessage throws ArgumentNullException when the message is null.")]
        public void ToHttpResponseMessage_Throws_When_This_Is_Null()
        {
            Message message = null;
            ExceptionAssert.ThrowsArgumentNull(
                "message",
                () =>
                {
                    message.ToHttpResponseMessage();
                });
        }

        #endregion ToHttpResponseMessage Tests

        #region ToMessage Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpRequestMessage.ToMessage always returns an HttpMessage instance.")]
        public void ToMessage_Returns_HttpMessage_For_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            Message message = request.ToMessage();
            Assert.IsNotNull(message, "HttpRequestMessage.ToMessage should never return null.");
            Assert.IsInstanceOfType(message, typeof(HttpMessage), "HttpRequestMessage.ToMessage should have returned an HttpMessage instance.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpRequestMessage.ToMessage returns an HttpMessage instance in which IsRequest is 'true'.")]
        public void ToMessage_Returns_HttpMessage_With_IsRequest_True()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = request.ToMessage() as HttpMessage;
            Assert.IsTrue(message.IsRequest, "HttpRequestMessage.ToMessage should have returned an HttpMessage instance in which IsRequest is 'true'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpRequestMessage.ToMessage throws ArgumentNullException when the HttpRequestMessage is null.")]
        public void ToMessage_Throws_When_This_Is_Null_For_HttpRequestMessage()
        {
            HttpRequestMessage request = null;
            ExceptionAssert.ThrowsArgumentNull(
                "request",
                () =>
                {
                    request.ToMessage();
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpResponseMessage.ToMessage always returns an HttpMessage instance.")]
        public void ToMessage_Returns_HttpMessage_For_HttpResponseMessage()
        {
            HttpResponseMessage request = new HttpResponseMessage();
            Message message = request.ToMessage();
            Assert.IsNotNull(message, "HttpResponseMessage.ToMessage should never return null.");
            Assert.IsInstanceOfType(message, typeof(HttpMessage), "HttpRequestMessage.ToMessage should have returned an HttpMessage instance.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpResponseMessage.ToMessage returns an HttpMessage instance in which IsRequest is 'false'.")]
        public void ToMessage_Returns_HttpMessage_With_IsRequest_False()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = response.ToMessage() as HttpMessage;
            Assert.IsFalse(message.IsRequest, "HttpResponseMessage.ToMessage should have returned an HttpMessage instance in which IsRequest is 'false'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpRequestMessage.ToMessage throws ArgumentNullException when the HttpResponseMessage is null.")]
        public void ToMessage_Throws_When_This_Is_Null_For_HttpResponseMessage()
        {
            HttpResponseMessage response = null;
            ExceptionAssert.ThrowsArgumentNull(
                "response",
                () =>
                {
                    response.ToMessage();
                });
        }

        #endregion ToMessage Tests
    }
}
