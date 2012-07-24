// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
  
    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class HttpMessageFormatterTests : UnitTest<HttpMessageFormatter>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpMessageFormatter is public abstract class.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpMessageFormatter should be public.");
            Assert.IsTrue(t.IsClass, "HttpMessageFormatter should be a class.");
            Assert.IsTrue(t.IsAbstract, "HttpMessageFormatter should be abstract");
        }

        #endregion Type

        #region DeserializeRequest Tests

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DeserializeRequest() receives HttpRequestMessage and message parameters.")]
        public void DeserializeRequest_Receives_Message_And_Parameters()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            Message wcfMessage = httpRequestMessage.ToMessage();
            object[] messageParameters = new object[] { "hello", 5.0 };

            SHttpMessageFormatter formatter = new SHttpMessageFormatter();
            formatter.OnDeserializeRequestHttpRequestMessageObjectArray =
                (msg, parameters) =>
                {
                    Assert.AreSame(httpRequestMessage, msg, "DeserializeRequest() did not receive the HttpRequestMessage we specified");
                    Assert.AreSame(messageParameters, parameters, "DeserializeRequest() did not receive the parameters we specified");
                };

            ((IDispatchMessageFormatter)formatter).DeserializeRequest(wcfMessage, messageParameters);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DeserializeRequest() throws ArgumentNullException for null WCF message.")]
        public void DeserializeRequest_Null_Message_Throws()
        {
            object[] parameters = new object[] { "hello", 5.0 };
            IDispatchMessageFormatter formatter = new SHttpMessageFormatter();
            ExceptionAssert.ThrowsArgumentNull("message", () => formatter.DeserializeRequest(/*message*/ null, parameters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DeserializeRequest() throws ArgumentNullException for null parameters.")]
        public void DeserializeRequest_Null_Parameters_Throws()
        {
            Message wcfMessage = new HttpRequestMessage().ToMessage();
            IDispatchMessageFormatter formatter = new SHttpMessageFormatter();
            ExceptionAssert.ThrowsArgumentNull("parameters", () => formatter.DeserializeRequest(wcfMessage, parameters: null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DeserializeRequest() throws InvalidOperationException for null HttpRequestMessage.")]
        public void DeserializeRequest_Null_HttpRequestMessage_Throws()
        {
            Message wcfMessage = Message.CreateMessage(MessageVersion.None, "unused");
            object[] parameters = new object[] { "hello", 5.0 };
            IDispatchMessageFormatter formatter = new SHttpMessageFormatter();
            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpMessageFormatterNullMessage(typeof(SHttpMessageFormatter).Name, typeof(HttpRequestMessage).Name, "DeserializeRequest"),
                () => formatter.DeserializeRequest(wcfMessage, parameters));
        }

        #endregion DeserializeRequest Tests

        #region SerializeReply Tests

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeReply() receives parameters and result.")]
        public void SerializeReply_Receives_Parameters_And_Result()
        {
            object[] messageParameters = new object[] { "hello", 5.0 };
            string messageResult = "hello";
            SHttpMessageFormatter formatter = new SHttpMessageFormatter();
            formatter.OnSerializeReplyObjectArrayObject =
                (parameters, result) =>
                {
                    Assert.AreSame(messageParameters, parameters, "SerializeReply() did not receive the input parameters");
                    Assert.AreSame(messageResult, result, "SerializeReply() did not receive the input result");
                    return new HttpResponseMessage();
                };

            Message responseMessage = ((IDispatchMessageFormatter)formatter).SerializeReply(MessageVersion.None, messageParameters, messageResult);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeReply() returns valid HttpResponseMessage.")]
        public void SerializeReply_Returns_HttpResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            SHttpMessageFormatter formatter = new SHttpMessageFormatter();
            formatter.OnSerializeReplyObjectArrayObject = (parameters, result) => httpResponseMessage;

            Message wcfMessage = ((IDispatchMessageFormatter)formatter).SerializeReply(MessageVersion.None, parameters: new object[0], result: "result");
            Assert.IsNotNull(wcfMessage, "Returned WCF message cannot be null");
            HttpResponseMessage returnedHttpResponseMessage = wcfMessage.ToHttpResponseMessage();
            Assert.AreSame(httpResponseMessage, returnedHttpResponseMessage, "SerializeReply() response message was not the one we returned.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeReply() throws for null parameters argument.")]
        public void SerializeReply_Null_Parameters_Throws()
        {
            IDispatchMessageFormatter formatter = new SHttpMessageFormatter();
            ExceptionAssert.ThrowsArgumentNull("parameters", () => formatter.SerializeReply(MessageVersion.None, /*parameters*/ null, /*result*/ "hello"));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpMessageFormatter.SerializeReply throws for null parameters argument.")]
        public void SerializeReply_HttpMessageFormatter_Null_Parameters_Throws()
        {
            HttpMessageFormatter formatter = new SHttpMessageFormatter();
            ExceptionAssert.ThrowsArgumentNull("parameters", () => formatter.SerializeReply(/*parameters*/ null, /*result*/ "hello"));
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeReply() accepts a null result argument.")]
        public void SerializeReply_Null_Result_Allowed()
        {
            bool receivedNullResult = false;
            SHttpMessageFormatter formatter = new SHttpMessageFormatter();
            formatter.OnSerializeReplyObjectArrayObject =
                (parameters, result) =>
                {
                    receivedNullResult = (result == null);
                    return new HttpResponseMessage();
                };

            ((IDispatchMessageFormatter)formatter).SerializeReply(MessageVersion.None, parameters: new object[0], result: null);
            Assert.IsTrue(receivedNullResult, "Null result did not make it through SerializeReply");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeReply() throws NotSupportedException if MessageVersion is not MessageVersion.None.")]
        public void SerializeReply_MessageVersion_Not_None_Throws()
        {
            IDispatchMessageFormatter formatter = new SHttpMessageFormatter();
            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageFormatterMessageVersion(typeof(SHttpMessageFormatter), typeof(MessageVersion), "None"),
                () => formatter.SerializeReply(MessageVersion.Soap11, parameters: new object[0], result: "result"));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeReply() throws InvalidOperationException for null returned HttpResponseMessage.")]
        public void SerializeReply_Null_HttpResponseMessage_Throws()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            SHttpMessageFormatter formatter = new SHttpMessageFormatter();
            formatter.OnSerializeReplyObjectArrayObject = (parameters, result) => null;

            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpMessageFormatterNullMessage(typeof(SHttpMessageFormatter), typeof(HttpResponseMessage).Name, "SerializeReply"),
                () => ((IDispatchMessageFormatter)formatter).SerializeReply(MessageVersion.None, parameters: new object[0], result: "result"));
        }

        #endregion SerializeReply Tests
    }
}
