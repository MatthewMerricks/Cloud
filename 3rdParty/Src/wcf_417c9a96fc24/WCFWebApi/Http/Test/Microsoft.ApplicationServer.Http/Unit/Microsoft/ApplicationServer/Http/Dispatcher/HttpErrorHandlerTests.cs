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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class HttpErrorHandlerTests : UnitTest<HttpErrorHandler>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpErrorHandler is public abstract class.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpErrorHandler should be public.");
            Assert.IsTrue(t.IsClass, "HttpErrorHandler should be a class.");
            Assert.IsTrue(t.IsAbstract, "HttpErrorHandler should be abstract");
        }

        #endregion Type

        #region ProvideResponse Tests

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ProvideResponse() throws for null response.")]
        public void ProvideResponse_Null_Response_Throws()
        {
            Exception error = new InvalidOperationException("problem");
            Message faultMessage = null;

            SHttpErrorHandler errorHandler = new SHttpErrorHandler();
            errorHandler.OnProvideResponseException = (ex) => null;

            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpErrorMessageNullResponse(typeof(SHttpErrorHandler).Name, typeof(HttpResponseMessage).Name, "ProvideResponse"),
                () => ((IErrorHandler)errorHandler).ProvideFault(error, MessageVersion.None, ref faultMessage));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ProvideResponse() can return custom response message.")]
        public void ProvideResponse_Returns_Custom_Response_Message()
        {
            Exception error = new InvalidOperationException("problem");
            HttpResponseMessage customResponseMessage = new HttpResponseMessage();
            Message faultMessage = null;

            SHttpErrorHandler errorHandler = new SHttpErrorHandler();
            errorHandler.OnProvideResponseException = (ex) => customResponseMessage;

            ((IErrorHandler)errorHandler).ProvideFault(error, MessageVersion.None, ref faultMessage);

            Assert.IsNotNull(faultMessage, "ProvideFault cannot yield null response");
            HttpResponseMessage responseMessage = faultMessage.ToHttpResponseMessage();
            Assert.AreSame(customResponseMessage, responseMessage, "ProvideFault should return custom message");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ProvideFault() throws ArgumentNullException for null error argument.")]
        public void ProvideFault_Null_Error_Argument_Throws()
        {
            IErrorHandler errorHandler = new SHttpErrorHandler();
            Message faultMessage = null;
            ExceptionAssert.ThrowsArgumentNull("error", () => errorHandler.ProvideFault(/*error*/ null, MessageVersion.None, ref faultMessage));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ProvideResponse() throws ArgumentNullException for null error argument.")]
        public void ProvideResponse_Null_Error_Argument_Throws()
        {
            HttpErrorHandler errorHandler = new SHttpErrorHandler();
            ExceptionAssert.ThrowsArgumentNull("error", () => errorHandler.ProvideResponse(/*error*/ null));
        }

        #endregion ProvideResponse Tests
    }
}
