// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageTests
    {
        #region Type Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage is an internal, non-abstract class.")]
        public void HttpMessage_Is_An_Internal_Non_Abstract_Class()
        {
            Type type = typeof(HttpMessage);
            Assert.IsTrue(type.IsNotPublic, "HttpMessage should not be public.");
            Assert.IsFalse(type.IsVisible, "HttpMessage should not be visible.");
            Assert.IsFalse(type.IsAbstract, "HttpMessage should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessage should be a class.");
        }

        #endregion Type Tests

        #region Constructor Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage constructor takes an HttpRequestMessage parameter.")]
        public void HttpMessage_Constructor_Takes_An_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            new HttpMessage(request);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage constructor takes an HttpResponseMessage parameter.")]
        public void HttpMessage_Constructor_Takes_An_HttpResponseMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            new HttpMessage(response);
        }

        #endregion Constructor Tests

        #region Property Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.Headers is never null.")]
        public void Headers_Is_Never_Null()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.IsNotNull(message.Headers, "HttpMessage.Headers should never be null.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.Headers always has zero headers.")]
        public void Headers_Count_Is_Zero_By_Default()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual(0, message.Headers.Count, "HttpMessage.Headers.Count should be 0 by default.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.Headers.MessageVersion is always MessageVersion.None.")]
        public void Headers_MessageVersion_Is_Always_None()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual(MessageVersion.None, message.Headers.MessageVersion, "HttpMessage.Headers.MessageVersion should always be MessageVersion.None.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.Properties always has a single 'AllowOutputBatching' property with a value of 'false'.")]
        public void Properties_Has_Only_AllowOutputBatching_MessageProperty()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual(1, message.Properties.Count, "HttpMessage.Properties.Count should be 1.");
            Assert.IsFalse(message.Properties.AllowOutputBatching, "The value of the 'AllowOutputBatching' property should be false.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.IsEmpty is true if the HttpContent length is zero.")]
        public void IsEmpty_Is_True_When_HttpContent_Length_Is_Zero()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new ByteArrayContent(new byte[0]);
            HttpMessage message = new HttpMessage(request);
            Assert.IsTrue(message.IsEmpty, "HttpMessage.IsEmpty should always be 'true' since the byte array was of zero length.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.IsEmpty is true if the HttpContent length is zero.")]
        public void IsEmpty_Is_False_When_HttpContent_Length_Is_Not_Zero()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new ByteArrayContent(new byte[1] { 0 });
            HttpMessage message = new HttpMessage(request);
            Assert.IsFalse(message.IsEmpty, "HttpMessage.IsEmpty should always be 'false' since the byte array was of length '1'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.IsEmpty is false if the HttpContent length is unknown.")]
        public void IsEmpty_Is_False_When_HttpContent_Length_Is_Unknown()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StreamContent(new MockUnseekableStream(new MemoryStream()));
            HttpMessage message = new HttpMessage(request);
            Assert.IsFalse(message.IsEmpty, "HttpMessage.IsEmpty should always be 'false' since the content length is unknown.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.IsEmpty is true if the HttpContent is null.")]
        public void IsEmpty_Is_True_When_HttpContent_Is_Null()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.IsTrue(message.IsEmpty, "HttpMessage.IsEmpty should be 'true' since there is no content.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage will never be a fault.")]
        public void IsFault_Is_False()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.IsFalse(message.IsFault, "HttpMessage.IsFault should always be 'false'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.IsRequest will be 'true' if the HttpRequestMethod constructor overload was used.")]
        public void IsRequest_Is_True_If_HttpRequestMessage_Constructor_Was_Used()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.IsTrue(message.IsRequest, "HttpMessage.IsRequest should be 'true'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.IsRequest will be 'false' if the HttpResponseMethod constructor overload was used.")]
        public void IsRequest_Is_False_If_HttpResponseMessage_Constructor_Was_Used()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);
            Assert.IsFalse(message.IsRequest, "HttpMessage.IsRequest should be 'false'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.State will be MessageState.Created after being created.")]
        public void MessageState_Is_Created_After_Creation()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual(MessageState.Created, message.State, "HttpMessage.State should be MessageState.Created.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.State will be MessageState.Closed after calling close.")]
        public void MessageState_Is_Closed_After_Calling_Closed()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            message.Close();
            Assert.AreEqual(MessageState.Closed, message.State, "HttpMessage.State should be MessageState.Closed.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.MessageVersion is always MessageVersion.None.")]
        public void MessageVersion_Always_None()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual(MessageVersion.None, message.Version, "HttpMessage.Version should always be MessageVersion.None.");
        }

        #endregion Property Tests

        #region CreateBufferedCopy Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.CreateBufferedCopy always throws.")]
        public void CreateBufferedCopy_Always_Returns_An_HttpMessageBuffer()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    MessageBuffer buffer = message.CreateBufferedCopy(5);
                });
        }

        #endregion CreateBufferedCopy Tests

        #region ToString Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a request message with a content length of zero bytes.")]
        public void ToString_With_A_Request_Indicates_Zero_Content_Length()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Content = new ByteArrayContent(new byte[0]);
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual("HTTP request message body with a content length of '0' bytes.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP request message body with a content length of '0' bytes.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a request message with the content length.")]
        public void ToString_With_A_Request_Indicates_Content_Length()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Content = new ByteArrayContent(new byte[3] { 0, 1, 2});
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual("HTTP request message body with a content length of '3' bytes.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP request message body with a content length of '3' bytes.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a request message with a content length of zero if the HttpContent is null.")]
        public void ToString_With_A_Request_And_Null_HttpContent_Indicates_Zero_Content_Length()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual("HTTP request message body with a content length of '0' bytes.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP request message body with a content length of '0' bytes.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a request message with an unknown content length if the HttpContent is not seekable.")]
        public void ToString_With_A_Request_And_UnSeekable_HttpContent_Indicates_Unknown_Content_Length()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Content = new StreamContent(new MockUnseekableStream(new MemoryStream()));
            HttpMessage message = new HttpMessage(request);
            Assert.AreEqual("HTTP request message body with an undetermined content length.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP request message body with an undetermined content length.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a response message with a content length of zero bytes.")]
        public void ToString_With_A_Response_Indicates_Zero_Content_Length()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[0]);
            HttpMessage message = new HttpMessage(response);
            Assert.AreEqual("HTTP response message body with a content length of '0' bytes.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP response message body with a content length of '0' bytes.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a response message with the content length.")]
        public void ToString_With_A_Response_Indicates_Content_Length()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[3] { 0, 1, 2 });
            HttpMessage message = new HttpMessage(response);
            Assert.AreEqual("HTTP response message body with a content length of '3' bytes.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP response message body with a content length of '3' bytes.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a response message with a content length of zero if the HttpContent is null.")]
        public void ToString_With_A_Response_And_Null_HttpContent_Indicates_Zero_Content_Length()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);
            Assert.AreEqual("HTTP response message body with a content length of '0' bytes.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP response message body with a content length of '0' bytes.'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.ToString() indicates that it is a response message with an unknown content length if the HttpContent is not seekable.")]
        public void ToString_With_A_Response_And_UnSeekable_HttpContent_Indicates_Unknown_Content_Length()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StreamContent(new MockUnseekableStream(new MemoryStream()));
            HttpMessage message = new HttpMessage(response);
            Assert.AreEqual("HTTP response message body with an undetermined content length.", message.ToString(), "HttpMessage.ToString() should have returned 'HTTP response message body with an undetermined content length.'.");
        }

        #endregion ToString Tests

        #region Close Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.Close disposes of the HttpRequestMessage.")]
        public void Closing_Disposes_Of_The_HttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Content = new ByteArrayContent(new byte[0]);
            HttpMessage message = new HttpMessage(request);
            message.Close();
            Assert.AreEqual(MessageState.Closed, message.State, "HttpMessage.State should be closed.");

            ExceptionAssert.ThrowsObjectDisposed(
                typeof(ByteArrayContent).FullName,
                () =>
                {
                    request.Content.ReadAsByteArray();
                });
        }    
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.Close disposes of the HttpResponseMessage.")]
        public void Closing_Disposes_Of_The_HttpResponseMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[0]);
            HttpMessage message = new HttpMessage(response);
            message.Close();
            Assert.AreEqual(MessageState.Closed, message.State, "HttpMessage.State should be closed.");

            ExceptionAssert.ThrowsObjectDisposed(
                typeof(ByteArrayContent).FullName,
                () =>
                {
                    response.Content.ReadAsByteArray();
                });           
        }

        #endregion Close Tests

        #region Not Supported Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.GetBodyAttribute always throws.")]
        public void GetBodyAttribute_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.GetBodyAttribute("localName", "nameSpace");
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.GetReaderAtBodyContents always throws.")]
        public void GetReaderAtBodyContents_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new ByteArrayContent(new byte[1] { 0 });
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.GetReaderAtBodyContents();
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.WriteBody always throws.")]
        public void WriteBody_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.WriteBody(new MockXmlDictionaryWriter());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.WriteBodyContents always throws.")]
        public void WriteBodyContents_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.WriteBodyContents(new MockXmlDictionaryWriter());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.WriteMessage allways throws.")]
        public void WriteMessage_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.WriteMessage(new MockXmlDictionaryWriter());
                });      
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.WriteStartBody throws.")]
        public void WriteStartBody_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            MockXmlDictionaryWriter writer = new MockXmlDictionaryWriter();

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.WriteStartBody(writer);
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessage.WriteStartEnvelope throws.")]
        public void WriteStartEnvelope_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpMessage message = new HttpMessage(request);
            MockXmlDictionaryWriter writer = new MockXmlDictionaryWriter();

            ExceptionAssert.Throws<NotSupportedException>(
                SR.MessageReadWriteCopyNotSupported("ToHttpRequestMessage", "ToHttpResponseMessage", typeof(Message).FullName),
                () =>
                {
                    message.WriteStartEnvelope(writer);
                });
        }

        #endregion Not Supported Tests
    }
}
