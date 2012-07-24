// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Description.Moles;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Net.Http.Headers;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(OperationHandlerPipelineInfo))]
    public class OperationHandlerPipelineInfoTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo is internal and concrete.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsFalse(t.IsPublic, "OperationHandlerPipelineInfo should be not public.");
            Assert.IsFalse(t.IsAbstract, "OperationHandlerPipelineInfo should not be abstract.");
        }

        #endregion Type

        #region Constructors

        #region OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws with null requestHandlers.")]
        public void ConstructorThrowsWithNullRequestHandlers()
        {
            List<HttpOperationHandler> handlers = new List<HttpOperationHandler>();
            handlers.Add(new SHttpOperationHandler());

            ExceptionAssert.ThrowsArgumentNull(
                "requestHandlers", 
                () => new OperationHandlerPipelineInfo(null, handlers, new SHttpOperationDescription()));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws with null responseHandlers.")]
        public void ConstructorThrowsWithNullResponseHandlers()
        {
            List<HttpOperationHandler> handlers = new List<HttpOperationHandler>();
            handlers.Add(new SHttpOperationHandler());

            ExceptionAssert.ThrowsArgumentNull(
                "responseHandlers", 
                () => new OperationHandlerPipelineInfo(handlers, null, new SHttpOperationDescription()));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws with null operation.")]
        public void ConstructorThrowsWithNullOperation()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            requestHandlers.Add(new SHttpOperationHandler());

            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();
            responseHandlers.Add(new SHttpOperationHandler());

            ExceptionAssert.ThrowsArgumentNull(
                "operation",
                () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) accepts an empty requestHandlers collection.")]
        public void ConstructorAcceptsAnEmptyRequestHandlersCollection()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();

            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();
            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => null;
            handler.OnGetOutputParameters01 = () => null;
            responseHandlers.Add(handler);

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) accepts an empty responseHandlers collection.")]
        public void ConstructorAcceptsAnEmptyResponseHandlersCollection()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => null;
            handler.OnGetOutputParameters01 = () => null;
            requestHandlers.Add(handler);

            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();
            
            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) calls OnGetInputParameters() on all of the requestHandlers.")]
        public void ConstructorCallsOnGetInputParametersOfAllRequestHandlers()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            bool onGetInputParamters1Called = false;
            requestHandler1.OnGetOutputParameters01 = () => null;
            requestHandler1.OnGetInputParameters01 =
                () =>
                {
                    onGetInputParamters1Called = true;
                    return null;
                };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler2);
            bool onGetInputParamters2Called = false;
            requestHandler2.OnGetOutputParameters01 = () => null;
            requestHandler2.OnGetInputParameters01 =
                () =>
                {
                    onGetInputParamters2Called = true;
                    return null;
                };  

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            Assert.IsTrue(onGetInputParamters1Called, "The OnGetInputParameters() method of requestHandler1 was not called.");
            Assert.IsTrue(onGetInputParamters2Called, "The OnGetInputParameters() method of requestHandler2 was not called.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) calls OnGetOutputtParameters() on all of the requestHandlers.")]
        public void ConstructorCallsOnGetOutputParametersOfAllRequestHandlers()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            bool onGetOutputParamters1Called = false;
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 =
                () =>
                {
                    onGetOutputParamters1Called = true;
                    return null;
                };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler2);
            bool onGetOutputParamters2Called = false;
            requestHandler2.OnGetInputParameters01 = () => null;
            requestHandler2.OnGetOutputParameters01 =
                () =>
                {
                    onGetOutputParamters2Called = true;
                    return null;
                };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            Assert.IsTrue(onGetOutputParamters1Called, "The OnGetInputParameters() method of requestHandler1 was not called.");
            Assert.IsTrue(onGetOutputParamters2Called, "The OnGetInputParameters() method of requestHandler2 was not called.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) does not call OnHandle() on any of the requestHandlers.")]
        public void ConstructorDoesNotCallOnHandleOfAnyRequestHandlers()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => null;
            requestHandler1.OnHandleObjectArray = 
                (inputs) => 
                {
                    Assert.Fail("OnHandle() was called.");
                    return null;
                };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler2);
            requestHandler2.OnGetInputParameters01 = () => null;
            requestHandler2.OnGetOutputParameters01 = () => null;
            requestHandler2.OnHandleObjectArray =
                (inputs) =>
                {
                    Assert.Fail("OnHandle() was called.");
                    return null;
                };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) calls OnGetInputParameters() on all of the responseHandlers.")]
        public void ConstructorCallsOnGetInputParametersOfAllResponseHandlers()
        {
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            bool onGetInputParamters1Called = false;
            responseHandler1.OnGetOutputParameters01 = () => null;
            responseHandler1.OnGetInputParameters01 =
                () =>
                {
                    onGetInputParamters1Called = true;
                    return null;
                };

            SHttpOperationHandler responseHandler2 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler2);
            bool onGetInputParamters2Called = false;
            responseHandler2.OnGetOutputParameters01 = () => null;
            responseHandler2.OnGetInputParameters01 =
                () =>
                {
                    onGetInputParamters2Called = true;
                    return null;
                };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            Assert.IsTrue(onGetInputParamters1Called, "The OnGetInputParameters() method of responseHandler1 was not called.");
            Assert.IsTrue(onGetInputParamters2Called, "The OnGetInputParameters() method of responseHandler2 was not called.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) calls OnGetOutputtParameters() on all of the responseHandlers.")]
        public void ConstructorCallsOnGetOutputParametersOfAllResponseHandlers()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();          

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            bool onGetOutputParamters1Called = false;
            responseHandler1.OnGetInputParameters01 = () => null;
            responseHandler1.OnGetOutputParameters01 =
                () =>
                {
                    onGetOutputParamters1Called = true;
                    return null;
                };

            SHttpOperationHandler responseHandler2 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler2);
            bool onGetOutputParamters2Called = false;
            responseHandler2.OnGetInputParameters01 = () => null;
            responseHandler2.OnGetOutputParameters01 =
                () =>
                {
                    onGetOutputParamters2Called = true;
                    return null;
                };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            Assert.IsTrue(onGetOutputParamters1Called, "The OnGetOutputParameters() method of responseHandler1 was not called.");
            Assert.IsTrue(onGetOutputParamters2Called, "The OnGetOutputParameters() method of responseHandler2 was not called.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) does not call OnHandle() on any of the responseHandlers.")]
        public void ConstructorDoesNotCallOnHandleOfAnyResponseHandlers()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();
            
            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => null;
            responseHandler1.OnGetOutputParameters01 = () => null;
            responseHandler1.OnHandleObjectArray =
                (inputs) =>
                {
                    Assert.Fail("OnHandle() was called.");
                    return null;
                };

            SHttpOperationHandler responseHandler2 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler2);
            responseHandler2.OnGetInputParameters01 = () => null;
            responseHandler2.OnGetOutputParameters01 = () => null;
            responseHandler2.OnHandleObjectArray =
                (inputs) =>
                {
                    Assert.Fail("OnHandle() was called.");
                    return null;
                };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.ReturnValue = HttpParameter.ResponseMessage;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) does not throw if the OperationHandlerPipeline can bind successfully.")]
        public void ConstructorDoesNotThrowIfTheOperationHandlerPipelineCanBindSuccessfully()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anInt", typeof(string)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("anInt", typeof(int)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;            

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws during binding if nothing produces an HttpResponseMessage.")]
        public void ConstructorThrowsWhenBindingIfNothingProducesAnHttpResponseMessage()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";

            string exceptionMessage = SR.ResponseSinkHandlerWithNoHttpResponseMessageSource(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                HttpTypeHelper.HttpResponseMessageType.Name,
                "operationName");

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a response HttpOperationHandler input parameter can not be bound because there are multiple type-only matches.")]
        public void ConstructorThrowsIfAResponseHandlerInputHasMultipleTypeOnlyBindings()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandler1.CallBase = true;
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime1", typeof(DateTime)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.CallBase = true;
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.OutputParameters.Add(new HttpParameter("dateTime2", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandler1.CallBase = true;
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime3", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            string exceptionMessage = SR.ResponseHandlerWithMultipleTypeOnlyBindings(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                responseHandler1.ToString(),
                "operationName",
                "dateTime3",
                "DateTime");
            string paramMessage1 = SR.ServiceOperationTypeOnlyOutputParameter(
                "dateTime2",
                "DateTime");
            string paramMessage2 = SR.RequestHandlerTypeOnlyOutputParameter(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler1.ToString(),
                "dateTime1",
                "DateTime");
            string completeMessage = string.Format("{0}{3}{1}{3}{2}", exceptionMessage, paramMessage1, paramMessage2, Environment.NewLine);

            ExceptionAssert.Throws<InvalidOperationException>(completeMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a response HttpOperationHandler input parameter can not bound.")]
        public void ConstructorThrowsIfAResponseHandlerInputCanNotBeBound()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.OutputParameters.Add(new HttpParameter("someOutput", typeof(string)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandler1.CallBase = true;
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("pocoType", typeof(PocoType)) };
            responseHandler1.OnGetOutputParameters01 = () => null;
            
            string exceptionMessage = SR.ResponseHandlerWithNoPossibleBindingForNonStringConvertableType(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                responseHandler1.ToString(),
                "operationName",
                "pocoType",
                "PocoType",
                responseHandler1.GetType().Name);

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a response HttpOperationHandler input parameter can not bound and it can be conveted to from a string.")]
        public void ConstructorThrowsIfAResponseHandlerInputCanNotBeBoundAndItCanBeConvertedToFromAString()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.OutputParameters.Add(new HttpParameter("someOutput", typeof(string)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandler1.CallBase = true;
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("someInt", typeof(int)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            string exceptionMessage = SR.ResponseHandlerWithNoPossibleBindingForStringConvertableType(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                responseHandler1.ToString(),
                "operationName",
                "someInt",
                "Int32",
                responseHandler1.GetType().Name);

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a service operation input parameter can not be bound because there are multiple type-only matches.")]
        public void ConstructorThrowsIfAServiceOperationInputHasMultipleTypeOnlyBindings()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandler1.CallBase = true;
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime1", typeof(DateTime)) };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandler2.CallBase = true;
            requestHandlers.Add(requestHandler2);
            requestHandler2.OnGetInputParameters01 = () => null;
            requestHandler2.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime2", typeof(DateTime)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("dateTime3", typeof(DateTime)));

            string exceptionMessage = SR.ServiceOperationWithMultipleTypeOnlyBindings(
                "operationName",
                "dateTime3",
                "DateTime",
                HttpOperationHandler.HttpOperationHandlerType.Name);
            string paramMessage1 = SR.RequestHandlerTypeOnlyOutputParameter(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler2.ToString(),
                "dateTime2",
                "DateTime");
            string paramMessage2 = SR.RequestHandlerTypeOnlyOutputParameter(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler1.ToString(),
                "dateTime1",
                "DateTime");
            string completeMessage = string.Format("{0}{3}{1}{3}{2}", exceptionMessage, paramMessage1, paramMessage2, Environment.NewLine);

            ExceptionAssert.Throws<InvalidOperationException>(completeMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a service operation input parameter can not bound.")]
        public void ConstructorThrowsIfAServiceOperationInputCanNotBeBound()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("someOutput", typeof(int)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("pocoType", typeof(PocoType)));

            string exceptionMessage = SR.ServiceOperationWithNoPossibleBindingForNonStringConvertableType(
                "operationName",
                "pocoType",
                "PocoType",
                HttpOperationHandler.HttpOperationHandlerType.Name);

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a service operation input parameter can not bound and it can be conveted to from a string.")]
        public void ConstructorThrowsIfAServiceOperationInputCanNotBeBoundAndItCanBeConvertedToFromAString()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("someOutput", typeof(string)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("someInt", typeof(int)));


            string exceptionMessage = SR.ServiceOperationWithNoPossibleBindingForStringConvertableType(
                "operationName",
                "someInt",
                "Int32",
                HttpOperationHandler.HttpOperationHandlerType.Name);

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a request HttpOperationHandler input parameter can not be bound because there are multiple type-only matches.")]
        public void ConstructorThrowsIfARequestHandlerInputHasMultipleTypeOnlyBindings()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandler1.CallBase = true;
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime1", typeof(DateTime)) };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandler2.CallBase = true;
            requestHandlers.Add(requestHandler2);
            requestHandler2.OnGetInputParameters01 = () => null;
            requestHandler2.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime2", typeof(DateTime)) };

            SHttpOperationHandler requestHandler3 = new SHttpOperationHandler();
            requestHandler3.CallBase = true;
            requestHandlers.Add(requestHandler3);
            requestHandler3.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("dateTime3", typeof(DateTime)) };
            requestHandler3.OnGetOutputParameters01 = () => null;

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;

            string exceptionMessage = SR.RequestHandlerWithMultipleTypeOnlyBindings(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler3.ToString(),
                "operationName",
                "dateTime3",
                "DateTime");
            string paramMessage1 = SR.RequestHandlerTypeOnlyOutputParameter(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler2.ToString(),
                "dateTime2",
                "DateTime");
            string paramMessage2 = SR.RequestHandlerTypeOnlyOutputParameter(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler1.ToString(),
                "dateTime1",
                "DateTime");
            string completeMessage = string.Format("{0}{3}{1}{3}{2}", exceptionMessage, paramMessage1, paramMessage2, Environment.NewLine);

            ExceptionAssert.Throws<InvalidOperationException>(completeMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a request HttpOperationHandler input parameter can not bound.")]
        public void ConstructorThrowsIfARequestHandlerInputCanNotBeBound()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("someOutput", typeof(string)) };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandler2.CallBase = true;
            requestHandlers.Add(requestHandler2);
            requestHandler2.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("pocoType", typeof(PocoType)) };
            requestHandler2.OnGetOutputParameters01 = () => null;

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;

            string exceptionMessage = SR.RequestHandlerWithNoPossibleBindingForNonStringConvertableType(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler2.ToString(),
                "operationName",
                "pocoType",
                "PocoType",
                 requestHandler2.GetType().Name);

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription) throws if a request HttpOperationHandler input parameter can not bound and it can be conveted to from a string.")]
        public void ConstructorThrowsIfARequestHandlerInputCanNotBeBoundAndItCanBeConvertedToFromAString()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => null;
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("someOutput", typeof(string)) };

            SHttpOperationHandler requestHandler2 = new SHttpOperationHandler();
            requestHandler2.CallBase = true;
            requestHandlers.Add(requestHandler2);
            requestHandler2.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("someInt", typeof(int)) };
            requestHandler2.OnGetOutputParameters01 = () => null;

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;

            string exceptionMessage = SR.RequestHandlerWithNoPossibleBindingForStringConvertableType(
                HttpOperationHandler.HttpOperationHandlerType.Name,
                requestHandler2.ToString(),
                "operationName",
                "someInt",
                "Int32",
                requestHandler2.GetType().Name);

            ExceptionAssert.Throws<InvalidOperationException>(exceptionMessage, () => new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation));
        }

        #endregion OperationHandlerPipelineInfo(IEnumerable<HttpOperationHandler>, IEnumerable<HttpOperationHandler>, HttpOperationDescription)

        #endregion Constructors

        #region Methods

        #region GetEmptyPipelineValueArray()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetEmptyPipelineValueArrayReturnsArray() returns an array instance in which the size is determined by the number of input parameters in the OperationHandlerPipeline.")]
        public void GetEmptyPipelineValueArrayReturnsAnArrayInstance()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anInt", typeof(string)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("anInt", typeof(int)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            object[] array = pipelineInfo.GetEmptyPipelineValuesArray();
            Assert.AreEqual(4, array.Length, "The returned array length was not the count of input parameters plus one for the HttpResponseMessage.");
        }

        #endregion GetEmptyPipelineValueArray()

        #region GetHttpResponseMessage()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetHttpResponseMessage(object[]) returns the HttpResponseMessage instance in the last indice of the object[].")]
        public void GetHttpResponseMessageReturnsAnHttpResponseMessage()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anInt", typeof(string)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("anInt", typeof(int)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            object[] array = pipelineInfo.GetEmptyPipelineValuesArray();

            HttpResponseMessage response = new HttpResponseMessage();
            array[array.Length - 1] = response;
            HttpResponseMessage responseFromArray = pipelineInfo.GetHttpResponseMessage(array);

            Assert.AreSame(response, responseFromArray, "GetHttpResponseMessage() did not return the HttpResponseMessage from the last indice of the array.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetHttpResponseMessage(object[]) returns the null if the last indice of the object[] is null.")]
        public void GetHttpResponseMessageReturnsNullIfNoHttpResponseMessage()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anInt", typeof(string)) };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(new HttpParameter("anInt", typeof(int)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            object[] array = pipelineInfo.GetEmptyPipelineValuesArray();

            array[array.Length - 1] = null;
            HttpResponseMessage responseFromArray = pipelineInfo.GetHttpResponseMessage(array);

            Assert.IsNull(responseFromArray, "GetHttpResponseMessage() did not return the null value from the last indice of the array.");
        }

        #endregion GetHttpResponseMessage()

        #region SetHttpRequestMessage()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SetHttpRequestMessage(HttpRequestMessage, object[]) sets references to the HttpRequestMessage at only those array indices that represent input parameters bound to the HttpRequestMessage.")]
        public void SetHttpRequestMessageAddsHttpRequestMessageToTheArray()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anotherMessage", typeof(HttpRequestMessage)), HttpParameter.RequestHeaders };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(HttpParameter.RequestHeaders);
            operation.InputParameters.Add(new HttpParameter("aThirdMessage", typeof(HttpRequestMessage)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            object[] array = pipelineInfo.GetEmptyPipelineValuesArray();

            HttpRequestMessage request = new HttpRequestMessage();
            pipelineInfo.SetHttpRequestMessage(request, array);

            Assert.AreSame(request, array[0], "The HttpRequestMessage was not added to the first array indice although the input parameter is bound to the HttpRequestMessage.");
            Assert.IsNull(array[1], "The HttpRequestMessage was added to the second array indice although the input parameter is not bound to the HttpRequestMessage.");
            Assert.AreSame(request, array[2], "The HttpRequestMessage was not added to the third array indice although the input parameter is bound to the HttpRequestMessage.");
            Assert.IsNull(array[3], "The HttpRequestMessage was added to the fourth array indice although the input parameter is not bound to the HttpRequestMessage.");
            Assert.IsNull(array[4], "The HttpRequestMessage was added to the fifth array indice although the input parameter is not bound to the HttpRequestMessage.");
        }

        #endregion SetHttpRequestMessage()

        #region GetInputValuesForHandler()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetInputValuesForHandler(int, object[]) returns an object array with references to the instances in the input array that correspond to the handler's inputs.")]
        public void GetInputValuesForHandlerReturnsInputsForAGivenHandler()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anotherMessage", typeof(HttpRequestMessage)), HttpParameter.RequestHeaders };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(HttpParameter.RequestHeaders);
            operation.InputParameters.Add(new HttpParameter("aThirdMessage", typeof(HttpRequestMessage)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            object[] array = pipelineInfo.GetEmptyPipelineValuesArray();

            object obj1 = new object();
            array[0] = obj1;

            object obj2 = new HttpRequestMessage().Headers;
            array[1] = obj2;

            object obj3 = new HttpRequestMessage();
            array[2] = obj3;

            object obj4 = new object();
            array[3] = obj4;

            object[] requestSourceInputs = pipelineInfo.GetInputValuesForHandler(0, array);
            object[] requestHandlerInputs = pipelineInfo.GetInputValuesForHandler(1, array);
            object[] serviceOperationInputs = pipelineInfo.GetInputValuesForHandler(2, array);
            object[] responseHandlerInputs = pipelineInfo.GetInputValuesForHandler(3, array);
            object[] responseSinkInputs = pipelineInfo.GetInputValuesForHandler(4, array);

            Assert.AreEqual(0, requestSourceInputs.Length, "GetInputValuesForHandler returned the wrong number of input values for the request source handler.");

            Assert.AreEqual(1, requestHandlerInputs.Length, "GetInputValuesForHandler returned the wrong number of input values for the request handler.");
            Assert.AreSame(obj1, requestHandlerInputs[0], "GetInputValuesForHandler returned the wrong input value for the request handler.");

            Assert.AreEqual(2, serviceOperationInputs.Length, "GetInputValuesForHandler returned the wrong number of input values for the service operation.");
            Assert.AreSame(obj2, serviceOperationInputs[0], "GetInputValuesForHandler returned the wrong input value for the service operation.");
            Assert.AreSame(obj3, serviceOperationInputs[1], "GetInputValuesForHandler returned the wrong input value for the service operation.");

            Assert.AreEqual(1, responseHandlerInputs.Length, "GetInputValuesForHandler returned the wrong number of input values for the response handler.");
            Assert.AreSame(obj4, responseHandlerInputs[0], "GetInputValuesForHandler returned the wrong input value for the response handler.");

            Assert.AreEqual(1, responseSinkInputs.Length, "GetInputValuesForHandler returned the wrong number of input values for the response sink handler.");
        }

        #endregion GetInputValuesForHandler()

        #region SetOutputValuesFromHandler()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SetOutputValuesFromHandler(int, object[], object[]) sets references to the output values instances in the array indices that correspond to input parameters that are bound to the given output parameters.")]
        public void SetOutputValuesFromHandlerSetsValuesOnBoundInputs()
        {
            List<HttpOperationHandler> requestHandlers = new List<HttpOperationHandler>();
            List<HttpOperationHandler> responseHandlers = new List<HttpOperationHandler>();

            SHttpOperationHandler requestHandler1 = new SHttpOperationHandler();
            requestHandlers.Add(requestHandler1);
            requestHandler1.OnGetInputParameters01 = () => new HttpParameter[] { HttpParameter.RequestMessage };
            requestHandler1.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("anotherMessage", typeof(HttpRequestMessage)), HttpParameter.RequestHeaders };

            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "operationName";
            operation.ReturnValue = HttpParameter.ResponseMessage;
            operation.InputParameters.Add(HttpParameter.RequestHeaders);
            operation.InputParameters.Add(new HttpParameter("aThirdMessage", typeof(HttpRequestMessage)));
            operation.OutputParameters.Add(new HttpParameter("unknown", typeof(DateTime)));

            SHttpOperationHandler responseHandler1 = new SHttpOperationHandler();
            responseHandlers.Add(responseHandler1);
            responseHandler1.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("aDateTime", typeof(DateTime)) };
            responseHandler1.OnGetOutputParameters01 = () => null;

            OperationHandlerPipelineInfo pipelineInfo = new OperationHandlerPipelineInfo(requestHandlers, responseHandlers, operation);
            object[] array = pipelineInfo.GetEmptyPipelineValuesArray();

            object obj1 = new object();
            object obj2 = new object();
            object[] requestHandlerOutputs = new object[] { obj1, obj2 };

            object obj3 = new HttpResponseMessage();
            object obj4 = DateTime.Now;
            object[] serviceOperationOutputs = new object[] { obj3, obj4 };

            pipelineInfo.SetOutputValuesFromHandler(1, requestHandlerOutputs, array);

            Assert.AreSame(obj1, array[2], "SetOutputValuesFromHandler didn't set the correct output values for the request handler.");
            Assert.AreSame(obj2, array[1], "SetOutputValuesFromHandler didn't set the correct output values for the request handler.");

            pipelineInfo.SetOutputValuesFromHandler(2, serviceOperationOutputs, array);

            Assert.AreSame(obj3, array[4], "SetOutputValuesFromHandler didn't set the correct output values for the service operation.");
            Assert.AreSame(obj4, array[3], "SetOutputValuesFromHandler didn't set the correct output values for the service operation.");
        }

        #endregion GetInputValuesForHandler()

        #endregion Methods
    }
}
