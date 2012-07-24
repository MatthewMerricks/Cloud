// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// Thic class is used to bind and execute an ordered collection of <see cref="HttpOperationHandler"/> instances. 
    /// </summary>
    internal class OperationHandlerPipeline
    {
        private int requestHandlersCount;
        private int responseHandlersCount;
        private OperationHandlerPipelineInfo pipelineContextInfo;
        private HttpOperationHandler[] requestHandlers;
        private HttpOperationHandler[] responseHandlers;

        internal OperationHandlerPipeline( 
                                    IEnumerable<HttpOperationHandler> requestHandlers,
                                    IEnumerable<HttpOperationHandler> responseHandlers,
                                    HttpOperationDescription operation)
        {
            if (requestHandlers == null)
            {
                throw Fx.Exception.ArgumentNull("requestHttpOperationHandlers");
            }

            if (responseHandlers == null)
            {
                throw Fx.Exception.ArgumentNull("responseHttpOperationHandlers");
            }

            if (operation == null)
            {
                throw Fx.Exception.ArgumentNull("operation");
            }

            this.requestHandlers = requestHandlers.ToArray();
            this.responseHandlers = responseHandlers.ToArray();

            this.requestHandlersCount = this.requestHandlers.Length;
            this.responseHandlersCount = this.responseHandlers.Length;

            this.pipelineContextInfo = new OperationHandlerPipelineInfo(this.requestHandlers, this.responseHandlers, operation);
        }

        internal OperationHandlerPipelineContext ExecuteRequestPipeline(HttpRequestMessage request, object[] parameters)
        {
            Fx.Assert(request != null, "The 'request' parameter should not be null.");
            Fx.Assert(parameters != null, "The 'parameters' parameter should not be null.");

            OperationHandlerPipelineContext pipelineContext = new OperationHandlerPipelineContext(this.pipelineContextInfo, request);

            for (int handlerIndex = 0; handlerIndex < this.requestHandlersCount; handlerIndex++)
            {
                HttpOperationHandler handler = this.requestHandlers[handlerIndex];
                object[] inputValues = pipelineContext.GetInputValues();
                object[] outputValues = handler.Handle(inputValues);
                pipelineContext.SetOutputValuesAndAdvance(outputValues);
            }

            object[] pipelineParameters = pipelineContext.GetInputValues();
            
            Fx.Assert(pipelineParameters.Length == parameters.Length, "The two parameter object arrays should have the same length.");
            Array.Copy(pipelineParameters, parameters, parameters.Length);

            return pipelineContext;
        }

        internal HttpResponseMessage ExecuteResponsePipeline(OperationHandlerPipelineContext pipelineContext, object[] parameters, object result)
        {
            Fx.Assert(pipelineContext != null, "The 'pipelineContext' parameter should not be null.");
            Fx.Assert(parameters != null, "The 'parameters' parameter should not be null.");

            object[] pipelineParameters = new object[parameters.Length + 1];
            pipelineParameters[0] = result;
            Array.Copy(parameters, 0, pipelineParameters, 1, parameters.Length);
            pipelineContext.SetOutputValuesAndAdvance(pipelineParameters);

            for (int handlerIndex = 0; handlerIndex < this.responseHandlersCount; handlerIndex++)
            {
                HttpOperationHandler handler = this.responseHandlers[handlerIndex];
                object[] inputValues = pipelineContext.GetInputValues();
                object[] outputValues = handler.Handle(inputValues);
                pipelineContext.SetOutputValuesAndAdvance(outputValues);
            }

            return pipelineContext.GetHttpResponseMessage();
        }
    }
}