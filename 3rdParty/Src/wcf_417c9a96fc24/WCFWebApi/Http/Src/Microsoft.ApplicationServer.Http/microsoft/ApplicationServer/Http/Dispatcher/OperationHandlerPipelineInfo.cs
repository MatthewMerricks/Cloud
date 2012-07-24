// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// Class that manages all of the <see cref="HttpParameter"/> binding information
    /// for an instance of an <see cref="OperationHandlerPipeline"/>.
    /// </summary>
    internal class OperationHandlerPipelineInfo
    {
        private static readonly HttpOperationHandler requestMessageSourceHandler = new RequestMessageSourceHandler();
        private static readonly HttpOperationHandler responseMessageSinkHandler = new ResponseMessageSinkHandler();

        private int pipelineValuesArraySize;
        private OperationHandlerInfo[] operationHandlerInfo;
        private int serviceOperationIndex;
        private ServiceOperationHandler serviceOperationHandler;

        internal OperationHandlerPipelineInfo(
                                    IEnumerable<HttpOperationHandler> requestHandlers,
                                    IEnumerable<HttpOperationHandler> responseHandlers,
                                    HttpOperationDescription operation)
        {
            if (requestHandlers == null)
            {
                throw Fx.Exception.ArgumentNull("requestHandlers");
            }

            if (responseHandlers == null)
            {
                throw Fx.Exception.ArgumentNull("responseHandlers");
            }

            if (operation == null)
            {
                throw Fx.Exception.ArgumentNull("operation");
            }

            this.serviceOperationHandler = new ServiceOperationHandler(operation);

            List<HttpOperationHandler> handlers = new List<HttpOperationHandler>();
            handlers.Add(requestMessageSourceHandler);
            handlers.AddRange(requestHandlers);
            
            this.serviceOperationIndex = handlers.Count;

            handlers.Add(this.serviceOperationHandler);
            handlers.AddRange(responseHandlers);
            handlers.Add(responseMessageSinkHandler);

            string operationName = string.IsNullOrWhiteSpace(operation.Name) ?
                SR.UnknownName : operation.Name;

            List<HttpParameterBinding> bindings = BindHandlers(handlers, operationName, this.serviceOperationIndex);

            this.operationHandlerInfo = GenerateHandlerInfo(handlers, bindings, operationName, out this.pipelineValuesArraySize);
        }

        /// <summary>
        /// An Enum that indicates if an <see cref="HttpOperationHandler"/> is
        /// a request handler, response handler or the service operation.
        /// </summary>
        private enum HandlerType
        {
            Request,
            ServiceOperation,
            Response
        }

        internal object[] GetEmptyPipelineValuesArray()
        {
            return new object[this.pipelineValuesArraySize];
        }

        internal object[] GetInputValuesForHandler(int handlerIndex, object[] pipelineValues)
        {
            Fx.Assert(handlerIndex >= 0 && handlerIndex < this.operationHandlerInfo.Length, "The 'handlerIndex' parameter should have been in bounds.");
            Fx.Assert(pipelineValues != null, "The 'pipelineValues' parameter should not be null.");
            Fx.Assert(pipelineValues.Length == this.pipelineValuesArraySize, "The 'pipelineValues' array should be size of the generated pipelineValues array.");

            int handlerInputCount = this.operationHandlerInfo[handlerIndex].HandlerInputCount;
            int pipelineValuesOffset = this.operationHandlerInfo[handlerIndex].PipelineValuesOffset;
            object[] handlerInputs = new object[handlerInputCount];

            Array.Copy(pipelineValues, pipelineValuesOffset, handlerInputs, 0, handlerInputCount);

            if (handlerIndex == this.serviceOperationIndex)
            {
                handlerInputs = this.serviceOperationHandler.ValidateAndConvertInput(handlerInputs);
            }

            return handlerInputs;
        }

        internal HttpResponseMessage GetHttpResponseMessage(object[] pipelineValues)
        {
            Fx.Assert(pipelineValues != null, "The 'pipelineValues' parameter should not be null.");
            Fx.Assert(pipelineValues.Length == this.pipelineValuesArraySize, "The 'pipelineValues' array should be size of the generated pipelineValues array.");

            object responseAsObject = pipelineValues[this.pipelineValuesArraySize - 1];

            return (HttpResponseMessage)responseAsObject;
        }

        internal void SetOutputValuesFromHandler(int handlerIndex, object[] handlerOutputValues, object[] pipelineValues)
        {
            Fx.Assert(handlerIndex >= 0 && handlerIndex < this.operationHandlerInfo.Length, "The 'handlerIndex' parameter should have been in bounds.");
            Fx.Assert(handlerOutputValues != null, "The 'pipelineValues' parameter should not be null.");
            Fx.Assert(pipelineValues != null, "The 'pipelineValues' parameter should not be null.");
            Fx.Assert(pipelineValues.Length == this.pipelineValuesArraySize, "The 'pipelineValues' array should be size of the generated pipelineValues array.");

            if (handlerIndex == this.serviceOperationIndex)
            {
                handlerOutputValues = this.serviceOperationHandler.ValidateOutput(handlerOutputValues);
            }

            for (int outputValueIndex = 0; outputValueIndex < handlerOutputValues.Length; outputValueIndex++)
            {
                int[] arrayPositions = this.operationHandlerInfo[handlerIndex].OutputParameterInfo[outputValueIndex].BoundArrayPositions;
                foreach (int arrayPosition in arrayPositions)
                {
                    pipelineValues[arrayPosition] = handlerOutputValues[outputValueIndex];
                }
            }
        }

        internal void SetHttpRequestMessage(HttpRequestMessage request, object[] pipelineValues)
        {
            Fx.Assert(request != null, "The 'request' parameter should not be null.");
            Fx.Assert(pipelineValues != null, "The 'pipelineValues' parameter should not be null.");
            Fx.Assert(pipelineValues.Length == this.pipelineValuesArraySize, "The 'pipelineValues' array should be size of the generated pipelineValues array.");

            int[] arrayPositions = this.operationHandlerInfo[0].OutputParameterInfo[0].BoundArrayPositions;
            foreach (int arrayPosition in arrayPositions)
            {
                pipelineValues[arrayPosition] = request;
            }
        }

        private static OperationHandlerInfo[] GenerateHandlerInfo(List<HttpOperationHandler> handlers, List<HttpParameterBinding> bindings, string operationName, out int pipelineValuesArraySize)
        {
            Fx.Assert(handlers != null, "The 'handlers' parameter should not be null.");
            Fx.Assert(bindings != null, "The 'bindings' parameter should not be null.");
            Fx.Assert(operationName != null, "The 'bindings' parameter should not be null.");

            pipelineValuesArraySize = 0;
            OperationHandlerInfo[] handlerInfo = new OperationHandlerInfo[handlers.Count];

            // First, construct the OperationHandlerInfo[] to determine each handlers's offset
            //  in the pipelineValuesArray.
            for (int handlerIndex = 0; handlerIndex < handlers.Count; handlerIndex++)
            {
                HttpOperationHandler handler = handlers[handlerIndex];
                OperationHandlerInfo info = new OperationHandlerInfo(pipelineValuesArraySize, handler);
                handlerInfo[handlerIndex] = info;
                pipelineValuesArraySize += info.HandlerInputCount;

                handlers[handlerIndex].OperationName = operationName;
            }

            // Second, map the HttpParameterBindings into the OutputParameterInfo array indices.
            for (int handlerIndex = 0; handlerIndex < handlers.Count; handlerIndex++)
            {
                HttpOperationHandler handler = handlers[handlerIndex];

                for (int paramIndex = 0; paramIndex < handler.OutputParameters.Count; paramIndex++)
                {
                    OperationHandlerInfo info = handlerInfo[handlerIndex];
                    info.OutputParameterInfo[paramIndex] = new OutputParameterInfo(
                        bindings.Where(binding => binding.OutputHandlerIndex == handlerIndex &&
                                                   binding.OutputParameterIndex == paramIndex)
                                .Select(binding => handlerInfo[binding.InputHandlerIndex].PipelineValuesOffset +
                                                    binding.InputParameterIndex));
                }
            }

            return handlerInfo;
        }

        private static HandlerType GetHandlerType(int handlerIndex, int serviceOperationIndex)
        {
            if (handlerIndex == serviceOperationIndex)
            {
                return HandlerType.ServiceOperation;
            }

            return handlerIndex > serviceOperationIndex ? HandlerType.Response : HandlerType.Request;
        }

        private static List<HttpParameterBinding> BindHandlers(List<HttpOperationHandler> handlers, string operationName, int serviceOperationIndex)
        {
            Fx.Assert(handlers != null, "The 'handlers' parameter should not be null.");
            Fx.Assert(operationName != null, "The 'operationName' parameter should not be null.");

            List<HttpParameterBinding> pipelineBindings = new List<HttpParameterBinding>();

            for (int inHandlerIndex = handlers.Count - 1; inHandlerIndex >= 1; inHandlerIndex--)
            {
                HttpOperationHandler inHandler = handlers[inHandlerIndex];
                HandlerType inHandlerType = GetHandlerType(inHandlerIndex, serviceOperationIndex);

                for (int inParamIndex = 0; inParamIndex < inHandler.InputParameters.Count; inParamIndex++)
                {
                    HttpParameter inParam = inHandler.InputParameters[inParamIndex];
                    List<HttpParameterBinding> bindings = new List<HttpParameterBinding>();
                    List<HttpParameterBinding> tentativeBindings = new List<HttpParameterBinding>();

                    for (int outHandlerIndex = inHandlerIndex - 1; outHandlerIndex >= 0; outHandlerIndex--)
                    {
                        HttpOperationHandler outHandler = handlers[outHandlerIndex];
                        HandlerType outHandlerType = GetHandlerType(outHandlerIndex, serviceOperationIndex);

                        for (int outParamIndex = 0; outParamIndex < outHandler.OutputParameters.Count; outParamIndex++)
                        {
                            HttpParameter outParam = outHandler.OutputParameters[outParamIndex];
                            
                            if (inParam.IsAssignableFromParameter(outParam.Type))
                            {
                                HttpParameterBinding binding = new HttpParameterBinding();
                                binding.InputHandler = inHandler;
                                binding.InputHandlerIndex = inHandlerIndex;
                                binding.InputParameter = inParam;
                                binding.InputParameterIndex = inParamIndex;
                                binding.InputHandlerType = inHandlerType;
                                binding.OutputHandler = outHandler;
                                binding.OutputHandlerIndex = outHandlerIndex;
                                binding.OutputParameter = outParam;
                                binding.OutputParameterIndex = outParamIndex;
                                binding.OutputHandlerType = outHandlerType;

                                // If their names match or the input is either HttpRequesMessage,
                                //  HttpResponseMessage, or HttpContent, then go ahead and bind
                                if (string.Equals(outParam.Name, inParam.Name, StringComparison.OrdinalIgnoreCase) ||
                                    HttpTypeHelper.IsHttp(inParam.Type))
                                {
                                    bindings.Add(binding);
                                }
                                else
                                {
                                    // Otherwise we will tentatively bind if this is 
                                    //  not a string conversion assignment
                                    if (outParam.Type != TypeHelper.StringType || 
                                        !inParam.ValueConverter.CanConvertFromString)
                                    {
                                        tentativeBindings.Add(binding);
                                    }
                                }
                            }
                        }
                    }

                    if (bindings.Count > 0)
                    {
                        pipelineBindings.AddRange(bindings);
                    }
                    else if (tentativeBindings.Count == 1)
                    {
                        pipelineBindings.AddRange(tentativeBindings);
                    }
                    else if (tentativeBindings.Count > 1)
                    {
                        ThrowForMulitpleTypeOnlyBindings(tentativeBindings, operationName);
                    }
                    else
                    {
                        ThrowForUnboundParameter(inHandler, inParam, inHandlerType, operationName);
                    }
                }
            }

            return pipelineBindings;
        }

        private static void ThrowForMulitpleTypeOnlyBindings(List<HttpParameterBinding> tentativeBindings, string operationName)
        {
            Fx.Assert(tentativeBindings != null, "The 'tentativeBindings' parameter should not be null.");
            Fx.Assert(tentativeBindings.Count > 0, "The 'tentativeBindings' list should not be empty.");
            Fx.Assert(operationName != null, "The 'operationName' parameter should not be null.");

            string exceptionMessage = null;
            HandlerType inHandlerType = tentativeBindings[0].InputHandlerType;
            HttpOperationHandler inHandler = tentativeBindings[0].InputHandler;
            HttpParameter inParameter = tentativeBindings[0].InputParameter;

            switch (inHandlerType)
            {
                case HandlerType.Request:
                    exceptionMessage = SR.RequestHandlerWithMultipleTypeOnlyBindings(
                        HttpOperationHandler.HttpOperationHandlerType.Name,
                        inHandler.ToString(),
                        operationName,
                        inParameter.Name,
                        inParameter.Type.Name);
                    break;
                case HandlerType.ServiceOperation:
                    exceptionMessage = SR.ServiceOperationWithMultipleTypeOnlyBindings(
                        operationName,
                        inParameter.Name,
                        inParameter.Type.Name,
                        HttpOperationHandler.HttpOperationHandlerType.Name);
                    break;
                case HandlerType.Response:
                    exceptionMessage = SR.ResponseHandlerWithMultipleTypeOnlyBindings(
                        HttpOperationHandler.HttpOperationHandlerType.Name,
                        inHandler.ToString(),
                        operationName,
                        inParameter.Name,
                        inParameter.Type.Name);
                    break;
                default:
                    Fx.Assert("The handlerType should have been one of the above cases.");
                    break;
            }

            StringBuilder stringBuilder = new StringBuilder(exceptionMessage);
            foreach (HttpParameterBinding binding in tentativeBindings)
            {
                string parameterMessage = null;
                HandlerType outHandlerType = binding.OutputHandlerType;
                HttpOperationHandler outHandler = binding.OutputHandler;
                HttpParameter outParameter = binding.OutputParameter;
                
                switch (outHandlerType)
                {
                    case HandlerType.Request:
                        parameterMessage = SR.RequestHandlerTypeOnlyOutputParameter(
                            HttpOperationHandler.HttpOperationHandlerType.Name,
                            outHandler.ToString(),
                            outParameter.Name,
                            outParameter.Type.Name);
                        break;
                    case HandlerType.ServiceOperation:
                        parameterMessage = SR.ServiceOperationTypeOnlyOutputParameter(
                            outParameter.Name,
                            outParameter.Type.Name);
                        break;
                    case HandlerType.Response:
                        parameterMessage = SR.ResponseHandlerTypeOnlyOutputParameter(
                            HttpOperationHandler.HttpOperationHandlerType.Name,
                            outHandler.ToString(),
                            outParameter.Name,
                            outParameter.Type.Name);
                        break;
                    default:
                        Fx.Assert("The handlerType should have been one of the above cases.");
                        break;
                }

                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(parameterMessage);
            }

            throw Fx.Exception.AsError(new InvalidOperationException(stringBuilder.ToString()));
        }

        private static void ThrowForUnboundParameter(HttpOperationHandler handler, HttpParameter parameter, HandlerType handlerType, string operationName)
        {
            Fx.Assert(handler != null, "The 'handler' parameter should not be null.");
            Fx.Assert(parameter != null, "The 'parameter' parameter should not be null.");
            Fx.Assert(operationName != null, "The 'operationName' parameter should not be null.");

            if (handler == responseMessageSinkHandler)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.ResponseSinkHandlerWithNoHttpResponseMessageSource(
                            HttpOperationHandler.HttpOperationHandlerType.Name,
                            HttpTypeHelper.HttpResponseMessageType.Name,
                            operationName)));
            }

            switch (handlerType)
            {
                case HandlerType.Request:
                    ThrowForUnboundRequestHandler(handler, parameter, operationName);
                    break;
                case HandlerType.ServiceOperation:
                    ThrowForUnboundServiceOperation(parameter, operationName);
                    break;
                case HandlerType.Response:
                    ThrowForUnboundResponseHandler(handler, parameter, operationName);
                    break;
                default:
                    Fx.Assert("The handlerType should have been one of the above cases.");
                    break;
            }
        }

        private static void ThrowForUnboundRequestHandler(HttpOperationHandler handler, HttpParameter parameter, string operationName)
        {
            Fx.Assert(handler != null, "The 'handler' parameter should not be null.");
            Fx.Assert(parameter != null, "The 'parameter' parameter should not be null.");
            Fx.Assert(operationName != null, "The 'operationName' parameter should not be null.");

            string exceptionMessage = null;

            if (parameter.ValueConverter.CanConvertFromString)
            {
                exceptionMessage = SR.RequestHandlerWithNoPossibleBindingForStringConvertableType(
                    HttpOperationHandler.HttpOperationHandlerType.Name,
                    handler.ToString(),
                    operationName,
                    parameter.Name,
                    parameter.Type.Name,
                    handler.GetType().Name);
            }
            else
            {
                exceptionMessage = SR.RequestHandlerWithNoPossibleBindingForNonStringConvertableType(
                    HttpOperationHandler.HttpOperationHandlerType.Name,
                    handler.ToString(),
                    operationName,
                    parameter.Name,
                    parameter.Type.Name,
                    handler.GetType().Name);
            }

            Fx.Assert(exceptionMessage != null, "The 'exceptionMessage' variable should have been set.");
            throw Fx.Exception.AsError(new InvalidOperationException(exceptionMessage));
        }

        private static void ThrowForUnboundServiceOperation(HttpParameter parameter, string operationName)
        {
            Fx.Assert(parameter != null, "The 'parameter' parameter should not be null.");
            Fx.Assert(operationName != null, "The 'operationName' parameter should not be null.");

            string exceptionMessage = null;

            if (parameter.ValueConverter.CanConvertFromString)
            {
                exceptionMessage = SR.ServiceOperationWithNoPossibleBindingForStringConvertableType(
                    operationName,
                    parameter.Name,
                    parameter.Type.Name,
                    HttpOperationHandler.HttpOperationHandlerType.Name);
            }
            else
            {
                exceptionMessage = SR.ServiceOperationWithNoPossibleBindingForNonStringConvertableType(
                    operationName,
                    parameter.Name,
                    parameter.Type.Name,
                    HttpOperationHandler.HttpOperationHandlerType.Name);
            }

            Fx.Assert(exceptionMessage != null, "The 'exceptionMessage' variable should have been set.");
            throw Fx.Exception.AsError(new InvalidOperationException(exceptionMessage));
        }

        private static void ThrowForUnboundResponseHandler(HttpOperationHandler handler, HttpParameter parameter, string operationName)
        {
            Fx.Assert(handler != null, "The 'handler' parameter should not be null.");
            Fx.Assert(parameter != null, "The 'parameter' parameter should not be null.");
            Fx.Assert(operationName != null, "The 'operationName' parameter should not be null.");

            string exceptionMessage = null;

            if (parameter.ValueConverter.CanConvertFromString)
            {
                exceptionMessage = SR.ResponseHandlerWithNoPossibleBindingForStringConvertableType(
                    HttpOperationHandler.HttpOperationHandlerType.Name,
                    handler.ToString(),
                    operationName,
                    parameter.Name,
                    parameter.Type.Name,
                    handler.GetType().Name);
            }
            else
            {
                exceptionMessage = SR.ResponseHandlerWithNoPossibleBindingForNonStringConvertableType(
                    HttpOperationHandler.HttpOperationHandlerType.Name,
                    handler.ToString(),
                    operationName,
                    parameter.Name,
                    parameter.Type.Name,
                    handler.GetType().Name);
            }

            Fx.Assert(exceptionMessage != null, "The 'exceptionMessage' variable should have been set.");
            throw Fx.Exception.AsError(new InvalidOperationException(exceptionMessage));
        }

        /// <summary>
        /// A class that represents a binding between an output <see cref="HttpParameter"/>
        /// and an input <see cref="HttpParameter"/>.
        /// </summary>
        private class HttpParameterBinding
        {
            internal HttpOperationHandler OutputHandler { get; set; }

            internal HttpParameter OutputParameter { get; set; }
            
            internal int OutputHandlerIndex { get; set; }
            
            internal int OutputParameterIndex { get; set; }
            
            internal HandlerType OutputHandlerType { get; set; }

            internal HttpOperationHandler InputHandler { get; set; }
            
            internal HttpParameter InputParameter { get; set; }
            
            internal int InputHandlerIndex { get; set; }
            
            internal int InputParameterIndex { get; set; }
            
            internal HandlerType InputHandlerType { get; set; }
        }

        /// <summary>
        /// A class that holds all of the binding information for a single <see cref="HttpOperationHandler"/>
        /// for using during runtime.
        /// </summary>
        private class OperationHandlerInfo
        {
            internal OperationHandlerInfo(int offset, HttpOperationHandler handler)
            {
                Fx.Assert(handler != null, "The 'handler' parameter should not be null.");

                this.PipelineValuesOffset = offset;
                this.HandlerInputCount = handler.InputParameters.Count;
                this.OutputParameterInfo = new OutputParameterInfo[handler.OutputParameters.Count];
            }

            internal int PipelineValuesOffset { get; private set; }

            internal int HandlerInputCount { get; private set; }

            internal OutputParameterInfo[] OutputParameterInfo { get; private set; }
        }

        /// <summary>
        /// A class that holds all of the binding information for a single <see cref="HttpParameter"/>
        /// for use during runtime.
        /// </summary>
        private class OutputParameterInfo
        {
            internal OutputParameterInfo(IEnumerable<int> boundArrayPositions)
            {
                Fx.Assert(boundArrayPositions != null, "The 'boundArrayPositions' parameter should not be null.");

                this.BoundArrayPositions = boundArrayPositions.ToArray();
            }

            internal int[] BoundArrayPositions { get; private set; }
        }

        /// <summary>
        /// A dummy <see cref="HttpOperationHandler"/> that is never executed, or even in the 
        /// collection of <see cref="HttpOperationHandler"/> instances used by an <see cref="OperationHandlerPipeline"/>,
        /// but is used as a place holder by the <see cref="OperationHandlerInfo"/> class to
        /// bind the incoming <see cref="HttpRequestMessage"/> to any <see cref="HttpOperationHandler"/>
        /// instances in the <see cref="OperationHandlerPipeline"/> that accept it as an input <see cref="HttpParameter"/>.
        /// </summary>
        private class RequestMessageSourceHandler : HttpOperationHandler
        {
            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetInputParameters"/> that always returns null.
            /// </summary>
            /// <returns>Always returns <c>null</c>.</returns>
            protected sealed override IEnumerable<HttpParameter> OnGetInputParameters()
            {
                return null;
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetOutputParameters"/> that always returns <see cref="HttpParameter.RequestMessage"/>.
            /// </summary>
            /// <returns>Always returns <see cref="HttpParameter.RequestMessage"/>.</returns>
            protected sealed override IEnumerable<HttpParameter> OnGetOutputParameters()
            {
                return new HttpParameter[] { HttpParameter.RequestMessage };
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetOutputParameters"/> that should never be called.
            /// </summary>
            /// <param name="input">An array of input values.</param>
            /// <returns>Always returns <c>null</c>.</returns>
            protected sealed override object[] OnHandle(object[] input)
            {
                Fx.Assert("The OnHandle method of the RequestMessageSourceHandler should never be called.");
                return null;
            }
        }

        /// <summary>
        /// A dummy <see cref="HttpOperationHandler"/> that is never executed, or even in the 
        /// collection of <see cref="HttpOperationHandler"/> instances used by an <see cref="OperationHandlerPipeline"/>,
        /// but is used as a place holder by the <see cref="OperationHandlerInfo"/> class to
        /// bind against any <see cref="HttpOperationHandler"/> instances in the 
        /// <see cref="OperationHandlerPipeline"/> that produce an <see cref="HttpResponseMessage"/> 
        /// as an output <see cref="HttpParameter"/>.
        /// </summary>
        private class ResponseMessageSinkHandler : HttpOperationHandler
        {
            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetInputParameters"/> that always returns <see cref="HttpParameter.ResponseMessage"/>.
            /// </summary>
            /// <returns>Always returns <see cref="HttpParameter.ResponseMessage"/>.</returns>
            protected sealed override IEnumerable<HttpParameter> OnGetInputParameters()
            {
                return new HttpParameter[] { HttpParameter.ResponseMessage };
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetOutputParameters"/> that always returns null.
            /// </summary>
            /// <returns>Always returns <c>null</c>.</returns>
            protected sealed override IEnumerable<HttpParameter> OnGetOutputParameters()
            {
                return null;
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetOutputParameters"/> that should never be called.
            /// </summary>
            /// <param name="input">An array of input values.</param>
            /// <returns>Always returns <c>null</c>.</returns>
            protected sealed override object[] OnHandle(object[] input)
            {
                Fx.Assert("The OnHandle method of the RequestMessageSourceHandler should never be called.");
                return null;
            }
        }

        /// <summary>
        /// A dummy <see cref="HttpOperationHandler"/> that is never executed, or even in the 
        /// collection of <see cref="HttpOperationHandler"/> instances used by an <see cref="OperationHandlerPipeline"/>,
        /// but is used as a place holder by the <see cref="OperationHandlerInfo"/> class to
        /// bind the service operation against the output <see cref="HttpParameter"/> instances of the
        /// request <see cref="HttpOperationHandler"/> instances and against the input <see cref="HttpParameter"/>
        /// instances of the response <see cref="HttpOperationHandler"/> instances.
        /// </summary>
        private class ServiceOperationHandler : HttpOperationHandler
        {
            private HttpOperationDescription httpOperationDescription;

            internal ServiceOperationHandler(HttpOperationDescription httpOperationDescription)
            {
                this.httpOperationDescription = httpOperationDescription;
            }

            internal object[] ValidateAndConvertInput(object[] values)
            {
                Fx.Assert(values != null, "The 'values' parameter should not be null.");

                int parameterCount = this.InputParameters.Count;
                int valueCount = values.Length;

                if (valueCount != parameterCount)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.ServiceOperationReceivedWrongNumberOfValues(
                                this.OperationName,
                                parameterCount,
                                valueCount)));
                }

                int i = 0;
                object[] newValues = new object[valueCount];

                try
                {
                    for (i = 0; i < valueCount; ++i)
                    {
                        newValues[i] = this.InputParameters[i].ValueConverter.Convert(values[i]);
                    }
                }
                catch (Exception innerException)
                {
                    if (Fx.IsFatal(innerException))
                    {
                        throw;
                    }

                    if (!this.InputParameters[i].ValueConverter.IsInstanceOf(values[i]))
                    {
                        throw Fx.Exception.AsError(
                                new InvalidOperationException(
                                    SR.ServiceOperationReceivedWrongType(
                                        this.OperationName,
                                        this.InputParameters[i].Type.Name,
                                        this.InputParameters[i].Name,
                                        values[i].GetType().Name)));
                    }
                    else if (this.InputParameters[i].ValueConverter.CanConvertFromString &&
                        values[i].GetType() == TypeHelper.StringType)
                    {
                        throw Fx.Exception.AsError(
                                new InvalidOperationException(
                                    SR.ServiceOperationFailedToConvertInputString(
                                        this.OperationName,
                                        this.InputParameters[i].Type.Name,
                                        this.InputParameters[i].Name,
                                        innerException.Message),
                                    innerException));
                    }
                    else
                    {
                        throw Fx.Exception.AsError(
                                new InvalidOperationException(
                                    SR.ServiceOperationFailedToGetInnerContent(
                                        this.OperationName,
                                        this.InputParameters[i].Type.Name,
                                        this.InputParameters[i].Name,
                                        values[i].GetType().Name,
                                        innerException.Message),
                                    innerException));
                    }
                }

                return newValues;
            }

            internal object[] ValidateOutput(object[] values)
            {
                int parameterCount = this.OutputParameters.Count;

                if (values == null)
                {
                    return new object[parameterCount];
                }

                int valueCount = values.Length;
                if (valueCount != parameterCount)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.ServiceOperationProducedWrongNumberOfValues(
                                this.OperationName,
                                parameterCount,
                                values.Length)));
                }

                int i = 0;
                for (i = 0; i < valueCount; ++i)
                {
                    if (!this.OutputParameters[i].ValueConverter.IsInstanceOf(values[i]))
                    {
                        throw Fx.Exception.AsError(
                               new InvalidOperationException(
                                   SR.ServiceOperationProducedWrongType(
                                       this.OperationName,
                                       this.OutputParameters[i].Type.Name,
                                       this.OutputParameters[i].Name,
                                       values[i].GetType().Name)));
                    }
                }

                return values;
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetInputParameters"/> that always returns 
            /// the input <see cref="HttpParameter"/> instances of the service operation.
            /// </summary>
            /// <returns>The input <see cref="HttpParameter"/> instances of the service operation.</returns>
            protected override IEnumerable<HttpParameter> OnGetInputParameters()
            {
                return this.httpOperationDescription.InputParameters;
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetOutputParameters"/> that always returns
            /// the output <see cref="HttpParameter"/> instances of the service operation.
            /// </summary>
            /// <returns>The output <see cref="HttpParameter"/> instances of the service operation.</returns>
            protected override IEnumerable<HttpParameter> OnGetOutputParameters()
            {
                List<HttpParameter> outputParameters = new List<HttpParameter>(this.httpOperationDescription.OutputParameters);
                HttpParameter returnValue = this.httpOperationDescription.ReturnValue;
                if (returnValue != null)
                {
                    outputParameters.Insert(0, returnValue);
                }

                return outputParameters;
            }

            /// <summary>
            /// Implementation of <see cref="HttpOperationHandler.OnGetOutputParameters"/> that should never be called.
            /// </summary>
            /// <param name="input">An array of input values.</param>
            /// <returns>Always returns <c>null</c>.</returns>
            protected override object[] OnHandle(object[] input)
            {
                Fx.Assert("The OnHandle method of the ServiceOperationHandler should never be called.");
                return null;
            }
        }
    }
}