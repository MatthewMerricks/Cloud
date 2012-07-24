// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// An <see cref="HttpMessageFormatter"/> that executes an
    /// <see cref="OperationHandlerPipeline"/> for each <see cref="HttpRequestMessage"/>
    /// instance that is dispatched to an operation and for each
    /// <see cref="HttpResponseMessage"/> that is created and returned 
    /// down the channel stack.
    /// </summary>
    internal class OperationHandlerFormatter : HttpMessageFormatter
    {
        private const string OperationHandlerPipelineContextPropertyName = "OperationHandlerPipelineContextPropertyName";
        
        private OperationHandlerPipeline pipeline;
      
        internal OperationHandlerFormatter(OperationHandlerPipeline pipeline)
        {
            Fx.Assert(pipeline != null, "The 'pipeline' parameter should not be null.");

            this.pipeline = pipeline;
        }

        private OperationHandlerPipelineContext CachedPipelineContext
        {
            get
            {
                OperationContext operationContext = OperationContext.Current;
                if (operationContext != null)
                {
                    MessageProperties properties = operationContext.IncomingMessageProperties;
                    Fx.Assert(properties != null, "InComingMessageProperties cannot be null");
                    return properties[OperationHandlerPipelineContextPropertyName] as OperationHandlerPipelineContext;
                }

                return null;
            }

            set
            {
                OperationContext operationContext = OperationContext.Current;
                if (operationContext != null)
                {
                    MessageProperties properties = operationContext.IncomingMessageProperties;
                    Fx.Assert(properties != null, "InComingMessageProperties cannot be null");
                    properties[OperationHandlerPipelineContextPropertyName] = value;
                }
            }
        }

        /// <summary>
        /// Deserializes the incoming <see cref="HttpRequestMessage"/> into the given <paramref name="parameters"/>.
        /// </summary>
        /// <param name="request">The incoming <see cref="HttpRequestMessage"/> containing the content 
        /// that requires deserialization</param>
        /// <param name="parameters">The array of objects into which this method should deserialize.</param>
        protected override void OnDeserializeRequest(HttpRequestMessage request, object[] parameters)
        {
            OperationHandlerPipelineContext pipelineContext = this.pipeline.ExecuteRequestPipeline(request, parameters);
            this.CachedPipelineContext = pipelineContext;
        }

        /// <summary>
        /// Serializes the given <paramref name="parameters"/> values into the outgoing <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="parameters">The array of values that need to be serialialized.</param>
        /// <param name="result">The return value of the operation that needs to be serialized.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> to return.  It cannot be <c>null</c>.</returns>
        protected override HttpResponseMessage OnSerializeReply(object[] parameters, object result)
        {
            OperationHandlerPipelineContext pipelineContext = this.CachedPipelineContext;
            Fx.Assert(pipelineContext != null, "The pipelineContext should always be available.");

            return this.pipeline.ExecuteResponsePipeline(pipelineContext, parameters, result);
        }
    }
}
