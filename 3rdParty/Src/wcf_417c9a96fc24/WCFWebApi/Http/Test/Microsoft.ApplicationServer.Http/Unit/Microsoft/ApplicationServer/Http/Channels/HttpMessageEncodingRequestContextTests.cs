// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageEncodingRequestContextTests
    {
        #region Type Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext is an internal, non-abstract class.")]
        public void HttpMessageEncodingRequestContext_Is_An_Internal_Non_Abstract_Class()
        {
            Type type = typeof(HttpMessageEncodingRequestContext);
            Assert.IsTrue(type.IsNotPublic, "HttpMessageEncodingRequestContext should not be public.");
            Assert.IsFalse(type.IsVisible, "HttpMessageEncodingRequestContext should not be visible.");
            Assert.IsFalse(type.IsAbstract, "HttpMessageEncodingRequestContext should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessageEncodingRequestContext should be a class.");
        }

        #endregion Type Tests

        #region Property Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage instance when the inner request is not an HttpMessage.")]
        public void RequestMessage_Returns_HttpMessage_When_Inner_Request_Is_Not_HttpMessage()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);
            
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.IsNotNull(message, "HttpMessageEncodingRequestContext.RequestMessage should not have returned null.");
            Assert.IsInstanceOfType(message, typeof(HttpMessage), "HttpMessageEncodingRequestContext.RequestMessage should not returned an HttpMessage instance.");
            Assert.IsNotNull(request, "ToHttpRequestMessage should have returned an HttpRequestMessage instance.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage instance when the inner request is an HttpMessage.")]
        public void RequestMessage_Returns_Same_HttpMessage_Instance_When_Inner_Request_Is_HttpMessage()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.IsNotNull(message, "HttpMessageEncodingRequestContext.RequestMessage should not have returned null.");
            Assert.IsInstanceOfType(message, typeof(HttpMessage), "HttpMessageEncodingRequestContext.RequestMessage should not returned an HttpMessage instance.");
            Assert.AreSame(innerMessage, message, "HttpMessageEncodingRequestContext.RequestMessage should have returned the same instance as the inner context's RequestMessage.");
            Assert.IsNotNull(request, "ToHttpRequestMessage should have returned an HttpRequestMessage instance.");
            Assert.AreSame(innerRequest, request, "HttpMessageEncodingRequestContext.RequestMessage should have returned the same HttpRequestMessage instance as the inner context.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns the same HttpMessage instance everytime.")]
        public void RequestMessage_Returns_Same_Instance_Everytime()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            Message message2 = requestContext.RequestMessage;

            Assert.AreSame(message, message2, "HttpMessageEncodingRequestContext.RequestMessage should have returned the same instance everytime.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage throws if the innerRequest message does not have an HttpRequestMessageProperty.")]
        public void RequestMessage_Throws_If_HttpRequestMessageProperty_Not_Present()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);

            ExceptionAssert.Throws<InvalidOperationException>(
                SR.RequestMissingHttpRequestMessageProperty(HttpRequestMessageProperty.Name, typeof(HttpRequestMessageProperty).FullName),
                () =>
                {
                    Message message = requestContext.RequestMessage;
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage throws if the innerRequest message does not have a To Header.")]
        public void RequestMessage_Throws_If_To_Header_Not_Present()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);

            ExceptionAssert.Throws<InvalidOperationException>(
                SR.RequestMissingToHeader,
                () =>
                {
                    Message message = requestContext.RequestMessage;
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage with the Uri determined by the 'To' header of the inner RequestContext's request message.")]
        public void RequestMessage_Uri_Determined_By_To_Header()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            innerRequest.RequestUri = new Uri("http://notThisUri.org");
            Message innerMessage = new HttpMessage(innerRequest);
            HttpRequestMessageProperty property = new HttpRequestMessageProperty();
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, property);
            innerMessage.Headers.To = new Uri("http://thisUri.org");

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            HttpRequestMessage request = message.ToHttpRequestMessage();
            Assert.AreEqual(new Uri("http://thisUri.org").ToString(), request.RequestUri.ToString(), "HttpRequestMessage.Uri should have been set to the 'To' header value of the inner RequestContext message.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage with the Method determined by the HttpRequestMessageProperty of the inner RequestContext's request message.")]
        public void RequestMessage_Method_Determined_By_HttpRequestMessageProperty()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            innerRequest.Method = HttpMethod.Get;
            Message innerMessage = new HttpMessage(innerRequest);
            HttpRequestMessageProperty property = new HttpRequestMessageProperty();
            property.Method = "PUT";
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, property);
            innerMessage.Headers.To = new Uri("http://thisUri.org");

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            HttpRequestMessage request = message.ToHttpRequestMessage();
            Assert.AreEqual(HttpMethod.Put, request.Method, "HttpRequestMessage.Method should have been set to the the value of the HttpRequestMessageProperty.Method.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage instance with an empty HttpContent when the inner request is not an HttpMessage.")]
        public void RequestMessage_Returns_HttpMessage_With_Empty_Content_When_Inner_Request_Is_Not_HttpMessage()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.IsNotNull(request.Content, "HttpRequestMessage.Content should not have been null.");
            Assert.AreEqual(0, request.Content.Headers.ContentLength, "HttpRequestMessage.Content length should have been zero.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an the same HttpContent instance when the inner request is an HttpMessage.")]
        public void RequestMessage_Returns_Same_HttpContent_Instance_When_Inner_Request_Is_HttpMessage()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            innerRequest.Content = new ByteArrayContent(new byte[5] { 1, 2, 3, 4, 5 });
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.AreSame(innerRequest.Content, request.Content, "The HttpRequestMessage.Content should not have been changed by the HttpMessageEncodingRequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage instance in the created state when the inner request is not an HttpMessage.")]
        public void RequestMessage_Returns_HttpMessage_In_Created_State_When_Inner_Request_Is_Not_HttpMessage()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            Assert.AreEqual(MessageState.Created, message.State, "HttpMessage.State should have been MessageState.Created.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage closes the inner message when the inner request is not an HttpMessage.")]
        public void RequestMessage_Closes_Inner_Message_When_Inner_Request_Is_Not_HttpMessage()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            Assert.AreEqual(MessageState.Closed, innerMessage.State, "HttpMessage.State should have been MessageState.Closed.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage does not change the state of the message when the inner request is an HttpMessage.")]
        public void RequestMessage_Does_Not_Change_The_Message_State_When_Inner_Request_Is_HttpMessage()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MessageState originalState = innerMessage.State;

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            Assert.AreEqual(originalState, message.State, "The state of the message should not have been changed by the HttpMessageEncodingRequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage copies the HTTP headers from the HttpRequestMessageProperty of the inner message to the HttpRequestMessage.")]
        public void RequestMessage_Returns_HttpMessage_With_HttpRequestHeaders_From_The_HttpRequestMessageProperty()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            innerRequest.Content = new ByteArrayContent(new byte[] { 0, 1, 2, 3, 4 });
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            HttpRequestMessageProperty property = new HttpRequestMessageProperty();
            property.Headers.Add(HttpRequestHeader.Host, "someHost.org");
            property.Headers.Add(HttpRequestHeader.UserAgent, "SomeUserAgent");
            property.Headers.Add(HttpRequestHeader.ContentType, "someType/someSubType");
            property.Headers.Add(HttpRequestHeader.ContentLength, "5");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, property);

            MessageState originalState = innerMessage.State;

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.AreEqual(2, request.Headers.Count(), "HttpRequestMessage.Headers.Count should have been two.");
            Assert.AreEqual(2, request.Content.Headers.Count(), "HttpRequestMessage.Headers.Count should have been two.");
            Assert.AreEqual("someHost.org", request.Headers.Host, "The host name header should have been 'someHost.org'.");
            Assert.AreEqual("SomeUserAgent", request.Headers.UserAgent.First().Product.ToString(), "The user agent header should have been 'SomeUserAgent'.");
            Assert.AreEqual("someType/someSubType", request.Content.Headers.ContentType.MediaType, "The content type header should have been 'someType/someSubType'.");
            Assert.AreEqual(5, request.Content.Headers.ContentLength, "The content length header should have been five.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage removes all headers from the HttpRequestMessage before adding those from the HttpRequestMessageProperty.")]
        public void RequestMessage_Returns_HttpMessage_With_HttpRequestHeaders_Only_From_The_HttpRequestMessageProperty()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            innerRequest.Content = new ByteArrayContent(new byte[] { 0, 1, 2, 3, 4 });
            innerRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("notThisType/notThisSubType");
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            HttpRequestMessageProperty property = new HttpRequestMessageProperty();
            property.Headers.Add(HttpRequestHeader.UserAgent, "SomeUserAgent");
            property.Headers.Add(HttpRequestHeader.ContentType, "someType/someSubType");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, property);

            MessageState originalState = innerMessage.State;

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.AreEqual(1, request.Headers.Count(), "HttpRequestMessage.Headers.Count should have been one.");
            Assert.AreEqual(1, request.Content.Headers.Count(), "HttpRequestMessage.Headers.Count should have been one.");
            Assert.AreEqual("SomeUserAgent", request.Headers.UserAgent.First().Product.ToString(), "The user agent header should have been 'SomeUserAgent'.");
            Assert.AreEqual("someType/someSubType", request.Content.Headers.ContentType.MediaType, "The content type header should have been 'someType/someSubType'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage with only a 'To' header.")]
        public void RequestMessage_Returns_HttpMessage_With_Only_To_Header()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            innerRequest.Content = new ByteArrayContent(new byte[] { 0, 1, 2, 3, 4 });
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            HttpRequestMessageProperty property = new HttpRequestMessageProperty();
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, property);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();

            Assert.AreEqual(1, message.Headers.Count, "HttpMessage.Headers.Count should have been 1.");
            Assert.AreEqual(new Uri("http://someHost.org/someService"), message.Headers.To, "HttpMessage.Headers.To should have been 'http://someHost.org/someService'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage without an HttpRequestMessageProperty when the inner request is not an HttpMessage.")]
        public void RequestMessage_Returns_HttpMessage_Sans_HttpRequestMessageProperty_When_Inner_Request_Is_Not_HttpMessage()
        {
            Message innerMessage = Message.CreateMessage(MessageVersion.None, string.Empty);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            Assert.IsFalse(message.Properties.Keys.Contains(HttpResponseMessageProperty.Name), "The HttpMessage instance should not have had an HttpRequestMessageProperty.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.RequestMessage returns an HttpMessage without an HttpRequestMessageProperty when the inner request is an HttpMessage.")]
        public void RequestMessage_Returns_Same_HttpMessage_Sans_HttpRequestMessageProperty_Instance_When_Inner_Request_Is_HttpMessage()
        {
            HttpRequestMessage innerRequest = new HttpRequestMessage();
            Message innerMessage = new HttpMessage(innerRequest);
            innerMessage.Headers.To = new Uri("http://someHost.org/someService");
            innerMessage.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.SetRequestMessage(innerMessage);

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            Message message = requestContext.RequestMessage;

            Assert.IsFalse(message.Properties.Keys.Contains(HttpResponseMessageProperty.Name), "The HttpMessage instance should not have had an HttpRequestMessageProperty.");
        }

        #endregion Property Tests

        #region Abort Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Abort calls Abort on the inner RequestContext.")]
        public void Abort_Calls_Inner_RequestContext()
        {
            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Abort();
            Assert.IsTrue(innerRequestContext.AbortCalled, "Abort should have been called on the inner RequestContext.");
        }

        #endregion Abort Tests

        #region Reply Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply provide same Message instance when the response is not an HttpMessage.")]
        public void Reply_Provides_Same_Message_Instance_To_Inner_RequestContext_When_Response_Is_Not_HttpMessage()
        {
            Message message = Message.CreateMessage(MessageVersion.None, string.Empty);
            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.IsNotNull(innerMessage, "HttpMessageEncodingRequestContext.Reply should not have provided null.");
                Assert.AreSame(message, innerMessage, "HttpMessageEncodingRequestContext.Reply should have provided the same message instance.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply ignores the HttpResponseMessageProperty and always returns status code 500 when the response is not an HttpMessage.")]
        public void Reply_Ignores_HttpResponseMessageProperty_When_Response_Is_Not_HttpMessage()
        {
            Message message = Message.CreateMessage(MessageVersion.None, string.Empty, "some content");
            HttpResponseMessageProperty property = new HttpResponseMessageProperty();
            property.StatusCode = HttpStatusCode.OK;
            property.StatusDescription = "SomeStatusDescription";
            property.SuppressEntityBody = false;
            property.Headers.Add(HttpResponseHeader.ContentType, "someType/someSubType");
            message.Properties.Add(HttpResponseMessageProperty.Name, property);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsNotNull(innerProperty, "The inner HttpMessage instance should have had an HttpResponseMessageProperty.");
                Assert.AreNotSame(property, innerProperty, "The inner HttpResponseMessageProperty should have been a different instance from the one on the response message.");

                Assert.AreEqual(HttpStatusCode.InternalServerError, innerProperty.StatusCode, "HttpResponseMessageProperty.StatusCode should have been HttpStatusCode.InternalServerError.");
                Assert.IsTrue(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'true'.");
                Assert.AreEqual(0, innerProperty.Headers.Count, "HttpResponseMessageProperty.Header.Count should have been zero.");
                Assert.IsNull(innerProperty.StatusDescription, "HttpResponseMessageProperty.StatusDescription should have been null.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply returns the same HttpMessage instance when the response is an HttpMessage.")]
        public void Reply_Returns_Same_HttpMessage_Instance_When_Response_Is_HttpMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            HttpMessage message = new HttpMessage(response);
            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.IsNotNull(innerMessage, "HttpMessageEncodingRequestContext.Reply should not have returned null.");
                Assert.IsInstanceOfType(innerMessage, typeof(HttpMessage), "HttpMessageEncodingRequestContext.Reply should have returned an HttpMessage instance.");
                Assert.AreSame(message, innerMessage, "HttpMessageEncodingRequestContext.Reply should have provided the same message instance as the HttpMessageEncodingRequestContext received.");

                HttpResponseMessage innerResponse = innerMessage.ToHttpResponseMessage();
                Assert.IsNotNull(innerResponse, "ToHttpResponseMessage should not have returned null.");
                Assert.AreSame(response, innerResponse, "HttpMessageEncodingRequestContext.Reply should have provided the same HttpResponseMessage instance as the HttpMessageEncodingRequestContext received.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply doesn't change the message state when the response is an HttpMessage.")]
        public void Reply_Does_Not_Change_Message_State_When_Response_Is_HttpMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);
            response.StatusCode = HttpStatusCode.OK;
            MessageState originalState = message.State;

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.AreEqual(originalState, innerMessage.State, "HttpMessageEncodingRequestContext.Reply should not have changed the message state.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply ignores the HttpResponseMessageProperty if the HttpMessage contains an HttpRequestMessage and returns status code 500.")]
        public void Reply_Ignores_HttpResponseMessageProperty_If_Contains_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.IsNotNull(innerMessage, "HttpMessageEncodingRequestContext.Reply should not have returned null.");
                Assert.IsInstanceOfType(innerMessage, typeof(HttpMessage), "HttpMessageEncodingRequestContext.Reply should have returned an HttpMessage instance.");
                Assert.AreSame(message, innerMessage, "HttpMessageEncodingRequestContext.Reply should have provided the same message instance as the HttpMessageEncodingRequestContext received.");

                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsNotNull(innerProperty, "The inner HttpMessage instance should have had an HttpResponseMessageProperty.");

                Assert.AreEqual(HttpStatusCode.InternalServerError, innerProperty.StatusCode, "HttpResponseMessageProperty.StatusCode should have been HttpStatusCode.InternalServerError.");
                Assert.IsTrue(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'true'.");
                Assert.AreEqual(0, innerProperty.Headers.Count, "HttpResponseMessageProperty.Header.Count should have been zero.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply throws if the message is closed.")]
        public void Reply_Throws_If__Message_Is_Closed()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);
            message.Close();

            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);

            ExceptionAssert.Throws<ObjectDisposedException>(
                SR.MessageClosed,
                () =>
                {
                    requestContext.Reply(message);
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply replaces the HttpResponseMessageProperty with the values from the HttpResponseMessage.")]
        public void Reply_Replaces_HttpResponseMessageProperty_When_Response_Is_HttpMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 1, 2, 3, 4, 5 });
            response.StatusCode = HttpStatusCode.Moved;
            response.ReasonPhrase = "SomeReason";
            response.Headers.Add("SomeHeader", "SomeHeaderValue");
            HttpMessage message = new HttpMessage(response);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsNotNull(innerProperty, "The inner HttpMessage instance should have had an HttpResponseMessageProperty.");

                Assert.AreEqual(HttpStatusCode.Moved, innerProperty.StatusCode, "HttpResponseMessageProperty.StatusCode should have been HttpStatusCode.Moved.");
                Assert.IsFalse(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'false'.");
                Assert.AreEqual(1, innerProperty.Headers.Count, "HttpResponseMessageProperty.Header.Count should have been 1.");
                Assert.AreEqual("SomeHeaderValue", innerProperty.Headers["SomeHeader"], "HttpResponseMessageProperty.Header 'SomeHeader' value should have been 'SomeHeaderValue'.");
                Assert.AreEqual("SomeReason", innerProperty.StatusDescription, "HttpResponseMessageProperty.StatusDescription value should have been 'SomeReason'.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply sets SuppressEntityBody when the HttpMessage has a null HttpContent.")]
        public void Reply_Sets_SuppressEntityBody_When_HttpContent_Of_HttpMessage_Is_Null()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = null;
            response.StatusCode = HttpStatusCode.OK;
            HttpMessage message = new HttpMessage(response);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsTrue(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'true'.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Reply(message);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "HttpMessageEncodingRequestContext.Reply should have called Reply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Reply calls Reply on the inner RequestContext and passes along the timeout parameter.")]
        public void Reply_With_TimeOut_Calls_Inner_RequestContext()
        {
            Message message = Message.CreateMessage(MessageVersion.None, string.Empty);
            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            TimeSpan timeout = new TimeSpan(0, 1, 0);
            requestContext.Reply(message, timeout);
            Assert.IsTrue(innerRequestContext.ReplyCalled, "Reply should have been called on the inner RequestContext.");
            Assert.AreEqual(timeout, innerRequestContext.Timeout, "HttpMessageEncodingRequestContext.Reply should have passed along the timeout instance to the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply provide same Message instance when the response is not an HttpMessage.")]
        public void BeginReply_Provides_Same_Message_Instance_To_Inner_RequestContext_When_Response_Is_Not_HttpMessage()
        {
            Message message = Message.CreateMessage(MessageVersion.None, string.Empty);
            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.IsNotNull(innerMessage, "HttpMessageEncodingRequestContext.Reply should not have provided null.");
                Assert.AreSame(message, innerMessage, "HttpMessageEncodingRequestContext.Reply should have provided the same message instance.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply ignores the HttpResponseMessageProperty and always returns status code 500 when the response is not an HttpMessage.")]
        public void BeginReply_Ignores_HttpResponseMessageProperty_When_Response_Is_Not_HttpMessage()
        {
            Message message = Message.CreateMessage(MessageVersion.None, string.Empty, "some content");
            HttpResponseMessageProperty property = new HttpResponseMessageProperty();
            property.StatusCode = HttpStatusCode.OK;
            property.SuppressEntityBody = false;
            property.Headers.Add(HttpResponseHeader.ContentType, "someType/someSubType");
            message.Properties.Add(HttpResponseMessageProperty.Name, property);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsNotNull(innerProperty, "The inner HttpMessage instance should have had an HttpResponseMessageProperty.");
                Assert.AreNotSame(property, innerProperty, "The inner HttpResponseMessageProperty should have been a different instance from the one on the response message.");

                Assert.AreEqual(HttpStatusCode.InternalServerError, innerProperty.StatusCode, "HttpResponseMessageProperty.StatusCode should have been HttpStatusCode.InternalServerError.");
                Assert.IsTrue(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'true'.");
                Assert.AreEqual(0, innerProperty.Headers.Count, "HttpResponseMessageProperty.Header.Count should have been zero.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply returns the same HttpMessage instance when the response is an HttpMessage.")]
        public void BeginReply_Returns_Same_HttpMessage_Instance_When_Response_Is_HttpMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            HttpMessage message = new HttpMessage(response);
            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.IsNotNull(innerMessage, "HttpMessageEncodingRequestContext.BeginReply should not have returned null.");
                Assert.IsInstanceOfType(innerMessage, typeof(HttpMessage), "HttpMessageEncodingRequestContext.BeginReply should have returned an HttpMessage instance.");
                Assert.AreSame(message, innerMessage, "HttpMessageEncodingRequestContext.BeginReply should have provided the same message instance as the HttpMessageEncodingRequestContext received.");

                HttpResponseMessage innerResponse = innerMessage.ToHttpResponseMessage();
                Assert.IsNotNull(innerResponse, "ToHttpResponseMessage should not have returned null.");
                Assert.AreSame(response, innerResponse, "HttpMessageEncodingRequestContext.BeginReply should have provided the same HttpResponseMessage instance as the HttpMessageEncodingRequestContext received.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply doesn't change the message state when the response is an HttpMessage.")]
        public void BeginReply_Does_Not_Change_Message_State_When_Response_Is_HttpMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);
            response.StatusCode = HttpStatusCode.OK;
            MessageState originalState = message.State;

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.AreEqual(originalState, innerMessage.State, "HttpMessageEncodingRequestContext.BeginReply should not have changed the message state.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply ignores the HttpResponseMessageProperty if the HttpMessage contains an HttpRequestMessage and returns status code 500.")]
        public void BeginReply_Ignores_HttpResponseMessageProperty_If_Contains_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                Assert.IsNotNull(innerMessage, "HttpMessageEncodingRequestContext.BeginReply should not have returned null.");
                Assert.IsInstanceOfType(innerMessage, typeof(HttpMessage), "HttpMessageEncodingRequestContext.BeginReply should have returned an HttpMessage instance.");
                Assert.AreSame(message, innerMessage, "HttpMessageEncodingRequestContext.BeginReply should have provided the same message instance as the HttpMessageEncodingRequestContext received.");

                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsNotNull(innerProperty, "The inner HttpMessage instance should have had an HttpResponseMessageProperty.");

                Assert.AreEqual(HttpStatusCode.InternalServerError, innerProperty.StatusCode, "HttpResponseMessageProperty.StatusCode should have been HttpStatusCode.InternalServerError.");
                Assert.IsTrue(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'true'.");
                Assert.AreEqual(0, innerProperty.Headers.Count, "HttpResponseMessageProperty.Header.Count should have been zero.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply throws if the message is closed.")]
        public void BeginReply_Throws_If_Message_Is_Closed()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);
            message.Close();

            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);

            ExceptionAssert.Throws<ObjectDisposedException>(
                SR.MessageClosed,
                () =>
                {
                    requestContext.BeginReply(message, null, null);
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply replaces the HttpResponseMessageProperty with the values from the HttpResponseMessage.")]
        public void BeginReply_Replaces_HttpResponseMessageProperty_When_Response_Is_HttpMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 1, 2, 3, 4, 5 });
            response.StatusCode = HttpStatusCode.Moved;
            response.Headers.Add("SomeHeader", "SomeHeaderValue");
            HttpMessage message = new HttpMessage(response);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsNotNull(innerProperty, "The inner HttpMessage instance should have had an HttpResponseMessageProperty.");

                Assert.AreEqual(HttpStatusCode.Moved, innerProperty.StatusCode, "HttpResponseMessageProperty.StatusCode should have been HttpStatusCode.Moved.");
                Assert.IsFalse(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'false'.");
                Assert.AreEqual(1, innerProperty.Headers.Count, "HttpResponseMessageProperty.Header.Count should have been 1.");
                Assert.AreEqual("SomeHeaderValue", innerProperty.Headers["SomeHeader"], "HttpResponseMessageProperty.Header 'SomeHeader' value should have been 'SomeHeaderValue'.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply sets SuppressEntityBody when the HttpMessage has a null HttpContent.")]
        public void BeginReply_Sets_SuppressEntityBody_When_HttpContent_Of_HttpMessage_Is_Null()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = null;
            response.StatusCode = HttpStatusCode.OK;
            HttpMessage message = new HttpMessage(response);

            MockRequestContext innerRequestContext = new MockRequestContext();
            innerRequestContext.OnReplyReceived = innerMessage =>
            {
                HttpResponseMessageProperty innerProperty = innerMessage.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                Assert.IsTrue(innerProperty.SuppressEntityBody, "HttpResponseMessageProperty.SuppressEntityBody should have been 'true'.");
            };

            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.BeginReply(message, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "HttpMessageEncodingRequestContext.BeginReply should have called BeginReply on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.BeginReply calls BeginReply on the inner RequestContext and passes along the timeout parameter.")]
        public void BeginReply_With_TimeOut_Calls_Inner_RequestContext()
        {
            Message message = Message.CreateMessage(MessageVersion.None, string.Empty);
            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            TimeSpan timeout = new TimeSpan(0, 1, 0);
            requestContext.BeginReply(message, timeout, null, null);
            Assert.IsTrue(innerRequestContext.BeginReplyCalled, "BeginReply should have been called on the inner RequestContext.");
            Assert.AreEqual(timeout, innerRequestContext.Timeout, "HttpMessageEncodingRequestContext.BeginReply should have passed along the timeout instance to the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.EndReply calls EndReply on the inner RequestContext.")]
        public void EndReply_Calls_Inner_RequestContext()
        {
            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.EndReply(null);
            Assert.IsTrue(innerRequestContext.EndReplyCalled, "EndReply should have been called on the inner RequestContext.");
        }

        #endregion Reply Tests

        #region Close Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Close calls Close on the inner RequestContext.")]
        public void Close_Calls_Inner_RequestContext()
        {
            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            requestContext.Close();
            Assert.IsTrue(innerRequestContext.CloseCalled, "Close should have been called on the inner RequestContext.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncodingRequestContext.Close calls Close on the inner RequestContext and passes along the timeout parameter.")]
        public void Close_With_TimeOut_Calls_Inner_RequestContext()
        {
            MockRequestContext innerRequestContext = new MockRequestContext();
            HttpMessageEncodingRequestContext requestContext = new HttpMessageEncodingRequestContext(innerRequestContext);
            TimeSpan timeout = new TimeSpan(0, 1, 0);
            requestContext.Close(timeout);
            Assert.IsTrue(innerRequestContext.CloseCalled, "Close should have been called on the inner RequestContext.");
            Assert.AreEqual(timeout, innerRequestContext.Timeout, "HttpMessageEncodingRequestContext.Close should have passed along the timeout instance to the inner RequestContext.");
        }

        #endregion Close Tests
    }
}
