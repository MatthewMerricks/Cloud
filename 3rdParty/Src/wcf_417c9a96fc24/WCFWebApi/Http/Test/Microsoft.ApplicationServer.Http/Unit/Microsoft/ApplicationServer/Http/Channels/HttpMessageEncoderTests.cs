// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class HttpMessageEncoderTests
    {
        #region Type Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder is a nested, private class.")]
        public void HttpMessageEncoder_Is_Nested_Private_Class()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            Type type = encoder.GetType();
            Assert.IsTrue(type.IsNestedPrivate, "HttpMessageEncoder should be nested and private.");
            Assert.IsFalse(type.IsVisible, "HttpMessageEncoder should not be visible.");
            Assert.IsFalse(type.IsAbstract, "HttpMessageEncoder should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessageEncoder should be a class.");
        }

        #endregion Type Tests

        #region Property Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ContentType always returns an empty string.")]
        public void ContentType_Is_Empty_String()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;
            Assert.AreEqual(string.Empty, encoder.ContentType, "HttpMessageEncoder.ContentType should have returned an empty string.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.MediaType always returns an empty string.")]
        public void MediaType_Is_Empty_String()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;
            Assert.AreEqual(string.Empty, encoder.MediaType, "HttpMessageEncoder.MediaType should have returned an empty string.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.MessageVersion always returns MessageVersion.None.")]
        public void MessageVersion_Is_None()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;
            Assert.AreEqual(MessageVersion.None, encoder.MessageVersion, "HttpMessageEncoder.MessageVersion should have returned MessageVersion.None.");
        }

        #endregion Property Tests

        #region IsContentTypeSupported Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.IsContentTypeSupported returns 'true' for all non-null, content types.")]
        public void IsContentTypeSupported_Is_True()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;
            Assert.IsTrue(encoder.IsContentTypeSupported("someContentType"), "HttpMessageEncoder.IsContentTypeSupported should have returned 'true'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.IsContentTypeSupported throws for null content types.")]
        public void IsContentTypeSupported_Throws()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            ExceptionAssert.ThrowsArgumentNull(
                "contentType",
                () =>
                {
                    encoder.IsContentTypeSupported(null);
                });
        }

        #endregion IsContentTypeSupported Tests

        #region ReadMessage Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage.")]
        public void ReadMessage_Returns_HttpMessage()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4};
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            Message message = encoder.ReadMessage(buffer, new MockBufferManager(), "someType/someSubType");
            Assert.IsNotNull(message, "HttpMessageEncoder.ReadMessage should not have returned null.");
            Assert.IsInstanceOfType(message, typeof(HttpMessage), "HttpMessageEncoder.ReadMessage should have returned an HttpMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage with IsRequest set to 'true'.")]
        public void ReadMessage_Returns_HttpMessage_With_IsRequest_True()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4};
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            HttpMessage message = encoder.ReadMessage(buffer, new MockBufferManager(), "someType/someSubType") as HttpMessage;
            Assert.IsTrue(message.IsRequest, "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with IsRequest set to 'true'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage with HttpContent created from a copy of the buffer parameter.")]
        public void ReadMessage_Returns_HttpMessage_With_HttpContent_Created_From_The_Buffer()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            HttpMessage message = encoder.ReadMessage(buffer, new MockBufferManager(), "someType/someSubType") as HttpMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();
            byte[] requestBytes = request.Content.ReadAsByteArray();

            Assert.IsNotNull(requestBytes, "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with content.");
            Assert.AreNotSame(bytes, requestBytes, "HttpMessageEncoder.ReadMessage should have returned a new byte array instance.");
            Assert.AreEqual(5, requestBytes.Length, "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with content of five bytes.");
            CollectionAssert.AreEquivalent(bytes, requestBytes, "HttpMessageEncoder.ReadMessage should have copied the byte array exactly.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage if the bufferManager parameter is null.")]
        public void ReadMessage_Throws_If_The_BufferManager_Parameter_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;
        
            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            ExceptionAssert.ThrowsArgumentNull(
                "bufferManager",
                () =>
                {
                    encoder.ReadMessage(buffer, null, "someContentType");
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage with HttpContent a null content-type if the contentType parameter is null.")]
        public void ReadMessage_Returns_HttpMessage_With_HttpContent_Null_Content_Type()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            HttpMessage message = encoder.ReadMessage(buffer, new MockBufferManager(), null) as HttpMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();
            Assert.IsNull(request.Content.Headers.ContentType, "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with a null content type.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage with HttpContent content-type determined by the contentType parameter.")]
        public void ReadMessage_Returns_HttpMessage_With_HttpContent_Given_By_ContentType_Parameter()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            HttpMessage message = encoder.ReadMessage(buffer, new MockBufferManager(), "someType/someSubType") as HttpMessage;
            HttpRequestMessage request = message.ToHttpRequestMessage();
            Assert.AreEqual("someType/someSubType", request.Content.Headers.ContentType.ToString(), "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with a content type of 'someType/someSubType'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage with no headers.")]
        public void ReadMessage_Returns_HttpMessage_With_No_Headers()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            HttpMessage message = encoder.ReadMessage(buffer, new MockBufferManager(), "someType/someSubType") as HttpMessage;
            Assert.AreEqual(0, message.Headers.Count, "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with no headers.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns an HttpMessage with 'AllowOutputBatching' and 'Encoder' properties.")]
        public void ReadMessage_Returns_HttpMessage_Only_AllowOutputBatching_MessageProperty_And_Encoder()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            HttpMessage message = encoder.ReadMessage(buffer, new MockBufferManager(), "someType/someSubType") as HttpMessage;
            Assert.AreEqual(2, message.Properties.Count, "HttpMessage.Properties.Count should be 2.");
            Assert.IsFalse(message.Properties.AllowOutputBatching, "The value of the 'AllowOutputBatching' property should be false.");
            Assert.AreSame(encoder, message.Properties.Encoder, "The value of the 'Encoder' property should be a reference to the message encoder used.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage returns the buffer to the buffer manager.")]
        public void ReadMessage_Returns_A_Message_That_Returns_The_Array_To_The_Buffer_Manager_When_Closed()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

            MockBufferManager bufferManager = new MockBufferManager();
            HttpMessage message = encoder.ReadMessage(buffer, bufferManager, "someType/someSubType") as HttpMessage;
            Assert.IsFalse(bufferManager.ReturnBufferCalled, "HttpMessageEncoder.ReadMessage should not have returned the buffer to the buffer manager yet.");

            message.Close();
            Assert.IsTrue(bufferManager.ReturnBufferCalled, "HttpMessageEncoder.ReadMessage should have returned the buffer to the buffer manager.");
            Assert.AreSame(bytes, bufferManager.BufferReturned, "HttpMessageEncoder.ReadMessage should have returned the original buffer instance to the buffer manager.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage that takes a stream parameter returns an HttpMessage.")]
        public void ReadMessage_With_Stream_Returns_HttpMessage()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                Message message = encoder.ReadMessage(stream, 5, "type/subType");
                Assert.IsNotNull(message, "HttpMessageEncoder.ReadMessage should not have returned null.");
                Assert.IsInstanceOfType(message, typeof(HttpMessage), "HttpMessageEncoder.ReadMessage should have returned an HttpMessage.");
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage that takes a stream parameter returns an HttpMessage with IsRequest set to 'true'.")]
        public void ReadMessage_With_Stream_Returns_HttpMessage_With_IsRequest_True()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                HttpMessage message = encoder.ReadMessage(stream, 5, "type/subType") as HttpMessage;
                Assert.IsTrue(message.IsRequest, "HttpMessageEncoder.ReadMessage should have returned an HttpMessage with IsRequest set to 'true'.");
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage does not advance the stream parameter.")]
        public void ReadMessage_With_Stream_Does_Not_Advance_The_Stream()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                HttpMessage message = encoder.ReadMessage(stream, 5, "type/subType") as HttpMessage;
                Assert.AreEqual(0, stream.Position, "HttpMessageEncoder.ReadMessage should not have advanced the stream parameter.");
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.ReadMessage throws if the stream parameter is null.")]
        public void ReadMessage_With_Stream_Throws_If_Stream_Parameter_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            ExceptionAssert.ThrowsArgumentNull(
                "stream",
                () =>
                {
                    encoder.ReadMessage(null, 5, "someContentType");
                });
        }

        #endregion ReadMessage Tests

        #region WriteMessage Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage returns an ArraySegment with the HttpContent from the HttpResponseMessage of the HttpMessage.")]
        public void WriteMessage_Returns_An_ArraySegment_With_The_Content_From_The_HttpMessage()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            response.Content = new ByteArrayContent(bytes);
            HttpMessage message = new HttpMessage(response);

            ArraySegment<byte> buffer = encoder.WriteMessage(message, int.MaxValue, new MockBufferManager());
            Assert.IsNotNull(buffer, "HttpMessageEncoder.WriteMessage should not have returned null.");
            CollectionAssert.AreEquivalent(bytes, buffer.Array, "HttpMessageEncoder.WriteMessage should have returned the content of the HttpMessage.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage returns an empty ArraySegment if the HttpContent from the HttpResponseMessage of the HttpMessage is null.")]
        public void WriteMessage_Returns_Empty_ArraySegment_If_HttpMessage_Content_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);

            ArraySegment<byte> buffer = encoder.WriteMessage(message, int.MaxValue, new MockBufferManager());
            Assert.AreEqual(0, buffer.Count, "HttpMessageEncoder.WriteMessage should have returned an empty ArraySegment.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage returns returns an ArraySegment with a size of the byte count of the HttpContent.")]
        public void WriteMessage_Returns_An_ArraySegment_With_Size_Of_Content_Byte_Count()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            response.Content = new ByteArrayContent(bytes);
            response.Content.LoadIntoBuffer();
            HttpMessage message = new HttpMessage(response);

            ArraySegment<byte> buffer = encoder.WriteMessage(message, int.MaxValue, new MockBufferManager());
            Assert.AreEqual(5, buffer.Count, "HttpMessageEncoder.WriteMessage should have returned an ArraySegment with the size limited by the HttpContent byte count.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the byte count of the HttpContent is greater than the maxMessageSize parameter.")]
        public void WriteMessage_Throws_If_MaxMessageSize_Is_Exceeded()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            response.Content = new ByteArrayContent(bytes);
            response.Content.LoadIntoBuffer();
            HttpMessage message = new HttpMessage(response);

            ExceptionAssert.Throws <QuotaExceededException>(
                null,
                () =>
                {
                    ArraySegment<byte> buffer2 = encoder.WriteMessage(message, 2, new MockBufferManager());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage takes a buffer from the bufferManager.")]
        public void WriteMessage_Takes_A_Buffer_From_The_BufferManager()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);

            MockBufferManager bufferManager = new MockBufferManager();
            ArraySegment<byte> buffer = encoder.WriteMessage(message, int.MaxValue, bufferManager);
            Assert.IsTrue(bufferManager.TakeBufferCalled, "HttpMessageEncoder.WriteMessage should have taken a buffer from the bufferManager.");
            Assert.AreSame(buffer.Array, bufferManager.BufferTaken, "HttpMessageEncoder.WriteMessage should have returned the array taken from the bufferManager.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the maxMessageSize parameter is negative.")]
        public void WriteMessage_Throws_If_MaxMessageSize_Parameter_Is_Negative()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);

            ExceptionAssert.ThrowsArgumentOutOfRange(
                "maxMessageSize",
                () =>
                {
                    encoder.WriteMessage(message, -1, new MockBufferManager());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the messageOffset parameter is negative.")]
        public void WriteMessage_Throws_If_MessageOffsert_Parameter_Is_Negative()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);

            ExceptionAssert.ThrowsArgumentOutOfRange(
                "messageOffset",
                () =>
                {
                    encoder.WriteMessage(message, 5, new MockBufferManager(), -1);
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the messageOffset parameter is greater than the maxMessageSize parameter.")]
        public void WriteMessage_Throws_If_MessageOffset_Is_Greater_Than_MaxMessageSize()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);

            ExceptionAssert.Throws<ArgumentException>(
                SR.ParameterMustBeLessThanOrEqualSecondParameter("messageOffset", "maxMessageSize"),
                () =>
                {
                    encoder.WriteMessage(message, 5, new MockBufferManager(), 6);
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the bufferManager parameter is null.")]
        public void WriteMessage_Throws_If_BufferManager_Parameter_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);

            ExceptionAssert.ThrowsArgumentNull(
                "bufferManager",
                () =>
                {
                    encoder.WriteMessage(message, 5, null);
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the message parameter is null.")]
        public void WriteMessage_Throws_If_Message_Parameter_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            ExceptionAssert.ThrowsArgumentNull(
                "message",
                () =>
                {
                    encoder.WriteMessage(null, 5, new MockBufferManager());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the message is not an HttpMessage with IsRequest set to 'false'.")]
        public void WriteMessage_Throws_If_HttpMessage_IsRequest_Is_Not_False()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(request);

            ExceptionAssert.Throws <InvalidOperationException>(
                SR.MessageInvalidForHttpMessageEncoder(typeof(HttpBinding).FullName, HttpMessageExtensionMethods.ToMessageMethodName, typeof(HttpResponseMessage).FullName),
                () =>
                {
                    encoder.WriteMessage(message, 5, new MockBufferManager());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage throws if the message state is not MessageState.Created.")]
        public void WriteMessage_Throws_If_The_Message_State_Is_Not_Created()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);
            message.Close();

            ExceptionAssert.Throws<InvalidOperationException>(
                null,
                () => 
                {
                    encoder.WriteMessage(message, 5, new MockBufferManager());
                });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage that takes a stream parameter writes the HttpContent to the stream.")]
        public void WriteMessage_That_Takes_A_Stream_Writes_Message_Content_To_The_Stream()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            byte[] bytes = new byte[5] { 0, 1, 2, 3, 4 };
            response.Content = new ByteArrayContent(bytes);
            HttpMessage message = new HttpMessage(response);

            using (MemoryStream stream = new MemoryStream())
            {
                encoder.WriteMessage(message, stream);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                byte[] writtenBytes = new byte[stream.Length];
                stream.Read(writtenBytes, 0, writtenBytes.Length);
                CollectionAssert.AreEquivalent(bytes, writtenBytes, "HttpMessageEncoder.WriteMessage should have written the content to the stream.");
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage that takes a stream parameter writes nothing if the HttpContent is null.")]
        public void WriteMessage_That_Takes_A_Stream_Writes_Nothing_If_Message_Content_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            HttpMessage message = new HttpMessage(response);

            using (MemoryStream stream = new MemoryStream())
            {
                encoder.WriteMessage(message, stream);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                Assert.AreEqual(0, stream.Length, "HttpMessageEncoder.WriteMessage should not have written anything to the stream since the content was null.");
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage that takes a stream parameter throws if the message parameter is null.")]
        public void WriteMessage_That_Takes_A_Stream_Throws_If_The_Message_Parameter_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            using (MemoryStream stream = new MemoryStream())
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "message",
                    () =>
                    {
                        encoder.WriteMessage(null, stream);
                    });
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageEncoder.WriteMessage that takes a stream parameter throws if the stream parameter is null.")]
        public void WriteMessage_That_Takes_A_Stream_Throws_If_The_Stream_Parameter_Is_Null()
        {
            HttpMessageEncoderFactory factory = new HttpMessageEncoderFactory();
            MessageEncoder encoder = factory.Encoder;

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[5] { 0, 1, 2, 3, 4 });
            HttpMessage message = new HttpMessage(response);

            ExceptionAssert.ThrowsArgumentNull(
                "stream",
                () =>
                {
                    encoder.WriteMessage(message, null);
                });
        }       
        #endregion WriteMessage Tests
    }
}
