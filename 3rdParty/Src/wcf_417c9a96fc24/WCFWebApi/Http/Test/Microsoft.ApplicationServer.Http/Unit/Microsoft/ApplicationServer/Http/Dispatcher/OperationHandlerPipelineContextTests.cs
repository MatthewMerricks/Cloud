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
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(OperationHandlerPipelineContext))]
    public class OperationHandlerPipelineContextTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineContext is internal and concrete.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsNotPublic, "OperationHandlerPipelineContext should be internal.");
            Assert.IsFalse(t.IsAbstract, "OperationHandlerPipelineContext should be concrete");
        }

        #endregion Type

        #region Constructors

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineContext(OperationHandlerPipelineInfo, HttpRequestMessage) initializes OperationHandlerPipelineInfo.")]
        public void Constructor()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(handlers, handlers, operation);
            MOperationHandlerPipelineInfo molePipelineInfo = new MOperationHandlerPipelineInfo(pipelineInfo);
            HttpRequestMessage requestAtCall = null;
            object[] valuesAtCall = null;

            molePipelineInfo.GetEmptyPipelineValuesArray = () => new object[0];
            molePipelineInfo.SetHttpRequestMessageHttpRequestMessageObjectArray = (req, values) =>
                {
                    requestAtCall = req;
                    valuesAtCall = values;
                };
            HttpRequestMessage request = new HttpRequestMessage();

            OperationHandlerPipelineContext context = new OperationHandlerPipelineContext(pipelineInfo, request);

            Assert.IsNotNull(requestAtCall, "HttpRequestMessage was not set in pipeline info.");
            Assert.IsNotNull(valuesAtCall, "Values were not set in pipeline info.");
            HttpAssert.AreEqual(request, requestAtCall);
        }

        #endregion Constructors

        #region Methods

        #region GetInputValues()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetInputValues() calls PipelineInfo.")]
        public void GetInputValuesCallsPipelineInfo()
        {
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(handlers, handlers, operation);
            MOperationHandlerPipelineInfo molePipelineInfo = new MOperationHandlerPipelineInfo(pipelineInfo);
            molePipelineInfo.GetEmptyPipelineValuesArray = () => new object[0];
            molePipelineInfo.SetHttpRequestMessageHttpRequestMessageObjectArray = (req, values) => { };

            bool calledForValues = false;
            int handlerIndexAtCall = -1;
            object[] valuesAtCall = null;
            molePipelineInfo.GetInputValuesForHandlerInt32ObjectArray = (index, values) => 
            { 
                calledForValues = true;
                handlerIndexAtCall = index;
                valuesAtCall = values;
                return new object[0]; 
            };

            HttpRequestMessage request = new HttpRequestMessage();
            OperationHandlerPipelineContext context = new OperationHandlerPipelineContext(pipelineInfo, request);

            object[] returnedValues = context.GetInputValues();

            Assert.IsTrue(calledForValues, "PipelineInfo was not called for its values.");
            Assert.AreEqual(1, handlerIndexAtCall, "Handler index should have been 0.");
            Assert.IsNotNull(valuesAtCall, "Values at call should have not been null.");
            Assert.IsNotNull(returnedValues, "Returned values were null.");
            Assert.AreEqual(0, returnedValues.Length, "Returned values were incorrect length.");
        }

        #endregion GetInputValues()

        #region GetHttpResponseMessage()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetHttpResponseMessage() calls PipelineInfo.")]
        public void GetHttpResponseMessageCallsPipelineInfo()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(handlers, handlers, operation);
            MOperationHandlerPipelineInfo molePipelineInfo = new MOperationHandlerPipelineInfo(pipelineInfo);
            molePipelineInfo.GetEmptyPipelineValuesArray = () => new object[0];
            molePipelineInfo.SetHttpRequestMessageHttpRequestMessageObjectArray = (req, values) => { };
            molePipelineInfo.GetHttpResponseMessageObjectArray = (values) => response;
            OperationHandlerPipelineContext context = new OperationHandlerPipelineContext(pipelineInfo, request);

            HttpResponseMessage responseReturned = context.GetHttpResponseMessage();

            Assert.IsNotNull(responseReturned, "HttpResponseMessage was not returned.");
            HttpAssert.AreEqual(response, responseReturned);
        }

        #endregion GetHttpResponseMessage()

        #region SetOutputValuesAndAdvance()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SetOutputValuesAndAdvance(object[]) calls PipelineInfo.")]
        public void SetOutputValuesAndAdvanceCallsPipelineInfo()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            HttpOperationHandler[] handlers = new HttpOperationHandler[0];
            SHttpOperationDescription operation = new SHttpOperationDescription() { ReturnValue = HttpParameter.ResponseMessage };
            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(handlers, handlers, operation);

            int indexAtCall = -1;
            object[] valuesAtCall = null;
            object[] pipelineValuesAtCall = null;

            MOperationHandlerPipelineInfo molePipelineInfo = new MOperationHandlerPipelineInfo(pipelineInfo);
            molePipelineInfo.GetEmptyPipelineValuesArray = () => new object[0];
            molePipelineInfo.SetHttpRequestMessageHttpRequestMessageObjectArray = (req, values) => { };
            molePipelineInfo.SetOutputValuesFromHandlerInt32ObjectArrayObjectArray = (index, values, pipelineValues) =>
                {
                    indexAtCall = index;
                    valuesAtCall = values;
                    pipelineValuesAtCall = pipelineValues;
                };

            OperationHandlerPipelineContext context = new OperationHandlerPipelineContext(pipelineInfo, request);

            context.SetOutputValuesAndAdvance(new object[0]);

            Assert.AreEqual(1, indexAtCall, "Handler index was not set.");
            Assert.IsNotNull(valuesAtCall, "Values were not set.");
            Assert.IsNotNull(pipelineValuesAtCall, "Pipeline values were not set.");
        }

        #endregion SetOutputValuesAndAdvance()

        #endregion Methods
    }
}
