// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Description.Moles;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(OperationHandlerPipeline))]
    public class OperationHandlerPipelineTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipeline is internal and concrete.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsNotPublic, "OperationHandlerPipeline should be internal.");
            Assert.IsTrue(t.IsClass, "OperationHandlerPipeline should be a class.");
            Assert.IsFalse(t.IsAbstract, "OperationHandlerPipeline should be concrete");
        }

        #endregion Type

        #region Constructors

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipeline() initializes.")]
        public void Constructor()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipeline pipeline = new OperationHandlerPipeline(handlers, handlers, operation);
        }


        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipeline() throws with null request handlers.")]
        public void ConstructorThrowsWithNullRequestHandlers()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            ExceptionAssert.ThrowsArgumentNull("requestHttpOperationHandlers", () => new OperationHandlerPipeline(null, handlers, operation));
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipeline() throws with null response handlers.")]
        public void ConstructorThrowsWithNullResponseHandlers()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            ExceptionAssert.ThrowsArgumentNull("responseHttpOperationHandlers", () => new OperationHandlerPipeline(handlers, null, operation));
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipeline() throws with null operation.")]
        public void ConstructorThrowsWithNullOperation()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            ExceptionAssert.ThrowsArgumentNull("operation", () => new OperationHandlerPipeline(handlers, handlers, null));
        }

        #endregion Constructors

        #region Methods

        #region ExecuteRequestPipeline

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ExecuteRequestPipeline() executes with no handlers.")]
        public void ExecuteRequestPipeline()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipeline pipeline = new OperationHandlerPipeline(handlers, handlers, operation);

            HttpRequestMessage request = new HttpRequestMessage();
            OperationHandlerPipelineContext context = pipeline.ExecuteRequestPipeline(request, new object[0]);

            Assert.IsNotNull(context, "Execute returned null context.");
        }

        #endregion ExecuteRequestPipeline

        #region ExecuteResponsePipeline

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ExecuteResponsePipeline() executes with no handlers.")]
        public void ExecuteResponsePipeline()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipeline pipeline = new OperationHandlerPipeline(handlers, handlers, operation);

            HttpRequestMessage request = new HttpRequestMessage();
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };

            OperationHandlerPipelineContext context = pipeline.ExecuteRequestPipeline(request, new object[0]);

            //// TODO: what is the convention for returning an HttpResponse from the operation?
            HttpResponseMessage actualResponse = pipeline.ExecuteResponsePipeline(context, new object[0], response);

            Assert.IsNotNull(actualResponse, "Execute returned null response.");
        }

        #endregion ExecuteResponsePipeline

        #endregion Methods
    }
}
