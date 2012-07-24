// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageInspectorTests
    {
        #region AfterReceiveRequest Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.AfterReceiveRequest receives HttpRequestMessage from WCF Message.")]
        public void AfterReceiveRequest_Receives_HttpRequestMessage()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            Message wcfMessage = httpRequestMessage.ToMessage();
            InstanceContext context = new InstanceContext(new EmptyService());

            SHttpMessageInspector inspector = new SHttpMessageInspector();
            inspector.OnAfterReceiveRequestHttpRequestMessage =
                (actualMessage) =>
                {
                    Assert.AreSame(httpRequestMessage, actualMessage, "AfterReceiveRequest did not receive the HttpRequestMessage");
                    return /*state*/ null;
                };

            ((IDispatchMessageInspector)inspector).AfterReceiveRequest(ref wcfMessage, null, context);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.AfterReceiveRequest can return a custom state value.")]
        public void AfterReceiveRequest_Returns_Custom_State_Value()
        {
            string stringInstance = "hello";
            Message wcfMessage = new HttpRequestMessage().ToMessage();
            InstanceContext context = new InstanceContext(new EmptyService());

            SHttpMessageInspector inspector = new SHttpMessageInspector();
            inspector.OnAfterReceiveRequestHttpRequestMessage = (localMessage) => stringInstance;

            object returnedValue = ((IDispatchMessageInspector)inspector).AfterReceiveRequest(ref wcfMessage, null, context);
            Assert.AreSame(stringInstance,returnedValue, "AfterReceiveRequest return value is not the one we returned.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.AfterReceiveRequest throws ArgumentNullException for null WCF message argument.")]
        public void AfterReceiveRequest_Null_Message_Throws()
        {
            IDispatchMessageInspector inspector = new SHttpMessageInspector();
            Message wcfMessage = null;
            InstanceContext context = new InstanceContext(new EmptyService());
            ExceptionAssert.ThrowsArgumentNull("request", () => inspector.AfterReceiveRequest(ref wcfMessage, null, context));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageInspector.AfterReceiveRequest throws ArgumentNullException for null request argument.")]
        public void AfterReceiveRequest_HttpMessageInspector_Null_Message_Throws()
        {
            HttpMessageInspector inspector = new SHttpMessageInspector();
            InstanceContext context = new InstanceContext(new EmptyService());
            ExceptionAssert.ThrowsArgumentNull("request", () => inspector.AfterReceiveRequest(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.AfterReceiveRequest throws InvalidOperationException for null HttpRequestMessage argument.")]
        public void AfterReceiveRequest_Null_HttpRequestMessage_Throws()
        {
            IDispatchMessageInspector inspector = new SHttpMessageInspector();
            Message wcfMessage = Message.CreateMessage(MessageVersion.None, "unused"); ;
            InstanceContext context = new InstanceContext(new EmptyService());
            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpMessageInspectorNullMessage(typeof(SHttpMessageInspector).Name, typeof(HttpRequestMessage).Name, "AfterReceiveRequest"),
                () =>
                {
                    inspector.AfterReceiveRequest(ref wcfMessage, null, context);
                });
        }

        #endregion AfterReceiveRequest Tests

        #region BeforeSendReply Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.BeforeSendReply receives HttpResponseMessage from WCF Message.")]
        public void BeforeSendReply_Receives_HttpResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            Message wcfMessage = httpResponseMessage.ToMessage();

            SHttpMessageInspector inspector = new SHttpMessageInspector();
            inspector.OnBeforeSendReplyHttpResponseMessageObject =
                (actualMessage, state) =>
                {
                    Assert.AreSame(httpResponseMessage, actualMessage, "BeforeSendReply did not receive the message we provided.");
                };

            ((IDispatchMessageInspector)inspector).BeforeSendReply(ref wcfMessage, correlationState: null);
            Assert.AreSame(httpResponseMessage, wcfMessage.ToHttpResponseMessage(), "Expected embedded HttpResponseMessage to remain unmodified");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.BeforeSendReply receives custom correlation state")]
        public void BeforeSendReply_Receives_Custom_CorrelationState()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            Message wcfMessage = httpResponseMessage.ToMessage();
            object correlationState = "Hello";

            SHttpMessageInspector inspector = new SHttpMessageInspector();
            inspector.OnBeforeSendReplyHttpResponseMessageObject =
                (actualMessage, state) =>
                {
                    Assert.AreSame(correlationState, state, "BeforeSendReply did not receive the state we provided.");
                };

            ((IDispatchMessageInspector)inspector).BeforeSendReply(ref wcfMessage, correlationState);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.BeforeSendReply throws ArgumentNullException for null WCF message argument")]
        public void BeforeSendReply_Null_Message_Throws()
        {
            IDispatchMessageInspector inspector = new SHttpMessageInspector();
            Message wcfMessage = null;
            ExceptionAssert.ThrowsArgumentNull("reply", () => inspector.BeforeSendReply(ref wcfMessage, correlationState: null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageInspector.BeforeSendReply throws ArgumentNullException for null message argument")]
        public void BeforeSendReply_HttpMessageInspector_Null_Message_Throws()
        {
            HttpMessageInspector inspector = new SHttpMessageInspector();
            ExceptionAssert.ThrowsArgumentNull("response", () => inspector.BeforeSendReply(null, "correlationState"));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("IDispatchMessageInspector.BeforeSendReplythrows InvalidOperationException for null HttpResponseMessage argument")]
        public void BeforeSendReply_Null_HttpResponseMessage_Throws()
        {
            IDispatchMessageInspector inspector = new SHttpMessageInspector();
            Message wcfMessage = Message.CreateMessage(MessageVersion.None, "unused");
            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpMessageInspectorNullMessage(typeof(SHttpMessageInspector).Name, typeof(HttpResponseMessage).Name, "BeforeSendReply"),
                () => inspector.BeforeSendReply(ref wcfMessage, correlationState: null));
        }

        #endregion BeforeSendReply Tests
    }
}
